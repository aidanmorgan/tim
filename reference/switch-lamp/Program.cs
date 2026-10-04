using System.Buffers.Binary;
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
foreach (var fixture in Enum.GetValues<ContactFixture>())
{
    var speed = fixture switch
    {
        ContactFixture.Below => BitConverter.UInt16BitsToHalf((ushort)(BitConverter.HalfToUInt16Bits((Half).8) - 1)),
        ContactFixture.Equal => (Half).8,
        ContactFixture.Above => BitConverter.UInt16BitsToHalf((ushort)(BitConverter.HalfToUInt16Bits((Half).8) + 1)),
        _ => (Half)0
    };
    var x = fixture == ContactFixture.Miss ? 2 : fixture == ContactFixture.BaseBox ? .85 : 0;
    var y = fixture == ContactFixture.Gentle ? 1.481 : speed > (Half)0 ? 1.5 : 3;
    var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Basketball(new(1), x, y, 0, 0, 0, 0, 1),
            new WorkshopSwitch(new(2), new(0, 16, 0), default, CanonicalRotation.Identity, ContactTriggerSettings.Default)));
    var scene = WorkshopPhysicsCompiler.Compile(construction, new(63, 35));
    if (speed > (Half)0)
    {
        var bodies = scene.Bodies.ToArray(); var slot = Array.FindIndex(bodies, b => b.Motion == RigidMotionKind.Dynamic);
        bodies[slot] = bodies[slot] with { Velocity = new((Half)0, (Half)(-speed / (Half)32), (Half)0), Gravity = default, LinearDrag = new((Half)0) };
        scene = new(scene.Document, scene.NextIdentity, bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides, scene.Triggers);
    }
    cases.Add(new { name = Name(fixture), ticks = 120, speedBits = BitConverter.HalfToUInt16Bits(speed),
        source = Convert.ToBase64String(PhysicsGpuAbi.Admission(scene, new(1), profile)) });
}
Console.WriteLine(JsonSerializer.Serialize(new { stateBytes = PhysicsGpuAbi.ByteLength,
    triggerOffset = PhysicsGpuAbi.TriggersOffset, preamble = PhysicsGpuAbi.ShaderPreamble(), cases }));

static string Name(ContactFixture fixture) => fixture switch
{
    ContactFixture.Drop => "drop", ContactFixture.Miss => "miss", ContactFixture.Gentle => "gentle",
    ContactFixture.Below => "below", ContactFixture.Equal => "equal", ContactFixture.Above => "above",
    ContactFixture.BaseBox => "base-box", _ => throw new ArgumentOutOfRangeException(nameof(fixture))
};
enum ContactFixture { Drop, Miss, Gentle, Below, Equal, Above, BaseBox }
