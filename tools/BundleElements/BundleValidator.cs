using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Fluid;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.BundleElements;

/// <summary>Why a package cannot enter the bundle. The number is the error's BUNDLE code.</summary>
public enum BundleErrorCode
{
    DuplicateId = 1,
    NonFinite = 2,
    UndefinedEnum = 3,
    BodyWithoutCollider = 4,
    DeclarationCount = 5,
    OutOfRange = 6,
    UnresolvedReference = 7
}

/// <summary>One generation failure, naming the package and the field.</summary>
public sealed record BundleError(BundleErrorCode Code, string Detail)
{
    public string Text => $"BUNDLE{(int)Code:000}";
    public override string ToString() => $"{Text} {Detail}";
}

/// <summary>One discovered package and its single declaration.</summary>
public sealed record BundlePackage(string Package, IPuzzleElementDeclaration Declaration);

/// <summary>Admits packages into the bundle: unique ids, finite f32 values, defined enum values, a collider for every body and every
/// value inside its declared range; a slider's parameter keys must name declared Real parameters.</summary>
public static class BundleValidator
{
    private const SimulationDomain AllDomains = SimulationDomain.Mechanical | SimulationDomain.Electrical | SimulationDomain.Signal |
        SimulationDomain.Fluid | SimulationDomain.Audio;

    public static IReadOnlyList<BundleError> Validate(IReadOnlyList<BundlePackage> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        var errors = new List<BundleError>();
        foreach (var duplicate in packages.GroupBy(package => package.Declaration.Metadata.Id, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1).OrderBy(group => group.Key, StringComparer.Ordinal))
            errors.Add(new(BundleErrorCode.DuplicateId,
                $"duplicate id '{duplicate.Key}' ({string.Join(", ", duplicate.Select(package => package.Package))})"));
        foreach (var package in packages) new PackageCheck(package.Package, errors).Check(package.Declaration);
        return errors;
    }

    private sealed class PackageCheck(string package, List<BundleError> errors)
    {
        public void Check(IPuzzleElementDeclaration declaration)
        {
            Metadata(declaration.Metadata);
            if ((declaration.SupportedDomains & ~AllDomains) != 0) Undefined("SupportedDomains");
            var footprint = declaration.Footprint;
            if (footprint.Bodies < 0) OutOfRange("Footprint.Bodies");
            if (footprint.MovingBodies < 0 || footprint.MovingBodies > footprint.Bodies) OutOfRange("Footprint.MovingBodies");
            if (footprint.PrismaticConstraints < 0) OutOfRange("Footprint.PrismaticConstraints");
            // Any package with a body (a declared footprint body, or a mechanical payload) needs a collider, whatever its domains.
            if ((footprint.Bodies > 0 || declaration is IMechanicalElement) && declaration.Colliders.Count == 0)
                errors.Add(new(BundleErrorCode.BodyWithoutCollider, $"{package}.Colliders: a body declares no collider"));
            for (var i = 0; i < declaration.Ports.Count; i++) Port($"Ports[{i}]", declaration.Ports[i]);
            for (var i = 0; i < declaration.Colliders.Count; i++) Collider($"Colliders[{i}]", declaration.Colliders[i]);
            for (var i = 0; i < declaration.Events.Count; i++)
                Defined($"Events[{i}].EventKind", declaration.Events[i].EventKind);
            if (declaration is IMechanicalElement mechanical) Mechanical(mechanical);
            if (declaration is IElectricalElement electrical) Electrical(electrical);
            if (declaration is ISignalElement signal) Signal(signal);
            if (declaration is IFluidElement fluid) Fluid(fluid);
        }

        private void Metadata(ElementMetadata metadata)
        {
            Defined("Metadata.Category", metadata.Category);
            for (var i = 0; i < metadata.Parameters.Count; i++) Parameter($"Metadata.Parameters[{i}]", metadata.Parameters[i]);
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

        private void Mechanical(IMechanicalElement mechanical)
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
            if (mechanical is ISliderElement slider)
                for (var i = 0; i < slider.Sliders.Count; i++) Slider($"Sliders[{i}]", slider.Sliders[i], slider.Metadata.Parameters);
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
                errors.Add(new(BundleErrorCode.BodyWithoutCollider, $"{package}.{path}.MovingBody.Colliders: a body declares no collider"));
        }

        /// <summary>A key must name one of the element's declared Real parameters.</summary>
        private void Real(string path, string key, IReadOnlyList<ParameterDescriptor> parameters)
        {
            if (!parameters.Any(parameter => parameter.Key == key && parameter.Kind == ParameterKind.Real))
                errors.Add(new(BundleErrorCode.UnresolvedReference, $"{package}.{path}: '{key}' names no declared Real parameter"));
        }

        private void Electrical(IElectricalElement electrical)
        {
            for (var i = 0; i < electrical.ElectricalNodes.Count; i++)
            {
                var node = electrical.ElectricalNodes[i];
                var path = $"ElectricalNodes[{i}]";
                Defined(path + ".Role", node.Role);
                Finite(path + ".RatedVoltage", node.RatedVoltage);
                Finite(path + ".Resistance", node.Resistance);
                Finite(path + ".Capacitance", node.Capacitance);
                Finite(path + ".MaxPower", node.MaxPower);
            }
        }

        private void Signal(ISignalElement signal)
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

        private void Fluid(IFluidElement fluid)
        {
            Finite("FluidProperties.BuoyancyCoefficient", fluid.FluidProperties.BuoyancyCoefficient);
            Finite("FluidProperties.Volume", fluid.FluidProperties.Volume);
            Finite("FluidProperties.FlowResistance", fluid.FluidProperties.FlowResistance);
            for (var i = 0; i < fluid.FluidPorts.Count; i++)
            {
                var port = fluid.FluidPorts[i];
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
            errors.Add(new(BundleErrorCode.UndefinedEnum, $"{package}.{path}: undefined enum value"));

        private void OutOfRange(string path) =>
            errors.Add(new(BundleErrorCode.OutOfRange, $"{package}.{path}: outside its declared range"));

        private bool Finite(string path, float value)
        {
            if (float.IsFinite(value)) return true;
            errors.Add(new(BundleErrorCode.NonFinite, $"{package}.{path}: not finite"));
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
