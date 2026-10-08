using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RequiredPhysicsParameterTests(NativeSceneFixture godot)
{
    public enum Fixture { Ball, Tennis, Bowling, Balloon, Ramp, Wall, Battery }
    private static string Catalogue(Fixture fixture)=>fixture switch
    {
        Fixture.Ball=>"ball",Fixture.Tennis=>"tennis",Fixture.Bowling=>"bowling",
        Fixture.Balloon=>"balloon",Fixture.Ramp=>"ramp",Fixture.Wall=>"wall",Fixture.Battery=>"battery",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static void Required<TParameter>(MachinePart part) where TParameter:struct,Enum
    {
        foreach(var parameter in Enum.GetValues<TParameter>())
        {
            var value=part.ReadParameter(parameter);
            var key=PartParameterName.Of(parameter); // Explicit resource dictionary boundary.
            Assert.Throws<NotSupportedException>(()=>((IDictionary<string,float>)part.Properties).Remove(key));
            var original=part.Definition;
            var specification=part.Serialize();
            using var incomplete=new PartDefinition {Parameters=new(original.Parameters)};
            Assert.True(incomplete.Parameters.Remove(key));
            var missing=part.Serialize();missing.Properties.Remove(key);
            try
            {
                part.Definition=incomplete;
                Assert.Throws<KeyNotFoundException>(()=>part.Configure(missing));
                Assert.Equal(value,part.ReadParameter(parameter));
            }
            finally {part.Definition=original;part.Configure(specification);}
            Assert.Equal(value,part.ReadParameter(parameter));
            part.ValidateParameters();
        }
    }

    [Theory]
    [InlineData(Fixture.Ball)]
    [InlineData(Fixture.Tennis)]
    [InlineData(Fixture.Bowling)]
    [InlineData(Fixture.Balloon)]
    [InlineData(Fixture.Ramp)]
    [InlineData(Fixture.Wall)]
    [InlineData(Fixture.Battery)]
    public void CurrentCatalogueIsCompleteAndMissingParametersReject(Fixture fixture)
    {
        var world=World();
        try
        {
            var id=Catalogue(fixture);
            var part=world.AddPart(new(){Id=id,Kind=id,Position=[0,6,0]});
            switch(fixture)
            {
                case Fixture.Ball or Fixture.Tennis or Fixture.Bowling or Fixture.Balloon:
                    Required<BallParameter>(part);
                    Assert.Equal(part.ReadParameter(BallParameter.Drag),part.Drag);
                    Assert.Equal(part.ReadParameter(BallParameter.Buoyancy),part.Buoyancy);
                    break;
                case Fixture.Ramp: Required<RampParameter>(part); break;
                case Fixture.Wall: Required<WallParameter>(part); break;
                case Fixture.Battery: Required<BatteryParameter>(part); break;
                default: throw new ArgumentOutOfRangeException(nameof(fixture));
            }
            var saved=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            world.Step();
            world.Restore();
            Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally {world.Free();}
    }

    [Fact]
    public void EveryCatalogueResourceBindsAndRoundTripsItsDeclaredSchema()
    {
        var registry=new PartRegistry();
        registry.Discover();
        Assert.NotEmpty(registry.Definitions);
        foreach(var definition in registry.Definitions.Values)
        {
            var part=registry.Create(new(){Id=definition.Id,Kind=definition.Id});
            try
            {
                godot.Tree.Root.AddChild(part);
                part.ValidateParameters();
                var saved=part.Serialize();
                part.Configure(saved);
                Assert.Equal(saved.Properties.OrderBy(pair=>pair.Key),part.Serialize().Properties.OrderBy(pair=>pair.Key));
            }
            finally {part.Free();}
        }
    }

    [Fact]
    public void TypedStorageRejectsOtherEnumsAndDetachesExternalDictionaries()
    {
        var fields=new Dictionary<string,float> {{PartParameterName.Of(PipeParameter.Length),3}};
        var values=PartParameterValues.Bind<PipeParameter>(fields);
        fields[PartParameterName.Of(PipeParameter.Length)]=7;
        Assert.Equal(3,values.Read(PipeParameter.Length));
        Assert.Throws<ArgumentException>(()=>values.Read(RampParameter.Length));
        Assert.Throws<ArgumentOutOfRangeException>(()=>values.Read((PipeParameter)999));
        Assert.Throws<NotSupportedException>(()=>((IDictionary<string,float>)values.Export()).Clear());
        fields.Add(PartParameterName.Of(ReceiverParameter.Threshold),1);
        Assert.Throws<ArgumentException>(()=>PartParameterValues.Bind<PipeParameter>(fields));
        Assert.Throws<ArgumentException>(()=>PartParameterValues.BindEmpty(fields));
        Assert.Throws<KeyNotFoundException>(()=>PartParameterValues.Bind<PipeParameter>(new Dictionary<string,float>()));
    }

    [Fact]
    public void ReplacedParameterBoundariesAreCanonicalAndRejectUnknownValues()
    {
        Assert.Equal("length",PartParameterName.Of(PipeParameter.Length));
        Assert.Equal("threshold",PartParameterName.Of(ReceiverParameter.Threshold));
        Assert.Equal("minimum_mass",PartParameterName.Of(PressurePlateParameter.MinimumMass));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((PipeParameter)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((ReceiverParameter)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((PressurePlateParameter)999));
    }
    [Fact]
    public void FixtureBoundaryRejectsUndefinedChoices()
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>Catalogue((Fixture)999));
}
