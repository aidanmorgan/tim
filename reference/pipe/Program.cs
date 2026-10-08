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
foreach(var fixture in Enum.GetValues<PipeFixture>())
{
    if (fixture is PipeFixture.PlaneRolling or PipeFixture.BoxRolling)
    {
        var supportBox = fixture == PipeFixture.BoxRolling;
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
        cases.Add(new { name=Name(fixture),ticks=120,
            source=Convert.ToBase64String(PhysicsGpuAbi.Admission(supportScene,new(1),profile)),
            expected="Existing shared Plane/Box support: static rolling from canonical launch, unchanged residual/work bounds." });
        continue;
    }
    var radius=BasketballMaterial.Default.Radius.Value;
    var dimensions=PipeDimensions.Default;
    var radial=(float)PipeDimensions.BoreRadius.Value-(float)radius;
    var orientation=fixture==PipeFixture.TiltedAxial ? Quaternion.CreateFromAxisAngle(Vector3.UnitZ,-.2f) : Quaternion.Identity;
    var local=fixture switch
    {
        PipeFixture.BoreAxis=>Vector3.Zero,
        PipeFixture.Circumferential or PipeFixture.ShorterTrial=>new Vector3(0,-radial,0),
        PipeFixture.TiltedAxial=>new Vector3(-.8f,-radial,0),
        PipeFixture.MouthDeparture=>new Vector3((float)dimensions.Profile.HalfLength.Value+(float)dimensions.Profile.EndHalfWidth.Value-.03f,-radial,0),
        PipeFixture.RaisedShoulder=>new Vector3((float)dimensions.Profile.HalfLength.Value-(float)dimensions.Profile.EndHalfWidth.Value-.4f,
            (float)dimensions.Profile.MiddleRadius.Value+(float)radius,0),
        _=>throw new ArgumentOutOfRangeException()
    };
    var localVelocity=fixture switch
    {
        PipeFixture.Circumferential=>new Vector3(0,0,.5f),
        PipeFixture.ShorterTrial=>new Vector3(0,0,3),
        PipeFixture.MouthDeparture or PipeFixture.RaisedShoulder=>Vector3.UnitX,
        _=>Vector3.Zero
    };
    var centre=new Vector3(0,4,0);
    var location=centre+Vector3.Transform(local,orientation);
    var velocity=Vector3.Transform(localVelocity,orientation);
    var ball=WorkshopInput.Basketball(new(1),location.X,location.Y,location.Z,0,0,0,1);
    var pipe=WorkshopInput.Pipe(new(2),centre.X,centre.Y,centre.Z,orientation.X,orientation.Y,orientation.Z,orientation.W,dimensions);
    var compiled=WorkshopPhysicsCompiler.Compile(new(new(1),WorkshopCadenceSettings.Default(),new(ball,pipe)),new(48,1));
    var bodies=compiled.Bodies.ToArray().Where(body=>body.Id.Value<=2).ToArray();
    var colliders=compiled.Colliders.ToArray().Where(collider=>collider.Body.Value<=2).ToArray();
    var materialIds=colliders.Select(collider=>collider.Material).ToHashSet();
    var materials=compiled.Materials.ToArray().Where(material=>materialIds.Contains(material.Id)).ToArray();
    var frictionless=fixture is PipeFixture.Circumferential or PipeFixture.ShorterTrial;
    for(var i=0;i<bodies.Length;i++) if(bodies[i].Motion==RigidMotionKind.Dynamic)
        bodies[i]=bodies[i] with
        {
            Velocity=new((Half)(velocity.X/32),(Half)(velocity.Y/32),(Half)(velocity.Z/32)),
            Gravity=frictionless ? default : new((Half)0,(Half)(-9.81),(Half)0),
            LinearDrag=new((Half)0)
        };
    if(frictionless)
        for(var i=0;i<materials.Length;i++) materials[i]=materials[i] with { Friction=new((Half)0),Restitution=new((Half)0) };
    var scene=new PhysicsSceneDeclaration(compiled.Document,compiled.NextIdentity,bodies,colliders,materials,[],[]);
    cases.Add(new
    {
        name=Name(fixture), ticks=fixture==PipeFixture.Circumferential ? 480 : fixture==PipeFixture.ShorterTrial ? 48 : 120,
        source=Convert.ToBase64String(PhysicsGpuAbi.Admission(scene,new(1),profile)),
        expected=fixture switch
        {
            PipeFixture.BoreAxis=>"Free bore fall followed by real inner-wall contact.",
            PipeFixture.Circumferential=>"Frictionless constant-speed circular motion; normal work zero; whole-Run oracle from canonical launch.",
            PipeFixture.ShorterTrial=>"Directed full-piece trial may subdivide; failed trial must restore all tentative state before shorter success.",
            PipeFixture.TiltedAxial=>"Gravity-driven tilted bore contact and axial rolling.",
            PipeFixture.MouthDeparture=>"Supported bore reaches actual mouth then releases to rim/free motion.",
            PipeFixture.RaisedShoulder=>"Outer-shell approach hits exposed collar shoulder/rim before passing its axial plane.",
            _=>throw new ArgumentOutOfRangeException()
        }
    });
}
Console.WriteLine(JsonSerializer.Serialize(new { stateBytes=PhysicsGpuAbi.ByteLength,preamble=PhysicsGpuAbi.ShaderPreamble(),cases }));
static string Name(PipeFixture fixture)=>fixture switch
{
    PipeFixture.BoreAxis=>"bore-axis",PipeFixture.Circumferential=>"circumferential",
    PipeFixture.ShorterTrial=>"shorter-trial",PipeFixture.TiltedAxial=>"tilted-axial",
    PipeFixture.MouthDeparture=>"mouth-departure",PipeFixture.RaisedShoulder=>"raised-shoulder",
    PipeFixture.PlaneRolling=>"plane-rolling",PipeFixture.BoxRolling=>"box-rolling",
    _=>throw new ArgumentOutOfRangeException(nameof(fixture))
};
enum PipeFixture { BoreAxis,Circumferential,ShorterTrial,TiltedAxial,MouthDeparture,RaisedShoulder,PlaneRolling,BoxRolling }
