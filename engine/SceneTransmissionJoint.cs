using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Couples registered axial guides, retaining their actual assembly
/// identities and bodies. Declaration order does not determine dependency order.</summary>
public sealed class SceneTransmissionJoint : SceneJointDeclaration
{
    private readonly SceneJointKey[] _dependencies;
    public SceneJointKey Input { get; }
    public SceneJointKey Output { get; }
    public double Ratio { get; }
    public TransmissionEngagement Engagement { get; }
    public override ReadOnlySpan<SceneJointKey> Dependencies=>_dependencies;
    public SceneTransmissionJoint(SceneJointKey key,SceneJointKey input,SceneJointKey output,double ratio,TransmissionEngagement engagement):base(key)
    {
        ArgumentNullException.ThrowIfNull(input.Owner);ArgumentNullException.ThrowIfNull(input.Slot);
        ArgumentNullException.ThrowIfNull(output.Owner);ArgumentNullException.ThrowIfNull(output.Slot);
        if(input==output) throw new ArgumentException("Transmission requires distinct guide identities.");
        if(!double.IsFinite(ratio)||ratio==0) throw new ArgumentOutOfRangeException(nameof(ratio));
        if(!Enum.IsDefined(engagement)) throw new ArgumentOutOfRangeException(nameof(engagement));
        Engagement=engagement;
        Input=input;Output=output;Ratio=ratio;_dependencies=[input,output];
    }
    public override PhysicsJoint Bind(PhysicsJointId id,IReadOnlyDictionary<SceneBodyKey,PhysicsBody> bodies,
        IReadOnlyDictionary<SceneJointKey,PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(bodies);ArgumentNullException.ThrowIfNull(joints);
        if(!joints.TryGetValue(Input,out var input)||input is not PhysicsFrameJoint first||
            !joints.TryGetValue(Output,out var output)||output is not PhysicsFrameJoint second)
            throw new ArgumentException("Transmission endpoints must be declared axial frame joints.");
        return new PhysicsTransmissionJoint(id,first,second,Ratio,Engagement);
    }
}
