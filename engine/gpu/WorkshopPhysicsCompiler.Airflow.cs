using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopPhysicsCompiler
{
    private sealed partial class Compilation
    {
        private readonly List<AirflowSourceDeclaration> airflowSources = [];
        private readonly List<AirflowReceiverDeclaration> airflowReceivers = [];
        private readonly List<RotaryAirflowCaptureDeclaration> rotaryAirflowCaptures = [];
        private readonly List<CoordinateTravelDeclaration> coordinateTravelObservers = [];

        private void CompileCoordinateTravel()
        {
            foreach (var owner in construction.Instances)
            {
                var ids = identities[owner.Id];
                foreach (var observer in DeclaredPhysics.Of(owner.PartId).Mechanical?.CoordinateTravelObservers ?? [])
                    coordinateTravelObservers.Add(new(ids.CoordinateTravelObservers[observer.Id], owner.Id,
                        ids.Hinges[observer.Coordinate.Index].Joint, observer.Threshold));
            }
        }

        private void CompileAirflow()
        {
            var boundary = AtmosphereBoundary.For(construction.Atmosphere);
            foreach (var owner in construction.Instances)
            {
                var mechanical = DeclaredPhysics.Of(owner.PartId).Mechanical;
                if (mechanical is null) continue;
                var parameters = DeclaredPhysics.Parameters(owner);
                var ids = identities[owner.Id];
                foreach (var source in mechanical.AirflowSources)
                {
                    var input = source.Inputs.Single(i => i.Selection is not { } selection ||
                        parameters[selection.Parameter.Value] == selection.Option);
                    airflowSources.Add(new(ids.AirflowSources[source.Id], owner.Id,
                        identities.Body(owner.Id, source.Frame), source.Origin, source.Axis,
                        source.Reach.Evaluate(parameters), source.TerminalRadius.Evaluate(parameters),
                        source.FlowSpeed.Evaluate(parameters), source.MaximumForce.Evaluate(parameters),
                        source.MaximumPower.Evaluate(parameters), boundary.RelativePressure,
                        source.InternalOccluders.Select(c => identities.Collider(owner.Id, c.Body, c.Collider)).ToImmutableArray(),
                        CompileInput(owner.Id, ids, input), input.Enable is not { } configured || parameters[configured.InitialState.Value] == 1,
                        input.Enable is { } enable ? ids.Signals[enable.Latch].Node : null));
                }
                foreach (var capture in mechanical.RotaryAirflowCaptures)
                    rotaryAirflowCaptures.Add(new(ids.RotaryAirflowCaptures[capture.Id], owner.Id,
                        identities.Body(owner.Id, capture.Frame), ids.Hinges[capture.Coordinate.Index].Joint,
                        capture.Samples.ToImmutableArray(),
                        capture.InternalOccluders.Select(c => identities.Collider(owner.Id, c.Body, c.Collider)).ToImmutableArray(),
                        capture.MaximumPitch.Evaluate(parameters), capture.Response.Evaluate(parameters),
                        capture.CutInForce.Evaluate(parameters), capture.MaximumRate.Evaluate(parameters)));
                foreach (var receiver in mechanical.AirflowReceivers)
                    airflowReceivers.Add(new(ids.AirflowReceivers[receiver.Id],
                        identities.Body(owner.Id, receiver.Collider.Body),
                        identities.Collider(owner.Id, receiver.Collider.Body, receiver.Collider.Collider),
                        receiver.Sampling, receiver.Samples.ToImmutableArray()));
            }
        }

        private ContinuousWorkInput CompileInput(GpuBodyId owner, CompiledPartIdentities ids, AirflowInputDescriptor input) =>
            input switch
            {
                StoredAirflowInput stored => new(ContinuousWorkInputKind.Store, ids.EnergyStores[stored.Store],
                    default, default, 0, true),
                ElectricalAirflowInput electrical => new(ContinuousWorkInputKind.Electrical, default,
                    ids.Supply[electrical.Consumer].Node, default, 0, true),
                CoordinateAirflowInput coordinate => new(ContinuousWorkInputKind.Coordinate, default, default,
                    ids.Hinges[coordinate.Coordinate.Index].Joint, coordinate.Sense,
                    construction.Connections.Any(link => link.Domain == WorkshopConnectionDomain.Mechanical &&
                        link.Target == owner && link.Input == coordinate.Port)),
                _ => throw new ArgumentException("Unknown continuous-work input.")
            };
    }
}
