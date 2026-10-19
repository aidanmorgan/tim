using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>The contact-work and work-occurrence records: admission, read and candidate validation. Every field is named
/// (Story 9.4b, Murdoch-2 R2-F4); <c>WorkshopAbiLiteralScanTests</c> fails on a literal offset in this file.</summary>
public static partial class PhysicsGpuAbi
{
    private static void AdmitContactWorks(Span<byte> data, PhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.ContactWorks.Length; i++)
        {
            var work = scene.ContactWorks[i]; var record = data.Slice(scene.Layout.GetContactWorkOffset(work.Id), ContactWorkBytes);
            U64(record, 0, work.Id.Value); U32(record, ContactWorkOwnerOffset, BodySlot(scene, work.Owner));
            U32(record, ContactWorkTargetKindOffset, (uint)work.Targets.Kind);
            U32(record, ContactWorkNamedTargetOffset, work.Targets.Kind == BodyTargetKind.NamedBody ? BodySlot(scene, work.Targets.Body) : NoBody);
            BinaryPrimitives.WriteSingleLittleEndian(record[ContactWorkTargetSpeedOffset..], work.TargetSpeed.Value);
            BinaryPrimitives.WriteSingleLittleEndian(record[ContactWorkAdmissionSpeedOffset..], work.Threshold.Value);
            U32(record, ContactWorkCooldownOffset, work.CooldownPhysicalSteps);
            BinaryPrimitives.WriteSingleLittleEndian(record[ContactWorkPreloadOffset..], work.InitialEnergy.Value);
            BinaryPrimitives.WriteSingleLittleEndian(record[ContactWorkAvailableOffset..], work.InitialEnergy.Value);
        }
        var occurrence = 0;
        for (var work = 0; work < scene.ContactWorks.Length; work++)
            foreach (var body in scene.Bodies)
                if (body.Motion == RigidMotionKind.Dynamic && scene.ContactWorks[work].Targets.Contains(body.Id))
                {
                    var slot = data.Slice(WorkOccurrencesOffset + occurrence++ * WorkOccurrenceBytes, WorkOccurrenceBytes);
                    BinaryPrimitives.WriteUInt16LittleEndian(slot[WorkOccurrenceWorkOffset..], (ushort)work);
                    U64(slot, WorkOccurrenceTargetOffset, body.Id.Value);
                }
        U32(data, HeaderWorkOccurrenceCountOffset, (uint)occurrence);
    }

    public static PhysicsContactWorkRead ReadContactWorks(ReadOnlySpan<byte> data)
    {
        if (ReadFailure(data)!=PhysicsFailure.None) throw new ArgumentException("Invalid contact-work read.");
        var count=checked((int)R32(data,HeaderContactWorkCountOffset)); var eventCount=checked((int)R32(data,HeaderWorkOccurrenceCountOffset));
        Span<ContactWorkRead> stores=stackalloc ContactWorkRead[PhysicsSceneDeclaration.ContactWorkCapacity];
        Span<ContactWorkOccurrence> events=stackalloc ContactWorkOccurrence[PhysicsContactWorkRead.OccurrenceCapacity];
        for (var i=0; i<count; i++)
        {
            var record=data.Slice(ContactWorksOffset+i*ContactWorkBytes,ContactWorkBytes);
            var owner=R32(record,ContactWorkOwnerOffset); var policy=(BodyTargetKind)R32(record,ContactWorkTargetKindOffset);
            var target=R32(record,ContactWorkNamedTargetOffset);
            if (owner>=R32(data,HeaderBodyCountOffset) || BodyMotion(data,owner)!=RigidMotionKind.Static ||
                !Enum.IsDefined(policy) || (policy==BodyTargetKind.AllDynamic ? target!=NoBody :
                    target>=R32(data,HeaderBodyCountOffset) || BodyMotion(data,target)!=RigidMotionKind.Dynamic) ||
                R32(record,ContactWorkCooldownOffset) is <9 or >14400 ||
                !AllZero(record[ContactWorkReservedOffset..ContactWorkAvailableOffset]) || !AllZero(record[ContactWorkPaddingOffset..]))
                throw new ArgumentException("Invalid contact-work declaration or padding.");
            new ContactSpeed(RF(record,ContactWorkTargetSpeedOffset)).Validate(20f);
            new Joules(RF(record,ContactWorkPreloadOffset)).Validate();
            new ContactSpeed(RF(record,ContactWorkAdmissionSpeedOffset)).Validate(64f);
            new Joules(RF(record,ContactWorkAvailableOffset)).Validate();
            if (RF(record,ContactWorkAvailableOffset) > RF(record,ContactWorkPreloadOffset)) throw new ArgumentException("Work store exceeds preload.");
            stores[i]=new(new(R64(record,0)),new(R64(data,BodiesOffset+(int)owner*BodyBytes)),R32(record,ContactWorkOccurrenceCountOffset),
                new(RF(record,ContactWorkAvailableOffset)),new(RF(record,ContactWorkSuppliedOffset)));
            if (stores[i].OccurrenceCount==0 && R32(record,ContactWorkPreloadOffset)!=R32(record,ContactWorkAvailableOffset))
                throw new ArgumentException("Unused work owner spent its reservoir.");
        }
        for (var i=0; i<eventCount; i++)
        {
            var record=data.Slice(WorkOccurrencesOffset+i*WorkOccurrenceBytes,WorkOccurrenceBytes);
            if (!AllZero(record[WorkOccurrencePaddingOffset..])) throw new ArgumentException("Nonzero work occurrence padding.");
            var value=new ContactWorkOccurrence(new(BinaryPrimitives.ReadUInt16LittleEndian(record[WorkOccurrenceWorkOffset..])),
                new(BinaryPrimitives.ReadUInt16LittleEndian(record[WorkOccurrenceColliderOffset..])),new(R64(record,WorkOccurrenceTargetOffset)),
                R32(record,WorkOccurrenceSequenceOffset),R32(record,WorkOccurrenceOrdinalOffset),RH(record,WorkOccurrencePhaseOffset),
                new(RF(record,WorkOccurrenceApproachOffset)),new(RF(record,WorkOccurrenceDebitOffset)),(ContactWorkEffect)record[WorkOccurrenceEffectOffset]);
            value.Validate();
            if (value.Work.Value>=count) throw new ArgumentException("Occurrence has no owner.");
            var declaration=data.Slice(ContactWorksOffset+value.Work.Value*ContactWorkBytes,ContactWorkBytes);
            var found=false;
            for (var body=0u; body<R32(data,HeaderBodyCountOffset); body++)
                if (R64(data,BodiesOffset+(int)body*BodyBytes)==value.Target.Value && BodyMotion(data,body)==RigidMotionKind.Dynamic &&
                    ((BodyTargetKind)R32(declaration,ContactWorkTargetKindOffset)==BodyTargetKind.AllDynamic || R32(declaration,ContactWorkNamedTargetOffset)==body))
                    found=true;
            if (!found || (value.Sequence!=0 && (value.Collider.Value>=R32(data,HeaderColliderCountOffset) ||
                R32(data,CollidersOffset+value.Collider.Value*ColliderBytes+ColliderBodyOffset)!=R32(declaration,ContactWorkOwnerOffset) ||
                !AdmittedTime(value.EventOrdinal,value.EventPhase,R32(data,HeaderSourceOrdinalOffset)) || value.ApproachSpeed.Value<RF(declaration,ContactWorkAdmissionSpeedOffset))))
                throw new ArgumentException("Occurrence does not bind its dynamic target, collider or time.");
            events[i]=value;
        }
        if (!AllZero(data[(ContactWorksOffset+count*ContactWorkBytes)..WorkOccurrencesOffset]) ||
            !AllZero(data[(WorkOccurrencesOffset+eventCount*WorkOccurrenceBytes)..OrientationSensorsOffset]))
            throw new ArgumentException("Unused contact-work slots changed.");
        return new(stores[..count],events[..eventCount]);
    }

    private static RigidMotionKind BodyMotion(ReadOnlySpan<byte> data, uint slot) =>
        (RigidMotionKind)R32(data, BodiesOffset + checked((int)slot) * BodyBytes + BodyMotionOffset);

    private static void ValidateContactWorkCandidate(ReadOnlySpan<byte> candidate,ReadOnlySpan<byte> source,SimulationTick expectedTick)
    {
        var before=ReadContactWorks(source); var after=ReadContactWorks(candidate);
        after.ValidateAdvance(before);
        for (var i=0; i<before.Count; i++)
        {
            var offset=ContactWorksOffset+i*ContactWorkBytes;
            if (!source.Slice(offset,ContactWorkDeclarationBytes).SequenceEqual(candidate.Slice(offset,ContactWorkDeclarationBytes)) ||
                R32(source,offset+ContactWorkPreloadOffset)!=R32(candidate,offset+ContactWorkPreloadOffset))
                throw new ArgumentException("Contact-work declaration changed.");
        }
        for (var i=0; i<after.OccurrenceCount; i++)
        {
            var previous=before.Occurrence(i); var current=after.Occurrence(i);
            if (current.Sequence==previous.Sequence) continue;
            var cooldown=R32(source,ContactWorksOffset+current.Work.Value*ContactWorkBytes+ContactWorkCooldownOffset);
            if (expectedTick.Value==0 || Before(current.EventOrdinal,current.EventPhase,R32(source,HeaderSourceOrdinalOffset),(Half)0) ||
                (previous.Sequence!=0 && Before(current.EventOrdinal,current.EventPhase,
                    checked(previous.EventOrdinal+cooldown),previous.EventPhase)))
                throw new ArgumentException("Contact work occurred outside this commit or before cooldown.");
        }
    }
}
