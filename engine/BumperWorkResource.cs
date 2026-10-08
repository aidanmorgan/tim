using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

[GlobalClass]
public partial class BumperWorkResource : Resource
{
    [Export] public int StrengthBits { get; set; }
    [Export] public int ReferenceMassBits { get; set; }
    [Export] public int PreloadBits { get; set; }
    public BumperWork Capture()
    {
        if (StrengthBits is < 0 or > ushort.MaxValue || ReferenceMassBits is < 0 or > ushort.MaxValue ||
            PreloadBits is < 0 or > ushort.MaxValue) throw new ArgumentException("Invalid canonical work calibration bits.");
        var value = new BumperWork(new(BitConverter.UInt16BitsToHalf((ushort)StrengthBits)),
            new(BitConverter.UInt16BitsToHalf((ushort)ReferenceMassBits)),
            new(BitConverter.UInt16BitsToHalf((ushort)PreloadBits)));
        value.Validate(); return value;
    }
}
