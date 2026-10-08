using CuriousContraptions.Physics;
using CuriousContraptions.Geometry;
using System.Reflection;
static class Program {
static void Main(){
var b=new Dictionary<PhysicsBodyId,PhysicsBody>();
b[new(0)]=new(new(0),PhysicsMotionType.Static,
new(new(0,0,0),
new(0,0,0,1)),
new(0,0,0),default,0,default);
b[new(0)].Restore(b[new(0)].Snapshot() with {AngularMomentum=new(0,0,0),KinematicAngularVelocity=new(0,0,0)});
if(BitConverter.DoubleToInt64Bits(b[new(0)].Pose.Rotation.X)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 0 X");
if(BitConverter.DoubleToInt64Bits(b[new(0)].Pose.Rotation.Y)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 0 Y");
if(BitConverter.DoubleToInt64Bits(b[new(0)].Pose.Rotation.Z)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 0 Z");
if(BitConverter.DoubleToInt64Bits(b[new(0)].Pose.Rotation.W)!=BitConverter.DoubleToInt64Bits((double)(1)))Console.WriteLine("Quaternion mismatch body 0 W");
b[new(1)]=new(new(1),PhysicsMotionType.Static,
new(new(0,3,0),
new(0.13052619665337553,0,0,0.9914448607901521)),
new(0,0,0),default,0,default);
b[new(1)].Restore(b[new(1)].Snapshot() with {AngularMomentum=new(0,0,0),KinematicAngularVelocity=new(0,0,0)});
if(BitConverter.DoubleToInt64Bits(b[new(1)].Pose.Rotation.X)!=BitConverter.DoubleToInt64Bits((double)(0.13052619665337553)))Console.WriteLine("Quaternion mismatch body 1 X");
if(BitConverter.DoubleToInt64Bits(b[new(1)].Pose.Rotation.Y)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 1 Y");
if(BitConverter.DoubleToInt64Bits(b[new(1)].Pose.Rotation.Z)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 1 Z");
if(BitConverter.DoubleToInt64Bits(b[new(1)].Pose.Rotation.W)!=BitConverter.DoubleToInt64Bits((double)(0.9914448607901521)))Console.WriteLine("Quaternion mismatch body 1 W");
b[new(2)]=new(new(2),PhysicsMotionType.Dynamic,
new(new(-0.6200000047683716,2.339331258428868,0.34061242934431946),
new(-0.08534229682404926,-0.09876122916201173,0.7501659866641247,-0.6482390796916248)),
new(0,-9.8100004196167,0),default,1.0/(4),
new(0.004433333333333333,0.004433333333333333,0.008450000000000001,0,0,0));
b[new(2)].Restore(b[new(2)].Snapshot() with {AngularMomentum=new(-4.1679290758882935E-56,2.1729136619460845E-39,5.822304817688808E-40),KinematicAngularVelocity=new(0,0,0)});
if(BitConverter.DoubleToInt64Bits(b[new(2)].Pose.Rotation.X)!=BitConverter.DoubleToInt64Bits((double)(-0.08534229682404926)))Console.WriteLine("Quaternion mismatch body 2 X");
if(BitConverter.DoubleToInt64Bits(b[new(2)].Pose.Rotation.Y)!=BitConverter.DoubleToInt64Bits((double)(-0.09876122916201173)))Console.WriteLine("Quaternion mismatch body 2 Y");
if(BitConverter.DoubleToInt64Bits(b[new(2)].Pose.Rotation.Z)!=BitConverter.DoubleToInt64Bits((double)(0.7501659866641247)))Console.WriteLine("Quaternion mismatch body 2 Z");
if(BitConverter.DoubleToInt64Bits(b[new(2)].Pose.Rotation.W)!=BitConverter.DoubleToInt64Bits((double)(-0.6482390796916248)))Console.WriteLine("Quaternion mismatch body 2 W");
b[new(3)]=new(new(3),PhysicsMotionType.Dynamic,
new(new(1.0504336186080495E-25,3.0965925965176573,0.025881911075962265),
new(2.7153127208704713E-17,-8.180284767285298E-28,-8.243781703172838E-27,1)),
new(0,237.46701420254558,66.25767874499869),default,1.0/(2),
new(0.020479999084472667,0.020479999084472667,0.020479999084472667,0,0,0));
b[new(3)].Restore(b[new(3)].Snapshot() with {AngularMomentum=new(2.3718399761780733E-07,-3.5897089547104103E-34,1.3396975724119152E-33),KinematicAngularVelocity=new(0,0,0)});
if(BitConverter.DoubleToInt64Bits(b[new(3)].Pose.Rotation.X)!=BitConverter.DoubleToInt64Bits((double)(2.7153127208704713E-17)))Console.WriteLine("Quaternion mismatch body 3 X");
if(BitConverter.DoubleToInt64Bits(b[new(3)].Pose.Rotation.Y)!=BitConverter.DoubleToInt64Bits((double)(-8.180284767285298E-28)))Console.WriteLine("Quaternion mismatch body 3 Y");
if(BitConverter.DoubleToInt64Bits(b[new(3)].Pose.Rotation.Z)!=BitConverter.DoubleToInt64Bits((double)(-8.243781703172838E-27)))Console.WriteLine("Quaternion mismatch body 3 Z");
if(BitConverter.DoubleToInt64Bits(b[new(3)].Pose.Rotation.W)!=BitConverter.DoubleToInt64Bits((double)(1)))Console.WriteLine("Quaternion mismatch body 3 W");
b[new(4)]=new(new(4),PhysicsMotionType.Static,
new(new(-3,3,2),
new(0,0,0,1)),
new(0,0,0),default,0,default);
b[new(4)].Restore(b[new(4)].Snapshot() with {AngularMomentum=new(0,0,0),KinematicAngularVelocity=new(0,0,0)});
if(BitConverter.DoubleToInt64Bits(b[new(4)].Pose.Rotation.X)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 4 X");
if(BitConverter.DoubleToInt64Bits(b[new(4)].Pose.Rotation.Y)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 4 Y");
if(BitConverter.DoubleToInt64Bits(b[new(4)].Pose.Rotation.Z)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 4 Z");
if(BitConverter.DoubleToInt64Bits(b[new(4)].Pose.Rotation.W)!=BitConverter.DoubleToInt64Bits((double)(1)))Console.WriteLine("Quaternion mismatch body 4 W");
b[new(5)]=new(new(5),PhysicsMotionType.Dynamic,
new(new(-3,3,2.540000021457672),
new(-3.7826721487059966E-22,4.526349965906708E-22,0.7566391398370499,-0.6538327095417064)),
new(0,-9.8100004196167,0),default,1.0/(2),
new(0.011954166666666667,0.011954166666666667,0.0225,0,0,0));
b[new(5)].Restore(b[new(5)].Snapshot() with {AngularMomentum=new(-1.1677164790289223E-100,-1.2744735289059618E-57,-2.996272867003007E-95),KinematicAngularVelocity=new(0,0,0)});
if(BitConverter.DoubleToInt64Bits(b[new(5)].Pose.Rotation.X)!=BitConverter.DoubleToInt64Bits((double)(-3.7826721487059966E-22)))Console.WriteLine("Quaternion mismatch body 5 X");
if(BitConverter.DoubleToInt64Bits(b[new(5)].Pose.Rotation.Y)!=BitConverter.DoubleToInt64Bits((double)(4.526349965906708E-22)))Console.WriteLine("Quaternion mismatch body 5 Y");
if(BitConverter.DoubleToInt64Bits(b[new(5)].Pose.Rotation.Z)!=BitConverter.DoubleToInt64Bits((double)(0.7566391398370499)))Console.WriteLine("Quaternion mismatch body 5 Z");
if(BitConverter.DoubleToInt64Bits(b[new(5)].Pose.Rotation.W)!=BitConverter.DoubleToInt64Bits((double)(-0.6538327095417064)))Console.WriteLine("Quaternion mismatch body 5 W");
b[new(6)]=new(new(6),PhysicsMotionType.Dynamic,
new(new(0.006891002683128049,3.713769241284806,0.2596454463225353),
new(0.0731607349537699,-0.008027310915876221,-0.013200434828735714,0.997200490202856)),
new(-0.001291693754376063,-9.810082881222677,0.0002557902741041781),default,1.0/(1),
new(0.04624000097274781,0.04624000097274781,0.04624000097274781,0,0,0));
b[new(6)].Restore(b[new(6)].Snapshot() with {AngularMomentum=new(5.4210108624275216E-20,0,-1.3552527156068804E-20),KinematicAngularVelocity=new(0,0,0)});
if(BitConverter.DoubleToInt64Bits(b[new(6)].Pose.Rotation.X)!=BitConverter.DoubleToInt64Bits((double)(0.0731607349537699)))Console.WriteLine("Quaternion mismatch body 6 X");
if(BitConverter.DoubleToInt64Bits(b[new(6)].Pose.Rotation.Y)!=BitConverter.DoubleToInt64Bits((double)(-0.008027310915876221)))Console.WriteLine("Quaternion mismatch body 6 Y");
if(BitConverter.DoubleToInt64Bits(b[new(6)].Pose.Rotation.Z)!=BitConverter.DoubleToInt64Bits((double)(-0.013200434828735714)))Console.WriteLine("Quaternion mismatch body 6 Z");
if(BitConverter.DoubleToInt64Bits(b[new(6)].Pose.Rotation.W)!=BitConverter.DoubleToInt64Bits((double)(0.997200490202856)))Console.WriteLine("Quaternion mismatch body 6 W");
b[new(7)]=new(new(7),PhysicsMotionType.Static,
new(new(-5,3,2),
new(0,0,0,1)),
new(0,0,0),default,0,default);
b[new(7)].Restore(b[new(7)].Snapshot() with {AngularMomentum=new(0,0,0),KinematicAngularVelocity=new(0,0,0)});
if(BitConverter.DoubleToInt64Bits(b[new(7)].Pose.Rotation.X)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 7 X");
if(BitConverter.DoubleToInt64Bits(b[new(7)].Pose.Rotation.Y)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 7 Y");
if(BitConverter.DoubleToInt64Bits(b[new(7)].Pose.Rotation.Z)!=BitConverter.DoubleToInt64Bits((double)(0)))Console.WriteLine("Quaternion mismatch body 7 Z");
if(BitConverter.DoubleToInt64Bits(b[new(7)].Pose.Rotation.W)!=BitConverter.DoubleToInt64Bits((double)(1)))Console.WriteLine("Quaternion mismatch body 7 W");
IImpulseConstraint r0=new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(-0,-0.9659258239744088,-0.2588190537409477),
new(5.551115123125783E-17,2.7187223521385955E-26,-1.014640958689064E-25)),
new(b[new(3)],
new(0,0.9659258239744088,0.2588190537409477),
new(1.8529999537772761E-09,-2.8044600790778075E-36,1.0466387128506808E-35)) ]),
1.2997835396684375E-60,0,double.PositiveInfinity,0);
IImpulseConstraint r1=new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(-0,-0.9659258239744088,-0.2588190537409477),
new(5.551115123125783E-17,2.7187223521385955E-26,-1.014640958689064E-25)),
new(b[new(3)],
new(0,0.9659258239744088,0.2588190537409477),
new(1.8529999537772761E-09,-2.8044600790778075E-36,1.0466387128506808E-35)) ]),
1.2997835396684375E-60,double.NegativeInfinity,0,0);
IImpulseConstraint r2=new BilateralConstraintBlock([new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(-1,-0,-0),
new(-0,-0.0258819092861017,0.09659259699724876)),
new(b[new(3)],
new(1,0,0),
new(0,-1.789860565315493E-09,-4.79591477642316E-10)) ]),
2.0784408598419793E-59,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(-0,-0.2588190537409477,0.9659258239744088),
new(0.1000000151148334,-1.014640958689064E-25,-2.7187223521385955E-26)),
new(b[new(3)],
new(0,0.2588190537409477,-0.9659258239744088),
new(2.2463043526447803E-16,1.0466387128506808E-35,2.8044600790778075E-36)) ]),
1.852215382704581E-58,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(0,0,0),
new(-1,-0,-0)),
new(b[new(3)],
new(0,0,0),
new(1,0,0)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(0,0,0),
new(-0,-1,-0)),
new(b[new(3)],
new(0,0,0),
new(0,1,0)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(0,0,0),
new(-0,-0,-1)),
new(b[new(3)],
new(0,0,0),
new(0,0,1)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(-1,-0,-0),
new(0,-0.34061242934431946,-0.6606687415711319)),
new(b[new(2)],
new(1,0,0),
new(0,0,0)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(-0,-1,-0),
new(0.34061242934431946,-0,0.6200000047683716)),
new(b[new(2)],
new(0,1,0),
new(0,0,0)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(-0,-0,-1),
new(0.6606687415711319,-0.6200000047683716,-0)),
new(b[new(2)],
new(0,0,1),
new(0,0,0)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(0,0,0),
new(0.14500557586670215,0.955716797354519,0.25608355320478327)),
new(b[new(2)],
new(0,0,0),
new(-0.14500557586670215,-0.955716797354519,-0.25608355320478327)) ]),
-1.504632769052528E-36,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(0,0,0),
new(-0.9894308378899285,0.14006463034992755,0.037530205932982175)),
new(b[new(2)],
new(0,0,0),
new(0.9894308378899285,-0.14006463034992755,-0.037530205932982175)) ]),
-1.2037062152420224E-35,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(-0,-0.9659258239744088,-0.2588190537409477),
new(1.1851401674854867E-16,-0.025881905759765178,0.09659258383678253)),
new(b[new(2)],
new(-0,-0,-0),
new(-6.300286551729084E-17,0.025881905759765178,-0.09659258383678253)),
new(b[new(3)],
new(0,0.9659258239744088,0.2588190537409477),
new(1.8529999537772761E-09,-2.8044600790778075E-36,1.0466387128506808E-35)) ]),
1.2754919859406197E-36,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(4)],
new(-1,-0,-0),
new(-0,-0.5400000214576721,-0)),
new(b[new(5)],
new(1,0,0),
new(0,0,0)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(4)],
new(-0,-1,-0),
new(0.5400000214576721,-0,-0)),
new(b[new(5)],
new(0,1,0),
new(0,0,0)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(4)],
new(-0,-0,-1),
new(-0,-0,-0)),
new(b[new(5)],
new(0,0,1),
new(0,0,0)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(4)],
new(0,0,0),
new(0.14500557586670149,0.9894308378899288,-1.9471572332064507E-23)),
new(b[new(5)],
new(0,0,0),
new(-0.14500557586670149,-0.9894308378899288,1.9471572332064507E-23)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(4)],
new(0,0,0),
new(-0.9894308378899288,0.14500557586670149,-1.1796096650203005E-21)),
new(b[new(5)],
new(0,0,0),
new(0.9894308378899288,-0.14500557586670149,1.1796096650203005E-21)) ]),
-0,double.NegativeInfinity,double.PositiveInfinity,0),
new ImpulseConstraint(new ConstraintGradient([new(b[new(1)],
new(0,0,0),
new(-6.3002864578475E-16,0.2588190537409473,-0.9659258239744088)),
new(b[new(2)],
new(0,0,0),
new(6.3002864578475E-16,-0.2588190537409473,0.9659258239744088)),
new(b[new(4)],
new(-0,-0,-0),
new(-5.821593463425264E-22,9.515787645087215E-23,1)),
new(b[new(5)],
new(-0,-0,-0),
new(5.821593463425264E-22,-9.515787645087215E-23,-1)) ]),
-2.4791981821763307E-35,double.NegativeInfinity,double.PositiveInfinity,0)]);
IImpulseConstraint r5=new ImpulseConstraint(new ConstraintGradient([new(b[new(4)],
new(0,0,0),
new(5.821593463425264E-22,-9.515787645087215E-23,-1)),
new(b[new(5)],
new(0,0,0),
new(-5.821593463425264E-22,9.515787645087215E-23,1)) ]),
-3722.922980642637,-19.999999999999982,19.999999999999982,0);
IImpulseConstraint r3=ContactConstraint.ForAcceleration(new ContactKinematics(new(0.20345601305263475,-0.2534056085164466,0.9457226064365467),
new ConstraintGradient([new(b[new(1)],
new(0.20345601305263475,-0.2534056085164466,0.9457226064365467),
new(0.7408233195859896,0.0463094502976058,-0.1469668627996172)),
new(b[new(6)],
new(-0.20345601305263475,0.2534056085164466,-0.9457226064365467),
new(1.4066837057205384E-17,9.653510302326123E-18,-4.395886580376683E-19)) ]),

new ConstraintGradient([new(b[new(1)],
new(-0,0.9659258263253478,0.258819044967124),
new(-0.3989986087300822,-0.019689968979509656,0.07348396467218704)),
new(b[new(6)],
new(0,-0.9659258263253478,-0.258819044967124),
new(0.33293743979561574,0.017906446246196566,-0.0668277672112764)) ]),

new ConstraintGradient([new(b[new(1)],
new(-0.9790840876823227,-0.05265829099110164,0.19652341752872698),
new(0.1539448560911431,-0.5840319427528934,0.6104658493040208)),
new(b[new(6)],
new(0.9790840876823227,0.05265829099110164,-0.19652341752872698),
new(2.7755575615628914E-17,0.3284629744218364,0.08801138869050841))])),-3.4679916360540353E-19,
new(1.996427708443611E-11,1.274521287578101E-12,-3.953465161121129E-12),
new(6.765973893018475E-09,5.618194071408652E-09,4.980722086631192E-11),0.29999999999999993,FrictionRegime.Sticking);
IImpulseConstraint r4=ContactConstraint.ForAcceleration(new ContactKinematics(new(-0.010440914508408389,-0.9351162495375843,-0.35418722048514195),
new ConstraintGradient([new(b[new(3)],
new(-0.010440914508408389,-0.9351162495375841,-0.35418722048514195),
new(-6.488672443360933E-18,7.244836082859512E-20,9.822775458209523E-26)),
new(b[new(6)],
new(0.010440914508408389,0.9351162495375841,0.35418722048514195),
new(-0,-6.144168862665492E-19,1.6221681108402332E-18)) ]),

new ConstraintGradient([new(b[new(3)],
new(0,-0.3542065275155464,0.9351672234768381),
new(0.3199825093947547,-0.003124479860384094,-0.001183436645185544)),
new(b[new(6)],
new(-0,0.3542065275155464,-0.9351672234768381),
new(0.3399814299663373,-0.003319759985768205,-0.0012574014863055553)) ]),

new ConstraintGradient([new(b[new(3)],
new(-0.9999454921665621,0.009764001031387309,0.0036982400721100235),
new(-2.168404344971009E-19,-0.11334607176722751,0.2992534665299533)),
new(b[new(6)],
new(0.9999454921665621,-0.009764001031387309,-0.0036982400721100235),
new(2.168404344971009E-19,-0.1204302061177622,0.3179568210327461))])),0.0016483917036691927,
new(0.0003707259166463043,-0.0006356088325652677,0.0016671901071721805),
new(6.836150552544313E-10,-2.120449581447362E-09,5.578206042786647E-09),0.29999999999999993,FrictionRegime.Sticking);
IImpulseConstraint[] rows=[r0,r1,r2,r3,r4,r5];
try {var result=typeof(ImpulseSolver).GetMethods(BindingFlags.NonPublic|BindingFlags.Static).Single(method=>method.ReturnType==typeof(ImpulseSolveResult)).Invoke(null,[rows,256,1.0000000000000002e-10]);Console.WriteLine(result);}
catch(TargetInvocationException e){Console.WriteLine(e.InnerException?.Message);}
for(var i=0;i<rows.Length;i++)Console.WriteLine($"row {i}: residual {rows[i].Residual:R}");

const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
const string tangentMapsName="TangentGradients",tangentCoordinatesName="TangentCoordinates";
const string biasUName="_bias0",biasVName="_bias1",applyName="Apply",commitName="CommitCoupledImpulse",commitTangentName="CommitTangentCoordinates";
var apply=typeof(ConstraintGradient).GetMethod(applyName,flags)!;
var commit=typeof(ImpulseConstraint).GetMethod(commitName,flags)!;
var commitTangent=typeof(ContactConstraint).GetMethod(commitTangentName,flags)!;
var active=new List<ImpulseConstraint>{(ImpulseConstraint)r0};
active.AddRange(r2.ScalarRows);
var oldEfforts=new List<double>{((ImpulseConstraint)r0).AccumulatedImpulse+((ImpulseConstraint)r1).AccumulatedImpulse};
oldEfforts.AddRange(r2.ScalarRows.Select(x=>x.AccumulatedImpulse));
foreach(var c in new[]{(ContactConstraint)r3,(ContactConstraint)r4})
{
 var maps=((ConstraintGradient U,ConstraintGradient V))typeof(ContactConstraint).GetProperty(tangentMapsName,flags)!.GetValue(c)!;
 var coords=((double U,double V))typeof(ContactConstraint).GetProperty(tangentCoordinatesName,flags)!.GetValue(c)!;
 var biasU=(double)typeof(ContactConstraint).GetField(biasUName,flags)!.GetValue(c)!;
 var biasV=(double)typeof(ContactConstraint).GetField(biasVName,flags)!.GetValue(c)!;
 active.Add(c.Normal);
 active.Add(new(maps.U,-biasU,double.NegativeInfinity,double.PositiveInfinity));
 active.Add(new(maps.V,-biasV,double.NegativeInfinity,double.PositiveInfinity));
 oldEfforts.Add(c.Normal.AccumulatedImpulse);oldEfforts.Add(coords.U);oldEfforts.Add(coords.V);
}
var n=active.Count;var matrix=new double[n,n];var residual=new double[n];
for(var i=0;i<n;i++){residual[i]=active[i].Speed-active[i].TargetSpeed;for(var j=0;j<n;j++)matrix[i,j]=active[i].Gradient.Coupling(active[j].Gradient);}
var correction=NewtonDirection.Solve(matrix,residual,new int[n],new int[n],out var statistics);
Console.WriteLine($"Diagnostic active mass rank={statistics.Rank}, threshold={statistics.RankResolution:R}");
var beforeCorrection=b.ToDictionary(x=>x.Key,x=>x.Value.Snapshot());
var proposed=oldEfforts.Zip(correction,(x,d)=>x+d).ToArray();
for(var i=0;i<n;i++)apply.Invoke(active[i].Gradient,[proposed[i]-oldEfforts[i]]);
commit.Invoke(r0,[Math.Max(0,proposed[0])]);commit.Invoke(r1,[Math.Min(0,proposed[0])]);
for(var i=1;i<18;i++)commit.Invoke(active[i],[proposed[i]]);
for(var ci=0;ci<2;ci++){
 var c=(ContactConstraint)rows[3+ci];var off=18+3*ci;
 commit.Invoke(c.Normal,[proposed[off]]);commitTangent.Invoke(c,[proposed[off+1],proposed[off+2]]);
 var magnitude=Math.Sqrt(proposed[off+1]*proposed[off+1]+proposed[off+2]*proposed[off+2]);
 Console.WriteLine($"Diagnostic contact={ci}, normal={proposed[off]:R}, tangentLength={magnitude:R}, radius={c.Friction*proposed[off]:R}, admissible={proposed[off]>=0&&magnitude<=c.Friction*proposed[off]}");
}
double maximum=0;
for(var i=0;i<rows.Length;i++){maximum=Math.Max(maximum,rows[i].Residual);Console.WriteLine($"Diagnostic original row={i}, residual={rows[i].Residual:R}");}
Console.WriteLine($"Diagnostic maximum={maximum:R}, withinOriginalBudget={maximum<=1.0000000000000002e-10}");
Console.WriteLine("Diagnostic correction=["+string.Join(",",correction.Select(x=>x.ToString("R")))+"]");
Console.WriteLine("Diagnostic efforts=["+string.Join(",",proposed.Select(x=>x.ToString("R")))+"]");
foreach(var body in b.Values)Console.WriteLine($"Diagnostic body={body.Id.Index}, snapshot={body.Snapshot()}");

}}
