using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CuriousContraptions;

public partial class Workshop : Node3D
{
    private static readonly Color Navy = new("#293954"), Muted = new("#48556a"), Cream = new("#25344b"), Mint = new("#326537");
    public MachineWorld World { get; private set; } = null!;
    private Camera3D _camera = null!;
    private Node3D _cables = null!;
    private CanvasLayer _canvas = null!;
    private VBoxContainer _palette = null!;
    private ScrollContainer _paletteScroll = null!;
    private Label _title = null!, _task = null!, _status = null!, _detail = null!, _state = null!, _precisionText = null!, _depthText = null!, _time = null!, _hint = null!;
    private Button _run = null!, _cancelButton = null!, _removeButton = null!, _connectButton = null!;
    private Control _optionsPanel = null!, _objectivePanel = null!;
    private Button _hintButton = null!;
    private Vector3 _cameraPan;
    private readonly HashSet<Key> _cameraKeys = new();
    private VBoxContainer _optionsContents = null!, _partDockContents = null!;
    private Button _menuButton = null!, _goalButton = null!;
    private HBoxContainer _mainActions = null!;
    private OptionButton _picker = null!;
    private HSlider _precision = null!;
    private CheckButton _friction = null!;
    private MachinePart? _selected, _linkSource;
    private string _tool = "";
    private bool _dragging, _orbiting, _sandbox, _inRun;
    private float _depth, _azimuth, _elevation, _zoom = 13.8f;
    private List<PuzzleData> _puzzles = new();
    private int _currentLevel, _nextId = 1;
    private Dictionary<string, int> _inventory = new();
    private MachineData _buildState = new();
    private readonly List<MachineData> _undo = new();

    public override void _Ready()
    {
        World = new MachineWorld { Name = "Machine" };
        AddChild(World);
        World.Solved += OnSolved;
        _cables = new Node3D { Name = "Connections" };
        AddChild(_cables);
        MakeStage();
        MakeInterface();
        MakeGuidance();
        _puzzles = MachineCodec.ReadPuzzles(FileAccess.GetFileAsString("res://content/puzzles.json"));
        foreach (var puzzle in _puzzles) _picker.AddItem(puzzle.Title);
        _picker.AddItem("Free workshop");
        LoadLevel(0);
        GetWindow().FocusExited += ClearCameraMotion;
    }

    private void MakeStage()
    {
        var environment = new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new("#91cbed"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new("#fff5dd"), AmbientLightEnergy = .65f,
                TonemapMode = Godot.Environment.ToneMapper.Linear
            }
        };
        AddChild(environment);
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new(-38, -28, 0), LightColor = new("#fff0d6"),
            LightEnergy = 1.3f, ShadowEnabled = true, DirectionalShadowMaxDistance = 45
        });
        AddChild(new DirectionalLight3D { RotationDegrees = new(-10, 145, 0), LightColor = new("#b7ddea"), LightEnergy = .45f });
        var stage = new Node3D { Name = "Workbench" };
        AddChild(stage);
        PartArt.Box(stage, Workbench.Base.Half * 2, new("#b77c42"), Workbench.Base.At);
        PartArt.Box(stage, Workbench.Deck.Half * 2, new("#ead39b"), Workbench.Deck.At);
        // A toy workbench, not a permanent drafting grid.
        foreach (var x in new[] { -8f, 8f })
            foreach (var z in new[] { -4.5f, 4.5f })
                PartArt.Sphere(stage, .09f, new("#9c743f"), new(x, -.43f, z));
        _camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Near = .1f, Far = 100 };
        AddChild(_camera);
        UpdateCamera();
    }
    private void UpdateCamera()
    {
        var focus = new Vector3(0, 3.6f, 0) + _cameraPan;
        _camera.Position = focus + new Vector3(Mathf.Sin(_azimuth) * Mathf.Cos(_elevation), Mathf.Sin(_elevation), Mathf.Cos(_azimuth) * Mathf.Cos(_elevation)) * 26;
        _camera.LookAt(focus);
        _camera.Size = _zoom;
    }
    private static StyleBoxFlat Style(Color color, Color border = default)
    {
        var style = new StyleBoxFlat
        {
            BgColor = color, BorderColor = border,
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 12, ContentMarginBottom = 12
        };
        style.SetCornerRadiusAll(10);
        style.BorderColor = border.A > 0 ? border : new("#867961");
        style.SetBorderWidthAll(1);
        style.ShadowColor = new("#3e342a55");
        style.ShadowSize = 0;
        style.ShadowOffset = new(2, 3);
        return style;
    }
    private static Label Text(string text, int size = 16, Color? color = null)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? Cream);
        return label;
    }
    private static Button Button(string text, Action action, bool accent = false)
    {
        var button = new WorkshopButton { Text = text, CustomMinimumSize = new(40, 40), MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeColorOverride("font_color", Navy);
        button.AddThemeColorOverride("font_hover_color", Navy);
        button.AddThemeColorOverride("font_pressed_color", Navy);
        button.AddThemeStyleboxOverride("normal", Style(accent ? new("#f7cb52") : new("#ffffff00")));
        button.AddThemeStyleboxOverride("hover", Style(accent ? new("#ffe38a") : new("#fff4d8")));
        button.AddThemeStyleboxOverride("pressed", Style(accent ? new("#dbae38") : new("#cbbd9b")));
        foreach (var state in new[] { "normal", "hover", "pressed" })
        {
            var style = (StyleBoxFlat)button.GetThemeStylebox(state);
            style.ContentMarginLeft = style.ContentMarginRight = 10;
            style.SetBorderWidthAll(0);
        }
        WorkshopIcons.Apply(button, text);
        button.Pressed += action;
        return button;
    }
    private VBoxContainer Panel(Vector2 at, Vector2 size)
    {
        var panel = new PanelContainer { Position = at, Size = size };
        panel.AddThemeStyleboxOverride("panel", Style(new("#fff8e9eb"), new("#c5bca8")));
        _canvas.AddChild(panel);
        var contents = new VBoxContainer();
        contents.AddThemeConstantOverride("separation", 10);
        panel.AddChild(contents);
        return contents;
    }
    private static Label Paragraph(string text, int size, Color color, Vector2 minimum)
    {
        var label = Text(text, size, color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = minimum;
        return label;
    }
    private void MakeInterface()
    {
        _canvas = new CanvasLayer();
        AddChild(_canvas);
        var heading = Text("CURIOUS CONTRAPTIONS", 18);
        heading.Position = new(28, 23);
        _canvas.AddChild(heading);
        _picker = new OptionButton { Position = new(470, 24), Size = new(350, 42) };
        _picker.AddThemeFontSizeOverride("font_size", 16);
        _picker.AddThemeColorOverride("font_color", Navy);
        _picker.AddThemeColorOverride("font_hover_color", Navy);
        _picker.AddThemeStyleboxOverride("normal", Style(new("#eee5cc")));
        _picker.ItemSelected += index => LoadLevel((int)index);
        _canvas.AddChild(_picker);
        _state = Text("BUILD MODE", 13, Mint);
        _state.Position = new(1050, 36);
        _state.Visible = false;
        _canvas.AddChild(_state);
        _menuButton = Button("Menu", () =>
        {
            _optionsPanel.Visible = !_optionsPanel.Visible;
            _objectivePanel.Visible = false;
        });
        _menuButton.Size = new(44, 44);
        _canvas.AddChild(_menuButton);
        var left = Panel(new(24, 94), new(216, 0));
        _partDockContents = left;
        left.AddChild(Text("PARTS", 11, Muted));
        _paletteScroll = new ScrollContainer
        {
            Name = "PartsScroll", CustomMinimumSize = new(180, 40),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        left.AddChild(_paletteScroll);
        _palette = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _palette.AddThemeConstantOverride("separation", 7);
        _paletteScroll.AddChild(_palette);
        _detail = Paragraph("Choose a part, then click the workbench.", 14, Muted, new(180, 0));
        left.AddChild(_detail);
        _cancelButton = Button("Cancel / deselect", CancelTool);
        _cancelButton.Visible = false;
        left.AddChild(_cancelButton);
        var objective = Panel(new(1150, 94), new(266, 0));
        _objectivePanel = objective.GetParent<Control>();
        _objectivePanel.Name = "PuzzleInfo";
        _title = Paragraph("", 16, Navy, new(230, 0));
        objective.AddChild(_title);
        _goalButton = Button("Goal", () =>
        {
            _objectivePanel.Visible = !_objectivePanel.Visible;
            _task.Visible = _hintButton.Visible = _objectivePanel.Visible;
            _optionsPanel.Visible = false;
            if (!_objectivePanel.Visible) _hint.Visible = false;
            _objectivePanel.Size = new(266, 0);
        });
        _goalButton.Size = new(44, 44);
        _canvas.AddChild(_goalButton);
        _task = Paragraph("", 14, Muted, new(230, 0));
        _task.Name = "PuzzleIntroduction";
        objective.AddChild(_task);
        _hintButton = Button("Show hint", ShowHint);
        objective.AddChild(_hintButton);
        _hint = Paragraph("", 14, Mint, new(230, 0));
        _hint.Name = "PuzzleHint";
        _hint.Visible = false;
        objective.AddChild(_hint);
        _objectivePanel.Visible = false;

        _mainActions = new HBoxContainer();
        _mainActions.AddThemeConstantOverride("separation", 8);
        _canvas.AddChild(_mainActions);
        _run = Button("▶  Run machine", ToggleRun, true);
        _run.CustomMinimumSize = new(56, 48);
        _mainActions.AddChild(_run);
        _mainActions.AddChild(Button("↺ Undo", Undo));
        _time = Text("00.00 s", 13, Muted);
        _mainActions.AddChild(_time);
        _status = Paragraph("Choose a part to get started.", 12, Muted, new(480, 28));
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.MouseFilter = Control.MouseFilterEnum.Ignore;
        _canvas.AddChild(_status);
        // Secondary actions live in one scrollable menu, closed by default.
        var options = Panel(new(1020, 90), new(396, 680));
        _optionsPanel = options.GetParent<Control>();
        _optionsPanel.Name = "WorkshopMenu";
        var optionsScroll = new ScrollContainer { CustomMinimumSize = new(360, 650) };
        options.AddChild(optionsScroll);
        _optionsContents = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _optionsContents.AddThemeConstantOverride("separation", 10);
        optionsScroll.AddChild(_optionsContents);
        _optionsContents.AddChild(Text("WORKSHOP", 16));
        var files = new HBoxContainer();
        files.AddChild(Button("Save", Save));
        files.AddChild(Button("Load", LoadSave));
        files.AddChild(Button("↶ Reset", ResetRun));
        _optionsContents.AddChild(files);
        _optionsContents.AddChild(Text("Difficulty", 14));
        _precisionText = Text("Balanced");
        _optionsContents.AddChild(_precisionText);
        _precision = new HSlider { MinValue = 0, MaxValue = 100, Value = 45 };
        _precision.ValueChanged += PrecisionChanged;
        _optionsContents.AddChild(_precision);
        _optionsContents.AddChild(Text("Forgiving                         Precise", 12, Muted));
        _friction = new CheckButton { Text = "More surface friction" };
        _friction.AddThemeColorOverride("font_color", Navy);
        _friction.Toggled += on => { if (!_inRun) World.Realistic = on; };
        _optionsContents.AddChild(_friction);
        _depthText = Text("", 13, Muted);
        _depthText.Visible = false;
        _optionsContents.AddChild(_depthText);
        _optionsPanel.Visible = false;
        GetViewport().SizeChanged += ResizeInterface;
        ResizeInterface();
    }
    private void ResizeInterface()
    {
        var size = GetViewport().GetVisibleRect().Size;
        var factor = Mathf.Min(size.X / 1440, size.Y / 900);
        _canvas.Transform = new Transform2D(0, Vector2.One * factor, 0, Vector2.Zero);
        var layout = size / factor;
        _picker.Position = new(layout.X / 2 - 175, 24);
        _menuButton.Position = new(layout.X - 68, 24);
        _goalButton.Position = new(layout.X - 122, 24);
        _optionsPanel.Position = new(layout.X - 420, 94);
        _objectivePanel.Position = new(layout.X - 290, 94);
        _mainActions.Position = new(layout.X / 2 - 60, layout.Y - 88);
        _status.Position = new(layout.X / 2 - 240, layout.Y - 32);
        ResizePartsToolbox();
    }

    private void ResizePartsToolbox()
    {
        if (_paletteScroll == null || _partDockContents == null) return;
        var panel = _partDockContents.GetParent<PanelContainer>();
        var layoutHeight = GetViewport().GetVisibleRect().Size.Y / _canvas.Transform.Scale.Y;
        var children = _partDockContents.GetChildren().OfType<Control>().Where(c => c.Visible).ToArray();
        var chromeHeight = panel.GetThemeStylebox("panel").GetMinimumSize().Y +
            children.Where(c => c != _paletteScroll).Sum(c => c.GetCombinedMinimumSize().Y) +
            Mathf.Max(0, children.Length - 1) * _partDockContents.GetThemeConstant("separation");
        var available = Mathf.Max(40, layoutHeight - panel.Position.Y - 24 - chromeHeight);
        var height = Mathf.Clamp(_palette.GetCombinedMinimumSize().Y, 40, available);
        _paletteScroll.CustomMinimumSize = new(180, height);
        // Containers grow automatically but do not otherwise shrink after changing
        // from a full inventory to a small puzzle or hiding contextual controls.
        var targetHeight = height + chromeHeight;
        if (Mathf.Abs(panel.Size.Y - targetHeight) > .1f)
            panel.Size = new(panel.Size.X, targetHeight);
    }

    private void LoadLevel(int index)
    {
        CancelTool();
        _inRun = false;
        _sandbox = index >= _puzzles.Count;
        _currentLevel = index;
        _depth = 0;
        _nextId = 1;
        _undo.Clear();
        if (_sandbox)
        {
            _inventory = World.Registry.Definitions.Keys.ToDictionary(key => key, _ => 9999);
            World.LoadMachine(new());
            _title.Text = "Your next bright idea";
            _task.Text = "An open workbench. Add parts, connect switches to machines, and see what happens.";
        }
        else
        {
            var puzzle = _puzzles[index];
            _inventory = new(puzzle.Inventory);
            World.LoadMachine(puzzle.CreateMachine());
            _title.Text = puzzle.Title;
            _task.Text = puzzle.Description;
        }
        _buildState = World.Snapshot();
        _hint.Text = "";
        _hint.Visible = _task.Visible = _hintButton.Visible = false;
        _objectivePanel.Size = new(266, 0);
        _optionsPanel.Visible = _objectivePanel.Visible = false;
        SetBuildUi();
        RefreshPalette();
        RefreshCables();
        SetLayer(World.Bodies.FirstOrDefault()?.Position.Z ?? 0);
        RefreshLayers();
        _placementHeight = 3;
        SetBuildView(false);
    }
    private void RefreshPalette()
    {
        foreach (var child in _palette.GetChildren()) { _palette.RemoveChild(child); child.QueueFree(); }
        foreach (var key in _inventory.Keys)
        {
            var definition = World.Registry.Definitions[key];
            var remaining = Remaining(key);
            var button = Button(definition.Title + (_sandbox ? "  ∞" : "  × " + remaining), () => ChooseTool(key));
            button.Disabled = remaining <= 0 || _inRun;
            button.TooltipText = ""; // Description already appears in the drawer when chosen.
            button.Icon = WorkshopIcons.Pictogram(key);
            button.SetMeta("part_kind", key);
            button.CustomMinimumSize = new(180, 40);
            button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            button.IconAlignment = HorizontalAlignment.Left;
            // The single button owns the whole row. Decorative labels ignore
            // mouse input so the name/count activate the same placement preview.
            var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            button.AddChild(row);
            row.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            row.OffsetLeft = 40;
            row.OffsetRight = -6;
            var name = Text(definition.Title, 14, button.Disabled ? Muted : Navy);
            name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            name.ClipText = true;
            name.MouseFilter = Control.MouseFilterEnum.Ignore;
            row.AddChild(name);
            var count = Text(_sandbox ? "∞" : remaining.ToString(), 14, Muted);
            count.MouseFilter = Control.MouseFilterEnum.Ignore;
            row.AddChild(count);
            _palette.AddChild(button);
        }
    }
    private int Remaining(string kind) => _inventory.GetValueOrDefault(kind) - World.Parts.Count(p => !p.Locked && p.Definition.Id == kind);
    private void ChooseTool(string kind)
    {
        if (_inRun) return;
        ClearPreview();
        _previewRotation = Vector3.Zero;
        _placementHeight = 3;
        _tool = kind;
        _linkSource = null;
        Select(null);
        _detail.Text = World.Registry.Definitions[kind].Title;
        CreatePreview(kind);
        _status.Text = "Click to place. Escape cancels.";
    }
    private void Select(MachinePart? part)
    {
        EndGizmo(false);
        if (GodotObject.IsInstanceValid(_selected)) _selected!.SetSelected(false);
        _selected = part;
        if (part == null) { _detail.Text = ""; return; }
        part.SetSelected(true);
        if (!_inRun) SetLayer(part.Position.Z);
        _detail.Text = part.Definition.Title + (part.Locked ? " · fixed" : "");
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey panKey)
        {
            var code = panKey.PhysicalKeycode != Key.None ? panKey.PhysicalKeycode : panKey.Keycode;
            if (code is Key.W or Key.A or Key.S or Key.D or Key.Q or Key.E)
            {
                if (panKey.Pressed && !panKey.CtrlPressed && !panKey.MetaPressed && !panKey.AltPressed &&
                    GetViewport().GuiGetFocusOwner() is not (LineEdit or TextEdit))
                    _cameraKeys.Add(code);
                else _cameraKeys.Remove(code);
                return;
            }
        }
        if (_rotationGizmo != null && _rotationGizmo.Dragging) return;
        if (input is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.Right) { _orbiting = mouse.Pressed; if (_orbiting) { _buildView = false; RefreshLayerAppearance(); } }
            if (mouse.Pressed && mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                _zoom = Mathf.Clamp(_zoom + (mouse.ButtonIndex == MouseButton.WheelUp ? -.6f : .6f), 8, 22);
                UpdateCamera();
            }
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                if (!mouse.Pressed) _dragging = _lifting = false;
                else if (!_inRun) Click(mouse.Position);
            }
        }
        else if (input is InputEventMouseMotion motion)
        {
            if (_lifting) return; // Lift motion is handled globally, including over UI.
            if (_orbiting)
            {
                _azimuth -= motion.Relative.X * .006f;
                _elevation = Mathf.Clamp(_elevation + motion.Relative.Y * .004f, .2f, 1.2f);
                UpdateCamera();
            }
            else if (_dragging && _selected is { Locked: false } && !_inRun)
            {
                var point = WorkPoint(motion.Position, _buildView ? _selected.Position.Z : _selected.Position.Y);
                if (point is { } at)
                {
                    _selected.Position = ClampPlacement(at + _grabOffset, _buildView ? _selected.Position.Z : _selected.Position.Y);
                    RefreshLayerAppearance();
                    RefreshCables();
                }
            }
        }
        else if (input is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode == Key.Space) ToggleRun();
            else if (key.Keycode == Key.Escape) { _optionsPanel.Visible = false; CancelTool(); }
            else if (!_inRun)
            {
                if (key.Keycode == Key.Z && (key.CtrlPressed || key.MetaPressed)) Undo();
                switch (key.Keycode)
                {

                    case Key.Delete: case Key.Backspace: DeleteSelected(); break;
                    case Key.Pageup: ChangeDepth(.5f); break;
                    case Key.Pagedown: ChangeDepth(-.5f); break;
                }
            }
        }
    }
    private Vector3? WorkPoint(Vector2 screen, float z) =>
        new Plane(_buildView ? Vector3.Back : Vector3.Up, z).IntersectsRay(_camera.ProjectRayOrigin(screen), _camera.ProjectRayNormal(screen));
    private MachinePart? Pick(Vector2 screen)
    {
        MachinePart? closest = null;
        var best = 65f;
        foreach (var part in World.Parts)
        {
            if (!part.Visible) continue;
            if (_buildView && _linkSource == null && Mathf.Abs(part.Position.Z - _depth) > .26f) continue;
            if (_linkSource != null && World.ConnectionOptions(_linkSource, part).Count == 0) continue;
            var distance = _camera.UnprojectPosition(part.Position).DistanceTo(screen);
            if (distance >= best) continue;
            best = distance;
            closest = part;
        }
        return closest;
    }
    private void Click(Vector2 screen)
    {
        if (_linkSource == null && _tool.Length == 0 && _rotationGizmo.Begin(_camera, screen))
        {
            PushUndo();
            _gizmoUndoPending = true;
            _dragging = _lifting = _orbiting = false;
            _status.Text = _rotationGizmo.ResizeMode ? "Drag a square to stretch. Shift snaps; Escape cancels." : _rotationGizmo.MoveMode ? "Drag an arrow. Shift aligns to 0.1; Escape cancels." : "Drag to rotate. Shift snaps; Escape cancels.";
            return;
        }
        if (_linkSource != null)
        {
            var target = Pick(screen);
            if (target != null && target != _linkSource)
            {
                var options = World.ConnectionOptions(_linkSource, target);
                if (options.Count == 1) CompleteLink(target, options[0]);
                else ShowLinkChoices(target, options);
            }
            return;
        }
        if (_tool.Length > 0)
        {
            if (Remaining(_tool) <= 0 || WorkPoint(screen, _buildView ? _depth : _placementHeight) is not { } at || !PlacementInside(at)) return;
            PushUndo();
            var part = World.AddPart(new()
            {
                Id = _tool + "_" + _nextId++, Kind = _tool,
                Position = [at.X, at.Y, at.Z],
                Rotation = [_previewRotation.X, _previewRotation.Y, _previewRotation.Z]
            });
            part.Position = part.Position.Snapped(Vector3.One * .1f);
            SnapTube(part);
            Select(part);
            _tool = "";
            ClearPreview();
            RefreshPalette();
            RefreshLayers();
            _dragging = false;
            _status.Text = "Drag to slide. Choose arrows to move or rings to rotate.";
        }
        else
        {
            Select(Pick(screen));
            if (_selected is { Locked: false })
            {
                PushUndo();
                _dragging = true;
                _grabOffset = WorkPoint(screen, _buildView ? _selected.Position.Z : _selected.Position.Y) is { } grabbed ? _selected.Position - grabbed : Vector3.Zero;
                _depth = _selected.Position.Z;
                ChangeDepth(0);
            }
        }
    }
    private void Rotate(Vector3 angles)
    {
        if (_inRun) return;
        if (_tool.Length > 0)
        {
            if (_preview != null)
            {
                ApplyRotation(_preview, angles);
                _previewRotation = _preview.RotationDegrees;
            }
            return;
        }
        if (_selected is not { Locked: false }) { _status.Text = "Choose a part to place, or select a movable part first."; return; }
        PushUndo();
        ApplyRotation(_selected, angles);
        RefreshCables();
    }
    // Compose rotations around fixed workbench axes, not Euler components.
    // This keeps every axis usable even after a part has been tipped through 90°.
    private static void ApplyRotation(MachinePart part, Vector3 angles)
    {
        var rotation = new Quaternion(Vector3.Right, Mathf.DegToRad(angles.X)) *
                       new Quaternion(Vector3.Up, Mathf.DegToRad(angles.Y)) *
                       new Quaternion(Vector3.Back, Mathf.DegToRad(angles.Z));
        part.Quaternion = (rotation * part.Quaternion).Normalized();
    }

    private void ChangeDepth(float amount)
    {
        if (_inRun) return;
        _depth = Mathf.Clamp(_depth + amount, -4, 4);
        if (_selected is { Locked: false } && amount != 0)
        {
            PushUndo();
            _selected.Position = new(_selected.Position.X, _selected.Position.Y, _depth);
            RefreshCables();
        }
        SetLayer(_depth);
    }
    private void DeleteSelected()
    {
        if (_inRun || _selected is not { Locked: false }) return;
        PushUndo();
        var part = _selected;
        Select(null);
        World.RemovePart(part);
        RefreshPalette();
        RefreshCables();
        RefreshLayers();
    }
    private void PushUndo()
    {
        _undo.Add(World.Snapshot());
        if (_undo.Count > 40) _undo.RemoveAt(0);
    }
    private void Undo()
    {
        if (_inRun || _undo.Count == 0) return;
        Select(null);
        World.LoadMachine(_undo[^1]);
        _undo.RemoveAt(_undo.Count - 1);
        _linkSource = null;
        RefreshPalette();
        RefreshCables();
        RefreshLayers();
    }
    private void BeginLink()
    {
        if (_inRun) return;
        if (_selected is { HasOutputSocket: true })
        {
            ClearLinkChoices();
            _linkSource = _selected;
            _tool = "";
            ClearPreview();
            RefreshLayerAppearance();
            _status.Text = "Click a highlighted compatible part to connect. Cancel stops linking.";
        }
        else _status.Text = "Select a part with an output socket, then choose Connect.";
    }
    private void RefreshCables()
    {
        foreach (var child in _cables.GetChildren()) { _cables.RemoveChild(child); child.QueueFree(); }
        foreach (var rope in RopeNetwork.Build(World.Parts, World.Connections))
            _cables.AddChild(new RopeVisual { Path = rope });
        foreach (var link in World.Connections)
        {
            if (link.Type == ConnectionDomain.Rope) continue;
            var source = World.FindPart(link.From);
            var target = World.FindPart(link.To);
            if (source == null || target == null) continue;
            if (!ConnectionRules.TryResolve(link, source.ConnectionPorts, target.ConnectionPorts,
                out var output, out var input)) continue;
            var a = source.Transform * output.LocalPosition;
            var b = target.Transform * input.LocalPosition;
            if (link.Type == ConnectionDomain.Mechanical)
            {
                _cables.AddChild(new MechanicalBeltVisual
                {
                    World = World, Source = source, Target = target, Output = output, Input = input
                });
                continue;
            }
            var mid = (a + b) * .5f + new Vector3(0, -.5f, 0);
            var electrical = link.Type == ConnectionDomain.Electrical;
            var color = new Color(electrical ? "#293954" : "#e8b764");
            var width = electrical ? .045f : .025f;
            PartArt.Line(_cables, a, mid, color, width);
            PartArt.Line(_cables, mid, b, color, width);
        }
    }
    private void ToggleRun()
    {
        if (_inRun) { ResetRun(); return; }
        Select(null);
        _linkSource = null;
        _tool = "";
        ClearPreview();
        _dragging = false;
        _buildState = World.Snapshot();
        World.Start();
        TracePlaytestStart();
        _inRun = true;
        RefreshLayerAppearance();
        WorkshopIcons.Apply(_run, "■  Back to building");
        _state.Text = "MACHINE RUNNING";
        _precision.Editable = false;
        _friction.Disabled = true;
        _status.Text = "Watch the chain reaction. Reset restores the starting arrangement.";
        RefreshPalette();
    }
    private void ResetRun()
    {
        Select(null);
        if (_inRun)
        {
            World.LoadMachine(_buildState);
            TracePlaytestReset();
        }
        _inRun = false;
        RefreshLayerAppearance();
        SetBuildUi();
        RefreshPalette();
        RefreshCables();
    }
    private void SetBuildUi()
    {
        WorkshopIcons.Apply(_run, "▶  Run machine");
        _state.Text = "BUILD MODE";
        _precision.Editable = true;
        _friction.Disabled = false;
        _status.Text = "Choose a part. Arrows move; rings rotate.";
    }
    public override void _PhysicsProcess(double delta)
    {
        var wasRunning = World.Running;
        World.Step();
        if (wasRunning) TracePlaytestFrame();
        _time.Text = $"{World.Ticks * MachineWorld.Tick:00.00} s";
        if (World.Running && World.Ticks >= 120 * 30)
        {
            World.Running = false;
            TracePlaytestResult("timeout");
            _state.Text = "TIME TO TINKER";
            _status.Text = "30 seconds elapsed. Reset and try another arrangement.";
        }
    }
    private void OnSolved()
    {
        TracePlaytestResult("won");
        _state.Text = "BEAUTIFULLY DONE";
        _status.Text = "It works! Reset to experiment, or choose the next puzzle above.";
        WorkshopIcons.Apply(_run, "↶  Build again");
    }
    private void PrecisionChanged(double value)
    {
        if (_inRun) return;
        World.Precision = (float)value / 100;
        var name = value < 34 ? "Forgiving" : value < 75 ? "Balanced" : "Precise";
        _precisionText.Text = $"{name} · {value:0}%";
        foreach (var part in World.Parts) part.UpdateAssistance(World.Precision);
    }

    private void ShowHint()
    {
        _hint.Text = _sandbox ? "Try chaining a falling ball, a switch, and a lamp." : _puzzles[_currentLevel].Hint;
        _hint.Visible = !_hint.Visible;
        _objectivePanel.Size = new(266, 0);
    }
    private void Save()
    {
        if (_inRun) { _status.Text = "Return to building before saving."; return; }
        using var file = FileAccess.Open("user://workshop.json", FileAccess.ModeFlags.Write);
        if (file == null) { _status.Text = "Storage is unavailable on this device."; return; }
        var data = new SavedMachine { Version = SavedMachine.CurrentVersion, PuzzleId = _sandbox ? "" : _puzzles[_currentLevel].Id,
            Precision = World.Precision, Realistic = World.Realistic, NextId = _nextId, Machine = World.Snapshot() };
        file.StoreString(JsonSerializer.Serialize(data, MachineJson.Default.SavedMachine));
        _status.Text = "Saved on this device.";
    }
    private void LoadSave()
    {
        if (!FileAccess.FileExists("user://workshop.json")) { _status.Text = "No saved machine on this device yet."; return; }
        try
        {
            var data = JsonSerializer.Deserialize(FileAccess.GetFileAsString("user://workshop.json"), MachineJson.Default.SavedMachine);
            if (data == null || !Validate(data.Machine)) { _status.Text = "This save is not a supported machine."; return; }
            var level = CampaignProgress.ResolveLevel(data, _puzzles);
            if (level < 0) { _status.Text = "This save refers to an unavailable puzzle or save version."; return; }
            World.ValidateMachine(data.Machine);
            LoadLevel(level);
            _picker.Select(level);
            World.LoadMachine(data.Machine);
            _nextId = Math.Max(data.NextId, 1);
            _precision.Value = Math.Clamp(data.Precision * 100, 0, 100);
            _friction.ButtonPressed = data.Realistic;
            _buildState = World.Snapshot();
            RefreshPalette();
            RefreshCables();
            RefreshLayers();
            RefreshLayerAppearance();
            _status.Text = "Machine restored from this device.";
        }
        catch (JsonException) { _status.Text = "The save file could not be read."; }
        catch (ArgumentException) { _status.Text = "This machine contains unsupported parts, properties or connections."; }
    }
    private bool Validate(MachineData data)
    {
        if (data.Parts.Count > 250 || data.Connections.Count > 500) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in data.Parts)
        {
            if (!ids.Add(part.Id) || !World.Registry.Definitions.ContainsKey(part.Kind) ||
                part.Position.Length != 3 || part.Rotation.Length != 3 ||
                part.Position.Any(v => !float.IsFinite(v) || Mathf.Abs(v) > 100) ||
                part.Rotation.Any(v => !float.IsFinite(v))) return false;
        }
        return data.Connections.All(link => ids.Contains(link.From) && ids.Contains(link.To));
    }
}
