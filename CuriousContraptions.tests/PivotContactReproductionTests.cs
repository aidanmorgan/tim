using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

/// <summary>Captured acceleration-space pivot/contact system; no scene, catalogue or graphics.</summary>
public class PivotContactReproductionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PivotAndStickingContactMeetTheDeclaredResidual(bool reverse)
    {
        PhysicsBodySnapshot Solve()
        {
            var b2=new PhysicsBody(new(2),PhysicsMotionType.Static,
                new(new(-1,6,0),new(0,0,0,1)),new(0,0,0),default);
            b2.Restore(b2.Snapshot() with {AngularMomentum=new(0,0,0)});
            var b3=new PhysicsBody(new(3),PhysicsMotionType.Dynamic,
                new(new(-0.7236998996709156,5.776328363203801,-0.000010765797603560284),new(0.000009075303163244615,-0.00003623126207272449,0.12026175784543852,0.9927422163909722)),new(25.714285683418808,-9.810000422514452,0.00010861555014487493),default,0.35,new(0.03375908554403953,0.002602678679840588,0.03513413927619809,0.00041111549479829813,0,0));
            b3.Restore(b3.Snapshot() with {AngularMomentum=new(-0.0000032341077930103667,-0.00002754160858362274,2.996457406232024)});
            var b4=new PhysicsBody(new(4),PhysicsMotionType.Static,
                new(new(-0.5999999940395355,6.200000002980232,0),new(0,0,0,1)),new(0,0,0),default);
            b4.Restore(b4.Snapshot() with {AngularMomentum=new(0,0,0)});
            var row0=new ImpulseConstraint(new ConstraintGradient([new(b2,new(-1,0,0),new(0,0,0.8999999761581421)),
                new(b3,new(1,0,0),new(0,0.000010766523223882572,-1.1236716161568312))]),
                9.416529196394026e-8,double.NegativeInfinity,double.PositiveInfinity);
            var row1=new ImpulseConstraint(new ConstraintGradient([new(b2,new(0,-1,0),new(0,0,0)),
                new(b3,new(0,1,0),new(-0.000010766523223882572,0,-0.27630008696845043))]),
                3.596634148607783e-8,double.NegativeInfinity,double.PositiveInfinity);
            var row2=new ImpulseConstraint(new ConstraintGradient([new(b2,new(0,0,-1),new(-0.8999999761581421,0,0)),
                new(b3,new(0,0,1),new(1.1236716161568312,0.27630008696845043,0))]),
                7.4994862726591e-12,double.NegativeInfinity,double.PositiveInfinity);
            var map=new ContactKinematics(new(-0.9999999975696359,0,-0.00006971892129941498),
                new ConstraintGradient([new(b3,new(-0.9999999975696358,0,-0.00006971892129941498),new(-0.000017409148204482705,-0.000006673303310183298,0.24970478369002272)),
                new(b4,new(0.9999999975696358,0,0.00006971892129941498),new(-0.000012128781505884422,0.000004531729718247073,0.17396685505673254))]),
                new ConstraintGradient([new(b3,new(0.000069718921299415,0,-0.999999997569636),new(-0.24970478369002272,0.05864994583753654,-0.00001740914820448271)),
                new(b4,new(-0.000069718921299415,0,0.999999997569636),new(-0.17396685505673248,0.06504996024378758,-0.000012128781505884423))]),
                new ConstraintGradient([new(b3,new(0,-0.9999999999999999,0),new(0.000010762314252026898,0,-0.05864994522974031)),
                new(b4,new(0,0.9999999999999999,0),new(3.4833515333829015e-9,0,-0.06504996040163978))]));
            var contact=ContactConstraint.ForAcceleration(map,9.433045942952181e-8,
                map.U*1.9136815503852419e-13+map.V*3.6152387262156624e-8,default,0.19999999999999998,FrictionRegime.Sticking);
            var block=new BilateralConstraintBlock([row0,row1,row2]);
            IImpulseConstraint[] constraints=reverse?[contact,block]:[block,contact];
            var result=ImpulseSolver.Solve(constraints,tolerance:1.0000000000000002e-10);
            Console.WriteLine($"reverse={reverse}, iterations={result.Iterations}, residual={result.MaximumResidual:R}");
            Assert.InRange(result.MaximumResidual,0,1.0000000000000002e-10);
            Assert.InRange(contact.Residual,0,1.0000000000000002e-10);
            Assert.InRange(block.Residual,0,1.0000000000000002e-10);
            Assert.InRange(contact.TangentImpulse.Length,0,contact.Friction*contact.Impulse.Normal);
            return b3.Snapshot();
        }
        Assert.Equal(Solve(),Solve());
    }
}
