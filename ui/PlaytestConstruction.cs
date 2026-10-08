using Godot;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions;

[JsonConverter(typeof(PlaytestConstructionEventConverter))]
public enum PlaytestConstructionEvent { Run, Reset, Save, Load }

public sealed class PlaytestConstructionEventConverter : JsonConverter<PlaytestConstructionEvent>
{
    public override PlaytestConstructionEvent Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
        reader.TokenType==JsonTokenType.String?reader.GetString() switch
        {
            "run"=>PlaytestConstructionEvent.Run,
            "reset"=>PlaytestConstructionEvent.Reset,
            "save"=>PlaytestConstructionEvent.Save,
            "load"=>PlaytestConstructionEvent.Load,
            _=>throw new JsonException("Unsupported construction observation event.")
        }:throw new JsonException("Construction observation event must be a canonical string.");
    public override void Write(Utf8JsonWriter writer,PlaytestConstructionEvent value,JsonSerializerOptions options)=>
        writer.WriteStringValue(value switch
        {
            PlaytestConstructionEvent.Run=>"run",
            PlaytestConstructionEvent.Reset=>"reset",
            PlaytestConstructionEvent.Save=>"save",
            PlaytestConstructionEvent.Load=>"load",
            _=>throw new JsonException("Undefined construction observation event.")
        });
}

/// <summary>Current construction and actual persisted UTF-8 text are distinct observations.</summary>
public sealed record PlaytestConstruction
{
    public PlaytestConstructionEvent Event { get; }
    public MachineData Construction { get; }
    public string? SavedText { get; }
    [JsonConstructor]
    public PlaytestConstruction(PlaytestConstructionEvent @event,MachineData construction,string? savedText)
    {
        if(!Enum.IsDefined(@event))throw new ArgumentOutOfRangeException(nameof(@event));
        ArgumentNullException.ThrowIfNull(construction);
        if((@event is PlaytestConstructionEvent.Save or PlaytestConstructionEvent.Load)!=(savedText is not null))
            throw new ArgumentException("Persisted observations require the actual file text.");
        if(savedText is not null&&JsonSerializer.Deserialize(savedText,MachineJson.Default.SavedMachine) is null)
            throw new ArgumentException("Saved construction text cannot be null.");
        Event=@event;Construction=construction;SavedText=savedText;
    }
}
