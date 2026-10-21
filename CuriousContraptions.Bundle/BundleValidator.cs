using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Bundle;

/// <summary>Why an entry cannot enter, or be loaded from, the bundle. The number is the error's BUNDLE code.</summary>
public enum BundleErrorCode
{
    DuplicateId = 1,
    NonFinite = 2,
    UndefinedEnum = 3,
    BodyWithoutCollider = 4,
    DeclarationCount = 5,
    OutOfRange = 6,
    UnresolvedReference = 7,
    DuplicatePaletteOrder = 8,
    MissingAsset = 9,
    EmptyText = 10
}

/// <summary>One validation failure, naming the entry's source (its package, or its element id in a loaded bundle) and the field.</summary>
public sealed record BundleError(BundleErrorCode Code, string Detail)
{
    public string Text => $"BUNDLE{(int)Code:000}";
    public override string ToString() => $"{Text} {Detail}";
}

/// <summary>One bundle entry and the name its errors report.</summary>
public sealed record NamedEntry(string Source, BundleEntry Entry);

/// <summary>Admits bundle entries, at generation and again at load: unique ids and palette orders, non-empty text, res:// asset paths,
/// finite f32 values, defined enum values, a collider for every body and every value inside its declared range; a slider's parameter
/// keys must name declared Real parameters.</summary>
public static class BundleValidator
{
    private const SimulationDomain AllDomains = SimulationDomain.Mechanical | SimulationDomain.Electrical | SimulationDomain.Signal |
        SimulationDomain.Fluid | SimulationDomain.Audio;

    public static IReadOnlyList<BundleError> Validate(IReadOnlyList<NamedEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var errors = new List<BundleError>();
        foreach (var duplicate in entries.GroupBy(entry => entry.Entry.Metadata.Id, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1).OrderBy(group => group.Key, StringComparer.Ordinal))
            errors.Add(new(BundleErrorCode.DuplicateId,
                $"duplicate id '{duplicate.Key}' ({string.Join(", ", duplicate.Select(entry => entry.Source))})"));
        foreach (var duplicate in entries.GroupBy(entry => entry.Entry.Metadata.PaletteOrder)
                     .Where(group => group.Count() > 1).OrderBy(group => group.Key))
            errors.Add(new(BundleErrorCode.DuplicatePaletteOrder,
                $"duplicate palette order {duplicate.Key} ({string.Join(", ", duplicate.Select(entry => entry.Source))})"));
        foreach (var entry in entries) new EntryCheck(entry.Source, errors).Check(entry.Entry);
        return errors;
    }

    private sealed class EntryCheck(string source, List<BundleError> errors)
    {
        public void Check(BundleEntry entry)
        {
            Metadata(entry.Metadata);
            foreach (var domain in entry.Domains)
                if (domain == SimulationDomain.None || (domain & ~AllDomains) != 0 || !BitOperations.IsPow2((uint)domain))
                    Undefined("SupportedDomains");
            var footprint = entry.Footprint;
            if (footprint.Bodies < 0) OutOfRange("Footprint.Bodies");
            if (footprint.MovingBodies < 0 || footprint.MovingBodies > footprint.Bodies) OutOfRange("Footprint.MovingBodies");
            if (footprint.PrismaticConstraints < 0) OutOfRange("Footprint.PrismaticConstraints");
            // Any entry with a body (a declared footprint body, or a mechanical payload) needs a collider, whatever its domains.
            if ((footprint.Bodies > 0 || entry.Mechanical is not null) && entry.Colliders.Count == 0)
                errors.Add(new(BundleErrorCode.BodyWithoutCollider, $"{source}.Colliders: a body declares no collider"));
            for (var i = 0; i < entry.Ports.Count; i++) Port($"Ports[{i}]", entry.Ports[i]);
            for (var i = 0; i < entry.Colliders.Count; i++) Collider($"Colliders[{i}]", entry.Colliders[i]);
            for (var i = 0; i < entry.Events.Count; i++) Defined($"Events[{i}].EventKind", entry.Events[i].EventKind);
            if (entry.Mechanical is { } mechanical) Mechanical(mechanical, entry.Metadata.Parameters);
            if (entry.Electrical is { } electrical) Electrical(electrical);
            if (entry.Signal is { } signal) Signal(signal);
            if (entry.Fluid is { } fluid) Fluid(fluid);
        }

        private void Metadata(ElementMetadata metadata)
        {
            Text("Metadata.Id", metadata.Id);
            Text("Metadata.DisplayName", metadata.DisplayName);
            Text("Metadata.Description", metadata.Description);
            Defined("Metadata.Category", metadata.Category);
            Channel("Metadata.Color.R", metadata.Color.R);
            Channel("Metadata.Color.G", metadata.Color.G);
            Channel("Metadata.Color.B", metadata.Color.B);
            Channel("Metadata.Color.A", metadata.Color.A);
            if (metadata.PaletteOrder <= 0) OutOfRange("Metadata.PaletteOrder");
            Resource("Metadata.IconPath", metadata.IconPath);
            Resource("Metadata.ScenePath", metadata.ScenePath);
            for (var i = 0; i < metadata.Parameters.Count; i++) Parameter($"Metadata.Parameters[{i}]", metadata.Parameters[i]);
        }

        private void Channel(string path, float value)
        {
            if (Finite(path, value) && (value < 0f || value > 1f)) OutOfRange(path);
        }

        private void Text(string path, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() != value)
                errors.Add(new(BundleErrorCode.EmptyText, $"{source}.{path}: empty or padded text"));
        }

        private void Resource(string path, string value)
        {
            if (!value.StartsWith("res://elements/", StringComparison.Ordinal) || value.Contains("..", StringComparison.Ordinal) ||
                value.Contains('\\', StringComparison.Ordinal))
                errors.Add(new(BundleErrorCode.UnresolvedReference, $"{source}.{path}: '{value}' is not a package asset path"));
        }

        private void Parameter(string path, ParameterDescriptor parameter)
        {
            Defined(path + ".Unit", parameter.Unit);
            Defined(path + ".Kind", parameter.Kind);
            var finite = Finite(path + ".Minimum", parameter.Minimum) & Finite(path + ".Maximum", parameter.Maximum) &
                Finite(path + ".Default", parameter.Default);
            if (!finite) return;
            if (parameter.Minimum > parameter.Maximum) OutOfRange(path + ".Maximum");
            else if (parameter.Default < parameter.Minimum || parameter.Default > parameter.Maximum) OutOfRange(path + ".Default");
            var whole = parameter.Kind is ParameterKind.WholeNumber or ParameterKind.Boolean or ParameterKind.Choice;
            if (whole && (!float.IsInteger(parameter.Minimum) || !float.IsInteger(parameter.Maximum) || !float.IsInteger(parameter.Default)))
                OutOfRange(path + ".Default");
            if (parameter.Kind == ParameterKind.Boolean && (parameter.Minimum != 0f || parameter.Maximum != 1f)) OutOfRange(path + ".Maximum");
            if (parameter.Kind == ParameterKind.Choice
                    ? parameter.Options.Count == 0 || parameter.Minimum != 0f || parameter.Maximum != parameter.Options.Count - 1
                    : parameter.Options.Count != 0)
                OutOfRange(path + ".Options");
        }

        private void Port(string path, PortDescriptor port)
        {
            Defined(path + ".Domain", port.Domain);
            Defined(path + ".Direction", port.Direction);
            if (port.Signal is { } signal) Defined(path + ".Signal", signal);
            Finite(path + ".LocalPosition", port.LocalPosition);
            Finite(path + ".Normal", port.Normal);
            // A port normal is a direction: unit length within f32 rounding.
            var length = port.Normal.LengthSquared();
            if (float.IsFinite(length) && MathF.Abs(length - 1f) > 1e-5f) OutOfRange(path + ".Normal");
            Text(path + ".PortId", port.PortId);
        }

        private void Collider(string path, ColliderDescriptor collider)
        {
            Defined(path + ".ShapeKind", collider.ShapeKind);
            Finite(path + ".HalfExtents", collider.HalfExtents);
            Finite(path + ".Offset", collider.Offset);
            Finite(path + ".Rotation", collider.Rotation);
            Finite(path + ".Material.Restitution", collider.Material.Restitution);
            Finite(path + ".Material.Friction", collider.Material.Friction);
            Finite(path + ".Material.RollingResistance", collider.Material.RollingResistance);
            Finite(path + ".Material.BounceThreshold", collider.Material.BounceThreshold);
        }

        private void Mechanical(MechanicalPayload mechanical, IReadOnlyList<ParameterDescriptor> parameters)
        {
            Defined("KinematicMode", mechanical.KinematicMode);
            var mass = mechanical.MassProperties;
            Finite("MassProperties.Mass", mass.Mass);
            Finite("MassProperties.CentreOfMass", mass.CentreOfMass);
            Finite("MassProperties.InertiaTensor", mass.InertiaTensor);
            Finite("MassProperties.LinearDrag", mass.LinearDrag);
            Finite("MassProperties.Gravity", mass.Gravity);
            for (var i = 0; i < mechanical.AnchorPoints.Count; i++)
            {
                var anchor = mechanical.AnchorPoints[i];
                Finite($"AnchorPoints[{i}].LocalPosition", anchor.LocalPosition);
                Finite($"AnchorPoints[{i}].LocalRotation", anchor.LocalRotation);
                Finite($"AnchorPoints[{i}].Axis", anchor.Axis);
            }
            for (var i = 0; i < mechanical.Sliders.Count; i++) Slider($"Sliders[{i}]", mechanical.Sliders[i], parameters);
        }

        private void Slider(string path, PrismaticSliderDescriptor slider, IReadOnlyList<ParameterDescriptor> parameters)
        {
            Finite(path + ".BaseFrame.Position", slider.BaseFrame.Position);
            Finite(path + ".BaseFrame.Rotation", slider.BaseFrame.Rotation);
            Finite(path + ".MovingFrame.Position", slider.MovingFrame.Position);
            Finite(path + ".MovingFrame.Rotation", slider.MovingFrame.Rotation);
            if (Finite(path + ".LowerLimit", slider.LowerLimit) & Finite(path + ".UpperLimit", slider.UpperLimit))
            {
                if (slider.LowerLimit > 0f) OutOfRange(path + ".LowerLimit");
                else if (slider.UpperLimit < 0f || slider.UpperLimit <= slider.LowerLimit) OutOfRange(path + ".UpperLimit");
            }
            Real(path + ".StiffnessParameter", slider.StiffnessParameter, parameters);
            Real(path + ".DampingParameter", slider.DampingParameter, parameters);
            Real(path + ".CompressionParameter", slider.CompressionParameter, parameters);
            var body = slider.MovingBody;
            if (Finite(path + ".MovingBody.Mass", body.Mass) && body.Mass <= 0f) OutOfRange(path + ".MovingBody.Mass");
            Finite(path + ".MovingBody.Gravity", body.Gravity);
            Finite(path + ".MovingBody.LinearDrag", body.LinearDrag);
            for (var i = 0; i < body.Colliders.Count; i++) Collider($"{path}.MovingBody.Colliders[{i}]", body.Colliders[i]);
            if (body.Colliders.Count == 0)
                errors.Add(new(BundleErrorCode.BodyWithoutCollider, $"{source}.{path}.MovingBody.Colliders: a body declares no collider"));
        }

        /// <summary>A key must name one of the element's declared Real parameters.</summary>
        private void Real(string path, string key, IReadOnlyList<ParameterDescriptor> parameters)
        {
            if (!parameters.Any(parameter => parameter.Key == key && parameter.Kind == ParameterKind.Real))
                errors.Add(new(BundleErrorCode.UnresolvedReference, $"{source}.{path}: '{key}' names no declared Real parameter"));
        }

        private void Electrical(ElectricalPayload electrical)
        {
            for (var i = 0; i < electrical.Nodes.Count; i++)
            {
                var node = electrical.Nodes[i];
                var path = $"ElectricalNodes[{i}]";
                Defined(path + ".Role", node.Role);
                Finite(path + ".RatedVoltage", node.RatedVoltage);
                Finite(path + ".Resistance", node.Resistance);
                Finite(path + ".Capacitance", node.Capacitance);
                Finite(path + ".MaxPower", node.MaxPower);
            }
        }

        private void Signal(SignalPayload signal)
        {
            for (var i = 0; i < signal.Triggers.Count; i++)
            {
                var trigger = signal.Triggers[i];
                Defined($"Triggers[{i}].Mode", trigger.Mode);
                Finite($"Triggers[{i}].Threshold", trigger.Threshold);
                Finite($"Triggers[{i}].PulseDurationSeconds", trigger.PulseDurationSeconds);
            }
            Defined("LatchMode", signal.LatchMode);
        }

        private void Fluid(FluidPayload fluid)
        {
            Finite("FluidProperties.BuoyancyCoefficient", fluid.Properties.BuoyancyCoefficient);
            Finite("FluidProperties.Volume", fluid.Properties.Volume);
            Finite("FluidProperties.FlowResistance", fluid.Properties.FlowResistance);
            for (var i = 0; i < fluid.Ports.Count; i++)
            {
                var port = fluid.Ports[i];
                Defined($"FluidPorts[{i}].Role", port.Role);
                Finite($"FluidPorts[{i}].MaxFlowRate", port.MaxFlowRate);
                Finite($"FluidPorts[{i}].OperatingPressure", port.OperatingPressure);
            }
        }

        private void Defined<TEnum>(string path, TEnum value) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value)) Undefined(path);
        }

        private void Undefined(string path) =>
            errors.Add(new(BundleErrorCode.UndefinedEnum, $"{source}.{path}: undefined enum value"));

        private void OutOfRange(string path) =>
            errors.Add(new(BundleErrorCode.OutOfRange, $"{source}.{path}: outside its declared range"));

        private bool Finite(string path, float value)
        {
            if (float.IsFinite(value)) return true;
            errors.Add(new(BundleErrorCode.NonFinite, $"{source}.{path}: not finite"));
            return false;
        }

        private void Finite(string path, Vector3 value)
        {
            Finite(path + ".x", value.X);
            Finite(path + ".y", value.Y);
            Finite(path + ".z", value.Z);
        }

        private void Finite(string path, Quaternion value)
        {
            Finite(path + ".x", value.X);
            Finite(path + ".y", value.Y);
            Finite(path + ".z", value.Z);
            Finite(path + ".w", value.W);
        }
    }
}
