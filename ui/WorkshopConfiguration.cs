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
            DeclaredPhysics.HasRunControlledSource(instance.PartId))
        { AddRunControl(instance); return; }
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
                    var descriptor = part.Definition.Parameters.Single(parameter => new ParameterId(parameter.Key) == key);
                    if (float.IsFinite(value) && descriptor.EditorPolicy == CuriousContraptions.Abstractions.ParameterEditorPolicy.Clamp)
                        value = Math.Clamp(value, descriptor.Minimum, descriptor.Maximum);
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

    // The run-time control of a declared Source store's enable state (Story 9A.39). A click is a typed ConfigureElectrical
    // command for the next physics step; it changes the running world only, so the authored value, Undo, Reset and a save are
    // untouched. An accepted change shows as queued until a later committed tick of the same epoch reports it; a second click
    // while paused reverses it.
    private Button? _runControlToggle;
    private GpuBodyId _runControlObservedOwner;
    private ElectricalEnable _runControlObservedState;
    private readonly System.Collections.Generic.Dictionary<GpuBodyId, (ulong Epoch, ulong Tick, ElectricalEnable Enabled)> _runControlQueued = new();
    private bool _runControlSending;

    private ElectricalEnable RunControlIntent(GpuBodyId owner, ElectricalEnable committed)
    {
        var read = World.WorkshopRead;
        if (_runControlQueued.TryGetValue(owner, out var pending))
        {
            if (pending.Epoch != read.Epoch.Value ||
                (read.Tick.Value > pending.Tick && committed == pending.Enabled))
                _runControlQueued.Remove(owner);
            else return pending.Enabled;
        }
        return committed;
    }

    private string RunControlText(GpuBodyId owner, ElectricalEnable intent) =>
        (intent == ElectricalEnable.Enabled ? "Disable supply" : "Enable supply") +
        (_runControlQueued.ContainsKey(owner) ? " (queued)" : "");

    private void RefreshRunControlObservation()
    {
        if (_runControlToggle is null) return;
        for (var i = 0; i < World.WorkshopRead.Electrical.Count; i++)
        {
            var read = World.WorkshopRead.Electrical[i];
            if (read.Owner != _runControlObservedOwner) continue;
            _runControlObservedState = RunControlIntent(read.Owner, read.Enabled);
            _runControlToggle.Text = RunControlText(read.Owner, _runControlObservedState);
            return;
        }
    }

    private void AddRunControl(WorkshopGenericInstance instance)
    {
        var owner = instance.Id;
        var store = DeclaredPhysics.SourceStore(instance.PartId) ??
            throw new System.Diagnostics.UnreachableException("A run-time control requires a declared Source store.");
        var state = WorkshopPhysicsCompiler.ElectricalSourceFor(owner, store, instance.Parameters).Enabled;
        for (var i = 0; i < World.WorkshopRead.Electrical.Count; i++)
            if (World.WorkshopRead.Electrical[i].Owner == owner) state = World.WorkshopRead.Electrical[i].Enabled;
        _runControlObservedOwner = owner;
        _runControlObservedState = RunControlIntent(owner, state);
        var toggle = SignalButton(RunControlText(owner, _runControlObservedState),
            () => ApplyRunControl(owner, _runControlObservedState == ElectricalEnable.Enabled ? ElectricalEnable.Disabled : ElectricalEnable.Enabled));
        toggle.Disabled = instance.Locked || _gpuPending || _runControlSending;
        _connectionChoices.AddChild(toggle);
        _runControlToggle = toggle;
    }

    private async void ApplyRunControl(GpuBodyId owner, ElectricalEnable enabled)
    {
        try { await ApplyRunControlAsync(owner, enabled); }
        catch (Exception error) { HandlerFailed(nameof(ApplyRunControl), error); }
    }
    private async Task ApplyRunControlAsync(GpuBodyId owner, ElectricalEnable enabled)
    {
        if (_gpuPending || _runControlSending) return;
        _runControlSending = true;
        var epoch = World.WorkshopRead.Epoch.Value;
        var tick = World.WorkshopRead.Tick.Value;
        try
        {
            var result = await World.ConfigureElectrical(owner, enabled);
            var accepted = result.Result.Outcome == WorkshopCommandOutcome.Applied && World.WorkshopRead.Epoch.Value == epoch;
            if (accepted)
                _runControlQueued[owner] = (epoch, tick, enabled);
            if (!_workshopUiRemoved)
            {
                _status.Text = accepted
                    ? "Supply change accepted for the next physics step." : "Supply change was not applied.";
            }
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error); if (!_workshopUiRemoved) _status.Text = failure; }
        finally
        {
            _runControlSending = false;
            if (!_workshopUiRemoved) RefreshConnectionChoices();
        }
    }

    private static string ParameterUnits(CuriousContraptions.Abstractions.ParameterUnit unit) => unit switch
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
        _ => throw new System.Diagnostics.UnreachableException("The admitted schema contains an undefined parameter unit.")
    };

    private async void ApplyDeclaredParameters(GpuBodyId owner, InstanceParameterCollection parameters)
    {
        try { await ApplyDeclaredParametersAsync(owner, parameters); }
        catch (Exception error) { HandlerFailed(nameof(ApplyDeclaredParameters), error); }
    }

    private async Task ApplyDeclaredParametersAsync(GpuBodyId owner, InstanceParameterCollection parameters)
    {
        if (!CanEdit) return;
        try
        {
            if (World.Construction.Instances.Single(instance => instance.Id == owner) is not WorkshopGenericInstance current || current.Locked)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.NotConfigurable, "This part has no editable parameters.");
            try { parameters.Validate(current.PartId); }
            catch (ArgumentException error)
            {
                var failure = WorkshopUiFailure.Report(new WorkshopEditRejectedException(WorkshopEditRejection.OutOfRange, error.Message));
                if (!_workshopUiRemoved) _status.Text = failure;
                return;
            }
            if (current.Parameters.Equals(parameters)) { RefreshConnectionChoices(); return; }
            var proposed = World.Construction.WithInstance(current with { Parameters = parameters });
            proposed.Validate();
            PushUndo();
            if (!await SubmitConstruction(proposed)) { if (_undo.Count > 0) _undo.RemoveAt(_undo.Count - 1); return; }
            if (!_workshopUiRemoved) { _status.Text = "Parameters updated."; RefreshConnectionChoices(); }
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error); if (!_workshopUiRemoved) _status.Text = failure; }
    }
}
