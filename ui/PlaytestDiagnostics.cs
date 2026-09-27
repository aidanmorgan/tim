using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions;

// Read-only instrumentation, compiled out of normal builds. No setters, commands or auto-solver.
public partial class Workshop
{
    private string _lastPlaytestUi = "";
    private static float[] ScreenPoint(Vector2 point) => [point.X, point.Y];
    private static float[] Point(Vector3 point) => [point.X, point.Y, point.Z];
    private static float[] Orientation(Quaternion value) => [value.X, value.Y, value.Z, value.W];

    [Conditional("PLAYTEST")]
    private void TracePlaytestStart()
    {
        GD.Print("CCRUN " + JsonSerializer.Serialize(new PlaytestRun
        {
            Level = _currentLevel + 1, Precision = World.Precision,
            Connections = World.Connections.ToList(),
            Parts = World.Parts.Select(p => new PlaytestPart
            {
                Id = p.Uid, Kind = p.Definition.Id, Locked = p.Locked, Dynamic = p.Dynamic,
                Properties = new(p.Properties), Position = Point(p.Position), Rotation = Orientation(p.Quaternion)
            }).ToList()
        }, PlaytestJson.Default.PlaytestRun));
        TracePlaytestFrame();
    }

    [Conditional("PLAYTEST")]
    private void TracePlaytestReset()
    {
        GD.Print("CCRESET " + JsonSerializer.Serialize(new PlaytestRun
        {
            Level = _currentLevel + 1, Precision = World.Precision,
            Connections = World.Connections.ToList(),
            Parts = World.Parts.Select(p => new PlaytestPart
            {
                Id = p.Uid, Kind = p.Definition.Id, Locked = p.Locked, Dynamic = p.Dynamic,
                Properties = new(p.Properties), Position = Point(p.Position), Rotation = Orientation(p.Quaternion)
            }).ToList()
        }, PlaytestJson.Default.PlaytestRun));
    }

    [Conditional("PLAYTEST")]
    private void TracePlaytestFrame()
    {
        // Full early trajectory to assess easing and bounds, not merely end-state success.
        if (World.Ticks > 120 || World.Ticks % 4 != 0) return;
        GD.Print("CCFRAME " + JsonSerializer.Serialize(new PlaytestFrame
        {
            Tick = World.Ticks,
            Parts = World.Parts.Where(p => !p.Locked && !p.Dynamic).Select(p => new PlaytestPart
            {
                Id = p.Uid, Kind = p.Definition.Id,
                Properties = new(p.Properties), Position = Point(p.Position), Rotation = Orientation(p.Quaternion)
            }).ToList()
        }, PlaytestJson.Default.PlaytestFrame));
    }

    [Conditional("PLAYTEST")]
    private void TracePlaytestResult(string outcome)
    {
        GD.Print("CCRESULT " + JsonSerializer.Serialize(new PlaytestResult
        { Level = _currentLevel + 1, Precision = World.Precision, Tick = World.Ticks, Outcome = outcome },
            PlaytestJson.Default.PlaytestResult));
    }

    [Conditional("PLAYTEST")]
    private void TracePlaytestUi()
    {
        if (_inRun && World.Running) return; // Moving-body geometry is unnecessary during simulation.
        var ui = new PlaytestUi
        {
            Level = _currentLevel + 1, Running = _inRun, MenuOpen = _optionsPanel.Visible,
            Precision = World.Precision,
            DifficultyLeft = ScreenPoint(_precision.GetGlobalTransformWithCanvas() * new Vector2(2, _precision.Size.Y * .5f)),
            DifficultyRight = ScreenPoint(_precision.GetGlobalTransformWithCanvas() * new Vector2(_precision.Size.X - 2, _precision.Size.Y * .5f))
        };
        foreach (var button in FindChildren("*", "Button", true, false).OfType<WorkshopButton>())
        {
            if (!button.IsVisibleInTree()) continue;
            var point = button.GetGlobalTransformWithCanvas() * (button.Size * .5f);
            var clipped = false;
            for (Node? parent = button.GetParent(); parent != null; parent = parent.GetParent())
                if (parent is ScrollContainer scroll &&
                    !new Rect2(scroll.GetGlobalTransformWithCanvas().Origin,
                        scroll.Size * _canvas.Transform.Scale).HasPoint(point)) clipped = true;
            ui.Buttons.Add(new()
            {
                Action = button.GetMeta("action_label").AsString(),
                Kind = button.HasMeta("part_kind") ? button.GetMeta("part_kind").AsString() : "",
                Screen = ScreenPoint(point), Enabled = !button.Disabled, Clipped = clipped
            });
        }
        foreach (var part in World.Parts.Where(p => p.Visible))
            ui.Parts.Add(new() { Id = part.Uid, Kind = part.Definition.Id,
                Screen = ScreenPoint(_camera.UnprojectPosition(part.GlobalPosition)) });
        if (_selected != null) ui.Selected = _selected.Uid;
        if (_selected != null && _rotationGizmo.Visible)
        {
            ui.Mode = _rotationGizmo.ResizeMode ? "resize" : _rotationGizmo.MoveMode ? "move" : "rotate";
            if (_selected is IResizablePart resizable) ui.Dimensions = Point(resizable.Dimensions);
            ui.Center = ScreenPoint(_camera.UnprojectPosition(_selected.GlobalPosition));
            var axes = new[] { Vector3.Right, Vector3.Up, Vector3.Back };
            for (var index = 0; index < axes.Length; index++)
            {
                if (!_rotationGizmo.AxisEnabled(index)) continue;
                var axis = axes[index];
                var direction = _rotationGizmo.ResizeMode ? _selected.GlobalBasis * axis : axis;
                var handle = _rotationGizmo.HandlePosition(index);
                // Projected visible handle geometry, analogous to a DOM element's bounding box.
                ui.Handles.Add(new()
                {
                    Axis = index,
                    Screen = ScreenPoint(_camera.UnprojectPosition(handle)),
                    Unit = ScreenPoint(_camera.UnprojectPosition(_selected.GlobalPosition + direction) -
                        _camera.UnprojectPosition(_selected.GlobalPosition)),
                    Quarter = ScreenPoint(_camera.UnprojectPosition(_selected.GlobalPosition +
                        axis.Cross(handle - _selected.GlobalPosition)))
                });
            }
        }
        else if (_selected != null) ui.Selected = _selected.Uid;
        var json = JsonSerializer.Serialize(ui, PlaytestJson.Default.PlaytestUi);
        if (json == _lastPlaytestUi) return;
        _lastPlaytestUi = json;
        GD.Print("CCUI " + json);
    }
}

public sealed class PlaytestButton
{
    public string Action { get; set; } = "";
    public string Kind { get; set; } = "";
    public float[] Screen { get; set; } = [];
    public bool Enabled { get; set; }
    public bool Clipped { get; set; }
}
public sealed class PlaytestHandle
{
    public int Axis { get; set; }
    public float[] Screen { get; set; } = [];
    public float[] Unit { get; set; } = [];
    public float[] Quarter { get; set; } = [];
}
public sealed class PlaytestUiPart
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public float[] Screen { get; set; } = [];
}
public sealed class PlaytestUi
{
    public float Precision { get; set; }
    public float[] DifficultyLeft { get; set; } = [];
    public float[] DifficultyRight { get; set; } = [];
    public int Level { get; set; }
    public bool Running { get; set; }
    public bool MenuOpen { get; set; }
    public string Selected { get; set; } = "";
    public string Mode { get; set; } = "";
    public float[] Dimensions { get; set; } = [];
    public float[] Center { get; set; } = [];
    public List<PlaytestButton> Buttons { get; set; } = new();
    public List<PlaytestUiPart> Parts { get; set; } = new();
    public List<PlaytestHandle> Handles { get; set; } = new();
}
public sealed class PlaytestPart
{
    public Dictionary<string, float> Properties { get; set; } = new();
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public bool Locked { get; set; }
    public bool Dynamic { get; set; }
    public float[] Position { get; set; } = [];
    public float[] Rotation { get; set; } = [];
}
public sealed class PlaytestRun
{
    public List<ConnectionSpec> Connections { get; set; } = new();
    public int Level { get; set; }
    public float Precision { get; set; }
    public List<PlaytestPart> Parts { get; set; } = new();
}
public sealed class PlaytestFrame
{
    public int Tick { get; set; }
    public List<PlaytestPart> Parts { get; set; } = new();
}
public sealed class PlaytestResult
{
    public int Level { get; set; }
    public int Tick { get; set; }
    public float Precision { get; set; }
    public string Outcome { get; set; } = "";
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PlaytestUi))]
[JsonSerializable(typeof(PlaytestRun))]
[JsonSerializable(typeof(PlaytestFrame))]
[JsonSerializable(typeof(PlaytestResult))]
public partial class PlaytestJson : JsonSerializerContext { }
