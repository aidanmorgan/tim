using System;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public enum ColliderQuerySource { PartProxy, BodyEnvelope }
public readonly record struct ColliderQueryChild(ConvexInstance Shape,SweepSurfaceKind Surface,bool Opaque,ColliderQuerySource Source);
internal sealed class ColliderQuerySet
{
    private readonly ColliderQueryChild[] _children;
    internal CompoundGeometry Geometry { get; }
    internal SweepSurfaceKind Surface(ColliderChildId child)=>_children[child.Index].Surface;
    internal ColliderQuerySet(ColliderQueryChild[] children)
    {
        _children=children;
        Geometry=new(children.Select(c=>c.Shape).ToArray());
    }
}

/// <summary>Immutable body-local collision and query declaration. Runtime updates
/// supply this value directly; they never recapture scene nodes or construction proxies.</summary>
public sealed class BodyQueryGeometry
{
    private readonly ColliderQueryChild[] _children;
    public BodyQueryPolicy Policy { get; }
    public int Count=>_children.Length;
    public CompoundGeometry Geometry=>All.Geometry;
    public ReadOnlySpan<ColliderQueryChild> Children=>_children;
    public ColliderQueryChild this[ColliderChildId child]
    {
        get
        {
            if(child.Index<0||child.Index>=_children.Length)throw new ArgumentOutOfRangeException(nameof(child));
            return _children[child.Index];
        }
    }
    public BodyQueryGeometry WithChild(ColliderChildId child,ColliderQueryChild replacement)
    {
        var current=this[child];
        if(current==replacement)return this;
        var changed=(ColliderQueryChild[])_children.Clone();
        changed[child.Index]=replacement;
        return new(Policy,changed);
    }
    internal ColliderQuerySet All { get; }
    private readonly ColliderQuerySet? _withoutEnvelope;
    private readonly CompoundGeometry? _opaque;
    public BodyQueryGeometry(BodyQueryPolicy policy,ReadOnlySpan<ColliderQueryChild> children)
    {
        if(!Enum.IsDefined(policy)) throw new ArgumentOutOfRangeException(nameof(policy));
        _children=children.ToArray();
        foreach(var child in _children)
        {
            ArgumentNullException.ThrowIfNull(child.Shape.Geometry);
            if(!Enum.IsDefined(child.Surface)||child.Surface==SweepSurfaceKind.None)
                throw new ArgumentOutOfRangeException(nameof(children),"A collider requires a defined surface kind.");
            if(!Enum.IsDefined(child.Source))
                throw new ArgumentOutOfRangeException(nameof(children),"A collider requires a defined source kind.");
        }
        Policy=policy; All=new(_children);
        var proxies=_children.Where(c=>c.Source==ColliderQuerySource.PartProxy).ToArray();
        _withoutEnvelope=proxies.Length==0?null:proxies.Length==_children.Length?All:new(proxies);
        var opaque=_children.Where(c=>c.Opaque).Select(c=>c.Shape).ToArray();
        _opaque=opaque.Length==0?null:opaque.Length==_children.Length?All.Geometry:new(opaque);
    }
    internal ColliderQuerySet? Solids(SweepBodyMode mode)=>mode switch
    {
        SweepBodyMode.IncludeBodies=>All,
        SweepBodyMode.ExcludeBodies=>Policy==BodyQueryPolicy.ExcludeFromStaticQueries?null:_withoutEnvelope,
        SweepBodyMode.ExcludeDynamicBodies=>_withoutEnvelope,
        _=>throw new ArgumentOutOfRangeException(nameof(mode))
    };
    internal CompoundGeometry? For(TraceMedium medium)=>medium switch
    {
        TraceMedium.Light or TraceMedium.Sound=>_opaque,
        TraceMedium.Air=>All.Geometry,
        _=>throw new ArgumentOutOfRangeException(nameof(medium))
    };
}

