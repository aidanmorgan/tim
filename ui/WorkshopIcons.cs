using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public static class WorkshopIcons
{
    private static readonly Dictionary<string, Texture2D> Cache = new();
    public static Texture2D Load(string name)
    {
        if (!Cache.TryGetValue(name, out var texture))
        {
            // Resource loading follows SVG import remaps in exported Godot packs.
            texture = GD.Load<Texture2D>("res://assets/icons/" + name + ".svg");
            if (texture == null) throw new System.InvalidOperationException("Cannot load icon: " + name);
            Cache.Add(name, texture);
        }
        return texture;
    }

    // Small original vector pictograms share the toolbar's stroke and palette.
    public static Texture2D Pictogram(string kind)
    {
        var key = "pictogram:" + kind;
        if (Cache.TryGetValue(key, out var cached)) return cached;
        var drawing = kind switch
        {
            "ball" => "<circle cx='12' cy='12' r='9'/><path d='M5 6q12 2 13 12'/>",
            "tennis" => "<circle cx='12' cy='12' r='9'/><path d='M5 5q12 7 0 14M19 5q-12 7 0 14'/>",
            "bowling" => "<circle cx='12' cy='12' r='9'/><circle cx='10' cy='7' r='1'/><circle cx='15' cy='9' r='1'/><circle cx='10' cy='12' r='1'/>",
            "balloon" => "<ellipse cx='12' cy='9' rx='7' ry='8'/><path d='m10 18 2-2 2 2m-2 0q-4 3 0 5'/>",
            "ramp" => "<path d='M3 19 21 6v13Z'/>",
            "wall" => "<rect x='3' y='4' width='18' height='16' rx='1'/><path d='M3 12h18M9 4v8m6 0v8'/>",
            "resize" => "<path d='M4 9V4h5M15 20h5v-5M4 4l6 6m4 4 6 6'/><rect x='9' y='9' width='6' height='6'/>",
            "basket" => "<path d='m3 7 3 13h12l3-13M2 7h20M8 7l2 13M16 7l-2 13M5 13h14'/>",
            "battery" => "<rect x='5' y='4' width='14' height='17' rx='2'/><path d='M9 4V2h6v2M8 12h8m-4-4v8'/>",
            "motor" => "<rect x='3' y='6' width='14' height='13' rx='3'/><path d='M17 11h4v4h-4M6 19v2m8-2v2'/><circle cx='10' cy='12' r='3'/>",
            "bumper" => "<circle cx='12' cy='12' r='6'/><circle cx='12' cy='12' r='3'/><path d='M12 1v2M12 21v2M1 12h2M21 12h2M4 4l2 2M18 18l2 2M4 20l2-2M18 6l2-2'/>",
            "spring" => "<path d='M4 3h16M12 3v2L5 8l14 4-14 4 7 3v2M4 21h16'/>",
            "fan" => "<circle cx='12' cy='12' r='2'/><path d='M10 10C1 2 16 0 14 10M14 12c12-3 5 12-2 2M10 14c-3 11-13-1 0-2'/>",
            "switch" => "<rect x='3' y='15' width='18' height='6' rx='2'/><path d='m12 15 5-10'/><circle cx='18' cy='4' r='2'/>",
            "domino" => "<rect x='5' y='2' width='14' height='20' rx='2'/><path d='M5 12h14M9 6h1m4 2h1M9 16h1m4 2h1'/>",
            "lamp" => "<path d='M8 16a7 7 0 1 1 8 0v3H8ZM9 22h6'/>",
            "set_input" => "<circle cx='12' cy='12' r='9'/><path d='M12 7v10'/>",
            "reset_input" => "<circle cx='12' cy='12' r='9'/><circle cx='12' cy='12' r='4'/>",
            "latch" => "<rect x='3' y='4' width='18' height='16' rx='2'/><path d='M7 7v4M8 16l8-3'/><circle cx='17' cy='9' r='2'/>",
            "clock" => "<rect x='4' y='2' width='16' height='20' rx='2'/><path d='M12 6l-3 9'/><circle cx='8' cy='17' r='2'/><circle cx='12' cy='6' r='1'/>",
            "first_input" => "<circle cx='12' cy='12' r='9'/><path d='M12 7v10'/>",
            "second_input" => "<circle cx='12' cy='12' r='9'/><path d='M9 7v10M15 7v10'/>",
            "both_gate" => "<rect x='2' y='3' width='20' height='18' rx='2'/><path d='M7 7l9 5-9 5'/><circle cx='7' cy='7' r='1.5'/><circle cx='7' cy='17' r='1.5'/><circle cx='17' cy='12' r='1.5'/>",
            "laser" => "<rect x='2' y='8' width='9' height='8' rx='2'/><path d='M11 12h11M17 5v3M17 16v3M4 18h6'/>",
            "light_receiver" => "<circle cx='12' cy='10' r='7'/><circle cx='12' cy='10' r='3'/><path d='M12 17v4M7 21h10'/>",
            "mirror" => "<ellipse cx='12' cy='10' rx='7' ry='8'/><path d='M9 8l3-3M11 13l4-5M12 18v3M7 21h10'/>",
            "counter" => "<rect x='3' y='4' width='18' height='16' rx='2'/><circle cx='7' cy='12' r='1.5'/><circle cx='12' cy='12' r='1.5'/><circle cx='17' cy='12' r='1.5'/>",
            "pressure_plate" => "<path d='M3 15h18v5H3ZM12 2v9m-4-4 4 4 4-4M6 17h12'/>",
            "funnel" => "<path d='M3 4h18l-6 11v5l-6 2v-7Z'/>",
            "ball_detector" => "<ellipse cx='10' cy='12' rx='5' ry='9'/><path d='M3 12h17m-4-4 4 4-4 4'/>",
            "hold_timer" => "<rect x='3' y='5' width='18' height='14' rx='2'/><path d='M6 10h12M7 14h3m4 0h3M12 2v3M1 12h2m18 0h2'/>",
            "powered_gate" => "<path d='M3 7h18v14H3ZM8 7V3h8v4M7 11h10m-10 4h10m-5-9v13'/><path d='m10 4 2-2 2 2'/>",
            "pipe_bend_45" => "<path d='M3 15h5q5 0 8-5l3-4m-16 3h5q2 0 4-3l2-3M3 8v8m10-14 7 5'/>",
            "pipe_bend_90" => "<path d='M3 20h4V11a4 4 0 0 1 4-4h9V3h-9a8 8 0 0 0-8 8zm-1 0h6M20 2v6'/>",
            "pipe" => "<ellipse cx=\'5\' cy=\'12\' rx=\'3\' ry=\'7\'/><path d=\'M5 5h14c4 0 4 14 0 14H5m2-12h12m-12 10h12\'/>",
            "delay" => "<circle cx='12' cy='13' r='8'/><path d='M12 8v5l3 2M9 2h6m-3 0v3M3 7 1 5m20 2 2-2'/>",
            "flashlight" => "<path d='M3 9h11l4-3v12l-4-3H3ZM20 8l2-2m-2 6h3m-3 4 2 2M7 6h4v3'/>",
            "solar_panel" => "<path d='M4 6h16l2 12H2ZM9 6 7 18m8-12 2 12M3 12h18m-9 6v4m-4 0h8'/><circle cx='19' cy='3' r='2'/>",
            "weight" => "<path d='M7 9h10l3 12H4L7 9Z'/><circle cx='12' cy='6' r='3'/><path d='M8 15h8m-9 3h10'/>",
            "pulley" => "<circle cx='12' cy='10' r='6'/><circle cx='12' cy='10' r='2'/><path d='M6 10v11m12-11v7M9 2h6'/>",
            "rope_anchor" => "<rect x='3' y='3' width='18' height='18' rx='3'/><circle cx='12' cy='12' r='5'/><path d='M12 17v5'/>",
            "reverse_transmission" => "<circle cx='7' cy='12' r='4'/><circle cx='17' cy='12' r='4'/><path d='M3 5h7L8 3m2 2L8 7m13 12h-7l2-2m-2 2 2 2'/>",
            "conveyor" => "<rect x='2' y='9' width='20' height='10' rx='5'/><circle cx='7' cy='14' r='2'/><circle cx='17' cy='14' r='2'/><path d='M7 4h10m-3-2 3 2-3 2'/>",
            "move" => "<path d='M12 2v20M2 12h20M9 5l3-3 3 3M9 19l3 3 3-3M5 9l-3 3 3 3M19 9l3 3-3 3'/>",
            "front" => "<rect x='3' y='3' width='18' height='18' rx='2'/><path d='M3 16h18'/>",
            "cube" => "<path d='m12 2 9 5v10l-9 5-9-5V7Zm0 10L3 7m9 5 9-5m-9 5v10'/>",
            _ => "<path d='M4 12h16m-6-6 6 6-6 6'/>"
        };
        using var image = new Image();
        var error = image.LoadSvgFromString("<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='#293954' stroke-width='1.7' stroke-linecap='round' stroke-linejoin='round'>" + drawing + "</svg>", 2);
        if (error != Error.Ok) throw new System.InvalidOperationException("Invalid pictogram: " + kind);
        var texture = ImageTexture.CreateFromImage(image);
        Cache.Add(key, texture);
        return texture;
    }

    public static void Apply(Button button, string action)
    {
        var (label, icon) = action switch
        {
            "▶  Run machine" => ("Run machine", "play"),
            "■  Back to building" => ("Back to building", "square"),
            "↶  Build again" => ("Build again", "rotate-ccw"),
            "↶ Reset" => ("Reset", "rotate-ccw"),
            "↺ Undo" => ("Undo", "undo-2"),
            "↶ View" => ("View left", "rotate-ccw"),
            "View ↷" => ("View right", "rotate-cw"),
            "↶ Tilt" => ("Tilt left", "rotate-ccw"),
            "Tilt ↷" => ("Tilt right", "rotate-cw"),
            "Turn 90°" => ("Quarter turn", "rotate-cw"),
            "↕ Lift" => ("Lift", "move-vertical"),
            "↑" => ("", "arrow-up"),
            "↓" => ("", "arrow-down"),
            "Zoom +" => ("Zoom in", "zoom-in"),
            "Zoom −" => ("Zoom out", "zoom-out"),
            "Remove" => ("Remove", "trash"),
            "Connect" => ("Connect", "link"),
            "Connect set" => ("Set input · turn on", "custom:set_input"),
            "Connect reset" => ("Reset input · turn off", "custom:reset_input"),
            "Connect activation" => ("Trigger connection", "custom:switch"),
            "Connect first input" => ("First input", "custom:first_input"),
            "Connect second input" => ("Second input", "custom:second_input"),
            "Connect electricity" => ("Electrical connection", "custom:battery"),
            "Connect drive" => ("Drive connection", "custom:conveyor"),
            "Connect rope" => ("Rope connection", "custom:rope_anchor"),
            "Save" => ("Save", "save"),
            "Load" => ("Load", "folder-open"),
            "Show hint" => ("Hint", "lightbulb"),
            "Cancel / deselect" => ("Deselect", "x"),
            "More…" => ("More", "sliders-horizontal"),
            "Menu" => ("Menu", "sliders-horizontal"),
            "Goal" => ("Goal", "lightbulb"),
            "Reset camera" => ("Reset camera", "rotate-ccw"),
            "Move mode" => ("Move", "custom:move"),
            "Rotate mode" => ("Rotate", "rotate-cw"),
            "Resize mode" => ("Resize", "custom:resize"),
            "Front view" => ("Front view", "custom:front"),
            "3D view" => ("3D view", "custom:cube"),
            "Move selected here" => ("Move to layer", "custom:move"),
            "Fine rotate" => ("Fine rotate", "sliders-horizontal"),
            _ when action.EndsWith(" −") => (action[..^2], "minus"),
            _ when action.EndsWith(" +") => (action[..^2], "plus"),
            _ => (action, "")
        };
        button.SetMeta("action_label", action);
        button.Text = "";
        button.Icon = icon.StartsWith("custom:") ? Pictogram(icon[7..]) : icon.Length > 0 ? Load(icon) : Pictogram("move");
        button.ExpandIcon = false;
        button.TooltipText = "";
        button.AddThemeConstantOverride("icon_max_width", 20);
    }
}

// Compact, wrapped tooltips; long explanations belong in the side panel.
public partial class WorkshopButton : Button
{
    public override GodotObject _MakeCustomTooltip(string forText)
    {
        return new Label
        {
            Text = forText, CustomMinimumSize = new(180, 0),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
    }
}
