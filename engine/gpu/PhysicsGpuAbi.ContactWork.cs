using System;
using System.Buffers.Binary;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>The contact-work and work-occurrence records: admission, read and candidate validation. Every field is named
/// (Story 9.4b, Murdoch-2 R2-F4); <c>WorkshopAbiLiteralScanTests</c> fails on a literal offset in this file.</summary>
public static partial class PhysicsGpuAbi
{
    private static void AdmitContactWorks(Span<byte> data, IPhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.ContactWorks.Length; i++)
        {
            var work = scene.ContactWorks[i]; var record = data.Slice(scene.Layout.GetContactWorkOffset(work.Id), ContactWorkBytes);
            U64(record, RecordIdentityOffset, work.Id.Value); U32(record, ContactWorkOwnerOffset, BodySlot(scene, work.Owner));
            U32(record, ContactWorkTargetKindOffset, (uint)work.Targets.Kind);
            U32(record, ContactWorkNamedTargetOffset, work.Targets.Kind == BodyTargetKind.NamedBody ? BodySlot(scene, work.Targets.Body) : NoBody);
            BinaryPrimitives.WriteSingleLittleEndian(record[ContactWorkTargetSpeedOffset..], work.TargetSpeed.Value);
            BinaryPrimitives.WriteSingleLittleEndian(record[ContactWorkAdmissionSpeedOffset..], work.Threshold.Value);
            U32(record, ContactWorkCooldownOffset, work.CooldownPhysicalSteps);
            U32(record, ContactWorkStoreOffset, (uint)scene.Layout.GetEnergyStoreSlot(work.Store));
        }
    }

    /// <summary>The device-private contact-work ledger of a world: its owners' stores and occurrence rows (Story 9A.23 D9).</summary>
    public static ContactWorkLedger ReadContactWorks(ReadOnlySpan<byte> data, PhysicsInteractionSnapshot interactions)
    {
        if (ReadFailure(data)!=PhysicsFailure.None) throw new ArgumentException("Invalid contact-work read.");
        var layout=Layout(data);
        var count=layout.ContactWorks;
        // Exactly the world's populations; the read owns these arrays (no fixed work or occurrence table, Story 9R.5b).
        var stores=new ContactWorkRead[count];
        var pairs=interactions.Works.OrderBy(pair=>pair.Value.Work.Value).ThenBy(pair=>pair.Key.Target.Value).ToArray();
        var events=new ContactWorkOccurrence[pairs.Length];
        ReadContactWorkStores(data, layout, stores);
        for (var i=0; i<pairs.Length; i++)
        {
            var value=pairs[i].Value;
            value.Validate();
            if (value.Work.Value>=count || pairs[i].Key.Policy!=stores[(int)value.Work.Value].Id)
                throw new ArgumentException("Occurrence has no owner.");
            var declaration=data.Slice(layout.ContactWorkOffset((int)value.Work.Value),ContactWorkBytes);
            var found=false;
            for (var body=0u; body<(uint)layout.Bodies; body++)
                if (R64(data,layout.BodyOffset((int)body))==value.Target.Value && BodyMotion(data,body)==RigidMotionKind.Dynamic &&
                    MatchesTarget(data,(BodyTargetKind)R32(declaration,ContactWorkTargetKindOffset),R32(declaration,ContactWorkNamedTargetOffset),body))
                    found=true;
            if (!found || (value.Sequence!=0 && (value.Collider.Value>=R32(data,HeaderColliderCountOffset) ||
                R32(data,layout.ColliderOffset((int)value.Collider.Value)+ColliderBodyOffset)!=R32(declaration,ContactWorkOwnerOffset) ||
                !AdmittedTime(value.EventOrdinal,value.EventPhase,R32(data,HeaderSourceOrdinalOffset)) || value.ApproachSpeed.Value<RF(declaration,ContactWorkAdmissionSpeedOffset))))
                throw new ArgumentException("Occurrence does not bind its dynamic target, collider or time.");
            events[i]=value;
        }
        return ContactWorkLedger.Adopt(stores,events);
    }

    private static void ReadContactWorkStores(ReadOnlySpan<byte> data, PhysicsWorldLayout layout, ContactWorkRead[] stores)
    {
        for (var i=0; i<stores.Length; i++)
        {
            var record=data.Slice(layout.ContactWorkOffset(i),ContactWorkBytes);
            var owner=R32(record,ContactWorkOwnerOffset);
            var rawPolicy = R32(record,ContactWorkTargetKindOffset);
            if (rawPolicy > byte.MaxValue) throw new ArgumentException("Contact-work target policy exceeds its enum domain.");
            var policy=(BodyTargetKind)rawPolicy;
            var target=R32(record,ContactWorkNamedTargetOffset);
            if (owner>=R32(data,HeaderBodyCountOffset) || BodyMotion(data,owner)!=RigidMotionKind.Static ||
                !Enum.IsDefined(policy) || (policy!=BodyTargetKind.NamedBody ? target!=NoBody :
                    target>=R32(data,HeaderBodyCountOffset) || BodyMotion(data,target)!=RigidMotionKind.Dynamic) ||
                !ContactWorkDeclaration.AdmitsCooldown(R32(record,ContactWorkCooldownOffset)) ||
                R32(record,ContactWorkStoreOffset) >= layout.EnergyStores || !AllZero(record[ContactWorkPaddingOffset..]))
                throw new ArgumentException("Invalid contact-work declaration or padding.");
            new ContactSpeed(RF(record,ContactWorkTargetSpeedOffset)).Validate(LinearVelocity.MaximumSpeed);
            new ContactSpeed(RF(record,ContactWorkAdmissionSpeedOffset)).Validate(LinearVelocity.MaximumSpeed);
            stores[i]=new(new(R64(record,RecordIdentityOffset)),new(R64(data,layout.BodyOffset((int)owner))),R32(record,ContactWorkOccurrenceCountOffset),
                new(R64(data, layout.EnergyStoreOffset((int)R32(record, ContactWorkStoreOffset)))));
        }
    }

    /// <summary>The body table is the first table after the header, so a body row needs no other population.</summary>
    private static RigidMotionKind BodyMotion(ReadOnlySpan<byte> data, uint slot) => slot < R32(data, HeaderBodyCountOffset)
        ? (RigidMotionKind)R32(data, HeaderBytes + checked((int)slot * BodyBytes) + BodyMotionOffset)
        : throw new ArgumentException("A body slot exceeds the world's body table.");

    private static void ValidateContactWorkCandidate(ReadOnlySpan<byte> candidate,ReadOnlySpan<byte> source,SimulationTick expectedTick,
        PhysicsInteractionSnapshot candidateInteractions, PhysicsInteractionSnapshot sourceInteractions)
    {
        var before=ReadContactWorks(source,sourceInteractions); var after=ReadContactWorks(candidate,candidateInteractions);
        after.ValidateAdvance(before);
        after.ValidateReleasedWork(before, new PhysicsEnergyStoreRead(ReadEnergyStores(candidate)),
            new PhysicsEnergyStoreRead(ReadEnergyStores(source)),
            new PhysicsFiniteWorkActuatorRead(ReadFiniteWorkActuators(candidate)), new PhysicsFiniteWorkActuatorRead(ReadFiniteWorkActuators(source)));
        var layout=Layout(source);
        for (var i=0; i<before.Count; i++)
        {
            var offset=layout.ContactWorkOffset(i);
            if (!source.Slice(offset,ContactWorkDeclarationBytes).SequenceEqual(candidate.Slice(offset,ContactWorkDeclarationBytes)))
                throw new ArgumentException("Contact-work declaration changed.");
        }
        for (var i=0; i<after.OccurrenceCount; i++)
        {
            var current=after.Occurrence(i);
            before.TryOccurrence(current.Work,current.Target,out var previous);
            if (current.Sequence==previous.Sequence) continue;
            var cooldown=R32(source,layout.ContactWorkOffset((int)current.Work.Value)+ContactWorkCooldownOffset);
            if (expectedTick.Value==0 || Before(current.EventOrdinal,current.EventPhase,R32(source,HeaderSourceOrdinalOffset),0f) ||
                (previous.Sequence!=0 && Before(current.EventOrdinal,current.EventPhase,
                    checked(previous.EventOrdinal+cooldown),previous.EventPhase)))
                throw new ArgumentException("Contact work occurred outside this commit or before cooldown.");
        }
    }
}
