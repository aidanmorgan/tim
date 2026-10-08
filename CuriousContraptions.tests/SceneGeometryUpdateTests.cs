using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class SceneGeometryUpdateTests
{
    public enum InvalidDeclaration { UndefinedPolicy, Empty, MissingShape, MissingSurface, UndefinedSurface, UndefinedSource }
    private static ColliderQueryChild Box()=>new(new(new ConvexBox(new(1,1,1)),AffineTransform.Identity),
        SweepSurfaceKind.Box,true,ColliderQuerySource.PartProxy);
    public static TheoryData<InvalidDeclaration> InvalidCases()
    {
        var cases=new TheoryData<InvalidDeclaration>();
        foreach(var value in Enum.GetValues<InvalidDeclaration>())cases.Add(value);
        return cases;
    }
    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void InvalidDeclarationsAreRejectedBeforePublication(InvalidDeclaration value)
    {
        var child=Box();
        Assert.ThrowsAny<ArgumentException>(()=>
        {
            _=value switch
            {
                InvalidDeclaration.UndefinedPolicy=>new BodyQueryGeometry((BodyQueryPolicy)999,[child]),
                InvalidDeclaration.Empty=>new BodyQueryGeometry(BodyQueryPolicy.Include,[]),
                InvalidDeclaration.MissingShape=>new BodyQueryGeometry(BodyQueryPolicy.Include,[default]),
                InvalidDeclaration.MissingSurface=>new BodyQueryGeometry(BodyQueryPolicy.Include,[child with {Surface=SweepSurfaceKind.None}]),
                InvalidDeclaration.UndefinedSurface=>new BodyQueryGeometry(BodyQueryPolicy.Include,[child with {Surface=(SweepSurfaceKind)999}]),
                InvalidDeclaration.UndefinedSource=>new BodyQueryGeometry(BodyQueryPolicy.Include,[child with {Source=(ColliderQuerySource)999}]),
                _=>throw new ArgumentOutOfRangeException(nameof(value))
            };
        });
    }

    [Fact]
    public void SceneCaptureRejectsMissingBodySlot()
    {
        Assert.Throws<ArgumentNullException>(()=>new SceneBodyGeometryDeclaration(null!,[Box()]));
    }

    [Fact]
    public void BodyLocalDeclarationsCopyInputsAndReplacementPreservesUnchangedChildren()
    {
        var box=Box();
        var envelope=new ColliderQueryChild(new(new ConvexSphere(.3),AffineTransform.Identity),
            SweepSurfaceKind.Body,false,ColliderQuerySource.BodyEnvelope);
        var supplied=new[]{box,envelope};
        var original=new BodyQueryGeometry(BodyQueryPolicy.Include,supplied);
        supplied[0]=default;supplied[1]=default;
        Assert.Equal(box,original[new(0)]);Assert.Equal(envelope,original[new(1)]);
        Assert.Equal(2,original.Count);Assert.Same(box.Shape.Geometry,original.Geometry[new(0)].Geometry);
        Assert.Same(original,original.WithChild(new(0),box));
        var opaque=envelope with {Opaque=true,Source=ColliderQuerySource.PartProxy};
        var changed=original.WithChild(new(1),opaque);
        Assert.NotSame(original,changed);Assert.Equal(original.Policy,changed.Policy);
        Assert.Equal(box,changed[new(0)]);Assert.Equal(opaque,changed[new(1)]);
        Assert.Equal(envelope,original[new(1)]);
        Assert.Same(original.Geometry,original.For(TraceMedium.Air));
        Assert.Equal(1,original.For(TraceMedium.Light)!.Count);
        Assert.Equal(1,original.For(TraceMedium.Sound)!.Count);
        Assert.Equal(1,original.Solids(SweepBodyMode.ExcludeBodies)!.Geometry.Count);
        Assert.Equal(1,original.Solids(SweepBodyMode.ExcludeDynamicBodies)!.Geometry.Count);
        Assert.Same(changed.Geometry,changed.For(TraceMedium.Light));
        Assert.Same(changed.Geometry,changed.For(TraceMedium.Sound));
        Assert.Same(changed.Geometry,changed.Solids(SweepBodyMode.ExcludeBodies)!.Geometry);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void UnknownChildIndicesRejectInsteadOfReplacingAnotherChild(int index)
    {
        var geometry=new BodyQueryGeometry(BodyQueryPolicy.Include,[Box()]);
        Assert.Throws<ArgumentOutOfRangeException>(()=>geometry[new(index)]);
        Assert.Throws<ArgumentOutOfRangeException>(()=>geometry.WithChild(new(index),Box()));
        Assert.Equal(1,geometry.Count);
    }

    [Fact]
    public void QueryModesAndMediaRemainValidatedEnums()
    {
        var geometry=new BodyQueryGeometry(BodyQueryPolicy.Include,[Box()]);
        Assert.Throws<ArgumentOutOfRangeException>(()=>geometry.For((TraceMedium)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>geometry.Solids((SweepBodyMode)999));
    }
}
