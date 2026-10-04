using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

[GlobalClass]
public partial class RampDimensionsResource : Resource
{
    [Export] public int LengthBits { get; set; }
    [Export] public int WidthBits { get; set; }
    public RampDimensions Capture()
    {
        var result = new RampDimensions(new(Read(LengthBits)), new(Read(WidthBits)));
        result.Validate(); return result;
    }
    private static Half Read(int bits) => bits is >= 0 and <= ushort.MaxValue
        ? BitConverter.UInt16BitsToHalf((ushort)bits) : throw new ArgumentException("Invalid binary16 resource bits.");
}
