using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CuriousContraptions.Physics;
using CuriousContraptions.Geometry;
enum CapturedCase { X15, Compound }
enum CapturedTrial { Candidate, Full, Best }
record Wrench(double[] Force,double[] Torque);
record BodyInput(int Id,PhysicsMotionType Motion,double[] Center,double[] Rotation,double[] InitialRotation,double[] Velocity,double[] Momentum,double[] KinematicAngular,double InverseMass,double[] Inertia,double[] PrescribedLinear,double[] PrescribedAngular,Wrench Candidate,Wrench Applied,double[] InitialVelocity,double[] InitialMomentum);
record TermInput(int Id,double[] Linear,double[] Angular);
record RowInput(AccelerationRelation Relation,double Bias,TermInput[] Terms);
record ContactInput(FrictionRegime Regime,double Friction,double[] Normal,double[] U,double[] V,double NormalBias,double[] TangentBias,double[] Slip,TermInput[] NormalTerms,TermInput[] UTerms,TermInput[] VTerms);
record DriveInput(int Id,double Bias,double Target,double Minimum,double Maximum,TermInput[] Terms);
record Input(CapturedCase Case,CapturedTrial Trial,BodyInput[] Bodies,RowInput[] Rows,ContactInput[] Contacts,DriveInput[] Drives,double[] Raw,double[] Projected,double[] Residual,double Physical,int Offset,double[] State,double Horizon);
enum DiagnosticMode { Reassembly, FrozenSupport, AffineSlip, AngleSweep }
delegate double[] NewtonAdvance(ReadOnlySpan<double> state,Func<double[],double[]> residual,ReadOnlySpan<NormalizationLinearization> normalizations);
delegate object ReactionEvaluator(ReadOnlySpan<double> coordinates,Span<double> projected);
static class Program {
static CollisionVector V(double[] v)=>new(v[0],v[1],v[2]);
static BodyWrench W(Wrench w)=>new(V(w.Force),V(w.Torque));
static bool Same(double a,double b)=>BitConverter.DoubleToInt64Bits(a)==BitConverter.DoubleToInt64Bits(b);
static void Main(string[] args){
var options=new JsonSerializerOptions();options.Converters.Add(new JsonStringEnumConverter());
foreach(var input in JsonSerializer.Deserialize<Input[]>(File.ReadAllText(args[0]),options)!){
 if(!Enum.IsDefined(input.Case)||!Enum.IsDefined(input.Trial))throw new ArgumentException("Unsupported capture identity.");
 var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>();int quaternionChanges=0;
 foreach(var b in input.Bodies){
  if(!Enum.IsDefined(b.Motion))throw new ArgumentException();
  if(b.PrescribedLinear.Any(x=>x!=0)||b.PrescribedAngular.Any(x=>x!=0))throw new ArgumentException("Nonzero prescribed acceleration needs explicit reconstruction.");
  var q=b.Rotation;var rotation=new RigidRotation(q[0],q[1],q[2],q[3]);
  var rebuilt=new[]{rotation.X,rotation.Y,rotation.Z,rotation.W};
  quaternionChanges+=q.Where((v,i)=>!Same(v,rebuilt[i])).Count();
  if(q.Where((v,i)=>!Same(v,rebuilt[i])).Any())Console.WriteLine(JsonSerializer.Serialize(new{input.Case,Body=b.Id,OriginalQuaternion=q,RebuiltQuaternion=rebuilt}));
  var z=b.Inertia;var tensor=b.Motion==PhysicsMotionType.Dynamic?new InertiaTensor(z[0],z[1],z[2],z[3],z[4],z[5]):default;
  var body=new PhysicsBody(new(b.Id),b.Motion,new(V(b.Center),rotation),V(b.Velocity),V(b.KinematicAngular),b.InverseMass==0?0:1/b.InverseMass,tensor);
  body.Restore(body.Snapshot() with {AngularMomentum=V(b.Momentum),KinematicAngularVelocity=V(b.KinematicAngular)});
  bodies.Add(body.Id,body);
 }
 ConstraintGradient G(TermInput[] terms)=>new(terms.Select(t=>new ConstraintTerm(bodies[new(t.Id)],V(t.Linear),V(t.Angular))).ToArray());
 var rows=input.Rows.Select(r=>new ConstraintAcceleration(G(r.Terms),r.Bias,r.Relation)).ToArray();
 int axesChanges=0;
 var contacts=input.Contacts.Select(c=>{
  if(!Enum.IsDefined(c.Regime))throw new ArgumentException();
  var k=new ContactKinematics(V(c.Normal),G(c.NormalTerms),G(c.UTerms),G(c.VTerms));
  if(k.U!=V(c.U)||k.V!=V(c.V))axesChanges++;
  return new ContactForce(k,c.NormalBias,V(c.TangentBias),V(c.Slip),c.Friction,c.Regime);
 }).ToArray();
 var drives=input.Drives.Select(d=>new AccelerationDrive(new(d.Id),G(d.Terms),d.Bias,d.Target,d.Minimum,d.Maximum)).ToArray();
 var applied=input.Bodies.ToDictionary(b=>new PhysicsBodyId(b.Id),b=>W(b.Applied));
 var candidate=input.Bodies.ToDictionary(b=>new PhysicsBodyId(b.Id),b=>W(b.Candidate));
 var type=typeof(PhysicsBody).Assembly.GetType("CuriousContraptions.Physics.AccelerationReactionSystem",true)!;
 var ctor=type.GetConstructors(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Single();
 var system=ctor.Invoke([bodies.Values,rows,contacts,applied,candidate,drives]);
 var method=type.GetMethod("Evaluate",BindingFlags.Instance|BindingFlags.NonPublic)!;
 var evaluate=(ReactionEvaluator)method.CreateDelegate(typeof(ReactionEvaluator),system);
 var projected=new double[input.Raw.Length];var result=evaluate(input.Raw,projected);
 object Get(string name)=>result.GetType().GetProperty(name)!.GetValue(result)!;
 var residual=(double[])Get("Residual");var physical=(double)Get("PhysicalResidual");
 var forces=(IReadOnlyDictionary<PhysicsBodyId,BodyWrench>)Get("Forces");
 int projectionChanges=0,residualChanges=0;double maxProjection=0,maxResidual=0;
 for(int i=0;i<projected.Length;i++){
  if(!Same(projected[i],input.Projected[i]))projectionChanges++;
  if(!Same(residual[i]*10,input.Residual[input.Offset+i]))residualChanges++;
  maxProjection=Math.Max(maxProjection,Math.Abs(projected[i]-input.Projected[i]));
  maxResidual=Math.Max(maxResidual,Math.Abs(residual[i]*10-input.Residual[input.Offset+i]));
 }
 int linearChanges=0,angularChanges=0;double maxLinear=0,maxAngular=0;int offset=0;
 foreach(var b in input.Bodies.Where(b=>b.Motion==PhysicsMotionType.Dynamic)){
  var f=(candidate[new(b.Id)].Force-forces[new(b.Id)].Force)*b.InverseMass;
  var v=new[]{f.X,f.Y,f.Z};
  for(int i=0;i<3;i++){if(!Same(v[i],input.Residual[offset+i]))linearChanges++;maxLinear=Math.Max(maxLinear,Math.Abs(v[i]-input.Residual[offset+i]));}
  var z=b.Inertia;var q=b.InitialRotation;var initialInverse=new InertiaTensor(z[0],z[1],z[2],z[3],z[4],z[5]).Inverse().Rotated(new(q[0],q[1],q[2],q[3]));
  var ca=initialInverse.Apply(candidate[new(b.Id)].Torque);var fa=initialInverse.Apply(forces[new(b.Id)].Torque);
  var av=new[]{input.State[offset+3]-fa.X,input.State[offset+4]-fa.Y,input.State[offset+5]-fa.Z};
  for(int i=0;i<3;i++){if(!Same(av[i],input.Residual[offset+3+i]))angularChanges++;maxAngular=Math.Max(maxAngular,Math.Abs(av[i]-input.Residual[offset+3+i]));}
  offset+=6;
 }
 Console.WriteLine(JsonSerializer.Serialize(new{input.Case,input.Trial,QuaternionChanges=quaternionChanges,AxesChanges=axesChanges,ProjectionChanges=projectionChanges,MaxProjection=maxProjection,ResidualChanges=residualChanges,MaxResidual=maxResidual,Physical=physical,ExpectedPhysical=input.Physical,PhysicalEqual=Same(physical,input.Physical),LinearChanges=linearChanges,MaxLinear=maxLinear,AngularChanges=angularChanges,MaxAngular=maxAngular,Projected=projected,Residual=residual}));
 if(args.Length>1 && input.Case==CapturedCase.X15){
  var mode=JsonSerializer.Deserialize<DiagnosticMode>(args[1]);
  if(!Enum.IsDefined(mode))throw new ArgumentException();
  if(mode is DiagnosticMode.FrozenSupport or DiagnosticMode.AffineSlip or DiagnosticMode.AngleSweep){
   var dynamicInputs=input.Bodies.Where(b=>b.Motion==PhysicsMotionType.Dynamic).ToArray();
   var initialInertias=dynamicInputs.Select(b=>{var z=b.Inertia;var q=b.InitialRotation;return new InertiaTensor(z[0],z[1],z[2],z[3],z[4],z[5]).Rotated(new(q[0],q[1],q[2],q[3]));}).ToArray();
   var initialInverses=dynamicInputs.Select(b=>{var z=b.Inertia;var q=b.InitialRotation;return new InertiaTensor(z[0],z[1],z[2],z[3],z[4],z[5]).Inverse().Rotated(new(q[0],q[1],q[2],q[3]));}).ToArray();
   double finalPhysical=double.PositiveInfinity;double[] finalProjected=[];CollisionVector finalSlip=default;double angle=0;
   double[] FrozenResidual(double[] state){
    var trialCandidate=new Dictionary<PhysicsBodyId,BodyWrench>(candidate);
    for(int i=0;i<dynamicInputs.Length;i++){
     var b=dynamicInputs[i];int o=6*i;
     var df=new CollisionVector(state[o],state[o+1],state[o+2])/b.InverseMass;
     var dt=initialInertias[i].Apply(new(state[o+3],state[o+4],state[o+5]));
     trialCandidate[new(b.Id)]=new(df,dt);
    }
    
    double Speed(TermInput[] terms){
     Span<ulong> storage=stackalloc ulong[BinaryProductSum.StorageLength];var sum=new BinaryProductSum(storage);
     foreach(var term in terms){
      var b=input.Bodies.Single(b=>b.Id==term.Id);var wrench=trialCandidate[new(b.Id)];var linear=term.Linear;var angular=term.Angular;
      var held=wrench.Force*b.InverseMass;var acceleration=new[]{held.X,held.Y,held.Z};
      for(int i=0;i<3;i++){sum.Add(linear[i],b.InitialVelocity[i]);sum.Add(linear[i],acceleration[i],input.Horizon*.5);}
      if(b.Motion!=PhysicsMotionType.Dynamic){for(int i=0;i<3;i++)sum.Add(angular[i],b.KinematicAngular[i]);continue;}
      var body=bodies[new(b.Id)];var inverse=body.LocalInertia.Rotated(body.Pose.Rotation).Inverse();
      var tensor=new[,]{{inverse.XX,inverse.XY,inverse.XZ},{inverse.XY,inverse.YY,inverse.YZ},{inverse.XZ,inverse.YZ,inverse.ZZ}};
      var torque=new[]{wrench.Torque.X,wrench.Torque.Y,wrench.Torque.Z};
      for(int column=0;column<3;column++)for(int row=0;row<3;row++){
       sum.Add(angular[row],tensor[row,column],b.InitialMomentum[column]);
       sum.Add(angular[row],tensor[row,column],torque[column],input.Horizon*.5);
      }
     }
     return sum.Finish();
    }
    var trialContacts=mode is DiagnosticMode.AffineSlip or DiagnosticMode.AngleSweep?input.Contacts.Select((c,i)=>{
     var slip=V(c.U)*Speed(c.UTerms)+V(c.V)*Speed(c.VTerms);
     if(i==0)finalSlip=slip;
     if(mode==DiagnosticMode.AffineSlip&&state.SequenceEqual(input.State))Console.WriteLine(JsonSerializer.Serialize(new{Mode=mode,Contact=i,DerivedSlip=new[]{slip.X,slip.Y,slip.Z},CapturedSlip=c.Slip,Exact=Same(slip.X,c.Slip[0])&&Same(slip.Y,c.Slip[1])&&Same(slip.Z,c.Slip[2])}));
     if(mode==DiagnosticMode.AngleSweep&&c.Regime==FrictionRegime.Sliding)slip=V(c.U)*Math.Cos(angle)+V(c.V)*Math.Sin(angle);
     return new ContactForce(contacts[i].Kinematics,c.NormalBias,V(c.TangentBias),slip,c.Friction,c.Regime);
    }).ToArray():contacts;
    var trialSystem=ctor.Invoke([bodies.Values,rows,trialContacts,applied,trialCandidate,drives]);
    var trialEvaluate=(ReactionEvaluator)method.CreateDelegate(typeof(ReactionEvaluator),trialSystem);
    var projection=new double[input.Raw.Length];var evaluated=trialEvaluate(state.AsSpan(input.Offset),projection);
    object Field(string name)=>evaluated.GetType().GetProperty(name)!.GetValue(evaluated)!;
    var signed=(double[])Field("Residual");var reported=(IReadOnlyDictionary<PhysicsBodyId,BodyWrench>)Field("Forces");
    finalPhysical=(double)Field("PhysicalResidual");finalProjected=projection;
    var all=new double[state.Length];
    for(int i=0;i<dynamicInputs.Length;i++){
     var b=dynamicInputs[i];int o=6*i;var force=reported[new(b.Id)].Force*b.InverseMass;
     all[o]=state[o]-force.X;all[o+1]=state[o+1]-force.Y;all[o+2]=state[o+2]-force.Z;
     var acceleration=initialInverses[i].Apply(reported[new(b.Id)].Torque);
     all[o+3]=state[o+3]-acceleration.X;all[o+4]=state[o+4]-acceleration.Y;all[o+5]=state[o+5]-acceleration.Z;
    }
    for(int i=0;i<signed.Length;i++)all[input.Offset+i]=10*signed[i];
    return all;
   }
   var newtonType=typeof(PhysicsBody).Assembly.GetType("CuriousContraptions.Physics.NonlinearIteration",true)!;
   var advance=(NewtonAdvance)newtonType.GetMethod("Advance",BindingFlags.Static|BindingFlags.Public)!.CreateDelegate(typeof(NewtonAdvance));
   for(int sample=0;sample<(mode==DiagnosticMode.AngleSweep&&args.Length<3?32:1);sample++){
   angle=args.Length>2?JsonSerializer.Deserialize<double>(args[2]):sample*Math.Tau/32;
   var state=args.Length>3?JsonSerializer.Deserialize<double[]>(args[3])!:input.State.ToArray();var baseline=FrozenResidual(state);
   int baselineChanges=baseline.Where((x,i)=>!Same(x,input.Residual[i])).Count();
   int updates=0;string? failure=null;
   for(;updates<=64;updates++){
    var value=FrozenResidual(state);double error=Math.Max(value.Max(Math.Abs),10*finalPhysical);
    if(error<=1e-9||updates==64)break;
    try{state=advance(state,FrozenResidual,[]);}catch(InvalidOperationException exception){failure=exception.Message;break;}
   }
   var final=FrozenResidual(state);
   if(mode==DiagnosticMode.AngleSweep){
    var fixedPhysical=finalPhysical;var fixedProjection=finalProjected;
    mode=DiagnosticMode.AffineSlip;var actual=FrozenResidual(state);var actualPhysical=finalPhysical;mode=DiagnosticMode.AngleSweep;
    var c=input.Contacts[0];double u=CollisionVector.Dot(finalSlip,V(c.U)),v=CollisionVector.Dot(finalSlip,V(c.V));
    Console.WriteLine(JsonSerializer.Serialize(new{Mode=mode,Angle=angle,Updates=updates,Failure=failure,ResidualMaximum=final.Max(Math.Abs),Physical=fixedPhysical,ActualResidualMaximum=actual.Max(Math.Abs),ActualPhysical=actualPhysical,SlipU=u,SlipV=v,DirectionCross=Math.Cos(angle)*v-Math.Sin(angle)*u,DirectionDot=Math.Cos(angle)*u+Math.Sin(angle)*v,State=state,Projected=fixedProjection}));
   }else Console.WriteLine(JsonSerializer.Serialize(new{Mode=mode,InitialBitChanges=baselineChanges,Updates=updates,Failure=failure,ResidualMaximum=final.Max(Math.Abs),Physical=finalPhysical,State=state,Residual=final,Projected=finalProjected}));
   }
  }
 }

}}
}
