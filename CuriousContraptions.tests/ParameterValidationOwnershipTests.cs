using Godot;
using System.Text.Json;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ParameterValidationOwnershipTests(NativeSceneFixture godot)
{
    public enum Element { Motor, Spring, Pusher }
    private readonly record struct CatalogueId(string Value);
    private static CatalogueId Catalogue(Element element)=>element switch
    {
        Element.Motor=>new("motor"),Element.Spring=>new("spring"),Element.Pusher=>new("linear_pusher"),
        _=>throw new ArgumentOutOfRangeException(nameof(element))
    };
    private static CatalogueId Optical(LogicGateKind kind)=>kind switch
    {
        LogicGateKind.And=>new("optical_and"),LogicGateKind.Or=>new("optical_or"),
        LogicGateKind.Xor=>new("optical_xor"),LogicGateKind.Nor=>new("optical_nor"),
        LogicGateKind.Nand=>new("optical_nand"),_=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static void Set<T>(PartSpec spec,T parameter,float value) where T:struct,Enum =>
        spec.Properties[PartParameterName.Of(parameter)]=value;

    [Theory]
    [InlineData(Element.Motor)]
    [InlineData(Element.Spring)]
    [InlineData(Element.Pusher)]
    public void RejectedNumericEditPreservesCompleteAuthoredState(Element element)
    {
        var world=World();
        try
        {
            var catalogue=Catalogue(element);
            var part=world.AddPart(new(){Id=catalogue.Value,Kind=catalogue.Value,Position=[0,6,0]});
            var saved=Saved(world);
            var replacement=part.Serialize();
            replacement.Id=FixtureParts.Id(FixturePartId.Second);
            replacement.Locked=!part.Locked;
            replacement.Position=[3,4,5];
            replacement.InitialVelocity=[1,2,3];
            replacement.Difficulty=[new(){Precision=.7f}];
            switch(element)
            {
                case Element.Motor: Set(replacement,MotorParameter.Torque,float.NaN);break;
                case Element.Spring: Set(replacement,SpringParameter.Stiffness,0);break;
                case Element.Pusher: Set(replacement,PusherParameter.Speed,0);break;
                default: throw new ArgumentOutOfRangeException(nameof(element));
            }
            Assert.Throws<ArgumentException>(()=>part.Configure(replacement));
            Assert.Equal(saved,Saved(world));
            part.ValidateParameters();
            world.Start();world.Step();world.Restore();
            Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(LogicGateKind.And)]
    [InlineData(LogicGateKind.Or)]
    [InlineData(LogicGateKind.Xor)]
    [InlineData(LogicGateKind.Nor)]
    [InlineData(LogicGateKind.Nand)]
    public void ValidationCannotReplaceCapturedOpticalControl(LogicGateKind kind)
    {
        var world=World();
        try
        {
            var catalogue=Optical(kind);
            var gate=(OpticalLogicPart)world.AddPart(new(){Id=catalogue.Value,Kind=catalogue.Value,Position=[0,6,0]});
            var saved=Saved(world);
            world.Start();
            gate.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>
                {[OpticalPortId.First]=Vector3.One,[OpticalPortId.Second]=Vector3.Zero});
            gate.BeforeNetworks(world);
            var participants=gate.RuntimeState.ToArray();
            var state=(gate.First,gate.Second,gate.IsOpen);
            gate.ValidateParameters();
            Assert.Equal(participants,gate.RuntimeState.ToArray());
            Assert.Equal(state,(gate.First,gate.Second,gate.IsOpen));
            world.Restore();
            Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void FixtureBoundariesRejectUnsupportedValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>Catalogue((Element)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Optical((LogicGateKind)999));
    }
}
