using Godot;

namespace CuriousContraptions.Tests;

public class BodySlotTests
{
    private static BodyDynamics Static(MachinePart? owner)=>
        new(Physics.PhysicsMotionType.Static,0,default,default,default);

    [Fact]
    public void TokensAreDistinctEvenWithIdenticalPoliciesAndProviders()
    {
        Func<MachinePart,global::CuriousContraptions.Geometry.RigidPose> pose=p=>global::CuriousContraptions.Geometry.RigidPose.Identity;
        var first=new BodySlot(pose,Static,BodyQueryPolicy.Include,_=>new(0,0,0),_=>[]);
        var second=new BodySlot(pose,Static,BodyQueryPolicy.Include,_=>new(0,0,0),_=>[]);
        var values=new Dictionary<BodySlot,int>{{first,1},{second,2}};
        Assert.NotEqual(first,second);
        Assert.Equal(1,values[first]); Assert.Equal(2,values[second]);
    }

    [Fact]
    public void UndefinedPoliciesAndMissingProviderAreRejected()
    {
        Assert.Throws<ArgumentNullException>(()=>new BodySlot(p=>global::CuriousContraptions.Geometry.RigidPose.Identity,Static,BodyQueryPolicy.Include,null!,_=>[]));
        Assert.Throws<ArgumentNullException>(()=>new BodySlot(null!,Static,BodyQueryPolicy.Include,_=>new(0,0,0),_=>[]));
        Assert.Throws<ArgumentNullException>(()=>new BodySlot(p=>global::CuriousContraptions.Geometry.RigidPose.Identity,null!,BodyQueryPolicy.Include,_=>new(0,0,0),_=>[]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BodySlot(p=>global::CuriousContraptions.Geometry.RigidPose.Identity,Static,(BodyQueryPolicy)99,_=>new(0,0,0),_=>[]));
        Assert.Throws<ArgumentNullException>(()=>new BodySlot(p=>global::CuriousContraptions.Geometry.RigidPose.Identity,Static,BodyQueryPolicy.Include,_=>new(0,0,0),null!));
    }
}
