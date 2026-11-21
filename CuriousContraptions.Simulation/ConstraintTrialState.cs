using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Simulation;

internal readonly record struct PrismaticTrialState(float CommandSpeed, float HoldCoordinate, float StartCoordinate,
    float MotorImpulse, float EffortMultiplier, float StepWork, float Work, float TickWork,
    bool Holding, bool MotorActive, bool MovingContact, PrismaticDrivePhase Phase)
{
    public static PrismaticTrialState Capture(SimJoint joint) => new(joint.CommandSpeed, joint.HoldCoordinate,
        joint.StartCoordinate, joint.MotorImpulse, joint.EffortMultiplier, joint.StepWork, joint.Work, joint.TickWork,
        joint.Holding, joint.MotorActive, joint.MovingContact, joint.Phase);
    public void Restore(SimJoint joint)
    {
        joint.CommandSpeed = CommandSpeed; joint.HoldCoordinate = HoldCoordinate; joint.StartCoordinate = StartCoordinate;
        joint.MotorImpulse = MotorImpulse; joint.EffortMultiplier = EffortMultiplier;
        joint.StepWork = StepWork; joint.Work = Work; joint.TickWork = TickWork;
        joint.Holding = Holding; joint.MotorActive = MotorActive; joint.MovingContact = MovingContact; joint.Phase = Phase;
    }
}

internal readonly record struct RevoluteTrialState(float Travel, float Work)
{
    public static RevoluteTrialState Capture(SimRevoluteJoint joint) => new(joint.TravelAngle, joint.Work);
    public void Restore(SimRevoluteJoint joint) { joint.TravelAngle = Travel; joint.Work = Work; }
}

internal readonly record struct TransmissionTrialState(float WindowImpulse, float StepWork, float StepPositive,
    float Work, float PositiveWork, bool Engaged, SimPrismaticRetention.TrialState? Retention)
{
    public static TransmissionTrialState Capture(SimMechanicalTransmission value) => new(value.WindowImpulse,
        value.StepWork, value.StepPositive, value.Work, value.PositiveWork, value.Engaged, value.Retention?.CaptureTrial());
    public void Restore(SimMechanicalTransmission value)
    {
        value.WindowImpulse = WindowImpulse; value.StepWork = StepWork; value.StepPositive = StepPositive;
        value.Work = Work; value.PositiveWork = PositiveWork; value.Engaged = Engaged;
        if (Retention is { } state) value.Retention!.RestoreTrial(state);
    }
}

internal readonly record struct TensileTrialState(float Travel, bool Active, ConstraintRowData Row, Vector3 UnitA, Vector3 UnitB)
{
    public static TensileTrialState Capture(SimTensileRoute value) => new(value.Travel, value.Active, value.Row, value.UnitA, value.UnitB);
    public void Restore(SimTensileRoute value)
    {
        value.Travel = Travel; value.Active = Active; value.Row = Row; value.UnitA = UnitA; value.UnitB = UnitB;
    }
}
