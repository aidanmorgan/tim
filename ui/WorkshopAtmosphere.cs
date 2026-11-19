using System;
using System.Threading.Tasks;
using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

public partial class Workshop
{
    private OptionButton? _atmosphere;

    private void AddAtmosphereControl(VBoxContainer parent)
    {
        parent.AddChild(Text("Atmosphere", 14));
        var choices = new OptionButton { Name = "Atmosphere", CustomMinimumSize = new(180, 36) };
        choices.AddItem("Standard", (int)AtmospherePreset.Standard);
        choices.AddItem("Vacuum", (int)AtmospherePreset.Vacuum);
        choices.ItemSelected += index => ApplyAtmosphere((AtmospherePreset)choices.GetItemId((int)index));
        parent.AddChild(choices);
        _atmosphere = choices;
        choices.Select((int)AtmospherePreset.Standard);
        choices.Disabled = true;
    }

    private void RefreshAtmosphere()
    {
        if (_atmosphere is not { } choices) return;
        choices.Disabled = !CanEdit;
        choices.Select(choices.GetItemIndex((int)World.Construction.Atmosphere));
    }

    private async void ApplyAtmosphere(AtmospherePreset selected)
    {
        try { await ApplyAtmosphereAsync(selected); }
        catch (Exception error) { HandlerFailed(nameof(ApplyAtmosphere), error); }
    }

    internal async Task ApplyAtmosphereAsync(AtmospherePreset selected)
    {
        if (!CanEdit) { RefreshAtmosphere(); return; }
        try
        {
            _ = AtmosphereBoundary.For(selected);
            if (selected == World.Construction.Atmosphere) return;
            var proposed = World.Construction with { Atmosphere = selected };
            proposed.Validate(_core.Supply, _bundle);
            PushUndo();
            if (!await SubmitConstruction(proposed))
            {
                if (_undo.Count > 0) _undo.RemoveAt(_undo.Count - 1);
                return;
            }
            if (!_workshopUiRemoved) _status.Text = "Atmosphere updated.";
        }
        catch (Exception error)
        {
            var failure = WorkshopUiFailure.Report(error, Diagnostics);
            if (!_workshopUiRemoved) _status.Text = failure;
        }
        finally { if (!_workshopUiRemoved) RefreshAtmosphere(); }
    }
}
