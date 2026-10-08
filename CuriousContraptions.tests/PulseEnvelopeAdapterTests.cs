using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class PulseEnvelopeAdapterTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(AnimationImpulseCurve.SineSquaredPulse,.5)]
    [InlineData(AnimationImpulseCurve.SmoothRiseFall,.08/.53)]
    public void SharedColourBindingRendersEnvelopeAndRetainedPeakThenRestoresBaseline(AnimationImpulseCurve curve,double peak)
    {
        Assert.NotNull(godot.Tree.Root);
        var baseline=new Color("#fff0c2");var active=new Color("#f7cb52");
        using var material=new StandardMaterial3D{AlbedoColor=baseline};
        var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(material);
        var definition=new AnimationImpulseDefinition(1,curve,AnimationImpulseOverlap.Maximum,
            AnimationImpulseVisibility.DeferUntilVisible,AnimationClock.Presentation,4,AnimationImpulseTiming.EventTimeRetainFirstFrame,peak);
        var animation=adapter.BindImpulseColour(target,definition,baseline,active);
        adapter.EnqueueImpulse(animation,new(1),1,0);
        adapter.Present(1,0,0);Assert.Equal(baseline,material.AlbedoColor);
        adapter.Present(2,peak,0);Assert.Equal(active,material.AlbedoColor);
        adapter.Present(3,1,0);Assert.Equal(baseline,material.AlbedoColor);
        adapter.EnqueueImpulse(animation,new(2),1,1);adapter.SetVisible(target,false);
        adapter.Present(4,3,0);Assert.Equal(1,adapter.ReadImpulses(animation).Pending);
        adapter.SetVisible(target,true);adapter.Present(5,3,0);Assert.Equal(active,material.AlbedoColor);
        adapter.Reset();Assert.Equal(baseline,material.AlbedoColor);
        Assert.Throws<ArgumentException>(()=>adapter.EnqueueImpulse(animation,new(3),1,3));
    }
}
