using System;

namespace CuriousContraptions.Physics;

/// <summary>Repeated work performed inside one successful PhysicsWorld.Step, not
/// unique pairs. Construction queries and separate instantaneous commands are excluded.</summary>
public readonly record struct PhysicsSpatialWork(long BodyQueries,long BodyNodeTests,long BodyLeafTests,
    long BodyCandidatePairs,long CompoundQueries,long CompoundNodeTests,long CompoundLeafTests,long CompoundCandidatePairs)
{
    internal PhysicsSpatialWork Add(BodyCandidateResult result)=>this with
    {
        BodyQueries=checked(BodyQueries+1),
        BodyNodeTests=checked(BodyNodeTests+result.NodeTests),
        BodyLeafTests=checked(BodyLeafTests+result.LeafTests),
        BodyCandidatePairs=checked(BodyCandidatePairs+result.Pairs.Count)
    };
    internal PhysicsSpatialWork Add(CompoundCandidateResult result)=>this with
    {
        CompoundQueries=checked(CompoundQueries+1),
        CompoundNodeTests=checked(CompoundNodeTests+result.NodeTests),
        CompoundLeafTests=checked(CompoundLeafTests+result.LeafTests),
        CompoundCandidatePairs=checked(CompoundCandidatePairs+result.Pairs.Count)
    };
    public void Validate()
    {
        if(BodyQueries<0||BodyNodeTests<0||BodyLeafTests<0||BodyCandidatePairs<0||
            CompoundQueries<0||CompoundNodeTests<0||CompoundLeafTests<0||CompoundCandidatePairs<0||
            BodyCandidatePairs>BodyLeafTests||BodyLeafTests>BodyNodeTests||
            CompoundCandidatePairs>CompoundLeafTests||CompoundLeafTests>CompoundNodeTests||
            BodyQueries==0&&BodyNodeTests!=0||CompoundQueries==0&&CompoundNodeTests!=0)
            throw new ArgumentOutOfRangeException(nameof(PhysicsSpatialWork),"Spatial work counts must be nonnegative and nested.");
    }
}
