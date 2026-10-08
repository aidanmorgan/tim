using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Explicit lossless conversion between authored data and scene presentation.</summary>
public static class SceneOrientation
{
    public static PartOrientation Capture(Basis basis)=>new(
        new(basis.X.X,basis.X.Y,basis.X.Z),new(basis.Y.X,basis.Y.Y,basis.Y.Z),new(basis.Z.X,basis.Z.Y,basis.Z.Z));
    public static Basis Present(PartOrientation orientation)
    {
        ArgumentNullException.ThrowIfNull(orientation);
        return new(new(orientation.X.X,orientation.X.Y,orientation.X.Z),
            new(orientation.Y.X,orientation.Y.Y,orientation.Y.Z),new(orientation.Z.X,orientation.Z.Y,orientation.Z.Z));
    }
}
