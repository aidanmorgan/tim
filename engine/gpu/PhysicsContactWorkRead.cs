using System;

namespace CuriousContraptions.Gpu;

/// <summary>Story 9A.23 D9: the device-private contact-work ledger of one candidate or committed world state: the owners' stores and actual
/// occurrence rows, the last qualified hit of each encountered pair. It is read from the world and its interaction snapshot and
/// is the only input of the Impact records (<see cref="EventRecordProducer.Stage"/>) and of the reservoir accounting
/// (<see cref="ValidateAdvance"/>); it never leaves the Simulation worker.</summary>
public readonly struct ContactWorkLedger
{
    private readonly ContactWorkOccurrence[]? _occurrences;
    public PhysicsContactWorkRead Stores { get; }
    public int Count => Stores.Count;
    public int OccurrenceCount => _occurrences?.Length ?? 0;

    /// <summary>Takes ownership of freshly decoded arrays (no copy); the caller never touches them again.</summary>
    internal static ContactWorkLedger Adopt(ContactWorkRead[] values, ContactWorkOccurrence[] occurrences) =>
        new(PhysicsContactWorkRead.FromImmutable(System.Runtime.InteropServices.ImmutableCollectionsMarshal.AsImmutableArray(values)), occurrences);

    public ContactWorkLedger(ReadOnlySpan<ContactWorkRead> values, ReadOnlySpan<ContactWorkOccurrence> occurrences)
        : this(new PhysicsContactWorkRead(values), occurrences.ToArray()) { }

    private ContactWorkLedger(PhysicsContactWorkRead stores, ContactWorkOccurrence[] occurrences)
    {
        Stores = stores; _occurrences = occurrences;
        for (var i = 0; i < occurrences.Length; i++)
        {
            var value = occurrences[i]; value.Validate();
            if (value.Work.Value >= Count || value.Sequence > stores[(int)value.Work.Value].OccurrenceCount ||
                (i > 0 && (occurrences[i-1].Work.Value > value.Work.Value ||
                    (occurrences[i-1].Work == value.Work && occurrences[i-1].Target.Value >= value.Target.Value))))
                throw new ArgumentException("Contact targets require ordered unique identities and owned sequences.");
            // Occurrences are ordered by owner, so only this owner's earlier rows can share its sequence.
            for (var j=i-1; j>=0 && occurrences[j].Work == value.Work; j--)
                if (value.Sequence != 0 && occurrences[j].Sequence == value.Sequence)
                    throw new ArgumentException("A contact-work owner sequence was duplicated.");
        }
    }
    public ContactWorkRead this[int index] => Stores[index];
    public ContactWorkOccurrence Occurrence(int index) => index >= 0 && index < OccurrenceCount ? _occurrences![index] :
        throw new ArgumentOutOfRangeException(nameof(index));

    public bool TryOccurrence(ContactWorkSlot work,GpuBodyId target,out ContactWorkOccurrence occurrence)
    {
        for(var i=0;i<OccurrenceCount;i++)
            if(_occurrences![i].Work==work && _occurrences[i].Target==target) { occurrence=_occurrences[i]; return true; }
        occurrence=default; return false;
    }

    /// <summary>Every owner's count increment is exactly its new occurrence rows, each later than the row it replaces, and the reservoir
    /// moved by exactly their debits (and any recharge in the same commit).</summary>
    public void ValidateAdvance(ContactWorkLedger previous)
    {
        Stores.ValidateAdvance(previous.Stores);
        for(var i=0;i<previous.OccurrenceCount;i++)
        {
            var prior=previous.Occurrence(i);
            if(!TryOccurrence(prior.Work,prior.Target,out _)) throw new ArgumentException("Earned work state was lost.");
        }
        for (var owner=0; owner<Count; owner++)
        {
            var before=previous[owner]; var after=this[owner];
            uint changed=0;
            for (var i=0; i<OccurrenceCount; i++)
            {
                var last=Occurrence(i);
                previous.TryOccurrence(last.Work,last.Target,out var first);
                if (last.Work.Value != owner) continue;
                if (last.Sequence == first.Sequence)
                {
                    if (!last.HasSameBits(first)) throw new ArgumentException("Unchanged occurrence mutated.");
                    continue;
                }
                if (last.Sequence <= before.OccurrenceCount || last.Sequence > after.OccurrenceCount ||
                    last.EventOrdinal < first.EventOrdinal ||
                    (last.EventOrdinal == first.EventOrdinal && last.EventPhase < first.EventPhase))
                    throw new ArgumentException("Contact occurrence was skipped or reversed.");
                changed++;
            }
            // Unique event sequences plus this cardinality prove the complete contiguous owner interval.
            if (after.OccurrenceCount-before.OccurrenceCount != changed)
                throw new ArgumentException("Committed contact occurrences were lost.");

        }
    }

    /// <summary>Accepted contact and actuator occurrences jointly account for each exact reservoir's released work.</summary>
    public void ValidateReleasedWork(ContactWorkLedger previous, PhysicsEnergyStoreRead current, PhysicsEnergyStoreRead prior,
        PhysicsFiniteWorkActuatorRead actuators, PhysicsFiniteWorkActuatorRead priorActuators)
    {
        for (var store = 0; store < current.Count; store++)
        {
            var after = current[store]; var before = prior[store];
            var debit = 0f;
            var allowance = PhysicsReadValidation.Residual(after.Released.Value) + PhysicsReadValidation.Residual(before.Released.Value);
            for (var i = 0; i < OccurrenceCount; i++)
            {
                var occurrence = Occurrence(i);
                previous.TryOccurrence(occurrence.Work, occurrence.Target, out var earlier);
                if (occurrence.Sequence == earlier.Sequence || this[(int)occurrence.Work.Value].Store != after.Id) continue;
                debit += occurrence.Debit.Value;
                allowance += PhysicsReadValidation.Residual(debit);
            }
            if (actuators.Count != priorActuators.Count) throw new ArgumentException("Actuator population changed.");
            for (var i = 0; i < actuators.Count; i++)
            {
                var actuator = actuators[i]; var earlier = priorActuators[i];
                if (actuator.Id != earlier.Id || actuator.Store != earlier.Store)
                    throw new ArgumentException("Actuator release ownership changed.");
                if (actuator.Store != after.Id || actuator.OccurrenceCount == earlier.OccurrenceCount) continue;
                if ((ulong)actuator.OccurrenceCount != (ulong)earlier.OccurrenceCount + 1UL ||
                    actuator.LastResult != FiniteWorkRequestResult.Fired)
                    throw new ArgumentException("Actuator release lacks its single accepted occurrence.");
                debit += actuator.LastDebit.Value;
                allowance += PhysicsReadValidation.Residual(debit);
            }
            if (MathF.Abs(after.Released.Value - before.Released.Value - debit) > allowance)
                throw new ArgumentException("Reservoir releases do not account for accepted work occurrences.");
        }
    }

    /// <summary>The ledger is exactly the scene's owners and one row per (owner, eligible dynamic target) of the read's bodies, each hit
    /// on an owner collider at or above its declared threshold.</summary>
    public void ValidateScene(IPhysicsSceneDeclaration scene, PhysicsBodyReadSet bodies)
    {
        Stores.ValidateScene(scene);
        for(var i=0;i<OccurrenceCount;i++)
        {
            var value=Occurrence(i);
            var declared=scene.ContactWorks[(int)value.Work.Value];
            if(!bodies.TryGet(value.Target,out _)) throw new ArgumentException("Work target is absent from the read.");
            var admitted=false;
            foreach(var body in scene.Bodies)
                if(body.Id==value.Target && body.Motion==RigidMotionKind.Dynamic && declared.Targets.Contains(body.Id,body.Payload)) admitted=true;
            if(!admitted || value.Collider.Value>=(uint)scene.Colliders.Length ||
                scene.Colliders[(int)value.Collider.Value].Body!=declared.Owner || value.ApproachSpeed.Value<declared.Threshold.Value)
                throw new ArgumentException("Contact occurrence violates its declared ownership or threshold.");
        }
    }

    /// <summary>An admitted world has hit nothing yet.</summary>
    public void ValidateAdmission()
    {
        if(OccurrenceCount!=0) throw new ArgumentException("Admission retained contact occurrence.");
    }
}
