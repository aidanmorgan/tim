using System;
using System.Linq;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    private static void ValidateTargetPolicy(ReadOnlySpan<byte> data, BodyTargetKind kind, uint named)
    {
        if(!Enum.IsDefined(kind) || (kind==BodyTargetKind.NamedBody
            ? named>=Layout(data).Bodies || BodyMotion(data,named)!=RigidMotionKind.Dynamic
            : named!=NoBody))
            throw new ArgumentException("Target policy does not name admitted dynamic bodies.");
    }

    public static bool MatchesTarget(ReadOnlySpan<byte> data, BodyTargetKind kind, uint named, uint body)
    {
        ValidateTargetPolicy(data,kind,named);
        if(body>=Layout(data).Bodies || BodyMotion(data,body)!=RigidMotionKind.Dynamic) return false;
        var payload=(PayloadPolicy)R32(data,Layout(data).BodyOffset((int)body)+BodyPayloadOffset);
        if(!Enum.IsDefined(payload)) throw new ArgumentException("Undefined body payload policy.");
        return kind switch
        {
            BodyTargetKind.NamedBody => named==body,
            BodyTargetKind.AllDynamic => true,
            BodyTargetKind.AllPayloads => payload==PayloadPolicy.CaptureTarget,
            _ => throw new ArgumentException("Undefined target policy.")
        };
    }

    public static PhysicsCaptureRead ReadCaptures(PhysicsInteractionSnapshot interactions) =>
        new(interactions.Captures.OrderBy(pair=>pair.Key.Policy.Value).ThenBy(pair=>pair.Key.Target.Value)
            .Select(pair=>pair.Value.Latch).ToArray());

    private static void ValidateCaptureState(ReadOnlySpan<byte> data, PhysicsInteractionSnapshot interactions)
    {
        ArgumentNullException.ThrowIfNull(interactions);
        var layout=Layout(data);
        foreach(var pair in interactions.Captures)
        {
            var sensor=-1; var body=-1;
            for(var i=0;i<layout.Sensors;i++)
                if(R64(data,layout.SensorOffset(i))==pair.Key.Policy.Value) sensor=i;
            for(var i=0;i<layout.Bodies;i++)
                if(R64(data,layout.BodyOffset(i))==pair.Key.Target.Value) body=i;
            if(sensor<0 || body<0) throw new ArgumentException("Capture pair has a foreign policy or target.");
            var offset=layout.SensorOffset(sensor);
            if(!MatchesTarget(data,(BodyTargetKind)R32(data,offset+SensorTargetKindOffset),
                R32(data,offset+SensorTargetOffset),(uint)body))
                throw new ArgumentException("Capture target does not belong to its policy.");
            pair.Value.Residence.Validate(R32(data,HeaderSourceOrdinalOffset));
            pair.Value.Latch.Validate();
            if(!AdmittedTime(pair.Value.Latch.EventOrdinal,pair.Value.Latch.EventPhase,R32(data,HeaderSourceOrdinalOffset)))
                throw new ArgumentException("Capture latch time exceeds the committed world.");
        }
    }

    private static void ValidateCaptureCandidate(ReadOnlySpan<byte> candidate,ReadOnlySpan<byte> source,
        PhysicsInteractionSnapshot after,PhysicsInteractionSnapshot before)
    {
        ValidateCaptureState(source,before); ValidateCaptureState(candidate,after);
        var layout=Layout(source);
        for(var i=0;i<layout.Sensors;i++)
        {
            var offset=layout.SensorOffset(i);
            if(!candidate.Slice(offset,SensorBytes).SequenceEqual(source.Slice(offset,SensorBytes)) ||
                !AllZero(candidate.Slice(offset+SensorPaddingOffset,SensorBytes-SensorPaddingOffset)))
                throw new ArgumentException("Residence policy changed or contains nonzero padding.");
            ValidateTargetPolicy(source,(BodyTargetKind)R32(source,offset+SensorTargetKindOffset),R32(source,offset+SensorTargetOffset));
        }
        for(var i=0;i<layout.Guides;i++)
        {
            var offset=layout.GuideOffset(i);
            ValidateTargetPolicy(source,(BodyTargetKind)R32(source,offset+GuideTargetKindOffset),R32(source,offset+GuideTargetOffset));
            if(!AllZero(candidate.Slice(offset+GuidePaddingOffset,GuideBytes-GuidePaddingOffset)))
                throw new ArgumentException("Nonzero guide padding.");
        }
        uint captured=0;
        foreach(var pair in after.Captures)
        {
            var current=pair.Value.Residence;
            var previous=before.Residence(pair.Key);
            var previousLatch=before.Captures.TryGetValue(pair.Key,out var retained) ? retained.Latch
                : CaptureLatch.Clear(pair.Key.Policy,pair.Key.Target);
            if(pair.Value.Latch!=previousLatch.Consume(current)) throw new ArgumentException("Capture latch changed without its earned event.");
            var sensor=-1;
            for(var i=0;i<layout.Sensors;i++) if(R64(source,layout.SensorOffset(i))==pair.Key.Policy.Value) sensor=i;
            var offset=layout.SensorOffset(sensor);
            if((SensorParticipation)R32(source,offset+SensorParticipationOffset)==SensorParticipation.Disabled &&
                (current.Phase!=ResidencePhase.Outside || current.OccurrenceCount!=0))
                throw new ArgumentException("Disabled capture policy produced residence.");
            var ordinal=R32(source,HeaderSourceOrdinalOffset);
            var sameEpisode=previous.Phase!=ResidencePhase.Outside && current.StartOrdinal==previous.StartOrdinal &&
                current.StartPhase==previous.StartPhase;
            if(current.OccurrenceCount!=0 && Before(current.EventOrdinal,current.EventPhase,ordinal,0) ||
                current.Phase!=ResidencePhase.Outside && Before(current.StartOrdinal,current.StartPhase,ordinal,0) && !sameEpisode ||
                current.Phase==ResidencePhase.Qualified && current.OccurrenceCount==0 && !(sameEpisode && previous.Phase==ResidencePhase.Qualified) ||
                sameEpisode && previous.Phase==ResidencePhase.Qualified && (current.Phase==ResidencePhase.Dwelling || current.OccurrenceCount!=0))
                throw new ArgumentException("Residence episode or qualification changed illegally.");
            captured=checked(captured+current.OccurrenceCount);
        }
        foreach(var pair in before.Captures)
            if(pair.Value.Latch.Phase==CaptureLatchPhase.Latched &&
                (!after.Captures.TryGetValue(pair.Key,out var retained) || retained.Latch!=pair.Value.Latch))
                throw new ArgumentException("Earned capture state was lost.");
        if(R32(candidate,HeaderCapturedCountOffset)!=captured) throw new ArgumentException("Capture count differs from actual occurrences.");
    }
}
