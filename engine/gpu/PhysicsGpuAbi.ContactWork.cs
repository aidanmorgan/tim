using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public static PhysicsContactWorkRead ReadContactWorks(ReadOnlySpan<byte> data)
    {
        if (ReadFailure(data)!=PhysicsFailure.None) throw new ArgumentException("Invalid contact-work read.");
        var count=checked((int)R32(data,104)); var eventCount=checked((int)R32(data,108));
        Span<ContactWorkRead> stores=stackalloc ContactWorkRead[PhysicsSceneDeclaration.ContactWorkCapacity];
        Span<ContactWorkOccurrence> events=stackalloc ContactWorkOccurrence[PhysicsContactWorkRead.OccurrenceCapacity];
        for (var i=0; i<count; i++)
        {
            var record=data.Slice(ContactWorksOffset+i*ContactWorkBytes,ContactWorkBytes);
            var owner=R32(record,8); var policy=(BodyTargetKind)R32(record,12); var target=R32(record,28);
            if (owner>=R32(data,12) || (RigidMotionKind)R32(data,BodiesOffset+(int)owner*BodyBytes+8)!=RigidMotionKind.Static ||
                !Enum.IsDefined(policy) || (policy==BodyTargetKind.AllDynamic ? target!=NoBody :
                    target>=R32(data,12) || (RigidMotionKind)R32(data,BodiesOffset+(int)target*BodyBytes+8)!=RigidMotionKind.Dynamic) ||
                R32(record,24) is <9 or >14400 || !AllZero(record[22..24]) ||
                !AllZero(record[36..48]) || !AllZero(record[50..]))
                throw new ArgumentException("Invalid contact-work declaration or padding.");
            PhysicsDeclarationBounds.Range(RH(record,16),(Half)0,(Half)20);
            PhysicsDeclarationBounds.Range(RH(record,18),(Half)0,(Half)200);
            PhysicsDeclarationBounds.Range(RH(record,20),(Half)0,(Half)64);
            PhysicsDeclarationBounds.Range(RH(record,48),(Half)0,RH(record,18));
            stores[i]=new(new(R64(record,0)),new(R64(data,BodiesOffset+(int)owner*BodyBytes)),R32(record,32),new(RH(record,48)));
            if (stores[i].OccurrenceCount==0 && !record[18..20].SequenceEqual(record[48..50]))
                throw new ArgumentException("Unused work owner spent its reservoir.");
        }
        for (var i=0; i<eventCount; i++)
        {
            var record=data.Slice(WorkOccurrencesOffset+i*WorkOccurrenceBytes,WorkOccurrenceBytes);
            if (!AllZero(record[27..])) throw new ArgumentException("Nonzero work occurrence padding.");
            var value=new ContactWorkOccurrence(new(BinaryPrimitives.ReadUInt16LittleEndian(record)),
                new(BinaryPrimitives.ReadUInt16LittleEndian(record[2..])),new(R64(record,4)),R32(record,12),
                R32(record,16),RH(record,20),new(RH(record,22)),new(RH(record,24)),(ContactWorkEffect)record[26]);
            value.Validate();
            if (value.Work.Value>=count) throw new ArgumentException("Occurrence has no owner.");
            var declaration=data.Slice(ContactWorksOffset+value.Work.Value*ContactWorkBytes,ContactWorkBytes);
            var found=false;
            for (var body=0; body<R32(data,12); body++)
                if (R64(data,BodiesOffset+body*BodyBytes)==value.Target.Value &&
                    (RigidMotionKind)R32(data,BodiesOffset+body*BodyBytes+8)==RigidMotionKind.Dynamic &&
                    ((BodyTargetKind)R32(declaration,12)==BodyTargetKind.AllDynamic || R32(declaration,28)==body))
                    found=true;
            if (!found || (value.Sequence!=0 && (value.Collider.Value>=R32(data,16) ||
                R32(data,CollidersOffset+value.Collider.Value*ColliderBytes+8)!=R32(declaration,8) ||
                !AdmittedTime(value.EventOrdinal,value.EventPhase,R32(data,88)) || value.ApproachSpeed.Value<RH(declaration,20))))
                throw new ArgumentException("Occurrence does not bind its dynamic target, collider or time.");
            events[i]=value;
        }
        if (!AllZero(data[(ContactWorksOffset+count*ContactWorkBytes)..WorkOccurrencesOffset]) ||
            !AllZero(data[(WorkOccurrencesOffset+eventCount*WorkOccurrenceBytes)..OrientationSensorsOffset]))
            throw new ArgumentException("Unused contact-work slots changed.");
        return new(stores[..count],events[..eventCount]);
    }

    private static void ValidateContactWorkCandidate(ReadOnlySpan<byte> candidate,ReadOnlySpan<byte> source,SimulationTick expectedTick)
    {
        var before=ReadContactWorks(source); var after=ReadContactWorks(candidate);
        after.ValidateAdvance(before);
        for (var i=0; i<before.Count; i++)
        {
            var offset=ContactWorksOffset+i*ContactWorkBytes;
            if (!source.Slice(offset,32).SequenceEqual(candidate.Slice(offset,32)))
                throw new ArgumentException("Contact-work declaration changed.");
        }
        for (var i=0; i<after.OccurrenceCount; i++)
        {
            var previous=before.Occurrence(i); var current=after.Occurrence(i);
            if (current.Sequence==previous.Sequence) continue;
            var cooldown=R32(source,ContactWorksOffset+current.Work.Value*ContactWorkBytes+24);
            if (expectedTick.Value==0 || Before(current.EventOrdinal,current.EventPhase,R32(source,88),(Half)0) ||
                (previous.Sequence!=0 && Before(current.EventOrdinal,current.EventPhase,
                    checked(previous.EventOrdinal+cooldown),previous.EventPhase)))
                throw new ArgumentException("Contact work occurred outside this commit or before cooldown.");
        }
    }
}
