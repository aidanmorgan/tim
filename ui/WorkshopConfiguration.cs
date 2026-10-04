using System;
using System.Linq;
using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

public partial class Workshop
{
    private void AddConfigurationChoices(MachinePart part)
    {
        if (part is not DelayPart delay) return;
        var owner=part.AuthoredId;
        var configure=SignalButton("Delay: "+((double)delay.CanonicalDuration.Seconds.Value).ToString("0.###")+" s",()=>
        {
            if (!CanEdit) return;
            ClearConnectionChoices();
            var duration=new SpinBox {MinValue=(double)(Half).1,MaxValue=12,Step=0,Value=(double)delay.CanonicalDuration.Seconds.Value,
                Suffix="s",Name="DelaySeconds",CustomMinimumSize=new(140,36)};
            _connectionChoices.AddChild(duration);
            _connectionChoices.AddChild(SignalButton("Apply duration",()=> {duration.Apply();ApplyDelayDuration(owner,duration.Value);}));
            _connectionChoices.AddChild(SignalButton("Cancel",RefreshConnectionChoices));
        });
        configure.Disabled=!CanEdit||part.Locked; _connectionChoices.AddChild(configure);
    }
    private async void ApplyDelayDuration(GpuBodyId owner,double seconds)
    {
        if (!CanEdit) return;
        try
        {
            if (World.Construction.Instances.Single(instance=>instance.Id==owner) is not WorkshopDelay current || current.Locked)
                throw new ArgumentException("This part has no editable delay.");
            var duration=DelayDuration.FromInput(seconds);
            if (duration==current.Duration) {RefreshConnectionChoices();return;}
            var proposed=World.Construction.WithInstance(current with {Duration=duration});
            PushUndo();
            if (!await SubmitConstruction(proposed)) {if(_undo.Count>0)_undo.RemoveAt(_undo.Count-1);return;}
            if (!_workshopUiRemoved) {_status.Text="Delay duration updated.";RefreshConnectionChoices();}
        }
        catch(Exception error) {if(!_workshopUiRemoved)_status.Text=error.Message;}
    }
}
