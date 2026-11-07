using CuriousContraptions.Abstractions;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

public partial class Workshop
{
    private void AddConfigurationChoices(MachinePart part)
    {
        if (World.Construction.Instances.FirstOrDefault(instance => instance.Id == part.AuthoredId) is WorkshopGenericInstance generic)
            AddDeclaredConfiguration(part, generic);
    }
    private void AddDeclaredConfiguration(MachinePart part, WorkshopGenericInstance instance)
    {
        if (part.Definition.Parameters.Count == 0) return;
        // While the machine runs, a part offers only what its package declares run-editable.
        if (World.WorkshopPhase is WorkshopSimulationPhase.Running or WorkshopSimulationPhase.Paused &&
            DeclaredPhysics.RunControls(_bundle, instance.PartId) is { Count: > 0 } runControls)
        { AddRunControls(part, instance, runControls); return; }
        var configure = SignalButton("Configure", () =>
        {
            if (!CanEdit || instance.Locked) return;
            ClearConnectionChoices();
            var fields = new System.Collections.Generic.Dictionary<ParameterId, LineEdit>();
            var choices = new System.Collections.Generic.Dictionary<ParameterId, OptionButton>();
            foreach (var descriptor in part.Definition.Parameters)
            {
                var key = new ParameterId(descriptor.Key);
                var row = new HBoxContainer();
                if (descriptor.Kind == CuriousContraptions.Abstractions.ParameterKind.Choice)
                {
                    // A Choice is picked from its declared options, by index.
                    var options = new OptionButton { Name = descriptor.Key, CustomMinimumSize = new(140, 36) };
                    for (var i = 0; i < descriptor.Options.Count; i++) options.AddItem(descriptor.Options[i], i);
                    options.Select((int)instance.Parameters[key]);
                    choices.Add(key, options);
                    row.AddChild(Text(descriptor.Key, 14));
                    row.AddChild(options);
                }
                else
                {
                    var field = new LineEdit { Text = instance.Parameters[key].ToString("R", CultureInfo.InvariantCulture),
                        Name = descriptor.Key, CustomMinimumSize = new(140, 36) };
                    fields.Add(key, field);
                    var units = ParameterUnits(descriptor.Unit);
                    row.AddChild(Text(descriptor.Key + " (" + descriptor.Minimum.ToString("R", CultureInfo.InvariantCulture) +
                        "–" + descriptor.Maximum.ToString("R", CultureInfo.InvariantCulture) + " " + units + ")", 14));
                    row.AddChild(field);
                }
                _connectionChoices.AddChild(row);
            }
            _connectionChoices.AddChild(SignalButton("Apply parameters", () =>
            {
                var values = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ParameterId, float>>();
                foreach (var (key, field) in fields)
                {
                    if (!float.TryParse(field.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    { _status.Text = "Enter valid parameter values."; return; }
                    values.Add(new(key, value));
                }
                foreach (var (key, options) in choices) values.Add(new(key, options.GetSelectedId()));
                ApplyDeclaredParameters(instance.Id, new(values));
            }));
            _connectionChoices.AddChild(SignalButton("Cancel", RefreshConnectionChoices));
        });
        configure.Disabled = !CanEdit || instance.Locked;
        _connectionChoices.AddChild(configure);
    }

    // The run-time controls a package declares (Story 9A.39; engine-run-control-table; engine-revolute-drive-target). Each is found
    // through the declaration and the installed run-control table, by owner and lane, and renders its typed value: an enable is a supply
    // toggle, a scalar a label (parameter, units, committed value), a field named after the parameter and a Set button. Each send is one
    // typed ConfigureRunControl command for the next physics step; it changes the running world only, so the authored value, Undo,
    // Reset and a save are untouched. An accepted change shows as queued until a later committed tick of the same epoch reports it; a
    // second change while paused replaces it. A locked part compiles no row: its control shows its committed value, disabled, with no
    // field and no action. The per-frame refresh updates the shown text only, never a field the player is typing in.
    private readonly System.Collections.Generic.List<(RunControlRow? Row, GpuBodyId Owner, RunControlLane Lane, ElectricalSourceId Source, RevoluteConstraintId Joint, ParameterDescriptor Parameter, Action<string> Show)> _runControlViews = new(4);
    private readonly System.Collections.Generic.Dictionary<RunControlId, (ulong Epoch, ulong Tick, RunControlValue Value)> _runControlQueued = new(16);
    private bool _runControlSending;

    /// <summary>What a control shows: the owner's committed lane value, unless an accepted change to its row is still queued (until a
    /// later committed tick of the same epoch reports it).</summary>
    private RunControlValue RunControlIntent(RunControlRow? row, GpuBodyId owner, RunControlLane lane, ElectricalSourceId source, RevoluteConstraintId joint)
    {
        var read = World.WorkshopRead;
        var committed = row is not null ? _core.RunControls.Read(read, row.Value) : _core.RunControls.Read(read, owner, lane, source, joint);
        if (row is { } r && _runControlQueued.TryGetValue(r.Id, out var pending))
        {
            if (pending.Epoch != read.Epoch.Value ||
                (read.Tick.Value > pending.Tick && committed == pending.Value))
                _runControlQueued.Remove(r.Id);
            else return pending.Value;
        }
        return committed;
    }

    /// <summary>What a control shows: an enable toggle names the change one click makes; a scalar shows its parameter, the committed
    /// value and the parameter's units. Either adds "(queued)" while an accepted change is pending.</summary>
    private string RunControlText(RunControlRow? row, GpuBodyId owner, RunControlLane lane, ElectricalSourceId source, RevoluteConstraintId joint, ParameterDescriptor parameter)
    {
        var intent = RunControlIntent(row, owner, lane, source, joint);
        var queued = row is { } r && _runControlQueued.ContainsKey(r.Id);
        var units = ParameterUnits(parameter.Unit);
        return intent.Kind switch
        {
            RunControlValueKind.Enable => intent.Enable == ElectricalEnable.Enabled ? "Disable supply" : "Enable supply",
            RunControlValueKind.Scalar => parameter.Key + ": " +
                (row is not null ? _core.RunControls.Read(World.WorkshopRead, row.Value) : _core.RunControls.Read(World.WorkshopRead, owner, lane, source, joint)).Scalar.ToString("R", CultureInfo.InvariantCulture) + (units.Length == 0 ? "" : " " + units),
            _ => throw new System.Diagnostics.UnreachableException("A committed run-control value has an undefined kind.")
        } + (queued ? " (queued)" : "");
    }

    /// <summary>The value one click sends: an enable control reverses the value it shows.</summary>
    private static RunControlValue RunControlNext(RunControlValue intent) => intent.Kind switch
    {
        RunControlValueKind.Enable => RunControlValue.Of(intent.Enable == ElectricalEnable.Enabled ? ElectricalEnable.Disabled : ElectricalEnable.Enabled),
        _ => throw new System.Diagnostics.UnreachableException("A scalar run control is sent from its field, never toggled.")
    };

    private void RefreshRunControlObservation()
    {
        foreach (var (row, owner, lane, source, joint, parameter, show) in _runControlViews) show(RunControlText(row, owner, lane, source, joint, parameter));
    }

    private void AddRunControls(MachinePart part, WorkshopGenericInstance instance, System.Collections.Generic.IReadOnlyList<DeclaredRunControl> controls)
    {
        var owner = instance.Id;
        var physics = WorkshopPhysicsCompiler.Compile(_core, _bundle, World.Construction, new(1, 1));
        for (var i = 0; i < controls.Count; i++)
        {
            var declared = controls[i];
            var lane = declared.Lane;
            var source = declared.SourceNode is { } selected
                ? World.Construction.Instances.Identities[owner].Supply[selected].Source : default;
            var joint = declared.Coordinate is { Kind: CoordinateKind.Revolute } coord &&
                coord.Index >= 0 && coord.Index < World.Construction.Instances.Identities[owner].Hinges.Length
                ? World.Construction.Instances.Identities[owner].Hinges[coord.Index].Joint : default;
            var parameter = part.Definition.Parameters.Single(descriptor => descriptor.Key == declared.Parameter.Value);
            var laneIndex = _core.RunControls.Slot(physics, lane, owner, source, joint);
            var row = _core.RunControls.Find(World.RunControls, owner, lane, source, laneIndex);
            var disabled = row is null || _gpuPending || _runControlSending;
            switch (_core.RunControls.ValueKind(lane))
            {
                case RunControlValueKind.Enable:
                {
                    var view = SignalButton(RunControlText(row, owner, lane, source, joint, parameter), row is { } compiled
                        ? () => ApplyRunControl(new(owner, compiled.Id, RunControlNext(RunControlIntent(row, owner, lane, source, joint))))
                        : null);
                    view.Disabled = disabled;
                    _connectionChoices.AddChild(view);
                    _runControlViews.Add((row, owner, lane, source, joint, parameter, text => view.Text = text));
                    break;
                }
                case RunControlValueKind.Scalar:
                {
                    var line = new HBoxContainer();
                    var value = Text(RunControlText(row, owner, lane, source, joint, parameter), 14);
                    line.AddChild(value);
                    LineEdit? field = null;
                    if (row is not null)
                    {
                        // The field starts at the value the control intends: a queued change while it is pending, else the committed value.
                        field = new LineEdit { Text = RunControlIntent(row, owner, lane, source, joint).Scalar.ToString("R", CultureInfo.InvariantCulture),
                            Name = parameter.Key, CustomMinimumSize = new(140, 36) };
                        line.AddChild(field);
                    }
                    var set = SignalButton("Set " + parameter.Key, row is { } compiled && field is { } typed
                        ? () => SubmitScalarRunControl(owner, compiled, parameter, typed.Text)
                        : null);
                    set.Disabled = disabled;
                    line.AddChild(set);
                    _connectionChoices.AddChild(line);
                    _runControlViews.Add((row, owner, lane, source, joint, parameter, text => value.Text = text));
                    break;
                }
                default: throw new System.Diagnostics.UnreachableException("A declared run control names an undefined lane kind.");
            }
        }
    }

    /// <summary>A scalar control's Set: text, NaN and infinity are refused as the build editor refuses them; the value then takes the
    /// parameter's editor policy (a Clamp parameter clamps into its range) and a value still outside the row's bounds is the typed
    /// OutOfRange edit rejection. Nothing is sent on a refusal; the worker itself refuses, never clamps, an out-of-range value.</summary>
    private void SubmitScalarRunControl(GpuBodyId owner, RunControlRow row, ParameterDescriptor parameter, string text)
    {
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !float.IsFinite(value))
        { _status.Text = "Enter valid parameter values."; return; }
        value = EditorValue(parameter, value);
        if (!_core.RunControls.WithinBounds(row, RunControlValue.Of(value)))
        {
            var units = ParameterUnits(parameter.Unit);
            var refusal = new WorkshopEditRejectedException(WorkshopEditRejection.OutOfRange, parameter.Key + " must be between " +
                row.Minimum.ToString("R", CultureInfo.InvariantCulture) + " and " + row.Maximum.ToString("R", CultureInfo.InvariantCulture) +
                (units.Length == 0 ? "" : " " + units) + ".");
            _status.Text = WorkshopUiFailure.Report(refusal, Diagnostics);
            return;
        }
        ApplyRunControl(new(owner, row.Id, RunControlValue.Of(value)));
    }

    /// <summary>The value a typed editor entry submits: a finite value of a Clamp parameter is clamped into its declared range; any other
    /// value is unchanged. The build editor and a scalar run control share it.</summary>
    private static float EditorValue(ParameterDescriptor descriptor, float value) =>
        float.IsFinite(value) && descriptor.EditorPolicy == ParameterEditorPolicy.Clamp ? Math.Clamp(value, descriptor.Minimum, descriptor.Maximum) : value;

    /// <summary>The subject a run control's status line names: "Supply" for an enable toggle, the parameter's key for a scalar, found
    /// through the control's compiled row.</summary>
    private string RunControlSubject(WorkshopRunControl control)
    {
        if (control.Value.Kind == RunControlValueKind.Enable) return "Supply";
        foreach (var (row, _, _, _, _, parameter, _) in _runControlViews)
            if (row is { } r && r.Id == control.Control) return parameter.Key;
        throw new System.Diagnostics.UnreachableException("A scalar run control is sent only from its own view.");
    }

    private async void ApplyRunControl(WorkshopRunControl control)
    {
        try { await ApplyRunControlAsync(control); }
        catch (Exception error) { HandlerFailed(nameof(ApplyRunControl), error); }
    }
    private async Task ApplyRunControlAsync(WorkshopRunControl control)
    {
        if (_gpuPending || _runControlSending) return;
        var subject = RunControlSubject(control);
        _runControlSending = true;
        var epoch = World.WorkshopRead.Epoch.Value;
        var tick = World.WorkshopRead.Tick.Value;
        try
        {
            var result = await World.ConfigureRunControl(control);
            var accepted = result.Result.Outcome == WorkshopCommandOutcome.Applied && World.WorkshopRead.Epoch.Value == epoch;
            if (accepted)
                _runControlQueued[control.Control] = (epoch, tick, control.Value);
            if (!_workshopUiRemoved)
            {
                _status.Text = accepted
                    ? subject + " change accepted for the next physics step." : subject + " change was not applied.";
            }
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error, Diagnostics); if (!_workshopUiRemoved) _status.Text = failure; }
        finally
        {
            _runControlSending = false;
            if (!_workshopUiRemoved) RefreshConnectionChoices();
        }
    }

    internal static string ParameterUnits(CuriousContraptions.Abstractions.ParameterUnit unit) => unit switch
    {
        CuriousContraptions.Abstractions.ParameterUnit.None => "",
        CuriousContraptions.Abstractions.ParameterUnit.Metre => "m",
        CuriousContraptions.Abstractions.ParameterUnit.Kilogram => "kg",
        CuriousContraptions.Abstractions.ParameterUnit.Second => "s",
        CuriousContraptions.Abstractions.ParameterUnit.MetrePerSecond => "m/s",
        CuriousContraptions.Abstractions.ParameterUnit.MetrePerSecondSquared => "m/s²",
        CuriousContraptions.Abstractions.ParameterUnit.NewtonPerMetre => "N/m",
        CuriousContraptions.Abstractions.ParameterUnit.NewtonSecondPerMetre => "N·s/m",
        CuriousContraptions.Abstractions.ParameterUnit.Joule => "J",
        CuriousContraptions.Abstractions.ParameterUnit.Watt => "W",
        CuriousContraptions.Abstractions.ParameterUnit.Degree => "°",
        CuriousContraptions.Abstractions.ParameterUnit.RadianPerSecond => "rad/s",
        CuriousContraptions.Abstractions.ParameterUnit.NewtonMetre => "N·m",
        CuriousContraptions.Abstractions.ParameterUnit.Newton => "N",
        CuriousContraptions.Abstractions.ParameterUnit.MetrePerRadian => "m/rad",
        _ => throw new System.Diagnostics.UnreachableException("The admitted schema contains an undefined parameter unit.")
    };

    private async void ApplyDeclaredParameters(GpuBodyId owner, InstanceParameterCollection parameters)
    {
        try { await ApplyDeclaredParametersAsync(owner, parameters); }
        catch (Exception error) { HandlerFailed(nameof(ApplyDeclaredParameters), error); }
    }

    internal async Task ApplyDeclaredParametersAsync(GpuBodyId owner, InstanceParameterCollection parameters)
    {
        if (!CanEdit) return;
        try
        {
            if (World.Construction.Instances.Single(instance => instance.Id == owner) is not WorkshopGenericInstance current || current.Locked)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.NotConfigurable, "This part has no editable parameters.");
            var values = parameters.ToDictionary();
            foreach (var descriptor in World.Registry.Definitions[current.PartId].Parameters)
            {
                var key = new ParameterId(descriptor.Key);
                if (values.TryGetValue(key, out var value))
                    values[key] = EditorValue(descriptor, value);
            }
            parameters = new(values);
            try { parameters.Validate(current.PartId); }
            catch (ArgumentException error)
            {
                var failure = WorkshopUiFailure.Report(new WorkshopEditRejectedException(WorkshopEditRejection.OutOfRange, error.Message), Diagnostics);
                if (!_workshopUiRemoved) _status.Text = failure;
                return;
            }
            if (current.Parameters.Equals(parameters)) { RefreshConnectionChoices(); return; }
            var proposed = World.Construction.WithInstance(_bundle, current with { Parameters = parameters });
            proposed.Validate(_core.Supply, _bundle);
            PushUndo();
            if (!await SubmitConstruction(proposed)) { if (_undo.Count > 0) _undo.RemoveAt(_undo.Count - 1); return; }
            if (!_workshopUiRemoved) { _status.Text = "Parameters updated."; RefreshConnectionChoices(); }
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error, Diagnostics); if (!_workshopUiRemoved) _status.Text = failure; }
    }
}
