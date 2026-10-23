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
            Presentation(entry.Presentation, entry.Metadata.Parameters, entry.Ports, entry.Electrical, entry.Signal);
            // Story 9A.22: each declared event kind is a defined record kind (None is no record), declared once.
            var kinds = new HashSet<SimulationEventKind>();
            for (var i = 0; i < entry.Events.Count; i++)
            {
                if (entry.Events[i].Kind == SimulationEventKind.None) Undefined($"Events[{i}].Kind");
                else Defined($"Events[{i}].Kind", entry.Events[i].Kind);
                if (!kinds.Add(entry.Events[i].Kind)) OutOfRange($"Events[{i}].Kind");
            }
            if (entry.Mechanical is { } mechanical) Mechanical(mechanical, entry.Metadata.Parameters, entry.Ports);
            if (entry.Electrical is { } electrical) Electrical(electrical, entry.Ports, entry.Metadata.Parameters);
            // A run-time control is declared only on a Source store's enable state: the one value ConfigureElectrical changes.
            var runControlled = entry.Electrical?.Supply?.Store?.Enable;
            for (var i = 0; i < entry.Metadata.Parameters.Count; i++)
                if (entry.Metadata.Parameters[i].RunEdit == ParameterRunEdit.RunControl && entry.Metadata.Parameters[i].Key != runControlled)
                    OutOfRange($"Metadata.Parameters[{i}].RunEdit");
            if (entry.Signal is { } signal) Signal(signal, entry.Metadata.Parameters, entry.Ports, entry.Mechanical, entry.Electrical, entry.Colliders);
            if (entry.Fluid is { } fluid) Fluid(fluid);
        }

        private void Presentation(ElementPresentation value, IReadOnlyList<ParameterDescriptor> parameters, IReadOnlyList<PortDescriptor> ports, ElectricalPayload? electrical,
            SignalPayload? signal)
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
                // A committed oscillator binding reads the row only a declared oscillator compiles.
                if (binding.Driver is PresentationDriver.CommittedOscillatorPhase or PresentationDriver.CommittedOscillatorPulse &&
                    signal?.Oscillator is null)
                    OutOfRange("Presentation.Oscillator");
                // A committed count binding reads the memory row only a declared pulse count compiles.
                if (binding.Driver == PresentationDriver.CommittedCount && signal?.PulseCount is null)
                    OutOfRange("Presentation.PulseCount");
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
            // A cylinder is (r, h, r): its radial lanes are bound together or not at all, so every evaluation keeps X = Z.
            for (var i = 0; i < entry.Colliders.Count; i++)
            {
                if (entry.Colliders[i].ShapeKind != ColliderShapeKind.Cylinder) continue;
                var x = entry.ColliderBindings.Where(binding => binding.Collider == i && binding.Component == GeometryComponent.HalfExtentX)
                    .Select(binding => (binding.Parameter, binding.Multiplier, binding.Offset)).ToArray();
                var z = entry.ColliderBindings.Where(binding => binding.Collider == i && binding.Component == GeometryComponent.HalfExtentZ)
                    .Select(binding => (binding.Parameter, binding.Multiplier, binding.Offset)).ToArray();
                if (!x.SequenceEqual(z)) OutOfRange($"ColliderBindings[{i}].CylinderRadius");
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
            Defined(path + ".RunEdit", parameter.RunEdit);
            // A Choice is picked from its options; there is no typed value to clamp.
            if (parameter.Kind == ParameterKind.Choice && parameter.EditorPolicy != ParameterEditorPolicy.Reject) OutOfRange(path + ".EditorPolicy");
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

        private void Mechanical(MechanicalPayload mechanical, IReadOnlyList<ParameterDescriptor> parameters, IReadOnlyList<PortDescriptor> ports)
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
            // A supplied drive is enabled by the element's one PowerIn route, so it needs that input, and one route drives one slider.
            var driven = mechanical.Sliders.Select((slider, index) => (slider, index)).Where(pair => pair.slider.Drive is not null).ToArray();
            if (driven.Length > 1) OutOfRange($"Sliders[{driven[1].index}].Drive");
            if (driven.Length > 0 && !ports.Any(port => port.Socket == PortSocket.PowerIn && port.Domain == SimulationDomain.Electrical &&
                    port.Direction == PortDirection.Input))
                errors.Add(new(BundleErrorCode.UnresolvedReference,
                    $"{source}.Sliders[{driven[0].index}].Drive: a supplied drive requires an electrical PowerIn input"));
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
            if (mechanical.ContactWork is { } work)
            {
                // The runtime store's domain (ContactWorkDeclaration): target speed 0–20 m/s, preload at most 200 J, threshold 0–64 m/s,
                // cooldown 9–14400 physical steps, on one static owner body.
                Real("ContactWork.StrengthParameter", work.StrengthParameter, parameters);
                var strengths = parameters.Where(parameter => parameter.Key == work.StrengthParameter).ToArray();
                var referenceMass = Finite("ContactWork.ReferenceMass", work.ReferenceMass) && work.ReferenceMass > 0f && work.ReferenceMass <= 100f;
                if (!referenceMass) OutOfRange("ContactWork.ReferenceMass");
                if (strengths.Length > 1 || (strengths.Length == 1 && (strengths[0].Unit != ParameterUnit.MetrePerSecond ||
                    strengths[0].Minimum < 0f || strengths[0].Maximum > 20f ||
                    (referenceMass && .5f * work.ReferenceMass * strengths[0].Maximum * strengths[0].Maximum > 200f))))
                    OutOfRange("ContactWork.StrengthParameter");
                if (Finite("ContactWork.Threshold", work.Threshold) && (work.Threshold < 0f || work.Threshold > 64f))
                    OutOfRange("ContactWork.Threshold");
                if (work.CooldownPhysicalSteps is < 9 or > 14400) OutOfRange("ContactWork.CooldownPhysicalSteps");
                if (mechanical.KinematicMode != KinematicMode.Static || mechanical.Sliders.Count != 0)
                    OutOfRange("ContactWork.Owner");
            }
            if (mechanical.MassParameter is { } bound) MassParameter(bound, mechanical, parameters);
        }

        /// <summary>A body mass bound to a parameter: the dynamic body's mass is one declared Real kilogram parameter whose whole range
        /// lies inside the compiled mass domain [1/1024, 1024] kg, and the fixed declared mass is that parameter's default (one truth
        /// at defaults).</summary>
        private void MassParameter(MechanicalValue bound, MechanicalPayload mechanical, IReadOnlyList<ParameterDescriptor> parameters)
        {
            const string path = "MassParameter";
            Value(path, bound, parameters);
            if (bound.Source != MechanicalValueSource.Parameter) { OutOfRange(path + ".Source"); return; }
            if (mechanical.KinematicMode != KinematicMode.Dynamic) OutOfRange(path + ".Owner");
            var named = parameters.Where(parameter => parameter.Key == bound.Parameter).ToArray();
            if (named.Length != 1) { OutOfRange(path + ".Parameter"); return; }
            var mass = named[0];
            if (mass.Kind != ParameterKind.Real || mass.Unit != ParameterUnit.Kilogram) OutOfRange(path + ".Parameter");
            if (!(mass.Minimum >= 1f / 1024f && mass.Maximum <= 1024f)) OutOfRange(path + ".Range");
            if (BitConverter.SingleToUInt32Bits(mechanical.MassProperties.Mass) != BitConverter.SingleToUInt32Bits(mass.Default))
                OutOfRange(path + ".Default");
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
            if (slider.Drive is { } drive)
            {
                // The target lies within the travel; every bound is a finite positive f32 (a zero bound is a drive that cannot move).
                DriveValue(path + ".Drive.Target", drive.Target, parameters, slider.LowerLimit, slider.UpperLimit, positive: false);
                // 64 m/s: the game-grade envelope's linear speed bound.
                DriveValue(path + ".Drive.MaxSpeed", drive.MaxSpeed, parameters, 0f, 64f, positive: true);
                DriveValue(path + ".Drive.StoppingAcceleration", drive.StoppingAcceleration, parameters, 0f, float.MaxValue, positive: true);
                DriveValue(path + ".Drive.MaxEffort", drive.MaxEffort, parameters, 0f, float.MaxValue, positive: true);
                DriveValue(path + ".Drive.MaxPower", drive.MaxPower, parameters, 0f, float.MaxValue, positive: true);
            }
        }

        /// <summary>One drive value inside [minimum, maximum] (excluding a zero bound when <paramref name="positive"/>): a constant
        /// directly, a parameter by its whole declared range.</summary>
        private void DriveValue(string path, MechanicalValue value, IReadOnlyList<ParameterDescriptor> parameters, float minimum, float maximum, bool positive)
        {
            if (value.Source == MechanicalValueSource.Constant && value.Parameter is null && float.IsFinite(value.Constant))
            {
                if (value.Constant < minimum || value.Constant > maximum || (positive && value.Constant <= 0f)) OutOfRange(path);
                return;
            }
            Value(path, value, parameters);
            if (value.Source == MechanicalValueSource.Parameter &&
                parameters.SingleOrDefault(parameter => parameter.Key == value.Parameter && parameter.Kind == ParameterKind.Real) is { Key: not null } parameter &&
                (parameter.Minimum < minimum || parameter.Maximum > maximum || (positive && parameter.Minimum <= 0f)))
                OutOfRange(path);
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

        private void Electrical(ElectricalPayload electrical, IReadOnlyList<PortDescriptor> ports, IReadOnlyList<ParameterDescriptor> parameters)
        {
            if (electrical.Supply is { } supply)
            {
                if ((supply.Kind == SupplyCapabilityKind.Source) != supply.Store.HasValue) OutOfRange("Supply.Store");
                if (supply.Store is { } store) SourceStore(store, parameters);
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

        /// <summary>A Source's finite store names four distinct declared parameters of their own kind and unit, each range inside the
        /// electrical source envelope. The enable state lists exactly Disabled then Enabled, so its index 1 always means Enabled.</summary>
        private void SourceStore(SupplySourceStoreDescriptor store, IReadOnlyList<ParameterDescriptor> parameters)
        {
            string[] keys = [store.Capacity, store.PowerLimit, store.InitialCharge, store.Enable];
            if (keys.Distinct(StringComparer.Ordinal).Count() != keys.Length) OutOfRange("Supply.Store");
            StoreValue("Supply.Store.Capacity", store.Capacity, parameters, ParameterUnit.Joule,
                ElectricalSourceEnvelope.MinimumCapacity, ElectricalSourceEnvelope.MaximumCapacity);
            StoreValue("Supply.Store.PowerLimit", store.PowerLimit, parameters, ParameterUnit.Watt,
                ElectricalSourceEnvelope.MinimumPower, ElectricalSourceEnvelope.MaximumPower);
            StoreValue("Supply.Store.InitialCharge", store.InitialCharge, parameters, ParameterUnit.None, 0f, 1f);
            if (StoreParameter("Supply.Store.Enable", store.Enable, parameters) is { } enable &&
                (enable.Kind != ParameterKind.Choice || enable.Unit != ParameterUnit.None ||
                 !enable.Options.SequenceEqual(["Disabled", "Enabled"], StringComparer.Ordinal)))
                OutOfRange("Supply.Store.Enable");
        }

        private void StoreValue(string path, string key, IReadOnlyList<ParameterDescriptor> parameters, ParameterUnit unit,
            float minimum, float maximum)
        {
            if (StoreParameter(path, key, parameters) is { } parameter &&
                (parameter.Kind != ParameterKind.Real || parameter.Unit != unit || !(parameter.Minimum >= minimum) || !(parameter.Maximum <= maximum)))
                OutOfRange(path);
        }

        /// <summary>The one declared parameter a store key names; an absent key is unresolved and a duplicated one out of range.</summary>
        private ParameterDescriptor? StoreParameter(string path, string key, IReadOnlyList<ParameterDescriptor> parameters)
        {
            var matches = parameters.Where(parameter => parameter.Key == key).ToArray();
            if (matches.Length == 0)
            {
                errors.Add(new(BundleErrorCode.UnresolvedReference, $"{source}.{path}: '{key}' names no declared parameter"));
                return null;
            }
            if (matches.Length > 1) { OutOfRange(path); return null; }
            return matches[0];
        }

        private void Signal(SignalPayload signal, IReadOnlyList<ParameterDescriptor> parameters, IReadOnlyList<PortDescriptor> ports,
            MechanicalPayload? mechanical, ElectricalPayload? electrical, IReadOnlyList<ColliderDescriptor> colliders)
        {
            // One node per element: at most one capability selects it.
            if ((signal.Contact is null ? 0 : 1) + (signal.Timer is null ? 0 : 1) + (signal.Indicator is null ? 0 : 1) + (signal.Passage is null ? 0 : 1) +
                    (signal.Orientation is null ? 0 : 1) + (signal.ContactLoad is null ? 0 : 1) + (signal.Oscillator is null ? 0 : 1) +
                    (signal.PulseCount is null ? 0 : 1) + (signal.Memory is null ? 0 : 1) > 1)
                OutOfRange("Capability");
            if (signal.PulseCount is { } count)
            {
                // The target is a declared WholeNumber parameter without a unit whose admitted range lies inside the network's [1, 9].
                if (!parameters.Any(parameter => parameter.Key == count.TargetParameter && parameter.Kind == ParameterKind.WholeNumber &&
                        parameter.Unit == ParameterUnit.None && parameter.Minimum >= 1f && parameter.Maximum <= 9f))
                    OutOfRange("PulseCount.TargetParameter");
                // It counts Pulse inputs on its ActivationIn and emits a Pulse on its ActivationOut.
                if (!ports.Any(port => port.Socket == PortSocket.ActivationIn && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Input && port.Signal == PortSignalKind.Pulse) ||
                        !ports.Any(port => port.Socket == PortSocket.ActivationOut && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Output && port.Signal == PortSignalKind.Pulse))
                    OutOfRange("PulseCount.Ports");
                // The reached target closes a switched contact.
                if (electrical?.Supply?.Kind != SupplyCapabilityKind.SwitchedContact)
                    OutOfRange("PulseCount.Supply");
                if ((mechanical is { } frame && (frame.KinematicMode == KinematicMode.Dynamic || frame.Sliders.Count != 0)) || colliders.Count == 0)
                    OutOfRange("PulseCount.Body");
            }
            if (signal.Memory is { } memory)
            {
                // Set and Reset are the two distinct Pulse inputs the shared memory law reads (SetIn turns it On, ResetIn Off).
                bool PulseInput(PortSocket socket) => ports.Any(port => port.Socket == socket && port.Domain == SimulationDomain.Signal &&
                    port.Direction == PortDirection.Input && port.Signal == PortSignalKind.Pulse);
                if (!Enum.IsDefined(memory.Set) || !Enum.IsDefined(memory.Reset)) Undefined("Memory.Inputs");
                else if (memory.Set != PortSocket.SetIn || memory.Reset != PortSocket.ResetIn || !PulseInput(memory.Set) || !PulseInput(memory.Reset))
                    OutOfRange("Memory.Inputs");
                // Memory drives a Level ActivationOut.
                if (!ports.Any(port => port.Socket == PortSocket.ActivationOut && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Output && port.Signal == PortSignalKind.Level))
                    OutOfRange("Memory.Ports");
                // Memory On closes a switched contact.
                if (electrical?.Supply?.Kind != SupplyCapabilityKind.SwitchedContact)
                    OutOfRange("Memory.Supply");
                if ((mechanical is { } frame && (frame.KinematicMode == KinematicMode.Dynamic || frame.Sliders.Count != 0)) || colliders.Count == 0)
                    OutOfRange("Memory.Body");
            }
            if (signal.Oscillator is { } oscillator)
            {
                // The interval is a declared Real second parameter with a finite positive admitted minimum (never a zero-tick interval).
                if (!parameters.Any(parameter => parameter.Key == oscillator.IntervalParameter && parameter.Kind == ParameterKind.Real &&
                        parameter.Unit == ParameterUnit.Second && float.IsFinite(parameter.Minimum) && parameter.Minimum > 0f))
                    OutOfRange("Oscillator.IntervalParameter");
                // A source: a Pulse ActivationOut and no ActivationIn.
                if (!ports.Any(port => port.Socket == PortSocket.ActivationOut && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Output && port.Signal == PortSignalKind.Pulse) ||
                        ports.Any(port => port.Socket == PortSocket.ActivationIn))
                    OutOfRange("Oscillator.Ports");
                // Supplied through its PowerIn route into a Consumer node.
                if (!ports.Any(port => port.Socket == PortSocket.PowerIn && port.Domain == SimulationDomain.Electrical && port.Direction == PortDirection.Input) ||
                        electrical?.Nodes.Any(node => node.Role == ElectricalRole.Consumer) != true)
                    OutOfRange("Oscillator.Supply");
                // A Static frame without sliders and with a collider: each pulse names the frame's first collider (PartIdentities + 1).
                if ((mechanical is { } frame && (frame.KinematicMode != KinematicMode.Static || frame.Sliders.Count != 0)) || colliders.Count == 0)
                    OutOfRange("Oscillator.Body");
            }
            if (signal.ContactLoad is { } load)
            {
                // The minimum mass is a declared Real kilogram parameter with a positive admitted minimum.
                if (!parameters.Any(parameter => parameter.Key == load.MinimumMassParameter && parameter.Kind == ParameterKind.Real &&
                        parameter.Unit == ParameterUnit.Kilogram && parameter.Minimum > 0f))
                    OutOfRange("ContactLoad.MinimumMassParameter");
                // The face is one of the element's own baked Box colliders, unrotated in the frame.
                if (load.FaceCollider < 0 || load.FaceCollider >= colliders.Count || colliders[load.FaceCollider].ShapeKind != ColliderShapeKind.Box ||
                        colliders[load.FaceCollider].Rotation != Quaternion.Identity)
                    OutOfRange("ContactLoad.FaceCollider");
                // The compiled row's own admission (ContactLoadSensorDeclaration): a unit normal, an alignment in (0, 1], a gap in
                // [0, 0.01] m; the z margin shares the gap's range, and the hold is positive and at most 1 s.
                var normal = load.LocalNormal;
                if (Finite("ContactLoad.LocalNormal.x", normal.X) & Finite("ContactLoad.LocalNormal.y", normal.Y) & Finite("ContactLoad.LocalNormal.z", normal.Z) &&
                        !(MathF.Abs(normal.LengthSquared() - 1f) <= 1e-6f))
                    OutOfRange("ContactLoad.LocalNormal");
                if (Finite("ContactLoad.MinimumAlignment", load.MinimumAlignment) && !(load.MinimumAlignment > 0f && load.MinimumAlignment <= 1f))
                    OutOfRange("ContactLoad.MinimumAlignment");
                if (Finite("ContactLoad.ContactDistance", load.ContactDistance) && !(load.ContactDistance >= 0f && load.ContactDistance <= .01f))
                    OutOfRange("ContactLoad.ContactDistance");
                if (Finite("ContactLoad.DepthMargin", load.DepthMargin) && !(load.DepthMargin >= 0f && load.DepthMargin <= .01f))
                    OutOfRange("ContactLoad.DepthMargin");
                if (Finite("ContactLoad.ReleaseHoldSeconds", load.ReleaseHoldSeconds) && !(load.ReleaseHoldSeconds > 0f && load.ReleaseHoldSeconds <= 1f))
                    OutOfRange("ContactLoad.ReleaseHoldSeconds");
                if (mechanical is not { KinematicMode: KinematicMode.Static, Sliders.Count: 0 })
                    OutOfRange("ContactLoad.Body");
                if (!ports.Any(port => port.Socket == PortSocket.ActivationOut && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Output && port.Signal == PortSignalKind.Level))
                    OutOfRange("ContactLoad.Ports");
            }
            if (signal.Orientation is { } orientation)
            {
                // The compiled half-angle cosine admits 4° to 179° (OrientationThreshold.Degrees).
                if (Finite("Orientation.ThresholdDegrees", orientation.ThresholdDegrees) &&
                        !(orientation.ThresholdDegrees >= 4f && orientation.ThresholdDegrees <= 179f))
                    OutOfRange("Orientation.ThresholdDegrees");
                if (!ports.Any(port => port.Socket == PortSocket.ActivationOut && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Output && port.Signal == PortSignalKind.Pulse))
                    OutOfRange("Orientation.Ports");
                if (mechanical?.KinematicMode != KinematicMode.Dynamic)
                    OutOfRange("Orientation.Body");
            }
            if (signal.Indicator is { } indicator)
            {
                if (!Enum.IsDefined(indicator.Input)) Undefined("Indicator.Input");
                else if (!ports.Any(port => port.Socket == indicator.Input && port.Domain == SimulationDomain.Signal &&
                        port.Direction == PortDirection.Input && port.Signal == PortSignalKind.Level))
                    OutOfRange("Indicator.Input");
            }
            if (signal.Passage is { } passage)
            {
                if (Finite("Passage.ApertureRadius", passage.ApertureRadius) && !(passage.ApertureRadius >= .01f && passage.ApertureRadius <= 10f))
                    OutOfRange("Passage.ApertureRadius");
                if (Finite("Passage.RearmClearance", passage.RearmClearance) && !(passage.RearmClearance >= .001f && passage.RearmClearance <= 10f))
                    OutOfRange("Passage.RearmClearance");
                if (!ports.Any(port => port.Socket == PortSocket.ActivationOut && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Output && port.Signal == PortSignalKind.Pulse))
                    OutOfRange("Passage.Ports");
                if (mechanical?.KinematicMode != KinematicMode.Static)
                    OutOfRange("Passage.Frame");
            }
            for (var i = 0; i < signal.Triggers.Count; i++)
            {
                var trigger = signal.Triggers[i];
                Defined($"Triggers[{i}].Mode", trigger.Mode);
                Finite($"Triggers[{i}].Threshold", trigger.Threshold);
                Finite($"Triggers[{i}].PulseDurationSeconds", trigger.PulseDurationSeconds);
            }
            if (signal.Contact is { } contact)
            {
                Real("Contact.ThresholdParameter", contact.ThresholdParameter, parameters);
                if (!parameters.Any(parameter => parameter.Key == contact.ThresholdParameter && parameter.Unit == ParameterUnit.MetrePerSecond && parameter.Minimum >= 0f && parameter.Maximum <= 64f))
                    OutOfRange("Contact.ThresholdParameter");
                if (!ports.Any(port => port.Socket == PortSocket.ActivationOut && port.Domain == SimulationDomain.Signal && port.Direction == PortDirection.Output && port.Signal == PortSignalKind.Pulse))
                    OutOfRange("Contact.Ports");
                if (signal.Triggers.Count != 0 || signal.Timer is not null || signal.PulseCount is not null || signal.Memory is not null)
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
