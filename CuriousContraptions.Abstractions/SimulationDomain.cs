using System;

namespace CuriousContraptions.Abstractions;

[Flags]
public enum SimulationDomain : uint
{
    None = 0,
    Mechanical = 1 << 0, // 1
    Electrical = 1 << 1, // 2
    Signal = 1 << 2,     // 4
    Fluid = 1 << 3,      // 8
    Audio = 1 << 4,      // 16
    Rope = 1 << 5        // 32
}
