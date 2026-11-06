using System;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Presentation;
using AnimationProviders = CuriousContraptions.Animation.Worker.RuntimeProviders;
using WebProviders = CuriousContraptions.Web.RuntimeProviders;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class ProviderConstructionTests
{
    [Fact]
    public void FixedProductionProvidersExposeTheirActualServices()
    {
        var animation = AnimationProviders.Animation();
        var first = animation.CreateBatch();
        var second = animation.CreateBatch();
        var definition = new AnimationDefinition(new(new AnimationOpacity(0)), new(new AnimationOpacity(1)),
            new(1), AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Simulation);
        var binding = new AnimationBinding(new(1), AnimationProperty.Opacity);
        var firstHandle = first.Register(binding, definition);
        var secondHandle = second.Register(binding, definition);
        first.StartAt(firstHandle, new(0));
        first.Advance(1, new(240), new(240));
        Assert.Equal(.5f, first.Read(firstHandle).Value.Opacity.Value);
        Assert.Equal(0f, second.Read(secondHandle).Value.Opacity.Value);
        Assert.Throws<ArgumentException>(() => second.Read(firstHandle));
        first.Reset();
        Assert.Equal(0f, second.Read(secondHandle).Value.Opacity.Value);
        Assert.NotEqual(IntPtr.Zero, WebProviders.GameBootstrap().PluginsInitializer());
    }

    [Fact]
    public void AnimationProviderRefusalsDoNotPreventALaterIndependentInitialization()
    {
        AssertAnimationRefused(() => AnimationProviders.ConstructAnimation(
            Type.GetType("Absent.Provider, Absent.Runtime")), EngineProviderRefusal.ProviderUnavailable);
        AssertAnimationRefused(() => AnimationProviders.ConstructAnimation(typeof(object)), EngineProviderRefusal.ProviderContractMismatch);
        AssertAnimationRefused(() => AnimationProviders.ConstructAnimation(typeof(MissingConstructor)), EngineProviderRefusal.ProviderContractMismatch);
        AssertAnimationRefused(() => AnimationProviders.ConstructAnimation(typeof(ThrowingConstructor)), EngineProviderRefusal.ProviderConstructionFailed);
        Assert.NotNull(AnimationProviders.Animation().CreateBatch());
    }

    [Fact]
    public void GameBootstrapRefusalsDoNotPreventALaterIndependentInitialization()
    {
        AssertGameRefused(() => WebProviders.ConstructGameBootstrap(
            Type.GetType("Absent.Provider, Absent.Runtime")), EngineProviderRefusal.ProviderUnavailable);
        AssertGameRefused(() => WebProviders.ConstructGameBootstrap(typeof(object)), EngineProviderRefusal.ProviderContractMismatch);
        AssertGameRefused(() => WebProviders.ConstructGameBootstrap(typeof(MissingConstructor)), EngineProviderRefusal.ProviderContractMismatch);
        AssertGameRefused(() => WebProviders.ConstructGameBootstrap(typeof(ThrowingConstructor)), EngineProviderRefusal.ProviderConstructionFailed);
        Assert.NotEqual(IntPtr.Zero, WebProviders.GameBootstrap().PluginsInitializer());
    }


    [Fact]
    public void GameCoreAndBundleProvidersRefuseMissingOrWrongContractsAndRemainUsable()
    {
        foreach (var missing in new[] { true, false })
        {
            var type = missing ? null : typeof(object);
            var reason = missing ? EngineProviderRefusal.ProviderUnavailable : EngineProviderRefusal.ProviderContractMismatch;
            var coreError = Assert.Throws<EngineProviderException>(() => RuntimeProviders.ConstructCore(type));
            Assert.Equal((EngineProviderId.Core, reason), (coreError.Provider, coreError.Reason));
            var bundleError = Assert.Throws<EngineProviderException>(() => RuntimeProviders.ConstructBundle(type));
            Assert.Equal((EngineProviderId.Bundle, reason), (bundleError.Provider, bundleError.Reason));
        }
        var core = RuntimeProviders.Core();
        var first = core.CreateClientCursor(new(1, 2));
        var second = core.CreateClientCursor(new(3, 4));
        Assert.NotSame(first, second);
        Assert.Equal(0UL, second.ReadOrder);
        var bundle = RuntimeProviders.Bundle();
        Assert.Empty(bundle.Read(bundle.WriteEntries([])).Elements);
        Assert.Throws<System.Text.Json.JsonException>(() => bundle.Read("{"));
    }

    private static void AssertAnimationRefused(Action construct, EngineProviderRefusal reason)
    {
        var error = Assert.Throws<EngineProviderException>(construct);
        Assert.Equal(EngineProviderId.Animation, error.Provider);
        Assert.Equal(reason, error.Reason);
    }

    private static void AssertGameRefused(Action construct, EngineProviderRefusal reason)
    {
        var error = Assert.Throws<EngineProviderException>(construct);
        Assert.Equal(EngineProviderId.GameBootstrap, error.Provider);
        Assert.Equal(reason, error.Reason);
    }

    public sealed class MissingConstructor : IAnimationServices, IGameBootstrap
    {
        private MissingConstructor() { }
        public IAnimationBatch CreateBatch() => throw new InvalidOperationException();
        public IntPtr PluginsInitializer() => throw new InvalidOperationException();
    }

    public sealed class ThrowingConstructor : IAnimationServices, IGameBootstrap
    {
        public ThrowingConstructor() => throw new InvalidOperationException("Provider construction failed.");
        public IAnimationBatch CreateBatch() => throw new InvalidOperationException();
        public IntPtr PluginsInitializer() => throw new InvalidOperationException();
    }
}
