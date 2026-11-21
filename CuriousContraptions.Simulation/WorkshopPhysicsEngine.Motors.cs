using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private PrismaticTrialWorkspace _motorTrial = new();
    private GuideWorkspace _guides = new();
    private int _trialRestitutionStamp;

    private static void ValidatePrismaticCommands(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, PhysicsTickInputs inputs)
    {
        if (inputs.PrismaticCommands is null) throw new ArgumentException("Missing prismatic command table.");
        var supplied = new HashSet<PrismaticConstraintId>();
        foreach (var command in inputs.PrismaticCommands)
            if (!Enum.IsDefined(command.Command) || !supplied.Add(command.Joint))
                throw new ArgumentException("Duplicate or undefined prismatic command.");
        for (var index = 0; index < layout.Prismatics; index++)
        {
            var drive = PhysicsGpuAbi.ReadPrismaticDrive(world, index);
            if (drive is { Selection: PrismaticTargetSelection.Directional } directional && !supplied.Remove(directional.Joint))
                throw new ArgumentException("Missing directional prismatic command.");
        }
        if (supplied.Count != 0) throw new ArgumentException("Prismatic command names no directional drive.");
    }

    private void SolveBoundedSubstep(float dt, SoftCoefficients soft)
    {
        foreach (var collider in _colliders) WorkshopCollisionKernels.RefreshCollider(collider);
        PrepareBroadphase(dt);
        _trialRestitutionStamp = _restitutionStamp;
        var requiresSnapshot = false;
        foreach (var joint in _joints) requiresSnapshot |= joint.MotorActive;
        // Inactive motors measure zero work, so only an active motor can require a replay snapshot.
        if (requiresSnapshot)
            _motorTrial.Capture(_dynamicBodies, _allBodies.Count, _joints, _revoluteJoints, _transmissions, _tensile, _candidatePairs);
        SolveIntegratedTrial(dt, soft);
        if (!MotorBudgetsHold(dt))
        {
            _motorTrial.MarkAffected(_joints, dt);
            RestoreTrial(0f);
            SolveIntegratedTrial(dt, soft);
            // All affected motor impulses are zero. An independent component must replay identically:
            // failure here is a missing coupling or captured-state defect, not a numerical convergence retry.
            if (!MotorBudgetsHold(dt))
                throw new InvalidOperationException("Prismatic trial changed an independent component.");
            var feasible = 0f;
            var upper = 1f;
            var current = 0f;
            for (var refinement = 0; refinement < 8; refinement++)
            {
                current = (feasible + upper) * .5f;
                RestoreTrial(current);
                SolveIntegratedTrial(dt, soft);
                if (MotorBudgetsHold(dt)) feasible = current;
                else upper = current;
            }
            if (current != feasible)
            {
                RestoreTrial(feasible);
                SolveIntegratedTrial(dt, soft);
                _ = MotorBudgetsHold(dt);
            }
        }
        foreach (var joint in _joints)
        {
            joint.Work += joint.StepWork;
            joint.TickWork += joint.StepWork;
        }
    }

    private bool MotorBudgetsHold(float dt)
    {
        var feasible = true;
        foreach (var joint in _joints) feasible &= PrismaticMotorSolver.MeasureWork(joint, dt);
        return feasible;
    }

    private void RestoreTrial(float multiplier)
    {
        GuideWorkspace.Detach(_dynamicBodies);
        _restitutionStamp = _trialRestitutionStamp;
        _motorTrial.Restore(_dynamicBodies, _joints, _revoluteJoints, _transmissions, _tensile, multiplier);
        foreach (var collider in _colliders) WorkshopCollisionKernels.RefreshCollider(collider);
    }

    private void SolveIntegratedTrial(float dt, SoftCoefficients soft)
    {
        // A replayed trial starts from the restored window and counters, so the failed trial's credit and new window never survive.
        foreach (var joint in _joints) PrismaticConstraintSolver.PrepareJointRows(joint, dt);
        _guides.Prepare(_dynamicBodies, _joints, true, true);
        ApplyContinuousImpulses();
        PrepareContacts(dt);
        WarmStartConstraints(dt);
        for (var iteration = 0; iteration < WorkshopConstraintSolver.SolverIterations; iteration++)
        {
            PrismaticConstraintSolver.SweepConstraints(_joints, true);
            RevoluteConstraintSolver.SweepConstraints(_revoluteJoints, true);
            foreach (var transmission in _transmissions) MechanicalTransmissionSolver.Solve(transmission);
            foreach (var tensile in _tensile) TensileConstraintSolver.Solve(tensile, true);
            WorkshopConstraintSolver.SweepManifolds(_manifolds, ContactPass.Solve, soft, dt);
            PrismaticMotorSolver.Solve(_joints, dt);
        }
        _guides.FlushReactions();
        // All applied channels use this same fixed-pose midpoint, before integration and relaxation.
        MeasureContinuousWindow();
        CloseWorkWindow();
        OpenWorkWindow();
        IntegrateBodies(dt);
        FinishIntegratedTrial(dt, soft);
    }

    private void FinishIntegratedTrial(float dt, SoftCoefficients soft)
    {
        PrepareRelaxation(dt);
        for (var iteration = 0; iteration < WorkshopConstraintSolver.RelaxIterations; iteration++)
        {
            PrismaticConstraintSolver.SweepConstraints(_joints, false);
            RevoluteConstraintSolver.SweepConstraints(_revoluteJoints, false);
            foreach (var transmission in _transmissions) MechanicalTransmissionSolver.Solve(transmission);
            foreach (var tensile in _tensile) TensileConstraintSolver.Solve(tensile, false);
            WorkshopConstraintSolver.SweepManifolds(_manifolds, ContactPass.Relax, soft, dt);
        }
        foreach (var transmission in _transmissions) transmission.Retention?.SampleRelaxedRate();

        WorkshopConstraintSolver.RestitutionPass(_manifolds, _restitutionBodies, ++_restitutionStamp);
        _guides.FlushReactions();
        foreach (var body in _dynamicBodies) WorkshopConstraintSolver.SettleVelocity(body);
    }
}
