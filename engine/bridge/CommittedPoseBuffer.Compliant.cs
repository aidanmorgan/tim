using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Bridge;

/// <summary>Fixed contact topology joins the same atomic publication and lease as body poses.</summary>
public sealed partial class CommittedPoseBuffer
{
    private CompliantContactState[] _previousContacts=[],_currentContacts=[],_pendingContacts=[];
    private readonly Dictionary<CompliantContactKey,int> _contactIndices=[];
    private bool _contactsRegistered,_contactsStaged;

    public void RegisterCompliantContacts(ReadOnlySpan<CompliantContactState> initial)
    {
        RequireWritable();
        if(_registrationClosed||_contactsRegistered)
            throw new InvalidOperationException("Contact topology must be registered once before the first lease or write.");
        var indices=new Dictionary<CompliantContactKey,int>();
        for(var i=0;i<initial.Length;i++)
        {
            ValidateContact(initial[i],_current);
            if(!indices.TryAdd(initial[i].Key,i))throw new ArgumentException("Duplicate compliant contact identity.");
        }
        _previousContacts=initial.ToArray();_currentContacts=initial.ToArray();_pendingContacts=new CompliantContactState[initial.Length];
        foreach(var pair in indices)_contactIndices.Add(pair.Key,pair.Value);
        _contactsRegistered=true;
    }
    private static void ValidateContact(CompliantContactState value,ReadOnlySpan<BodyPublicationRead> bodies)
    {
        var key=value.Key;var footprint=value.Footprint;
        if(key.Body==key.Frame||key.Body.Index>=bodies.Length||key.Frame.Index>=bodies.Length||
            bodies[key.Body.Index].Pose.MotionType!=PhysicsMotionType.Dynamic||!Enum.IsDefined(value.Phase)||
            !double.IsFinite(value.RelativeSpeed)||!footprint.LowestPoint.IsFinite||
            !double.IsFinite(footprint.MinimumX)||!double.IsFinite(footprint.MaximumX)||
            !double.IsFinite(footprint.MinimumZ)||!double.IsFinite(footprint.MaximumZ)||
            footprint.MinimumX>footprint.MaximumX||footprint.MinimumZ>footprint.MaximumZ||
            !double.IsFinite(footprint.RoundingRadius)||footprint.RoundingRadius<0)
            throw new ArgumentException("Invalid committed compliant contact.");
    }
    internal void StageCompliantContacts(ReadOnlySpan<CompliantContactState> values)
    {
        if(_phase!=Phase.Writing||!_contactsRegistered||_contactsStaged)
            throw new InvalidOperationException("Contact staging requires one reserved complete write.");
        if(values.Length!=_currentContacts.Length)throw new ArgumentException("Contact topology changed within a run.");
        for(var i=0;i<values.Length;i++)
        {
            ValidateContact(values[i],_current);
            if(values[i].Key!=_currentContacts[i].Key||values[i].EpisodeCount<_currentContacts[i].EpisodeCount)
                throw new ArgumentException("Contact identity, order or episode history changed.");
        }
        values.CopyTo(_pendingContacts);_contactsStaged=true;
    }
    private void ValidateContactStage(ReadOnlySpan<BodyPublicationRead> bodies)
    {
        if(_contactsRegistered&&!_contactsStaged)
            throw new InvalidOperationException("Contact observations must publish with the complete tick.");
        foreach(var contact in _pendingContacts)ValidateContact(contact,bodies);
    }
    private void PublishContacts()
    {
        if(!_contactsRegistered)return;
        var scratch=_previousContacts;_previousContacts=_currentContacts;_currentContacts=_pendingContacts;
        _pendingContacts=scratch;_contactsStaged=false;
    }
    private void DiscardContacts(){Array.Clear(_pendingContacts);_contactsStaged=false;}
    private void RemoveContacts()
    {
        _previousContacts=[];_currentContacts=[];_pendingContacts=[];_contactIndices.Clear();
    }
    private ReadOnlySpan<CompliantContactState> Contacts(ulong token,PoseSample sample)
    {
        CheckLease(token);
        if(!_contactsRegistered)throw new ArgumentException("Contact observations are not registered.");
        return sample switch
        {
            PoseSample.Previous=>_previousContacts,PoseSample.Current=>_currentContacts,
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal int ContactCount(ulong token)=>Contacts(token,PoseSample.Current).Length;
    internal CompliantContactState Contact(ulong token,PoseSample sample,CompliantContactKey key)
    {
        var values=Contacts(token,sample);
        if(!_contactIndices.TryGetValue(key,out var index))throw new ArgumentException("Contact observation is absent.");
        return values[index];
    }
    internal void CopyContacts(ulong token,PoseSample sample,Span<CompliantContactState> target)
    {
        var values=Contacts(token,sample);
        if(target.Length!=values.Length)throw new ArgumentException("Contact target requires the complete topology.");
        values.CopyTo(target);
    }
}
