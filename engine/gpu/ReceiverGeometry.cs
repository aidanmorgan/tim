using System;

namespace CuriousContraptions.Gpu;

public readonly record struct AuthoredBox(MetreVector Centre, MetreVector HalfExtents);

/// <summary>Shared canonical catalogue geometry consumed by declaration compilation and mesh adapters.</summary>
public static class ReceiverGeometry
{
    private static readonly AuthoredBox[] Boxes =
    [
        new(new((Half)0, (Half)(-.45), (Half)0), new((Half).75, (Half).075, (Half).75)),
        new(new((Half)(-.75), (Half)0, (Half)0), new((Half).06, (Half).5, (Half).8)),
        new(new((Half).75, (Half)0, (Half)0), new((Half).06, (Half).5, (Half).8)),
        new(new((Half)0, (Half)0, (Half)(-.75)), new((Half).75, (Half).5, (Half).06)),
        new(new((Half)0, (Half)(-.2), (Half).75), new((Half).75, (Half).3, (Half).06))
    ];
    public static ReadOnlySpan<AuthoredBox> Walls => Boxes;
}
