using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed class WorkshopPhysicsEngine
{
    private readonly Dictionary<ulong, float> _contactCache = new();
    private readonly Dictionary<FrictionPatchKey, FrictionWarmStart> _frictionCache = new();
    private readonly WorkshopDynamicBvh _bvh = new();
    // Story 9R.4: one substep's static-side contact records and the bodies a contact-load row already counted (caller-owned
    // scratch, so ContactLoadSensorTracker allocates nothing per substep).
    private readonly List<ContactPointRecord> _loadContacts = new(128);
    private GpuBodyId[] _loadCounted = new GpuBodyId[128];

    private readonly List<SimBody> _staticBodies = new(64);
    private readonly List<SimBody> _dynamicBodies = new(64);
    // The restitution pass's touched bodies and its per-substep stamp (WorkshopConstraintSolver.RestitutionPass).
    private readonly List<SimBody> _restitutionBodies = new(64);
    private int _restitutionStamp;
    private readonly Dictionary<int, SimBody> _allBodies = new(64);
    private readonly List<SimCollider> _colliders = new(64);
    private readonly List<SimJoint> _joints = new(32);
    private readonly List<(SimCollider ColA, SimCollider ColB)> _candidatePairs = new(64);
    private readonly List<ContactPoint> _contacts = new(128);
    // Scratch for the speculative sphere-contact touch probe (WorkshopCollisionKernels.AdvanceSpeculativeSphereContact).
    private readonly SimCollider _probeCollider = new();
    private readonly List<ContactPoint> _probeContacts = new(4);
    private readonly List<ContactManifold> _manifolds = new(32);
    private readonly List<int> _storeIndices = new(8);
    private readonly List<float> _balances = new(8);
    private readonly List<float> _capacities = new(8);
    private readonly List<float> _credits = new(8);
    // Story 9R.5b: per-tick working rows sized from the world's own populations, grown only when a world needs more (per Run).
    private ContactLoadSensorDeclaration[] _loadDeclarations = [];
    private int[] _loadFrames = [];
    private float[] _loadMass = [];
    private float[] _elecBefore = [], _elecLimits = [], _elecAfter = [];

    /// <summary>A cleared span of exactly <paramref name="count"/> rows, growing the owned array only when it is too short.</summary>
    private static Span<T> Scratch<T>(ref T[] array, int count)
    {
        if (array.Length < count) array = new T[count];
        var span = array.AsSpan(0, count); span.Clear();
        return span;
    }

    private readonly List<SimBody> _bodyPool = new(64);
    private readonly List<SimCollider> _colliderPool = new(64);
    private readonly List<SimJoint> _jointPool = new(32);
    private readonly List<ContactManifold> _manifoldPool = new(32);

    public void Reset()
    {
        _contactCache.Clear();
        _frictionCache.Clear();
        _loadContacts.Clear();
        _bvh.Clear();
        _staticBodies.Clear();
        _dynamicBodies.Clear();
        _allBodies.Clear();
        _colliders.Clear();
        _joints.Clear();
        _candidatePairs.Clear();
        _contacts.Clear();
        _manifolds.Clear();
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

    private static void EmitContactEvents(Span<byte> span, PhysicsWorldLayout layout, SimCollider colStatic, SimBody bDyn, float approach, Vector3 normalTowardDyn, ContactPoint contact, int substep, uint sourceOrdinal)
    {
        var bStatic = colStatic.Body;
        for (var t = 0; t < layout.Triggers; t++)
        {
            var tOffset = layout.TriggerOffset(t);
            var owner = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerOwnerOffset, 4));
            if (owner != bStatic.BodySlot) continue;
            var kind = (BodyTargetKind)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerTargetKindOffset, 4));
            var named = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerNamedTargetOffset, 4));
            if (kind == BodyTargetKind.NamedBody && named != bDyn.BodySlot) continue;
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
            if (kind == BodyTargetKind.NamedBody && named != bDyn.BodySlot) continue;
            var threshold = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkAdmissionSpeedOffset, 4));
            if (approach < threshold) continue;

            for (var o = 0; o < layout.WorkOccurrences; o++)
            {
                var oOffset = layout.WorkOccurrenceOffset(o);
                var occWork = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceWorkOffset, 4));
                var target = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceTargetOffset, 8));
                if (occWork != w || target != ((ulong)bDyn.BodyIdHigh << 32 | bDyn.BodyIdLow)) continue;

                var prevSeq = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceSequenceOffset, 4));
                var prevOrd = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceOrdinalOffset, 4));
                var cooldown = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkCooldownOffset, 4));
                var curOrd = sourceOrdinal + (uint)substep + 1;
                var curCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkOccurrenceCountOffset, 4));
                var previousInCommit = prevOrd > sourceOrdinal;

                if (!previousInCommit && (prevSeq == 0 || curOrd >= prevOrd + cooldown))
                {
                    var newCount = curCount + 1;
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkOccurrenceCountOffset, 4), newCount);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceColliderOffset, 4), checked((uint)colStatic.Index));
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceSequenceOffset, 4), newCount);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceOrdinalOffset, 4), curOrd);
                    WriteF32(span, oOffset + PhysicsGpuAbi.WorkOccurrencePhaseOffset, 0f);
                    BinaryPrimitives.WriteSingleLittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceApproachOffset, 4), approach);

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
                    BinaryPrimitives.WriteSingleLittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceDebitOffset, 4), response.Debit);
                    span[oOffset + PhysicsGpuAbi.WorkOccurrenceEffectOffset] = response.Debit > 0 ? (byte)1 : (byte)0;
                    span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrencePaddingOffset, PhysicsGpuAbi.WorkOccurrenceBytes - PhysicsGpuAbi.WorkOccurrencePaddingOffset).Clear();

                    var isA = contact.A == bDyn;
                    var r = isA ? new Vector3(contact.RAx, contact.RAy, contact.RAz) : new Vector3(contact.RBx, contact.RBy, contact.RBz);
                    var imp = new Vector3(response.Impulse.GetElement(0), response.Impulse.GetElement(1), response.Impulse.GetElement(2));
                    WorkshopCollisionKernels.ApplyImpulse(bDyn, imp, r);
                    WorkshopConstraintSolver.SettleVelocity(bDyn);
                }
                break;
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
        if (_loadCounted.Length < _loadContacts.Count) _loadCounted = new GpuBodyId[_loadContacts.Count * 2];
        var records = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_loadContacts);
        for (var row = 0; row < rows.Length; row++)
        {
            var frame = _allBodies[frames[row]];
            maximum[row] = MathF.Max(maximum[row], ContactLoadSensorTracker.Measure(rows[row], frame.Position, frame.Rotation, records, _loadCounted));
        }

    }

    /// <summary>One step from the committed world. Besides the world bytes it reads only the explicit tick inputs: per-source enables
    /// and per-prismatic switched-supply state (compiled routes × committed switch contacts). No authoring or host state is read.</summary>
    public void Advance(byte[] source, byte[] candidate, PhysicsTickInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        _ = PhysicsGpuAbi.ReadFailure(source);
        if (source.Length != PhysicsGpuAbi.ByteLength(source) || candidate.Length != source.Length)
            throw new ArgumentException("The candidate world must have its committed world's exact length.");
        Array.Copy(source, candidate, source.Length);
        var span = candidate.AsSpan();
        // Story 9R.5b: every table offset is this world's own (its header's populations); no table has a fixed capacity.
        var layout = PhysicsGpuAbi.Layout(span);

        // Motion is a closed ABI discriminant. Refuse the entire candidate before clocks,
        // bodies or solver state advance; every downstream motion value is typed.
        for (var slot = 0; slot < layout.Bodies; slot++)
        {
            var motion = (RigidMotionKind)ReadU32(span, layout.BodyOffset(slot) + PhysicsGpuAbi.BodyMotionOffset);
            if (Enum.IsDefined(motion)) continue;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderStatusOffset, 4), (uint)PhysicsCandidateStatus.Invalid);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderFailureOffset, 4), (uint)PhysicsFailure.InvalidDeclaration);
            return;
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
            return;
        }

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderStatusOffset, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderFailureOffset, 4), 0);

        var currentTick = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(PhysicsGpuAbi.HeaderTickOffset, 8));
        BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(PhysicsGpuAbi.HeaderTickOffset, 8), currentTick + 1);

        var sourceOrdinal = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderSourceOrdinalOffset, 4));
        for (var work = 0; work < layout.ContactWorks; work++)
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(layout.ContactWorkOffset(work) + PhysicsGpuAbi.ContactWorkSuppliedOffset, 4), 0f);

        var supplyCount = layout.ElectricalSources;
        inputs.Validate(supplyCount, layout.Prismatics);
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
        _allBodies.Clear();

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
            b.Motion = motion;
            b.Collider = null;

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

            if (body.Motion == RigidMotionKind.Dynamic)
            {
                body.Collider = collider;
                body.SupportExtent = shapeKind switch
                {
                    ShapeKind.Sphere => radius,
                    ShapeKind.Box => hx * MathF.Abs(collider.U0.Y) + hy * MathF.Abs(collider.U1.Y) + hz * MathF.Abs(collider.U2.Y),
                    ShapeKind.Cylinder => WorkshopCollisionKernels.CylinderProjection(collider, Vector3.UnitY),
                    // A plane and the hollow kinds are never dynamic (admission).
                    ShapeKind.Plane or ShapeKind.Tube or ShapeKind.Torus or ShapeKind.Frustum => 0f,
                    _ => 0f
                };
            }
        }

        _joints.Clear();
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

        var dt = WorkshopConstraintSolver.SubstepSeconds;
        var softCoeffs = SoftCoefficients.MakeSoft(WorkshopConstraintSolver.ContactHertz, WorkshopConstraintSolver.ContactDampingRatio, dt);

        _bvh.Clear();
        var tickTime = dt * steps;
        const float velocitySlop = 0.05f;

        foreach (var c in _colliders)
        {
            var b = c.Body;
            var isDyn = b.Motion == RigidMotionKind.Dynamic;
            var spin = isDyn ? MathF.Sqrt(b.Wx * b.Wx + b.Wy * b.Wy + b.Wz * b.Wz) * WorkshopCollisionKernels.ColliderExtent(c) * tickTime : 0f;
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
            _candidatePairs.Add((colA, colB));
        });

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

        for (var s = 0; s < steps; s++)
        {
            // Box2D v3 prepares contacts before integrating velocities: the restitution approach is the substep-start velocity.
            foreach (var b in _dynamicBodies)
            {
                b.V0 = b.LinearVelocity;
                b.W0 = b.AngularVelocity;
            }

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

            var guideCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderGuideCountOffset, 4));
            for (var g = 0; g < guideCount; g++)
            {
                var guideOffset = layout.GuideOffset(g);
                var frameSlot = (int)ReadU32(span, guideOffset + PhysicsGpuAbi.GuideFrameOffset);
                var targetSlot = (int)ReadU32(span, guideOffset + PhysicsGpuAbi.GuideTargetOffset);
                var minimum = ReadVector(span, guideOffset + PhysicsGpuAbi.GuideMinimumOffset);
                var maximum = ReadVector(span, guideOffset + PhysicsGpuAbi.GuideMaximumOffset);
                var (minX, minY, minZ) = (minimum.X, minimum.Y, minimum.Z);
                var (maxX, maxY, maxZ) = (maximum.X, maximum.Y, maximum.Z);
                var maxAcc = ReadF32(span, guideOffset + PhysicsGpuAbi.GuideMaximumAccelerationOffset);
                var supportHeight = ReadF32(span, guideOffset + PhysicsGpuAbi.GuideSupportHeightOffset);
                var supportMargin = ReadF32(span, guideOffset + PhysicsGpuAbi.GuideSupportMarginOffset);

                if (maxAcc <= 0f) continue;
                if (!_allBodies.TryGetValue(targetSlot, out var targetBody) || !_allBodies.TryGetValue(frameSlot, out var frameBody)) continue;

                var rel = targetBody.Position - frameBody.Position;
                var loc = WorkshopCollisionKernels.InvRotateVector(frameBody.Rotation, rel);
                var locV = WorkshopCollisionKernels.InvRotateVector(frameBody.Rotation, targetBody.LinearVelocity);

                var inside = loc.X >= minX && loc.X <= maxX &&
                             loc.Y >= minY && loc.Y <= maxY &&
                             loc.Z >= minZ && loc.Z <= maxZ &&
                             loc.Y - targetBody.SupportExtent >= supportHeight - supportMargin &&
                             locV.Y <= 0f;

                if (inside)
                {
                    var flatLen = MathF.Sqrt(loc.X * loc.X + loc.Z * loc.Z);
                    var accScale = -maxAcc / MathF.Max(1.0f, flatLen);
                    var aloc = new Vector3(loc.X * accScale, 0f, loc.Z * accScale);
                    var aWorld = WorkshopCollisionKernels.RotateVector(frameBody.Rotation, aloc);
                    targetBody.Vx += aWorld.X * dt;
                    targetBody.Vz += aWorld.Z * dt;
                }
            }

            // Each declared drive follows its own compiled route (SimJoint.SupplyClosed), closed from committed state, never a timer.
            PrismaticConstraintSolver.ApplySuppliedDrives(_joints, dt);

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
                    bA.AngularVelocity.Length() * WorkshopCollisionKernels.ColliderExtent(colA) +
                    bB.AngularVelocity.Length() * WorkshopCollisionKernels.ColliderExtent(colB);

                var first = _contacts.Count;
                var margin = WorkshopConstraintSolver.SpeculativeSlop + relSpeed * dt;
                WorkshopCollisionKernels.GenerateManifold(colA, colB, margin, _contacts);
                WorkshopCollisionKernels.AdvanceSpeculativeSphereContact(colA, colB, margin, dt, _contacts, first, _probeCollider, _probeContacts);
                if (_contacts.Count > first)
                {
                    while (_manifoldPool.Count <= _manifolds.Count) _manifoldPool.Add(new ContactManifold());
                    var manifold = _manifoldPool[_manifolds.Count];
                    manifold.Points.Clear();
                    manifold.Rolling = 0;
                    manifold.Roll1 = default;
                    manifold.Roll2 = default;
                    manifold.RollingMass1 = 0;
                    manifold.RollingMass2 = 0;
                    manifold.LambdaR1 = 0;
                    manifold.LambdaR2 = 0;
                    for (var i = first; i < _contacts.Count; i++) manifold.Points.Add(_contacts[i]);
                    _manifolds.Add(manifold);
                }
            }

            WorkshopCollisionKernels.RemoveInternalHollowEdges(_manifolds, _contacts);
            foreach (var c in _contacts) WorkshopConstraintSolver.PrepareContact(c, _contactCache);
            foreach (var m in _manifolds)
            {
                WorkshopConstraintSolver.PrepareRolling(m);
                WorkshopFrictionSolver.Prepare(m, _frictionCache);
            }
            foreach (var b in _dynamicBodies)
            {
                b.Cm0 = b.CentreOfMass;
                b.Q0 = b.Rotation;
            }

            foreach (var joint in _joints)
            {
                PrismaticConstraintSolver.PrepareJointRows(joint, dt);
                for (var row = 0; row < 8; row++)
                    PrismaticConstraintSolver.ApplyConstraintImpulse(joint, joint.Rows[row], joint.Impulses[row]);
            }
            foreach (var c in _contacts) WorkshopConstraintSolver.WarmStartContact(c);
            foreach (var m in _manifolds) WorkshopFrictionSolver.WarmStart(m);

            for (var iteration = 0; iteration < WorkshopConstraintSolver.SolverIterations; iteration++)
            {
                PrismaticConstraintSolver.SweepConstraints(_joints, true);
                WorkshopConstraintSolver.SweepManifolds(_manifolds, ContactPass.Solve, softCoeffs, dt);
            }

            foreach (var b in _dynamicBodies)
            {
                WorkshopConstraintSolver.ClampVelocity(b);
                var prevCellX = b.CellX; var prevCellY = b.CellY; var prevCellZ = b.CellZ;
                var prevLocX = b.LocalX; var prevLocY = b.LocalY; var prevLocZ = b.LocalZ;
                var prevQ = b.Rotation; var prevV = b.LinearVelocity; var prevW = b.AngularVelocity;

                var nq = b.Rotation;
                var wLen = b.AngularVelocity.Length();
                if (wLen > 1e-9f)
                {
                    var angle = wLen * dt;
                    var sAngle = MathF.Sin(angle * 0.5f) / wLen;
                    var cAngle = MathF.Cos(angle * 0.5f);
                    var dq = new Quaternion(b.Wx * sAngle, b.Wy * sAngle, b.Wz * sAngle, cAngle);
                    nq = Quaternion.Normalize(Quaternion.Multiply(dq, b.Rotation));
                }

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
                    b.Vx = b.Vy = b.Vz = b.Wx = b.Wy = b.Wz = 0f;
                }
                WorkshopCollisionKernels.UpdateDynamicFrame(b);
            }

            foreach (var joint in _joints)
            {
                var softRowError = joint.Rows[(int)PrismaticRowKind.Spring].Error;
                PrismaticConstraintSolver.PrepareJointRows(joint, dt);
                joint.Rows[(int)PrismaticRowKind.Spring].Error = softRowError;
            }

            for (var iteration = 0; iteration < WorkshopConstraintSolver.RelaxIterations; iteration++)
            {
                PrismaticConstraintSolver.SweepConstraints(_joints, false);
                WorkshopConstraintSolver.SweepManifolds(_manifolds, ContactPass.Relax, softCoeffs, dt);
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

            WorkshopConstraintSolver.RestitutionPass(_manifolds, _restitutionBodies, ++_restitutionStamp);
            // Loads are read after the restitution pass: a point that carried normal impulse presses its face even from the gap.
            if (loadRows > 0) MeasureContactLoads(loadDeclarations[..loadRows], loadFrames[..loadRows], loadMass[..loadRows]);
            foreach (var b in _dynamicBodies) WorkshopConstraintSolver.SettleVelocity(b);

            _contactCache.Clear();
            _frictionCache.Clear();
            foreach (var c in _contacts) _contactCache[c.Key] = c.LambdaN;
            foreach (var m in _manifolds) WorkshopFrictionSolver.Store(m, _frictionCache);

            // A point that carried normal impulse this substep, touching or speculative, is a contact (Box2D v3 hit events).
            foreach (var c in _contacts)
            {
                if (c.MaxImpulse <= 0f) continue;
                if (c.A.Motion == RigidMotionKind.Dynamic && c.B.Motion == RigidMotionKind.Static) EmitContactEvents(span, layout, c.ColB, c.A, -c.RelVel, c.Normal, c, s, sourceOrdinal);
                else if (c.B.Motion == RigidMotionKind.Dynamic && c.A.Motion == RigidMotionKind.Static) EmitContactEvents(span, layout, c.ColA, c.B, -c.RelVel, -c.Normal, c, s, sourceOrdinal);
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

        for (var row = 0; row < loadRows; row++)
        {
            var record = span.Slice(layout.ContactLoadOffset(row), PhysicsGpuAbi.ContactLoadSensorBytes);
            BinaryPrimitives.WriteUInt32LittleEndian(record[PhysicsGpuAbi.ContactLoadPhaseOffset..], (uint)loadDeclarations[row].PhaseOf(loadMass[row]));
            BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.ContactLoadMassOffset..], loadMass[row]);
        }

        var totalCaptured = 0u;
        var sensorCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderSensorCountOffset, 4));
        for (var i = 0; i < sensorCount; i++)
        {
            var sensorOffset = layout.SensorOffset(i);
            var frameSlot = (int)ReadU32(span, sensorOffset + PhysicsGpuAbi.SensorFrameOffset);
            var targetSlot = (int)ReadU32(span, sensorOffset + PhysicsGpuAbi.SensorTargetOffset);
            var participation = ReadU32(span, sensorOffset + PhysicsGpuAbi.SensorParticipationOffset);
            var minimum = ReadVector(span, sensorOffset + PhysicsGpuAbi.SensorMinimumOffset);
            var maximum = ReadVector(span, sensorOffset + PhysicsGpuAbi.SensorMaximumOffset);
            var (minX, minY, minZ) = (minimum.X, minimum.Y, minimum.Z);
            var (maxX, maxY, maxZ) = (maximum.X, maximum.Y, maximum.Z);
            var speedLimit = ReadF32(span, sensorOffset + PhysicsGpuAbi.SensorSpeedLimitOffset);
            var dwell = ReadF32(span, sensorOffset + PhysicsGpuAbi.SensorDwellOffset);

            var prevPhase = ReadU32(span, sensorOffset + PhysicsGpuAbi.SensorPhaseOffset);
            var prevStartOrdinal = ReadU32(span, sensorOffset + PhysicsGpuAbi.SensorStartOrdinalOffset);
            var prevStartPhase = ReadF32(span, sensorOffset + PhysicsGpuAbi.SensorStartPhaseOffset);

            _allBodies.TryGetValue(targetSlot, out var targetBody);
            _allBodies.TryGetValue(frameSlot, out var frameBody);

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

            WriteU32(span, sensorOffset + PhysicsGpuAbi.SensorPhaseOffset, phase);
            WriteU32(span, sensorOffset + PhysicsGpuAbi.SensorStartOrdinalOffset, startOrdinal);
            WriteF32(span, sensorOffset + PhysicsGpuAbi.SensorStartPhaseOffset, startPhase);
            WriteU32(span, sensorOffset + PhysicsGpuAbi.SensorOccurrenceCountOffset, occurrenceCount);
            WriteU32(span, sensorOffset + PhysicsGpuAbi.SensorEventOrdinalOffset, eventOrdinal);
            WriteF32(span, sensorOffset + PhysicsGpuAbi.SensorEventPhaseOffset, eventPhase);
            span.Slice(sensorOffset + PhysicsGpuAbi.SensorPaddingOffset, PhysicsGpuAbi.SensorBytes - PhysicsGpuAbi.SensorPaddingOffset).Clear();

            totalCaptured += occurrenceCount;
        }

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderCapturedCountOffset, 4), totalCaptured);

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
    }
}
