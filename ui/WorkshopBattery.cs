using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

public partial class Workshop
{
    private Label? _batteryCharge;
    private Button? _batteryToggle;
    private GpuBodyId _batteryObservedOwner;
    private ElectricalEnable _batteryObservedState;
    private readonly Dictionary<GpuBodyId, (ulong Epoch, ulong Tick, ElectricalEnable Enabled)> _batteryQueued = new();
    private bool _batteryCommandSending;

    private ElectricalEnable BatteryIntent(GpuBodyId owner, ElectricalEnable committed)
    {
        var read = World.WorkshopRead;
        if (_batteryQueued.TryGetValue(owner, out var pending))
        {
            if (pending.Epoch != read.Epoch.Value ||
                (read.Tick.Value > pending.Tick && committed == pending.Enabled))
                _batteryQueued.Remove(owner);
            else return pending.Enabled;
        }
        return committed;
    }

    private string BatteryToggleText(GpuBodyId owner, ElectricalEnable intent) =>
        (intent == ElectricalEnable.Enabled ? "Disable supply" : "Enable supply") +
        (_batteryQueued.ContainsKey(owner) ? " (queued)" : "");

    private void RefreshBatteryObservation()
    {
        if (_batteryCharge is null || _batteryToggle is null) return;
        for (var i = 0; i < World.WorkshopRead.Electrical.Count; i++)
        {
            var read = World.WorkshopRead.Electrical[i];
            if (read.Owner != _batteryObservedOwner) continue;
            _batteryObservedState = BatteryIntent(read.Owner, read.Enabled);
            _batteryToggle.Text = BatteryToggleText(read.Owner, _batteryObservedState);
            _batteryCharge.Text = "Charge: " + read.Remaining.Value.ToString("0.##") + " J";
            return;
        }
    }

    private void AddBatteryConfiguration(BatteryPart battery)
    {
        var owner = battery.AuthoredId;
        if (World.WorkshopPhase is WorkshopSimulationPhase.Running or WorkshopSimulationPhase.Paused)
        {
            var state = battery.CanonicalSource.Enabled;
            var remaining = battery.CanonicalSource.Capacity.Value * battery.CanonicalSource.InitialFraction;
            for (var i = 0; i < World.WorkshopRead.Electrical.Count; i++)
                if (World.WorkshopRead.Electrical[i].Owner == owner)
                {
                    state = World.WorkshopRead.Electrical[i].Enabled;
                    remaining = World.WorkshopRead.Electrical[i].Remaining.Value;
                }
            _batteryObservedOwner = owner;
            _batteryObservedState = BatteryIntent(owner, state);
            var toggle = SignalButton(BatteryToggleText(owner, _batteryObservedState),
                () => ApplyBatteryEnable(owner, _batteryObservedState == ElectricalEnable.Enabled ? ElectricalEnable.Disabled : ElectricalEnable.Enabled));
            toggle.Disabled = battery.Locked || _gpuPending || _batteryCommandSending;
            _connectionChoices.AddChild(toggle);
            _batteryToggle = toggle;
            _batteryCharge = Text("Charge: " + remaining.ToString("0.##") + " J", 12);
            _connectionChoices.AddChild(_batteryCharge);
            return;
        }
        var configure = SignalButton("Battery settings", () =>
        {
            if (!CanEdit) return;
            ClearConnectionChoices();
            var settings = battery.CanonicalSource;
            var capacity = new SpinBox { MinValue = 60, MaxValue = 14400, Step = 0, Value = settings.Capacity.Value,
                Suffix = "J", Name = "BatteryCapacity", CustomMinimumSize = new(140, 36) };
            var power = new SpinBox { MinValue = 10, MaxValue = 480, Step = 0, Value = settings.MaximumPower.Value,
                Suffix = "W", Name = "BatteryPower", CustomMinimumSize = new(140, 36) };
            var initial = new SpinBox { MinValue = 0, MaxValue = 1, Step = 0, Value = settings.InitialFraction,
                Name = "BatteryInitialCharge", CustomMinimumSize = new(140, 36) };
            var enabled = new OptionButton { Name = "BatteryEnabled" };
            enabled.AddItem("Disabled", (int)ElectricalEnable.Disabled);
            enabled.AddItem("Enabled", (int)ElectricalEnable.Enabled);
            enabled.Select((int)settings.Enabled);
            _connectionChoices.AddChild(Text("Capacity / output / initial charge", 12));
            _connectionChoices.AddChild(capacity); _connectionChoices.AddChild(power);
            _connectionChoices.AddChild(initial); _connectionChoices.AddChild(enabled);
            _connectionChoices.AddChild(SignalButton("Apply battery settings", () =>
            {
                capacity.Apply(); power.Apply(); initial.Apply();
                ApplyBatterySettings(owner, new(new((float)capacity.Value), new((float)power.Value),
                    (float)initial.Value, (ElectricalEnable)enabled.GetSelectedId()));
            }));
            _connectionChoices.AddChild(SignalButton("Cancel", RefreshConnectionChoices));
        });
        configure.Disabled = !CanEdit || battery.Locked;
        _connectionChoices.AddChild(configure);
    }

    private async void ApplyBatterySettings(GpuBodyId owner, ElectricalSourceSettings settings)
    {
        if (!CanEdit) return;
        try
        {
            settings.Validate();
            if (World.Construction.Instances.Single(instance => instance.Id == owner) is not WorkshopBattery current || current.Locked)
                throw new ArgumentException("This source cannot be configured.");
            PushUndo();
            if (!await SubmitConstruction(World.Construction.WithInstance(current with { Settings = settings })))
            { if (_undo.Count > 0) _undo.RemoveAt(_undo.Count - 1); return; }
            if (!_workshopUiRemoved) { _status.Text = "Battery settings updated."; RefreshConnectionChoices(); }
        }
        catch (Exception error) { if (!_workshopUiRemoved) _status.Text = error.Message; }
    }

    private async void ApplyBatteryEnable(GpuBodyId owner, ElectricalEnable enabled)
    {
        if (_gpuPending || _batteryCommandSending) return;
        _batteryCommandSending = true;
        var epoch = World.WorkshopRead.Epoch.Value;
        var tick = World.WorkshopRead.Tick.Value;
        try
        {
            var result = await World.ConfigureElectrical(owner, enabled);
            var accepted = result.Result.Outcome == WorkshopCommandOutcome.Applied && World.WorkshopRead.Epoch.Value == epoch;
            if (accepted)
                _batteryQueued[owner] = (epoch, tick, enabled);
            if (!_workshopUiRemoved)
            {
                _status.Text = accepted
                    ? "Supply change accepted for the next physics step." : "Supply change was not applied.";
            }
        }
        catch (Exception error) { if (!_workshopUiRemoved) _status.Text = error.Message; }
        finally
        {
            _batteryCommandSending = false;
            if (!_workshopUiRemoved) RefreshConnectionChoices();
        }
    }
}
