using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

internal sealed partial class LawScene
{
    private readonly List<AirflowSourceDeclaration> _airflowSources = [];
    private readonly List<AirflowReceiverDeclaration> _airflowTargets = [];
    private readonly List<RotaryAirflowCaptureDeclaration> _rotaryCaptures = [];

    public RotaryAirflowCaptureId RotaryCapture(GpuBodyId owner, GpuBodyId frame, RevoluteConstraintId coordinate,
        float response, float pitch = .4f, float cutIn = .05f, float maximumRate = 12,
        AirflowSample[]? samples = null, GpuColliderId[]? exclusions = null)
    {
        var id = new RotaryAirflowCaptureId(_next++);
        _rotaryCaptures.Add(new(id, owner, frame, coordinate,
            samples is null ? [new(Vector3.Zero, 1)] : [.. samples], exclusions is null ? [] : [.. exclusions],
            pitch, response, cutIn, maximumRate));
        return id;
    }

    public EnergyStoreId EnergyStore(GpuBodyId owner, float capacity, float initial, float chargePower = 0)
    {
        var id = new EnergyStoreId(_next++);
        _energyStores.Add(new(id, owner, new(capacity), new(initial), new(chargePower)));
        return id;
    }

    public AirflowSourceId Airflow(GpuBodyId frame, ContinuousWorkInput input, float force = 9, float speed = 12,
        float pressure = 1, bool enabled = true, Vector3? axis = null, params GpuColliderId[] exclusions)
    {
        var id = new AirflowSourceId(_next++);
        _airflowSources.Add(new(id, frame, frame, Vector3.Zero, axis ?? Vector3.UnitX, 5, 1, speed, force, force * speed,
            pressure, [ColliderOf(frame), .. exclusions], input, enabled));
        return id;
    }

    public ElectricalSourceDeclaration LimitedSource(GpuBodyId owner, float energy, float power)
    {
        var source = Source(owner) with { Capacity = new(energy), MaximumPower = new(power) };
        _sources[^1] = source;
        return source;
    }

    public AirflowSourceId ElectricalAirflow(GpuBodyId frame, ElectricalSourceDeclaration supply, float force, float speed)
    {
        var consumer = new SupplyNodeId(_next++);
        var source = Airflow(frame, new(ContinuousWorkInputKind.Electrical, default, consumer, default, 0, true), force, speed);
        _supplyNodes.Add(new(consumer, frame, SupplyNodeKind.Consumer, ContinuousWork: [source]));
        var provider = _supplyNodes.Single(node => node.Source == supply.Id).Id;
        _supplyEdges.Add(new(provider, consumer, CuriousContraptions.Abstractions.Electrical.SupplyTerminal.PowerIn));
        return source;
    }


    public void RevoluteSpring(RevoluteConstraintId coordinate, float stiffness)
    {
        var index = _revolutes.FindIndex(joint => joint.Id == coordinate);
        _revolutes[index] = _revolutes[index] with { Stiffness = stiffness };
    }


    public void ReverseResourceDeclarations()
    {
        _airflowSources.Reverse(); _airflowTargets.Reverse(); _rotaryCaptures.Reverse(); _sources.Reverse();
        _supplyNodes.Reverse(); _supplyEdges.Reverse(); _storageBindings.Reverse();
        _energyStores.Reverse(); _actuators.Reverse(); _chambers.Reverse();
    }

    public void AirflowTarget(GpuBodyId body, params AirflowSample[] samples) =>
        _airflowTargets.Add(new(new(_next++), body, ColliderOf(body), AirflowSampling.LocalPosition,
            samples.Length == 0 ? [new(Vector3.Zero, 1)] : [.. samples]));
}
