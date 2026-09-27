using Godot;
using System;

namespace CuriousContraptions;

[Flags]
public enum ResizeAxes { None = 0, X = 1, Y = 2, Z = 4, All = X | Y | Z }

public interface IResizablePart
{
    Vector3 Dimensions { get; }
    ResizeAxes ResizableAxes { get; }
    void SetDimensions(Vector3 dimensions);
}
