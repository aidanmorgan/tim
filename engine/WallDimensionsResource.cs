using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

[GlobalClass]
public partial class WallDimensionsResource : Resource
{
    [Export] public int WidthBits { get; set; }
    [Export] public int HeightBits { get; set; }
    [Export] public int ThicknessBits { get; set; }
    public WallDimensions Capture()
    {
        var result = new WallDimensions(new(Read(WidthBits)), new(Read(HeightBits)), new(Read(ThicknessBits)));
        result.Validate(); return result;
    }
    private static Half Read(int bits) => bits is >= 0 and <= ushort.MaxValue
        ? BitConverter.UInt16BitsToHalf((ushort)bits) : throw new ArgumentException("Invalid binary16 resource bits.");
}
