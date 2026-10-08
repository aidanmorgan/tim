using CuriousContraptions.Physics;
using CuriousContraptions.Geometry;
using System.Text.Json;
enum Feature { Face, Points, PointEdge, CrossedEdges, ParallelEdges }
enum Check { Cancellation, ReversedCancellation, DrivenStatic, DrivenKinematic, FeatureDerivative }
static class Program {
static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
static readonly ConvexInstance Sphere=new(new ConvexSphere(.5),AffineTransform.Identity),Floor=new(new ConvexBox(new(10,.5,10)),AffineTransform.Identity);
static int failures;
static void Report(Check check,bool pass,object data){if(!pass)failures++;Console.WriteLine(JsonSerializer.Serialize(new{Check=check,Pass=pass,Data=data}));}
static void Cancellation(bool reverse){
var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(Y*.5),X,default,1,new(.1,.1,.1));
var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.At(-Y*.5),X,default);
var gap=(reverse?ContactGap.Query(b,Floor,a,Sphere,.001,1e-9):ContactGap.Query(a,Sphere,b,Floor,.001,1e-9)).Single();
var ap=a.CreateTrajectory(.75,new(X*1e-20,default));var bp=b.CreateTrajectory(.75,default);
var path=new ContactSlipPath(new MaterialContact(gap,[]),new Dictionary<PhysicsBodyId,BodyTrajectory>{{a.Id,ap},{b.Id,bp}});
var sample=path.At(.375);var sign=reverse?-1:1;
Report(reverse?Check.ReversedCancellation:Check.Cancellation,sample.Slip.X==sign*(1e-20*.375)&&sample.Derivative.X==sign*1e-20,new{sample.Slip,sample.Derivative});
}
static void Driven(PhysicsMotionType carrierType){
var carrier=new PhysicsBody(new(1),carrierType,RigidPose.At(-Y*.5),carrierType==PhysicsMotionType.Kinematic?new(.1,.2,0):default,carrierType==PhysicsMotionType.Kinematic?new(.2,.5,.4):default);
var shaft=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,carrier.Pose,carrier.LinearVelocity,carrier.AngularVelocity+Z*1.5,1,new(1,1,1));
var load=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(Y*.5),new(.3,-.1,.2),default,1,new(.1,.1,.1));
JointFrame origin=new(default,RigidRotation.Identity);
var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,shaft,origin,carrier,origin,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
var surface=new DrivenSurface(joint,Y,X,.8);
var gap=ContactGap.Query(load,Sphere,carrier,Floor,.001,1e-9).Single();
var path=new ContactSlipPath(new MaterialContact(gap,[surface]),new Dictionary<PhysicsBodyId,BodyTrajectory>{{load.Id,load.CreateTrajectory(.01,default)},{carrier.Id,carrier.CreateTrajectory(.01,default)},{shaft.Id,shaft.CreateTrajectory(.01,new(default,new(.2,-.3,.7)))}});
double maxError=0;bool enclosed=true;
for(int i=1;i<10;i++){double t=i*.001,h=1e-6;var sample=path.At(t);var finite=(path.At(t+h).Slip-path.At(t-h).Slip)/(2*h);maxError=Math.Max(maxError,(finite-sample.Derivative).Length);var bound=path.Bounds(t-h,t+h);enclosed&=bound.HasValue&&sample.Derivative.Length<=bound.Value.Rate;}
Report(carrierType==PhysicsMotionType.Static?Check.DrivenStatic:Check.DrivenKinematic,maxError<1e-6&&enclosed,new{maxError,enclosed});
}
static ConvexInstance Segment(CollisionVector axis)=>new(new ConvexRounded(new ConvexHull([-axis,axis]),.25),AffineTransform.Identity);
static void Geometry(Feature feature,bool reverse){
var(sa,sb,height)=feature switch{Feature.Face=>(Sphere,Floor,1.0),Feature.Points=>(Sphere,Sphere,1.0),Feature.PointEdge=>(Sphere,Segment(X*2),.75),Feature.CrossedEdges=>(Segment(X*2),Segment(Z*2),.5),Feature.ParallelEdges=>(Segment(X*2),Segment(X*2),.5),_=>throw new ArgumentOutOfRangeException()};
var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(Y*height),new(.2,.1,.3),new(.3,.4,-.6),1,new(.1,.2,.3));
var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,new(-.1,.2,.1),new(.1,-.3,.2));
var gaps=reverse?ContactGap.Query(b,sb,a,sa,.001,1e-9):ContactGap.Query(a,sa,b,sb,.001,1e-9);
var ap=a.CreateTrajectory(.01,new(new(1,-2,.5),new(.3,.7,-.2)));var bp=b.CreateTrajectory(.01,default);
var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>{{a.Id,ap},{b.Id,bp}};
double maxError=0;bool enclosed=true;int samples=0;
foreach(var gap in gaps){var path=new ContactSlipPath(new MaterialContact(gap,[]),paths);for(int i=1;i<10;i++){double t=i*.001,h=1e-7;var sample=path.At(t);var finite=(path.At(t+h).Slip-path.At(t-h).Slip)/(2*h);maxError=Math.Max(maxError,(finite-sample.Derivative).Length);var bounds=path.Bounds(t-h,t+h);enclosed&=bounds.HasValue&&sample.Derivative.Length<=bounds.Value.Rate;samples++;}}
Report(Check.FeatureDerivative,samples>0&&maxError<1e-6&&enclosed,new{feature,reverse,samples,maxError,enclosed});
}
static int Main(){Cancellation(false);Cancellation(true);Driven(PhysicsMotionType.Static);Driven(PhysicsMotionType.Kinematic);foreach(var f in Enum.GetValues<Feature>()){Geometry(f,false);Geometry(f,true);}return failures==0?0:1;}
}
