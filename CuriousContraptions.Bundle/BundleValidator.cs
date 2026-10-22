using System.Numerics;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;
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
    EmptyText = 10,
    /// <summary>A scene's <c>Loc_Port_</c> locator names neither a declared socket nor a tube mouth (Story 9A.17).</summary>
    UndeclaredPortLocator = 11,
    /// <summary>A declared port has no <c>Loc_Port_&lt;socket&gt;</c> locator in its element's scene.</summary>
    MissingPortLocator = 12,
    /// <summary>A port's locator is not at the declared anchor, is not a child of the scene root, or has no readable transform.</summary>
    PortLocatorDisagrees = 13,
    /// <summary>One element declares the same socket twice.</summary>
    DuplicatePort = 14,
    /// <summary>A Signal port without a signal kind, or a non-Signal port with one.</summary>
    PortSignalKind = 15
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
            var sockets = new HashSet<PortSocket>();
            for (var i = 0; i < entry.Ports.Count; i++)
            {
                Port($"Ports[{i}]", entry.Ports[i]);
                if (!sockets.Add(entry.Ports[i].Socket))
                    errors.Add(new(BundleErrorCode.DuplicatePort, $"{source}.Ports[{i}].Socket: {entry.Ports[i].Socket} is declared more than once"));
            }
            for (var i = 0; i < entry.Colliders.Count; i++) Collider($"Colliders[{i}]", entry.Colliders[i]);
            Geometry(entry);
            Presentation(entry.Presentation, entry.Metadata.Parameters, entry.Ports, entry.Electrical);
            for (var i = 0; i < entry.Events.Count; i++) Defined($"Events[{i}].EventKind", entry.Events[i].EventKind);
            if (entry.Mechanical is { } mechanical) Mechanical(mechanical, entry.Metadata.Parameters);
            if (entry.Electrical is { } electrical) Electrical(electrical, entry.Ports);
            if (entry.Signal is { } signal) Signal(signal, entry.Metadata.Parameters, entry.Ports);
            if (entry.Fluid is { } fluid) Fluid(fluid);
        }

        private void Presentation(ElementPresentation value, IReadOnlyList<ParameterDescriptor> parameters, IReadOnlyList<PortDescriptor> ports, ElectricalPayload? electrical)
        {
            if (!Finite("Presentation.PickRadius", value.PickRadius) || value.PickRadius <= 0)
                OutOfRange("Presentation.PickRadius");
            Defined("Presentation.ResizeAxes", value.ResizeAxes);
            if ((value.ResizeAxes == PresentationAxes.None) != (value.Sizing is null)) OutOfRange("Presentation.Sizing");
            if (value.Sizing is { } sizing)
            {
                Finite("Presentation.Sizing.Dimensions", sizing.Dimensions);
                Finite("Presentation.Sizing.RadiusScale", sizing.RadiusScale);
                Finite("Presentation.Sizing.RadiusOffset", sizing.RadiusOffset);
                if (sizing.Dimensions.X <= 0 || sizing.Dimensions.Y <= 0 || sizing.Dimensions.Z <= 0 ||
                    (sizing.Dimensions * sizing.RadiusScale + sizing.RadiusOffset).Length() <= 0) OutOfRange("Presentation.Sizing");
                var axes = PresentationAxes.None;
                foreach (var resize in sizing.Parameters)
                {
                    if (resize.Axis is not (PresentationAxes.X or PresentationAxes.Y or PresentationAxes.Z) ||
                        (axes & resize.Axis) != 0 || !parameters.Any(p => p.Key == resize.Parameter))
                        OutOfRange("Presentation.Sizing.Parameter");
                    axes |= resize.Axis;
                }
                if (axes != value.ResizeAxes) OutOfRange("Presentation.Sizing.Axes");
            }
            var writers = new HashSet<(string, PresentationProperty, PresentationTimerPhase)>();
            foreach (var binding in value.Bindings)
            {
                Text("Presentation.Node", binding.Node);
                Defined("Presentation.Property", binding.Property);
                Defined("Presentation.Driver", binding.Driver);
                Defined("Presentation.Curve", binding.Curve);
                Defined("Presentation.Overlap", binding.Overlap);
                Defined("Presentation.Phase", binding.Phase);
                Finite("Presentation.From", binding.From); Finite("Presentation.To", binding.To);
                Finite("Presentation.Duration", binding.Duration); Finite("Presentation.Threshold", binding.Threshold);
                Finite("Presentation.Response", binding.Response);
                if (binding.Intermediate is { } middle) Finite("Presentation.Intermediate", middle);
                if (binding.Duration < 0 || binding.Duration > 30 || binding.Response < 0 ||
                    (binding.Curve == PresentationCurve.ExponentialFollow && binding.Response <= 0))
                    OutOfRange("Presentation.Timing");
                bool AdmitsValue(float input) => float.IsFinite(input) && (binding.Property switch
                {
                    PresentationProperty.Visible => input is 0f or 1f,
                    PresentationProperty.UniformScale or PresentationProperty.ScaleX or PresentationProperty.ScaleY or PresentationProperty.ScaleZ => input > 0,
                    PresentationProperty.AlbedoRed or PresentationProperty.AlbedoGreen or PresentationProperty.AlbedoBlue or
                        PresentationProperty.EmissionRed or PresentationProperty.EmissionGreen or PresentationProperty.EmissionBlue => input is >= 0 and <= 1,
                    _ => true
                });
                if (!AdmitsValue(binding.From) || !AdmitsValue(binding.To) || (binding.Intermediate is { } intermediate && !AdmitsValue(intermediate)))
                    OutOfRange("Presentation.Value");
                var supported = binding.Driver switch
                {
                    PresentationDriver.ActivationPulse or PresentationDriver.CapturePulse or PresentationDriver.CommittedActivation or
                        PresentationDriver.CommittedCount or PresentationDriver.CommittedElectricalSocket => binding.Curve == PresentationCurve.SmoothStep && binding.Duration > 0 && binding.Overlap == PresentationOverlap.Replace,
                    PresentationDriver.ContactImpulse => binding.Curve == PresentationCurve.SineSquaredPulse && binding.Duration > 0 && binding.Overlap == PresentationOverlap.SaturatingSum,
                    PresentationDriver.CommittedOscillatorPhase => binding.Curve == PresentationCurve.ExponentialFollow && binding.Duration == 0 && binding.Overlap == PresentationOverlap.Replace,
                    PresentationDriver.CommittedOscillatorPulse => binding.Curve == PresentationCurve.LinearDecay && binding.Duration > 0 && binding.Overlap == PresentationOverlap.Maximum,
                    PresentationDriver.CommittedContactLoadPhase => binding.Curve == PresentationCurve.CubicSmoothStep && binding.Duration > 0 && binding.Overlap == PresentationOverlap.Replace,
                    PresentationDriver.CommittedTimerActive => binding.Overlap == PresentationOverlap.Replace &&
                        ((binding.Curve == PresentationCurve.Linear && binding.Duration == 0) || (binding.Curve == PresentationCurve.SmoothStep && binding.Duration > 0)),
                    PresentationDriver.CommittedTimerRemaining or PresentationDriver.CommittedTimerProgress or PresentationDriver.CommittedTimerPhase or PresentationDriver.CommittedSupply or
                        PresentationDriver.CommittedCharge or PresentationDriver.CommittedActuatorDrive or PresentationDriver.CommittedActuatorState =>
                            binding.Curve == PresentationCurve.Linear && binding.Duration == 0 && binding.Overlap == PresentationOverlap.Replace,
                    _ => false
                };
                if ((binding.Driver == PresentationDriver.CommittedElectricalSocket) != binding.Socket.HasValue ||
                    (binding.Socket is { } socket &&
                        (electrical?.Supply?.Kind != SupplyCapabilityKind.LogicGate ||
                         !ports.Any(port => port.Socket == socket && port.Domain == SimulationDomain.Electrical))))
                    OutOfRange("Presentation.Socket");
                if (binding.Socket is { } boundSocket) Defined("Presentation.Socket", boundSocket);
                if (!supported || (binding.Curve != PresentationCurve.ExponentialFollow && binding.Response != 0) ||
                    (binding.Driver == PresentationDriver.CommittedTimerPhase && binding.Phase == PresentationTimerPhase.None))
                    OutOfRange("Presentation.DriverCurve");
                if (!writers.Add((binding.Node, binding.Property, binding.Phase)))
                    errors.Add(new(BundleErrorCode.UnresolvedReference, $"{source}.Presentation: duplicate property writer"));
                if (binding.Phase != PresentationTimerPhase.None && binding.Driver != PresentationDriver.CommittedTimerPhase)
                    OutOfRange("Presentation.Phase");
            }
            var bodies = new HashSet<PresentationBody>();
            foreach (var body in value.Bodies)
            {
                Text("Presentation.Body.Node", body.Node); Defined("Presentation.Body", body.Body);
                Finite("Presentation.Body.RestOffset", body.RestOffset);
                if (!bodies.Add(body.Body)) OutOfRange("Presentation.Body");
            }
            foreach (var span in value.Spans)
            {
                Text("Presentation.Span.MovingNode", span.MovingNode); Text("Presentation.Span.Node", span.SpanNode);
                Finite("Presentation.Span.MovingAnchor", span.MovingAnchor); Finite("Presentation.Span.FixedAnchor", span.FixedAnchor);
                if (!Finite("Presentation.Span.RestLength", span.RestLength) || span.RestLength <= 0)
                    OutOfRange("Presentation.Span.RestLength");
            }
            foreach (var parameter in value.Parameters)
            {
                if (!parameters.Any(p => p.Key == parameter.Parameter)) OutOfRange("Presentation.Parameter.Key");
                Text("Presentation.Parameter.Node", parameter.Node); Text("Presentation.Parameter", parameter.Parameter);
                Defined("Presentation.Parameter.Property", parameter.Property);
                Finite("Presentation.Parameter.Multiplier", parameter.Multiplier);
                Finite("Presentation.Parameter.Offset", parameter.Offset);
            }
        }

        private void Geometry(BundleEntry entry)
        {
            var targets = new HashSet<(int, GeometryComponent)>();
            foreach (var binding in entry.ColliderBindings)
            {
                const string path = "ColliderBindings";
                Defined(path + ".Component", binding.Component);
                if (binding.Collider < 0 || binding.Collider >= entry.Colliders.Count) OutOfRange(path + ".Collider");
                if (!targets.Add((binding.Collider, binding.Component))) OutOfRange(path + ".DuplicateTarget");
                Finite(path + ".Multiplier", binding.Multiplier);
                Finite(path + ".Offset", binding.Offset);
                Real(path + ".Parameter", binding.Parameter, entry.Metadata.Parameters);
                var parameter = entry.Metadata.Parameters.FirstOrDefault(p => p.Key == binding.Parameter);
                foreach (var value in new[] { parameter.Minimum, parameter.Maximum })
                {
                    var evaluated = value * binding.Multiplier + binding.Offset;
                    if (!float.IsFinite(evaluated) || (binding.Component is GeometryComponent.HalfExtentX or
                        GeometryComponent.HalfExtentY or GeometryComponent.HalfExtentZ && evaluated <= 0f))
                        OutOfRange(path + ".EvaluatedBoundary");
                }
            }
            var ends = new HashSet<TubeEnd>();
            foreach (var mouth in entry.TubeMouths)
            {
                Defined("TubeMouths.End", mouth.End);
                if (!ends.Add(mouth.End)) OutOfRange("TubeMouths.DuplicateEnd");
                Finite("TubeMouths.Position", mouth.Position);
                Finite("TubeMouths.Rotation", mouth.Rotation);
                if (MathF.Abs(mouth.Rotation.LengthSquared() - 1f) > 1e-5f) OutOfRange("TubeMouths.Rotation");
                if (Finite("TubeMouths.BoreRadius", mouth.BoreRadius) && mouth.BoreRadius <= 0f) OutOfRange("TubeMouths.BoreRadius");
            }
            var boundEnds = new HashSet<TubeEnd>();
            foreach (var binding in entry.TubeMouthBindings)
            {
                Defined("TubeMouthBindings.End", binding.End);
                if (!ends.Contains(binding.End) || !boundEnds.Add(binding.End)) OutOfRange("TubeMouthBindings.End");
                Finite("TubeMouthBindings.Multiplier", binding.Multiplier);
                Finite("TubeMouthBindings.Offset", binding.Offset);
                Real("TubeMouthBindings.Parameter", binding.Parameter, entry.Metadata.Parameters);
                var parameter = entry.Metadata.Parameters.FirstOrDefault(p => p.Key == binding.Parameter);
                foreach (var value in new[] { parameter.Minimum, parameter.Maximum })
                    Finite("TubeMouthBindings.EvaluatedBoundary", value * binding.Multiplier + binding.Offset);
            }
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
            Defined(path + ".EditorPolicy", parameter.EditorPolicy);
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
            Defined(path + ".Socket", port.Socket);
            Defined(path + ".Domain", port.Domain);
            Defined(path + ".Direction", port.Direction);
            if (port.Signal is { } signal) Defined(path + ".Signal", signal);
            if ((port.Domain == SimulationDomain.Signal) != port.Signal.HasValue)
                errors.Add(new(BundleErrorCode.PortSignalKind, $"{source}.{path}.Signal: a Signal port declares its signal kind and no other port does"));
            Finite(path + ".LocalPosition", port.LocalPosition);
            Finite(path + ".Normal", port.Normal);
            // A port normal is a direction: unit length within f32 rounding.
            var length = port.Normal.LengthSquared();
            if (float.IsFinite(length) && MathF.Abs(length - 1f) > 1e-5f) OutOfRange(path + ".Normal");
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
            if (mechanical.Capture is { } capture)
            {
                Volume("Capture",capture.Minimum,capture.Maximum);
                CaptureValue("Capture.UpperMargin",capture.UpperMargin,parameters,ParameterUnit.Metre,0f,1f);
                CaptureValue("Capture.SpeedLimit",capture.SpeedLimit,parameters,ParameterUnit.MetrePerSecond,1f/1024f,64f);
                CaptureValue("Capture.Dwell",capture.Dwell,parameters,ParameterUnit.Second,1f/1024f,30f);
                var participation=parameters.SingleOrDefault(parameter=>parameter.Key==capture.ParticipationParameter);
                if (string.IsNullOrWhiteSpace(capture.ParticipationParameter) || participation.Options is null ||
                    participation.Key!=capture.ParticipationParameter || participation.Kind!=ParameterKind.Choice || participation.Minimum!=0f ||
                    participation.Maximum!=1f || participation.Options.Count!=2 ||
                    participation.Options[0]!="Disabled" || participation.Options[1]!="Enabled")
                    OutOfRange("Capture.ParticipationParameter");
                var guide=capture.Guide;
                Volume("Capture.Guide",guide.Minimum,guide.Maximum);
                if (Finite("Capture.Guide.SupportHeight",guide.SupportHeight) && MathF.Abs(guide.SupportHeight)>16f)
                    OutOfRange("Capture.Guide.SupportHeight");
                CaptureValue("Capture.Guide.SupportMargin",guide.SupportMargin,parameters,ParameterUnit.Metre,0f,1f);
                CaptureValue("Capture.Guide.MaximumAcceleration",guide.MaximumAcceleration,parameters,ParameterUnit.MetrePerSecondSquared,0f,12f);
            }
        }

        private void Volume(string path,Vector3 minimum,Vector3 maximum)
        {
            Finite(path+".Minimum",minimum);Finite(path+".Maximum",maximum);
            if (minimum.X>=maximum.X || minimum.Y>=maximum.Y || minimum.Z>=maximum.Z ||
                Vector3.Abs(minimum).X>16f || Vector3.Abs(minimum).Y>16f || Vector3.Abs(minimum).Z>16f ||
                Vector3.Abs(maximum).X>16f || Vector3.Abs(maximum).Y>16f || Vector3.Abs(maximum).Z>16f)
                OutOfRange(path);
        }

        private void CaptureValue(string path,MechanicalValue value,IReadOnlyList<ParameterDescriptor> parameters,
            ParameterUnit unit,float minimum,float maximum)
        {
            Value(path,value,parameters);
            if(value.Source==MechanicalValueSource.Constant)
            {
                if(value.Constant<minimum || value.Constant>maximum) OutOfRange(path);
            }
            else if(parameters.SingleOrDefault(parameter=>parameter.Key==value.Parameter) is { } parameter &&
                (parameter.Unit!=unit || parameter.Minimum<minimum || parameter.Maximum>maximum))
                OutOfRange(path);
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
            Value(path + ".Stiffness", slider.Stiffness, parameters);
            Value(path + ".Damping", slider.Damping, parameters);
            Value(path + ".Compression", slider.Compression, parameters);
            var body = slider.MovingBody;
            if (Finite(path + ".MovingBody.Mass", body.Mass) && body.Mass <= 0f) OutOfRange(path + ".MovingBody.Mass");
            Finite(path + ".MovingBody.Gravity", body.Gravity);
            Finite(path + ".MovingBody.LinearDrag", body.LinearDrag);
            for (var i = 0; i < body.Colliders.Count; i++) Collider($"{path}.MovingBody.Colliders[{i}]", body.Colliders[i]);
            if (body.Colliders.Count == 0)
                errors.Add(new(BundleErrorCode.BodyWithoutCollider, $"{source}.{path}.MovingBody.Colliders: a body declares no collider"));
        }

        private void Value(string path, MechanicalValue value, IReadOnlyList<ParameterDescriptor> parameters)
        {
            Defined(path + ".Source", value.Source);
            Finite(path + ".Constant", value.Constant);
            if (value.Source == MechanicalValueSource.Parameter)
            {
                if (value.Constant != 0f) OutOfRange(path + ".Constant");
                Real(path + ".Parameter", value.Parameter ?? "", parameters);
            }
            else if (value.Parameter is not null || value.Constant < 0f) OutOfRange(path);
        }

        /// <summary>A key must name one of the element's declared Real parameters.</summary>
        private void Real(string path, string key, IReadOnlyList<ParameterDescriptor> parameters)
        {
            if (!parameters.Any(parameter => parameter.Key == key && parameter.Kind == ParameterKind.Real))
                errors.Add(new(BundleErrorCode.UnresolvedReference, $"{source}.{path}: '{key}' names no declared Real parameter"));
        }

        private void Electrical(ElectricalPayload electrical, IReadOnlyList<PortDescriptor> ports)
        {
            if (electrical.Supply is { } supply)
            {
                Defined("Supply.Kind", supply.Kind);
                Defined("Supply.Output", supply.Output);
                if (supply.Input is { } input) Defined("Supply.Input", input);
                var role = supply.Kind switch
                {
                    SupplyCapabilityKind.Source => ElectricalRole.Source,
                    SupplyCapabilityKind.SwitchedContact => ElectricalRole.Switch,
                    SupplyCapabilityKind.LogicGate => ElectricalRole.LogicGate,
                    _ => (ElectricalRole)byte.MaxValue
                };
                if (supply.Operation is { } operation) Defined("Supply.Operation", operation);
                if ((supply.Kind == SupplyCapabilityKind.LogicGate) != supply.Operation.HasValue ||
                    (supply.Kind == SupplyCapabilityKind.LogicGate &&
                        (!ports.Any(port => port.Socket == PortSocket.FirstIn && port.Domain == SimulationDomain.Electrical && port.Direction == PortDirection.Input) ||
                         !ports.Any(port => port.Socket == PortSocket.SecondIn && port.Domain == SimulationDomain.Electrical && port.Direction == PortDirection.Input))))
                    OutOfRange("Supply.Operation");
                if (!electrical.Nodes.Any(node => node.Role == role) ||
                    supply.Output != PortSocket.Supply ||
                    (supply.Kind == SupplyCapabilityKind.Source ? supply.Input is not null : supply.Input != PortSocket.PowerIn) ||
                    !ports.Any(port => port.Socket == supply.Output && port.Domain == SimulationDomain.Electrical && port.Direction == PortDirection.Output) ||
                    (supply.Input is { } incoming && !ports.Any(port => port.Socket == incoming && port.Domain == SimulationDomain.Electrical && port.Direction == PortDirection.Input)))
                    OutOfRange("Supply");
            }
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

        private void Signal(SignalPayload signal, IReadOnlyList<ParameterDescriptor> parameters, IReadOnlyList<PortDescriptor> ports)
        {
            for (var i = 0; i < signal.Triggers.Count; i++)
            {
                var trigger = signal.Triggers[i];
                Defined($"Triggers[{i}].Mode", trigger.Mode);
                Finite($"Triggers[{i}].Threshold", trigger.Threshold);
                Finite($"Triggers[{i}].PulseDurationSeconds", trigger.PulseDurationSeconds);
            }
            Defined("LatchMode", signal.LatchMode);
            if (signal.Contact is { } contact)
            {
                Real("Contact.ThresholdParameter", contact.ThresholdParameter, parameters);
                if (!parameters.Any(parameter => parameter.Key == contact.ThresholdParameter && parameter.Unit == ParameterUnit.MetrePerSecond && parameter.Minimum >= 0f && parameter.Maximum <= 64f))
                    OutOfRange("Contact.ThresholdParameter");
                if (!ports.Any(port => port.Socket == PortSocket.ActivationOut && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Output && port.Signal == PortSignalKind.Pulse))
                    OutOfRange("Contact.Ports");
                if (signal.Triggers.Count != 0 || signal.Timer is not null || signal.Counter is not null || signal.LatchMode != SignalLatchMode.None)
                    OutOfRange("Contact.Ownership");
            }
            if (signal.Timer is { } timer)
            {
                Real("Timer.DurationParameter", timer.DurationParameter, parameters);
                Defined("Timer.Completion", timer.Completion);
                Defined("Timer.Boundary", timer.Boundary);
                Defined("Timer.ElapsedSignal", timer.ElapsedSignal);
                if ((timer.Completion == TimerCompletionPolicy.Rearm && (timer.ElapsedSignal != TimerElapsedSignal.None || timer.Boundary != TimerBoundary.BeforeNetworks)) ||
                    (timer.Completion == TimerCompletionPolicy.Latch && (timer.ElapsedSignal != TimerElapsedSignal.Activation || timer.Boundary != TimerBoundary.BeforePhysics)))
                    OutOfRange("Timer.Policy");
                if (!ports.Any(port => port.Socket == PortSocket.ActivationIn && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Input && port.Signal == PortSignalKind.Pulse) ||
                    (timer.ElapsedSignal == TimerElapsedSignal.Activation && !ports.Any(port => port.Socket == PortSocket.ActivationOut && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Output && port.Signal == PortSignalKind.Pulse)))
                    OutOfRange("Timer.Ports");
                if (!parameters.Any(parameter => parameter.Key == timer.DurationParameter && parameter.Unit == ParameterUnit.Second && parameter.Minimum > 0f))
                    OutOfRange("Timer.DurationParameter");
            }
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
