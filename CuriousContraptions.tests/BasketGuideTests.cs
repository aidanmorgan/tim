using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class BasketGuideTests(HeadlessFixture godot)
{
    private const string BasketId="basket",BallId="ball";
    private const string BasketKind="basket",BallKind="ball";
    [Theory]
    [InlineData(0f,.3f,.7f,0f,-1f,true)]
    [InlineData(47f,.3f,.7f,0f,-1f,true)]
    [InlineData(0f,.3f,.84f,0f,0f,true)]
    [InlineData(0f,0f,.7f,0f,-1f,false)]
    [InlineData(0f,.3f,.49f,0f,-1f,false)]
    [InlineData(0f,.3f,.7f,1.2f,-1f,false)]
    [InlineData(0f,.3f,.7f,0f,1f,false)]
    [InlineData(0f,.3f,1.6f,0f,-1f,false)]
    public void AuthoredMarginEnablesBoundedNearRimForceWithoutMovingGeometry(
        float degrees,float margin,float height,float depth,float verticalSpeed,bool guided)
    {
        var world=new MachineWorld{Precision=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            const float acceleration=12,delta=.01f;
            var basket=world.AddPart(new(){Id=BasketId,Kind=BasketKind,Position=[0,5,0],
                Rotation=[0,0,degrees],Difficulty=[
                    new(){Precision=0,CaptureMargin=margin,GuideAcceleration=acceleration},
                    new(){Precision=1,CaptureMargin=0,GuideAcceleration=0}]});
            var at=basket.Transform*new Vector3(-.9f,height,depth);
            var ball=world.AddPart(new(){Id=BallId,Kind=BallKind,Position=[at.X,at.Y,at.Z]});
            var incoming=basket.Basis*new Vector3(0,verticalSpeed,0);
            ball.Velocity=incoming;
            var boxes=basket.Boxes.ToArray();
            basket.BeforeStep(world,delta);
            var change=basket.Basis.Inverse()*(ball.Velocity-incoming);
            Assert.Equal(at,ball.Position);
            Assert.Equal(boxes,basket.Boxes.ToArray());
            Assert.InRange(Mathf.Abs(change.Y),0,.00001f);
            Assert.InRange(change.Length(),0,acceleration*delta+.00001f);
            if(guided)Assert.InRange(change.X,.10799f,.10801f);
            else Assert.InRange(change.Length(),0,.00001f);
            world.Precision=1;ball.Velocity=incoming;
            basket.BeforeStep(world,delta);
            Assert.Equal(incoming,ball.Velocity);
        }
        finally{world.Free();}
    }
}
