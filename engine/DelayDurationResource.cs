using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;
[GlobalClass]
public partial class DelayDurationResource : Resource
{
    [Export] public int SecondsBits { get; set; }
    public DelayDuration Capture()
    {
        if (SecondsBits is < 0 or > ushort.MaxValue) throw new ArgumentException("Invalid canonical duration bits.");
        var value = new DelayDuration(new(BitConverter.UInt16BitsToHalf((ushort)SecondsBits)));
        value.Validate(); return value;
    }
}
