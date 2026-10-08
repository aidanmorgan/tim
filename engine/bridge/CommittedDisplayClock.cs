using System;
namespace CuriousContraptions.Bridge;

public enum DisplayPlayback { Running, Stopped }

/// <summary>Run-scoped rendered simulation time. Never advances physics or extrapolates.</summary>
public sealed class CommittedDisplayClock
{
    private readonly WorldGeneration _generation;
    private SimulationRevision _revision;
    private double _time;
    public CommittedDisplayClock(WorldGeneration generation)
    {
        if(generation.Value<=0)throw new ArgumentException("A display clock requires a live generation.");
        _generation=generation;
    }

    public double Select(PoseReadLease read,DisplayPlayback playback,double fraction)
    {
        if(!Enum.IsDefined(playback))throw new ArgumentOutOfRangeException(nameof(playback));
        if(!double.IsFinite(fraction)||fraction<0||fraction>1)
            throw new ArgumentOutOfRangeException(nameof(fraction));
        var previous=read.Stamp(PoseSample.Previous);var current=read.Stamp(PoseSample.Current);
        if(current.Generation!=_generation||current.Revision.Value<_revision.Value||current.SimulationTime<_time)
            throw new InvalidOperationException("Display clock received a retired or older publication.");
        var discontinuity=false;
        for(var i=0;i<read.Count;i++)
            if(read.ReadQuery(PoseSample.Previous,i).Revision!=read.ReadQuery(PoseSample.Current,i).Revision)
            {discontinuity=true;break;}
        var time=playback==DisplayPlayback.Stopped||discontinuity||fraction==1
            ?current.SimulationTime
            :previous.SimulationTime+(current.SimulationTime-previous.SimulationTime)*fraction;
        // A pause endpoint or repeated host phase must not rewind the next rendered frame.
        _time=Math.Max(_time,time);
        _revision=current.Revision;
        return _time;
    }
}
