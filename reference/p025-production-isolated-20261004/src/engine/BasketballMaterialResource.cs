using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Resource boundary: integers encode canonical IEEE binary16, never wider game parameters.</summary>
[GlobalClass]
public partial class BasketballMaterialResource : Resource
{
    [Export] public int RadiusBits { get; set; }
    [Export] public int MassBits { get; set; }
    [Export] public int BounceBits { get; set; }
    [Export] public int DragBits { get; set; }
    [Export] public int BuoyancyBits { get; set; }
    public BasketballMaterial Capture()
    {
        var result = new BasketballMaterial(new(Read(RadiusBits)), new(Read(MassBits)),
            new(Read(BounceBits)), new(Read(DragBits)), new(Read(BuoyancyBits)));
        result.Validate();
        return result;
    }
    private static Half Read(int bits) => bits is >= 0 and <= ushort.MaxValue
        ? BitConverter.UInt16BitsToHalf((ushort)bits) : throw new ArgumentException("Invalid binary16 resource bits.");
}

