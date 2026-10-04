using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using CuriousContraptions.Gpu;

if (args.Length==3)
{
    using var raw=JsonDocument.Parse(File.ReadAllText(args[0]));
    var row=raw.RootElement.GetProperty(args[1]).EnumerateArray().Single(value=>value.GetProperty("name").GetString()==args[2]);
    var previous=Convert.FromBase64String(row.GetProperty("source").GetString()!);
    var count=0;
    foreach (var item in row.GetProperty("states").EnumerateArray())
    {
        var next=Convert.FromBase64String(item.GetString()!);
        PhysicsGpuAbi.ValidateCandidate(next,previous,new(BinaryPrimitives.ReadUInt64LittleEndian(previous.AsSpan(40))+1));
        previous=next;count++;
    }
    Console.WriteLine(JsonSerializer.Serialize(new { valid=true,count }));
    return;
}
var profile = new WorkshopGpuProfile(SimulationCadence.Hz120,PhysicalStepProfile.Canonical480Hz,new(1));
var cases=new List<object>();
foreach(var fixture in Enum.GetValues<SupportFixture>())
{
        var supportBox = fixture == SupportFixture.BoxRolling;
        var supportRadius = (float)BasketballMaterial.Default.Radius.Value;
        var supportRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -.2f);
        var supportLocation = supportBox ? new Vector3(0,4,0) + Vector3.Transform(new Vector3(-1,.2f+supportRadius,0),supportRotation)
            : new Vector3(0,(float)WorkshopConstruction.WorkbenchSurface.Value+supportRadius,0);
        var supportBall = WorkshopInput.Basketball(new(1),supportLocation.X,supportLocation.Y,supportLocation.Z,0,0,0,1);
        var supportInstances = new List<IWorkshopInstance> { supportBall };
        if (supportBox) supportInstances.Add(WorkshopInput.Wall(new(2),0,4,0,supportRotation.X,supportRotation.Y,supportRotation.Z,supportRotation.W,
            new(new((Half)8),new((Half).4),new((Half)2))));
        var supportCompiled = WorkshopPhysicsCompiler.Compile(new(new(1),WorkshopCadenceSettings.Default(),
            new(supportInstances.ToArray())),new(48,1));
        var supportColliders = supportCompiled.Colliders.ToArray().Where(c => supportBox ? c.Body.Value<=2 : true).ToArray();
        var supportIds = supportColliders.Select(c => c.Body).ToHashSet();
        var supportBodies = supportCompiled.Bodies.ToArray().Where(b => supportIds.Contains(b.Id)).ToArray();
        var supportMaterialIds = supportColliders.Select(c => c.Material).ToHashSet();
        var supportMaterials = supportCompiled.Materials.ToArray().Where(m => supportMaterialIds.Contains(m.Id)).ToArray();
        for (var i=0;i<supportBodies.Length;i++) if (supportBodies[i].Motion==RigidMotionKind.Dynamic)
            supportBodies[i]=supportBodies[i] with { LinearDrag=new((Half)0),
                Gravity=supportBox ? new((Half)0,(Half)(-9.81),(Half)0) : new((Half)2,(Half)(-9.81),(Half)0) };
        var supportScene = new PhysicsSceneDeclaration(supportCompiled.Document,supportCompiled.NextIdentity,supportBodies,supportColliders,supportMaterials,[],[]);
        cases.Add(new { name=fixture == SupportFixture.PlaneRolling ? "plane-rolling" : "box-rolling",ticks=120,
            source=Convert.ToBase64String(PhysicsGpuAbi.Admission(supportScene,new(1),profile)),
            expected="Existing shared Plane/Box support: static rolling from canonical launch, unchanged residual/work bounds." });
}
Console.WriteLine(JsonSerializer.Serialize(new { stateBytes=PhysicsGpuAbi.ByteLength,preamble=PhysicsGpuAbi.ShaderPreamble(),cases }));
enum SupportFixture { PlaneRolling, BoxRolling }
