using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BumperTests(NativeSceneFixture godot)
{
    private enum Role { Ball, Other, Miss, Bumper, PlacedBumper }
    private static string Id(Role role)=>role switch
    {
        Role.Ball=>"ball",Role.Other=>"other",Role.Miss=>"miss",
        Role.Bumper=>"bumper",Role.PlacedBumper=>"bumper_1",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Kind(Role role)=>role switch
    {
        Role.Ball or Role.Other or Role.Miss=>"ball",
        Role.Bumper or Role.PlacedBumper=>"bumper",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static readonly NodePath RingPath=new("Visual/ImpactRing");
    private const string PuzzleResource="res://content/puzzles.json";
    private const string PuzzleId="bumper_sidekick";
    private MachineWorld World()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(-1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 0, -1)]
    public void RoundBumperLaunchesAwayFromContactInEveryAxis(int x, int y, int z)
    {
        var world = World();
        try
        {
            var normal = new Vector3(x, y, z);
            var center = new Vector3(0, 6, 0);
            var start = center + normal * 2;
            world.LoadMachine(new()
            {
                Gravity = 0, Pressure = 0,
                Parts = [
                    new() { Id = Id(Role.Ball), Kind = Kind(Role.Ball), Position = [start.X, start.Y, start.Z] },
                    new() { Id = Id(Role.Bumper), Kind = Kind(Role.Bumper), Position = [0, 6, 0], Orientation = PartOrientation.FromEulerDegrees(30, 40, 20) }
                ]
            });
            var ball = world.FindPart(Id(Role.Ball))!;
            var bumper = (BumperPart)world.FindPart(Id(Role.Bumper))!;
            ball.InitialVelocity = -normal * 4;
            world.Start();
            for (var tick = 0; tick < 100 && bumper.HitCount == 0; tick++) world.Step();
            Assert.Equal(1, bumper.HitCount);
            Assert.True(CollisionVector.Dot(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity,SceneGeometryAdapter.CaptureVector(normal)) > 7.99f);
            Assert.True(ball.Position.DistanceTo(center) >= ball.Radius + .65f);
            Assert.Contains(new MachineEvent(MachineEventKind.Bumped, Id(Role.Bumper), Id(Role.Ball)), world.Events.Keys);
            Assert.Empty(bumper.Boxes);
            Assert.Single(bumper.Spheres);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void GlancingContactTransfersMotionToSpinAndAnotherDepthPlaneMisses()
    {
        var world = World();
        try
        {
            world.LoadMachine(new()
            {
                Gravity = 0, Pressure = 0,
                Parts = [
                    new() { Id = Id(Role.Ball), Kind = Kind(Role.Ball), Position = [0, 4.991f, 0] },
                    new() { Id = Id(Role.Miss), Kind = Kind(Role.Ball), Position = [0, 5, 2] },
                    new() { Id = Id(Role.Bumper), Kind = Kind(Role.Bumper), Position = [0, 4, 0] }
                ]
            });
            world.FindPart(Id(Role.Ball))!.InitialVelocity = new(2, -4, 0);
            world.FindPart(Id(Role.Miss))!.InitialVelocity = new(0, -4, 0);
            world.Start();
            world.Step();
            var ball = world.FindPart(Id(Role.Ball))!;
            var body=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var bumper=world.FindPart(Id(Role.Bumper))!;
            var target=world.PhysicsAssembly.Body(new(bumper,MachinePart.RootBody));
            var hit=Assert.Single(world.TickImpacts.ToArray(),
                impact=>impact.Pair.A==target.Id||impact.Pair.B==target.Id);
            var surface=hit.Pair.A==target.Id?hit.Separation.PointA:hit.Separation.PointB;
            var offset=surface-target.Center;
            var normal=offset/offset.Length;
            CollisionVector Tangent(CollisionVector value)=>value-normal*CollisionVector.Dot(value,normal);
            // Contact friction transfers tangential momentum into solid-sphere
            // spin. The subsequent central radial launch must not undo that.
            var reconstructed=Tangent(body.LinearVelocity)+
                CollisionVector.Cross(body.AngularVelocity,normal)*(.4*ball.Radius);
            Assert.InRange((reconstructed-Tangent(new(2,-4,0))).Length,0,1e-5);
            Assert.True(body.AngularVelocity.Length>1);
            Assert.True(CollisionVector.Dot(body.LinearVelocity,normal)>7.99);
            for (var i = 0; i < 90; i++) world.Step();
            Assert.DoesNotContain(new MachineEvent(MachineEventKind.Bumped, Id(Role.Bumper), Id(Role.Miss)), world.Events.Keys);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void CooldownIsPerBallAndPulseFinishesAfterPhysicsStops()
    {
        var world=World();
        try
        {
            world.LoadMachine(new()
            {
                Gravity=0,Pressure=0,
                Parts=[
                    new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[0,4.992f,0]},
                    new(){Id=Id(Role.Other),Kind=Kind(Role.Other),Position=[1.2f,4,0]},
                    new(){Id=Id(Role.Bumper),Kind=Kind(Role.Bumper),Position=[0,4,0]}
                ]
            });
            var bumper=(BumperPart)world.FindPart(Id(Role.Bumper))!;
            var ball=world.FindPart(Id(Role.Ball))!;
            var other=world.FindPart(Id(Role.Other))!;
            ball.InitialVelocity=Vector3.Down*2;
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            void Aim(MachinePart part,Vector3 velocity)
            {
                var body=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
                world.Physics.ApplyImpulse(body.Id,(SceneGeometryAdapter.CaptureVector(velocity)-body.LinearVelocity)/body.InverseMass,body.Center);
            }
            void UntilHit(int expected)
            {
                for(var i=0;i<100&&bumper.HitCount<expected;i++) world.Step();
                Assert.Equal(expected,bumper.HitCount);
            }
            UntilHit(1);
            Aim(ball,Vector3.Down*20);
            for(var i=0;i<3;i++) world.Step();
            Assert.Equal(1,bumper.HitCount);
            Assert.True(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Y>0); // Suppressed effect still gets ordinary collision response.
            world.PresentFrame(.08,1);
            var existingPulse=world.ReadOccurrenceFeedback(new(bumper,BumperPart.ImpactOccurrence),0).Animation.Value;
            Aim(other,Vector3.Left*2);
            UntilHit(2);
            world.PresentFrame(0,1);
            Assert.Equal(existingPulse,world.ReadOccurrenceFeedback(new(bumper,BumperPart.ImpactOccurrence),0).Animation.Value);
            var shape=bumper.Spheres.Single();
            world.Running=false;
            var state=world.Physics.Capture();
            world.PresentFrame(.16,1);
            Assert.InRange(world.ReadOccurrenceFeedback(new(bumper,BumperPart.ImpactOccurrence),0).Animation.Value,1.2376,1.24);
            Assert.True(bumper.GetNode<Node3D>(RingPath).Scale.X>1.2f);
            world.PresentFrame(.2,1);
            Assert.Equal(1,world.ReadOccurrenceFeedback(new(bumper,BumperPart.ImpactOccurrence),0).Animation.Value);
            Assert.Equal(Vector3.One,bumper.GetNode<Node3D>(RingPath).Scale);
            Assert.Equal(shape,bumper.Spheres.Single());
            Assert.Equal(state.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            world.Running=true;
            for(var i=0;i<18;i++) world.Step();
            Aim(ball,Vector3.Down*40);
            UntilHit(3);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var restored=world.Parts.OfType<BumperPart>().Single();
            Assert.Equal(0,restored.HitCount);
            Assert.False(restored.Active);
            Assert.Equal(Vector3.One,restored.GetNode<Node3D>(RingPath).Scale);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(0f, true)]
    [InlineData(.45f, true)]
    [InlineData(1f, false)]
    public void PlacementNudgingAloneRescuesTheSameImperfectBumper(float precision, bool expectedWin)
    {
        var world = World();
        try
        {
            var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString(PuzzleResource))
                .Single(p => p.Id == PuzzleId);
            var data = MachineCodec.Clone(puzzle.CreateMachine());
            data.Parts.AddRange(puzzle.Solution);
            data = MachineCodec.Clone(data);
            // Freeze capture and every other assistance rule at strict defaults.
            // Only the bumper's authored placement curve can differ between runs.
            var placed = data.Parts.Single(p => p.Id == Id(Role.PlacedBumper));
            foreach (var part in data.Parts.Where(p => p != placed)) part.Difficulty.Clear();
            placed.Position = [-3.1104352f, 1.4996231f, .03465762f];
            world.Precision = precision;
            world.LoadMachine(data);
            var initial = world.FindPart(Id(Role.PlacedBumper))!.Position;
            world.Start();
            Assert.Equal(initial, world.FindPart(Id(Role.PlacedBumper))!.Position);
            world.Step();
            Assert.InRange(initial.DistanceTo(world.FindPart(Id(Role.PlacedBumper))!.Position), 0, .001f);
            for (var i = 1; i < 3600 && world.Running; i++) world.Step();
            Assert.Equal(expectedWin, world.Won);
            Assert.Contains(new MachineEvent(MachineEventKind.Bumped, Id(Role.PlacedBumper), Id(Role.Ball)), world.Events.Keys);
            Assert.Equal(9.81f, world.Gravity);
            world.PresentFrame(0,1); // Runtime scene poses are written only by presentation.
            if (precision == 1) Assert.Equal(initial, world.FindPart(Id(Role.PlacedBumper))!.Position);
            else Assert.True(world.FindPart(Id(Role.PlacedBumper))!.Position.DistanceTo(new(-3.2f, 1.5f, 0)) < .0001f);
            world.Restore();
            Assert.Equal(initial, world.FindPart(Id(Role.PlacedBumper))!.Position);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void StationaryOrSeparatingBodiesDoNotTriggerAndReplayIsIdentical()
    {
        var world = World();
        try
        {
            var machine = new MachineData
            {
                Gravity = 0, Pressure = 0,
                Parts = [
                    new() { Id = Id(Role.Ball), Kind = Kind(Role.Ball), Position = [0, 4.991f, 0] },
                    new() { Id = Id(Role.Bumper), Kind = Kind(Role.Bumper), Position = [0, 4, 0] }
                ]
            };
            foreach (var speed in new[] { 0f, 2f })
            {
                world.LoadMachine(machine);
                world.FindPart(Id(Role.Ball))!.InitialVelocity = Vector3.Up * speed;
                world.Start();
                world.Step();
                Assert.Equal(0, ((BumperPart)world.FindPart(Id(Role.Bumper))!).HitCount);
            }
            world.LoadMachine(machine);
            world.FindPart(Id(Role.Ball))!.InitialVelocity = Vector3.Down * 4;
            world.Start();
            var signatures = new List<string>();
            for (var i = 0; i < 120; i++) { world.Step(); signatures.Add(world.StateSignature()); }
            world.Restore();
            world.FindPart(Id(Role.Ball))!.InitialVelocity = Vector3.Down * 4;
            world.Start();
            for (var i = 0; i < 120; i++) { world.Step(); Assert.Equal(signatures[i], world.StateSignature()); }
        }
        finally { world.Free(); }
    }
    [Fact]
    public void FixtureBoundaryRejectsUndefinedRoles()
    {
        Assert.Equal("bumper",Kind(Role.Bumper));
        Assert.Equal("ball",Kind(Role.Other));
        Assert.Equal("bumper_1",Id(Role.PlacedBumper));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Kind((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Id((Role)999));
    }

}
