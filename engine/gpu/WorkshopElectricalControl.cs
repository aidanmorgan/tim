using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>Explicit player input; a committed electrical phase applies this enable state.</summary>
public readonly record struct WorkshopElectricalControl(GpuBodyId Owner, ElectricalEnable Enabled)
{
    public void Validate()
    {
        if (Owner.Value == 0 || !Enum.IsDefined(Enabled)) throw new ArgumentException("Invalid source enable control.");
    }
    internal void Write(Span<byte> bytes)
    {
        Validate();
        if (bytes.Length != 16) throw new ArgumentException("Invalid source control width.");
        bytes.Clear(); BinaryPrimitives.WriteUInt64LittleEndian(bytes, Owner.Value); bytes[8] = (byte)Enabled;
    }
    internal static WorkshopElectricalControl Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 16 || bytes[9..].IndexOfAnyExcept((byte)0) >= 0)
            throw new ArgumentException("Invalid source control shape.");
        var control = new WorkshopElectricalControl(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes)), (ElectricalEnable)bytes[8]);
        control.Validate(); return control;
    }
}
