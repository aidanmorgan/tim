using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PartOrientationTests
{
    public enum InvalidWire { Euler, Short, Long, Null, NonNumber, Overflow, Scale, Shear, Reflection, Missing, OldField, UnknownField }
    private static string InvalidJson(InvalidWire value)=>value switch
    {
        InvalidWire.Euler=>"""{"orientation":[0,0,0]}""",
        InvalidWire.Short=>"""{"orientation":[1,0,0,0,1,0,0,0]}""",
        InvalidWire.Long=>"""{"orientation":[1,0,0,0,1,0,0,0,1,0]}""",
        InvalidWire.Null=>"""{"orientation":null}""",
        InvalidWire.NonNumber=>"""{"orientation":[1,0,0,0,1,0,0,0,"one"]}""",
        InvalidWire.Overflow=>"""{"orientation":[1e100,0,0,0,1,0,0,0,1]}""",
        InvalidWire.Scale=>"""{"orientation":[2,0,0,0,1,0,0,0,1]}""",
        InvalidWire.Shear=>"""{"orientation":[1,0,0,0.1,1,0,0,0,1]}""",
        InvalidWire.Reflection=>"""{"orientation":[-1,0,0,0,1,0,0,0,1]}""",
        InvalidWire.Missing=>"""{}""",
        InvalidWire.OldField=>"""{"rotation":[0,0,0],"orientation":[1,0,0,0,1,0,0,0,1]}""",
        InvalidWire.UnknownField=>"""{"orientation":[1,0,0,0,1,0,0,0,1],"unexpected":0}""",
        _=>throw new ArgumentOutOfRangeException(nameof(value))
    };
    public static TheoryData<InvalidWire> InvalidCases()
    {
        var cases=new TheoryData<InvalidWire>();
        foreach(var value in Enum.GetValues<InvalidWire>())cases.Add(value);
        return cases;
    }
    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void OnlyCanonicalRigidBasisIsAccepted(InvalidWire value)
        =>Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize(InvalidJson(value),MachineJson.Default.PartSpec));

    [Fact]
    public void InvalidFixtureAndConstructionInputsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>InvalidJson((InvalidWire)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartOrientation.FromEulerDegrees(float.NaN,0,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartOrientation.FromEulerDegrees(0,float.PositiveInfinity,0));
        Assert.Throws<ArgumentException>(()=>PartOrientation.FromEulerDegrees([1,2]));
        Assert.Throws<ArgumentException>(()=>new PartOrientation(default,default,default));
        Assert.Throws<ArgumentNullException>(()=>new PartSpec {Orientation=null!});
    }

    private static int[] Bits(PartOrientation value)=>new[]{value.X,value.Y,value.Z}
        .SelectMany(v=>new[]{v.X,v.Y,v.Z}).Select(BitConverter.SingleToInt32Bits).ToArray();

    [Theory]
    [InlineData(0,0,0)]
    [InlineData(25,40,15)]
    [InlineData(30,45,60)]
    [InlineData(89.5f,170,-179)]
    [InlineData(89.99f,170,-179)]
    [InlineData(-89.99f,170,-179)]
    [InlineData(90,35,70)]
    [InlineData(-90,35,70)]
    [InlineData(135,-215,405)]
    public void EditorConventionAndGestureRecipesMatchAndSerializationRetainsEveryBit(float x,float y,float z)
    {
        var authored=PartOrientation.FromEulerDegrees(x,y,z);
        var scene=Basis.FromEuler(new Vector3(x,y,z)*(Mathf.Pi/180));
        var basis=SceneOrientation.Present(authored);
        Assert.InRange((scene.X-basis.X).Length(),0,5e-7);
        Assert.InRange((scene.Y-basis.Y).Length(),0,5e-7);
        Assert.InRange((scene.Z-basis.Z).Length(),0,5e-7);
        var recipe=authored.ToEulerDegrees();
        var reconstructed=SceneOrientation.Present(PartOrientation.FromEulerDegrees(recipe.X,recipe.Y,recipe.Z));
        Assert.InRange((basis.X-reconstructed.X).Length(),0,2e-5);
        Assert.InRange((basis.Y-reconstructed.Y).Length(),0,2e-5);
        Assert.InRange((basis.Z-reconstructed.Z).Length(),0,2e-5);
        // Successive arbitrary-axis edits are preserved, not re-decomposed as Euler angles.
        basis=basis.Rotated(new Vector3(1,2,3).Normalized(),.173f);
        var spec=new PartSpec {Position=[1.25f,6,-2.125f],Orientation=SceneOrientation.Capture(basis)};
        var expected=Bits(spec.Orientation);
        var json=JsonSerializer.Serialize(spec,MachineJson.Default.PartSpec);
        for(var cycle=0;cycle<100;cycle++)
        {
            spec=JsonSerializer.Deserialize(json,MachineJson.Default.PartSpec)!;
            Assert.Equal(expected,Bits(spec.Orientation));
            Assert.Equal(basis,SceneOrientation.Present(spec.Orientation));
            Assert.Equal(json,JsonSerializer.Serialize(spec,MachineJson.Default.PartSpec));
        }
    }

    [Fact]
    public void CompositionTransformsSocketsWithoutAnEulerRoundTrip()
    {
        var a=PartOrientation.FromEulerDegrees(25,40,15);
        var b=PartOrientation.FromEulerDegrees(-70,35,110);
        var point=new System.Numerics.Vector3(.3f,-1.2f,.6f);
        Assert.InRange(System.Numerics.Vector3.Distance(a.Transform(b.Transform(point)),(a*b).Transform(point)),0,4e-7);
        var scene=SceneOrientation.Present(a)*SceneOrientation.Present(b);
        var combined=SceneOrientation.Present(a*b);
        Assert.Equal(scene,combined);
    }

    [Fact]
    public void ComposedQuarterAndHalfTurnRetainIndependentMatrixGolden()
    {
        // For the captured binary32 angles: c90=-4.371138828673793e-8, s180=-8.742277657347586e-8.
        // C270=c180*c90-s180*s90 rounds to bits 0x340ccde2.
        // Adding Euler angles first produces a different capture and is not composition.
        var combined=PartOrientation.FromEulerDegrees(0,180,0)*PartOrientation.FromEulerDegrees(0,90,0);
        int[] expected=[0x340ccde2,0,0x3f800000,0,0x3f800000,0,
            unchecked((int)0xbf800000),0,0x340ccde2];
        Assert.Equal(expected,Bits(combined));
        Assert.NotEqual(Bits(PartOrientation.FromEulerDegrees(0,270,0)),Bits(combined));
        var json=JsonSerializer.Serialize(new PartSpec {Orientation=combined},MachineJson.Default.PartSpec);
        var restored=JsonSerializer.Deserialize(json,MachineJson.Default.PartSpec)!;
        Assert.Equal(expected,Bits(restored.Orientation));
    }

    [Fact]
    public void RotationVectorMeasuresPhysicalArcNearEulerSingularity()
    {
        // Exercise the same rotation-vector operation used by placement assistance without a Node.
        var from=SceneGeometryAdapter.CaptureRigidPose(new(SceneOrientation.Present(PartOrientation.FromEulerDegrees(89,170,170)),default)).Rotation;
        var to=SceneGeometryAdapter.CaptureRigidPose(new(SceneOrientation.Present(PartOrientation.FromEulerDegrees(89,-170,-170)),default)).Rotation;
        var delta=(to*from.Inverse()).RotationVector();
        Assert.InRange(delta.Length,0,.02);
        var end=RigidRotation.FromRotationVector(delta)*from;
        Assert.InRange((to*end.Inverse()).RotationVector().Length,0,1e-12);
    }
}
