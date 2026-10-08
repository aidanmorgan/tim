using System;
using Godot;

namespace CuriousContraptions.Presentation;

public enum ScalarExtentAxis { X, Y, Z }

/// <summary>Cosmetic single-axis fill around a local anchor. No physical geometry is changed.</summary>
public sealed record ScalarExtentDefinition
{
    public ScalarExtentAxis Axis { get; }
    public double Anchor { get; }
    public double MinimumScale { get; }
    public double InitialFraction { get; }
    public ScalarExtentDefinition(ScalarExtentAxis axis,double anchor,double minimumScale,double initialFraction)
    {
        if(!Enum.IsDefined(axis))throw new ArgumentOutOfRangeException(nameof(axis));
        if(!double.IsFinite(anchor)||Math.Abs(anchor)>1e6)throw new ArgumentOutOfRangeException(nameof(anchor));
        if(!double.IsFinite(minimumScale)||minimumScale<1e-6||minimumScale>1)
            throw new ArgumentOutOfRangeException(nameof(minimumScale));
        if(!double.IsFinite(initialFraction)||initialFraction<0||initialFraction>1)
            throw new ArgumentOutOfRangeException(nameof(initialFraction));
        Axis=axis;Anchor=anchor;MinimumScale=minimumScale;InitialFraction=initialFraction;
    }
    internal Transform3D Compose(Transform3D baseline,double fraction)
    {
        if(!double.IsFinite(fraction)||fraction<0||fraction>1)throw new ArgumentOutOfRangeException(nameof(fraction));
        var initial=Math.Max(MinimumScale,InitialFraction);
        var requested=Math.Max(MinimumScale,fraction);
        if(requested==initial)return baseline;
        var scale=(float)(requested/initial);
        var basis=baseline.Basis;
        var axis=Axis switch
        {
            ScalarExtentAxis.X=>Vector3.Right,ScalarExtentAxis.Y=>Vector3.Up,ScalarExtentAxis.Z=>Vector3.Back,
            _=>throw new InvalidOperationException("Unsupported extent axis.")
        };
        var origin=baseline.Origin+baseline.Basis*axis*(float)(Anchor*(initial-requested)/initial);
        switch(Axis)
        {
            case ScalarExtentAxis.X:basis.X*=scale;break;
            case ScalarExtentAxis.Y:basis.Y*=scale;break;
            case ScalarExtentAxis.Z:basis.Z*=scale;break;
            default:throw new InvalidOperationException("Unsupported extent axis.");
        }
        var result=new Transform3D(basis,origin);
        SceneAnimationAdapter.ValidateTransform(result);
        return result;
    }
}
