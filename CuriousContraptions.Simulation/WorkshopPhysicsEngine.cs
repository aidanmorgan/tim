using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.Intrinsics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed class WorkshopPhysicsEngine
{
    private readonly MechanicalTransmissionDeclaration[] _declaredTransmissions;
    private readonly DrivenSurfaceDeclaration[] _declaredSurfaces;
    private readonly List<SimDrivenSurface> _surfaces = [];

    public WorkshopPhysicsEngine(ReadOnlySpan<MechanicalTransmissionDeclaration> transmissions = default, ReadOnlySpan<DrivenSurfaceDeclaration> surfaces = default)
    {
        _declaredTransmissions = transmissions.ToArray();
        _declaredSurfaces = surfaces.ToArray();
    }

    private Dictionary<ContactFeatureIdentity, float> _contactCache = new(64);
    private HashSet<ContactFeatureSetKey> _normalContactSets = new(16);
    private Dictionary<ContactFeatureSetKey, FrictionWarmStart> _frictionCache = new(16);
    private readonly WorkshopDynamicBvh _bvh = new();
    // Story 9R.4: one substep's static-side contact records and the bodies a contact-load row already counted (caller-owned
    // scratch, so ContactLoadSensorTracker allocates nothing per substep).
    private List<ContactPointRecord> _loadContacts = new(64);
    private GpuBodyId[] _loadCounted = new GpuBodyId[64];

    private List<SimBody> _staticBodies = new(16);
    private List<SimBody> _dynamicBodies = new(16);
    // The restitution pass's touched bodies and its per-substep stamp (WorkshopConstraintSolver.RestitutionPass).
    private List<SimBody> _restitutionBodies = new(16);
    private int _restitutionStamp;
    private Dictionary<int, SimBody> _allBodies = new(16);
    private List<SimCollider> _colliders = new(32);
    private List<SimJoint> _joints = new(4);
    private List<SimRevoluteJoint> _revoluteJoints = new(4);
    private readonly List<SimMechanicalTransmission> _transmissions = [];
    private bool _transmissionsResolved;
    private readonly List<SimTensileRoute> _tensile = new(4);
    private readonly List<SimTensileRoute> _tensilePool = new(4);
    private List<(SimCollider ColA, SimCollider ColB)> _candidatePairs = new(64);
    private List<ContactPoint> _contacts = new(64);
    // Scratch for the speculative sphere-contact touch probe (WorkshopCollisionKernels.AdvanceSpeculativeSphereContact).
    private SimCollider _probeCollider = new();
    private readonly List<ContactPoint> _probeContacts = new(4);
    private List<ContactManifold> _manifolds = new(16);
    private List<int> _storeIndices = new(4);
    private List<float> _balances = new(4);
    private List<float> _capacities = new(4);
    private List<float> _credits = new(4);
    // Story 9R.5b: per-tick working rows sized from the world's own populations, grown only when a world needs more (per Run).
    private ContactLoadSensorDeclaration[] _loadDeclarations = new ContactLoadSensorDeclaration[4];
    private int[] _loadFrames = new int[4];
    private float[] _loadMass = new float[4];
    private float[] _elecBefore = new float[4], _elecLimits = new float[4], _elecAfter = new float[4];
    // Whether each drive slot (every prismatic, then every revolute) declares a drive, read once per tick to validate the inputs.
    private bool[] _drivenSlots = new bool[4];

    /// <summary>A cleared span of exactly <paramref name="count"/> rows, growing the owned array only when it is too short.</summary>
    private static Span<T> Scratch<T>(ref T[] array, int count)
    {
        if (count > Array.MaxLength) throw new WorkshopStorageException("Scratch storage is not representable.");
        if (count > 3L * array.Length / 4)
        {
            var capacity = (int)Math.Min(Array.MaxLength, Math.Max(2L * array.Length, (4L * count + 2) / 3));
            if (capacity != array.Length) array = new T[capacity];
        }
        var span = array.AsSpan(0, count); span.Clear();
        return span;
    }

    private List<SimBody> _bodyPool = new(16);
    private List<SimCollider> _colliderPool = new(32);
    private List<SimJoint> _jointPool = new(4);
    private List<SimRevoluteJoint> _revoluteJointPool = new(4);
    private List<ContactManifold> _manifoldPool = new(16);

    /// <summary>Retire an epoch's scratch and warm state. Substep Clear calls retain ordinary within-world reuse.</summary>
    public void Reset()
    {
        _contactCache = new(64); _normalContactSets = new(16); _frictionCache = new(16); _allBodies = new(16);
        _loadContacts = new(64); _staticBodies = new(16); _dynamicBodies = new(16); _restitutionBodies = new(16);
        _colliders = new(32); _joints = new(4); _revoluteJoints = new(4); _candidatePairs = new(64); _contacts = new(64); _manifolds = new(16);
        _storeIndices = new(4); _balances = new(4); _capacities = new(4); _credits = new(4);
        _bodyPool = new(16); _colliderPool = new(32); _jointPool = new(4); _revoluteJointPool = new(4); _manifoldPool = new(16);
        _loadCounted = new GpuBodyId[64]; _loadDeclarations = new ContactLoadSensorDeclaration[4]; _loadFrames = new int[4]; _loadMass = new float[4];
        _elecBefore = new float[4]; _elecLimits = new float[4]; _elecAfter = new float[4]; _drivenSlots = new bool[4];
        _restitutionStamp = 0;
        _probeContacts.Clear(); _probeCollider = new();
        _transmissions.Clear(); _surfaces.Clear(); _transmissionsResolved = false;
        _tensile.Clear(); _tensilePool.Clear();
        _bvh.Reset();
    }

    /// <summary>One f32 lane of a world record at its named offset (Story 9A.44: every game value in the world is f32).</summary>
    private static float ReadF32(ReadOnlySpan<byte> span, int offset) => BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset, 4));
    private static void WriteF32(Span<byte> span, int offset, float value) => BinaryPrimitives.WriteSingleLittleEndian(span.Slice(offset, 4), value);
    private static Vector3 ReadVector(ReadOnlySpan<byte> span, int offset) =>
        new(ReadF32(span, offset), ReadF32(span, offset + sizeof(float)), ReadF32(span, offset + 2 * sizeof(float)));
    private static void WriteVector(Span<byte> span, int offset, float x, float y, float z)
    {
        WriteF32(span, offset, x); WriteF32(span, offset + sizeof(float), y); WriteF32(span, offset + 2 * sizeof(float), z);
    }
    private static uint ReadU32(ReadOnlySpan<byte> span, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset, 4));
    private static void WriteU32(Span<byte> span, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), value);
    /// <summary>A prismatic constraint frame: its f32 position, then its f32 rotation.</summary>
    private static (Vector3 Position, Quaternion Rotation) ReadConstraintFrame(ReadOnlySpan<byte> span, int offset)
    {
        var rotation = offset + PhysicsGpuAbi.ConstraintFrameRotationOffset;
        return (ReadVector(span, offset), new(ReadF32(span, rotation), ReadF32(span, rotation + PhysicsGpuAbi.RotationYOffset),
            ReadF32(span, rotation + PhysicsGpuAbi.RotationZOffset), ReadF32(span, rotation + PhysicsGpuAbi.RotationWOffset)));
    }

    private static void EmitContactEvents(Span<byte> span, PhysicsWorldLayout layout, SimCollider colStatic, SimBody bDyn, float approach, Vector3 normalTowardDyn, ContactPoint contact, int substep, uint sourceOrdinal, ImmutableDictionary<WorkPairKey,ContactWorkOccurrence>.Builder workStates)
    {
        var bStatic = colStatic.Body;
        for (var t = 0; t < layout.Triggers; t++)
        {
            var tOffset = layout.TriggerOffset(t);
            var owner = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerOwnerOffset, 4));
            if (owner != bStatic.BodySlot) continue;
            var kind = (BodyTargetKind)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerTargetKindOffset, 4));
            var named = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerNamedTargetOffset, 4));
            if (!PhysicsGpuAbi.MatchesTarget(span,kind,named,(uint)bDyn.BodySlot)) continue;
            var threshold = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerThresholdOffset, 4));
            if (approach >= threshold)
            {
                var curCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerCountOffset, 4));
                if (curCount == 0)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerCountOffset, 4), 1);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerColliderOffset, 4), (uint)colStatic.Index);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerOrdinalOffset, 4), sourceOrdinal + (uint)substep + 1);
                    WriteF32(span, tOffset + PhysicsGpuAbi.TriggerPhaseOffset, 0f);
                    BinaryPrimitives.WriteSingleLittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerApproachOffset, 4), approach);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerBodyOffset, 4), (uint)bDyn.BodySlot);
                }
            }
        }

        for (var w = 0; w < layout.ContactWorks; w++)
        {
            var wOffset = layout.ContactWorkOffset(w);
            var owner = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkOwnerOffset, 4));
            if (owner != bStatic.BodySlot) continue;
            var kind = (BodyTargetKind)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkTargetKindOffset, 4));
            var named = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkNamedTargetOffset, 4));
            if (!PhysicsGpuAbi.MatchesTarget(span,kind,named,(uint)bDyn.BodySlot)) continue;
            var threshold = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkAdmissionSpeedOffset, 4));
            if (approach < threshold) continue;

            var key=new WorkPairKey(new(BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(wOffset+PhysicsGpuAbi.RecordIdentityOffset,8))),
                new((ulong)bDyn.BodyIdHigh<<32 | bDyn.BodyIdLow));
            workStates.TryGetValue(key,out var previous);
            var prevSeq=previous.Sequence;
            var prevOrd=previous.EventOrdinal;
                var cooldown = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkCooldownOffset, 4));
                var curOrd = sourceOrdinal + (uint)substep + 1;
                var curCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkOccurrenceCountOffset, 4));
                var previousInCommit = prevOrd > sourceOrdinal;

                if (!previousInCommit && (prevSeq == 0 || curOrd >= prevOrd + cooldown))
                {
                    var newCount = curCount + 1;
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkOccurrenceCountOffset, 4), newCount);
                    var invEffectiveMass = 1f / WorkshopConstraintSolver.EffectiveMass(contact, contact.Normal);
                    var relVel = WorkshopConstraintSolver.RelativeVelocity(contact);
                    var speed = Vector3.Dot(relVel, contact.Normal);

                    var targetImp = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkTargetSpeedOffset, 4));
                    var avail = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkAvailableOffset, 4));
                    var capacity = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkPreloadOffset, 4));
                    var normVec = Vector128.Create(normalTowardDyn.X, normalTowardDyn.Y, normalTowardDyn.Z, 0f);
                    var response = ContactWorkImpulse.FromStore(speed, targetImp, invEffectiveMass, avail, capacity, normVec);
                    // A validated world never holds one; it is a broken ledger, reported rather than paid as empty (blind hunter 14).
                    if (response.Store == ContactWorkStore.Impossible)
                        throw new InvalidOperationException(
                            $"Contact work {w} holds an impossible store: {avail:R} J available of a {capacity:R} J preload.");

                    BinaryPrimitives.WriteSingleLittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkAvailableOffset, 4), response.Remaining);
                    workStates[key]=new(new((uint)w),new((uint)colStatic.Index),key.Target,newCount,curOrd,0,
                        new(approach),new(response.Debit),response.Debit>0 ? ContactWorkEffect.Paid : ContactWorkEffect.Passive);

                    var isA = contact.A == bDyn;
                    var r = isA ? new Vector3(contact.RAx, contact.RAy, contact.RAz) : new Vector3(contact.RBx, contact.RBy, contact.RBz);
                    var imp = new Vector3(response.Impulse.GetElement(0), response.Impulse.GetElement(1), response.Impulse.GetElement(2));
                    WorkshopCollisionKernels.ApplyImpulse(bDyn, imp, r);
                    WorkshopConstraintSolver.SettleVelocity(bDyn);
                }
        }
    }

    /// <summary>Evaluates every contact-load row against this substep's contacts through ContactLoadSensorTracker, each contact seen
    /// from its static side, and keeps each row's maximum qualifying mass (Story 9R.4 D2).</summary>
    private void MeasureContactLoads(ReadOnlySpan<ContactLoadSensorDeclaration> rows, ReadOnlySpan<int> frames, Span<float> maximum)
    {
        _loadContacts.Clear();
        foreach (var c in _contacts)
        {
            // ContactPoint.Normal points from B toward A.
            if (c.A.Motion == RigidMotionKind.Static && c.B.Motion == RigidMotionKind.Dynamic)
                _loadContacts.Add(new(new(c.A.Id), new(c.ColA.Id), new(c.B.Id), c.B.Mass, c.Point, -c.Normal, c.Gap, c.MaxImpulse > 0f));
            else if (c.B.Motion == RigidMotionKind.Static && c.A.Motion == RigidMotionKind.Dynamic)
                _loadContacts.Add(new(new(c.B.Id), new(c.ColB.Id), new(c.A.Id), c.A.Mass, c.Point, c.Normal, c.Gap, c.MaxImpulse > 0f));
        }
        _ = Scratch(ref _loadCounted, _loadContacts.Count);
        var records = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_loadContacts);
        for (var row = 0; row < rows.Length; row++)
        {
            var frame = _allBodies[frames[row]];
            maximum[row] = MathF.Max(maximum[row], ContactLoadSensorTracker.Measure(rows[row], frame.Position, frame.Rotation, records, _loadCounted));
        }

    }

    /// <summary>One step from the committed world. Besides the world bytes it reads only the explicit tick inputs: per-source enables,
    /// per-drive-slot switched-supply state (compiled routes × committed switch contacts) and per-revolute-drive target speeds. No
    /// authoring or host state is read.</summary>
    public PhysicsInteractionSnapshot Advance(ReadOnlyMemory<byte> source, Memory<byte> candidate, PhysicsTickInputs inputs, PhysicsInteractionSnapshot committedInteractions)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(committedInteractions);
        var captureStates=committedInteractions.Captures.ToBuilder();
        var workStates=committedInteractions.Works.ToBuilder();
        _ = PhysicsGpuAbi.ReadFailure(source.Span);
        if (source.Length != PhysicsGpuAbi.ByteLength(source.Span) || candidate.Length != source.Length)
            throw new ArgumentException("The candidate world must have its committed world's exact length.");
        source.CopyTo(candidate);
        var span = candidate.Span;
        // Story 9R.5b: every table offset is this world's own (its header's populations); no table has a fixed capacity.
        var layout = PhysicsGpuAbi.Layout(span);
        ApplyCuts(span, layout, inputs);
        // Motion is a closed ABI discriminant. Refuse the entire candidate before clocks,
        // bodies or solver state advance; every downstream motion value is typed.
        for (var slot = 0; slot < layout.Bodies; slot++)
        {
            var motion = (RigidMotionKind)ReadU32(span, layout.BodyOffset(slot) + PhysicsGpuAbi.BodyMotionOffset);
            if (Enum.IsDefined(motion)) continue;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderStatusOffset, 4), (uint)PhysicsCandidateStatus.Invalid);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderFailureOffset, 4), (uint)PhysicsFailure.InvalidDeclaration);
            return committedInteractions;
        }

        // Story 10.1: the collider shape is a closed set. A world carrying an undefined shape is an invalid declaration, refused
        // before anything is stepped (the candidate is the source with only its status and failure set); the tick path below is
        // total over the admitted shapes.
        for (var c = 0; c < layout.Colliders; c++)
        {
            var shape = (ShapeKind)ReadU32(span, layout.ColliderOffset(c) + PhysicsGpuAbi.ColliderShapeOffset);
            if (Enum.IsDefined(shape)) continue;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderStatusOffset, 4), (uint)PhysicsCandidateStatus.Invalid);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderFailureOffset, 4), (uint)PhysicsFailure.InvalidDeclaration);
            return committedInteractions;
        }

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderStatusOffset, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderFailureOffset, 4), 0);

        var currentTick = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(PhysicsGpuAbi.HeaderTickOffset, 8));
        BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(PhysicsGpuAbi.HeaderTickOffset, 8), currentTick + 1);

        var sourceOrdinal = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderSourceOrdinalOffset, 4));
        for (var work = 0; work < layout.ContactWorks; work++)
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(layout.ContactWorkOffset(work) + PhysicsGpuAbi.ContactWorkSuppliedOffset, 4), 0f);

        var supplyCount = layout.ElectricalSources;
        Span<bool> driven = Scratch(ref _drivenSlots, layout.Prismatics + layout.Revolutes);
        for (var p = 0; p < layout.Prismatics; p++)
            driven[p] = (PrismaticDriveKind)ReadU32(span, layout.PrismaticOffset(p) + PhysicsGpuAbi.PrismaticDriveKindOffset) == PrismaticDriveKind.Supplied;
        var revoluteDrives = 0;
        for (var r = 0; r < layout.Revolutes; r++)
        {
            driven[layout.Prismatics + r] = (RevoluteDriveKind)ReadU32(span, layout.RevoluteOffset(r) + PhysicsGpuAbi.RevoluteDriveKindOffset) == RevoluteDriveKind.Supplied;
            if (driven[layout.Prismatics + r]) revoluteDrives++;
        }
        inputs.Validate(supplyCount, driven, revoluteDrives);
        // engine-revolute-drive-target: each driven revolute commits this tick's target before its substeps. The world's revolutes and
        // the compiled drive table are both in joint identity order, so a driven revolute's rank among the driven ones is its drive index.
        for (int r = 0, drive = 0; r < layout.Revolutes; r++)
            if (driven[layout.Prismatics + r])
                WriteF32(span, layout.RevoluteOffset(r) + PhysicsGpuAbi.RevoluteDriveTargetSpeedOffset, inputs.DriveTarget[drive++]);
        for (var supply = 0; supply < supplyCount; supply++)
            WriteF32(span, layout.ElectricalSourceOffset(supply) + PhysicsGpuAbi.ElectricalSourceDebitOffset, 0f);

        var cadence = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderCadenceOffset, 4));
        var steps = cadence == 1 ? 8 : (cadence == 2 ? 4 : 2);
        var nextOrdinal = sourceOrdinal + (uint)steps;
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderSourceOrdinalOffset, 4), nextOrdinal);

        var bodyCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderBodyCountOffset, 4));
        var colliderCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderColliderCountOffset, 4));
        var dynamicCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderDynamicCountOffset, 4));

        var motionOffset = layout.MotionOffset;
        WriteU32(span, motionOffset + PhysicsMotionRead.CountOffset, (uint)(dynamicCount * (uint)steps));
        WriteU32(span, motionOffset + PhysicsMotionRead.SubstepsOffset, (uint)steps);
        WriteU32(span, motionOffset + PhysicsMotionRead.FirstOrdinalOffset, sourceOrdinal);
        WriteU32(span, motionOffset + PhysicsMotionRead.LastOrdinalOffset, nextOrdinal);

        var triggerCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderTriggerCountOffset, 4));
        for (var t = 0; t < triggerCount; t++)
        {
            var tOffset = layout.TriggerOffset(t);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerCountOffset, 4), 0);
            span.Slice(tOffset + PhysicsGpuAbi.TriggerColliderOffset, PhysicsGpuAbi.TriggerPaddingOffset - PhysicsGpuAbi.TriggerColliderOffset).Clear();
        }

        _staticBodies.Clear();
        _dynamicBodies.Clear();
        var revoluteJointMap = _transmissionsResolved ? null : new Dictionary<RevoluteConstraintId, SimRevoluteJoint>();
        _allBodies.Clear();

        ReadBodies(span, layout, bodyCount);
        ReadColliders(span, layout, colliderCount);
        ReadTensileRoutes(span, layout);
        ReadPrismatics(span, layout, inputs);
        ReadRevolutes(span, layout, inputs, revoluteJointMap);
        var dt = WorkshopConstraintSolver.SubstepSeconds;
        PrepareBroadphase(dt, steps);
        // Story 9R.4: every compiled contact-load row, read once per tick. A row's tick measurement is the maximum qualifying mass
        // of its substeps (D2), written over the copied source state so a removed body reads Empty on the very next tick.
        var loadRows = layout.ContactLoads;
        Span<ContactLoadSensorDeclaration> loadDeclarations = Scratch(ref _loadDeclarations, loadRows);
        Span<int> loadFrames = Scratch(ref _loadFrames, loadRows);
        Span<float> loadMass = Scratch(ref _loadMass, loadRows);
        for (var row = 0; row < loadRows; row++)
        {
            loadDeclarations[row] = PhysicsGpuAbi.ReadContactLoadDeclaration(span, row);
            loadFrames[row] = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(
                layout.ContactLoadOffset(row) + PhysicsGpuAbi.ContactLoadFrameOffset, 4));
            loadMass[row] = 0f;
        }

        AdvanceSubsteps(span, layout, inputs, steps, sourceOrdinal, loadDeclarations, loadFrames, loadMass, workStates);
        for (var row = 0; row < loadRows; row++)
        {
            var record = span.Slice(layout.ContactLoadOffset(row), PhysicsGpuAbi.ContactLoadSensorBytes);
            BinaryPrimitives.WriteUInt32LittleEndian(record[PhysicsGpuAbi.ContactLoadPhaseOffset..], (uint)loadDeclarations[row].PhaseOf(loadMass[row]));
            BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.ContactLoadMassOffset..], loadMass[row]);
        }

        CommitResidence(span, layout, committedInteractions, captureStates, sourceOrdinal, nextOrdinal, dt);
        foreach (var b in _dynamicBodies)
        {
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyCellOffset, 4), b.CellX);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyCellOffset + sizeof(int), 4), b.CellY);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyCellOffset + 2 * sizeof(int), 4), b.CellZ);
            WriteVector(span, b.BodyOffset + PhysicsGpuAbi.BodyLocalOffset, b.LocalX, b.LocalY, b.LocalZ);
            WriteVector(span, b.BodyOffset + PhysicsGpuAbi.BodyRotationOffset, b.Qx, b.Qy, b.Qz);
            WriteF32(span, b.BodyOffset + PhysicsGpuAbi.BodyRotationOffset + PhysicsGpuAbi.VectorBytes, b.Qw);
            WriteVector(span, b.BodyOffset + PhysicsGpuAbi.BodyVelocityOffset, b.Vx, b.Vy, b.Vz);
            WriteVector(span, b.BodyOffset + PhysicsGpuAbi.BodyAngularVelocityOffset, b.Wx, b.Wy, b.Wz);
        }
        return new(captureStates.ToImmutable(),workStates.ToImmutable());
    }

    private static void ApplyCuts(Span<byte> span, PhysicsWorldLayout layout, PhysicsTickInputs inputs)
    {
        foreach (var cut in inputs.Cuts)
        {
            var route = default(RopeRouteId);
            for (var i = 0; i < layout.TensileSpans; i++)
            {
                var declared = PhysicsGpuAbi.ReadTensileSpan(span, i);
                if (declared.Id == cut) route = declared.Route;
            }
            if (route.Value == 0) throw new ArgumentException("Unknown cut span.");
            for (var i = 0; i < layout.TensileSpans; i++)
            {
                if (PhysicsGpuAbi.ReadTensileSpan(span, i).Route != route) continue;
                var offset = layout.TensileSpanOffset(i);
                BinaryPrimitives.WriteUInt32LittleEndian(span[(offset + PhysicsGpuAbi.TensileIntegrityOffset)..], (uint)RopeIntegrity.Cut);
                BinaryPrimitives.WriteUInt64LittleEndian(span[(offset + PhysicsGpuAbi.TensileCutSpanOffset)..], cut.Value);
                WriteF32(span, offset + PhysicsGpuAbi.TensileImpulseOffset, 0f);
            }
        }

    }

    private void ReadBodies(ReadOnlySpan<byte> span, PhysicsWorldLayout layout, uint bodyCount)
    {
        for (var slot = 0; slot < bodyCount; slot++)
        {
            var bodyOffset = layout.BodyOffset(slot);
            var motion = (RigidMotionKind)ReadU32(span, bodyOffset + PhysicsGpuAbi.BodyMotionOffset);
            var bodyIdLow = ReadU32(span, bodyOffset + PhysicsGpuAbi.RecordIdentityOffset);
            var bodyIdHigh = ReadU32(span, bodyOffset + PhysicsGpuAbi.RecordIdentityOffset + sizeof(uint));
            var id = ((ulong)bodyIdHigh << 32) | bodyIdLow;

            var cellX = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyCellOffset, 4));
            var cellY = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyCellOffset + sizeof(int), 4));
            var cellZ = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyCellOffset + 2 * sizeof(int), 4));
            var local = ReadVector(span, bodyOffset + PhysicsGpuAbi.BodyLocalOffset);
            var (localX, localY, localZ) = (local.X, local.Y, local.Z);
            var rotationXyz = ReadVector(span, bodyOffset + PhysicsGpuAbi.BodyRotationOffset);
            var (qx, qy, qz) = (rotationXyz.X, rotationXyz.Y, rotationXyz.Z);
            var qw = ReadF32(span, bodyOffset + PhysicsGpuAbi.BodyRotationOffset + PhysicsGpuAbi.VectorBytes);

            var px = (cellX + localX) / 16.0f;
            var py = (cellY + localY) / 16.0f;
            var pz = (cellZ + localZ) / 16.0f;

            while (_bodyPool.Count <= slot) _bodyPool.Add(new SimBody());
            var b = _bodyPool[slot];
            b.BodySlot = slot;
            b.BodyOffset = bodyOffset;
            b.BodyIdLow = bodyIdLow;
            b.BodyIdHigh = bodyIdHigh;
            b.Id = id;
            b.CellX = cellX; b.CellY = cellY; b.CellZ = cellZ;
            b.LocalX = localX; b.LocalY = localY; b.LocalZ = localZ;
            b.Px = px; b.Py = py; b.Pz = pz;
            b.Cmx = px; b.Cmy = py; b.Cmz = pz;
            b.Qx = qx; b.Qy = qy; b.Qz = qz; b.Qw = qw;
            b.TickStartRotation = b.Rotation;
            b.TickRelativeRotation = Quaternion.Identity;
            b.Motion = motion;

            if (motion == RigidMotionKind.Static)
            {
                // A pooled body may last have been dynamic (another world): a static body has no motion, and the
                // contact rows (rolling, prismatic frames) read its velocities directly.
                b.Vx = b.Vy = b.Vz = b.Wx = b.Wy = b.Wz = 0f;
                _staticBodies.Add(b);
                _allBodies[slot] = b;
            }
            else
            {
                var velocity = ReadVector(span, bodyOffset + PhysicsGpuAbi.BodyVelocityOffset);
                var angular = ReadVector(span, bodyOffset + PhysicsGpuAbi.BodyAngularVelocityOffset);
                var (vx, vy, vz) = (velocity.X, velocity.Y, velocity.Z);
                var (wx, wy, wz) = (angular.X, angular.Y, angular.Z);

                var mass = ReadF32(span, bodyOffset + PhysicsGpuAbi.BodyMassOffset);
                var drag = ReadF32(span, bodyOffset + PhysicsGpuAbi.BodyDragOffset);
                var gravity = ReadVector(span, bodyOffset + PhysicsGpuAbi.BodyGravityOffset);
                var (gx, gy, gz) = (gravity.X, gravity.Y, gravity.Z);

                var comLocal = ReadVector(span, bodyOffset + PhysicsGpuAbi.BodyCentreOfMassOffset);

                var frame = ReadVector(span, bodyOffset + PhysicsGpuAbi.BodyPrincipalFrameOffset);
                var principal = new Quaternion(frame, ReadF32(span, bodyOffset + PhysicsGpuAbi.BodyPrincipalFrameOffset + PhysicsGpuAbi.VectorBytes));

                var ix = ReadF32(span, bodyOffset + PhysicsGpuAbi.BodyPrincipalInertiaOffset);
                var iy = ReadF32(span, bodyOffset + PhysicsGpuAbi.BodyPrincipalInertiaOffset + PhysicsGpuAbi.PrincipalInertiaBytes);
                var iz = ReadF32(span, bodyOffset + PhysicsGpuAbi.BodyPrincipalInertiaOffset + 2 * PhysicsGpuAbi.PrincipalInertiaBytes);

                var invI = new Vector3(
                    float.IsFinite(ix) && ix > 0f ? 1f / ix : 0f,
                    float.IsFinite(iy) && iy > 0f ? 1f / iy : 0f,
                    float.IsFinite(iz) && iz > 0f ? 1f / iz : 0f);

                b.Vx = vx; b.Vy = vy; b.Vz = vz;
                b.Wx = wx; b.Wy = wy; b.Wz = wz;
                b.Mass = mass; b.InvMass = 1.0f / mass;
                b.Gx = gx; b.Gy = gy; b.Gz = gz;
                b.Drag = drag;
                b.ComLocal = comLocal; b.Principal = principal;
                b.InvI = invI;
                b.DynamicIndex = _dynamicBodies.Count;

                WorkshopCollisionKernels.UpdateDynamicFrame(b);
                _dynamicBodies.Add(b);
                _allBodies[slot] = b;
            }
        }

    }

    private void ReadColliders(ReadOnlySpan<byte> span, PhysicsWorldLayout layout, uint colliderCount)
    {
        _colliders.Clear();
        for (var c = 0; c < colliderCount; c++)
        {
            var colliderOffset = layout.ColliderOffset(c);
            var bodySlot = (int)ReadU32(span, colliderOffset + PhysicsGpuAbi.ColliderBodyOffset);
            var materialSlot = (int)ReadU32(span, colliderOffset + PhysicsGpuAbi.ColliderMaterialOffset);
            var shapeKind = (ShapeKind)ReadU32(span, colliderOffset + PhysicsGpuAbi.ColliderShapeOffset);

            var translation = ReadVector(span, colliderOffset + PhysicsGpuAbi.ColliderPoseOffset);
            var (tx, ty, tz) = (translation.X, translation.Y, translation.Z);
            var rotationOffset = colliderOffset + PhysicsGpuAbi.ColliderPoseOffset + PhysicsGpuAbi.PoseRotationOffset;
            var rq = ReadVector(span, rotationOffset);
            var (rqx, rqy, rqz) = (rq.X, rq.Y, rq.Z);
            var rqw = ReadF32(span, rotationOffset + PhysicsGpuAbi.VectorBytes);
            var radius = ReadF32(span, colliderOffset + PhysicsGpuAbi.ColliderRadiusOffset);
            var extents = ReadVector(span, colliderOffset + PhysicsGpuAbi.ColliderHalfExtentsOffset);
            var (hx, hy, hz) = (extents.X, extents.Y, extents.Z);

            var materialOffset = layout.MaterialOffset(materialSlot);
            var restitution = ReadF32(span, materialOffset + PhysicsGpuAbi.MaterialRestitutionOffset);
            var bounceThreshold = ReadF32(span, materialOffset + PhysicsGpuAbi.MaterialBounceThresholdOffset);
            var friction = ReadF32(span, materialOffset + PhysicsGpuAbi.MaterialFrictionOffset);
            var rollingResistance = ReadF32(span, materialOffset + PhysicsGpuAbi.MaterialRollingResistanceOffset);

            if (!_allBodies.TryGetValue(bodySlot, out var body)) continue;

            while (_colliderPool.Count <= c) _colliderPool.Add(new SimCollider());
            var collider = _colliderPool[c];
            collider.Index = c;
            collider.Id = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(colliderOffset + PhysicsGpuAbi.RecordIdentityOffset, 8));
            collider.BodySlot = bodySlot;
            collider.Body = body;
            collider.MaterialSlot = materialSlot;
            collider.Shape = shapeKind;
            collider.Tx = tx; collider.Ty = ty; collider.Tz = tz;
            collider.Rq = (rqx == 0f && rqy == 0f && rqz == 0f) ? Quaternion.Identity : new Quaternion(rqx, rqy, rqz, rqw);
            collider.Radius = radius;
            collider.Hx = hx; collider.Hy = hy; collider.Hz = hz;
            collider.Restitution = restitution;
            collider.BounceThreshold = bounceThreshold;
            collider.Friction = friction;
            collider.RollingResistance = rollingResistance;

            WorkshopCollisionKernels.RefreshCollider(collider);
            _colliders.Add(collider);


        }

    }

    private void ReadTensileRoutes(ReadOnlySpan<byte> span, PhysicsWorldLayout layout)
    {
        _joints.Clear();
        _revoluteJoints.Clear();
        _tensile.Clear();
        var routeSpans = new Dictionary<RopeRouteId, List<(int Index, TensileSpanDeclaration Declared, int Offset)>>();
        for (var index = 0; index < layout.TensileSpans; index++)
        {
            var declared = PhysicsGpuAbi.ReadTensileSpan(span, index);
            var offset = layout.TensileSpanOffset(index);
            if (!routeSpans.TryGetValue(declared.Route, out var list))
                routeSpans.Add(declared.Route, list = []);
            list.Add((index, declared, offset));
        }

        var routeIndex = 0;
        foreach (var (_, list) in routeSpans)
        {
            list.Sort((x, y) => x.Declared.Order.CompareTo(y.Declared.Order));
            while (_tensilePool.Count <= routeIndex) _tensilePool.Add(new());
            var route = _tensilePool[routeIndex++];

            var span0 = list[0];
            var lastSpan = list[^1];

            route.A = _allBodies[(int)ReadU32(span, span0.Offset + PhysicsGpuAbi.TensileBodyAOffset)];
            route.B = _allBodies[(int)ReadU32(span, lastSpan.Offset + PhysicsGpuAbi.TensileBodyBOffset)];
            route.LocalA = span0.Declared.LocalA;
            route.LocalB = lastSpan.Declared.LocalB;

            if (list.Count > 1)
            {
                var guides = new SimTensileGuide[list.Count - 1];
                for (var i = 0; i < list.Count - 1; i++)
                {
                    var body = _allBodies[(int)ReadU32(span, list[i].Offset + PhysicsGpuAbi.TensileBodyBOffset)];
                    guides[i] = new(body, list[i].Declared.GuideRadius, list[i].Declared.GuideOffset);
                }
                route.Guides = guides;
            }
            else
            {
                route.Guides = [];
            }

            route.Maximum = 0f;
            route.Complete = true;
            route.Cut = false;
            route.Offsets = new int[list.Count];

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                route.Offsets[i] = item.Offset;
                route.Maximum += item.Declared.MaximumLength;
                if (!item.Declared.Complete) route.Complete = false;
                if (ReadU32(span, item.Offset + PhysicsGpuAbi.TensileIntegrityOffset) == (uint)RopeIntegrity.Cut)
                    route.Cut = true;
            }

            route.Impulse = ReadF32(span, span0.Offset + PhysicsGpuAbi.TensileImpulseOffset);
            route.Travel = ReadF32(span, span0.Offset + PhysicsGpuAbi.TensileTravelOffset);
            route.Active = false;
            route.Row = default;
            _tensile.Add(route);
        }
    }

    private void ReadPrismatics(ReadOnlySpan<byte> span, PhysicsWorldLayout layout, PhysicsTickInputs inputs)
    {
        for (var index = 0; index < layout.Prismatics; index++)
        {
            var prismaticOffset = layout.PrismaticOffset(index);
            var aSlot = (int)ReadU32(span, prismaticOffset + PhysicsGpuAbi.PrismaticBodyAOffset);
            var bSlot = (int)ReadU32(span, prismaticOffset + PhysicsGpuAbi.PrismaticBodyBOffset);

            var (pa, qa) = ReadConstraintFrame(span, prismaticOffset + PhysicsGpuAbi.PrismaticFrameAOffset);
            var (pb, qb) = ReadConstraintFrame(span, prismaticOffset + PhysicsGpuAbi.PrismaticFrameBOffset);
            var lower = ReadF32(span, prismaticOffset + PhysicsGpuAbi.PrismaticLowerOffset);
            var upper = ReadF32(span, prismaticOffset + PhysicsGpuAbi.PrismaticUpperOffset);
            var stiffness = ReadF32(span, prismaticOffset + PhysicsGpuAbi.PrismaticStiffnessOffset);
            var damping = ReadF32(span, prismaticOffset + PhysicsGpuAbi.PrismaticDampingOffset);
            var soft = SoftConstraintCoefficients.FromSpring(stiffness, damping, 1f / WorkshopCadenceSettings.PhysicalFrequency);
            var collision = ReadU32(span, prismaticOffset + PhysicsGpuAbi.PrismaticCollisionOffset);

            while (_jointPool.Count <= index) _jointPool.Add(new SimJoint());
            var joint = _jointPool[index];
            joint.Offset = prismaticOffset;
            joint.A = _allBodies[aSlot];
            joint.B = _allBodies[bSlot];
            joint.Pa = pa; joint.Qa = qa; joint.Pb = pb; joint.Qb = qb;
            joint.Lower = lower; joint.Upper = upper;
            joint.Stiffness = stiffness;
            joint.Soft = soft;
            joint.Collision = collision;
            joint.SupplyClosed = inputs.SupplyClosed[index];
            joint.DriveKind = (PrismaticDriveKind)ReadU32(span, prismaticOffset + PhysicsGpuAbi.PrismaticDriveKindOffset);
            joint.Drive = new(new(BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(prismaticOffset + PhysicsGpuAbi.RecordIdentityOffset, 8))),
                ReadF32(span, prismaticOffset + PhysicsGpuAbi.PrismaticDriveTargetOffset),
                ReadF32(span, prismaticOffset + PhysicsGpuAbi.PrismaticDriveMaxSpeedOffset),
                ReadF32(span, prismaticOffset + PhysicsGpuAbi.PrismaticDriveStoppingAccelerationOffset),
                ReadF32(span, prismaticOffset + PhysicsGpuAbi.PrismaticDriveMaxEffortOffset),
                ReadF32(span, prismaticOffset + PhysicsGpuAbi.PrismaticDriveMaxPowerOffset));
            joint.MovingContact = false;

            for (var imp = 0; imp < PhysicsGpuAbi.PrismaticRowCount; imp++)
                joint.Impulses[imp] = ReadF32(span, prismaticOffset + PhysicsGpuAbi.PrismaticImpulseOffset + imp * 4);

            _joints.Add(joint);
        }

    }

    private void ReadRevolutes(ReadOnlySpan<byte> span, PhysicsWorldLayout layout, PhysicsTickInputs inputs, Dictionary<RevoluteConstraintId, SimRevoluteJoint>? revoluteJointMap)
    {
        var revoluteCount = layout.Revolutes;
        for (var index = 0; index < revoluteCount; index++)
        {
            var revoluteOffset = layout.RevoluteOffset(index);
            var aSlot = (int)ReadU32(span, revoluteOffset + PhysicsGpuAbi.RevoluteBodyAOffset);
            var bSlot = (int)ReadU32(span, revoluteOffset + PhysicsGpuAbi.RevoluteBodyBOffset);

            var (pa, qa) = ReadConstraintFrame(span, revoluteOffset + PhysicsGpuAbi.RevoluteFrameAOffset);
            var (pb, qb) = ReadConstraintFrame(span, revoluteOffset + PhysicsGpuAbi.RevoluteFrameBOffset);
            var lower = ReadF32(span, revoluteOffset + PhysicsGpuAbi.RevoluteLowerOffset);
            var upper = ReadF32(span, revoluteOffset + PhysicsGpuAbi.RevoluteUpperOffset);
            var stiffness = ReadF32(span, revoluteOffset + PhysicsGpuAbi.RevoluteStiffnessOffset);
            var damping = ReadF32(span, revoluteOffset + PhysicsGpuAbi.RevoluteDampingOffset);
            var frictionTorque = ReadF32(span, revoluteOffset + PhysicsGpuAbi.RevoluteFrictionTorqueOffset);
            var soft = stiffness > 0f
                ? SoftConstraintCoefficients.FromSpring(stiffness, damping, WorkshopConstraintSolver.SubstepSeconds)
                : (damping > 0f ? SoftConstraintCoefficients.FromDamper(damping, WorkshopConstraintSolver.SubstepSeconds) : default);
            var collision = ReadU32(span, revoluteOffset + PhysicsGpuAbi.RevoluteCollisionOffset);

            while (_revoluteJointPool.Count <= index) _revoluteJointPool.Add(new SimRevoluteJoint());
            var joint = _revoluteJointPool[index];
            if (revoluteJointMap is not null)
            {
                var revoluteId = new RevoluteConstraintId(BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(revoluteOffset + PhysicsGpuAbi.RecordIdentityOffset, 8)));
                revoluteJointMap.Add(revoluteId, joint);
            }
            joint.Offset = revoluteOffset;
            joint.A = _allBodies[aSlot];
            joint.B = _allBodies[bSlot];
            joint.Pa = pa; joint.Qa = qa; joint.Pb = pb; joint.Qb = qb;
            joint.Lower = lower; joint.Upper = upper;
            joint.Stiffness = stiffness;
            joint.Damping = damping;
            joint.FrictionTorque = frictionTorque;
            joint.Travel = (RevoluteTravel)ReadU32(span, revoluteOffset + PhysicsGpuAbi.RevoluteTravelKindOffset);
            // An undriven record's drive values are zero, so its drive row is bounded to [0, 0].
            joint.DriveTargetSpeed = ReadF32(span, revoluteOffset + PhysicsGpuAbi.RevoluteDriveTargetSpeedOffset);
            joint.DriveMaxTorque = ReadF32(span, revoluteOffset + PhysicsGpuAbi.RevoluteDriveMaxTorqueOffset);
            // Revolute supply slots follow the prismatic ones (CompiledSupplyGraph's drive-slot order).
            joint.SupplyClosed = inputs.SupplyClosed[layout.Prismatics + index];
            joint.TravelAngle = ReadF32(span, revoluteOffset + PhysicsGpuAbi.RevoluteTravelOffset);
            joint.Work = ReadF32(span, revoluteOffset + PhysicsGpuAbi.RevoluteWorkOffset);
            joint.Soft = soft;
            joint.Collision = collision;

            for (var imp = 0; imp < PhysicsGpuAbi.RevoluteRowCount; imp++)
                joint.Impulses[imp] = ReadF32(span, revoluteOffset + PhysicsGpuAbi.RevoluteImpulseOffset + imp * 4);

            _revoluteJoints.Add(joint);
        }

        // Resolve the admitted topology once per epoch. The referenced pool objects above
        // refresh from committed state every tick; each row and its scratch remain owned here.
        if (revoluteJointMap is not null)
        {
            foreach (var declaration in _declaredTransmissions)
            {
                _transmissions.Add(new SimMechanicalTransmission
                {
                    DrivingJoint = revoluteJointMap[declaration.DrivingJoint],
                    DrivenJoint = revoluteJointMap[declaration.DrivenJoint],
                    Ratio = declaration.Ratio
                });
            }
            foreach (var declaration in _declaredSurfaces)
            {
                var collider = _colliders.Single(value => value.Id == declaration.Collider.Value);
                _surfaces.Add(new SimDrivenSurface
                {
                    Collider = collider, Source = revoluteJointMap[declaration.Source], Face = declaration.Face,
                    LocalDirection = declaration.LocalDirection, TravelPerRadian = declaration.TravelPerRadian
                });
            }
            _transmissionsResolved = true;
        }

    }

    private void PrepareBroadphase(float dt, int steps)
    {
        _bvh.Clear();
        var tickTime = dt * steps;
        const float velocitySlop = 0.05f;

        foreach (var c in _colliders)
        {
            var b = c.Body;
            var isDyn = b.Motion == RigidMotionKind.Dynamic;
            var spin = isDyn ? MathF.Sqrt(b.Wx * b.Wx + b.Wy * b.Wy + b.Wz * b.Wz) * WorkshopCollisionKernels.SweepReach(c) * tickTime : 0f;
            var fatX = isDyn ? (MathF.Abs(b.Vx) + MathF.Abs(b.Gx) * tickTime) * tickTime + spin + velocitySlop : velocitySlop;
            var fatY = isDyn ? (MathF.Abs(b.Vy) + MathF.Abs(b.Gy) * tickTime) * tickTime + spin + velocitySlop : velocitySlop;
            var fatZ = isDyn ? (MathF.Abs(b.Vz) + MathF.Abs(b.Gz) * tickTime) * tickTime + spin + velocitySlop : velocitySlop;

            Vector3 min, max;
            if (c.Shape == ShapeKind.Plane)
            {
                min = new Vector3(-100f, -100f, -100f);
                max = new Vector3(100f, c.Py + velocitySlop, 100f);
            }
            else if (c.Shape == ShapeKind.Sphere)
            {
                var r = c.Radius;
                min = new Vector3(c.Px - r - fatX, c.Py - r - fatY, c.Pz - r - fatZ);
                max = new Vector3(c.Px + r + fatX, c.Py + r + fatY, c.Pz + r + fatZ);
            }
            else if (c.Shape == ShapeKind.Box)
            {
                var ex = c.Hx * MathF.Abs(c.U0.X) + c.Hy * MathF.Abs(c.U1.X) + c.Hz * MathF.Abs(c.U2.X);
                var ey = c.Hx * MathF.Abs(c.U0.Y) + c.Hy * MathF.Abs(c.U1.Y) + c.Hz * MathF.Abs(c.U2.Y);
                var ez = c.Hx * MathF.Abs(c.U0.Z) + c.Hy * MathF.Abs(c.U1.Z) + c.Hz * MathF.Abs(c.U2.Z);
                min = new Vector3(c.Px - ex - fatX, c.Py - ey - fatY, c.Pz - ez - fatZ);
                max = new Vector3(c.Px + ex + fatX, c.Py + ey + fatY, c.Pz + ez + fatZ);
            }
            else if (WorkshopCollisionKernels.IsHollow(c.Shape))
            {
                // A hollow collider (static): its outer cylinder's, or its torus arc's, exact bounds.
                WorkshopCollisionKernels.HollowBounds(c, out var low, out var high);
                min = low - new Vector3(fatX, fatY, fatZ);
                max = high + new Vector3(fatX, fatY, fatZ);
            }
            else
            {
                // The cylinder: its exact rotated extents h |a_i| + r √(1 − a_i²).
                var ex = WorkshopCollisionKernels.CylinderProjection(c, Vector3.UnitX);
                var ey = WorkshopCollisionKernels.CylinderProjection(c, Vector3.UnitY);
                var ez = WorkshopCollisionKernels.CylinderProjection(c, Vector3.UnitZ);
                min = new Vector3(c.Px - ex - fatX, c.Py - ey - fatY, c.Pz - ez - fatZ);
                max = new Vector3(c.Px + ex + fatX, c.Py + ey + fatY, c.Pz + ez + fatZ);
            }

            // Exactly the complement of the overlap callback's static-static rejection below.
            _bvh.InsertLeaf(c.Index, min, max, b.Motion != RigidMotionKind.Static);
        }

        _candidatePairs.Clear();
        _bvh.QueryOverlaps((idxA, idxB) =>
        {
            if (idxA >= idxB) return;
            var colA = _colliders[idxA];
            var colB = _colliders[idxB];
            if (colA.BodySlot == colB.BodySlot) return;
            if (colA.Body.Motion == RigidMotionKind.Static && colB.Body.Motion == RigidMotionKind.Static) return;
            foreach (var j in _joints)
            {
                if (j.Collision == 0 && ((j.A == colA.Body && j.B == colB.Body) || (j.B == colA.Body && j.A == colB.Body)))
                    return;
            }
            foreach (var j in _revoluteJoints)
            {
                if (j.Collision == 0 && ((j.A == colA.Body && j.B == colB.Body) || (j.B == colA.Body && j.A == colB.Body)))
                    return;
            }
            _candidatePairs.Add((colA, colB));
        });

    }

    private void CommitResidence(Span<byte> span, PhysicsWorldLayout layout, PhysicsInteractionSnapshot committedInteractions, ImmutableDictionary<CapturePairKey, CapturePairState>.Builder captureStates, uint sourceOrdinal, uint nextOrdinal, float dt)
    {
        var totalCaptured = 0u;
        var sensorCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderSensorCountOffset, 4));
        for (var i = 0; i < sensorCount; i++)
        {
            var sensorOffset = layout.SensorOffset(i);
            var frameSlot = (int)ReadU32(span, sensorOffset + PhysicsGpuAbi.SensorFrameOffset);
            var named=ReadU32(span,sensorOffset+PhysicsGpuAbi.SensorTargetOffset);
            var kind=(BodyTargetKind)ReadU32(span,sensorOffset+PhysicsGpuAbi.SensorTargetKindOffset);
            var sensorId=new GpuSensorId(BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(sensorOffset+PhysicsGpuAbi.RecordIdentityOffset,8)));
            var participation = ReadU32(span, sensorOffset + PhysicsGpuAbi.SensorParticipationOffset);
            var minimum = ReadVector(span, sensorOffset + PhysicsGpuAbi.SensorMinimumOffset);
            var maximum = ReadVector(span, sensorOffset + PhysicsGpuAbi.SensorMaximumOffset);
            var (minX, minY, minZ) = (minimum.X, minimum.Y, minimum.Z);
            var (maxX, maxY, maxZ) = (maximum.X, maximum.Y, maximum.Z);
            var speedLimit = ReadF32(span, sensorOffset + PhysicsGpuAbi.SensorSpeedLimitOffset);
            var dwell = ReadF32(span, sensorOffset + PhysicsGpuAbi.SensorDwellOffset);

            _allBodies.TryGetValue(frameSlot,out var frameBody);
            foreach(var targetBody in _dynamicBodies)
            {
            if(!PhysicsGpuAbi.MatchesTarget(span,kind,named,(uint)targetBody.BodySlot)) continue;
            var targetId=new GpuBodyId((ulong)targetBody.BodyIdHigh<<32 | targetBody.BodyIdLow);
            var key=new CapturePairKey(sensorId,targetId);
            var previous=committedInteractions.Residence(key);
            var prevPhase=(uint)previous.Phase;
            var prevStartOrdinal=previous.StartOrdinal;
            var prevStartPhase=previous.StartPhase;
            uint phase = 0;
            uint startOrdinal = 0;
            float startPhase = 0f;
            uint occurrenceCount = 0;
            uint eventOrdinal = 0;
            float eventPhase = 0f;

            if (participation == 1 && targetBody != null && frameBody != null)
            {
                var rel = targetBody.Position - frameBody.Position;
                var loc = WorkshopCollisionKernels.InvRotateVector(frameBody.Rotation, rel);
                var speed = targetBody.LinearVelocity.Length();

                var inside = loc.X >= minX && loc.X <= maxX &&
                             loc.Y >= minY && loc.Y <= maxY &&
                             loc.Z >= minZ && loc.Z <= maxZ &&
                             speed <= speedLimit;

                if (prevPhase == 2)
                {
                    phase = 2;
                    startOrdinal = prevStartOrdinal;
                    startPhase = prevStartPhase;
                }
                else if (inside)
                {
                    if (prevPhase == 0)
                    {
                        phase = 1;
                        startOrdinal = sourceOrdinal;
                    }
                    else if (prevPhase == 1)
                    {
                        startOrdinal = prevStartOrdinal;
                        startPhase = prevStartPhase;
                        var elapsed = (nextOrdinal - startOrdinal) * dt;
                        if (elapsed >= dwell)
                        {
                            phase = 2;
                            occurrenceCount = 1;
                            eventOrdinal = nextOrdinal;
                        }
                        else
                        {
                            phase = 1;
                        }
                    }
                }
            }

            var residence=new ResidenceRead(sensorId,targetId,(ResidencePhase)phase,occurrenceCount,startOrdinal,startPhase,eventOrdinal,eventPhase);
            var captureState=committedInteractions.Captures.TryGetValue(key,out var retained) ? retained.Latch : CaptureLatch.Clear(sensorId,targetId);
            captureState=captureState.Consume(residence);
            if(phase==0 && captureState.Phase==CaptureLatchPhase.Clear) captureStates.Remove(key);
            else captureStates[key]=new(residence,captureState);
            totalCaptured = checked(totalCaptured+occurrenceCount);
            }
        }

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderCapturedCountOffset, 4), totalCaptured);

    }

    private void AdvanceSubsteps(Span<byte> span, PhysicsWorldLayout layout, PhysicsTickInputs inputs, int steps, uint sourceOrdinal, Span<ContactLoadSensorDeclaration> loadDeclarations, Span<int> loadFrames, Span<float> loadMass, ImmutableDictionary<WorkPairKey, ContactWorkOccurrence>.Builder workStates)
    {
        var dt = WorkshopConstraintSolver.SubstepSeconds;
        var softCoeffs = SoftCoefficients.MakeSoft(WorkshopConstraintSolver.ContactHertz, WorkshopConstraintSolver.ContactDampingRatio, dt);
        var loadRows = loadDeclarations.Length;
        var motionOffset = layout.MotionOffset;
        for (var s = 0; s < steps; s++)
        {
            // Box2D v3 prepares contacts before integrating velocities: the restitution approach is the substep-start velocity.
            foreach (var b in _dynamicBodies)
            {
                b.V0 = b.LinearVelocity;
                b.W0 = b.AngularVelocity;
            }

            TransferSupply(span, layout, inputs, sourceOrdinal, s);
            ApplyGuides(span, layout, dt);
            // Each declared drive follows its own compiled route (SimJoint.SupplyClosed), closed from committed state, never a timer.
            PrismaticConstraintSolver.ApplySuppliedDrives(_joints, dt);
            // Compliant membranes are external forces like gravity, inside the restitution pass's pre-solve energy bound.
            ApplyCompliantMembranes(span, layout, dt);

            foreach (var b in _dynamicBodies)
            {
                b.Vx += b.Gx * dt;
                b.Vy += b.Gy * dt;
                b.Vz += b.Gz * dt;
                var decay = MathF.Exp(-b.Drag * dt);
                b.Vx *= decay;
                b.Vy *= decay;
                b.Vz *= decay;
                b.PreSolveKinetic = WorkshopConstraintSolver.Kinetic(b, b.LinearVelocity, b.AngularVelocity);
            }

            foreach (var c in _colliders)
                if (c.Body.Motion == RigidMotionKind.Dynamic) WorkshopCollisionKernels.RefreshCollider(c);

            PrepareContacts(dt);
            WarmStartConstraints(dt);
            for (var iteration = 0; iteration < WorkshopConstraintSolver.SolverIterations; iteration++)
            {
                PrismaticConstraintSolver.SweepConstraints(_joints, true);
                RevoluteConstraintSolver.SweepConstraints(_revoluteJoints, true);
                foreach (var transmission in _transmissions) MechanicalTransmissionSolver.Solve(transmission);
                foreach (var tensile in _tensile) TensileConstraintSolver.Solve(tensile, true);
                WorkshopConstraintSolver.SweepManifolds(_manifolds, ContactPass.Solve, softCoeffs, dt);
            }

            IntegrateBodies(dt);
            PrepareRelaxation(dt);
            for (var iteration = 0; iteration < WorkshopConstraintSolver.RelaxIterations; iteration++)
            {
                PrismaticConstraintSolver.SweepConstraints(_joints, false);
                RevoluteConstraintSolver.SweepConstraints(_revoluteJoints, false);
                foreach (var transmission in _transmissions) MechanicalTransmissionSolver.Solve(transmission);
                foreach (var tensile in _tensile) TensileConstraintSolver.Solve(tensile, false);
                WorkshopConstraintSolver.SweepManifolds(_manifolds, ContactPass.Relax, softCoeffs, dt);
            }

            CommitConstraints(span, dt);
            WorkshopConstraintSolver.RestitutionPass(_manifolds, _restitutionBodies, ++_restitutionStamp);
            // Loads are read after the restitution pass: a point that carried normal impulse presses its face even from the gap.
            if (loadRows > 0) MeasureContactLoads(loadDeclarations[..loadRows], loadFrames[..loadRows], loadMass[..loadRows]);
            foreach (var b in _dynamicBodies) WorkshopConstraintSolver.SettleVelocity(b);
            // Each sample's contact state and the bed's deflection follow this substep's resulting pose; the next substep's force reads them.
            UpdateCompliantContacts(span, layout);

            CommitContactObservations(span, layout, sourceOrdinal, s, workStates);
            CommitMotion(span, motionOffset, steps, s, sourceOrdinal);
        }

    }

    private void TransferSupply(Span<byte> span, PhysicsWorldLayout layout, PhysicsTickInputs inputs, uint sourceOrdinal, int s)
    {
        var supplyCount = layout.ElectricalSources;
        if ((sourceOrdinal + s) % 4 == 0)
        {
            for (var supply = 0; supply < supplyCount; supply++)
            {
                var supplyOffset = layout.ElectricalSourceOffset(supply);
                var nextEnabled = inputs.SourceEnable[supply];
                WriteU32(span, supplyOffset + PhysicsGpuAbi.ElectricalSourceEnableOffset, (uint)nextEnabled);

                _storeIndices.Clear();
                _balances.Clear();
                _capacities.Clear();
                _credits.Clear();

                for (var route = 0; route < layout.ElectricalBindings; route++)
                {
                    var bindingOffset = layout.ElectricalBindingOffset(route);
                    if (ReadU32(span, bindingOffset + PhysicsGpuAbi.ElectricalBindingSourceOffset) != supply) continue;
                    var targetStore = (int)ReadU32(span, bindingOffset + PhysicsGpuAbi.ElectricalBindingStorageOffset);
                    var storeOffset = layout.ContactWorkOffset(targetStore);
                    _storeIndices.Add(storeOffset);
                    _balances.Add(ReadF32(span, storeOffset + PhysicsGpuAbi.ContactWorkAvailableOffset));
                    _capacities.Add(ReadF32(span, storeOffset + PhysicsGpuAbi.ContactWorkPreloadOffset));
                    _credits.Add(ReadF32(span, storeOffset + PhysicsGpuAbi.ContactWorkSuppliedOffset));
                }

                var count = _storeIndices.Count;
                // Per-source load scratch, grown to the largest fan-out this world has (no load capacity).
                Span<float> elecBefore = Scratch(ref _elecBefore, count);
                Span<float> elecLimits = Scratch(ref _elecLimits, count);
                Span<float> elecAfter = Scratch(ref _elecAfter, count);
                for (var i = 0; i < count; i++)
                {
                    elecBefore[i] = _balances[i];
                    elecLimits[i] = _capacities[i];
                }

                var bal = ReadF32(span, supplyOffset + PhysicsGpuAbi.ElectricalSourceRemainingOffset);
                var pwr = ReadF32(span, supplyOffset + PhysicsGpuAbi.ElectricalSourceMaximumPowerOffset);
                var deb = ReadF32(span, supplyOffset + PhysicsGpuAbi.ElectricalSourceDebitOffset);
                var transfer = ElectricalSupply.Allocate((ElectricalEnable)nextEnabled, bal, pwr, elecBefore, elecLimits, elecAfter);

                WriteF32(span, supplyOffset + PhysicsGpuAbi.ElectricalSourceRemainingOffset, transfer.Remaining);
                WriteF32(span, supplyOffset + PhysicsGpuAbi.ElectricalSourceDebitOffset, deb + transfer.Debit);
                for (var load = 0; load < count; load++)
                {
                    WriteF32(span, _storeIndices[load] + PhysicsGpuAbi.ContactWorkAvailableOffset, elecAfter[load]);
                    WriteF32(span, _storeIndices[load] + PhysicsGpuAbi.ContactWorkSuppliedOffset, _credits[load] + (elecAfter[load] - elecBefore[load]));
                }
            }
        }

    }

    private void ApplyGuides(ReadOnlySpan<byte> span, PhysicsWorldLayout layout, float dt)
    {
        var guideCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderGuideCountOffset, 4));
        foreach(var targetBody in _dynamicBodies)
        {
            var bestDistance=float.PositiveInfinity;
            var bestOwner=ulong.MaxValue; var bestGuide=ulong.MaxValue;
            var bestAcceleration=Vector3.Zero;
        for (var g = 0; g < guideCount; g++)
        {
            var guideOffset = layout.GuideOffset(g);
            var frameSlot = (int)ReadU32(span, guideOffset + PhysicsGpuAbi.GuideFrameOffset);
            var named=ReadU32(span,guideOffset+PhysicsGpuAbi.GuideTargetOffset);
            var kind=(BodyTargetKind)ReadU32(span,guideOffset+PhysicsGpuAbi.GuideTargetKindOffset);
            if(!PhysicsGpuAbi.MatchesTarget(span,kind,named,(uint)targetBody.BodySlot)) continue;
            var minimum = ReadVector(span, guideOffset + PhysicsGpuAbi.GuideMinimumOffset);
            var maximum = ReadVector(span, guideOffset + PhysicsGpuAbi.GuideMaximumOffset);
            var (minX, minY, minZ) = (minimum.X, minimum.Y, minimum.Z);
            var (maxX, maxY, maxZ) = (maximum.X, maximum.Y, maximum.Z);
            var maxAcc = ReadF32(span, guideOffset + PhysicsGpuAbi.GuideMaximumAccelerationOffset);
            var supportHeight = ReadF32(span, guideOffset + PhysicsGpuAbi.GuideSupportHeightOffset);
            var supportMargin = ReadF32(span, guideOffset + PhysicsGpuAbi.GuideSupportMarginOffset);

            if (maxAcc <= 0f) continue;
            if (!_allBodies.TryGetValue(frameSlot, out var frameBody)) continue;

            var up = WorkshopCollisionKernels.RotateVector(frameBody.Rotation, Vector3.UnitY);
            var lowest = float.PositiveInfinity;
            foreach (var child in _colliders)
            {
                if (child.Body != targetBody) continue;
                WorkshopCollisionKernels.RefreshCollider(child);
                var point = WorkshopCollisionKernels.SupportPoint(child, -up);
                lowest = MathF.Min(lowest, Vector3.Dot(point - frameBody.Position, up));
            }
            var rel = targetBody.Position - frameBody.Position;
            var loc = WorkshopCollisionKernels.InvRotateVector(frameBody.Rotation, rel);
            var locV = WorkshopCollisionKernels.InvRotateVector(frameBody.Rotation, targetBody.LinearVelocity);

            var inside = loc.X >= minX && loc.X <= maxX &&
                         loc.Y >= minY && loc.Y <= maxY &&
                         loc.Z >= minZ && loc.Z <= maxZ &&
                         lowest >= supportHeight - supportMargin &&
                         locV.Y <= 0f;

            if (inside)
            {
                var flatLen = MathF.Sqrt(loc.X * loc.X + loc.Z * loc.Z);
                var accScale = -maxAcc / MathF.Max(1.0f, flatLen);
                var aloc = new Vector3(loc.X * accScale, 0f, loc.Z * accScale);
                var aWorld = WorkshopCollisionKernels.RotateVector(frameBody.Rotation, aloc);
                var distance=loc.X*loc.X+loc.Z*loc.Z;
                var owner=(ulong)frameBody.BodyIdHigh<<32 | frameBody.BodyIdLow;
                var guide=BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(guideOffset+PhysicsGpuAbi.RecordIdentityOffset,8));
                if(distance<bestDistance || (distance==bestDistance && (owner<bestOwner || (owner==bestOwner && guide<bestGuide))))
                { bestDistance=distance; bestOwner=owner; bestGuide=guide; bestAcceleration=aWorld; }
            }
        }
            targetBody.Vx+=bestAcceleration.X*dt;
            targetBody.Vz+=bestAcceleration.Z*dt;
        }

    }

    private void PrepareContacts(float dt)
    {
        _contacts.Clear();
        _manifolds.Clear();
        foreach (var pair in _candidatePairs)
        {
            var colA = pair.ColA;
            var colB = pair.ColB;
            if (colA.Index > colB.Index) (colA, colB) = (colB, colA);

            var bA = colA.Body;
            var bB = colB.Body;
            var relSpeed = (bA.LinearVelocity - bB.LinearVelocity).Length() +
                bA.AngularVelocity.Length() * WorkshopCollisionKernels.SweepReach(colA) +
                bB.AngularVelocity.Length() * WorkshopCollisionKernels.SweepReach(colB);

            var first = _contacts.Count;
            var margin = WorkshopConstraintSolver.SpeculativeSlop + relSpeed * dt;
            WorkshopCollisionKernels.GenerateManifold(colA, colB, margin, _contacts);
            WorkshopCollisionKernels.AdvanceSpeculativeSphereContact(colA, colB, margin, dt, _contacts, first, _probeCollider, _probeContacts);
            if (_contacts.Count > first)
            {
                while (_manifoldPool.Count <= _manifolds.Count) _manifoldPool.Add(new ContactManifold());
                var manifold = _manifoldPool[_manifolds.Count];
                manifold.Points.Clear();
                for (var i = first; i < _contacts.Count; i++) manifold.Points.Add(_contacts[i]);
                _manifolds.Add(manifold);
            }
        }

        WorkshopCollisionKernels.RemoveInternalFeatures(_manifolds, _contacts);
        WorkshopCollisionKernels.GroupRigidSupports(_manifolds, _contacts, _manifoldPool);
        foreach (var b in _dynamicBodies)
        {
            b.Cm0 = b.CentreOfMass;
            b.Q0 = b.Rotation;
        }
        foreach (var c in _contacts) WorkshopConstraintSolver.PrepareContact(c, _contactCache);
        foreach (var m in _manifolds)
        {
            WorkshopConstraintSolver.PrepareNormalBlocks(m, dt, _normalContactSets);
            WorkshopFrictionSolver.Prepare(m, _frictionCache, _surfaces);
            WorkshopConstraintSolver.PrepareRolling(m, dt);
        }

    }

    private void WarmStartConstraints(float dt)
    {
        foreach (var joint in _joints)
        {
            PrismaticConstraintSolver.PrepareJointRows(joint, dt);
            // Warm start: each stored impulse is first clamped to its row's current bounds.
            for (var row = 0; row < 8; row++)
            {
                joint.Impulses[row] = joint.Rows[row].Bounded(joint.Impulses[row]);
                PrismaticConstraintSolver.ApplyConstraintImpulse(joint, joint.Rows[row], joint.Impulses[row]);
            }
        }
        foreach (var joint in _revoluteJoints)
        {
            RevoluteConstraintSolver.PrepareJointRows(joint, dt);
            // Warm start: each stored impulse is first clamped to its row's current bounds, so a drive impulse from a supplied
            // substep never acts on an unsupplied one.
            for (var row = 0; row < RevoluteRows.Count; row++)
            {
                joint.Impulses[row] = joint.Rows[row].Bounded(joint.Impulses[row]);
                RevoluteConstraintSolver.ApplyConstraintImpulse(joint, joint.Rows[row], joint.Impulses[row]);
            }
        }
        foreach (var transmission in _transmissions)
        {
            MechanicalTransmissionSolver.Prepare(transmission);
        }
        foreach (var tensile in _tensile)
        {
            TensileConstraintSolver.Prepare(tensile, dt);
            TensileConstraintSolver.Apply(tensile, tensile.Impulse);
        }
        foreach (var c in _contacts) WorkshopConstraintSolver.WarmStartContact(c);
        foreach (var m in _manifolds) WorkshopFrictionSolver.WarmStart(m);

    }

    private void IntegrateBodies(float dt)
    {
        foreach (var b in _dynamicBodies)
        {
            WorkshopConstraintSolver.ClampVelocity(b);
            var prevCellX = b.CellX; var prevCellY = b.CellY; var prevCellZ = b.CellZ;
            var prevLocX = b.LocalX; var prevLocY = b.LocalY; var prevLocZ = b.LocalZ;
            var prevQ = b.Rotation;
            var prevRelativeRotation = b.TickRelativeRotation;

            var wLen = b.AngularVelocity.Length();
            var halfAngle = wLen * dt * 0.5f;
            // The analytic sin(w dt/2)/w limit also handles underflow in the length calculation.
            var sAngle = wLen == 0f ? dt * 0.5f : MathF.Sin(halfAngle) / wLen;
            var dq = new Quaternion(b.Wx * sAngle, b.Wy * sAngle, b.Wz * sAngle, MathF.Cos(halfAngle));
            b.TickRelativeRotation = Quaternion.Normalize(Quaternion.Multiply(dq, b.TickRelativeRotation));
            var nq = Quaternion.Normalize(Quaternion.Multiply(b.TickRelativeRotation, b.TickStartRotation));

            var cm = b.CentreOfMass + b.LinearVelocity * dt;
            var offset = Vector3.Transform(b.ComLocal, nq);
            var nPos = cm - offset;

            b.LocalX += (nPos.X - b.Px) * 16.0f;
            b.LocalY += (nPos.Y - b.Py) * 16.0f;
            b.LocalZ += (nPos.Z - b.Pz) * 16.0f;
            b.Qx = nq.X; b.Qy = nq.Y; b.Qz = nq.Z; b.Qw = nq.W;

            var carryX = (int)MathF.Floor(b.LocalX + 0.5f);
            var carryY = (int)MathF.Floor(b.LocalY + 0.5f);
            var carryZ = (int)MathF.Floor(b.LocalZ + 0.5f);
            b.CellX += carryX; b.LocalX -= carryX;
            b.CellY += carryY; b.LocalY -= carryY;
            b.CellZ += carryZ; b.LocalZ -= carryZ;

            b.Px = (b.CellX + b.LocalX) / 16.0f;
            b.Py = (b.CellY + b.LocalY) / 16.0f;
            b.Pz = (b.CellZ + b.LocalZ) / 16.0f;

            if (!float.IsFinite(b.Px + b.Py + b.Pz + b.Qx + b.Qy + b.Qz + b.Qw + b.Vx + b.Vy + b.Vz + b.Wx + b.Wy + b.Wz))
            {
                b.CellX = prevCellX; b.CellY = prevCellY; b.CellZ = prevCellZ;
                b.LocalX = prevLocX; b.LocalY = prevLocY; b.LocalZ = prevLocZ;
                b.Px = (b.CellX + b.LocalX) / 16.0f;
                b.Py = (b.CellY + b.LocalY) / 16.0f;
                b.Pz = (b.CellZ + b.LocalZ) / 16.0f;
                b.Qx = prevQ.X; b.Qy = prevQ.Y; b.Qz = prevQ.Z; b.Qw = prevQ.W;
                b.TickRelativeRotation = prevRelativeRotation;
                b.Vx = b.Vy = b.Vz = b.Wx = b.Wy = b.Wz = 0f;
            }
            WorkshopCollisionKernels.UpdateDynamicFrame(b);
        }

    }

    private void PrepareRelaxation(float dt)
    {
        foreach (var route in _tensile)
        {
            if (route.Active)
            {
                var aPoint = route.A.Position + Vector3.Transform(route.LocalA, route.A.Rotation);
                var bPoint = route.B.Position + Vector3.Transform(route.LocalB, route.B.Rotation);
                var vA = route.A.LinearVelocity + Vector3.Cross(route.A.AngularVelocity, aPoint - route.A.CentreOfMass);
                var vB = route.B.LinearVelocity + Vector3.Cross(route.B.AngularVelocity, bPoint - route.B.CentreOfMass);
                var termA = Vector3.Dot(route.UnitA, vA);
                var termB = -Vector3.Dot(route.UnitB, vB);
                var vTravel = (route.A.InvMass > 0f && route.B.InvMass > 0f) ? (termA + termB) * 0.5f : (route.A.InvMass > 0f ? termA : termB);
                route.Travel += vTravel * dt;
            }
        }

        foreach (var joint in _joints)
        {
            var softRowError = joint.Rows[(int)PrismaticRowKind.Spring].Error;
            PrismaticConstraintSolver.PrepareJointRows(joint, dt);
            joint.Rows[(int)PrismaticRowKind.Spring].Error = softRowError;
        }
        foreach (var joint in _revoluteJoints)
        {
            var axialError = joint.Rows[(int)RevoluteRowKind.Axial].Error;
            RevoluteConstraintSolver.PrepareJointRows(joint, dt);
            joint.Rows[(int)RevoluteRowKind.Axial].Error = axialError;
        }

        foreach (var tensile in _tensile) TensileConstraintSolver.Prepare(tensile, dt);
        foreach (var transmission in _transmissions) MechanicalTransmissionSolver.Prepare(transmission);
    }

    private void CommitConstraints(Span<byte> span, float dt)
    {
        foreach (var tensile in _tensile)
        {
            foreach (var offset in tensile.Offsets)
            {
                WriteF32(span, offset + PhysicsGpuAbi.TensileImpulseOffset, tensile.Impulse);
                WriteF32(span, offset + PhysicsGpuAbi.TensileTravelOffset, tensile.Travel);
            }
        }
        foreach (var joint in _joints)
        {
            for (var row = 0; row < PhysicsGpuAbi.PrismaticRowCount; row++)
                WriteF32(span, joint.Offset + PhysicsGpuAbi.PrismaticImpulseOffset + row * 4, joint.Impulses[row]);
            // The committed moving-contact lane: a positive normal impulse on the moving body from anything but its base.
            foreach (var c in _contacts)
                if (c.LambdaN > 0f && ((c.A == joint.B && c.B != joint.A) || (c.B == joint.B && c.A != joint.A)))
                {
                    joint.MovingContact = true;
                    break;
                }
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(joint.Offset + PhysicsGpuAbi.PrismaticMovingContactOffset, 4),
                joint.MovingContact ? 1u : 0u);
        }
        foreach (var joint in _revoluteJoints)
        {
            // After relax, with this substep's final impulses: the relative axial spin ω_rel (the drive row's Jacobian, prepared on
            // the relax pose) advances the signed travel by ω_rel·h, and the drive's final impulse adds its positive work
            // max(0, λ_drive·ω_rel). Braking and stall add nothing.
            ref readonly var drive = ref joint.Rows[(int)RevoluteRowKind.Drive];
            var spin = ConstraintBodyImpulse.ConstraintVelocity(joint.A, drive.La, drive.Aa) +
                ConstraintBodyImpulse.ConstraintVelocity(joint.B, drive.Lb, drive.Ab);
            joint.TravelAngle += spin * dt;
            joint.Work += MathF.Max(0f, joint.Impulses[(int)RevoluteRowKind.Drive] * spin);
            for (var row = 0; row < PhysicsGpuAbi.RevoluteRowCount; row++)
                WriteF32(span, joint.Offset + PhysicsGpuAbi.RevoluteImpulseOffset + row * 4, joint.Impulses[row]);
            WriteU32(span, joint.Offset + PhysicsGpuAbi.RevoluteSuppliedOffset, joint.SupplyClosed ? 1u : 0u);
            WriteF32(span, joint.Offset + PhysicsGpuAbi.RevoluteTravelOffset, joint.TravelAngle);
            WriteF32(span, joint.Offset + PhysicsGpuAbi.RevoluteWorkOffset, joint.Work);
        }

    }

    private void CommitContactObservations(Span<byte> span, PhysicsWorldLayout layout, uint sourceOrdinal, int s, ImmutableDictionary<WorkPairKey, ContactWorkOccurrence>.Builder workStates)
    {
        _contactCache.Clear();
        _normalContactSets.Clear();
        _frictionCache.Clear();
        foreach (var c in _contacts) _contactCache[c.Key] = c.LambdaN;
        foreach (var m in _manifolds)
        {
            _normalContactSets.Add(m.NormalWarmStartKey);
            for (var block = 0; block < m.NormalBlockCount; block++)
                _normalContactSets.Add(m.NormalBlocks[block].Key);
            WorkshopFrictionSolver.Store(m, _frictionCache);
        }

        // A point that carried normal impulse this substep, touching or speculative, is a contact (Box2D v3 hit events).
        foreach (var c in _contacts)
        {
            if (c.MaxImpulse <= 0f) continue;
            if (c.A.Motion == RigidMotionKind.Dynamic && c.B.Motion == RigidMotionKind.Static) EmitContactEvents(span, layout, c.ColB, c.A, -c.RelVel, c.Normal, c, s, sourceOrdinal, workStates);
            else if (c.B.Motion == RigidMotionKind.Dynamic && c.A.Motion == RigidMotionKind.Static) EmitContactEvents(span, layout, c.ColA, c.B, -c.RelVel, -c.Normal, c, s, sourceOrdinal, workStates);
        }

        var orientationCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderOrientationSensorCountOffset, 4));
        for (var o = 0; o < orientationCount; o++)
        {
            var oOffset = layout.OrientationSensorOffset(o);
            if (ReadU32(span, oOffset + PhysicsGpuAbi.OrientationSensorPhaseOffset) != (uint)OrientationSensorPhase.Armed) continue;
            var sensedSlot = (int)ReadU32(span, oOffset + PhysicsGpuAbi.OrientationSensorBodyOffset);
            if (!_allBodies.TryGetValue(sensedSlot, out var sensed)) continue;

            var declaredXyz = ReadVector(span, oOffset + PhysicsGpuAbi.OrientationSensorRotationOffset);
            var qDeclared = new Quaternion(declaredXyz,
                ReadF32(span, oOffset + PhysicsGpuAbi.OrientationSensorRotationOffset + PhysicsGpuAbi.VectorBytes));

            var alignment = MathF.Abs(sensed.Qx * qDeclared.X + sensed.Qy * qDeclared.Y + sensed.Qz * qDeclared.Z + sensed.Qw * qDeclared.W);
            if (alignment <= ReadF32(span, oOffset + PhysicsGpuAbi.OrientationSensorCosineOffset))
            {
                WriteU32(span, oOffset + PhysicsGpuAbi.OrientationSensorPhaseOffset, (uint)OrientationSensorPhase.Fired);
                WriteU32(span, oOffset + PhysicsGpuAbi.OrientationSensorOrdinalOffset, sourceOrdinal + (uint)s + 1);
                WriteF32(span, oOffset + PhysicsGpuAbi.OrientationSensorEventPhaseOffset, 0f);
            }
        }

    }

    private void CommitMotion(Span<byte> span, int motionOffset, int steps, int s, uint sourceOrdinal)
    {
        foreach (var b in _dynamicBodies)
        {
            var pieceOffset = motionOffset + PhysicsMotionRead.HeaderBytes + (b.DynamicIndex * steps + s) * PhysicsMotionRead.PieceBytes;
            span.Slice(pieceOffset, PhysicsMotionRead.PieceBytes).Clear();
            var pieceSpan = span.Slice(pieceOffset, PhysicsMotionRead.PieceBytes);

            WriteU32(pieceSpan, PhysicsMotionRead.KindOffset, (uint)PhysicsMotionKind.FreePolynomial);
            WriteU32(pieceSpan, PhysicsMotionRead.StartOrdinalOffset, sourceOrdinal + (uint)s);
            WriteU32(pieceSpan, PhysicsMotionRead.EndOrdinalOffset, sourceOrdinal + (uint)s + 1);
            WriteU32(pieceSpan, PhysicsMotionRead.AnchorOrdinalOffset, sourceOrdinal + (uint)s);
            WriteF32(pieceSpan, PhysicsMotionRead.StartPhaseOffset, 0f);
            WriteF32(pieceSpan, PhysicsMotionRead.EndPhaseOffset, 0f);
            WriteF32(pieceSpan, PhysicsMotionRead.AnchorPhaseOffset, 0f);
            WriteU32(pieceSpan, PhysicsMotionRead.FrequencyOffset, WorkshopCadenceSettings.PhysicalFrequency);
            WriteU32(pieceSpan, PhysicsMotionRead.OwnerOffset, b.BodyIdLow);
            WriteU32(pieceSpan, PhysicsMotionRead.OwnerOffset + sizeof(uint), b.BodyIdHigh);
            BinaryPrimitives.WriteInt32LittleEndian(pieceSpan.Slice(PhysicsMotionRead.CellOffset, 4), b.CellX);
            BinaryPrimitives.WriteInt32LittleEndian(pieceSpan.Slice(PhysicsMotionRead.CellOffset + sizeof(int), 4), b.CellY);
            BinaryPrimitives.WriteInt32LittleEndian(pieceSpan.Slice(PhysicsMotionRead.CellOffset + 2 * sizeof(int), 4), b.CellZ);
            WriteVector(pieceSpan, PhysicsMotionRead.CentreRemainderOffset, b.LocalX, b.LocalY, b.LocalZ);
            WriteF32(pieceSpan, PhysicsMotionRead.DragRateOffset, b.Drag);
            WriteVector(pieceSpan, PhysicsMotionRead.RotationOffset, b.Qx, b.Qy, b.Qz);
            WriteF32(pieceSpan, PhysicsMotionRead.RotationOffset + PhysicsGpuAbi.VectorBytes, b.Qw);
            WriteVector(pieceSpan, PhysicsMotionRead.VelocityOffset, b.Vx, b.Vy, b.Vz);
            WriteVector(pieceSpan, PhysicsMotionRead.AngularVelocityOffset, b.Wx, b.Wy, b.Wz);
            WriteVector(pieceSpan, PhysicsMotionRead.GravityOffset, b.Gx, b.Gy, b.Gz);
        }
    }

    /// <summary>Every declared compliant surface (Story 10.5, CAT-065 §2) as a massless unilateral spring-damper bed on its static frame,
    /// applied once per substep as an external force beside gravity. The bed meets each dynamic body at its four support samples
    /// (<see cref="SampleMembrane"/>), each with its own committed contact state and offset (<see cref="UpdateCompliantContacts"/>). A
    /// sample bears the bed when it is <see cref="CompliantSampleState.OnTop"/>, over the bed (<see cref="OverBed"/>) and below the rest
    /// plane, a purely geometric condition read from the substep's starting pose, and then carries a quarter of the bed:
    /// Fᵢ = max(0, (k/4)·(dᵢ − oᵢ) − (c/4)·vᵢ) along n, with dᵢ = clamp(rest − heightᵢ, 0, stroke), oᵢ the sample's committed offset
    /// (the depth the bed was not paid for), vᵢ the normal velocity there and c = 2ζ√(k·m). Outside and Under samples get nothing; the
    /// frame carries them. The bed never pulls, and its spring part is the gradient of Σ½(k/4)·(dᵢ − oᵢ)², so it returns only compression
    /// it took while bearing that sample. All four samples are read before any is applied.</summary>
    private void ApplyCompliantMembranes(ReadOnlySpan<byte> span, PhysicsWorldLayout layout, float dt)
    {
        for (var s = 0; s < layout.CompliantSurfaces; s++)
        {
            var membrane = ReadMembrane(span, layout, s);
            var sampleTension = membrane.Tension / MembraneSamples;
            foreach (var body in _dynamicBodies)
            {
                var row = layout.CompliantEngagementOffset(s, body.DynamicIndex);
                var onTop = false;
                for (var i = 0; i < MembraneSamples; i++) onTop |= SampleState(span, row, i) == CompliantSampleState.OnTop;
                if (!onTop || !SampleMembrane(membrane, body)) continue;
                var sampleDamping = 2f * membrane.DampingRatio * MathF.Sqrt(membrane.Tension * body.Mass) / MembraneSamples;
                for (var i = 0; i < MembraneSamples; i++)
                {
                    _membraneImpulses[i] = 0f;
                    var support = _membraneSamples[i];
                    var local = support - membrane.Origin;
                    var depth = Depth(membrane, Vector3.Dot(local, membrane.Normal));
                    if (SampleState(span, row, i) != CompliantSampleState.OnTop || depth <= 0f || !OverBed(membrane, local)) continue;
                    var offset = ReadF32(span, row + PhysicsGpuAbi.CompliantSampleOffsetOffset + 4 * i);
                    var angular = Vector3.Cross(support - body.CentreOfMass, membrane.Normal);
                    var velocity = ConstraintBodyImpulse.ConstraintVelocity(body, membrane.Normal, angular);
                    _membraneImpulses[i] = MathF.Max(0f, sampleTension * (depth - offset) - sampleDamping * velocity) * dt;
                    _membraneAxes[i] = angular;
                }
                for (var i = 0; i < MembraneSamples; i++)
                    if (_membraneImpulses[i] > 0f) ConstraintBodyImpulse.ApplyBodyImpulse(body, membrane.Normal, _membraneAxes[i], _membraneImpulses[i]);
            }
        }
    }

    /// <summary>Each support sample's committed contact state and offset on each compliant surface, from the substep's resulting pose
    /// (CAT-065 §2), and each surface's committed deflection; the next substep's membrane force reads them at that same pose. One uniform
    /// rule per (surface, dynamic body, sample direction i), with overᵢ (<see cref="OverBed"/>) and dᵢ = clamp(rest − heightᵢ, 0, stroke):
    /// <list type="bullet">
    /// <item>not overᵢ: Outside, oᵢ = 0;</item>
    /// <item>Outside and overᵢ: OnTop with oᵢ = dᵢ when its unclamped depth max(0, rest − heightᵢ) is at most the surface's declared
    /// arrival depth a, otherwise Under, oᵢ = 0 (classifying on the unclamped depth keeps a deep entry Under whatever the stroke);</item>
    /// <item>Under and overᵢ: OnTop with oᵢ = 0 once dᵢ = 0 (it rose to the plane), otherwise Under;</item>
    /// <item>OnTop and overᵢ: OnTop, oᵢ = min(oᵢ, dᵢ).</item>
    /// </list>
    /// It keeps three invariants: (1) a sample that reaches the footprint on top (from above the plane, or crossing its edge no deeper than
    /// the arrival depth) bears the bed while it stays over it; (2) a sample that enters the footprint deeper than the arrival depth is
    /// never pushed until it rises to the plane; (3) the bed never returns more energy than it took, because a sample starts bearing with
    /// its offset at its entry depth and the offset only shrinks; the one exception is a support-direction jump between body features
    /// (at most about ε × the body's width), which can carry a sample's offset to a deeper point and so store up to that much unpaid
    /// compression. The deflection is the deepest dᵢ among OnTop over-bed samples, zero
    /// when there is none. Zeroed rows (Run and Reset) are Outside and classify themselves on the first update.</summary>
    private void UpdateCompliantContacts(Span<byte> span, PhysicsWorldLayout layout)
    {
        for (var s = 0; s < layout.CompliantSurfaces; s++)
        {
            var membrane = ReadMembrane(span, layout, s);
            var deflection = 0f;
            foreach (var body in _dynamicBodies)
            {
                var row = layout.CompliantEngagementOffset(s, body.DynamicIndex);
                var sampled = SampleMembrane(membrane, body);
                for (var i = 0; i < MembraneSamples; i++)
                {
                    var offsetAt = row + PhysicsGpuAbi.CompliantSampleOffsetOffset + 4 * i;
                    var (state, offset) = (CompliantSampleState.Outside, 0f);
                    var local = _membraneSamples[i] - membrane.Origin;
                    if (sampled && OverBed(membrane, local))
                    {
                        var height = Vector3.Dot(local, membrane.Normal);
                        var depth = Depth(membrane, height);
                        var below = MathF.Max(0f, membrane.Rest - height);
                        (CompliantSampleState State, float Offset) next = SampleState(span, row, i) switch
                        {
                            CompliantSampleState.Outside => below <= membrane.ArrivalDepth ? (CompliantSampleState.OnTop, depth) : (CompliantSampleState.Under, 0f),
                            CompliantSampleState.Under => depth > 0f ? (CompliantSampleState.Under, 0f) : (CompliantSampleState.OnTop, 0f),
                            CompliantSampleState.OnTop => (CompliantSampleState.OnTop, MathF.Min(ReadF32(span, offsetAt), depth)),
                            _ => throw new ArgumentException("Undefined compliant sample state.")
                        };
                        (state, offset) = next;
                        if (next.State == CompliantSampleState.OnTop) deflection = MathF.Max(deflection, depth);
                    }
                    WriteU32(span, row + PhysicsGpuAbi.CompliantSampleStateOffset + 4 * i, (uint)state);
                    WriteF32(span, offsetAt, offset);
                }
            }
            WriteF32(span, layout.CompliantSurfaceOffset(s) + PhysicsGpuAbi.CompliantSurfaceDeflectionOffset, deflection);
        }
    }

    /// <summary>Sample <paramref name="i"/>'s committed contact state in the engagement row at <paramref name="row"/>.</summary>
    private static CompliantSampleState SampleState(ReadOnlySpan<byte> span, int row, int i) =>
        (CompliantSampleState)ReadU32(span, row + PhysicsGpuAbi.CompliantSampleStateOffset + 4 * i);

    /// <summary>A sample's depth below the rest plane at height <paramref name="height"/> along n, within the stroke.</summary>
    private static float Depth(in Membrane membrane, float height) => Math.Clamp(membrane.Rest - height, 0f, membrane.Stroke);

    /// <summary>Whether a sample, at <paramref name="local"/> from the surface origin, lies over the bed: within the half-extents plus
    /// the footprint edge (<see cref="FootprintEdge"/>) on both in-plane axes.</summary>
    private static bool OverBed(in Membrane membrane, Vector3 local) =>
        MathF.Abs(Vector3.Dot(local, membrane.Right)) <= membrane.HalfX + FootprintEdge &&
        MathF.Abs(Vector3.Dot(local, membrane.Forward)) <= membrane.HalfZ + FootprintEdge;

    /// <summary>One compliant surface in world space: its frame's position and rotated basis (normal n = R·Y), its declaration, and
    /// the four diagonal sample directions −n ± ε·right ± ε·forward.</summary>
    private readonly record struct Membrane(Vector3 Origin, Vector3 Normal, Vector3 Right, Vector3 Forward,
        float HalfX, float HalfZ, float Rest, float Stroke, float ArrivalDepth, float Tension, float DampingRatio);

    private Membrane ReadMembrane(ReadOnlySpan<byte> span, PhysicsWorldLayout layout, int surface)
    {
        var offset = layout.CompliantSurfaceOffset(surface);
        var frame = _allBodies[(int)ReadU32(span, offset + PhysicsGpuAbi.CompliantSurfaceBodyOffset)];
        return new Membrane(frame.Position,
            WorkshopCollisionKernels.RotateVector(frame.Rotation, Vector3.UnitY),
            WorkshopCollisionKernels.RotateVector(frame.Rotation, Vector3.UnitX),
            WorkshopCollisionKernels.RotateVector(frame.Rotation, Vector3.UnitZ),
            ReadF32(span, offset + PhysicsGpuAbi.CompliantSurfaceHalfXOffset),
            ReadF32(span, offset + PhysicsGpuAbi.CompliantSurfaceHalfZOffset),
            ReadF32(span, offset + PhysicsGpuAbi.CompliantSurfaceRestHeightOffset),
            ReadF32(span, offset + PhysicsGpuAbi.CompliantSurfaceMaxStrokeOffset),
            ReadF32(span, offset + PhysicsGpuAbi.CompliantSurfaceArrivalDepthOffset),
            ReadF32(span, offset + PhysicsGpuAbi.CompliantSurfaceTensionOffset),
            ReadF32(span, offset + PhysicsGpuAbi.CompliantSurfaceDampingRatioOffset));
    }

    /// <summary>The body's support points in the current membrane's four diagonal sample directions, into the sample scratch. The
    /// diagonals give an axis-aligned box all four bottom corners (a box resolves a zero direction component to +H), a flat face its
    /// extreme vertices or rim points and a sphere four points within r·ε√2 of its bottom, so a flat payload rests flat. False for a
    /// body with no collider.</summary>
    private bool SampleMembrane(in Membrane membrane, SimBody body)
    {
        for (var i = 0; i < MembraneSamples; i++)
        {
            var right = (i & 1) == 0 ? membrane.Right : -membrane.Right;
            var forward = (i & 2) == 0 ? membrane.Forward : -membrane.Forward;
            var direction = Vector3.Normalize(-membrane.Normal + MembraneSampleSpread * (right + forward));
            var reach = float.NegativeInfinity;
            foreach (var child in _colliders)
            {
                if (child.Body != body) continue;
                WorkshopCollisionKernels.RefreshCollider(child);
                var point = WorkshopCollisionKernels.SupportPoint(child, direction);
                var along = Vector3.Dot(point, direction);
                if (along > reach) { reach = along; _membraneSamples[i] = point; }
            }
            if (reach == float.NegativeInfinity) return false;
        }
        return true;
    }

    /// <summary>The bed's support samples per body, and the tilt of each sample direction from −n along each in-plane axis (small, so
    /// a sphere's samples sit close to its bottom, while a flat face still yields its extreme vertices).</summary>
    private const int MembraneSamples = 4;
    private const float MembraneSampleSpread = 1f / 64f;
    /// <summary>The footprint edge e, 1 cm: a sample is over the bed within the half-extents plus e on both in-plane axes. It is a flush-
    /// contact tolerance, independent of any frame's dimensions: it covers a load pressed flush against a frame face, whose edge samples
    /// sit a fraction of a millimetre past the footprint (the contact slop). It is not widened, so a sample on a frame top beyond the
    /// footprint edge does not count as over the bed. The arrival depth, which a frame sizes, is declared per surface.</summary>
    private const float FootprintEdge = 1e-2f;
    private readonly Vector3[] _membraneSamples = new Vector3[MembraneSamples];
    private readonly Vector3[] _membraneAxes = new Vector3[MembraneSamples];
    private readonly float[] _membraneImpulses = new float[MembraneSamples];
}
