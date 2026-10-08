using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

[GlobalClass]
public partial class PipeDimensionsResource : Resource
{
    [Export] public int LengthBits { get; set; }
    public PipeDimensions Capture()
    {
        if (LengthBits is < 0 or > ushort.MaxValue)
            throw new ArgumentException("Invalid binary16 resource bits.");
        var result = new PipeDimensions(new(BitConverter.UInt16BitsToHalf((ushort)LengthBits)));
        result.Validate(); return result;
    }
}
