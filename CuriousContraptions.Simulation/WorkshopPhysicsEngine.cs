using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed class WorkshopPhysicsEngine
{
    private readonly Dictionary<ulong, (float LambdaN, float LambdaT1, float LambdaT2, float Approach)> _contactCache = new();
    private readonly WorkshopDynamicBvh _bvh = new();
    // Story 9R.4: one substep's static-side contact records and the bodies a contact-load row already counted (caller-owned
    // scratch, so ContactLoadSensorTracker allocates nothing per substep).
    private readonly List<ContactPointRecord> _loadContacts = new(128);
    private GpuBodyId[] _loadCounted = new GpuBodyId[128];

    private readonly List<SimBody> _staticBodies = new(64);
    private readonly List<SimBody> _dynamicBodies = new(64);
    private readonly Dictionary<int, SimBody> _allBodies = new(64);
    private readonly List<SimCollider> _colliders = new(64);
    private readonly List<SimJoint> _joints = new(32);
    private readonly List<(SimCollider ColA, SimCollider ColB)> _candidatePairs = new(64);
    private readonly List<ContactPoint> _contacts = new(128);
    private readonly List<ContactManifold> _manifolds = new(32);
    private readonly List<int> _storeIndices = new(8);
    private readonly List<float> _balances = new(8);
    private readonly List<float> _capacities = new(8);
    private readonly List<float> _credits = new(8);

    private readonly List<SimBody> _bodyPool = new(64);
    private readonly List<SimCollider> _colliderPool = new(64);
    private readonly List<SimJoint> _jointPool = new(32);
    private readonly List<ContactManifold> _manifoldPool = new(32);

    public void Reset()
    {
        _contactCache.Clear();
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

    /// <summary>The warm-up world's one contact-work store is a valid full 32 J store with an 8 m/s target, so warming
    /// exercises the payment path rather than an empty store (Story 9.4b).</summary>
    private static void WarmupStore(byte[] world)
    {
        var store = world.AsSpan(PhysicsGpuAbi.ContactWorksOffset, PhysicsGpuAbi.ContactWorkBytes);
        BinaryPrimitives.WriteSingleLittleEndian(store[PhysicsGpuAbi.ContactWorkTargetSpeedOffset..], 8f);
        BinaryPrimitives.WriteSingleLittleEndian(store[PhysicsGpuAbi.ContactWorkPreloadOffset..], 32f);
        BinaryPrimitives.WriteSingleLittleEndian(store[PhysicsGpuAbi.ContactWorkAvailableOffset..], 32f);
    }

    public void Warmup()
    {
        var dummyBodyA = new SimBody { Mass = 1f, InvMass = 1f, Motion = 1 };
        dummyBodyA.InvI = new Vector3(1f, 1f, 1f);
        dummyBodyA.Principal = Quaternion.Identity;
        dummyBodyA.Qx = 0f; dummyBodyA.Qy = 0f; dummyBodyA.Qz = 0f; dummyBodyA.Qw = 1f;
        dummyBodyA.Q0 = Quaternion.Identity;
        dummyBodyA.Vx = 1f; dummyBodyA.Vy = -1f; dummyBodyA.Vz = 0.5f;
        dummyBodyA.Wx = 0.5f; dummyBodyA.Wy = 0.5f; dummyBodyA.Wz = 0.5f;
        WorkshopCollisionKernels.UpdateDynamicFrame(dummyBodyA);

        var dummyBodyB = new SimBody { Mass = 1f, InvMass = 1f, Motion = 0 };
        dummyBodyB.Principal = Quaternion.Identity;
        dummyBodyB.Q0 = Quaternion.Identity;
        dummyBodyB.Qw = 1f;

        var dummyPlane = new SimCollider
        {
            Body = dummyBodyB,
            Shape = ShapeKind.Plane,
            U0 = Vector3.UnitX,
            U1 = Vector3.UnitY,
            U2 = Vector3.UnitZ,
            Restitution = 0.8f, BounceThreshold = 0.2f, Friction = 0.4f, RollingResistance = 0.05f
        };

        var dummySphere = new SimCollider
        {
            Body = dummyBodyA,
            Shape = ShapeKind.Sphere,
            Radius = 0.5f,
            Px = 0f, Py = 0.4f, Pz = 0f,
            U0 = Vector3.UnitX, U1 = Vector3.UnitY, U2 = Vector3.UnitZ,
            Restitution = 0.8f, BounceThreshold = 0.2f, Friction = 0.4f, RollingResistance = 0.05f
        };

        var dummyBoxA = new SimCollider
        {
            Body = dummyBodyA,
            Shape = ShapeKind.Box,
            Hx = 0.5f, Hy = 0.5f, Hz = 0.5f,
            Px = 0f, Py = 0.4f, Pz = 0f,
            U0 = Vector3.UnitX, U1 = Vector3.UnitY, U2 = Vector3.UnitZ,
            Restitution = 0.8f, BounceThreshold = 0.2f, Friction = 0.4f, RollingResistance = 0.05f
        };

        var dummyBoxB = new SimCollider
        {
            Body = dummyBodyB,
            Shape = ShapeKind.Box,
            Hx = 0.5f, Hy = 0.5f, Hz = 0.5f,
            Px = 0f, Py = 0f, Pz = 0f,
            U0 = Vector3.UnitX, U1 = Vector3.UnitY, U2 = Vector3.UnitZ,
            Restitution = 0.8f, BounceThreshold = 0.2f, Friction = 0.4f, RollingResistance = 0.05f
        };

        var dummySphereB = new SimCollider
        {
            Body = dummyBodyB,
            Shape = ShapeKind.Sphere,
            Radius = 0.5f,
            Px = 0f, Py = 0f, Pz = 0f,
            U0 = Vector3.UnitX, U1 = Vector3.UnitY, U2 = Vector3.UnitZ,
            Restitution = 0.8f, BounceThreshold = 0.2f, Friction = 0.4f, RollingResistance = 0.05f
        };

        var warmupContacts = new List<ContactPoint>(16);
        for (var i = 0; i < 20; i++)
        {
            warmupContacts.Clear();
            WorkshopCollisionKernels.GenerateManifold(dummySphere, dummyPlane, 0.05f, warmupContacts);
            warmupContacts.Clear();
            WorkshopCollisionKernels.GenerateManifold(dummyBoxA, dummyPlane, 0.05f, warmupContacts);
            warmupContacts.Clear();
            WorkshopCollisionKernels.GenerateManifold(dummyBoxA, dummyBoxB, 0.05f, warmupContacts);
            warmupContacts.Clear();
            WorkshopCollisionKernels.GenerateManifold(dummyBoxA, dummySphere, 0.05f, warmupContacts);
            warmupContacts.Clear();
            WorkshopCollisionKernels.GenerateManifold(dummySphere, dummySphereB, 0.05f, warmupContacts);
        }

        var soft = SoftCoefficients.MakeSoft(WorkshopConstraintSolver.ContactHertz, WorkshopConstraintSolver.ContactDampingRatio, WorkshopConstraintSolver.SubstepSeconds);
        var warmupManifolds = new List<ContactManifold>();
        if (warmupContacts.Count > 0)
        {
            var m = new ContactManifold();
            foreach (var cp in warmupContacts) m.Points.Add(cp);
            warmupManifolds.Add(m);

            for (var i = 0; i < 20; i++)
            {
                WorkshopConstraintSolver.PrepareContact(warmupContacts[0], _contactCache);
                WorkshopConstraintSolver.PrepareRolling(m);
                WorkshopConstraintSolver.WarmStartContact(warmupContacts[0]);
                WorkshopConstraintSolver.SweepManifolds(warmupManifolds, true, soft, WorkshopConstraintSolver.SubstepSeconds);
                WorkshopConstraintSolver.SweepManifolds(warmupManifolds, false, soft, WorkshopConstraintSolver.SubstepSeconds);
                warmupContacts[0].RelVel = -2f;
                warmupContacts[0].MaxImpulse = 1f;
                WorkshopConstraintSolver.ApplyRestitution(warmupContacts[0]);
                WorkshopConstraintSolver.CurrentSeparation(warmupContacts[0]);
                WorkshopConstraintSolver.SettleVelocity(dummyBodyA);
                _contactCache[warmupContacts[0].Key] = (warmupContacts[0].LambdaN, warmupContacts[0].LambdaT1, warmupContacts[0].LambdaT2, 0f);
                WorkshopConstraintSolver.PrepareContact(warmupContacts[0], _contactCache);
            }
        }

        var boxContacts = new List<ContactPoint>(16);
        WorkshopCollisionKernels.GenerateManifold(dummyBoxA, dummyBoxB, 0.05f, boxContacts);
        if (boxContacts.Count > 1)
        {
            var boxManifold = new ContactManifold();
            foreach (var cp in boxContacts) boxManifold.Points.Add(cp);
            var boxManifolds = new List<ContactManifold> { boxManifold };
            for (var i = 0; i < 20; i++)
            {
                foreach (var cp in boxContacts) WorkshopConstraintSolver.PrepareContact(cp, _contactCache);
                WorkshopConstraintSolver.SweepManifolds(boxManifolds, true, soft, WorkshopConstraintSolver.SubstepSeconds);
                WorkshopConstraintSolver.SweepManifolds(boxManifolds, false, soft, WorkshopConstraintSolver.SubstepSeconds);
            }
        }

        Span<float> elecBefore = stackalloc float[2] { 5f, 10f };
        Span<float> elecLimits = stackalloc float[2] { 20f, 20f };
        Span<float> elecAfter = stackalloc float[2];
        ElectricalSupply.Allocate(ElectricalEnable.Enabled, 50f, 10f, elecBefore, elecLimits, elecAfter);

        // Each warm-up world is sized from its own collider count: none here, two in the stepped mock below.
        var mockBuffer = new byte[PhysicsGpuAbi.ByteLength(0)];
        BinaryPrimitives.WriteUInt32LittleEndian(mockBuffer.AsSpan(PhysicsGpuAbi.HeaderTriggerCountOffset, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(mockBuffer.AsSpan(PhysicsGpuAbi.HeaderContactWorkCountOffset, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(mockBuffer.AsSpan(PhysicsGpuAbi.HeaderWorkOccurrenceCountOffset, 4), 1);
        WarmupStore(mockBuffer);
        if (warmupContacts.Count > 0)
            EmitContactEvents(mockBuffer.AsSpan(), dummyBoxB, dummyBodyA, 2.0f, Vector3.UnitY, warmupContacts[0], 0, 0);

        var dummyJoint = new SimJoint
        {
            A = dummyBodyB,
            B = dummyBodyA,
            Lower = 0f,
            Upper = 1.55f,
            Soft = SoftConstraintCoefficients.FromSpring(14f, 1f, WorkshopConstraintSolver.SubstepSeconds),
            SupplyClosed = true
        };
        var dummyJoints = new List<SimJoint> { dummyJoint };
        for (var i = 0; i < 20; i++)
        {
            PrismaticConstraintSolver.PrepareJointRows(dummyJoint, WorkshopConstraintSolver.SubstepSeconds);
            PrismaticConstraintSolver.SweepConstraints(dummyJoints, true);
            PrismaticConstraintSolver.SweepConstraints(dummyJoints, false);
            PrismaticConstraintSolver.ActuatorSliderDrive(dummyJoints, WorkshopConstraintSolver.SubstepSeconds);
        }

        const int mockColliders = 2;
        var mockSource = new byte[PhysicsGpuAbi.ByteLength(mockColliders)];
        var mockCandidate = new byte[PhysicsGpuAbi.ByteLength(mockColliders)];
        var mSpan = mockSource.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.HeaderBodyCountOffset, 4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.HeaderColliderCountOffset, 4), mockColliders);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.HeaderDynamicCountOffset, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.HeaderCadenceOffset, 4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.HeaderTriggerCountOffset, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.HeaderContactWorkCountOffset, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.HeaderWorkOccurrenceCountOffset, 4), 1);
        WarmupStore(mockSource);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.HeaderElectricalSourceCountOffset, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.HeaderElectricalBindingCountOffset, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.PrismaticsOffset, 4), 1);

        var b0 = PhysicsGpuAbi.BodiesOffset;
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(b0 + 8, 4), 0);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(b0 + 60, 4), 1f);

        var b1 = PhysicsGpuAbi.BodiesOffset + PhysicsGpuAbi.BodyBytes;
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(b1, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(b1 + 8, 4), 1);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(b1 + 36, 4), 6.4f);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(b1 + 60, 4), 1f);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(b1 + 68, 4), -1f);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(b1 + 80, 4), 1f);
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(b1 + 88, 2), (Half)1f);
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(b1 + 90, 2), (Half)0.1f);
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(b1 + 94, 2), (Half)(-9.8f));
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(b1 + 110, 2), (Half)1f);
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(b1 + 112, 2), (Half)1f);
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(b1 + 114, 2), (Half)1f);
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(b1 + 116, 2), (Half)1f);

        var c0 = PhysicsGpuAbi.CollidersOffset;
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(c0 + 8, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(c0 + 12, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(c0 + 16, 4), 2);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(c0 + 44, 4), 1f);

        var c1 = PhysicsGpuAbi.CollidersOffset + PhysicsGpuAbi.ColliderBytes;
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(c1 + 8, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(c1 + 12, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(c1 + 16, 4), 0);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(c1 + 44, 4), 1f);
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(c1 + 48, 2), (Half)0.5f);

        var mat = PhysicsGpuAbi.MaterialsOffset;
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(mat + 8, 2), (Half)0.8f);
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(mat + 10, 2), (Half)0.2f);
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(mat + 12, 2), (Half)0.4f);
        BinaryPrimitives.WriteHalfLittleEndian(mSpan.Slice(mat + 14, 2), (Half)0.05f);

        var j0 = PhysicsGpuAbi.PrismaticRecordsOffset;
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(j0 + 8, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(j0 + 12, 4), 1);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(j0 + 40, 4), 1f);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(j0 + 68, 4), 1f);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(j0 + 76, 4), 1.55f);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(j0 + 80, 4), 14f);
        BinaryPrimitives.WriteSingleLittleEndian(mSpan.Slice(j0 + 84, 4), 1f);
        BinaryPrimitives.WriteUInt32LittleEndian(mSpan.Slice(j0 + 88, 4), 1);

        // Warm-up inputs match the mock document's own tables: every source disabled, no supply route closed.
        var warmupInputs = new PhysicsTickInputs(
            new ElectricalEnable[BinaryPrimitives.ReadUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.HeaderElectricalSourceCountOffset, 4))],
            new bool[BinaryPrimitives.ReadUInt32LittleEndian(mSpan.Slice(PhysicsGpuAbi.PrismaticsOffset, 4))]);
        for (var step = 0; step < 5; step++)
        {
            Advance(mockSource, mockCandidate, warmupInputs);
            Array.Copy(mockCandidate, mockSource, mockSource.Length);
        }

        Reset();
    }

    private static float ReadF16(ReadOnlySpan<byte> span, int offset) =>
        (float)BinaryPrimitives.ReadHalfLittleEndian(span.Slice(offset, 2));

    private static void WriteF16(Span<byte> span, int offset, float value) =>
        BinaryPrimitives.WriteHalfLittleEndian(span.Slice(offset, 2), (Half)value);

    private static float ReadPrincipalInertia(ReadOnlySpan<byte> span, int offset)
    {
        var mantissa = ReadF16(span, offset);
        var exponent = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset + 4, 4));
        return mantissa * MathF.Pow(2f, exponent);
    }

    private static void EmitContactEvents(Span<byte> span, SimCollider colStatic, SimBody bDyn, float approach, Vector3 normalTowardDyn, ContactPoint contact, int substep, uint sourceOrdinal)
    {
        var bStatic = colStatic.Body;
        var tCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderTriggerCountOffset, 4));
        for (var t = 0; t < tCount; t++)
        {
            var tOffset = PhysicsGpuAbi.TriggersOffset + t * PhysicsGpuAbi.TriggerBytes;
            var owner = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerOwnerOffset, 4));
            if (owner != bStatic.BodySlot) continue;
            var kind = (BodyTargetKind)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerTargetKindOffset, 4));
            var named = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerNamedTargetOffset, 4));
            if (kind == BodyTargetKind.NamedBody && named != bDyn.BodySlot) continue;
            var threshold = ReadF16(span, tOffset + PhysicsGpuAbi.TriggerThresholdOffset);
            if (approach >= threshold)
            {
                var curCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerCountOffset, 4));
                if (curCount == 0)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerCountOffset, 4), 1);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerColliderOffset, 4), (uint)colStatic.Index);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerOrdinalOffset, 4), sourceOrdinal + (uint)substep + 1);
                    WriteF16(span, tOffset + PhysicsGpuAbi.TriggerPhaseOffset, 0f);
                    WriteF16(span, tOffset + PhysicsGpuAbi.TriggerApproachOffset, approach);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(tOffset + PhysicsGpuAbi.TriggerBodyOffset, 4), (uint)bDyn.BodySlot);
                }
            }
        }

        var cwCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderContactWorkCountOffset, 4));
        var workOccCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderWorkOccurrenceCountOffset, 4));
        for (var w = 0; w < cwCount; w++)
        {
            var wOffset = PhysicsGpuAbi.ContactWorksOffset + w * PhysicsGpuAbi.ContactWorkBytes;
            var owner = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkOwnerOffset, 4));
            if (owner != bStatic.BodySlot) continue;
            var kind = (BodyTargetKind)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkTargetKindOffset, 4));
            var named = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkNamedTargetOffset, 4));
            if (kind == BodyTargetKind.NamedBody && named != bDyn.BodySlot) continue;
            var threshold = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkAdmissionSpeedOffset, 4));
            if (approach < threshold) continue;

            for (var o = 0; o < workOccCount; o++)
            {
                var oOffset = PhysicsGpuAbi.WorkOccurrencesOffset + o * PhysicsGpuAbi.WorkOccurrenceBytes;
                var occWork = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceWorkOffset, 2));
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
                    BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceColliderOffset, 2), (ushort)colStatic.Index);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceSequenceOffset, 4), newCount);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrenceOrdinalOffset, 4), curOrd);
                    WriteF16(span, oOffset + PhysicsGpuAbi.WorkOccurrencePhaseOffset, 0f);
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
                    span.Slice(oOffset + PhysicsGpuAbi.WorkOccurrencePaddingOffset, 1).Clear();

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
            if (c.A.Motion == 0 && c.B.Motion == 1)
                _loadContacts.Add(new(new(c.A.Id), new(c.ColA.Id), new(c.B.Id), c.B.Mass, c.Point, -c.Normal, c.Gap));
            else if (c.B.Motion == 0 && c.A.Motion == 1)
                _loadContacts.Add(new(new(c.B.Id), new(c.ColB.Id), new(c.A.Id), c.A.Mass, c.Point, c.Normal, c.Gap));
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
        if (source.Length != PhysicsGpuAbi.ByteLength(source) || candidate.Length != source.Length)
            throw new ArgumentException("The candidate world must have its committed world's exact length.");
        Array.Copy(source, candidate, source.Length);
        var span = candidate.AsSpan();
        Span<float> elecBefore = stackalloc float[ElectricalSupply.LoadCapacity];
        Span<float> elecLimits = stackalloc float[ElectricalSupply.LoadCapacity];
        Span<float> elecAfter = stackalloc float[ElectricalSupply.LoadCapacity];

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderStatusOffset, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderFailureOffset, 4), 0);

        var currentTick = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(PhysicsGpuAbi.HeaderTickOffset, 8));
        BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(PhysicsGpuAbi.HeaderTickOffset, 8), currentTick + 1);

        var sourceOrdinal = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderSourceOrdinalOffset, 4));
        var contactWorkCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderContactWorkCountOffset, 4));
        for (var work = 0; work < contactWorkCount; work++)
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(PhysicsGpuAbi.ContactWorksOffset + work * PhysicsGpuAbi.ContactWorkBytes + PhysicsGpuAbi.ContactWorkSuppliedOffset, 4), 0f);

        var supplyCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderElectricalSourceCountOffset, 4));
        inputs.Validate((int)supplyCount, (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.PrismaticsOffset, 4)));
        for (var supply = 0; supply < supplyCount; supply++)
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(PhysicsGpuAbi.ElectricalSourcesOffset + supply * PhysicsGpuAbi.ElectricalSourceBytes + 40, 4), 0f);

        var cadence = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderCadenceOffset, 4));
        var steps = cadence == 1 ? 8 : (cadence == 2 ? 4 : 2);
        var nextOrdinal = sourceOrdinal + (uint)steps;
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderSourceOrdinalOffset, 4), nextOrdinal);

        var bodyCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderBodyCountOffset, 4));
        var colliderCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderColliderCountOffset, 4));
        var dynamicCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderDynamicCountOffset, 4));

        var motionOffset = PhysicsGpuAbi.MotionOffset;
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(motionOffset, 4), (uint)(dynamicCount * (uint)steps));
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(motionOffset + 4, 4), (uint)steps);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(motionOffset + 8, 4), sourceOrdinal);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(motionOffset + 12, 4), nextOrdinal);

        var triggerCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderTriggerCountOffset, 4));
        for (var t = 0; t < triggerCount; t++)
        {
            var tOffset = PhysicsGpuAbi.TriggersOffset + t * 64;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(tOffset + 32, 4), 0);
            span.Slice(tOffset + 36, 16).Clear();
        }

        _staticBodies.Clear();
        _dynamicBodies.Clear();
        _allBodies.Clear();

        for (var slot = 0; slot < bodyCount; slot++)
        {
            var bodyOffset = PhysicsGpuAbi.BodiesOffset + slot * PhysicsGpuAbi.BodyBytes;
            var motion = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(bodyOffset + 8, 4));
            var bodyIdLow = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(bodyOffset, 4));
            var bodyIdHigh = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(bodyOffset + 4, 4));
            var id = ((ulong)bodyIdHigh << 32) | bodyIdLow;

            var cellX = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyCellOffset, 4));
            var cellY = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyCellOffset + 4, 4));
            var cellZ = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(bodyOffset + 24, 4));
            var localX = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 32, 4));
            var localY = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 36, 4));
            var localZ = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 40, 4));
            var qx = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 48, 4));
            var qy = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 52, 4));
            var qz = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 56, 4));
            var qw = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 60, 4));

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

            if (motion == 0)
            {
                _staticBodies.Add(b);
                _allBodies[slot] = b;
            }
            else
            {
                var vx = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyVelocityOffset, 4));
                var vy = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyVelocityOffset + 4, 4));
                var vz = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyVelocityOffset + 8, 4));
                var wx = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyAngularVelocityOffset, 4));
                var wy = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyAngularVelocityOffset + 4, 4));
                var wz = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyAngularVelocityOffset + 8, 4));

                var mass = ReadF16(span, bodyOffset + PhysicsGpuAbi.BodyMassOffset);
                var drag = ReadF16(span, bodyOffset + PhysicsGpuAbi.BodyDragOffset);
                var dragBits = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(bodyOffset + PhysicsGpuAbi.BodyDragOffset, 2));

                var gx = ReadF16(span, bodyOffset + PhysicsGpuAbi.BodyGravityOffset);
                var gy = ReadF16(span, bodyOffset + PhysicsGpuAbi.BodyGravityOffset + 2);
                var gz = ReadF16(span, bodyOffset + PhysicsGpuAbi.BodyGravityOffset + 4);

                var comLocal = new Vector3(
                    ReadF16(span, bodyOffset + PhysicsGpuAbi.BodyCentreOfMassOffset),
                    ReadF16(span, bodyOffset + PhysicsGpuAbi.BodyCentreOfMassOffset + 2),
                    ReadF16(span, bodyOffset + PhysicsGpuAbi.BodyCentreOfMassOffset + 4));

                var principal = new Quaternion(
                    BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 104, 4)),
                    BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 108, 4)),
                    BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 112, 4)),
                    BinaryPrimitives.ReadSingleLittleEndian(span.Slice(bodyOffset + 116, 4)));

                var ix = ReadPrincipalInertia(span, bodyOffset + 120);
                var iy = ReadPrincipalInertia(span, bodyOffset + 128);
                var iz = ReadPrincipalInertia(span, bodyOffset + 136);

                var invI = new Vector3(
                    float.IsFinite(ix) && ix > 0f ? 1f / ix : 0f,
                    float.IsFinite(iy) && iy > 0f ? 1f / iy : 0f,
                    float.IsFinite(iz) && iz > 0f ? 1f / iz : 0f);

                b.Vx = vx; b.Vy = vy; b.Vz = vz;
                b.Wx = wx; b.Wy = wy; b.Wz = wz;
                b.Mass = mass; b.InvMass = 1.0f / mass;
                b.Gx = gx; b.Gy = gy; b.Gz = gz;
                b.Drag = drag; b.DragBits = dragBits;
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
            var offset = PhysicsGpuAbi.CollidersOffset + c * PhysicsGpuAbi.ColliderBytes;
            var bodySlot = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 8, 4));
            var materialSlot = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 12, 4));
            var shapeKind = (ShapeKind)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 16, 4));

            var tx = ReadF16(span, offset + 24);
            var ty = ReadF16(span, offset + 26);
            var tz = ReadF16(span, offset + 28);
            var rqx = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 32, 4));
            var rqy = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 36, 4));
            var rqz = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 40, 4));
            var rqw = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 44, 4));
            var radius = ReadF16(span, offset + 48);
            var hx = ReadF16(span, offset + 50);
            var hy = ReadF16(span, offset + 52);
            var hz = ReadF16(span, offset + 54);

            var matOffset = PhysicsGpuAbi.MaterialsOffset + materialSlot * PhysicsGpuAbi.MaterialBytes;
            var restitution = ReadF16(span, matOffset + 8);
            var bounceThreshold = ReadF16(span, matOffset + 10);
            var friction = ReadF16(span, matOffset + 12);
            var rollingResistance = ReadF16(span, matOffset + 14);

            if (!_allBodies.TryGetValue(bodySlot, out var body)) continue;

            while (_colliderPool.Count <= c) _colliderPool.Add(new SimCollider());
            var collider = _colliderPool[c];
            collider.Index = c;
            collider.Id = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(offset, 8));
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

            if (body.Motion == 1)
            {
                body.Collider = collider;
                body.SupportExtent = shapeKind == ShapeKind.Sphere ? radius :
                    hx * MathF.Abs(collider.U0.Y) + hy * MathF.Abs(collider.U1.Y) + hz * MathF.Abs(collider.U2.Y);
            }
        }

        _joints.Clear();
        var prismaticCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.PrismaticsOffset, 4));
        for (var index = 0; index < prismaticCount; index++)
        {
            var offset = PhysicsGpuAbi.PrismaticRecordsOffset + index * PhysicsGpuAbi.PrismaticBytes;
            var aSlot = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 8, 4));
            var bSlot = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 12, 4));

            var pa = new Vector3(BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 16, 4)), BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 20, 4)), BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 24, 4)));
            var qa = new Quaternion(BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 28, 4)), BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 32, 4)), BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 36, 4)), BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 40, 4)));
            var pb = new Vector3(BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 44, 4)), BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 48, 4)), BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 52, 4)));
            var qb = new Quaternion(BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 56, 4)), BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 60, 4)), BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 64, 4)), BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 68, 4)));
            var lower = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 72, 4));
            var upper = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 76, 4));
            var stiffness = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 80, 4));
            var damping = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 84, 4));
            var soft = SoftConstraintCoefficients.FromSpring(stiffness, damping, 1f / WorkshopCadenceSettings.PhysicalFrequency);
            var collision = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 88, 4));

            while (_jointPool.Count <= index) _jointPool.Add(new SimJoint());
            var joint = _jointPool[index];
            joint.Offset = offset;
            joint.A = _allBodies[aSlot];
            joint.B = _allBodies[bSlot];
            joint.Pa = pa; joint.Qa = qa; joint.Pb = pb; joint.Qb = qb;
            joint.Lower = lower; joint.Upper = upper;
            joint.Soft = soft;
            joint.Collision = collision;
            joint.SupplyClosed = inputs.SupplyClosed[index];

            for (var imp = 0; imp < 8; imp++)
                joint.Impulses[imp] = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset + 96 + imp * 4, 4));

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
            var isDyn = b.Motion == 1;
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
            else continue;

            // Exactly the complement of the overlap callback's static-static rejection below.
            _bvh.InsertLeaf(c.Index, min, max, b.Motion != 0);
        }

        _candidatePairs.Clear();
        _bvh.QueryOverlaps((idxA, idxB) =>
        {
            if (idxA >= idxB) return;
            var colA = _colliders[idxA];
            var colB = _colliders[idxB];
            if (colA.BodySlot == colB.BodySlot) return;
            if (colA.Body.Motion == 0 && colB.Body.Motion == 0) return;
            foreach (var j in _joints)
            {
                if (j.Collision == 0 && ((j.A == colA.Body && j.B == colB.Body) || (j.B == colA.Body && j.A == colB.Body)))
                    return;
            }
            _candidatePairs.Add((colA, colB));
        });

        // Story 9R.4: every compiled contact-load row, read once per tick. A row's tick measurement is the maximum qualifying mass
        // of its substeps (D2), written over the copied source state so a removed body reads Empty on the very next tick.
        var loadCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.ContactLoadSensorsOffset, 4));
        if (loadCount > PhysicsSceneDeclaration.ContactLoadSensorCapacity)
            throw new ArgumentException($"The world declares {loadCount} contact-load rows, beyond the table's {PhysicsSceneDeclaration.ContactLoadSensorCapacity}.");
        var loadRows = (int)loadCount;
        Span<ContactLoadSensorDeclaration> loadDeclarations = stackalloc ContactLoadSensorDeclaration[PhysicsSceneDeclaration.ContactLoadSensorCapacity];
        Span<int> loadFrames = stackalloc int[PhysicsSceneDeclaration.ContactLoadSensorCapacity];
        Span<float> loadMass = stackalloc float[PhysicsSceneDeclaration.ContactLoadSensorCapacity];
        for (var row = 0; row < loadRows; row++)
        {
            loadDeclarations[row] = PhysicsGpuAbi.ReadContactLoadDeclaration(span, row);
            loadFrames[row] = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(
                PhysicsGpuAbi.ContactLoadRecordsOffset + row * PhysicsGpuAbi.ContactLoadSensorBytes + PhysicsGpuAbi.ContactLoadFrameOffset, 4));
            loadMass[row] = 0f;
        }

        for (var s = 0; s < steps; s++)
        {
            if ((sourceOrdinal + s) % 4 == 0)
            {
                for (var supply = 0; supply < supplyCount; supply++)
                {
                    var record = PhysicsGpuAbi.ElectricalSourcesOffset + supply * PhysicsGpuAbi.ElectricalSourceBytes;
                    var nextEnabled = inputs.SourceEnable[supply];
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(record + 36, 4), (uint)nextEnabled);

                    _storeIndices.Clear();
                    _balances.Clear();
                    _capacities.Clear();
                    _credits.Clear();

                    var bindingCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderElectricalBindingCountOffset, 4));
                    for (var route = 0; route < bindingCount; route++)
                    {
                        var binding = PhysicsGpuAbi.ElectricalBindingsOffset + route * PhysicsGpuAbi.ElectricalBindingBytes;
                        if (BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(binding, 4)) != supply) continue;
                        var targetStore = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(binding + 4, 4));
                        var storeOffset = PhysicsGpuAbi.ContactWorksOffset + targetStore * 64;
                        _storeIndices.Add(storeOffset);
                        _balances.Add(BinaryPrimitives.ReadSingleLittleEndian(span.Slice(storeOffset + 48, 4)));
                        _capacities.Add(BinaryPrimitives.ReadSingleLittleEndian(span.Slice(storeOffset + 36, 4)));
                        _credits.Add(BinaryPrimitives.ReadSingleLittleEndian(span.Slice(storeOffset + 52, 4)));
                    }

                    var count = _storeIndices.Count;
                    for (var i = 0; i < count; i++)
                    {
                        elecBefore[i] = _balances[i];
                        elecLimits[i] = _capacities[i];
                    }

                    var bal = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(record + 32, 4));
                    var pwr = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(record + 20, 4));
                    var deb = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(record + 40, 4));
                    var transfer = ElectricalSupply.Allocate((ElectricalEnable)nextEnabled, bal, pwr, elecBefore[..count], elecLimits[..count], elecAfter[..count]);

                    BinaryPrimitives.WriteSingleLittleEndian(span.Slice(record + 32, 4), transfer.Remaining);
                    BinaryPrimitives.WriteSingleLittleEndian(span.Slice(record + 40, 4), deb + transfer.Debit);
                    for (var load = 0; load < count; load++)
                    {
                        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(_storeIndices[load] + 48, 4), elecAfter[load]);
                        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(_storeIndices[load] + 52, 4), _credits[load] + (elecAfter[load] - elecBefore[load]));
                    }
                }
            }

            var guideCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderGuideCountOffset, 4));
            for (var g = 0; g < guideCount; g++)
            {
                var offset = PhysicsGpuAbi.GuidesOffset + g * 64;
                var frameSlot = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 8, 4));
                var targetSlot = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 12, 4));
                var minX = ReadF16(span, offset + 40);
                var minY = ReadF16(span, offset + 42);
                var minZ = ReadF16(span, offset + 44);
                var maxX = ReadF16(span, offset + 48);
                var maxY = ReadF16(span, offset + 50);
                var maxZ = ReadF16(span, offset + 52);
                var maxAcc = ReadF16(span, offset + 56);
                var supportHeight = ReadF16(span, offset + 58);
                var supportMargin = ReadF16(span, offset + 60);

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

            // Each supplied gate follows its own compiled route (SimJoint.SupplyClosed), closed from committed state, never a timer.
            PrismaticConstraintSolver.ActuatorSliderDrive(_joints, dt);

            foreach (var b in _dynamicBodies)
            {
                b.Vx += b.Gx * dt;
                b.Vy += b.Gy * dt;
                b.Vz += b.Gz * dt;
                var decay = MathF.Exp(-b.Drag * dt);
                b.Vx *= decay;
                b.Vy *= decay;
                b.Vz *= decay;
            }

            foreach (var c in _colliders)
                if (c.Body.Motion == 1) WorkshopCollisionKernels.RefreshCollider(c);

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
                WorkshopCollisionKernels.GenerateManifold(colA, colB, WorkshopConstraintSolver.SpeculativeSlop + relSpeed * dt, _contacts);
                if (_contacts.Count > first)
                {
                    while (_manifoldPool.Count <= _manifolds.Count) _manifoldPool.Add(new ContactManifold());
                    var manifold = _manifoldPool[_manifolds.Count];
                    manifold.Points.Clear();
                    manifold.Rolling = 0;
                    manifold.RollingMass1 = 0;
                    manifold.RollingMass2 = 0;
                    manifold.LambdaR1 = 0;
                    manifold.LambdaR2 = 0;
                    for (var i = first; i < _contacts.Count; i++) manifold.Points.Add(_contacts[i]);
                    _manifolds.Add(manifold);
                }
            }

            if (loadRows > 0) MeasureContactLoads(loadDeclarations[..loadRows], loadFrames[..loadRows], loadMass[..loadRows]);

            foreach (var c in _contacts) WorkshopConstraintSolver.PrepareContact(c, _contactCache);
            foreach (var m in _manifolds) WorkshopConstraintSolver.PrepareRolling(m);
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

            for (var iteration = 0; iteration < WorkshopConstraintSolver.SolverIterations; iteration++)
            {
                PrismaticConstraintSolver.SweepConstraints(_joints, true);
                WorkshopConstraintSolver.SweepManifolds(_manifolds, true, softCoeffs, dt);
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
                WorkshopConstraintSolver.SweepManifolds(_manifolds, false, softCoeffs, dt);
            }

            foreach (var joint in _joints)
            {
                for (var row = 0; row < 8; row++)
                    BinaryPrimitives.WriteSingleLittleEndian(span.Slice(joint.Offset + 96 + row * 4, 4), joint.Impulses[row]);
            }

            foreach (var c in _contacts) WorkshopConstraintSolver.ApplyRestitution(c);
            foreach (var b in _dynamicBodies) WorkshopConstraintSolver.SettleVelocity(b);

            _contactCache.Clear();
            foreach (var c in _contacts)
            {
                var approach = (c.Bounced || WorkshopConstraintSolver.CurrentSeparation(c) <= 0f) ? 0f : c.RelVel;
                _contactCache[c.Key] = (c.LambdaN, c.LambdaT1, c.LambdaT2, approach);
            }

            foreach (var c in _contacts)
            {
                if (c.MaxImpulse <= 0f || WorkshopConstraintSolver.CurrentSeparation(c) > 0f) continue;
                if (c.A.Motion == 1 && c.B.Motion == 0) EmitContactEvents(span, c.ColB, c.A, -c.RelVel, c.Normal, c, s, sourceOrdinal);
                else if (c.B.Motion == 1 && c.A.Motion == 0) EmitContactEvents(span, c.ColA, c.B, -c.RelVel, -c.Normal, c, s, sourceOrdinal);
            }

            var orientationCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderOrientationSensorCountOffset, 4));
            for (var o = 0; o < orientationCount; o++)
            {
                var oOffset = PhysicsGpuAbi.OrientationSensorsOffset + o * 64;
                if (BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(oOffset + 40, 4)) != 0) continue;
                var sensedSlot = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(oOffset + 8, 4));
                if (!_allBodies.TryGetValue(sensedSlot, out var sensed)) continue;

                var qDeclared = new Quaternion(
                    BinaryPrimitives.ReadSingleLittleEndian(span.Slice(oOffset + PhysicsGpuAbi.OrientationSensorRotationOffset, 4)),
                    BinaryPrimitives.ReadSingleLittleEndian(span.Slice(oOffset + PhysicsGpuAbi.OrientationSensorRotationOffset + 4, 4)),
                    BinaryPrimitives.ReadSingleLittleEndian(span.Slice(oOffset + PhysicsGpuAbi.OrientationSensorRotationOffset + 8, 4)),
                    BinaryPrimitives.ReadSingleLittleEndian(span.Slice(oOffset + PhysicsGpuAbi.OrientationSensorRotationOffset + 12, 4)));

                var alignment = MathF.Abs(sensed.Qx * qDeclared.X + sensed.Qy * qDeclared.Y + sensed.Qz * qDeclared.Z + sensed.Qw * qDeclared.W);
                if (alignment <= ReadF16(span, oOffset + 32))
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(oOffset + 40, 4), 1);
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(oOffset + 44, 4), sourceOrdinal + (uint)s + 1);
                    WriteF16(span, oOffset + 48, 0f);
                }
            }

            foreach (var b in _dynamicBodies)
            {
                var pieceOffset = motionOffset + PhysicsMotionRead.HeaderBytes + (b.DynamicIndex * steps + s) * PhysicsMotionRead.PieceBytes;
                span.Slice(pieceOffset, PhysicsMotionRead.PieceBytes).Clear();
                var pieceSpan = span.Slice(pieceOffset, PhysicsMotionRead.PieceBytes);

                BinaryPrimitives.WriteUInt32LittleEndian(pieceSpan.Slice(0, 4), 1);
                BinaryPrimitives.WriteUInt32LittleEndian(pieceSpan.Slice(4, 4), sourceOrdinal + (uint)s);
                BinaryPrimitives.WriteUInt32LittleEndian(pieceSpan.Slice(8, 4), sourceOrdinal + (uint)s + 1);
                BinaryPrimitives.WriteUInt32LittleEndian(pieceSpan.Slice(12, 4), sourceOrdinal + (uint)s);
                WriteF16(pieceSpan, 16, 0f);
                WriteF16(pieceSpan, 18, 0f);
                WriteF16(pieceSpan, 20, 0f);
                WriteF16(pieceSpan, 22, 480f);
                BinaryPrimitives.WriteUInt32LittleEndian(pieceSpan.Slice(24, 4), b.BodyIdLow);
                BinaryPrimitives.WriteUInt32LittleEndian(pieceSpan.Slice(28, 4), b.BodyIdHigh);
                BinaryPrimitives.WriteInt32LittleEndian(pieceSpan.Slice(32, 4), b.CellX);
                BinaryPrimitives.WriteInt32LittleEndian(pieceSpan.Slice(36, 4), b.CellY);
                BinaryPrimitives.WriteInt32LittleEndian(pieceSpan.Slice(40, 4), b.CellZ);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(48, 4), b.LocalX);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(52, 4), b.LocalY);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(56, 4), b.LocalZ);
                BinaryPrimitives.WriteUInt16LittleEndian(pieceSpan.Slice(PhysicsMotionRead.DragRateOffset, 2), b.DragBits);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(64, 4), b.Qx);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(68, 4), b.Qy);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(72, 4), b.Qz);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(76, 4), b.Qw);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(PhysicsMotionRead.VelocityOffset, 4), b.Vx);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(PhysicsMotionRead.VelocityOffset + 4, 4), b.Vy);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(PhysicsMotionRead.VelocityOffset + 8, 4), b.Vz);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(PhysicsMotionRead.AngularVelocityOffset, 4), b.Wx);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(PhysicsMotionRead.AngularVelocityOffset + 4, 4), b.Wy);
                BinaryPrimitives.WriteSingleLittleEndian(pieceSpan.Slice(PhysicsMotionRead.AngularVelocityOffset + 8, 4), b.Wz);
                WriteF16(pieceSpan, PhysicsMotionRead.GravityOffset, b.Gx);
                WriteF16(pieceSpan, PhysicsMotionRead.GravityOffset + 2, b.Gy);
                WriteF16(pieceSpan, PhysicsMotionRead.GravityOffset + 4, b.Gz);
            }
        }

        for (var row = 0; row < loadRows; row++)
        {
            var record = span.Slice(PhysicsGpuAbi.ContactLoadRecordsOffset + row * PhysicsGpuAbi.ContactLoadSensorBytes, PhysicsGpuAbi.ContactLoadSensorBytes);
            BinaryPrimitives.WriteUInt32LittleEndian(record[PhysicsGpuAbi.ContactLoadPhaseOffset..], (uint)loadDeclarations[row].PhaseOf(loadMass[row]));
            BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.ContactLoadMassOffset..], loadMass[row]);
        }

        var totalCaptured = 0u;
        var sensorCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderSensorCountOffset, 4));
        for (var i = 0; i < sensorCount; i++)
        {
            var offset = PhysicsGpuAbi.SensorsOffset + i * PhysicsGpuAbi.SensorBytes;
            var frameSlot = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 8, 4));
            var targetSlot = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 12, 4));
            var participation = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 16, 4));
            var minX = ReadF16(span, offset + 48);
            var minY = ReadF16(span, offset + 50);
            var minZ = ReadF16(span, offset + 52);
            var maxX = ReadF16(span, offset + 56);
            var maxY = ReadF16(span, offset + 58);
            var maxZ = ReadF16(span, offset + 60);
            var speedLimit = ReadF16(span, offset + 64);
            var dwell = ReadF16(span, offset + 66);

            var prevPhase = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 72, 4));
            var prevStartOrdinal = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 76, 4));
            var prevStartPhase = ReadF16(span, offset + 80);

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

            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset + 72, 4), phase);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset + 76, 4), startOrdinal);
            WriteF16(span, offset + 80, startPhase);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset + 82, 2), 0);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset + 84, 4), occurrenceCount);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset + 88, 4), eventOrdinal);
            WriteF16(span, offset + 92, eventPhase);
            span.Slice(offset + 94, 34).Clear();

            totalCaptured += occurrenceCount;
        }

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(PhysicsGpuAbi.HeaderCapturedCountOffset, 4), totalCaptured);

        foreach (var b in _dynamicBodies)
        {
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyCellOffset, 4), b.CellX);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyCellOffset + 4, 4), b.CellY);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(b.BodyOffset + 24, 4), b.CellZ);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + 32, 4), b.LocalX);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + 36, 4), b.LocalY);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + 40, 4), b.LocalZ);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + 48, 4), b.Qx);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + 52, 4), b.Qy);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + 56, 4), b.Qz);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + 60, 4), b.Qw);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyVelocityOffset, 4), b.Vx);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyVelocityOffset + 4, 4), b.Vy);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyVelocityOffset + 8, 4), b.Vz);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyAngularVelocityOffset, 4), b.Wx);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyAngularVelocityOffset + 4, 4), b.Wy);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(b.BodyOffset + PhysicsGpuAbi.BodyAngularVelocityOffset + 8, 4), b.Wz);
        }
    }
}
