using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Presentation;

/// <summary>Declared audio assets; cosmetic motion is routed through the shared animation registry.</summary>
public sealed class SceneAcousticBinding
{
    public AudioStreamPlayer3D Player { get; }
    private readonly Dictionary<ToneBand,AudioStreamWav> _tones;
    public SceneAcousticBinding(AudioStreamPlayer3D player,IReadOnlyDictionary<ToneBand,AudioStreamWav> tones)
    {
        ArgumentNullException.ThrowIfNull(player);ArgumentNullException.ThrowIfNull(tones);
        Player=player;_tones=new(tones);
        if(_tones.Count==0)throw new ArgumentException("Playback requires declared tones.");
        foreach(var pair in _tones)
            if(!Enum.IsDefined(pair.Key)||!GodotObject.IsInstanceValid(pair.Value))
                throw new ArgumentException("Invalid acoustic asset binding.");
    }
    internal AudioStreamWav Resolve(ToneBand tone)
    {
        if(!GodotObject.IsInstanceValid(Player)||!Player.IsInsideTree())
            throw new InvalidOperationException("Acoustic player is no longer attached.");
        return _tones.TryGetValue(tone,out var stream)&&GodotObject.IsInstanceValid(stream)
            ?stream:throw new InvalidOperationException("Undeclared acoustic tone.");
    }
    internal void Play(float strength,AudioStreamWav stream)
    {
        Player.Stream=stream;
        Player.VolumeDb=-15+20*Mathf.Log(strength)/Mathf.Log(10);
        Player.Play();
    }
}

public enum AcousticPublicationPhase { Idle, Staged, Publishing, Faulted, Removed }

/// <summary>Per-Run binding registry. Stage immutable final tick occurrences before commit;
/// dispatch only after commit. Every tick is submitted, even without a rendered frame.</summary>
public sealed class SceneAcousticRun
{
    private readonly record struct Source(MachinePart Part,SceneAcousticBinding Binding);
    private readonly record struct SourceId(int Index);
    private readonly record struct Cue(SourceId Source,ToneBand Tone,float Strength);
    private readonly Source[] _sources;
    // Reserved within the world's event-stream identity space.
    public static readonly EventStreamId StreamId=new(0);
    private readonly CommittedEventStream<Cue> _events;
    private readonly AudioStreamWav?[] _streams;
    public AcousticPublicationPhase Phase { get; private set; }
    public int PublishedCount { get; private set; }
    public int UnconfirmedCount => _events.PendingCount;
    public int Capacity => _events.Capacity;
    public int HighWaterMark => _events.HighWaterMark;
    public Exception? Failure { get; private set; }
    private int _completedInBatch;
    private SceneAcousticWavefrontRun? _wavefronts;
    public SceneAcousticRun(IEnumerable<MachinePart> parts,PoseReadStamp initial,int capacity)
    {
        _events=new(StreamId,initial,capacity);_streams=new AudioStreamWav?[capacity];
        var sources=new List<Source>();
        var players=new HashSet<AudioStreamPlayer3D>();
        foreach(var part in parts)
            if(part.AcousticPlayback is { } binding)
            {
                if(!players.Add(binding.Player))throw new ArgumentException("Duplicate acoustic player owner.");
                sources.Add(new(part,binding));
            }
        _sources=sources.ToArray();
    }
    public void RequireReady()
    {
        if(Phase!=AcousticPublicationPhase.Idle)
            throw new InvalidOperationException("Acoustic publication is not ready; a faulted run requires Reset or Load.",Failure);
        _events.RequireWritable();
    }
    public void Stage(PoseReadStamp stamp,SceneAcousticWavefrontRun wavefronts)
    {
        RequireReady();
        var tick=checked((int)stamp.Revision.Value-1);
        ArgumentNullException.ThrowIfNull(wavefronts);
        if(_wavefronts is not null&&_wavefronts!=wavefronts)throw new ArgumentException("Wavefront consumer changed within a run.");
        _wavefronts=wavefronts;
        _events.Begin(stamp);_completedInBatch=0;wavefronts.Begin(stamp);
        for(var i=0;i<_sources.Length;i++)
            foreach(var pulse in _sources[i].Part.AcousticPulses)
                if(pulse.EmissionTick==tick)
                {
                    var stream=_sources[i].Binding.Resolve(pulse.Tone);
                    var id=_events.Append(new(new(i),pulse.Tone,pulse.Strength));
                    wavefronts.Append(_sources[i].Part,id,pulse);
                    _streams[_events.StagedCount-1]=stream;
                }
        _events.Seal();Phase=AcousticPublicationPhase.Staged;
    }
    public void Discard()
    {
        if(Phase is not (AcousticPublicationPhase.Idle or AcousticPublicationPhase.Staged))
            throw new InvalidOperationException("Only an uncommitted acoustic batch can be discarded.");
        if(_events.Phase is EventStreamPhase.Writing or EventStreamPhase.Rejected or EventStreamPhase.Staged)
            _events.Discard();
        _wavefronts?.Discard();
        Array.Clear(_streams);_completedInBatch=0;Phase=AcousticPublicationPhase.Idle;
    }
    public void Publish(SceneAcousticMotionRun motion)
    {
        ArgumentNullException.ThrowIfNull(motion);
        if(Phase!=AcousticPublicationPhase.Staged)throw new InvalidOperationException("No staged acoustic tick.");
        _events.Commit();
        Phase=AcousticPublicationPhase.Publishing;
        try
        {
            if(_wavefronts!=motion.Wavefronts)throw new InvalidOperationException("Wavefront and motion consumers differ.");
            _wavefronts.Commit(_events.CurrentStamp);
            motion.Commit(_events.CurrentStamp);
            while(_events.PendingCount>0)
            {
                var occurrence=_events.Peek();var cue=occurrence.Payload;
                var source=_sources[cue.Source.Index];
                _=source.Binding.Resolve(cue.Tone);
                motion.Admit(source.Part,occurrence.Stamp,occurrence.Id,cue.Strength);
                source.Binding.Play(cue.Strength,_streams[_completedInBatch]!);
                _events.Acknowledge(occurrence.Id);
                _streams[_completedInBatch]=null;
                PublishedCount++;_completedInBatch++;
            }
            _completedInBatch=0;
            Phase=AcousticPublicationPhase.Idle;
        }
        catch(Exception failure)
        {
            // Playback may already have happened. Preserve the uncertain occurrence and
            // the unattempted tail; automatic retry could duplicate an external effect.
            Failure=failure;Phase=AcousticPublicationPhase.Faulted;
            throw;
        }
    }
    public void Remove()
    {
        if(Phase==AcousticPublicationPhase.Publishing)
            throw new InvalidOperationException("Cannot remove acoustic bindings during publication.");
        if(Phase==AcousticPublicationPhase.Removed)return;
        _events.Remove();Array.Clear(_streams);_completedInBatch=0;Phase=AcousticPublicationPhase.Removed;
        foreach(var source in _sources)
            if(GodotObject.IsInstanceValid(source.Binding.Player))source.Binding.Player.Stop();
    }
}
