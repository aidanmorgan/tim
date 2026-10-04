using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using CuriousContraptions.Gpu;

if (args.Length == 3)
{
    using var raw = JsonDocument.Parse(File.ReadAllText(args[0]));
    var row = raw.RootElement.GetProperty(args[1]).EnumerateArray().Single(x => x.GetProperty("name").GetString() == args[2]);
    var source = Convert.FromBase64String(row.GetProperty("source").GetString()!);
    var count = 0;
    foreach (var item in row.GetProperty("states").EnumerateArray())
    {
        var candidate = Convert.FromBase64String(item.GetString()!);
        PhysicsGpuAbi.ValidateCandidate(candidate, source, new(BinaryPrimitives.ReadUInt64LittleEndian(source.AsSpan(40)) + 1));
        source = candidate; count++;
    }
    Console.WriteLine(JsonSerializer.Serialize(new { valid = true, count }));
    return;
}
var profile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
var cases = new List<object>();
foreach (var fixture in Enum.GetValues<WallFixture>())
{
    var dimensions = fixture == WallFixture.DefaultFace ? WallDimensions.Default : WallDimensions.Maximum;
    var rotation = fixture == WallFixture.RotatedEdge
        ? Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(.3f, .2f, -.35f)) : Quaternion.Identity;
    var wallCentre = new Vector3(0, fixture == WallFixture.RotatedEdge ? 4.5f : 3.5f, 0);
    var position = fixture switch
    {
        WallFixture.DefaultFace => new Vector3(0, 6, 0),
        WallFixture.MaximumFace => new Vector3(0, 8, 0),
        WallFixture.RotatedEdge => wallCentre + Vector3.Transform(new Vector3(4.3f, 3, 0), rotation) + Vector3.UnitY,
        WallFixture.Miss => new Vector3(6, 8, 0),
        WallFixture.InitialOverlap => wallCentre,
        _ => throw new ArgumentOutOfRangeException()
    };
    var wall = WorkshopInput.Wall(new(2), wallCentre.X, wallCentre.Y, wallCentre.Z,
        rotation.X, rotation.Y, rotation.Z, rotation.W, dimensions);
    var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Basketball(new(1), position.X, position.Y, position.Z, 0, 0, 0, 1), wall));
    var scene = WorkshopPhysicsCompiler.Compile(construction, new(66, 1));
    cases.Add(new { name = Name(fixture), ticks = fixture == WallFixture.InitialOverlap ? 0 : 120,
        expectedAdmission = fixture == WallFixture.InitialOverlap ? PhysicsFailure.InvalidDeclaration : PhysicsFailure.None,
        ball = new[] { position.X, position.Y, position.Z },
        wallCentre = new[] { wallCentre.X, wallCentre.Y, wallCentre.Z },
        rotation = new[] { (float)wall.Rotation.X, (float)wall.Rotation.Y, (float)wall.Rotation.Z, (float)wall.Rotation.W },
        dimensions = new[] { (float)dimensions.Width.Value, (float)dimensions.Height.Value, (float)dimensions.Thickness.Value },
        source = Convert.ToBase64String(PhysicsGpuAbi.Admission(scene, new(1), profile)) });
}
Console.WriteLine(JsonSerializer.Serialize(new { stateBytes = PhysicsGpuAbi.ByteLength,
    preamble = PhysicsGpuAbi.ShaderPreamble(), cases }));

static string Name(WallFixture fixture) => fixture switch
{
    WallFixture.DefaultFace => "default-face", WallFixture.MaximumFace => "maximum-face",
    WallFixture.RotatedEdge => "rotated-edge", WallFixture.Miss => "miss",
    WallFixture.InitialOverlap => "initial-overlap", _ => throw new ArgumentOutOfRangeException(nameof(fixture))
};
enum WallFixture { DefaultFace, MaximumFace, RotatedEdge, Miss, InitialOverlap }
