using System;

namespace CuriousContraptions.Physics;

public enum TransferPortRole { Source, Receiver }

public readonly record struct TransferBodyKey(MechanicalTransferId Transfer,TransferPortRole Role,PhysicsBodyId Body);

/// <summary>Accepted physical impulse from one transfer port on one body.
/// Linear impulse is in world N s; angular impulse is in world N m s about
/// the body's centre. Source entries include the negative extraction reaction.
/// Stored supplies have no fictitious source body impulse.</summary>
public readonly record struct MechanicalTransferBodyImpulse
{
    public TransferBodyKey Key { get; }
    public MechanicalSourceId Source { get; }
    public CollisionVector Linear { get; }
    public CollisionVector Angular { get; }
    public MechanicalTransferBodyImpulse(TransferBodyKey key,MechanicalSourceId source,
        CollisionVector linear,CollisionVector angular)
    {
        if(!Enum.IsDefined(key.Role))throw new ArgumentOutOfRangeException(nameof(key));
        if(!linear.IsFinite||!angular.IsFinite)
            throw new ArgumentException("Transfer body impulse exceeds the supported numeric range.");
        Key=key;Source=source;Linear=linear;Angular=angular;
    }
    internal MechanicalTransferBodyImpulse Add(MechanicalTransferBodyImpulse other)
    {
        if(Key!=other.Key||Source!=other.Source)
            throw new ArgumentException("Transfer body impulse identity cannot change.");
        return new(Key,Source,Linear+other.Linear,Angular+other.Angular);
    }
}
