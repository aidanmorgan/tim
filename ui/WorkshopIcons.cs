using Godot;
using System;
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

    /// <summary>An element's palette icon: the icon.svg its package ships (ElementMetadata.IconPath, Story 9A.18), exported byte for
    /// byte and rasterised at twice its 24 px design size, as the UI pictograms are.</summary>
    public static Texture2D Element(string iconPath)
    {
        var key = "element:" + iconPath;
        if (Cache.TryGetValue(key, out var cached)) return cached;
        if (!Godot.FileAccess.FileExists(iconPath)) throw new System.InvalidOperationException("Cannot load element icon: " + iconPath);
        var texture = Rasterise(Godot.FileAccess.GetFileAsString(iconPath), iconPath);
        Cache.Add(key, texture);
        return texture;
    }

    // Small original vector pictograms for UI controls and socket choices share the toolbar's stroke and palette. Element icons ship
    // in their packages (Element); no element is drawn here.
    public static Texture2D Pictogram(string kind)
    {
        var key = "pictogram:" + kind;
        if (Cache.TryGetValue(key, out var cached)) return cached;
        var drawing = kind switch
        {
            "extend_input" => "<path d='M3 8h7v8H3zM10 12h11m-4-4 4 4-4 4'/>",
            "retract_input" => "<path d='M3 8h7v8H3zM21 12H11m4-4-4 4 4 4'/>",
            "extended_output" => "<path d='M2 7h7v10H2zM9 12h11M20 7v10M14 3h7m-3-2 3 2-3 2'/>",
            "retracted_output" => "<path d='M2 7h7v10H2zM9 12h3M12 7v10M21 3h-7m3-2-3 2 3 2'/>",
            "resize" => "<path d='M4 9V4h5M15 20h5v-5M4 4l6 6m4 4 6 6'/><rect x='9' y='9' width='6' height='6'/>",
            // The Set and Reset socket choices mirror the on-part tactile marks: a raised bar for Set, a hollow ring for Reset.
            "socket_set" => "<rect x='9.5' y='3' width='5' height='18' rx='1.5' fill='#293954'/>",
            "socket_reset" => "<circle cx='12' cy='12' r='7.5' stroke-width='3'/>",
            "set_input" => "<circle cx='12' cy='12' r='9'/><path d='M12 7v10'/>",
            "reset_input" => "<circle cx='12' cy='12' r='9'/><circle cx='12' cy='12' r='4'/>",
            "first_input" => "<circle cx='12' cy='12' r='9'/><path d='M12 7v10'/>",
            "second_input" => "<circle cx='12' cy='12' r='9'/><path d='M9 7v10M15 7v10'/>",
            "move" => "<path d='M12 2v20M2 12h20M9 5l3-3 3 3M9 19l3 3 3-3M5 9l-3 3 3 3M19 9l3 3-3 3'/>",
            "front" => "<rect x='3' y='3' width='18' height='18' rx='2'/><path d='M3 16h18'/>",
            "cube" => "<path d='m12 2 9 5v10l-9 5-9-5V7Zm0 10L3 7m9 5 9-5m-9 5v10'/>",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No UI pictogram has this name; element icons ship in their packages.")
        };
        var texture = Rasterise("<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='#293954' stroke-width='1.7' stroke-linecap='round' stroke-linejoin='round'>" + drawing + "</svg>", kind);
        Cache.Add(key, texture);
        return texture;
    }

    private static ImageTexture Rasterise(string svg, string name)
    {
        using var image = new Image();
        var error = image.LoadSvgFromString(svg, 2);
        if (error != Error.Ok) throw new System.InvalidOperationException("Invalid pictogram: " + name);
        return ImageTexture.CreateFromImage(image);
    }

    public static void Apply(Button button, string action)
    {
        var (_, icon) = action switch
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
            _ when action.EndsWith(" −", StringComparison.Ordinal) => (action[..^2], "minus"),
            _ when action.EndsWith(" +", StringComparison.Ordinal) => (action[..^2], "plus"),
            _ => (action, "")
        };
        button.SetMeta("action_label", action);
        button.Text = "";
        button.Icon = icon.StartsWith("custom:", StringComparison.Ordinal) ? Pictogram(icon[7..]) : icon.Length > 0 ? Load(icon) : Pictogram("move");
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
