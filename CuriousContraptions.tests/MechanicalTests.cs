using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class MechanicalTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }

    [Fact]
    public void BeltArtworkFollowsSocketTransformsAndOnlyMovesMarksWhileRunning()
    {
        var world = World();
        try
        {
            var battery = world.AddPart(new() { Id = "battery", Kind = "battery" });
            var motor = world.AddPart(new() { Id = "motor", Kind = "motor", Position = [-3, 1, 0] });
            var belt = world.AddPart(new() { Id = "belt", Kind = "conveyor", Position = [3, 2, 0] });
            Assert.True(world.Connect(battery, motor));
            Assert.True(world.Connect(motor, belt));
            var output = motor.ConnectionPorts.Single(p => p.Id == SocketId.Drive);
            var input = belt.ConnectionPorts.Single(p => p.Id == SocketId.DriveIn);
            var artwork = new MechanicalBeltVisual { World = world, Source = motor, Target = belt, Output = output, Input = input };
            world.AddChild(artwork);
            var lines = artwork.GetChildren().OfType<MeshInstance3D>().Where(n => n.Mesh is CylinderMesh).ToArray();
            var marks = artwork.GetChildren().OfType<MeshInstance3D>().Where(n => n.Mesh is SphereMesh).ToArray();
            Assert.Equal(4, lines.Length);
            Assert.Equal(2, marks.Length);
            var original = marks[0].Position;
            artwork._Process(.1);
            Assert.Equal(original, marks[0].Position);
            world.Start();
            world.Step();
            artwork._Process(.1);
            Assert.NotEqual(original, marks[0].Position);
            belt.Position += new Vector3(0, .1f, .2f);
            belt.RotationDegrees = new(20, 45, 30);
            artwork._Process(0);
            var midpoint = (motor.Transform * output.LocalPosition + belt.Transform * input.LocalPosition) * .5f;
            Assert.InRange(((lines[0].Position + lines[1].Position) * .5f - midpoint).Length(), 0, .0001f);
            var rate = motor.MechanicalSpeed(SocketId.Drive) * .16f;
            artwork._Process((1 - .00001f - rate * .1f) / rate);
            var beforeSeam = marks[0].Position;
            artwork._Process(.00002f / rate);
            Assert.InRange(marks[0].Position.DistanceTo(beforeSeam), 0, .001f);
            world.Running = false;
            var stopped = marks[0].Position;
            artwork._Process(.5);
            Assert.Equal(stopped, marks[0].Position);
            artwork.Free();
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public void ConveyorsRelaySignedDriveWithoutOrderDependence(bool reverseOrder, int reversers)
    {
        var world = World();
        try
        {
            var battery = world.AddPart(new() { Id = "battery", Kind = "battery" });
            var motor = (MotorPart)world.AddPart(new() { Id = reverseOrder ? "z_motor" : "a_motor", Kind = "motor" });
            var first = (ConveyorPart)world.AddPart(new() { Id = "first", Kind = "conveyor" });
            var last = (ConveyorPart)world.AddPart(new() { Id = "last", Kind = "conveyor", Rotation = [30, 90, 20] });
            Assert.True(world.Connect(battery, motor));
            Assert.True(world.Connect(motor, first));
            MachinePart previous = first;
            for (var i = 0; i < reversers; i++)
            {
                var reverse = world.AddPart(new() { Id = "reverse_" + i, Kind = "reverse_transmission" });
                Assert.True(world.Connect(previous, reverse));
                previous = reverse;
            }
            Assert.True(world.Connect(previous, last));
            if (reverseOrder) world.Connections.Reverse();
            var saved = world.Snapshot();
            world.Start();
            // Assert the same substep, not only the eventual steady state.
            world.Step();
            var sign = reversers % 2 == 0 ? 1 : -1;
            Assert.Equal(motor.ShaftSpeed, first.ShaftSpeed);
            Assert.Equal(motor.ShaftSpeed * sign, last.ShaftSpeed);
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(6, first.ShaftSpeed, 3);
            Assert.Equal(6 * sign, last.MechanicalSpeed(SocketId.Drive), 3);
            Assert.Equal(4 * sign, last.SurfaceSpeed, 3);
            Assert.All(world.Parts.OfType<ReverseTransmissionPart>(), r =>
            {
                Assert.Equal(-r.InputSpeed, r.OutputSpeed);
                Assert.InRange(Mathf.Abs(Mathf.AngleDifference(-r.InputAngle, r.OutputAngle)), 0, .0001f);
            });
            battery.Properties["enabled"] = 0;
            world.Step();
            Assert.InRange(Mathf.Abs(last.ShaftSpeed), .001f, 5.999f);
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(0, last.ShaftSpeed);
            Assert.False(last.Active);
            world.Restore();
            Assert.Equal(saved.Connections.Count, world.Connections.Count);
            Assert.All(world.Parts.OfType<ConveyorPart>(), c =>
            {
                Assert.Equal(0, c.ShaftSpeed);
                Assert.Equal(0, c.ShaftAngle);
                Assert.Equal(0, c.MechanicalSpeed(SocketId.Drive));
            });
            Assert.All(world.Parts.OfType<ReverseTransmissionPart>(), r =>
            {
                Assert.Equal(0, r.InputAngle);
                Assert.Equal(0, r.OutputAngle);
            });
            world.Start();
            world.Step();
            Assert.True(((ConveyorPart)world.FindPart("last")!).ShaftSpeed * sign > 0);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void DisconnectedConveyorsCannotCreateDriveAndRemovingUpstreamLinkStopsTheChain()
    {
        var world = World();
        try
        {
            var battery = world.AddPart(new() { Id = "battery", Kind = "battery" });
            var motor = world.AddPart(new() { Id = "motor", Kind = "motor" });
            var first = (ConveyorPart)world.AddPart(new() { Id = "first", Kind = "conveyor" });
            var last = (ConveyorPart)world.AddPart(new() { Id = "last", Kind = "conveyor" });
            Assert.True(world.Connect(first, last));
            world.Start();
            world.Activate(first); // An activation command is not mechanical energy.
            world.Step();
            Assert.Equal(0, first.SurfaceSpeed);
            Assert.Equal(0, last.SurfaceSpeed);
            world.Restore();
            battery = world.FindPart("battery")!;
            motor = world.FindPart("motor")!;
            first = (ConveyorPart)world.FindPart("first")!;
            last = (ConveyorPart)world.FindPart("last")!;
            Assert.True(world.Connect(battery, motor));
            Assert.True(world.Connect(motor, first));
            world.Start();
            world.Step();
            Assert.True(last.ShaftSpeed > 0);
            world.Connections.RemoveAll(c => c.From == "motor");
            world.Step();
            Assert.Equal(0, first.ShaftSpeed);
            Assert.Equal(0, last.ShaftSpeed);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void MechanicalGraphRejectsLoopsCompetingDriversAndWrongSockets()
    {
        var world = World();
        try
        {
            var motor = world.AddPart(new() { Id = "motor", Kind = "motor" });
            var other = world.AddPart(new() { Id = "other", Kind = "motor" });
            var a = world.AddPart(new() { Id = "a", Kind = "conveyor" });
            var b = world.AddPart(new() { Id = "b", Kind = "reverse_transmission" });
            var c = world.AddPart(new() { Id = "c", Kind = "conveyor" });
            Assert.False(world.Connect(motor, SocketId.Drive, a, SocketId.PowerIn, ConnectionDomain.Mechanical));
            Assert.False(world.Connect(motor, SocketId.Drive, a, SocketId.DriveIn, ConnectionDomain.Electrical));
            Assert.True(world.Connect(a, b));
            Assert.False(world.Connect(b, a)); // No source yet: a loop is still unsupported.
            Assert.True(world.Connect(motor, a));
            Assert.False(world.Connect(other, a));
            Assert.True(world.Connect(motor, c)); // Explicit fan-out is allowed.
            var bad = world.Snapshot();
            bad.Connections.Add(new() { From = "other", To = "a", Type = ConnectionDomain.Mechanical,
                FromPort = SocketId.Drive, ToPort = SocketId.DriveIn });
            Assert.Throws<ArgumentException>(() => world.LoadMachine(bad));
            Assert.Same(a, world.FindPart("a"));
            Assert.Equal(3, world.Connections.Count);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData("powered")]
    [InlineData("speed")]
    public void ObsoleteSelfPoweredConveyorPropertiesAreRejected(string property)
    {
        var world = World();
        try
        {
            Assert.Throws<ArgumentException>(() => world.AddPart(new()
                { Id = "belt", Kind = "conveyor", Properties = new() { [property] = 1 } }));
            Assert.Empty(world.Parts);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.45f)]
    [InlineData(1f)]
    public void MechanicalLessonsRequireTheirDriveLinksAndReverseLessonRequiresOppositeTravel(float precision)
    {
        var world = World();
        try
        {
            foreach (var id in new[] { "conveyor_courier", "belt_relay", "reverse_belt" })
            {
                var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json")).Single(p => p.Id == id);
                var solution = puzzle.CreateMachine();
                solution.Parts.AddRange(puzzle.Solution);
                solution.Connections = puzzle.SolutionConnections;
                world.Precision = precision;
                world.LoadMachine(solution);
                world.Start();
                for (var tick = 0; tick < 1500 && world.Running; tick++) world.Step();
                Assert.True(world.Won, id + " reference");
                foreach (var omitted in puzzle.SolutionConnections)
                {
                    var missing = MachineCodec.Clone(solution);
                    missing.Connections.RemoveAll(c => c.From == omitted.From && c.To == omitted.To);
                    world.LoadMachine(missing);
                    world.Start();
                    for (var tick = 0; tick < 1500 && world.Running; tick++) world.Step();
                    Assert.False(world.Won, id + " must need " + omitted.From + " -> " + omitted.To);
                }
                if (id == "reverse_belt")
                {
                    var bypass = MachineCodec.Clone(solution);
                    bypass.Connections.RemoveAll(c => c.Type == ConnectionDomain.Mechanical);
                    bypass.Connections.Add(new() { From = "motor", To = "conveyor_1",
                        Type = ConnectionDomain.Mechanical, FromPort = SocketId.Drive, ToPort = SocketId.DriveIn });
                    world.LoadMachine(bypass);
                    world.Start();
                    for (var tick = 0; tick < 1500 && world.Running; tick++) world.Step();
                    Assert.False(world.Won);
                    Assert.True(((ConveyorPart)world.FindPart("conveyor_1")!).SurfaceSpeed > 0);
                }
            }
        }
        finally { world.Free(); }
    }
}
