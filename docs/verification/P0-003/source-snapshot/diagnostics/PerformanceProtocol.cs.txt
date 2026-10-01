using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions;

public sealed class PlaytestPerformanceRunIdConverter : JsonConverter<PerformanceRunId>
{
    public override PerformanceRunId Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
    {
        if(reader.TokenType!=JsonTokenType.Number||!reader.TryGetInt64(out var value)||value<=0)
            throw new JsonException("A performance run must be a positive integer.");
        return new(value);
    }
    public override void Write(Utf8JsonWriter writer,PerformanceRunId value,JsonSerializerOptions options)
    {
        if(value.Value<=0)throw new JsonException("A performance run must be initialized.");
        writer.WriteNumberValue(value.Value);
    }
}
public sealed class PlaytestPerformanceStageConverter : ExactPlaytestEnumConverter<PerformanceStage>;
public sealed class PlaytestPerformanceMetricConverter : ExactPlaytestEnumConverter<PerformanceMetric>;
public sealed class PlaytestPerformanceOutcomeConverter : ExactPlaytestEnumConverter<PerformanceOutcome>;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlaytestPerformanceSample(
    [property:JsonRequired,JsonConverter(typeof(PlaytestPerformanceRunIdConverter))] PerformanceRunId Run,
    [property:JsonRequired] long Sequence,[property:JsonRequired] int Tick,
    [property:JsonRequired,JsonConverter(typeof(PlaytestPerformanceStageConverter))] PerformanceStage Stage,
    [property:JsonRequired,JsonConverter(typeof(PlaytestPerformanceOutcomeConverter))] PerformanceOutcome Outcome,
    [property:JsonRequired] int Calls,[property:JsonRequired] double Milliseconds,
    [property:JsonRequired] long AllocatedBytes);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlaytestPerformanceCounter(
    [property:JsonRequired,JsonConverter(typeof(PlaytestPerformanceRunIdConverter))] PerformanceRunId Run,
    [property:JsonRequired] long Sequence,[property:JsonRequired] int Tick,
    [property:JsonRequired,JsonConverter(typeof(PlaytestPerformanceMetricConverter))] PerformanceMetric Metric,
    [property:JsonRequired,JsonConverter(typeof(PlaytestPerformanceOutcomeConverter))] PerformanceOutcome Outcome,[property:JsonRequired] long Value);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PlaytestPerformanceBatch(
    [property:JsonRequired,JsonConverter(typeof(PlaytestPerformanceRunIdConverter))] PerformanceRunId Run,
    [property:JsonRequired] PlaytestPerformanceSample[] Samples,[property:JsonRequired] long OverwrittenSamples,
    [property:JsonRequired] PlaytestPerformanceCounter[] Counters,[property:JsonRequired] long OverwrittenCounters)
{
    public static PlaytestPerformanceBatch Capture(PerformanceBatch batch)=>new(batch.Run,
        batch.Samples.Select(s=>new PlaytestPerformanceSample(s.Run,s.Sequence,s.Tick,s.Stage,s.Outcome,
            s.Calls,s.Milliseconds,s.AllocatedBytes)).ToArray(),batch.OverwrittenSamples,
        batch.Counters.Select(c=>new PlaytestPerformanceCounter(c.Run,c.Sequence,c.Tick,c.Metric,c.Outcome,c.Value)).ToArray(),
        batch.OverwrittenCounters);
}
