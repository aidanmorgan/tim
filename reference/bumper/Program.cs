using System.Buffers.Binary;
using System.Text.Json;
using CuriousContraptions.Gpu;

if (args is ["--boundaries"])
{
    BumperBoundaries.Emit();
    return;
}
if (args.Length == 3)
{
    using var raw = JsonDocument.Parse(File.ReadAllText(args[0]));
    var row = raw.RootElement.GetProperty(args[1]).EnumerateArray()
        .Single(value => value.GetProperty("name").GetString() == args[2]);
    var previous = Convert.FromBase64String(row.GetProperty("source").GetString()!);
    var count = 0;
    foreach (var state in row.GetProperty("states").EnumerateArray())
    {
        var next = Convert.FromBase64String(state.GetString()!);
        PhysicsGpuAbi.ValidateCandidate(next, previous,
            new(BinaryPrimitives.ReadUInt64LittleEndian(previous.AsSpan(40)) + 1));
        previous = next; count++;
    }
    Console.WriteLine(JsonSerializer.Serialize(new { valid = true, count }));
    return;
}
if (args is ["--work"])
{
    var paidProfile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
    var paidCases = new List<object>();
    foreach (var fixture in Enum.GetValues<WorkFixture>())
    {
        var speed = fixture == WorkFixture.StrongerNatural ? 20 : 4;
        var strength = fixture == WorkFixture.ZeroStrength ? (Half)0 : (Half)8;
        var ball = WorkshopInput.Basketball(new(1), 0, 5.1, 0, 0, 0, 0, 1);
        var bumper = WorkshopInput.Bumper(new(2), 0, 4, 0, 0, 0, 0, 1,
            BumperWork.FromCanonicalStrength(strength));
        var compiled = WorkshopPhysicsCompiler.Compile(new(new(1), WorkshopCadenceSettings.Default(),
            new(new IWorkshopInstance[] { ball, bumper })), new(15, 1));
        var bodies = compiled.Bodies.ToArray().Where(b => b.Id.Value <= 2).ToArray();
        for (var i = 0; i < bodies.Length; i++)
            if (bodies[i].Motion == RigidMotionKind.Dynamic)
                bodies[i] = bodies[i] with { Mass = new((Half)1), LinearDrag = new((Half)0),
                    Gravity = default, Velocity = new((Half)0, (Half)(-speed / 32d), (Half)0) };
        var colliders = compiled.Colliders.ToArray().Where(c => c.Body.Value <= 2).ToArray();
        var ids = colliders.Select(c => c.Material).ToHashSet();
        var materials = compiled.Materials.ToArray().Where(m => ids.Contains(m.Id)).ToArray();
        var work = compiled.ContactWorks[0];
        if (fixture == WorkFixture.Depleted) work = work with { InitialEnergy = new((Half)0) };
        if (fixture == WorkFixture.LastQuantum) work = work with { InitialEnergy = new(BitConverter.UInt16BitsToHalf(1)) };
        var scene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
            bodies, colliders, materials, [], [], [], [work]);
        var name = fixture switch
        {
            WorkFixture.Paid => "work-paid",
            WorkFixture.ZeroStrength => "work-zero",
            WorkFixture.Depleted => "work-depleted",
            WorkFixture.StrongerNatural => "work-stronger-natural",
            WorkFixture.LastQuantum => "work-last-quantum",
            _ => throw new ArgumentException()
        };
        paidCases.Add(new { name, ticks = 24,
            source = Convert.ToBase64String(PhysicsGpuAbi.Admission(scene, new(1), paidProfile)) });
    }
    Console.WriteLine(JsonSerializer.Serialize(new { stateBytes = PhysicsGpuAbi.ByteLength,
        preamble = PhysicsGpuAbi.ShaderPreamble(), cases = paidCases }));
    return;
}
var profile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
var cases = new List<object>();
var surfaceRadius = (Half).65;
var radiusSum = (double)surfaceRadius + (double)BasketballMaterial.Default.Radius.Value;
foreach (var fixture in Enum.GetValues<SphereFixture>())
{
    var x = fixture switch
    {
        SphereFixture.Miss => 2,
        SphereFixture.Graze => radiusSum - .01,
        SphereFixture.OffAxisRest => radiusSum * Math.Sin(Math.PI / 12),
        _ => 0d
    };
    var y = fixture switch
    {
        SphereFixture.Drop or SphereFixture.Miss or SphereFixture.Graze => 6d,
        SphereFixture.OffAxisRest => 4 + radiusSum * Math.Cos(Math.PI / 12),
        _ => 4 + radiusSum
    };
    var ball = WorkshopInput.Basketball(new(1), x, y, 0, 0, 0, 0, 1);
    var wall = WorkshopInput.Wall(new(2), 0, 4, 0, 0, 0, 0, 1,
        new(new((Half)1), new((Half)1), new((Half)1)));
    var compiled = WorkshopPhysicsCompiler.Compile(new(new(1), WorkshopCadenceSettings.Default(),
        new(new IWorkshopInstance[] { ball, wall })), new(15, 1));
    var colliders = compiled.Colliders.ToArray().Where(c => c.Body.Value <= 2).ToArray();
    for (var i = 0; i < colliders.Length; i++)
        if (colliders[i].Body.Value == 2)
            colliders[i] = colliders[i] with { Shape = ColliderShapeKind.Sphere,
                Radius = new(surfaceRadius), HalfExtents = default };
    var bodies = compiled.Bodies.ToArray().Where(b => b.Id.Value <= 2).ToArray();
    for (var i = 0; i < bodies.Length; i++)
        if (bodies[i].Motion == RigidMotionKind.Dynamic)
            bodies[i] = bodies[i] with { LinearDrag = new((Half)0),
                Velocity = fixture switch {
                    SphereFixture.Separating => new((Half)0, (Half)(2d / 32), (Half)0),
                    SphereFixture.FastTangentRelease => new((Half)(4d / 32), (Half)0, (Half)0),
                    _ => default },
                Gravity = fixture == SphereFixture.Stationary ? default : new((Half)0, (Half)(-9.81), (Half)0) };
    var materialIds = colliders.Select(c => c.Material).ToHashSet();
    var materials = compiled.Materials.ToArray().Where(m => materialIds.Contains(m.Id)).ToArray();
    var scene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
        bodies, colliders, materials, [], []);
    var name = fixture switch
    {
        SphereFixture.Drop => "sphere-drop",
        SphereFixture.Miss => "sphere-miss",
        SphereFixture.Graze => "sphere-graze",
        SphereFixture.ApexRest => "sphere-apex-rest",
        SphereFixture.OffAxisRest => "sphere-off-axis-rest",
        SphereFixture.Separating => "sphere-separating",
        SphereFixture.Stationary => "sphere-stationary",
        SphereFixture.FastTangentRelease => "sphere-fast-tangent-release",
        _ => throw new ArgumentException()
    };
    cases.Add(new { name, ticks = fixture == SphereFixture.Separating ? 1 : fixture == SphereFixture.FastTangentRelease ? 12 : 120,
        source = Convert.ToBase64String(PhysicsGpuAbi.Admission(scene, new(1), profile)) });
}
Console.WriteLine(JsonSerializer.Serialize(new { stateBytes = PhysicsGpuAbi.ByteLength,
    preamble = PhysicsGpuAbi.ShaderPreamble(), cases }));
enum SphereFixture { Drop, Miss, Graze, ApexRest, OffAxisRest, Separating, Stationary, FastTangentRelease }

enum WorkFixture { Paid, ZeroStrength, Depleted, StrongerNatural, LastQuantum }
