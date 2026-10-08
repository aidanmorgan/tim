using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Extensible component-owned identities, declared once and scoped by body.</summary>
public sealed class MechanicalSourceSlot;
public sealed class MechanicalReceiverSlot;
public readonly record struct SceneMechanicalSourceKey(SceneBodyKey Body,MechanicalSourceSlot Slot);
public readonly record struct SceneMechanicalReceiverKey(SceneBodyKey Body,MechanicalReceiverSlot Slot);

/// <summary>Construction-owned immutable lookup. Membership never follows activation,
/// exposure or load submission order. Branch IDs encode declared source/receiver pairs
/// without allocating a quadratic table. Rebuild only with a new physics assembly.</summary>
public sealed class SceneMechanicalTransferBindings
{
    private readonly Dictionary<SceneMechanicalSourceKey,MechanicalSourceId> _sources=new();
    private readonly record struct ReceiverIndex(int Value);
    private readonly Dictionary<SceneMechanicalReceiverKey,ReceiverIndex> _receivers=new();
    private readonly List<SceneMechanicalReceiverKey> _receiverKeys=[];

    public SceneMechanicalTransferBindings(ScenePhysicsAssembly assembly,IEnumerable<MachinePart> parts)
    {
        ArgumentNullException.ThrowIfNull(assembly);ArgumentNullException.ThrowIfNull(parts);
        foreach(var part in parts)
        {
            ArgumentNullException.ThrowIfNull(part);
            foreach(var key in part.PhysicsTransferSources)
            {
                ValidateBody(assembly,part,key.Body);ArgumentNullException.ThrowIfNull(key.Slot);
                if(!_sources.TryAdd(key,new(_sources.Count)))
                    throw new ArgumentException("Duplicate mechanical source declaration.");
            }
            foreach(var key in part.PhysicsTransferReceivers)
            {
                ValidateBody(assembly,part,key.Body);ArgumentNullException.ThrowIfNull(key.Slot);
                if(!_receivers.TryAdd(key,new(_receivers.Count)))
                    throw new ArgumentException("Duplicate mechanical receiver declaration.");
                _receiverKeys.Add(key);
            }
        }
        // Reject an unrepresentable identity space before publishing any bindings.
        if((long)_sources.Count*_receivers.Count>(long)int.MaxValue+1)
            throw new ArgumentException("Mechanical transfer identity space exceeds the supported range.");
    }

    private static void ValidateBody(ScenePhysicsAssembly assembly,MachinePart part,SceneBodyKey key)
    {
        if(key.Owner!=part||key.Slot is null)
            throw new ArgumentException("Mechanical declaration requires its owning part's body.");
        _=assembly.Body(key);
    }

    public MechanicalSourceId Source(SceneMechanicalSourceKey key)=>
        _sources.TryGetValue(key,out var id)?id:
        throw new ArgumentException("Mechanical source is not declared in this construction.",nameof(key));

    public SceneMechanicalReceiverKey Receiver(MechanicalTransferId transfer)
    {
        if(_receivers.Count==0||(long)transfer.Index>=(long)_sources.Count*_receivers.Count)
            throw new ArgumentException("Transfer is not declared in this construction.",nameof(transfer));
        return _receiverKeys[transfer.Index%_receivers.Count];
    }

    public MechanicalTransferId Transfer(SceneMechanicalSourceKey source,SceneMechanicalReceiverKey receiver)
    {
        var id=Source(source);
        if(!_receivers.TryGetValue(receiver,out var index))
            throw new ArgumentException("Mechanical receiver is not declared in this construction.",nameof(receiver));
        return new(checked(id.Index*_receivers.Count+index.Value));
    }
}
