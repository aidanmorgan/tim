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
        { AddDeclaredConfiguration(part, generic); return; }
        if (part.Definition?.PartId == PartId.Battery) { AddBatteryConfiguration(part); return; }
        if (part.Definition?.PartId == PartId.Clock) { AddClockConfiguration(part); return; }

    }
    private void AddDeclaredConfiguration(MachinePart part, WorkshopGenericInstance instance)
    {
        if (part.Definition.Parameters.Count == 0) return;
        var configure = SignalButton("Configure", () =>
        {
            if (!CanEdit || instance.Locked) return;
            ClearConnectionChoices();
            var fields = new System.Collections.Generic.Dictionary<ParameterId, LineEdit>();
            foreach (var descriptor in part.Definition.Parameters)
            {
                var key = new ParameterId(descriptor.Key);
                var row = new HBoxContainer();
                var field = new LineEdit { Text = instance.Parameters[key].ToString("R", CultureInfo.InvariantCulture),
                    Name = descriptor.Key, CustomMinimumSize = new(140, 36) };
                fields.Add(key, field);
                var units = ParameterUnits(descriptor.Unit);
                row.AddChild(Text(descriptor.Key + " (" + descriptor.Minimum.ToString("R", CultureInfo.InvariantCulture) +
                    "–" + descriptor.Maximum.ToString("R", CultureInfo.InvariantCulture) + " " + units + ")", 14));
                row.AddChild(field);
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
                ApplyDeclaredParameters(instance.Id, new(values));
            }));
            _connectionChoices.AddChild(SignalButton("Cancel", RefreshConnectionChoices));
        });
        configure.Disabled = !CanEdit || instance.Locked;
        _connectionChoices.AddChild(configure);
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

    private void AddClockConfiguration(MachinePart part)
    {
        var owner = part.AuthoredId;
        var configure = SignalButton("Interval: " + part.CanonicalIntervalSeconds.ToString("0.###", CultureInfo.CurrentCulture) + " s", () =>
        {
            if (!CanEdit) return;
            ClearConnectionChoices();
            var interval = new SpinBox { MinValue = (double)WorkshopClock.MinIntervalSeconds, MaxValue = (double)WorkshopClock.MaxIntervalSeconds, Step = 0,
                Value = part.CanonicalIntervalSeconds, Suffix = "s", Name = "ClockInterval", CustomMinimumSize = new(140, 36) };
            _connectionChoices.AddChild(interval);
            _connectionChoices.AddChild(SignalButton("Apply interval", () => { interval.Apply(); ApplyClockInterval(owner, (float)interval.Value); }));
            _connectionChoices.AddChild(SignalButton("Cancel", RefreshConnectionChoices));
        });
        configure.Disabled = !CanEdit || part.Locked; _connectionChoices.AddChild(configure);
    }
    private async void ApplyClockInterval(GpuBodyId owner, float seconds)
    {
        try { await ApplyClockIntervalAsync(owner, seconds); }
        catch (Exception error) { HandlerFailed(nameof(ApplyClockInterval), error); }
    }
    private async Task ApplyClockIntervalAsync(GpuBodyId owner, float seconds)
    {
        if (!CanEdit) return;
        try
        {
            if (World.Construction.Instances.Single(instance => instance.Id == owner) is not WorkshopClock current || current.Locked)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.NotConfigurable, "This part has no editable clock interval.");
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < WorkshopClock.MinIntervalSeconds || seconds > WorkshopClock.MaxIntervalSeconds)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.OutOfRange, "Clock interval is outside its authored range.");
            if (seconds == current.IntervalSeconds) { RefreshConnectionChoices(); return; }
            var proposed = World.Construction.WithInstance(current with { IntervalSeconds = seconds });
            PushUndo();
            if (!await SubmitConstruction(proposed)) { if (_undo.Count > 0) _undo.RemoveAt(_undo.Count - 1); return; }
            if (!_workshopUiRemoved) { _status.Text = "Clock interval updated."; RefreshConnectionChoices(); }
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error); if (!_workshopUiRemoved) _status.Text = failure; }
    }
}
