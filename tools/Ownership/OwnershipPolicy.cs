namespace Ownership;

// Source-derived independent expectations for the mixed-state boundaries listed in
// P0-004/assemblies.md. These are semantic guard examples, not a substitute for review
// of every assignment. Identities are extensible compiler declaration IDs.
public static class OwnershipPolicy
{
    public static readonly IReadOnlyDictionary<MemberId, AssemblyOwner> Expected =
        new Dictionary<MemberId, AssemblyOwner>
        {
            [new("F:CuriousContraptions.SceneBodyGeometry._children")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.SceneBodyGeometry._opaque")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.SceneBodyGeometry._withoutEnvelope")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.SceneColliderSet._children")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._byBody")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._declarations")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._initialJoints")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._jointIds")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._objects")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._presentationPoses")] = AssemblyOwner.SimulationHost,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._queryGeometry")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._queryOwners")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._referenceBindings")] = AssemblyOwner.SimulationHost,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._surfaces")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.WorldGeometry.SweepSnapshot._surfaces")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.BodyDynamics.AngularVelocity")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.BodyDynamics.Inertia")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.BodyDynamics.LinearVelocity")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.BodyDynamics.Mass")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.BodyDynamics.Motion")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.BodySpatialState.Enabled")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.BodySpatialState.Participation")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.BodySpatialState.Pose")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.MachineWorld.PhysicsAssembly")] = AssemblyOwner.SimulationHost,
            [new("P:CuriousContraptions.SceneBodyGeometry.All")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.SceneBodyGeometry.Children")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.SceneBodyGeometry.Count")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.SceneBodyGeometry.Geometry")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.SceneBodyGeometry.Item(CuriousContraptions.Physics.ColliderChildId)")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.SceneBodyGeometry.Slot")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.SceneCollider.Opaque")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.SceneCollider.Shape")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.SceneCollider.Source")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.SceneCollider.Surface")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.SceneColliderSet.Geometry")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.ScenePhysicsAssembly.Bodies")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.ScenePhysicsAssembly.Declarations")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.ScenePhysicsAssembly.InitialJoints")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.ScenePhysicsAssembly.Objects")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.ScenePhysicsAssembly.Surfaces")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldBodyDeclaration.Dynamics")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldBodyDeclaration.Geometry")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldBodyDeclaration.InitialParticipation")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldBodyDeclaration.Material")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldBodyDeclaration.PrescribedMotion")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldBodyGeometry.Geometry")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldBodyGeometry.Owner")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldBodyGeometry.Pose")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldBodyGeometry.QueryGeometry")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldBodyGeometry.Slot")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldGeometry.PreparedColliderGroup.Kind")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldGeometry.PreparedColliderGroup.Part")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldGeometry.PreparedColliderGroup.Pose")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldGeometry.PreparedColliderGroup.Set")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldGeometry.PreparedColliderGroup.Slot")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldOverlapResult.Kind")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldOverlapResult.MovingChild")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldOverlapResult.ObstacleChild")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldOverlapResult.Part")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldOverlapResult.Separation")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldOverlapResult.Surface")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldSweepResult.Distance")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldSweepResult.Kind")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldSweepResult.Normal")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldSweepResult.Part")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldSweepResult.Penetration")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldSweepResult.Status")] = AssemblyOwner.SimulationCore,
            [new("P:CuriousContraptions.WorldSweepResult.Surface")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.MachineWorld._acoustics")] = AssemblyOwner.SimulationHost,
            [new("F:CuriousContraptions.MachineWorld._animations")] = AssemblyOwner.SimulationHost,
            [new("F:CuriousContraptions.MachineWorld._booleanObservations")] = AssemblyOwner.SimulationHost,
            [new("F:CuriousContraptions.MachineWorld._enumObservations")] = AssemblyOwner.SimulationHost,
            [new("F:CuriousContraptions.MachineWorld._physicsAssembly")] = AssemblyOwner.SimulationHost,
            [new("F:CuriousContraptions.MachineWorld._scalarObservations")] = AssemblyOwner.SimulationHost,
            [new("P:CuriousContraptions.Bridge.BodyPublicationRead.Activity")] = AssemblyOwner.SimulationHost,
            [new("P:CuriousContraptions.Bridge.BodyPublicationRead.Pose")] = AssemblyOwner.SimulationHost,
            [new("P:CuriousContraptions.Bridge.BodyPublicationRead.Query")] = AssemblyOwner.SimulationHost,
            [new("P:CuriousContraptions.Bridge.BodyPublicationRead.Velocity")] = AssemblyOwner.SimulationHost,
            [new("P:CuriousContraptions.Bridge.CommittedEventId.Generation")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.Bridge.CommittedEventId.Sequence")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.Bridge.CommittedEventId.Stream")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.Bridge.EnumObservationSlot`1.Index")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.Bridge.EnumRead`1.Key")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.Bridge.EnumRead`1.Value")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.Bridge.EnumReadKey`1.Owner")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.Bridge.EnumReadKey`1.Slot")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.Bridge.PoseReadStamp.Generation")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.Bridge.PoseReadStamp.Revision")] = AssemblyOwner.Protocol,
            [new("P:CuriousContraptions.Bridge.PoseReadStamp.SimulationTime")] = AssemblyOwner.Protocol,
            [new("F:CuriousContraptions.SimulationTimers._states")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.SimulationTimers._indices")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.SimulationTransaction._participants")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.Physics.BodyBoundsTree._root")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.Presentation.AnimationBatch._slots")] = AssemblyOwner.AnimationKernel,
            [new("F:CuriousContraptions.Presentation.AnimationBatch._free")] = AssemblyOwner.AnimationKernel,
            [new("F:CuriousContraptions.Presentation.SceneAnimationAdapter.Target.Object")] = AssemblyOwner.GodotPresenter,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._bodies")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.ScenePhysicsAssembly._poseBatch")] = AssemblyOwner.GodotPresenter,
            [new("F:CuriousContraptions.MachinePart._poweredInputs")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.MachinePart.BaseStateCheckpoint._visible")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.TrampolinePart._meshDirty")] = AssemblyOwner.GodotPresenter,
            [new("F:CuriousContraptions.TrampolinePart._patches")] = AssemblyOwner.GodotPresenter,
            [new("F:CuriousContraptions.WoundSpringPart._internalBodies")] = AssemblyOwner.GodotPresenter,
            [new("F:CuriousContraptions.SceneImpactEffect._last")] = AssemblyOwner.SimulationCore,
            [new("F:CuriousContraptions.PressurePlatePart._glow")] = AssemblyOwner.AnimationKernel,
            [new("F:CuriousContraptions.PulleyPart._wheelAngle")] = AssemblyOwner.AnimationKernel
        };

    private static readonly MemberId TimerStates = new("F:CuriousContraptions.SimulationTimers._states");
    private static readonly IReadOnlySet<CallerId> TimerStateCallers = new HashSet<CallerId>
    {
        new("M:CuriousContraptions.SimulationTimers.#ctor(System.Collections.Generic.IEnumerable{CuriousContraptions.SimulationTimerDeclaration})"),
        new("M:CuriousContraptions.SimulationTimers.get_PublicationReads~System.ReadOnlySpan{CuriousContraptions.SimulationTimerState}"),
        new("M:CuriousContraptions.SimulationTimers.Read(CuriousContraptions.SimulationTimerId)~CuriousContraptions.SimulationTimerState"),
        new("M:CuriousContraptions.SimulationTimers.Trigger(CuriousContraptions.SimulationTimerId,System.Int32)~System.Boolean"),
        new("M:CuriousContraptions.SimulationTimers.Advance(System.Int32,CuriousContraptions.TimerBoundary)~System.ReadOnlySpan{CuriousContraptions.SimulationTimerElapsed}"),
        new("M:CuriousContraptions.SimulationTimers.CaptureCheckpoint"),
        new("M:CuriousContraptions.SimulationTimers.RestoreCheckpoint"),
        new("M:CuriousContraptions.SimulationTimers.Capture~CuriousContraptions.SimulationTimerSnapshot"),
        new("M:CuriousContraptions.SimulationTimers.Restore(CuriousContraptions.SimulationTimerSnapshot)")
    };

    public static void Validate(StateMember member, OwnershipAssignment assignment)
    {
        if (Expected.TryGetValue(member.Id, out var owner) && assignment.Owner != owner)
            throw new InvalidDataException("Source-derived ownership policy rejects this otherwise valid placement.");
        if (member.Id != TimerStates) return;
        if (member.Mutability != StorageMutability.ReferencedStorage)
            throw new InvalidDataException("Readonly timer array still has mutable contents.");
        if (member.Uses.Any(site => !TimerStateCallers.Contains(site.Caller)))
            throw new InvalidDataException("Timer storage has an unreviewed mutator or reference recipient.");
    }
}
