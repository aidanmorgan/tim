using System;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>The domain rules of Workshop commands and responses that every encoder and decoder of the one codec
/// (<see cref="WorkshopWireFlatBuffers"/>) applies.</summary>
public static class WorkshopMessages
{
    public static bool Matches(WorkshopCommand command, SimulationEpoch epoch, AuthorityRevision revision,
        RuntimeSessionId session, CadenceRevision cadence, ProjectionEpoch projection) =>
        command.Session == session && command.Epoch == epoch && command.Cadence == cadence && command.Projection == projection &&
        (command.RevisionKind == ExpectedRevisionKind.Any || command.Revision == revision);

    /// <summary>A committed read past admission carries its committed motion; admission (tick 0) has no preceding interval.</summary>
    internal static void ValidatePhysicalRead(WorkshopRead read)
    {
        if (read.PhysicalRevision.Value < read.Tick.Value || (read.Epoch.Value == 0 && !read.PhysicalRevision.IsAdmission))
            throw new ArgumentException("Invalid installed physical commit identity.");
        read.Bodies.ValidateTime(read.Epoch, read.Tick);
        ChamberReadValidation.ValidateBodies(read);
        GasReadValidation.ValidateBodies(read);
        CoordinateTravelReadValidation.Bodies(read);
        for (var i = 0; i < read.Bodies.Count; i++) read.Bodies[i].Validate();
        if (read.Motion is { } motion) motion.ValidateBinding(read.Bodies, read.Tick);
        else if (read.Tick.Value != 0) throw new ArgumentException("Committed motion description is required.");
        for (var i = 0; i < read.Activations.Count; i++)
        {
            var value = read.Activations[i]; value.Validate();
            var requiresDynamicBody = value.Kind is not (ActivationOccurrenceKind.OscillatorTick or ActivationOccurrenceKind.ThresholdReached or
                ActivationOccurrenceKind.TimerElapsed or ActivationOccurrenceKind.ContactLoad);
            if (value.Phase == ActivationPhase.Latched && ((requiresDynamicBody && !read.Bodies.TryGet(value.Body, out _)) ||
                value.EventOrdinal > read.Tick.Value * 8 || read.Tick.Value == 0))
                throw new ArgumentException("Activation event does not belong to a running physical world.");
        }
        for (var i = 0; i < read.Captures.Count; i++)
        {
            var value = read.Captures[i]; value.Validate();
            if (!read.Bodies.TryGet(value.Target,out _) || value.EventOrdinal > WorkshopCadenceSettings.PhysicalOrdinalLimit)
                throw new ArgumentException("Capture time exceeds the world duration.");
        }
    }

    internal static void ValidatePresentationOwners(WorkshopRead read, ICompiledScene construction)
    {
        construction.Activation.ValidateOrigins(read.Activations, read.Timers, read.Tick, construction.Physics);
        construction.Activation.ValidateCommittedMemory(read.Memory, read.Tick, construction.Physics);
        ValidateEnergyStoreOwners(read, construction);
        ChamberReadValidation.ValidateOwners(read, construction);
        GasReadValidation.ValidateOwners(read, construction);
        CoordinateTravelReadValidation.Owners(read, construction);
        ValidateAirflowOwners(read, construction);
        ValidateRopeOwners(read, construction);
        for(var i=0;i<read.Captures.Count;i++)
            if(!construction.OwnsCapture(new(read.Captures[i].Sensor,read.Captures[i].Target)))
                throw new ArgumentException("Capture observation names a foreign policy or target.");
        // Render body reads omit static bodies; ownership belongs to the authored construction.
        foreach (var actuator in read.Presentation.Actuators)
            if (!HasActuatorOwner(actuator, construction.Physics)) throw new ArgumentException("Actuator observation names a foreign joint or owner.");
        ValidateLoadOwners(read, construction);
        ValidateLogicOwners(read, construction);
        ValidateTransmissionOwners(read, construction);
        ValidateRetentionOwners(read, construction);

    }

    private static void ValidateAirflowOwners(WorkshopRead read, ICompiledScene construction)
    {
        var declared = construction.Physics.AirflowSources;
        var expected = read.Epoch.Value == 0 ? 0 : declared.Length;
        if (read.Airflow.Count != expected) throw new ArgumentException("Airflow population differs from installation.");
        for (var i = 0; i < expected; i++)
        {
            var value = read.Airflow[i];
            var source = declared[i];
            if (value.Id != source.Id || value.Owner != source.Owner || value.Input != source.Input.Kind ||
                value.FlowSpeed != 0 && value.FlowSpeed != source.FlowSpeed ||
                value.TransferPower > source.MaximumPower)
                throw new ArgumentException("Airflow identity or rating differs from installation.");
        }
    }

    private static void ValidateEnergyStoreOwners(WorkshopRead read, ICompiledScene construction)
    {
        var declared = construction.Physics.EnergyStores;
        var expected = read.Epoch.Value == 0 ? 0 : declared.Length;
        if (read.EnergyStores.Count != expected) throw new ArgumentException("Energy store population differs from installation.");
        for (var i = 0; i < expected; i++)
        {
            var value = read.EnergyStores[i];
            if (value.Id != declared[i].Id || value.Owner != declared[i].Owner || value.Balance.CompareTo(declared[i].Capacity.Value) > 0)
                throw new ArgumentException("Energy store identity or capacity differs from installation.");
            if (read.PhysicalRevision.IsAdmission && (value.Balance != new FiniteEnergyBalance(declared[i].InitialEnergy.Value, 0f) || value.Supplied.Value != 0f ||
                value.Released.Value != 0f || value.Dissipated.Value != 0f || value.Transactions != 0 ||
                value.CommitSupplied.Value != 0f || value.CommitReleased.Value != 0f || value.CommitDissipated.Value != 0f))
                throw new ArgumentException("Admission changed authored energy store state.");
        }
    }

    private static void ValidateRopeOwners(WorkshopRead read, ICompiledScene construction)
    {
        var expectedRopes = read.Epoch.Value == 0 ? 0 : construction.Physics.TensileSpans.Length;
        if (read.Presentation.Ropes.Length != expectedRopes) throw new ArgumentException("Rope observation population differs from its installation.");
        var cutOwners = new System.Collections.Generic.Dictionary<RopeSpanId, RopeRouteId>();
        foreach (var span in construction.Physics.TensileSpans) cutOwners.Add(span.Id, span.Route);
        var bodies = construction.Physics.Bodies.ToArray().ToDictionary(body => body.Id);
        for (var i = 0; i < read.Presentation.Ropes.Length; i++)
        {
            var rope = read.Presentation.Ropes[i];
            var declared = construction.Physics.TensileSpans[i];
            if (declared.Id != rope.Id || declared.Route != rope.Route || declared.Order != rope.Order ||
                declared.BodyA != rope.BodyA || declared.BodyB != rope.BodyB || declared.MaximumLength != rope.MaximumLength ||
                (rope.Integrity == RopeIntegrity.Intact && (rope.State == RopeState.Open) == declared.Complete) ||
                (rope.CutSpan.Value != 0 && (!cutOwners.TryGetValue(rope.CutSpan, out var cutRoute) || cutRoute != rope.Route)) ||
                (read.Tick.Value == 0 && rope.Integrity != RopeIntegrity.Intact))
                throw new ArgumentException("Rope observation names foreign ownership.");
            // One centimetre is below the strand's 2.5cm radius: enough for render/pose f32 rounding,
            // but detached knots cannot be hidden by translating both endpoints together.
            const float attachmentAllowance = .01f;
            if (Vector3.Distance(rope.AttachmentA, Attachment(declared.BodyA, declared.LocalA)) > attachmentAllowance ||
                Vector3.Distance(rope.AttachmentB, Attachment(declared.BodyB, declared.LocalB)) > attachmentAllowance)
                throw new ArgumentException("Rope attachment differs from its committed body socket.");
        }
        Vector3 Attachment(GpuBodyId owner, Vector3 local)
        {
            var body = bodies[owner];
            var cell = body.Cell; var position = body.Local; var rotation = body.Rotation;
            if (body.Motion == RigidMotionKind.Dynamic)
            {
                if (!read.Bodies.TryGet(owner, out var committed))
                    throw new ArgumentException("Rope attachment has no committed dynamic owner.");
                cell = committed.Body.Cell; position = committed.Body.Local; rotation = committed.Rotation;
            }
            return new Vector3((cell.X + position.X) / 16f, (cell.Y + position.Y) / 16f, (cell.Z + position.Z) / 16f) +
                Vector3.Transform(local, new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W));
        }
    }

    private static void ValidateLogicOwners(WorkshopRead read, ICompiledScene construction)
    {
        var expectedLogic=0;
        foreach(var node in construction.Physics.SupplyGraph.Nodes)
        {
            if(node.Kind != SupplyNodeKind.LogicGate) continue;
            expectedLogic++;
            var found=false;
            foreach(var logic in read.Presentation.Logic)
                if(logic.Node==node.Id && logic.Owner==node.Owner)
                {
                    found=true;
                    var expected=logic.Supplied && CompiledSupplyGraph.Evaluate(node.Operation!.Value,
                        logic.At(CuriousContraptions.Abstractions.Electrical.SupplyTerminal.FirstIn),logic.At(CuriousContraptions.Abstractions.Electrical.SupplyTerminal.SecondIn));
                    if(logic.Active!=expected || (read.Tick.Value==0 &&
                        (logic.Inputs!=ElectricalInputState.Neither || logic.Supplied || logic.Active)))
                        throw new ArgumentException("Electrical logic observation contradicts its declared operation or admission state.");
                }
            if(read.Epoch.Value!=0 && !found)
                throw new ArgumentException("Electrical logic observation omits a declared owner.");
        }
        if(read.Presentation.Logic.Length!=(read.Epoch.Value==0 ? 0 : expectedLogic))
            throw new ArgumentException("Electrical logic observation population differs from its installed owners.");
    }

    private static void ValidateLoadOwners(WorkshopRead read, ICompiledScene construction)
    {
        var sensors = construction.Physics.ContactLoadSensors;
        var loads = read.Presentation.Loads;
        if (loads.Length != (read.Epoch.Value == 0 ? 0 : sensors.Length))
            throw new ArgumentException("Contact-load observation population differs from its installation.");
        for (var i = 0; i < loads.Length; i++)
            if (loads[i].Sensor != sensors[i].Id || loads[i].Frame != sensors[i].Frame || loads[i].Region != sensors[i].Region)
                throw new ArgumentException("Contact-load observation names a foreign sensor, frame or region.");
    }

    private static bool HasActuatorOwner(ActuatorDriveRead read, IPhysicsSceneDeclaration physics) => read.Drive.Kind switch
    {
        SupplyConsumerKind.PrismaticDrive => physics.Prismatics.ToArray().Any(joint => joint.Id == read.Drive.PrismaticJoint && joint.BodyA == read.Owner),
        SupplyConsumerKind.RevoluteDrive => physics.Revolutes.ToArray().Any(joint => joint.Id == read.Drive.RevoluteJoint && joint.BodyA == read.Owner),
        _ => false
    };

    private static void ValidateRetentionOwners(WorkshopRead read, ICompiledScene construction)
    {
        var owned = construction.Physics.Transmissions.ToArray().Where(t => t.Retention.HasValue)
            .OrderBy(t => t.Retention!.Value.Value).ToArray();
        var observed = read.Presentation.Retentions;
        if (observed.Length != (read.Epoch.Value == 0 ? 0 : owned.Length))
            throw new ArgumentException("Retention observation population differs from its installation.");
        for (var i = 0; i < observed.Length; i++)
        {
            var row = observed[i];
            if (row.Id != owned[i].Retention || row.Owner != owned[i].Owner || row.Coordinate != owned[i].Driven.Prismatic)
                throw new ArgumentException("Retention observation names a foreign coordinate or owner.");
            if (read.PhysicalRevision.IsAdmission && (row.Mode != CuriousContraptions.Abstractions.Mechanical.RetentionMode.Retained ||
                row.Compression != -owned[i].InitialRetentionCoordinate || row.AcceptedElasticEnergy != 0f ||
                row.ReleasedElasticEnergy != 0f || row.WindingWork != 0f || row.PositiveWindingTransfer != 0f ||
                row.ContactWork != 0f || row.GravityWork != 0f || row.RequestResult.HasValue))
                throw new ArgumentException("Retention admission observations disagree with authored state.");
        }
    }

    private static void ValidateTransmissionOwners(WorkshopRead read, ICompiledScene construction)
    {
        var owned = construction.Physics.Transmissions.ToArray().Where(t => t.Owner != default).ToArray();
        var observed = read.Presentation.Transmissions;
        if (observed.Length != (read.Epoch.Value == 0 ? 0 : owned.Length))
            throw new ArgumentException("Mechanical transmission observation population differs from its installation.");
        for (var i = 0; i < observed.Length; i++)
        {
            if (observed[i].Transmission != owned[i].Id || observed[i].Owner != owned[i].Owner)
                throw new ArgumentException("Mechanical transmission observation names a foreign owner.");
            if (read.PhysicalRevision.IsAdmission)
            {
                var untimed = owned[i].CloseSeconds == 0f;
                if (observed[i].Phase != (untimed ? CuriousContraptions.Abstractions.Mechanical.TransmissionEngagementPhase.Engaged : CuriousContraptions.Abstractions.Mechanical.TransmissionEngagementPhase.Open) ||
                    (untimed ? observed[i].Closure != 1f : !PhysicsDeclarationBounds.Zero(observed[i].Closure)))
                    throw new ArgumentException("Mechanical transmission admission state disagrees with its declaration.");
            }
        }
    }

    internal static void ValidateScheduleIdentity(WorkshopResponse response)
    {
        if (response.Read.Epoch.Value == 0)
        {
            if (response.Cadence.Value != 0 || response.Projection.Value != 0 || response.MasterGeneration.Value != 0 || response.SourcePulse.Value != 0)
                throw new ArgumentException("Uninstalled response contains a schedule identity.");
        }
        else if (response.Cadence.Value == 0 || response.Projection.Value == 0 || response.MasterGeneration.Value == 0 ||
            response.Read.Capture?.Generation != response.MasterGeneration || response.SourcePulse.Value != response.Read.Tick.Value)
            throw new ArgumentException("Physical response does not bind its installed master schedule.");
    }

    private static bool HasPhysicalState(WorkshopRead read) =>
        read.Epoch.Value != 0 || read.Tick.Value != 0 || read.Revision.Value != 0 || !read.PhysicalRevision.IsAdmission || read.Bodies.Count != 0 || read.Captures.Count != 0 || read.Activations.Count != 0 || read.Timers.Count != 0 || read.ContactWorks.Count != 0 || read.EnergyStores.Count != 0 || read.ChamberRegions.Count != 0 || read.FiniteWorkActuators.Count != 0 || read.Airflow.Count != 0 || read.GasStores.Count != 0 || read.IntegrityObservers.Count != 0 || read.CoordinateTravelObservers.Count != 0 || read.Electrical.Count != 0 || read.Memory.Count != 0;

    private static bool HasPresentationState(WorkshopRead read) =>
        read.Presentation.Actuators.Length != 0 || read.Presentation.Loads.Length != 0 || read.Presentation.Logic.Length != 0 || read.Presentation.Ropes.Length != 0 || read.Presentation.Transmissions.Length != 0 || read.Presentation.Retentions.Length != 0;

    internal static void ValidateCapture(WorkshopRead read)
    {
        if (read.Capture is { } capture)
        {
            capture.Validate();
            if (capture.Domain != WorkshopClockDomain.SimulationMonotonic || read.Epoch.Value == 0)
                throw new ArgumentException("Physical read requires its simulation native clock.");
        }
        else if (HasPhysicalState(read) || HasPresentationState(read))
            throw new ArgumentException("An installed physical read requires its original capture.");
    }
}
