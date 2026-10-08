using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ConstructionLifecycleTests(NativeSceneFixture godot)
{
    public enum RunState { Running, Paused }
    public enum Edit { Add, Attach, Remove, Disconnect, StartAgain }
    private enum Fixture { Battery, Motor, Ball }
    public enum ResizableFixture { Wall, Pipe }
    private static string Kind(ResizableFixture fixture)=>fixture switch
    {
        ResizableFixture.Wall=>"wall",ResizableFixture.Pipe=>"pipe",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    public enum InvalidAttachment { MissingDefinition, MissingIdentity }
    private static string Kind(Fixture fixture)=>fixture switch
    {
        Fixture.Battery=>"battery",Fixture.Motor=>"motor",Fixture.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private static PartSpec Spec(Fixture fixture)=>new()
    {
        Id=Kind(fixture),Kind=Kind(fixture),Position=fixture switch
        {
            Fixture.Battery=>[-4,6,0],Fixture.Motor=>[0,6,0],Fixture.Ball=>[4,6,0],
            _=>throw new ArgumentOutOfRangeException(nameof(fixture))
        }
    };
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);return world;
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static void SetRunState(MachineWorld world,RunState state)=>world.Running=state switch
    {
        RunState.Running=>true,RunState.Paused=>false,
        _=>throw new ArgumentOutOfRangeException(nameof(state))
    };

    public enum InvalidConfiguration { NullPosition, ShortPosition, LongPosition, NonfinitePosition, NullDifficulty, NullKnot, NullProperties, NullSpecification }
    [Theory]
    [InlineData(InvalidConfiguration.NullPosition)]
    [InlineData(InvalidConfiguration.ShortPosition)]
    [InlineData(InvalidConfiguration.LongPosition)]
    [InlineData(InvalidConfiguration.NonfinitePosition)]
    [InlineData(InvalidConfiguration.NullDifficulty)]
    [InlineData(InvalidConfiguration.NullKnot)]
    [InlineData(InvalidConfiguration.NullProperties)]
    [InlineData(InvalidConfiguration.NullSpecification)]
    public void MalformedConfigurationPreservesConstructionAndReset(InvalidConfiguration invalid)
    {
        var world=World();
        try
        {
            var part=world.AddPart(Spec(Fixture.Ball));
            var saved=Saved(world);
            var difficulty=part.Difficulty;
            var transform=part.Transform;
            var replacement=part.Serialize();
            replacement.Id=Kind(Fixture.Motor);
            replacement.Locked=!part.Locked;
            replacement.InitialVelocity=[1,2,3];
            replacement.Position=[9,8,7];
            replacement.Difficulty=[new(){Precision=.5f}];
            switch(invalid)
            {
                case InvalidConfiguration.NullPosition: replacement.Position=null!;break;
                case InvalidConfiguration.ShortPosition: replacement.Position=[1,2];break;
                case InvalidConfiguration.LongPosition: replacement.Position=[1,2,3,4];break;
                case InvalidConfiguration.NonfinitePosition: replacement.Position=[1,float.NaN,3];break;
                case InvalidConfiguration.NullDifficulty: replacement.Difficulty=null!;break;
                case InvalidConfiguration.NullKnot: replacement.Difficulty=[null!];break;
                case InvalidConfiguration.NullProperties: replacement.Properties=null!;break;
                case InvalidConfiguration.NullSpecification: replacement=null!;break;
                default: throw new ArgumentOutOfRangeException(nameof(invalid));
            }
            Assert.ThrowsAny<ArgumentException>(()=>part.Configure(replacement));
            Assert.Equal(saved,Saved(world));
            Assert.Equal(transform,part.Transform);
            Assert.Same(difficulty,part.Difficulty);
            world.Start();world.Step();world.Restore();
            Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(ResizableFixture.Wall,RunState.Running)]
    [InlineData(ResizableFixture.Wall,RunState.Paused)]
    [InlineData(ResizableFixture.Pipe,RunState.Running)]
    [InlineData(ResizableFixture.Pipe,RunState.Paused)]
    public void ResizingRequiresResetAndCannotRewriteCapturedGeometry(ResizableFixture fixture,RunState state)
    {
        var world=World();
        try
        {
            var id=Kind(fixture);
            var part=world.AddPart(new(){Id=id,Kind=id,Position=[0,6,0]});
            var resize=Assert.IsAssignableFrom<IResizablePart>(part);
            var initial=resize.Dimensions;
            var edited=new Vector3(initial.X+.5f,initial.Y,initial.Z);
            var saved=Saved(world);
            var boxes=part.Boxes.ToArray();
            var tubes=part.Tubes.ToArray();
            world.Start();
            SetRunState(world,state);
            var physics=world.Physics;
            var bodies=physics.Capture().BodyStates.ToArray();
            var owned=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
            var collider=physics.Collider(owned.Id).Declaration;
            Assert.Throws<InvalidOperationException>(()=>resize.SetDimensions(edited));
            Assert.Equal(initial,resize.Dimensions);
            Assert.Equal(saved,Saved(world));
            Assert.Equal(boxes,part.Boxes.ToArray());
            Assert.Equal(tubes,part.Tubes.ToArray());
            Assert.Same(physics,world.Physics);
            Assert.Equal(bodies,physics.Capture().BodyStates.ToArray());
            Assert.Equal(collider,physics.Collider(owned.Id).Declaration);
            world.Restore();
            Assert.Equal(saved,Saved(world));
            part=world.FindPart(id)!;
            resize=Assert.IsAssignableFrom<IResizablePart>(part);
            resize.SetDimensions(edited);
            Assert.Equal(edited,resize.Dimensions);
            var editedSave=Saved(world);
            Assert.NotEqual(saved,editedSave);
            world.Start();
            world.Step();
            world.Restore();
            Assert.Equal(editedSave,Saved(world));
            Assert.Equal(edited,Assert.IsAssignableFrom<IResizablePart>(world.FindPart(id)).Dimensions);
        }
        finally {world.Free();}
    }

    [Fact]
    public void ResizableFixtureBoundaryRejectsUndefinedValues()
    {
        Assert.Equal("wall",Kind(ResizableFixture.Wall));
        Assert.Equal("pipe",Kind(ResizableFixture.Pipe));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Kind((ResizableFixture)999));
    }

    [Theory]
    [InlineData(RunState.Running,false)]
    [InlineData(RunState.Paused,false)]
    [InlineData(RunState.Running,true)]
    [InlineData(RunState.Paused,true)]
    public void CapturedPartsCannotBeReconfiguredThroughRootOrInternalBody(RunState state,bool internalBody)
    {
        var world=World();
        try
        {
            var id=internalBody?WoundSpringPart.CatalogId:Kind(Fixture.Ball);
            var root=world.AddPart(new(){Id=id,Kind=id,Position=[0,6,0]});
            var part=internalBody?Assert.Single(root.InternalBodies):root;
            var before=part.Serialize();
            var construction=Saved(world);
            var replacement=part.Serialize();
            replacement.Id=Kind(Fixture.Motor);
            replacement.Position=[5,8,2];
            replacement.Orientation = PartOrientation.FromEulerDegrees(30,40,50);
            replacement.Locked=!part.Locked;
            world.Start();
            SetRunState(world,state);
            var physics=world.Physics;
            var bodyStates=physics.Capture().BodyStates.ToArray();
            var properties=part.Properties;
            var difficulty=part.Difficulty;
            var transform=part.Transform;
            Assert.Throws<InvalidOperationException>(()=>part.Configure(replacement));
            Assert.Throws<InvalidOperationException>(()=>part.SetDifficulty([]));
            Assert.Equal(before.Id,part.Uid);
            Assert.Equal(before.Locked,part.Locked);
            Assert.Equal(transform,part.Transform);
            Assert.Equal(properties.OrderBy(pair=>pair.Key),part.Properties.OrderBy(pair=>pair.Key));
            Assert.Same(difficulty,part.Difficulty);
            Assert.Equal(construction,Saved(world));
            Assert.Same(physics,world.Physics);
            Assert.Equal(bodyStates,physics.Capture().BodyStates.ToArray());
            world.Restore();
            Assert.Equal(construction,Saved(world));
            root=world.FindPart(id)!;
            part=internalBody?Assert.Single(root.InternalBodies):root;
            part.Configure(part.Serialize());
            Assert.Equal(construction,Saved(world));
            world.Start();
            world.Restore();
            Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(RunState.Running,false)]
    [InlineData(RunState.Paused,false)]
    [InlineData(RunState.Running,true)]
    [InlineData(RunState.Paused,true)]
    public void ConstructionPreparationRejectsCapturedRootAndInternalBodies(RunState state,bool internalBody)
    {
        var world=World();
        try
        {
            var root=world.AddPart(new(){Id=WoundSpringPart.CatalogId,Kind=WoundSpringPart.CatalogId,Position=[0,6,0]});
            var construction=Saved(world);
            world.Start();
            SetRunState(world,state);
            var part=internalBody?Assert.Single(root.InternalBodies):root;
            var physics=world.Physics;
            var before=physics.Capture();
            var rootTransform=root.Transform;
            var head=Assert.Single(root.InternalBodies);
            var headTransform=head.Transform;
            Assert.Throws<InvalidOperationException>(()=>part.PrepareForPhysicsCapture());
            Assert.Equal(rootTransform,root.Transform);
            Assert.Equal(headTransform,head.Transform);
            Assert.Equal(construction,Saved(world));
            Assert.Same(physics,world.Physics);
            Assert.Equal(before.BodyStates.ToArray(),physics.Capture().BodyStates.ToArray());
            world.Restore();
            Assert.Equal(construction,Saved(world));
            root=world.FindPart(WoundSpringPart.CatalogId)!;
            root.PrepareForPhysicsCapture();
            Assert.Single(root.InternalBodies).PrepareForPhysicsCapture();
            world.Start();
            world.Step();
            world.Restore();
            Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void BodyMembershipIsReadOnlyAndTracksConstructionAndReset()
    {
        var world=World();
        var foreign=World();
        try
        {
            var parts=world.Parts;
            var placed=Assert.IsAssignableFrom<ICollection<MachinePart>>(parts);
            Assert.True(placed.IsReadOnly);
            var view=world.Bodies;
            var ball=world.AddPart(Spec(Fixture.Ball));
            var other=foreign.AddPart(Spec(Fixture.Ball));
            Assert.Same(ball,Assert.Single(view));
            Assert.Same(ball,Assert.Single(parts));
            Assert.Throws<NotSupportedException>(()=>placed.Clear());
            Assert.Throws<NotSupportedException>(()=>placed.Add(other));
            var collection=Assert.IsAssignableFrom<ICollection<MachinePart>>(view);
            Assert.True(collection.IsReadOnly);
            Assert.Throws<NotSupportedException>(()=>collection.Clear());
            Assert.Throws<NotSupportedException>(()=>collection.Add(other));
            Assert.Throws<ArgumentException>(()=>world.RemovePart(other));
            Assert.Same(other,Assert.Single(foreign.Bodies));
            Assert.True(GodotObject.IsInstanceValid(other));
            world.Start();
            Assert.Throws<NotSupportedException>(()=>collection.Remove(ball));
            world.Running=false;
            Assert.Throws<NotSupportedException>(()=>collection.Clear());
            world.Restore();
            Assert.Same(view,world.Bodies);
            Assert.Same(parts,world.Parts);
            var restored=Assert.Single(view);
            Assert.NotSame(ball,restored);
            world.RemovePart(restored);
            Assert.Empty(view);
            Assert.Empty(parts);
        }
        finally {world.Free();foreign.Free();}
    }

    [Fact]
    public void AttachmentRejectsDuplicateAndParentedNodesWithoutTakingOwnership()
    {
        var world=World();
        var foreign=World();
        var duplicate=world.Registry.Create(Spec(Fixture.Ball));
        try
        {
            var ball=world.AddPart(Spec(Fixture.Ball));
            var other=foreign.AddPart(Spec(Fixture.Battery));
            Assert.Throws<ArgumentException>(()=>world.AttachPart(duplicate));
            Assert.Null(duplicate.GetParent());
            Assert.True(GodotObject.IsInstanceValid(duplicate));
            Assert.Throws<ArgumentException>(()=>world.AttachPart(other));
            Assert.Same(foreign,other.GetParent());
            Assert.Same(ball,Assert.Single(world.Parts));
            Assert.Same(ball,Assert.Single(world.Bodies));
            Assert.Same(other,Assert.Single(foreign.Parts));
        }
        finally {duplicate.Free();world.Free();foreign.Free();}
    }

    [Theory]
    [InlineData(InvalidAttachment.MissingDefinition)]
    [InlineData(InvalidAttachment.MissingIdentity)]
    public void UnconfiguredAttachmentsRejectWithoutMutation(InvalidAttachment fault)
    {
        var world=World();
        var part=new MachinePart();
        try
        {
            switch(fault)
            {
                case InvalidAttachment.MissingDefinition:break;
                case InvalidAttachment.MissingIdentity:part.Definition=new PartDefinition();break;
                default:throw new ArgumentOutOfRangeException(nameof(fault));
            }
            Assert.ThrowsAny<ArgumentException>(()=>world.AttachPart(part));
            Assert.Null(part.GetParent());
            Assert.Empty(world.Parts);
            Assert.Empty(world.Bodies);
            Assert.True(GodotObject.IsInstanceValid(part));
        }
        finally {part.Free();world.Free();}
    }

    [Fact]
    public void SupplyContactChangesPowerWithoutChangingTopologyAndResetRestoresConstruction()
    {
        var world=World();
        try
        {
            var battery=world.AddPart(Spec(Fixture.Battery));
            var motor=world.AddPart(Spec(Fixture.Motor));
            var supply=new SupplyControl(world,battery);
            var originalContact=supply.Output;
            Assert.True(world.Connect(supply.Output,motor));
            var saved=Saved(world);
            var links=world.Connections.ToArray();
            world.Start();world.Step();
            Assert.False(motor.HasElectricalPower(SocketId.PowerIn));
            var time=world.Physics.Time;
            Assert.Throws<ArgumentOutOfRangeException>(()=>supply.SetAndSettle((SimulationLatchPhase)999));
            Assert.Equal(time,world.Physics.Time);
            supply.SetAndSettle(SimulationLatchPhase.On);
            Assert.True(motor.HasElectricalPower(SocketId.PowerIn));
            supply.SetAndSettle(SimulationLatchPhase.Off);
            Assert.False(motor.HasElectricalPower(SocketId.PowerIn));
            supply.SetAndSettle(SimulationLatchPhase.On);
            Assert.True(motor.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(links,world.Connections.ToArray());
            world.Restore();
            Assert.Equal(saved,Saved(world));
            Assert.Equal(links,world.Connections.ToArray());
            world.Start();world.Step();
            Assert.False(world.FindPart(Kind(Fixture.Motor))!.HasElectricalPower(SocketId.PowerIn));
            Assert.NotSame(originalContact,supply.Output);
            supply.SetAndSettle(SimulationLatchPhase.On);
            Assert.True(world.FindPart(Kind(Fixture.Motor))!.HasElectricalPower(SocketId.PowerIn));
            supply.SetAndSettle(SimulationLatchPhase.Off);
            Assert.False(world.FindPart(Kind(Fixture.Motor))!.HasElectricalPower(SocketId.PowerIn));
            world.Restore();
            Assert.Equal(saved,Saved(world));
            world.RemovePart(supply.Output);
            Assert.Throws<InvalidOperationException>(()=>supply.Output);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(RunState.Running)]
    [InlineData(RunState.Paused)]
    public void ConnectionMembershipIsReadOnlyAndTracksResetAndConstruction(RunState state)
    {
        var world=World();
        try
        {
            var view=world.Connections;
            var collection=Assert.IsAssignableFrom<ICollection<ConnectionSpec>>(view);
            var indexed=Assert.IsAssignableFrom<IList<ConnectionSpec>>(view);
            Assert.True(collection.IsReadOnly);
            var battery=world.AddPart(Spec(Fixture.Battery));
            var motor=world.AddPart(Spec(Fixture.Motor));
            Assert.True(world.Connect(battery,motor));
            var link=Assert.Single(view);
            var saved=Saved(world);
            var detached=world.Snapshot();
            detached.Connections.Clear();
            Assert.Equal(link,Assert.Single(view));
            world.Start();
            SetRunState(world,state);
            var physics=world.Physics;
            Assert.Throws<NotSupportedException>(()=>collection.Add(link));
            Assert.Throws<NotSupportedException>(()=>collection.Remove(link));
            Assert.Throws<NotSupportedException>(()=>collection.Clear());
            Assert.Throws<NotSupportedException>(()=>indexed[0]=link);
            Assert.Throws<NotSupportedException>(()=>indexed.RemoveAt(0));
            Assert.Same(physics,world.Physics);
            Assert.Equal(saved,Saved(world));
            Assert.Equal(link,Assert.Single(view));
            world.Restore();
            Assert.Same(view,world.Connections);
            Assert.Equal(link,Assert.Single(view));
            Assert.True(world.Disconnect(link));
            Assert.Empty(view);
            Assert.True(world.Connect(world.FindPart(Kind(Fixture.Battery))!,world.FindPart(Kind(Fixture.Motor))!));
            world.RemovePart(world.FindPart(Kind(Fixture.Motor))!);
            Assert.Empty(view);
            world.LoadMachine(detached);
            Assert.Same(view,world.Connections);
            Assert.Empty(view);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(RunState.Running)]
    [InlineData(RunState.Paused)]
    public void ExportedSnapshotsCannotRewriteResetConstruction(RunState state)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(Spec(Fixture.Battery));
            var motor=world.AddPart(Spec(Fixture.Motor));
            var ball=world.AddPart(Spec(Fixture.Ball));
            ball.InitialVelocity=Vector3.Right;
            Assert.True(world.Connect(battery,motor));
            var saved=Saved(world);
            Assert.False(world.HasConstructionSnapshot);
            world.Start();
            SetRunState(world,state);
            Assert.True(world.HasConstructionSnapshot);
            var exported=world.Snapshot();
            exported.Parts[0].Position[0]=100;
            exported.Parts.Clear();
            exported.Connections.Clear();
            exported.Gravity=100;
            exported.Pressure=100;
            Assert.Equal(saved,Saved(world));
            Assert.True(world.HasConstructionSnapshot);
            world.Restore();
            Assert.False(world.HasConstructionSnapshot);
            Assert.Equal(saved,Saved(world));
            Assert.Equal(Vector3.Right,world.FindPart(Kind(Fixture.Ball))!.InitialVelocity);
            world.Start();
            world.Step();
            world.Restore();
            Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void FixtureIdentityBoundaryIsCanonicalAndRejectsUnknownValues()
    {
        var ids=Enum.GetValues<FixturePartId>();
        Assert.Equal(ids.Length,ids.Select(FixtureParts.Id).Distinct().Count());
        Assert.Equal("fixture_first",FixtureParts.Id(FixturePartId.First));
        Assert.Equal("fixture_second",FixtureParts.Id(FixturePartId.Second));
        Assert.Equal("fixture_third",FixtureParts.Id(FixturePartId.Third));
        Assert.Equal("fixture_fourth",FixtureParts.Id(FixturePartId.Fourth));
        Assert.Throws<ArgumentOutOfRangeException>(()=>FixtureParts.Id((FixturePartId)(-1)));
    }

    [Theory]
    [InlineData(RunState.Running,Edit.Add)]
    [InlineData(RunState.Paused,Edit.Add)]
    [InlineData(RunState.Running,Edit.Attach)]
    [InlineData(RunState.Paused,Edit.Attach)]
    [InlineData(RunState.Running,Edit.Remove)]
    [InlineData(RunState.Paused,Edit.Remove)]
    [InlineData(RunState.Running,Edit.Disconnect)]
    [InlineData(RunState.Paused,Edit.Disconnect)]
    [InlineData(RunState.Running,Edit.StartAgain)]
    [InlineData(RunState.Paused,Edit.StartAgain)]
    public void CapturedTopologyRejectsConstructionActionsWithoutMutation(RunState state,Edit edit)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(Spec(Fixture.Battery));
            var motor=world.AddPart(Spec(Fixture.Motor));
            Assert.True(world.Connect(battery,motor));
            world.Start();SetRunState(world,state);
            var physics=world.Physics;var hadConstruction=world.HasConstructionSnapshot;var saved=Saved(world);
            var bodies=physics.Capture().BodyStates.ToArray();
            Assert.Throws<InvalidOperationException>(()=>
            {
                switch(edit)
                {
                    case Edit.Add:world.AddPart(Spec(Fixture.Ball));break;
                    case Edit.Attach:
                        var candidate=world.Registry.Create(Spec(Fixture.Ball));
                        try {world.AttachPart(candidate);} finally {if(candidate.GetParent() is null)candidate.Free();}
                        break;
                    case Edit.Remove:world.RemovePart(motor);break;
                    case Edit.Disconnect:world.Disconnect(world.Connections.Single());break;
                    case Edit.StartAgain:world.Start();break;
                    default:throw new ArgumentOutOfRangeException(nameof(edit));
                }
            });
            Assert.Same(physics,world.Physics);Assert.Equal(hadConstruction,world.HasConstructionSnapshot);
            Assert.Equal(saved,Saved(world));Assert.Equal(bodies,physics.Capture().BodyStates.ToArray());
            Assert.Equal(state==RunState.Running,world.Running);
            Assert.Equal(2,world.Parts.Count);Assert.Single(world.Connections);
            Assert.True(GodotObject.IsInstanceValid(motor));
            world.Restore();
            Assert.Equal(saved,Saved(world));
            Assert.False(world.HasConstructionSnapshot);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(RunState.Running)]
    [InlineData(RunState.Paused)]
    public void ConnectingRequiresResetEvenWhenSimulationIsPaused(RunState state)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(Spec(Fixture.Battery));
            var motor=world.AddPart(Spec(Fixture.Motor));
            world.Start();SetRunState(world,state);
            Assert.False(world.Connect(battery,motor));
            Assert.False(world.Connect(battery,SocketId.Supply,motor,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.Empty(world.Connections);
            world.Restore();
            battery=world.FindPart(Kind(Fixture.Battery))!;motor=world.FindPart(Kind(Fixture.Motor))!;
            Assert.True(world.Connect(battery,motor));
            var link=world.Connections.Single();
            Assert.True(world.Disconnect(link));
            Assert.False(world.Disconnect(link));
            Assert.Empty(world.Connections);
            Assert.True(world.Connect(battery,motor));
            var ball=world.AddPart(Spec(Fixture.Ball));world.RemovePart(ball);
            Assert.Equal(2,world.Parts.Count);
            world.Start();world.Step();
            Assert.True(motor.HasElectricalPower(SocketId.PowerIn));
        }
        finally {world.Free();}
    }
}
