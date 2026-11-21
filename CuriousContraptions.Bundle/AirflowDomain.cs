using System.Collections.Immutable;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

/// <summary>Admits exact airflow references and the entire authored scalar/choice domain before compilation.</summary>
internal static class AirflowDomain
{
    public static void Validate(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) return;
        ArgumentNullException.ThrowIfNull(mechanical.AirflowSources);
        ArgumentNullException.ThrowIfNull(mechanical.AirflowReceivers);
        var sources = new HashSet<LocalAirflowSourceId>();
        var senses = new Dictionary<CoordinateReference, float>();
        foreach (var source in mechanical.AirflowSources)
        {
            if (source is null || source.Id.Value == 0 || !sources.Add(source.Id))
                throw new ArgumentException("Duplicate or missing airflow source.");
            Source(entry, source, senses);
        }
        var airflowTargets = new HashSet<LocalAirflowReceiverId>();
        foreach (var receiver in mechanical.AirflowReceivers)
        {
            if (receiver is null || receiver.Id.Value == 0 || !airflowTargets.Add(receiver.Id))
                throw new ArgumentException("Duplicate or missing airflow receiver.");
            if (Collider(entry, receiver.Collider).Participation != CollisionParticipation.Enabled)
                throw new ArgumentException("Airflow receiver requires an active collider.");
            if (receiver.Collider.Body.Value == 0 && mechanical.KinematicMode == KinematicMode.Static)
                throw new ArgumentException("Airflow receiver requires a dynamic body.");
            ArgumentNullException.ThrowIfNull(receiver.Samples);
            new AirflowReceiverDeclaration(new(1), new(1), new(1), receiver.Sampling,
                receiver.Samples.ToImmutableArray()).Validate();
        }
    }

    private static void Source(BundleEntry entry, AirflowSourceDescriptor source,
        Dictionary<CoordinateReference, float> senses)
    {
        if (source.Frame.Value < 0 || source.Frame.Value >= entry.Footprint.Bodies)
            throw new ArgumentException("Unresolved airflow frame.");
        ArgumentNullException.ThrowIfNull(source.InternalOccluders);
        var exclusions = new HashSet<LocalColliderReference>();
        foreach (var excluded in source.InternalOccluders)
        {
            var collider = Collider(entry, excluded);
            if (!exclusions.Add(excluded) || collider.Participation != CollisionParticipation.Enabled)
                throw new ArgumentException("Duplicate or inactive internal occluder.");
        }
        Scalar(entry, source.Reach, ParameterUnit.Metre, true);
        Scalar(entry, source.TerminalRadius, ParameterUnit.Metre, true);
        Scalar(entry, source.FlowSpeed, ParameterUnit.MetrePerSecond, false);
        Scalar(entry, source.MaximumForce, ParameterUnit.Newton, false);
        Scalar(entry, source.MaximumPower, ParameterUnit.Watt, false);
        if (!Finite(source.Origin) || !Finite(source.Axis) || MathF.Abs(source.Axis.LengthSquared() - 1f) > 1e-5f)
            throw new ArgumentException("Invalid airflow local frame.");
        Inputs(entry, source, senses);
    }

    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    internal static ColliderDescriptor Collider(BundleEntry entry, LocalColliderReference reference)
    {
        if (reference.Body.Value < 0 || reference.Body.Value >= entry.Footprint.Bodies)
            throw new ArgumentException("Unresolved airflow collider body.");
        var colliders = BundleMaterial.Shapes(entry, reference.Body);
        if (reference.Collider.Value < 0 || reference.Collider.Value >= colliders.Count)
            throw new ArgumentException("Unresolved airflow collider.");
        return colliders[reference.Collider.Value];
    }

    internal static void Scalar(BundleEntry entry, MechanicalValue value, ParameterUnit unit, bool positive)
    {
        if (!MechanicalValueDomain.TryRange(value, entry.Metadata.Parameters, unit, out var minimum, out _) ||
            (positive ? minimum <= 0 : minimum < 0))
            throw new ArgumentException("Airflow scalar leaves its dimensioned domain.");
        IEnumerable<string> keys = value.Product is { } product
            ? new[] { product.Left.Parameter, product.Right.Parameter }.OfType<string>()
            : value.Parameter is { } parameter ? [parameter] : [];
        foreach (var key in keys)
            if (!entry.Metadata.Parameters.Any(p => p.Key == key && p.RunEdit == ParameterRunEdit.BuildOnly))
                throw new ArgumentException("Airflow geometry and ratings are BuildOnly.");
    }

    private static void Inputs(BundleEntry entry, AirflowSourceDescriptor source,
        Dictionary<CoordinateReference, float> senses)
    {
        ArgumentNullException.ThrowIfNull(source.Inputs);
        if (source.Inputs.Count == 0) throw new ArgumentException("Missing airflow work input.");
        var selections = new HashSet<ChoicePredicate>();
        ParameterId? choice = null;
        foreach (var input in source.Inputs)
        {
            if (input is null) throw new ArgumentException("Null airflow input.");
            if (input.Selection is { } selection)
            {
                var parameter = ChoiceDomain.Parameter(entry, selection);
                if (!selections.Add(selection) || choice.HasValue && choice.Value != selection.Parameter)
                    throw new ArgumentException("Ambiguous airflow input selection.");
                choice = selection.Parameter;
                if (source.Inputs.Count != parameter.Options.Count)
                    throw new ArgumentException("Airflow inputs must cover every choice exactly once.");
            }
            else if (source.Inputs.Count != 1)
                throw new ArgumentException("Unconditional airflow input must be unique.");
            Enable(entry, input.Enable);
            Input(entry, source.Id, input, senses);
        }
    }

    private static void Enable(BundleEntry entry, AirflowEnableDescriptor? binding)
    {
        if (binding is not { } enable) return;
        if (!(entry.Signal?.Nodes.Any(n => n.Id == enable.Latch &&
                n is CuriousContraptions.Abstractions.Signal.SignalMemoryDescriptor) ?? false))
            throw new ArgumentException("Airflow enable requires an exact memory node.");
        var parameters = entry.Metadata.Parameters.Where(p => p.Key == enable.InitialState.Value).ToArray();
        if (parameters.Length != 1 || parameters[0].Kind != ParameterKind.Choice || parameters[0].Unit != ParameterUnit.None ||
            parameters[0].Minimum != 0 || parameters[0].Maximum != 1 || parameters[0].RunEdit != ParameterRunEdit.BuildOnly)
            throw new ArgumentException("Initial enable requires a binary BuildOnly choice.");
    }

    private static void Input(BundleEntry entry, LocalAirflowSourceId source, AirflowInputDescriptor input,
        Dictionary<CoordinateReference, float> senses)
    {
        switch (input)
        {
            case StoredAirflowInput stored:
                if (stored.Store.Value == 0 || !entry.Mechanical!.EnergyStores.Any(s => s.Id == stored.Store))
                    throw new ArgumentException("Unresolved airflow store.");
                break;
            case ElectricalAirflowInput electrical:
                var nodes = entry.Electrical?.SupplyGraph?.Nodes ?? [];
                if (!nodes.OfType<SupplyConsumerDescriptor>().Any(n => n.Id == electrical.Consumer &&
                        n.Targets.Contains(new SupplyContinuousWorkTarget(source))))
                    throw new ArgumentException("Unresolved exact continuous-work consumer.");
                break;
            case CoordinateAirflowInput coordinate:
                Coordinate(entry, coordinate, senses);
                break;
            default: throw new ArgumentException("Unsupported airflow input.");
        }
    }

    private static void Coordinate(BundleEntry entry, CoordinateAirflowInput input,
        Dictionary<CoordinateReference, float> senses)
    {
        if (input.Coordinate.Kind != CoordinateKind.Revolute || input.Coordinate.Index < 0 ||
            input.Coordinate.Index >= entry.Mechanical!.Hinges.Count || (input.Sense != -1 && input.Sense != 1))
            throw new ArgumentException("Unresolved signed airflow coordinate.");
        if (!entry.Ports.Any(p => p.Id == input.Port && p.Domain == SimulationDomain.Mechanical &&
                p.Direction == PortDirection.Input && p.Coordinate == input.Coordinate))
            throw new ArgumentException("Airflow coordinate needs its exact mechanical input port.");
        if (senses.TryGetValue(input.Coordinate, out var prior) && prior != input.Sense)
            throw new ArgumentException("Conflicting work-input senses on one coordinate.");
        senses[input.Coordinate] = input.Sense;
    }
}

internal static class ChoiceDomain
{
    public static ParameterDescriptor Parameter(BundleEntry entry, ChoicePredicate selection)
    {
        var matches = entry.Metadata.Parameters.Where(p => p.Key == selection.Parameter.Value).ToArray();
        if (matches.Length != 1) throw new ArgumentException("Unresolved choice predicate.");
        var value = matches[0];
        if (value.Kind != ParameterKind.Choice || value.RunEdit != ParameterRunEdit.BuildOnly ||
            selection.Option < 0 || selection.Option >= value.Options.Count)
            throw new ArgumentException("Invalid BuildOnly choice predicate.");
        return value;
    }
}
