using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>The exact drive input consumed by one committed physics tick, with its resulting axial motion.</summary>
public readonly record struct ActuatorDriveRead(
    PrismaticConstraintId Joint, GpuBodyId Owner, bool SupplyClosed, float Coordinate, float Speed, float Lower, float Upper)
{
    public bool Blocked => Coordinate > Lower + 1e-5f && Coordinate < Upper - 1e-5f && MathF.Abs(Speed) < 1e-5f;
    public void Validate()
    {
        if (Joint.Value == 0 || Owner.Value == 0 || !float.IsFinite(Coordinate) || !float.IsFinite(Speed) ||
            !float.IsFinite(Lower) || !float.IsFinite(Upper) || Lower > Upper)
            throw new ArgumentException("Invalid committed actuator observation.");
    }
}

/// <summary>Immutable committed presentation queries. Population comes from the compiled world.</summary>
public readonly struct PhysicsPresentationRead
{
    private readonly ActuatorDriveRead[]? _actuators;
    private readonly ContactLoadSensorRead[]? _loads;
    public ReadOnlySpan<ActuatorDriveRead> Actuators => _actuators;
    public ReadOnlySpan<ContactLoadSensorRead> Loads => _loads;
    public PhysicsPresentationRead(ReadOnlySpan<ActuatorDriveRead> actuators, ReadOnlySpan<ContactLoadSensorRead> loads)
    {
        for (var i = 0; i < actuators.Length; i++)
        {
            actuators[i].Validate();
            if (i > 0 && actuators[i - 1].Joint.Value >= actuators[i].Joint.Value)
                throw new ArgumentException("Actuator observations require ordered unique identities.");
        }
        for (var i = 0; i < loads.Length; i++)
        {
            var load = loads[i];
            if (load.Sensor.Value == 0 || load.Frame.Value == 0 || load.Face.Value == 0 ||
                !float.IsFinite(load.MeasuredMass) || load.MeasuredMass < 0 || !Enum.IsDefined(load.Phase) ||
                (i > 0 && loads[i - 1].Sensor.Value >= load.Sensor.Value))
                throw new ArgumentException("Invalid committed contact-load observation.");
        }
        _actuators = actuators.ToArray(); _loads = loads.ToArray();
    }
    public bool HasSameBits(PhysicsPresentationRead other) =>
        Actuators.SequenceEqual(other.Actuators) && Loads.SequenceEqual(other.Loads);
}

public static partial class PhysicsGpuAbi
{
    /// <summary>Projection of validated output plus the very inputs supplied to this tick; no supply reconstruction.</summary>
    public static PhysicsPresentationRead ReadPresentation(ReadOnlySpan<byte> bytes, PhysicsTickInputs inputs, bool admission)
    {
        var layout = Layout(bytes);
        var actuators = new ActuatorDriveRead[layout.Prismatics];
        Span<PrismaticRow> rows = stackalloc PrismaticRow[PrismaticRows.Count];
        for (var i = 0; i < actuators.Length; i++)
        {
            var joint = ReadPrismatic(bytes, i);
            var record = bytes.Slice(layout.PrismaticOffset(i), PrismaticBytes);
            var a = bytes.Slice(layout.BodyOffset((int)R32(record, 8)), BodyBytes);
            var b = bytes.Slice(layout.BodyOffset((int)R32(record, 12)), BodyBytes);
            PrismaticRows.Prepare(PresentationFrame(a), PresentationFrame(b), joint.FrameA, joint.FrameB,
                joint.Lower, joint.Upper, rows);
            var axial = rows[(int)PrismaticRowKind.Spring];
            var speed = Vector3.Dot(axial.LinearA, PresentationVector(a, BodyVelocityOffset)) +
                Vector3.Dot(axial.LinearB, PresentationVector(b, BodyVelocityOffset)) +
                Vector3.Dot(axial.AngularA, PresentationVector(a, BodyAngularVelocityOffset)) +
                Vector3.Dot(axial.AngularB, PresentationVector(b, BodyAngularVelocityOffset));
            actuators[i] = new(joint.Id, joint.BodyA, !admission && inputs.SupplyClosed[i],
                axial.Error, speed, joint.Lower, joint.Upper);
        }
        var loads = new ContactLoadSensorRead[ReadContactLoadCount(bytes)];
        for (var i = 0; i < loads.Length; i++) loads[i] = ReadContactLoadSensor(bytes, i);
        return new(actuators, loads);
    }
    private static Vector3 PresentationVector(ReadOnlySpan<byte> body, int offset) =>
        new(RF(body, offset), RF(body, offset + 4), RF(body, offset + 8));
    private static ConstraintBodyFrame PresentationFrame(ReadOnlySpan<byte> body)
    {
        var value = ReadRotation(body, BodyRotationOffset);
        var rotation = new Quaternion(value.X, value.Y, value.Z, value.W);
        var position = PresentationPosition(body);
        var centre = ReadCentreOfMass(body);
        return new(position, rotation, position + Vector3.Transform(new((float)centre.X, (float)centre.Y, (float)centre.Z), rotation));
    }
    private static Vector3 PresentationPosition(ReadOnlySpan<byte> body)
    {
        var cell = ReadCell(body, BodyCellOffset); var local = ReadLocal(body, BodyLocalOffset);
        return new((cell.X + local.X) / 16f, (cell.Y + local.Y) / 16f, (cell.Z + local.Z) / 16f);
    }
}

