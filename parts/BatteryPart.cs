using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Battery declaration and artwork. The shared electrical worker owns supply and charge.</summary>
public partial class BatteryPart : MachinePart
{
    public ElectricalSourceSettings CanonicalSource { get; private set; } = ElectricalSourceSettings.Default;
    internal void ApplySource(ElectricalSourceSettings source) { source.Validate(); CanonicalSource = source; }
    protected override void Build()
    {
        ApplySource(Definition.ElectricalSource!.Capture());
        PickRadius = .8f;
        PartArt.Box(Visual, new(.85f, 1.05f, .75f), Definition.Color, Vector3.Zero);
        PartArt.Box(Visual, new(.87f, .25f, .77f), new("#fff8e9"), new(0, .3f, 0));
        var terminal = PartArt.Cylinder(Visual, .16f, .14f, new("#556573"), new(0, .59f, 0));
        BindElectricalIndicator(terminal, ElectricalIndicator.Supply);
        for (var i = 0; i < 4; i++)
        {
            var mark = PartArt.Box(Visual, new(.12f, .12f, .02f), new("#556573"), new(-.27f + i * .18f, .3f, .4f));
            BindElectricalIndicator(mark, (ElectricalIndicator)(i + 1));
        }
        PartArt.Box(Visual, new(.32f, .07f, .015f), new("#293954"), new(0, .02f, .385f));
        PartArt.Box(Visual, new(.07f, .32f, .015f), new("#293954"), new(0, .02f, .395f));
    }
}
