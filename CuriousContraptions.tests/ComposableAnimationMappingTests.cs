using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using CuriousContraptions.Simulation;
using Godot;
using Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class ComposableAnimationMappingTests(NativeSceneFixture godot)
{
    [Fact]
    public void DriverRules_Supports_PhysicalContinuousDriversSupportLinearAndExponentialFollowOnly()
    {
        PresentationDriver[] continuous =
        [
            PresentationDriver.CommittedWheelAngle,
            PresentationDriver.CommittedSurfaceDeflection,
            PresentationDriver.CommittedTimerProgress,
            PresentationDriver.CommittedTimerRemaining,
            PresentationDriver.CommittedOscillatorPhase
        ];
        foreach (var driver in continuous)
        {
            Assert.True(PresentationDriverRules.Supports(driver, PresentationCurve.Linear, 0f, PresentationOverlap.Replace));
            Assert.True(PresentationDriverRules.Supports(driver, PresentationCurve.ExponentialFollow, 0f, PresentationOverlap.Replace));

            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.SmoothStep, 0.2f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.CubicSmoothStep, 0.2f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.Linear, 0.2f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.ExponentialFollow, 0.2f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.LinearDecay, 0.2f, PresentationOverlap.Maximum));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.SineSquaredPulse, 0.2f, PresentationOverlap.SaturatingSum));
        }
    }

    [Fact]
    public void DriverRules_Supports_LevelDriversSupportLinearAndTweenClipsOnly()
    {
        PresentationDriver[] level =
        [
            PresentationDriver.CommittedActivation,
            PresentationDriver.CommittedCount,
            PresentationDriver.CommittedElectricalSocket,
            PresentationDriver.CommittedSupply,
            PresentationDriver.CommittedCharge,
            PresentationDriver.CommittedActuatorDrive,
            PresentationDriver.CommittedTimerActive
        ];
        foreach (var driver in level)
        {
            Assert.True(PresentationDriverRules.Supports(driver, PresentationCurve.Linear, 0f, PresentationOverlap.Replace));
            Assert.True(PresentationDriverRules.Supports(driver, PresentationCurve.SmoothStep, 0.2f, PresentationOverlap.Replace));
            Assert.True(PresentationDriverRules.Supports(driver, PresentationCurve.CubicSmoothStep, 0.2f, PresentationOverlap.Replace));

            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.ExponentialFollow, 0f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.Linear, 0.2f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.SmoothStep, 0f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.CubicSmoothStep, 0f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.LinearDecay, 0.2f, PresentationOverlap.Maximum));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.SineSquaredPulse, 0.2f, PresentationOverlap.SaturatingSum));
        }
    }

    [Fact]
    public void DriverRules_Supports_Discrete3StateDriversSupportLinearOnly()
    {
        PresentationDriver[] discrete =
        [
            PresentationDriver.CommittedActuatorState,
            PresentationDriver.CommittedContactLoadPhase,
            PresentationDriver.CommittedTimerPhase
        ];
        foreach (var driver in discrete)
        {
            Assert.True(PresentationDriverRules.Supports(driver, PresentationCurve.Linear, 0f, PresentationOverlap.Replace));

            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.ExponentialFollow, 0f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.SmoothStep, 0.2f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.CubicSmoothStep, 0.2f, PresentationOverlap.Replace));
            Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.Linear, 0.2f, PresentationOverlap.Replace));
        }
    }

    [Fact]
    public void DriverRules_Supports_EventDriverSupportsClipsAndImpulses()
    {
        var driver = PresentationDriver.Event;
        Assert.True(PresentationDriverRules.Supports(driver, PresentationCurve.SmoothStep, 0.2f, PresentationOverlap.Replace));
        Assert.True(PresentationDriverRules.Supports(driver, PresentationCurve.CubicSmoothStep, 0.2f, PresentationOverlap.Replace));
        Assert.True(PresentationDriverRules.Supports(driver, PresentationCurve.SineSquaredPulse, 0.2f, PresentationOverlap.SaturatingSum));

        Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.Linear, 0f, PresentationOverlap.Replace));
        Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.ExponentialFollow, 0f, PresentationOverlap.Replace));
        Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.LinearDecay, 0.2f, PresentationOverlap.Maximum));
        Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.SineSquaredPulse, 0.2f, PresentationOverlap.Replace));
        Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.SmoothStep, 0f, PresentationOverlap.Replace));
    }

    [Fact]
    public void DriverRules_Supports_OscillatorPulseDriverSupportsLinearDecayOnly()
    {
        var driver = PresentationDriver.CommittedOscillatorPulse;
        Assert.True(PresentationDriverRules.Supports(driver, PresentationCurve.LinearDecay, 0.25f, PresentationOverlap.Maximum));

        Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.LinearDecay, 0f, PresentationOverlap.Maximum));
        Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.LinearDecay, 0.25f, PresentationOverlap.Replace));
        Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.Linear, 0f, PresentationOverlap.Replace));
        Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.SmoothStep, 0.25f, PresentationOverlap.Replace));
        Assert.False(PresentationDriverRules.Supports(driver, PresentationCurve.ExponentialFollow, 0f, PresentationOverlap.Replace));
    }

    [Fact]
    public void DriverRules_SupportsMapping_ValidatesDomainMappings()
    {
        foreach (PresentationDriver driver in Enum.GetValues<PresentationDriver>())
        {
            var expectedIdentity = driver is not (PresentationDriver.CommittedActuatorState or
                                                 PresentationDriver.CommittedContactLoadPhase or
                                                 PresentationDriver.CommittedTimerPhase);
            Assert.Equal(expectedIdentity, PresentationDriverRules.SupportsMapping(driver, PresentationValueMapping.Identity));

            var expectedSine = driver is PresentationDriver.CommittedOscillatorPhase or
                                        PresentationDriver.CommittedTimerProgress or
                                        PresentationDriver.CommittedTimerRemaining;
            Assert.Equal(expectedSine, PresentationDriverRules.SupportsMapping(driver, PresentationValueMapping.SineCyclesToUnit));

            var expectedState = driver is PresentationDriver.CommittedActuatorState or
                                         PresentationDriver.CommittedContactLoadPhase or
                                         PresentationDriver.CommittedTimerPhase;
            Assert.Equal(expectedState, PresentationDriverRules.SupportsMapping(driver, PresentationValueMapping.StateValues));

            Assert.False(PresentationDriverRules.SupportsMapping(driver, (PresentationValueMapping)99));
        }
    }

    [Fact]
    public void DriverRules_InitialValue_ReturnsAuthoredResetState()
    {
        var stateValues = new PresentationStateValues(0.2f, 0.8f, 0.5f);
        var stateBinding = new PresentationBinding("Node", PresentationProperty.LocalX,
            PresentationDriver.CommittedActuatorState, PresentationValueMapping.StateValues,
            0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
            StateValues: stateValues);
        Assert.Equal(0.2f, PresentationDriverRules.InitialValue(stateBinding));

        var levelBinding = new PresentationBinding("Node", PresentationProperty.LocalX,
            PresentationDriver.CommittedActivation, PresentationValueMapping.Identity,
            -2.5f, 4.5f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f);
        Assert.Equal(-2.5f, PresentationDriverRules.InitialValue(levelBinding));

        var followBinding = new PresentationBinding("Node", PresentationProperty.LocalX,
            PresentationDriver.CommittedWheelAngle, PresentationValueMapping.Identity,
            1.5f, 3.5f, PresentationCurve.ExponentialFollow, 0f, PresentationOverlap.Replace, 0f, Response: 10f);
        Assert.Equal(1.5f, PresentationDriverRules.InitialValue(followBinding));
    }

    [Fact]
    public void PresentationStateValues_MapsAllThreeDiscreteDomains()
    {
        var values = new PresentationStateValues(0.1f, 0.6f, 0.9f);
        Assert.True(values.IsFinite());

        Assert.Equal(0.1f, values.Map(ActuatorObservationState.Unpowered));
        Assert.Equal(0.6f, values.Map(ActuatorObservationState.Powered));
        Assert.Equal(0.9f, values.Map(ActuatorObservationState.Blocked));
        Assert.Throws<ArgumentOutOfRangeException>(() => values.Map((ActuatorObservationState)99));

        Assert.Equal(0.1f, values.Map(ContactLoadPhase.Empty));
        Assert.Equal(0.6f, values.Map(ContactLoadPhase.Loaded));
        Assert.Equal(0.9f, values.Map(ContactLoadPhase.Underweight));
        Assert.Throws<ArgumentOutOfRangeException>(() => values.Map((ContactLoadPhase)99));

        Assert.Equal(0.1f, values.Map(ActivationTimerPhase.Ready));
        Assert.Equal(0.6f, values.Map(ActivationTimerPhase.Counting));
        Assert.Equal(0.9f, values.Map(ActivationTimerPhase.Finished));
        Assert.Throws<ArgumentOutOfRangeException>(() => values.Map((ActivationTimerPhase)99));

        Assert.False(new PresentationStateValues(float.NaN, 1f, 2f).IsFinite());
        Assert.False(new PresentationStateValues(0f, float.PositiveInfinity, 2f).IsFinite());
        Assert.False(new PresentationStateValues(0f, 1f, float.NegativeInfinity).IsFinite());
    }

    [Fact]
    public void BundleValidator_EnforcesSinglePropertyWriterUniqueness()
    {
        var template = TestBundle.Entry(new("battery"));
        var validBindings = new PresentationBinding[]
        {
            new("Visual/Terminal", PresentationProperty.AlbedoRed, PresentationDriver.CommittedSupply, PresentationValueMapping.Identity, 0, 1, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, SupplyNode: new("source")),
            new("Visual/Terminal", PresentationProperty.AlbedoGreen, PresentationDriver.CommittedSupply, PresentationValueMapping.Identity, 0, 1, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, SupplyNode: new("source")),
            new("Visual/Mark_0", PresentationProperty.AlbedoRed, PresentationDriver.CommittedSupply, PresentationValueMapping.Identity, 0, 1, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, SupplyNode: new("source"))
        };
        var validEntry = template with { Presentation = template.Presentation with { Bindings = validBindings } };
        Assert.Empty(BundleValidator.Validate([new("test_battery", validEntry)]));

        var duplicateBindings = new PresentationBinding[]
        {
            new("Visual/Terminal", PresentationProperty.AlbedoRed, PresentationDriver.CommittedSupply, PresentationValueMapping.Identity, 0, 1, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, SupplyNode: new("source")),
            new("Visual/Terminal", PresentationProperty.AlbedoRed, PresentationDriver.CommittedCharge, PresentationValueMapping.Identity, 0, 1, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, SupplyNode: new("source"))
        };
        var duplicateEntry = template with { Presentation = template.Presentation with { Bindings = duplicateBindings } };
        var errors = BundleValidator.Validate([new("test_battery_dup", duplicateEntry)]);
        Assert.Contains(errors, e => e.Detail.Contains("duplicate property writer"));
    }

    [Fact]
    public void BundleValidator_ValidatesStateValues()
    {
        var template = TestBundle.Entry(new("delay"));
        var validStateValues = new PresentationStateValues(0.3f, 0.7f, 0.5f);

        // Missing StateValues when Mapping == StateValues
        var missingState = template.Presentation.Bindings[1] with { StateValues = null };
        var badEntry = template with { Presentation = template.Presentation with { Bindings = [missingState] } };
        Assert.Contains(BundleValidator.Validate([new("test_delay_bad", badEntry)]), e => e.Detail.Contains("Presentation.StateValues"));

        // Non-finite StateValues
        var nonFiniteState = template.Presentation.Bindings[1] with { StateValues = new(float.NaN, 0f, 0f) };
        badEntry = template with { Presentation = template.Presentation with { Bindings = [nonFiniteState] } };
        Assert.Contains(BundleValidator.Validate([new("test_delay_bad", badEntry)]), e => e.Detail.Contains("Presentation.StateValues"));

        // StateValues component out of range for property (AlbedoRed requires [0, 1])
        var outOfRangeState = template.Presentation.Bindings[1] with { StateValues = new(1.5f, 0.5f, 0.5f) };
        badEntry = template with { Presentation = template.Presentation with { Bindings = [outOfRangeState] } };
        Assert.Contains(BundleValidator.Validate([new("test_delay_bad", badEntry)]), e => e.Detail.Contains("Presentation.StateValues"));

        // Extraneous StateValues when Mapping != StateValues
        var batteryTemplate = TestBundle.Entry(new("battery"));
        var extraneousState = batteryTemplate.Presentation.Bindings[0] with { StateValues = validStateValues };
        badEntry = batteryTemplate with { Presentation = batteryTemplate.Presentation with { Bindings = [extraneousState] } };
        Assert.Contains(BundleValidator.Validate([new("test_battery_bad", badEntry)]), e => e.Detail.Contains("Presentation.StateValues"));
    }

    [Fact]
    public void BundleValidator_RejectsDriverCurveMismatch()
    {
        var template = TestBundle.Entry(new("battery"));
        // Linear with duration > 0 is forbidden
        var badCurve = template.Presentation.Bindings[0] with { Curve = PresentationCurve.Linear, Duration = 0.5f };
        var badEntry = template with { Presentation = template.Presentation with { Bindings = [badCurve] } };
        Assert.Contains(BundleValidator.Validate([new("test_battery_bad", badEntry)]), e => e.Detail.Contains("Presentation.DriverCurve"));

        // ExponentialFollow on level driver (battery supply) is forbidden
        var badFollow = template.Presentation.Bindings[0] with { Curve = PresentationCurve.ExponentialFollow, Duration = 0f, Response = 10f };
        badEntry = template with { Presentation = template.Presentation with { Bindings = [badFollow] } };
        Assert.Contains(BundleValidator.Validate([new("test_battery_bad", badEntry)]), e => e.Detail.Contains("Presentation.DriverCurve"));
    }

    [Fact]
    public async Task WorkshopDeclaredPresentation_UnfamiliarBindings_DifferentCurvesForOneSource()
    {
        var setClock = PlacedParts.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 0.1f);
        var battery = PlacedParts.Battery(new(2), -6, 0, 0);
        var latch = PlacedParts.Latch(new(3), 0, 0, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(setClock, battery, latch),
            Connections: new(
                new(battery.Id, PortId.Supply, setClock.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new(setClock.Id, PortId.ActivationOut, latch.Id, PortId.SetIn, WorkshopConnectionDomain.Activation)));

        var reads = await WorkshopIntegrationHarness.Observe(construction, 30);
        var compiled = CompiledSceneFixture.Compile(construction);
        var baseTrack = compiled.Presentation.First(t => t.Owner == latch.Id);

        var customTracks = new CompiledPresentationTrack[]
        {
            baseTrack with { Binding = 0, Target = new(501), Declaration = baseTrack.Declaration with { Curve = PresentationCurve.Linear, Duration = 0f } },
            baseTrack with { Binding = 1, Target = new(502), Declaration = baseTrack.Declaration with { Curve = PresentationCurve.SmoothStep, Duration = 0.2f } },
            baseTrack with { Binding = 2, Target = new(503), Declaration = baseTrack.Declaration with { Curve = PresentationCurve.CubicSmoothStep, Duration = 0.2f } }
        };

        var allTracks = compiled.Presentation.Where(t => t.Owner != latch.Id).Concat(customTracks).ToArray();
        var customScene = new WorkshopCompiledScene(compiled.Identity, compiled.Revision, compiled.Settings,
            compiled.Physics, compiled.Activation, allTracks, compiled.Owners, compiled.RunControls);

        var presentation = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), customScene, Response(reads[1], 1));
        var samples0 = presentation.Advance(AnimationTestTime.Seconds(Time(reads[1])));
        Assert.Equal(baseTrack.Declaration.From, Scalar(samples0, latch.Id, 0));
        Assert.Equal(baseTrack.Declaration.From, Scalar(samples0, latch.Id, 1));
        Assert.Equal(baseTrack.Declaration.From, Scalar(samples0, latch.Id, 2));

        // When input latches to 1
        var latchedRead = reads.First(r =>
        {
            for (var i = 0; i < r.Activations.Count; i++)
            {
                if (r.Activations[i].Owner == latch.Id && r.Activations[i].Phase == ActivationPhase.Latched) return true;
            }
            return false;
        });
        var latchedTime = AnimationTestTime.Seconds(Time(latchedRead));
        presentation.Advance(latchedTime);
        presentation.Accept(Response(latchedRead, 2));

        // At t = 0 (occurrence): linear steps immediately to To, clips remain at From
        var samplesAtStart = presentation.Advance(latchedTime);
        Assert.Equal(baseTrack.Declaration.To, Scalar(samplesAtStart, latch.Id, 0));

        // At t = 0.1s (intermediate progress 50%): SmoothStep and CubicSmoothStep are strictly between From and To
        var samplesMid = presentation.Advance(AnimationTestTime.Seconds(Time(latchedRead) + 0.1));
        var midSmooth = Scalar(samplesMid, latch.Id, 1);
        var midCubic = Scalar(samplesMid, latch.Id, 2);
        Assert.True(midSmooth > baseTrack.Declaration.From && midSmooth < baseTrack.Declaration.To);
        Assert.True(midCubic > baseTrack.Declaration.From && midCubic < baseTrack.Declaration.To);
        // Halfway evaluation on symmetric smooth step is 0.5
        Assert.Equal(float.Lerp(baseTrack.Declaration.From, baseTrack.Declaration.To, 0.5f), midSmooth, 4);

        // At t = 0.2s: both clips reach To
        var samplesEnd = presentation.Advance(AnimationTestTime.Seconds(Time(latchedRead) + 0.2));
        Assert.Equal(baseTrack.Declaration.To, Scalar(samplesEnd, latch.Id, 1), 4);
        Assert.Equal(baseTrack.Declaration.To, Scalar(samplesEnd, latch.Id, 2), 4);
    }

    [Fact]
    public async Task WorkshopDeclaredPresentation_OneCurveWithDifferentSources_ExponentialFollow()
    {
        var trampoline = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("trampoline"), new(1), 0, 0, 0, 0, 0, 0, 1);
        var clock = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("clock"), new(2), 2, 0, 0, 0, 0, 0, 1);
        var pulley = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("pulley"), new(3), 4, 0, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(trampoline, clock, pulley));
        var reads = await WorkshopIntegrationHarness.Observe(construction, 5);
        var compiled = CompiledSceneFixture.Compile(construction);

        var pulleyTrack = compiled.Presentation.First(t => t.Owner == pulley.Id && t.Declaration.Driver == PresentationDriver.CommittedWheelAngle);
        var trampolineTrack = compiled.Presentation.First(t => t.Owner == trampoline.Id && t.Declaration.Driver == PresentationDriver.CommittedSurfaceDeflection);
        var clockTrack = compiled.Presentation.First(t => t.Owner == clock.Id && t.Declaration.Driver == PresentationDriver.CommittedOscillatorPhase);

        var customTracks = compiled.Presentation.Select(t =>
            t.Owner == pulley.Id && t.Declaration.Driver == PresentationDriver.CommittedWheelAngle
                ? t with { Declaration = t.Declaration with { Curve = PresentationCurve.ExponentialFollow, Response = 10f } }
                : t.Owner == trampoline.Id && t.Declaration.Driver == PresentationDriver.CommittedSurfaceDeflection
                ? t with { Declaration = t.Declaration with { Curve = PresentationCurve.ExponentialFollow, Response = 10f, From = 0f, To = 1f } }
                : t.Owner == clock.Id && t.Declaration.Driver == PresentationDriver.CommittedOscillatorPhase
                ? t with { Declaration = t.Declaration with { Mapping = PresentationValueMapping.Identity, Curve = PresentationCurve.ExponentialFollow, Response = 10f, From = 0f, To = 1f } }
                : t).ToArray();

        var customScene = new WorkshopCompiledScene(compiled.Identity, compiled.Revision, compiled.Settings,
            compiled.Physics, compiled.Activation, customTracks, compiled.Owners, compiled.RunControls);

        var evaluator = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), customScene, Response(reads[1], 1));
        var samples0 = evaluator.Advance(AnimationTestTime.Seconds(Time(reads[1])));
        Assert.Equal(0f, Scalar(samples0, pulley.Id, pulleyTrack.Binding));
        Assert.Equal(0f, Scalar(samples0, trampoline.Id, trampolineTrack.Binding));

        // Read with updated wheel angle, surface deflection, and oscillator phase
        var basePres = reads[1].Presentation;
        var newGuides = new[] { new RopeGuideRead(pulleyTrack.Guide, 1f, 1.0f) };
        var newSurfaces = new[] { new CompliantSurfaceRead(trampoline.Id, 0.2f) };
        var updatedPres = new PhysicsPresentationRead(basePres.Actuators, basePres.Loads, basePres.Logic, basePres.Ropes, newGuides, newSurfaces);

        var memoryList = new List<ActivationMemory>();
        for (var i = 0; i < reads[1].Memory.Count; i++)
        {
            var m = reads[1].Memory[i];
            if (m.Node == clockTrack.Activation.Id)
                memoryList.Add(m with { Phase = OscillatorPhase.Running, DueTick = reads[1].Tick.Value + 60 });
            else
                memoryList.Add(m);
        }
        var updatedRead = reads[1] with { Presentation = updatedPres, Memory = new(memoryList.ToArray()) };
        evaluator.Accept(Response(updatedRead, 2));

        // Intermediate advancement at t + 0.05s: follow progresses towards targets
        var samplesMid = evaluator.Advance(AnimationTestTime.Seconds(Time(reads[1]) + 0.05));
        var valPulley = Scalar(samplesMid, pulley.Id, pulleyTrack.Binding);
        var valTramp = Scalar(samplesMid, trampoline.Id, trampolineTrack.Binding);
        var valClock = Scalar(samplesMid, clock.Id, clockTrack.Binding);

        Assert.True(valPulley > 0f && valPulley < 1.0f);
        Assert.True(valTramp > 0f && valTramp < 0.2f);
        Assert.True(valClock > 0f && valClock < 0.5f);

        // Long advancement at t + 1.0s: converged to targets
        var samplesEnd = evaluator.Advance(AnimationTestTime.Seconds(Time(reads[1]) + 1.0));
        Assert.Equal(1.0f, Scalar(samplesEnd, pulley.Id, pulleyTrack.Binding), 2);
        Assert.Equal(0.2f, Scalar(samplesEnd, trampoline.Id, trampolineTrack.Binding), 2);
        Assert.Equal(0.5f, Scalar(samplesEnd, clock.Id, clockTrack.Binding), 2);
    }

    [Fact]
    public async Task WorkshopDeclaredPresentation_TargetScaleAndOffset_AndWheelBypassDeleted()
    {
        var pulley = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("pulley"), new(1), 0f, 0f, 0f, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(pulley));
        var reads = await WorkshopIntegrationHarness.Observe(construction, 1);
        var read = reads[0];

        // Wheel angle 2.0 radians
        var pres = new PhysicsPresentationRead(read.Presentation.Actuators, read.Presentation.Loads,
            read.Presentation.Logic, read.Presentation.Ropes, [new(new(1), 0.8f, 2.0f)], read.Presentation.Surfaces);

        // Default pulley has From: 0, To: 1 -> output is 2.0
        var compiled = CompiledSceneFixture.Compile(construction);
        var evaluator = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), compiled, Response(read with { Presentation = pres }, 1));
        var samples = evaluator.Advance(AnimationTestTime.Seconds(read.Tick.Value / 120.0));
        var wheelSample = Assert.Single(samples, s => s.Owner == new GpuBodyId(1) && s.Binding == 0);
        Assert.Equal(2.0f, BitConverter.UInt32BitsToSingle(wheelSample.Value.CanonicalBits));

        // Custom gear-ratio pulley: From: 0, To: 2 (2:1 gear ratio) -> output is 4.0 (proves wheel bypass deletion)
        var template = TestBundle.Entry(new("pulley"));
        var customBindings = new PresentationBinding[]
        {
            new("Visual/Wheel", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedWheelAngle, PresentationValueMapping.Identity,
                0f, 2f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, GuidePort: PortId.Tie)
        };
        var customPulley = template with { Metadata = template.Metadata with { Id = "custom_pulley" }, Presentation = template.Presentation with { Bindings = customBindings } };
        using var physics = DeclaredPhysics.Scoped([customPulley]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new("custom_pulley"), customPulley.Ports)]));

        var scaledPulley = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("custom_pulley"), new(1), 0f, 0f, 0f, 0, 0, 0, 1);
        var scaledConstruction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(scaledPulley));
        var scaledCompiled = CompiledSceneFixture.Compile(scaledConstruction);
        var scaledEvaluator = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), scaledCompiled, Response(read with { Presentation = pres }, 1));
        var scaledSamples = scaledEvaluator.Advance(AnimationTestTime.Seconds(read.Tick.Value / 120.0));
        var wheelSampleScaled = Assert.Single(scaledSamples, s => s.Owner == new GpuBodyId(1) && s.Binding == 0);
        Assert.Equal(4.0f, BitConverter.UInt32BitsToSingle(wheelSampleScaled.Value.CanonicalBits));
    }

    [Fact]
    public async Task WorkshopDeclaredPresentation_UnifiedStateValues_AcrossActuatorContactAndTimer()
    {
        // 1. PoweredGate (ActuatorState)
        var gate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("powered_gate"), new(1), 0, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(gate));
        var reads = await WorkshopIntegrationHarness.Observe(construction, 5);
        var compiled = CompiledSceneFixture.Compile(construction);

        var presentation = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), compiled, Response(reads[1], 1));
        var samples = presentation.Advance(AnimationTestTime.Seconds(Time(reads[1])));
        // Unpowered gate emits State0 (85/255, 101/255, 115/255) for all 3 colour channels
        Assert.Equal(85f / 255f, Scalar(samples, gate.Id, 0));
        Assert.Equal(101f / 255f, Scalar(samples, gate.Id, 1));
        Assert.Equal(115f / 255f, Scalar(samples, gate.Id, 2));

        // 2. DelayBox (TimerPhase): verify Ready (State0) is not suppressed
        var delay = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("delay"), new(1), 0, 3, 0, 0, 0, 0, 1,
            new InstanceParameterCollection([new(new ParameterId("delay_seconds"), .4f)]));
        var delayConstruction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(delay));
        var delayReads = await WorkshopIntegrationHarness.Observe(delayConstruction, 5);
        var delayCompiled = CompiledSceneFixture.Compile(delayConstruction);

        var delayPresentation = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), delayCompiled, Response(delayReads[1], 1));
        var delaySamples = delayPresentation.Advance(AnimationTestTime.Seconds(Time(delayReads[1])));
        // Ready emits State0 (85/255, 101/255, 115/255) and is not suppressed (proves timer suppression deletion)
        Assert.Equal(85f / 255f, Scalar(delaySamples, delay.Id, 1));
        Assert.Equal(101f / 255f, Scalar(delaySamples, delay.Id, 2));
        Assert.Equal(115f / 255f, Scalar(delaySamples, delay.Id, 3));
    }

    [Fact]
    public async Task WorkshopDeclaredPresentation_Discrete3State_AllThreeStatesAndTransitions()
    {
        // 1. DelayBox (TimerPhase): Observe real transition: Ready -> Counting -> Finished
        var trigger = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(1), 0, 1, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new(2), 0, 3, 0, 0, 0, 0, 1);
        var delay = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("delay"), new(3), 2, 1, 0, 0, 0, 0, 1,
            new InstanceParameterCollection([new(new ParameterId("delay_seconds"), 0.1f)]));
        var lamp = PlacedParts.Lamp(new(4), 4, 1, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(trigger, ball, delay, lamp),
            Connections: new(
                new(trigger.Id, PortId.ActivationOut, delay.Id, PortId.ActivationIn, WorkshopConnectionDomain.Activation),
                new(delay.Id, PortId.ActivationOut, lamp.Id, PortId.ActivationIn, WorkshopConnectionDomain.Activation)));

        var reads = await WorkshopIntegrationHarness.Observe(construction, 120);
        var compiled = CompiledSceneFixture.Compile(construction);

        var readyRead = reads.First(r => r.Timers[0].Phase == ActivationTimerPhase.Ready);
        var countingRead = reads.First(r => r.Timers[0].Phase == ActivationTimerPhase.Counting);
        var finishedRead = reads.First(r => r.Timers[0].Phase == ActivationTimerPhase.Finished);

        var delayPresentation = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), compiled, Response(readyRead, 1));

        // Ready (State0) -> Red=85, Green=101, Blue=115
        var readySamples = delayPresentation.Advance(AnimationTestTime.Seconds(Time(readyRead)));
        Assert.Equal(85f / 255f, Scalar(readySamples, delay.Id, 1), 3);
        Assert.Equal(101f / 255f, Scalar(readySamples, delay.Id, 2), 3);
        Assert.Equal(115f / 255f, Scalar(readySamples, delay.Id, 3), 3);

        // Counting (State1) -> Red=232, Green=183, Blue=100
        delayPresentation.Accept(Response(countingRead, 2));
        var countingSamples = delayPresentation.Advance(AnimationTestTime.Seconds(Time(countingRead)));
        Assert.Equal(232f / 255f, Scalar(countingSamples, delay.Id, 1), 3);
        Assert.Equal(183f / 255f, Scalar(countingSamples, delay.Id, 2), 3);
        Assert.Equal(100f / 255f, Scalar(countingSamples, delay.Id, 3), 3);

        // Finished (State2) -> Red=247, Green=203, Blue=82
        delayPresentation.Accept(Response(finishedRead, 3));
        var finishedSamples = delayPresentation.Advance(AnimationTestTime.Seconds(Time(finishedRead)));
        Assert.Equal(247f / 255f, Scalar(finishedSamples, delay.Id, 1), 3);
        Assert.Equal(203f / 255f, Scalar(finishedSamples, delay.Id, 2), 3);
        Assert.Equal(82f / 255f, Scalar(finishedSamples, delay.Id, 3), 3);

        // 2. PressurePlate (ContactLoadPhase): Empty -> Loaded -> Underweight
        var plate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("pressure_plate"), new(10), 0, 0, 0, 0, 0, 0, 1);
        var plateConstruction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(plate));
        var plateReads = await WorkshopIntegrationHarness.Observe(plateConstruction, 5);
        var plateCompiled = CompiledSceneFixture.Compile(plateConstruction);

        var platePresentation = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), plateCompiled, Response(plateReads[1], 1));

        // Empty (State0) -> Red=85, Green=101, Blue=115
        var emptySamples = platePresentation.Advance(AnimationTestTime.Seconds(Time(plateReads[1])));
        Assert.Equal(85f / 255f, Scalar(emptySamples, plate.Id, 0), 3);
        Assert.Equal(101f / 255f, Scalar(emptySamples, plate.Id, 1), 3);
        Assert.Equal(115f / 255f, Scalar(emptySamples, plate.Id, 2), 3);

        // Transition to Loaded (State1) -> Red=247, Green=203, Blue=82
        var baseLoad = Assert.Single(plateReads[1].Presentation.Loads.ToArray());
        var loadedLoads = new[] { baseLoad with { Phase = ContactLoadPhase.Loaded, MeasuredMass = 10f } };
        var loadedPres = new PhysicsPresentationRead(
            plateReads[1].Presentation.Actuators,
            loadedLoads,
            plateReads[1].Presentation.Logic,
            plateReads[1].Presentation.Ropes,
            plateReads[1].Presentation.Guides,
            plateReads[1].Presentation.Surfaces);
        platePresentation.Accept(Response(plateReads[1] with { Presentation = loadedPres }, 2));
        var loadedSamples = platePresentation.Advance(AnimationTestTime.Seconds(Time(plateReads[1]) + 0.1));
        Assert.Equal(247f / 255f, Scalar(loadedSamples, plate.Id, 0), 3);
        Assert.Equal(203f / 255f, Scalar(loadedSamples, plate.Id, 1), 3);
        Assert.Equal(82f / 255f, Scalar(loadedSamples, plate.Id, 2), 3);

        // Transition to Underweight (State2) -> Red=232, Green=183, Blue=100
        var underweightLoads = new[] { baseLoad with { Phase = ContactLoadPhase.Underweight, MeasuredMass = 0.5f } };
        var underweightPres = new PhysicsPresentationRead(
            plateReads[1].Presentation.Actuators,
            underweightLoads,
            plateReads[1].Presentation.Logic,
            plateReads[1].Presentation.Ropes,
            plateReads[1].Presentation.Guides,
            plateReads[1].Presentation.Surfaces);
        platePresentation.Accept(Response(plateReads[1] with { Presentation = underweightPres }, 3));
        var underweightSamples = platePresentation.Advance(AnimationTestTime.Seconds(Time(plateReads[1]) + 0.2));
        Assert.Equal(232f / 255f, Scalar(underweightSamples, plate.Id, 0), 3);
        Assert.Equal(183f / 255f, Scalar(underweightSamples, plate.Id, 1), 3);
        Assert.Equal(100f / 255f, Scalar(underweightSamples, plate.Id, 2), 3);
    }

    [Fact]
    public async Task WorkshopDeclaredPresentation_Query_ThrowsOnMissingSource()
    {
        var pulley = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("pulley"), new(1), 0f, 0f, 0f, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(pulley));
        var reads = await WorkshopIntegrationHarness.Observe(construction, 1);
        var compiled = CompiledSceneFixture.Compile(construction);

        // Presentation read lacks guides for the compiled wheel angle track
        var basePres = reads[0].Presentation;
        var emptyPres = new PhysicsPresentationRead(basePres.Actuators, basePres.Loads, basePres.Logic, basePres.Ropes, [], basePres.Surfaces);
        var emptyRead = reads[0] with { Presentation = emptyPres };

        var ex = Assert.Throws<ArgumentException>(() => new WorkshopDeclaredPresentation(
            new CuriousContraptions.Presentation.AnimationServices(), compiled, Response(emptyRead, 1)));
        Assert.Contains("Declared presentation query is absent", ex.Message);
    }

    [Fact]
    public void MachinePart_ResetPhysicalVisuals_RestoresAuthoredState0()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();

        var platePart = registry.Create(new PartId("pressure_plate"));
        try
        {
            godot.Tree.Root.AddChild(platePart);
            // Initially authored State0 (#556573 -> 85/255, 101/255, 115/255)
            var mat = Assert.IsType<StandardMaterial3D>(((MeshInstance3D)platePart.GetNode("Visual/Stud_0")).MaterialOverride);
            Assert.Equal(85f / 255f, mat.AlbedoColor.R, 3);
            Assert.Equal(101f / 255f, mat.AlbedoColor.G, 3);
            Assert.Equal(115f / 255f, mat.AlbedoColor.B, 3);

            // Apply altered visual property (turn red channel to 1.0)
            platePart.ApplyDeclaredProperty(0, WorkshopDeclaredPresentation.Value(PresentationProperty.AlbedoRed, 1.0f));
            Assert.Equal(1.0f, mat.AlbedoColor.R, 3);

            // Reset restores State0 via PresentationDriverRules.InitialValue, not 0 (pitch black)
            platePart.ResetPhysicalVisuals();
            Assert.Equal(85f / 255f, mat.AlbedoColor.R, 3);
            Assert.Equal(101f / 255f, mat.AlbedoColor.G, 3);
            Assert.Equal(115f / 255f, mat.AlbedoColor.B, 3);
        }
        finally
        {
            platePart.Free();
        }
    }

    private static WorkshopResponse Response(WorkshopRead read, ulong publication) =>
        new(default, WorkshopResponseKind.Read, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running, read, new(1, 2), new(publication), new(1),
            read.Capture?.Generation ?? new(1), new(1), new(read.Tick.Value));

    private static float Scalar(IReadOnlyList<DeclaredPresentationSample> samples, GpuBodyId owner, int binding) =>
        BitConverter.UInt32BitsToSingle(Assert.Single(samples, s => s.Owner == owner && s.Binding == binding).Value.CanonicalBits);

    private static double Time(WorkshopRead read) => read.Tick.Value / 120.0;
}
