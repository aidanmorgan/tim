using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Resource boundary: integers encode canonical IEEE binary16, never wider game parameters. One resource type declares every ball kind.</summary>
[GlobalClass]
public partial class BallMaterialResource : Resource
{
    [Export] public int RadiusBits { get; set; }
    [Export] public int MassBits { get; set; }
    [Export] public int BounceBits { get; set; }
    [Export] public int DragBits { get; set; }
    [Export] public int BuoyancyBits { get; set; }
    [Export] public int FrictionBits { get; set; }
    [Export] public int BounceThresholdBits { get; set; }
    [Export] public int RollingResistanceBits { get; set; }
    /// <summary>The catalogue bits must equal the declared material of the named kind bit-for-bit.</summary>
    public BallMaterial Capture(WorkshopPartKind kind)
    {
        var result = new BallMaterial(new(Read(RadiusBits)), new(Read(MassBits)),
            new(Read(BounceBits)), new(Read(DragBits)), new(Read(BuoyancyBits)), new(Read(FrictionBits)), new(Read(BounceThresholdBits)),
            new(Read(RollingResistanceBits)));
        result.Validate(kind);
        return result;
    }
    private static Half Read(int bits) => bits is >= 0 and <= ushort.MaxValue
        ? BitConverter.UInt16BitsToHalf((ushort)bits) : throw new ArgumentException("Invalid binary16 resource bits.");
}
