using System.Buffers.Binary;
using System.Text.Json;
using CuriousContraptions.Gpu;

var profile = new WorkshopGpuProfile(SimulationCadence.Hz120,PhysicalStepProfile.Canonical480Hz,new(1));
if (args.Length == 3)
{
    using var raw = JsonDocument.Parse(File.ReadAllText(args[0]));
    var selected = raw.RootElement;
    foreach (var property in args[1].Split('.')) selected = selected.GetProperty(property);
    var row = selected.EnumerateArray().Single(x => x.GetProperty("name").GetString() == args[2]);
    var source = Convert.FromBase64String(row.GetProperty("source").GetString()!);
    var rows = row.GetProperty("states").EnumerateArray();
    var count = 0;
    foreach (var state in rows)
    {
        var candidate = Convert.FromBase64String(state.GetString()!);
        PhysicsGpuAbi.ValidateCandidate(candidate,source,new(BinaryPrimitives.ReadUInt64LittleEndian(source.AsSpan(40))+1));
        source = candidate; count++;
    }
    Console.WriteLine(JsonSerializer.Serialize(new { valid=true,count }));
    return;
}
var cases = new List<object>();
foreach (var precision in new[]{0.0,.45,1.0})
{
    var construction = FirstPrinciples.Create(new(1),WorkshopCadenceSettings.Default(),new(1),new(2),new((Half)precision));
    var angle = -20 * Math.PI/360;
    construction = construction.WithInstance(WorkshopInput.Ramp(new(3),-3.4,4.3,0,0,0,Math.Sin(angle),Math.Cos(angle),RampDimensions.Default));
    construction = construction.WithInstance(WorkshopInput.Ramp(new(4),-.7,2.7,0,0,0,Math.Sin(angle),Math.Cos(angle),RampDimensions.Default));
    var scene = WorkshopPhysicsCompiler.Compile(construction,new(51,54));
    cases.Add(new { name=$"puzzle-{precision}", ticks=600,source=Convert.ToBase64String(PhysicsGpuAbi.Admission(scene,new(1),profile)) });
}
foreach (var item in new[]{("interior",.5,1.1,-1.0,0.0,0.0,0.0,-9.8,false),("entry",.5,1.6,-1.0,0.0,0.0,0.0,-9.8,false),
    ("exit",.5,.55,-1.0,0.0,0.0,0.0,-9.8,false),("rotated",.5,1.1,-1.0,Math.PI/6,0.0,0.0,-9.8,false),
    ("ascending",.5,1.1,1.0,0.0,0.0,0.0,-9.8,false),("reentry",1.05,1.1,-.1,0.0,2.0,-12.0,0.0,true)})
{
    var scene = WorkshopPhysicsCompiler.Compile(FirstPrinciples.Create(new(1),WorkshopCadenceSettings.Default(),new(1),new(2),new((Half)0)),new(51,54));
    var bodies = scene.Bodies.ToArray();
    var frameIndex = Array.FindIndex(bodies,x=>x.Id==new GpuBodyId(2));
    var targetIndex = Array.FindIndex(bodies,x=>x.Id==new GpuBodyId(1));
    var q = new CanonicalRotation((Half)0,(Half)0,(Half)Math.Sin(item.Item5/2),(Half)Math.Cos(item.Item5/2));
    var norm=(double)q.Z*(double)q.Z+(double)q.W*(double)q.W;
    var cos=1-2*(double)q.Z*(double)q.Z/norm;var sin=2*(double)q.Z*(double)q.W/norm;
    var authored=WorkshopInput.Basketball(new(1),item.Item2*cos-item.Item3*sin,item.Item2*sin+item.Item3*cos,0,0,0,0,1);
    bodies[frameIndex]=bodies[frameIndex] with { Cell=default,Local=default,Rotation=q };
    bodies[targetIndex]=bodies[targetIndex] with { Cell=authored.Cell,Local=authored.Local,
        Velocity=item.Item9 ? new((Half)((item.Item6*cos-item.Item4*sin)/32),(Half)((item.Item6*sin+item.Item4*cos)/32),(Half)0)
            : new((Half)(-item.Item4*sin/32),(Half)(item.Item4*cos/32),(Half)0),
        Gravity=item.Item9 ? new((Half)item.Item7,(Half)item.Item8,(Half)0) : bodies[targetIndex].Gravity };
    var colliders=scene.Colliders.ToArray().Where(x=>x.Body==new GpuBodyId(1)).ToArray();
    var probe = new PhysicsSceneDeclaration(scene.Document,scene.NextIdentity,bodies,colliders,scene.Materials,[],scene.Guides);
    cases.Add(new { name=item.Item1,ticks=48,source=Convert.ToBase64String(PhysicsGpuAbi.Admission(probe,new(1),profile)) });
}
Console.WriteLine(JsonSerializer.Serialize(new { stateBytes=PhysicsGpuAbi.ByteLength,preamble=PhysicsGpuAbi.ShaderPreamble(),cases }));
