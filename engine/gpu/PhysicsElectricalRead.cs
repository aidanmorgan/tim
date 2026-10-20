using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public readonly record struct ElectricalSourceRead(ElectricalSourceId Id, GpuBodyId Owner, Joules Remaining,
    Joules Debit, ElectricalEnable Enabled)
{
    public bool Available => Enabled == ElectricalEnable.Enabled && Remaining.Value > 0;
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || !Enum.IsDefined(Enabled))
            throw new ArgumentException("Invalid electrical observation identity.");
        Remaining.Validate(ElectricalSupply.MaximumCapacity);
        Debit.Validate(8f);
    }
}

public readonly struct PhysicsElectricalRead
{
    // Story 9R.5b: exactly the compiled source population; no fixed source capacity.
    private readonly ElectricalSourceRead[]? _values;
    public int Count => _values?.Length ?? 0;
    public ElectricalSourceRead this[int index] => index >= 0 && index < Count ? _values![index] :
        throw new ArgumentOutOfRangeException(nameof(index));
    public PhysicsElectricalRead(ReadOnlySpan<ElectricalSourceRead> values) : this(values.ToArray()) { }

    /// <summary>Takes ownership of a freshly decoded array (no copy).</summary>
    internal static PhysicsElectricalRead Adopt(ElectricalSourceRead[] values) => new(values);

    private PhysicsElectricalRead(ElectricalSourceRead[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i > 0 && values[i - 1].Id.Value >= values[i].Id.Value)
                throw new ArgumentException("Electrical observations require ordered unique identities.");
        }
        _values = values;
    }
    public bool HasSameBits(PhysicsElectricalRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++)
            if (this[i].Id != other[i].Id || this[i].Owner != other[i].Owner || this[i].Enabled != other[i].Enabled ||
                !this[i].Remaining.HasSameBits(other[i].Remaining) || !this[i].Debit.HasSameBits(other[i].Debit)) return false;
        return true;
    }
    public void ValidateAdvance(PhysicsElectricalRead previous)
    {
        if (Count != previous.Count) throw new ArgumentException("Electrical source population changed.");
        for (var i = 0; i < Count; i++)
        {
            var before = previous[i]; var after = this[i];
            if (before.Id != after.Id || before.Owner != after.Owner || after.Remaining.Value > before.Remaining.Value ||
                Math.Abs((double)before.Remaining.Value - after.Remaining.Value - after.Debit.Value) >
                    Residual(before.Remaining.Value) + Residual(after.Debit.Value))
                throw new ArgumentException("Electrical source debit changed identity or balance.");
        }
    }
    // Boundary validation only: two ULPs cover a rounded sum/subtraction; ordinary residuals do not fault valid ticks.
    public static double Residual(float balance) => 2d * Math.Max(float.Epsilon, MathF.BitIncrement(balance) - balance);
}

internal static class WorkshopElectricalWire
{
    internal const int RecordBytes = 32;
    /// <summary>The section is exactly its population (Story 9R.5b).</summary>
    internal static int ByteLength(int count) => WorkshopWire.Width((long)count * RecordBytes);
    internal static void Write(PhysicsElectricalRead read, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength(read.Count)) throw new ArgumentException("Invalid electrical wire width.");
        bytes.Clear();
        for (var i = 0; i < read.Count; i++)
        {
            var v = read[i]; v.Validate(); var slot = bytes.Slice(i * RecordBytes, RecordBytes);
            BinaryPrimitives.WriteUInt64LittleEndian(slot, v.Id.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(slot[8..], v.Owner.Value);
            BinaryPrimitives.WriteSingleLittleEndian(slot[16..], v.Remaining.Value);
            BinaryPrimitives.WriteSingleLittleEndian(slot[20..], v.Debit.Value);
            slot[24] = (byte)v.Enabled; slot[25] = v.Available ? (byte)1 : (byte)0;
        }
    }
    internal static PhysicsElectricalRead Read(ReadOnlySpan<byte> bytes, int count)
    {
        if (count < 0 || bytes.Length != ByteLength(count))
            throw new ArgumentException("Invalid electrical wire population.");
        var values = new ElectricalSourceRead[count];
        for (var i = 0; i < count; i++)
        {
            var slot = bytes.Slice(i * RecordBytes, RecordBytes);
            var v = new ElectricalSourceRead(new(BinaryPrimitives.ReadUInt64LittleEndian(slot)),
                new(BinaryPrimitives.ReadUInt64LittleEndian(slot[8..])), new(BinaryPrimitives.ReadSingleLittleEndian(slot[16..])),
                new(BinaryPrimitives.ReadSingleLittleEndian(slot[20..])), (ElectricalEnable)slot[24]);
            v.Validate();
            if (slot[25] != (v.Available ? 1 : 0) || slot[26..].IndexOfAnyExcept((byte)0) >= 0)
                throw new ArgumentException("Invalid electrical wire availability or padding.");
            values[i] = v;
        }
        return PhysicsElectricalRead.Adopt(values);
    }
}
