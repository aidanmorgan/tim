using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class PrismaticConstraintTests
{
    private static readonly WorkshopGpuProfile Profile = new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
    private static PrismaticConstraintDeclaration Joint => new(new(100), new(1), new(2),
        new(new Vector3(0, .14f, 0), Quaternion.Identity), ConstraintFrame.Identity,
        -.25f, 0, 400.00003f, .2f, ConnectedCollision.Disabled);

    private static PhysicsSceneDeclaration Scene(PrismaticConstraintDeclaration joint) => new(new(1, 2), 101,
        [new(new(1), RigidMotionKind.Static, default, default, CanonicalRotation.Identity, default, default, new((Half)0), default, new((Half)0)),
         new(new(2), RigidMotionKind.Dynamic, default, default, CanonicalRotation.Identity, default, default, new((Half).25), default, new((Half)0))],
        [new(new(4), new(2), new(3), ColliderShapeKind.Box, RigidLocalPose.Identity, new((Half)0), new((Half).65, (Half).075, (Half).6))],
        [new(new(3), new((Half)0), new((Half).05), new((Half).1), new((Half)0))], [], [], prismatics: [joint]);

    [Fact]
    public void CanonicalFramesParametersAndZeroStateRoundTrip()
    {
        var bytes = PhysicsGpuAbi.Admission(Scene(Joint), new(1), Profile);
        Assert.Equal(Joint, PhysicsGpuAbi.ReadPrismatic(bytes, 0));
        Assert.NotEqual((float)(Half)Joint.Stiffness, Joint.Stiffness);
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.PrismaticsOffset)));
        Assert.All(bytes.AsSpan(PhysicsGpuAbi.PrismaticRecordsOffset + PhysicsGpuAbi.PrismaticImpulseOffset, 32).ToArray(),
            value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public void InvalidIdentityFramesParametersAndPairsReject()
    {
        PrismaticConstraintDeclaration[] invalid =
        [
            Joint with { BodyB = new(99) }, Joint with { BodyB = Joint.BodyA }, Joint with { Id = new(3) },
            Joint with { FrameA = new(Vector3.Zero, default) },
            Joint with { FrameB = new(new Vector3(float.NaN, 0, 0), Quaternion.Identity) },
            Joint with { Stiffness = float.NaN }, Joint with { Damping = -1 },
            Joint with { Lower = .1f }, Joint with { Upper = -.5f },
            Joint with { Collision = (ConnectedCollision)256 }
        ];
        foreach (var value in invalid) Assert.Throws<ArgumentException>(() => Scene(value));
        Assert.Equal(Joint, Scene(Joint).Prismatics[0]);
    }

    [Theory]
    [InlineData(8, 99u)]
    [InlineData(88, 256u)]
    [InlineData(92, 1u)]
    [InlineData(128, 1u)]
    public void MalformedAbiDoesNotAlterAcceptedSource(int field, uint value)
    {
        var source = PhysicsGpuAbi.Admission(Scene(Joint), new(1), Profile);
        var before = source.ToArray(); var malformed = source.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(PhysicsGpuAbi.PrismaticRecordsOffset + field), value);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismatic(malformed, 0));
        Assert.Equal(before, source); Assert.Equal(Joint, PhysicsGpuAbi.ReadPrismatic(source, 0));
    }

    private static byte[] Advance(byte[] source)
    {
        var bytes = source.ToArray();
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(40), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(88), 4);
        var motion = bytes.AsSpan(PhysicsGpuAbi.MotionOffset, PhysicsMotionRead.ByteLength);
        BinaryPrimitives.WriteUInt32LittleEndian(motion, 4);
        BinaryPrimitives.WriteUInt32LittleEndian(motion[4..], 4);
        BinaryPrimitives.WriteUInt32LittleEndian(motion[12..], 4);
        var body = bytes.AsSpan(PhysicsGpuAbi.BodiesOffset + PhysicsGpuAbi.BodyBytes, PhysicsGpuAbi.BodyBytes);
        for (var step = 0; step < 4; step++)
        {
            var piece = motion.Slice(PhysicsMotionRead.HeaderBytes + step * PhysicsMotionRead.PieceBytes, PhysicsMotionRead.PieceBytes);
            BinaryPrimitives.WriteUInt32LittleEndian(piece, (uint)PhysicsMotionKind.FreePolynomial);
            BinaryPrimitives.WriteUInt32LittleEndian(piece[4..], (uint)step);
            BinaryPrimitives.WriteUInt32LittleEndian(piece[8..], (uint)step + 1);
            BinaryPrimitives.WriteUInt32LittleEndian(piece[12..], (uint)step);
            BinaryPrimitives.WriteUInt16LittleEndian(piece[22..], BitConverter.HalfToUInt16Bits((Half)480));
            body[..8].CopyTo(piece[24..]); body[16..28].CopyTo(piece[32..]);
            body[32..38].CopyTo(piece[48..]); body[40..48].CopyTo(piece[56..]);
        }
        return bytes;
    }

    [Fact]
    public void CandidateAcceptsSignedAlignmentAndSpringAndNonnegativeStops()
    {
        var source = PhysicsGpuAbi.Admission(Scene(Joint), new(1), Profile);
        PhysicsGpuAbi.ValidateCandidate(source, source, new(0));
        var candidate = Advance(source);
        float[] impulses = [-1, 2, -3, 4, -5, -6, 7, 8];
        for (var row = 0; row < impulses.Length; row++)
            BinaryPrimitives.WriteSingleLittleEndian(candidate.AsSpan(PhysicsGpuAbi.PrismaticRecordsOffset +
                PhysicsGpuAbi.PrismaticImpulseOffset + row * 4), impulses[row]);
        PhysicsGpuAbi.ValidateCandidate(candidate, source, new(1));
        Assert.All(source.AsSpan(PhysicsGpuAbi.PrismaticRecordsOffset + PhysicsGpuAbi.PrismaticImpulseOffset, 32).ToArray(),
            value => Assert.Equal((byte)0, value));
    }

    [Theory]
    [InlineData(0, 99u)]
    [InlineData(80, 1u)]
    [InlineData(92, 1u)]
    [InlineData(96, 0x7fc00000u)]
    [InlineData(120, 0xbf800000u)]
    [InlineData(124, 0xbf800000u)]
    [InlineData(128, 1u)]
    public void FullCandidateRejectsMalformedDeclarationPaddingOrState(int field, uint value)
    {
        var source = PhysicsGpuAbi.Admission(Scene(Joint), new(1), Profile);
        var before = source.ToArray(); var candidate = Advance(source);
        BinaryPrimitives.WriteUInt32LittleEndian(candidate.AsSpan(PhysicsGpuAbi.PrismaticRecordsOffset + field), value);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, source, new(1)));
        Assert.Equal(before, source);
    }

    [Fact]
    public void FullCandidateRejectsChangedConstraintCount()
    {
        var source = PhysicsGpuAbi.Admission(Scene(Joint), new(1), Profile);
        var candidate = Advance(source);
        BinaryPrimitives.WriteUInt32LittleEndian(candidate.AsSpan(PhysicsGpuAbi.PrismaticsOffset), 0);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, source, new(1)));
    }

    [Fact]
    public void DuplicateReversedAndStaticOnlyPairsReject()
    {
        var valid = Scene(Joint);
        foreach (var duplicate in new[]
        {
            Joint with { Id = new(101) },
            Joint with { Id = new(101), BodyA = Joint.BodyB, BodyB = Joint.BodyA }
        })
            Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(new(1, 2), 102,
                valid.Bodies.ToArray(), valid.Colliders.ToArray(), valid.Materials.ToArray(), [], [],
                prismatics: [Joint, duplicate]));
        var bodies = valid.Bodies.ToArray();
        bodies[1] = bodies[0] with { Id = new(2) };
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(new(1, 2), 101,
            bodies, valid.Colliders.ToArray(), valid.Materials.ToArray(), [], [], prismatics: [Joint]));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.7f)]
    [InlineData(-1.3f)]
    public void LinearJacobiansMatchMovingRotatingOffCentreFrames(float angle)
    {
        var a = new ConstraintBodyFrame(new(1, 2, -1), Quaternion.CreateFromYawPitchRoll(angle, .2f, -.3f), new(1.1f, 1.8f, -.9f));
        var b = new ConstraintBodyFrame(new(-.4f, 2.4f, .8f), Quaternion.CreateFromYawPitchRoll(-.2f, angle, .5f), new(-.5f, 2.6f, .7f));
        var localA = new ConstraintFrame(new(.2f, -.1f, .3f), Quaternion.CreateFromYawPitchRoll(.1f, -.2f, .4f));
        var localB = new ConstraintFrame(new(-.3f, .2f, .1f), Quaternion.CreateFromYawPitchRoll(.3f, .1f, -.2f));
        Vector3 va = new(.3f, -.2f, .7f), vb = new(-.4f, .6f, .2f), wa = new(.5f, .2f, -.3f), wb = new(-.2f, .4f, .3f);
        var rows = Rows(a, b, localA, localB);
        const float h = .001f;
        var before = Rows(Move(a, va, wa, -h), Move(b, vb, wb, -h), localA, localB);
        var after = Rows(Move(a, va, wa, h), Move(b, vb, wb, h), localA, localB);
        foreach (var index in new[] { 0, 1, 5, 6, 7 })
        {
            var row = rows[index];
            var expected = (after[index].Error - before[index].Error) / (2 * h);
            var actual = Vector3.Dot(row.LinearA, va) + Vector3.Dot(row.LinearB, vb) +
                Vector3.Dot(row.AngularA, wa) + Vector3.Dot(row.AngularB, wb);
            Assert.InRange(MathF.Abs(actual - expected), 0, .001f);
        }
        Assert.Equal(rows[5].LinearA, rows[6].LinearA);
        Assert.Equal(-rows[6].LinearA, rows[7].LinearA);
        Assert.Equal(-rows[6].AngularA, rows[7].AngularA);
        Assert.Equal(-rows[6].AngularB, rows[7].AngularB);
        var rotation = Quaternion.CreateFromYawPitchRoll(.4f, -.6f, .2f);
        var shift = new Vector3(2, -1, .5f);
        ConstraintBodyFrame Transform(ConstraintBodyFrame value) => new(
            Vector3.Transform(value.Position, rotation) + shift, Quaternion.Multiply(rotation, value.Rotation),
            Vector3.Transform(value.CentreOfMass, rotation) + shift);
        var transformed = Rows(Transform(a), Transform(b), localA, localB);
        foreach (var index in new[] { 0, 1, 5, 6, 7 })
        {
            Assert.InRange(MathF.Abs(rows[index].Error - transformed[index].Error), 0, 4e-6f);
            Assert.InRange(Vector3.Distance(Vector3.Transform(rows[index].AngularA, rotation), transformed[index].AngularA), 0, 4e-6f);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AngularRowsRestoreEachAxisAndIgnoreQuaternionSign(int axis)
    {
        var unit = axis == 0 ? Vector3.UnitX : axis == 1 ? Vector3.UnitY : Vector3.UnitZ;
        var a = new ConstraintBodyFrame(Vector3.Zero, Quaternion.Identity, Vector3.Zero);
        var rotation = Quaternion.CreateFromAxisAngle(unit, .2f);
        var b = new ConstraintBodyFrame(Vector3.Zero, rotation, Vector3.Zero);
        var rows = Rows(a, b, ConstraintFrame.Identity, ConstraintFrame.Identity);
        var opposite = Rows(a, b with { Rotation = new(-rotation.X, -rotation.Y, -rotation.Z, -rotation.W) },
            ConstraintFrame.Identity, ConstraintFrame.Identity);
        Assert.InRange(rows[axis + 2].Error, .19999f, .20001f);
        Assert.Equal(-unit, rows[axis + 2].AngularA);
        Assert.Equal(unit, rows[axis + 2].AngularB);
        for (var row = 0; row < 8; row++) Assert.Equal(rows[row], opposite[row]);
    }


    [Fact]
    public void RowKindsHaveOneCanonicalConstitutiveOrder()
    {
        Assert.Equal(new[] { PrismaticRowKind.TransverseX, PrismaticRowKind.TransverseZ,
            PrismaticRowKind.AngularX, PrismaticRowKind.AngularY, PrismaticRowKind.AngularZ,
            PrismaticRowKind.Spring, PrismaticRowKind.LowerStop, PrismaticRowKind.UpperStop }, Enum.GetValues<PrismaticRowKind>());
        Assert.Equal(Enumerable.Range(0, PrismaticRows.Count), Enum.GetValues<PrismaticRowKind>().Select(kind => (int)kind));
        foreach (var kind in Enum.GetValues<PrismaticRowKind>())
            Assert.Equal(kind is PrismaticRowKind.LowerStop or PrismaticRowKind.UpperStop, PrismaticRows.IsStop(kind));
    }

    [Theory]
    [InlineData(179.5f)]
    [InlineData(180.5f)]
    public void CompoundLocalFramesUseEquivalentShortestArcAcrossQuaternionSigns(float degrees)
    {
        var qa = Quaternion.CreateFromYawPitchRoll(.4f, -.2f, .3f);
        var la = new ConstraintFrame(new(.2f, 0, -.1f), Quaternion.CreateFromYawPitchRoll(-.3f, .4f, .2f));
        var lb = new ConstraintFrame(new(-.1f, .2f, .3f), Quaternion.CreateFromYawPitchRoll(.2f, -.4f, .1f));
        var delta = Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1, 2, -3)), degrees * MathF.PI / 180f);
        var qb = Quaternion.Normalize(delta * qa * la.Rotation * Quaternion.Conjugate(lb.Rotation));
        var a = new ConstraintBodyFrame(Vector3.Zero, qa, Vector3.Zero);
        var b = new ConstraintBodyFrame(Vector3.One, qb, Vector3.One);
        var rows = Rows(a, b, la, lb);
        var opposite = Rows(a, b with { Rotation = -qb }, la, lb with { Rotation = -lb.Rotation });
        var error = new Vector3(rows[(int)PrismaticRowKind.AngularX].Error,
            rows[(int)PrismaticRowKind.AngularY].Error, rows[(int)PrismaticRowKind.AngularZ].Error);
        Assert.InRange(MathF.Abs(error.Length() - 179.5f * MathF.PI / 180f), 0, 2e-6f);
        foreach (var kind in Enum.GetValues<PrismaticRowKind>())
        {
            var row = rows[(int)kind]; var equivalent = opposite[(int)kind];
            Assert.InRange(MathF.Abs(row.Error - equivalent.Error), 0, 2e-6f);
            Assert.InRange(Vector3.Distance(row.AngularA, equivalent.AngularA), 0, 2e-6f);
            Assert.InRange(Vector3.Distance(row.AngularB, equivalent.AngularB), 0, 2e-6f);
        }
    }

    private static PrismaticRow[] Rows(ConstraintBodyFrame a, ConstraintBodyFrame b, ConstraintFrame localA, ConstraintFrame localB)
    {
        var rows = new PrismaticRow[8];
        PrismaticRows.Prepare(a, b, localA, localB, -.25f, 0, rows);
        return rows;
    }

    private static ConstraintBodyFrame Move(ConstraintBodyFrame body, Vector3 velocity, Vector3 angular, float h)
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(angular), angular.Length() * h);
        var centre = body.CentreOfMass + velocity * h;
        return new(centre + Vector3.Transform(body.Position - body.CentreOfMass, rotation),
            Quaternion.Multiply(rotation, body.Rotation), centre);
    }
}
