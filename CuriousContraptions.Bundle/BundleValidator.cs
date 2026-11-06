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
    /// <summary>A port's locator is not at the declared anchor, is not a direct child of its owning body's scene root, or has no
    /// readable transform.</summary>
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
        SimulationDomain.Fluid | SimulationDomain.Audio | SimulationDomain.Rope;

    public static IReadOnlyList<BundleError> Validate(IReadOnlyList<NamedEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var errors = new List<BundleError>(4);
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
            CheckPhysicalOwnership(entry);
            var childBodies = (entry.Mechanical?.Sliders.Count ?? 0) + (entry.Mechanical?.Hinges.Count ?? 0);
            var portsSeen = new HashSet<PortId>(4);
            for (var i = 0; i < entry.Ports.Count; i++)
            {
                Port($"Ports[{i}]", entry.Ports[i], entry.Metadata.Parameters, childBodies, entry.Mechanical);
                if (!portsSeen.Add(entry.Ports[i].Id))
                    errors.Add(new(BundleErrorCode.DuplicatePort, $"{source}.Ports[{i}].Id: {entry.Ports[i].Id} is declared more than once"));
            }
            for (var i = 0; i < entry.Colliders.Count; i++) Collider($"Colliders[{i}]", entry.Colliders[i]);
            Geometry(entry);
            errors.AddRange(DrivenSurfaceValidation.Validate(entry));
            Presentation(entry.Presentation, entry.Metadata.Parameters, entry.Ports, entry.Mechanical, entry.Electrical, entry.Signal, entry.Events, childBodies);
            CheckEvents(entry.Events);
            if (entry.Mechanical is { } mechanical) Mechanical(mechanical, entry.Metadata.Parameters, entry.Ports, entry.Electrical?.Supply);
            if (entry.Electrical is { } electrical) Electrical(electrical, entry.Ports, entry.Metadata.Parameters);
            CheckRunControls(entry);
            errors.AddRange(SignalBundleValidation.Validate(entry));
            if (entry.Fluid is { } fluid) Fluid(fluid);
        }

        private void Presentation(ElementPresentation value, IReadOnlyList<ParameterDescriptor> parameters, IReadOnlyList<PortDescriptor> ports,
            MechanicalPayload? mechanical, ElectricalPayload? electrical, SignalPayload? signal, IReadOnlyList<EventDescriptor> events, int childBodies)
        {
            PresentationSizing(value, parameters);
            var writers = new HashSet<(string, PresentationProperty, PresentationTimerPhase)>(4);
            foreach (var binding in value.Bindings)
            {
                PresentationBindingIdentity(binding, signal);
                PresentationBindingValues(binding);
                PresentationBindingSocket(binding, electrical, ports);
                PresentationBindingCapabilities(binding, signal, mechanical);
                PresentationBindingEvents(binding, events);
                if (!writers.Add((binding.Node, binding.Property, binding.Phase)))
                    errors.Add(new(BundleErrorCode.UnresolvedReference, $"{source}.Presentation: duplicate property writer"));
                if (binding.Phase != PresentationTimerPhase.None && binding.Driver != PresentationDriver.CommittedTimerPhase)
                    OutOfRange("Presentation.Phase");
            }
            PresentationBodies(value, childBodies);
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

        private void CheckPhysicalOwnership(BundleEntry entry)
        {
            var footprint = entry.Footprint;
            if (footprint.Bodies < 0) OutOfRange("Footprint.Bodies");
            if (footprint.MovingBodies < 0 || footprint.MovingBodies > footprint.Bodies) OutOfRange("Footprint.MovingBodies");
            if (footprint.PrismaticConstraints < 0) OutOfRange("Footprint.PrismaticConstraints");
            if (footprint.RevoluteConstraints < 0) OutOfRange("Footprint.RevoluteConstraints");
            // Any entry with a body (a declared footprint body, or a mechanical payload) needs a collider, whatever its domains.
            if ((footprint.Bodies > 0 || entry.Mechanical is not null) && entry.Colliders.Count == 0)
                errors.Add(new(BundleErrorCode.BodyWithoutCollider, $"{source}.Colliders: a body declares no collider"));
            if (footprint.Bodies == 0 && (entry.Mechanical is not null || entry.Colliders.Count != 0 ||
                entry.ColliderBindings.Count != 0 || entry.Presentation.Bodies.Count != 0 ||
                footprint.MovingBodies != 0 || footprint.PrismaticConstraints != 0 || footprint.RevoluteConstraints != 0))
                OutOfRange("Footprint.PhysicalOwnership");
        }

        private void CheckEvents(IReadOnlyList<EventDescriptor> events)
        {
            // Story 9A.22: each declared event kind is a defined record kind (None is no record), declared once.
            var kinds = new HashSet<(SimulationEventKind, SignalNodeId?)>(4);
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Kind == SimulationEventKind.None) Undefined($"Events[{i}].Kind");
                else Defined($"Events[{i}].Kind", events[i].Kind);
                if (!kinds.Add((events[i].Kind, events[i].SignalNode))) OutOfRange($"Events[{i}].Kind");
            }
        }

        private void CheckRunControls(BundleEntry entry)
        {
            // A run-time control is declared only on a parameter bound to exactly one run-controllable lane: the lane its compiled
            // run-control row drives. A drive-target control also requires exactly one driven hinge on its part, so the owner's drive
            // slot is unique.
            for (var i = 0; i < entry.Metadata.Parameters.Count; i++)
            {
                if (entry.Metadata.Parameters[i].RunEdit != ParameterRunEdit.RunControl) continue;
                var lanes = RunControlBindings.Lanes(entry, entry.Metadata.Parameters[i].Key);
                if (lanes.Count != 1 || (lanes[0] == RunControlLane.RevoluteDriveTarget &&
                        entry.Mechanical?.Hinges.Count(hinge => hinge.Drive is not null) != 1))
                    OutOfRange($"Metadata.Parameters[{i}].RunEdit");
            }
        }

        private void PresentationSizing(ElementPresentation value, IReadOnlyList<ParameterDescriptor> parameters)
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
        }

        private void PresentationBindingIdentity(PresentationBinding binding, SignalPayload? signal)
        {
                Text("Presentation.Node", binding.Node);
                Defined("Presentation.Property", binding.Property);
                Defined("Presentation.Driver", binding.Driver);
                if (!PresentationDriverRules.SupportsMapping(binding.Driver, binding.Mapping)) OutOfRange("Presentation.Mapping");
                if (PresentationDriverRules.SignalSource(binding.Driver, binding.Event) != binding.SignalNode.HasValue ||
                    binding.SignalNode is { } selected && signal?.Nodes?.Any(node => node?.Id == selected) != true)
                    OutOfRange("Presentation.SignalNode");
                Defined("Presentation.Curve", binding.Curve);
                Defined("Presentation.Overlap", binding.Overlap);
                Defined("Presentation.Phase", binding.Phase);
        }

        private void PresentationBindingValues(PresentationBinding binding)
        {
                Finite("Presentation.From", binding.From); Finite("Presentation.To", binding.To);
                Finite("Presentation.Duration", binding.Duration); Finite("Presentation.Threshold", binding.Threshold);
                Finite("Presentation.Response", binding.Response);
                if ((binding.Driver == PresentationDriver.CommittedActuatorState) != binding.StateValues.HasValue ||
                    binding.StateValues is { } stateValues && (!stateValues.IsFinite() || binding.Intermediate.HasValue))
                    OutOfRange("Presentation.StateValues");
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
        }

        private void PresentationBindingSocket(PresentationBinding binding, ElectricalPayload? electrical, IReadOnlyList<PortDescriptor> ports)
        {
                if ((binding.Driver == PresentationDriver.CommittedElectricalSocket) != binding.Socket.HasValue ||
                    (binding.Socket is { } socket &&
                        (electrical?.Supply?.Kind != SupplyCapabilityKind.LogicGate ||
                         !ports.Any(port => port.Id == socket && port.Domain == SimulationDomain.Electrical))))
                    OutOfRange("Presentation.Socket");
                if ((binding.Driver == PresentationDriver.CommittedWheelAngle) != binding.GuidePort.HasValue ||
                    binding.GuidePort is { } guide && !ports.Any(port => port.Id == guide &&
                        port.Domain == SimulationDomain.Rope && port.AttachmentRole == RopeAttachmentRole.Guide))
                    OutOfRange("Presentation.GuidePort");
        }

        private void PresentationBindingCapabilities(PresentationBinding binding, SignalPayload? signal, MechanicalPayload? mechanical)
        {
                // A committed oscillator binding reads the row only a declared oscillator compiles.
                if (binding.Driver is PresentationDriver.CommittedOscillatorPhase or PresentationDriver.CommittedOscillatorPulse &&
                    signal?.Nodes?.Any(node => node is SignalOscillatorDescriptor && node.Id == binding.SignalNode) != true)
                    OutOfRange("Presentation.Oscillator");
                // A committed count binding reads the memory row only a declared pulse count compiles.
                if (binding.Driver == PresentationDriver.CommittedCount && signal?.Nodes?.Any(node => node is SignalPulseCountDescriptor && node.Id == binding.SignalNode) != true)
                    OutOfRange("Presentation.PulseCount");
                // A committed deflection binding reads the row only a declared compliant surface compiles.
                if (binding.Driver == PresentationDriver.CommittedSurfaceDeflection && mechanical?.CompliantSurface is null)
                    OutOfRange("Presentation.CompliantSurface");
                var actuatorDriver = binding.Driver is PresentationDriver.CommittedActuatorDrive or PresentationDriver.CommittedActuatorState;
                if (actuatorDriver != binding.Actuator.HasValue) OutOfRange("Presentation.Actuator");
                if (binding.Actuator is { } actuator)
                {
                    var valid = actuator.Index >= 0 && (actuator.Kind switch
                    {
                        CoordinateKind.Prismatic => mechanical is not null && actuator.Index < mechanical.Sliders.Count &&
                            mechanical.Sliders[actuator.Index].Drive is not null,
                        CoordinateKind.Revolute => mechanical is not null && actuator.Index < mechanical.Hinges.Count &&
                            mechanical.Hinges[actuator.Index].Drive is not null,
                        _ => false
                    });
                    if (!valid) OutOfRange("Presentation.Actuator");
                }
        }

        private void PresentationBindingEvents(PresentationBinding binding, IReadOnlyList<EventDescriptor> events)
        {
                if (!PresentationDriverRules.Supports(binding.Driver, binding.Curve, binding.Duration, binding.Overlap) || (binding.Curve != PresentationCurve.ExponentialFollow && binding.Response != 0) ||
                    (binding.Driver == PresentationDriver.CommittedTimerPhase && binding.Phase == PresentationTimerPhase.None))
                    OutOfRange("Presentation.DriverCurve");
                // Story 9A.25b: exactly an event-driven binding names a record kind, and only a kind its package declares it produces.
                if ((binding.Driver == PresentationDriver.Event) != (binding.Event != SimulationEventKind.None)) OutOfRange("Presentation.Event");
                Defined("Presentation.EventMagnitude", binding.EventMagnitude);
                if (binding.Driver != PresentationDriver.Event && binding.EventMagnitude != PresentationEventMagnitude.Any)
                    OutOfRange("Presentation.EventMagnitude");
                if (binding.Event != SimulationEventKind.None)
                {
                    Defined("Presentation.Event", binding.Event);
                    if (!events.Any(declared => declared.Kind == binding.Event && declared.SignalNode == binding.SignalNode)) OutOfRange("Presentation.Event");
                }
        }

        private void PresentationBodies(ElementPresentation value, int childBodies)
        {
            var bodies = new HashSet<PackageBodyIndex>(4);
            var bodyNodes = new HashSet<string>(4, StringComparer.Ordinal);
            foreach (var body in value.Bodies)
            {
                Text("Presentation.Body.Node", body.Node);
                if (body.Body.Value < 1 || body.Body.Value > childBodies || !bodyNodes.Add(body.Node)) OutOfRange("Presentation.Body");
                Finite("Presentation.Body.RestOffset", body.RestOffset);
                if (!bodies.Add(body.Body)) OutOfRange("Presentation.Body");
            }
            if (bodies.Count != childBodies) OutOfRange("Presentation.Body.Missing");
        }

        private void MechanicalPort(string path, PortDescriptor port, MechanicalPayload? mechanical)
        {
            if (port.Domain == SimulationDomain.Mechanical)
            {
                if (port.Role is not (PortRole.Drive or PortRole.DriveIn))
                    OutOfRange(path + ".Role");
                if (port.Role == PortRole.Drive && port.Direction != PortDirection.Output)
                    OutOfRange(path + ".Direction");
                if (port.Role == PortRole.DriveIn && port.Direction != PortDirection.Input)
                    OutOfRange(path + ".Direction");
                if (port.Coordinate is not { } coordinate)
                {
                    OutOfRange(path + ".Coordinate");
                }
                else
                {
                    if (coordinate.Kind != CoordinateKind.Revolute)
                        OutOfRange(path + ".Coordinate.Kind");
                    if (coordinate.Index < 0 || coordinate.Index >= (mechanical?.Hinges.Count ?? 0))
                        OutOfRange(path + ".Coordinate.Index");
                }
                if (!float.IsFinite(port.CoordinatePerRadian) || port.CoordinatePerRadian == 0f)
                    OutOfRange(path + ".CoordinatePerRadian");
            }
            else
            {
                if (port.Role is PortRole.Drive or PortRole.DriveIn)
                    OutOfRange(path + ".Role");
                if (port.Coordinate.HasValue)
                    OutOfRange(path + ".Coordinate");
                if (port.CoordinatePerRadian != 0f)
                    OutOfRange(path + ".CoordinatePerRadian");
            }
        }

        private void Geometry(BundleEntry entry)
        {
            var targets = new HashSet<(int, GeometryComponent)>(4);
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
                    if (!float.IsFinite(evaluated) || (binding.Component is GeometryComponent.Radius or GeometryComponent.HalfExtentX or
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
            var ends = new HashSet<TubeEnd>(4);
            foreach (var mouth in entry.TubeMouths)
            {
                Defined("TubeMouths.End", mouth.End);
                if (!ends.Add(mouth.End)) OutOfRange("TubeMouths.DuplicateEnd");
                Finite("TubeMouths.Position", mouth.Position);
                Finite("TubeMouths.Rotation", mouth.Rotation);
                if (MathF.Abs(mouth.Rotation.LengthSquared() - 1f) > 1e-5f) OutOfRange("TubeMouths.Rotation");
                if (Finite("TubeMouths.BoreRadius", mouth.BoreRadius) && mouth.BoreRadius <= 0f) OutOfRange("TubeMouths.BoreRadius");
            }
            var boundEnds = new HashSet<TubeEnd>(4);
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

        private void Port(string path, PortDescriptor port, IReadOnlyList<ParameterDescriptor> parameters, int childBodies, MechanicalPayload? mechanical)
        {
            Text(path + ".Id", port.Id.Value);
            Defined(path + ".Role", port.Role);
            Defined(path + ".Domain", port.Domain);
            Defined(path + ".Direction", port.Direction);
            if (port.Signal is { } signal) Defined(path + ".Signal", signal);
            if ((port.Domain == SimulationDomain.Signal) != port.Signal.HasValue)
                errors.Add(new(BundleErrorCode.PortSignalKind, $"{source}.{path}.Signal: a Signal port declares its signal kind and no other port does"));
            Finite(path + ".LocalPosition", port.LocalPosition);
            Finite(path + ".Normal", port.Normal);
            Defined(path + ".AttachmentRole", port.AttachmentRole);
            if (port.Domain == SimulationDomain.Rope)
            {
                if (port.Role is not (PortRole.Tie or PortRole.TieA or PortRole.TieB) || port.Direction != PortDirection.Bidirectional ||
                    port.AttachmentRole == RopeAttachmentRole.None) OutOfRange(path + ".AttachmentRole");
            }
            else if (port.AttachmentRole != RopeAttachmentRole.None || port.Role is PortRole.Tie or PortRole.TieA or PortRole.TieB)
                OutOfRange(path + ".AttachmentRole");
            MechanicalPort(path, port, mechanical);
            if (port.AttachmentRole == RopeAttachmentRole.Guide)
            {
                if (!float.IsFinite(port.GuideRadius) || port.GuideRadius <= 0f)
                    OutOfRange(path + ".GuideRadius");
                Finite(path + ".GuideOffset", port.GuideOffset);
            }
            else
            {
                if (port.GuideRadius != 0f) OutOfRange(path + ".GuideRadius");
                if (port.GuideOffset != Vector3.Zero) OutOfRange(path + ".GuideOffset");
            }
            if (port.Body.Value < 0 || port.Body.Value > childBodies)
                OutOfRange(path + ".Body");
            if (port.PositionBinding is { } binding)
            {
                Finite(path + ".PositionBinding.Multiplier", binding.Multiplier);
                Finite(path + ".PositionBinding.Offset", binding.Offset);
                var matches = parameters.Where(p => new ParameterId(p.Key) == binding.Parameter).ToArray();
                if (matches.Length != 1 || matches[0].Kind != ParameterKind.Real)
                    OutOfRange(path + ".PositionBinding.Parameter");
                else
                {
                    var parameter = matches[0];
                    Finite(path + ".PositionBinding.Minimum", binding.Evaluate(parameter.Minimum));
                    Finite(path + ".PositionBinding.Maximum", binding.Evaluate(parameter.Maximum));
                    if (binding.Evaluate(parameter.Default) != port.LocalPosition)
                        OutOfRange(path + ".PositionBinding.Default");
                }
            }
            // A port normal is a direction: unit length within f32 rounding.
            var length = port.Normal.LengthSquared();
            if (float.IsFinite(length) && MathF.Abs(length - 1f) > 1e-5f) OutOfRange(path + ".Normal");
        }

        private void Collider(string path, ColliderDescriptor collider)
        {
            Defined(path + ".ShapeKind", collider.ShapeKind);
            Finite(path + ".HalfExtents", collider.HalfExtents);
            Finite(path + ".Radius", collider.Radius);
            // A solid kind keeps its radius in its half-extents; only the hollow kinds use the radius lane.
            if (float.IsFinite(collider.Radius) && !ColliderDescriptor.Hollow(collider.ShapeKind) && collider.Radius != 0f)
                OutOfRange(path + ".Radius");
            Finite(path + ".Offset", collider.Offset);
            Finite(path + ".Rotation", collider.Rotation);
            Finite(path + ".Material.Restitution", collider.Material.Restitution);
            Finite(path + ".Material.Friction", collider.Material.Friction);
            Finite(path + ".Material.RollingResistance", collider.Material.RollingResistance);
            Finite(path + ".Material.BounceThreshold", collider.Material.BounceThreshold);
        }

        private void Mechanical(MechanicalPayload mechanical, IReadOnlyList<ParameterDescriptor> parameters, IReadOnlyList<PortDescriptor> ports,
            SupplyCapabilityDescriptor? supply)
        {
            Defined("KinematicMode", mechanical.KinematicMode);
            Defined("Payload", mechanical.Payload);
            if (mechanical.Payload != PayloadPolicy.None && mechanical.KinematicMode != KinematicMode.Dynamic)
                OutOfRange("Payload.Owner");
            MassFrame("MassFrame", mechanical.MassFrame, mechanical.KinematicMode, parameters);
            for (var i = 0; i < mechanical.AnchorPoints.Count; i++)
            {
                var anchor = mechanical.AnchorPoints[i];
                Finite($"AnchorPoints[{i}].LocalPosition", anchor.LocalPosition);
                Finite($"AnchorPoints[{i}].LocalRotation", anchor.LocalRotation);
                Finite($"AnchorPoints[{i}].Axis", anchor.Axis);
            }
            for (var i = 0; i < mechanical.Sliders.Count; i++) Slider($"Sliders[{i}]", mechanical.Sliders[i], parameters);
            for (var i = 0; i < mechanical.Hinges.Count; i++) Hinge($"Hinges[{i}]", mechanical.Hinges[i], parameters);
            // The owner's PowerIn route enables every declared drive; each retains its own physical limits.
            var driven = mechanical.Sliders.Select((slider, index) => (Path: $"Sliders[{index}]", Driven: slider.Drive is not null))
                .Concat(mechanical.Hinges.Select((hinge, index) => (Path: $"Hinges[{index}]", Driven: hinge.Drive is not null)))
                .Where(pair => pair.Driven).ToArray();
            // The owner compiles a consumer node only when it has an electrical PowerIn input and declares no supply capability of its
            // own (a source, switched contact or logic gate compiles another node kind, so its drives could never be supplied).
            if (driven.Length > 0 && (supply is not null || !ports.Any(port => port.Role == PortRole.PowerIn &&
                    port.Domain == SimulationDomain.Electrical && port.Direction == PortDirection.Input)))
                errors.Add(new(BundleErrorCode.UnresolvedReference,
                    $"{source}.{driven[0].Path}.Drive: a supplied drive requires an electrical PowerIn input on a consumer (no supply capability)"));
            MechanicalCapture(mechanical, parameters);
            MechanicalContactWork(mechanical, parameters);
            MechanicalSurface(mechanical, parameters);
        }

        private void MechanicalCapture(MechanicalPayload mechanical, IReadOnlyList<ParameterDescriptor> parameters)
        {
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

        private void MechanicalContactWork(MechanicalPayload mechanical, IReadOnlyList<ParameterDescriptor> parameters)
        {
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
                ValidateTargetSelector("ContactWork.Target", work.Target);
            }
        }

        private void MechanicalSurface(MechanicalPayload mechanical, IReadOnlyList<ParameterDescriptor> parameters)
        {
            if (mechanical.CompliantSurface is { } surface)
            {
                // The runtime surface's domain (CompliantSurfaceDeclaration): half extents and stroke 1/1024–16 m, rest height within
                // ±16 m, arrival depth finite, positive and under half the stroke, tension 120–1200 N/m and damping ratio 0.08–0.8, on
                // one static owner body.
                if (mechanical.KinematicMode != KinematicMode.Static)
                    OutOfRange("CompliantSurface.Owner");
                Finite("CompliantSurface.HalfExtents", surface.HalfExtents);
                if (float.IsFinite(surface.HalfExtents.X) && float.IsFinite(surface.HalfExtents.Y) &&
                    (!InSurfaceRange(surface.HalfExtents.X) || !InSurfaceRange(surface.HalfExtents.Y)))
                    OutOfRange("CompliantSurface.HalfExtents");
                if (Finite("CompliantSurface.RestHeight", surface.RestHeight) && MathF.Abs(surface.RestHeight) > 16f)
                    OutOfRange("CompliantSurface.RestHeight");
                if (Finite("CompliantSurface.MaxStroke", surface.MaxStroke) && !InSurfaceRange(surface.MaxStroke))
                    OutOfRange("CompliantSurface.MaxStroke");
                if (Finite("CompliantSurface.ArrivalDepth", surface.ArrivalDepth) &&
                    (surface.ArrivalDepth <= 0f || !float.IsFinite(surface.MaxStroke) || surface.ArrivalDepth >= surface.MaxStroke / 2f))
                    OutOfRange("CompliantSurface.ArrivalDepth");
                CaptureValue("CompliantSurface.Tension", surface.Tension, parameters, ParameterUnit.NewtonPerMetre, 120f, 1200f);
                CaptureValue("CompliantSurface.DampingRatio", surface.DampingRatio, parameters, ParameterUnit.None, 0.08f, 0.8f);
            }
        }

        private void SupplyPorts(ElectricalPayload electrical, SupplyCapabilityDescriptor supply, IReadOnlyList<PortDescriptor> ports)
        {
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
                    (!ports.Any(port => port.Role == PortRole.FirstIn && port.Domain == SimulationDomain.Electrical && port.Direction == PortDirection.Input) ||
                     !ports.Any(port => port.Role == PortRole.SecondIn && port.Domain == SimulationDomain.Electrical && port.Direction == PortDirection.Input))))
                OutOfRange("Supply.Operation");
            if (!electrical.Nodes.Any(node => node.Role == role) ||
                !ports.Any(port => port.Id == supply.Output && port.Role == PortRole.Supply && port.Domain == SimulationDomain.Electrical && port.Direction == PortDirection.Output) ||
                (supply.Kind == SupplyCapabilityKind.Source ? supply.Input is not null : (supply.Input is not { } inPort || !ports.Any(port => port.Id == inPort && port.Role == PortRole.PowerIn && port.Domain == SimulationDomain.Electrical && port.Direction == PortDirection.Input))))
                OutOfRange("Supply");
        }

        private static bool InSurfaceRange(float metres) => metres >= 1f / 1024f && metres <= 16f;

        private void MassFrame(string path, MassFrameDeclaration frame, KinematicMode mode, IReadOnlyList<ParameterDescriptor> parameters)
        {
            var scalars = new[] { frame.Mass, frame.CentreOfMass.X, frame.CentreOfMass.Y, frame.CentreOfMass.Z,
                frame.PrincipalMoments.X, frame.PrincipalMoments.Y, frame.PrincipalMoments.Z };
            foreach (var scalar in scalars)
                if (scalar.Parameter is { } key && !parameters.Any(p => p.Key == key.Value && p.Kind == ParameterKind.Real))
                    OutOfRange(path + ".Parameter");
            try
            {
                var values = parameters.ToDictionary(p => new ParameterId(p.Key), p => p.Default);
                frame.Evaluate(values).Validate(mode);
                // Every constraint is a sum of univariate cubics. Group shared parameters before
                // finding extrema, then sum independent domain minima/maxima (no Cartesian sampling).
                (float Min, float Max) Bounds(params (MassScalar Value, float Sign)[] terms)
                {
                    float minimum = 0, maximum = 0;
                    foreach (var group in terms.GroupBy(term => term.Value.Parameter))
                    {
                        var polynomial = new MassScalar(group.Key,
                            group.Sum(t => t.Sign * t.Value.C0), group.Sum(t => t.Sign * t.Value.C1),
                            group.Sum(t => t.Sign * t.Value.C2), group.Sum(t => t.Sign * t.Value.C3));
                        var domain = group.Key is { } key ? parameters.Single(p => p.Key == key.Value) : default;
                        float low = float.PositiveInfinity, high = float.NegativeInfinity;
                        foreach (var x in MassExtrema(polynomial, domain.Minimum, domain.Maximum))
                        {
                            if (group.Key is { } parameter) values[parameter] = x;
                            float value = 0;
                            foreach (var term in group) value += term.Sign * term.Value.Evaluate(values);
                            if (!float.IsFinite(value)) throw new ArgumentException("Mass-domain overflow.");
                            low = MathF.Min(low, value); high = MathF.Max(high, value);
                        }
                        minimum += low; maximum += high;
                    }
                    if (!float.IsFinite(minimum) || !float.IsFinite(maximum)) throw new ArgumentException("Mass-domain overflow.");
                    return (minimum, maximum);
                }
                void Range(MassScalar scalar, float minimum, float maximum, bool exclusiveMaximum = false)
                {
                    var bounds = Bounds((scalar, 1));
                    if (bounds.Min < minimum || (exclusiveMaximum ? bounds.Max >= maximum : bounds.Max > maximum))
                        throw new ArgumentException("Mass polynomial leaves its physical domain.");
                }
                if (mode == KinematicMode.Static)
                {
                    foreach (var scalar in scalars) Range(scalar, 0, 0);
                }
                else
                {
                    Range(frame.Mass, 1f / 1024, 1024);
                    foreach (var scalar in new[] { frame.CentreOfMass.X, frame.CentreOfMass.Y, frame.CentreOfMass.Z }) Range(scalar, -16, 16);
                    var moments = new[] { frame.PrincipalMoments.X, frame.PrincipalMoments.Y, frame.PrincipalMoments.Z };
                    foreach (var scalar in moments) Range(scalar, 1f / 4294967296, 524288, true);
                    for (var i = 0; i < 3; i++)
                        if (Bounds((moments[(i + 1) % 3], 1), (moments[(i + 2) % 3], 1), (moments[i], -1)).Min < 0)
                            throw new ArgumentException("Principal moments are nonphysical inside the parameter domain.");
                }
            }
            catch (ArgumentException) { OutOfRange(path); }
        }

        // Degree <= 3 has at most two interior stationary points. Inspect their adjacent f32
        // inputs too; admission still validates the actual evaluated frame at every supplied value.
        private static List<float> MassExtrema(MassScalar polynomial, float minimum, float maximum)
        {
            var points = new List<float>(8) { minimum, maximum };
            var scale = MathF.Max(MathF.Abs(polynomial.C1), MathF.Max(MathF.Abs(polynomial.C2), MathF.Abs(polynomial.C3)));
            if (!float.IsFinite(scale)) throw new ArgumentException("Invalid mass coefficients.");
            if (scale == 0) return points;
            var a = 3 * (polynomial.C3 / scale); var b = 2 * (polynomial.C2 / scale); var c = polynomial.C1 / scale;
            void Add(float x)
            {
                if (!float.IsFinite(x) || x < minimum || x > maximum) return;
                points.Add(x);
                if (float.BitDecrement(x) >= minimum) points.Add(float.BitDecrement(x));
                if (float.BitIncrement(x) <= maximum) points.Add(float.BitIncrement(x));
            }
            if (a == 0) { if (b != 0) Add(-c / b); return points; }
            var discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return points;
            var q = -.5f * (b + MathF.CopySign(MathF.Sqrt(discriminant), b));
            if (q == 0) Add(-b / (2 * a));
            else { Add(q / a); Add(c / q); }
            return points;
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
            if (float.IsFinite(slider.BaseFrame.Rotation.LengthSquared()) && MathF.Abs(slider.BaseFrame.Rotation.LengthSquared() - 1f) > 1e-4f) OutOfRange(path + ".BaseFrame.Rotation");
            Finite(path + ".MovingFrame.Position", slider.MovingFrame.Position);
            Finite(path + ".MovingFrame.Rotation", slider.MovingFrame.Rotation);
            if (float.IsFinite(slider.MovingFrame.Rotation.LengthSquared()) && MathF.Abs(slider.MovingFrame.Rotation.LengthSquared() - 1f) > 1e-4f) OutOfRange(path + ".MovingFrame.Rotation");
            if (Finite(path + ".LowerLimit", slider.LowerLimit) & Finite(path + ".UpperLimit", slider.UpperLimit))
            {
                if (slider.LowerLimit > 0f) OutOfRange(path + ".LowerLimit");
                else if (slider.UpperLimit < 0f || slider.UpperLimit <= slider.LowerLimit) OutOfRange(path + ".UpperLimit");
            }
            Value(path + ".Stiffness", slider.Stiffness, parameters);
            Value(path + ".Damping", slider.Damping, parameters);
            Value(path + ".Compression", slider.Compression, parameters);
            var body = slider.MovingBody;
            MassFrame(path + ".MovingBody.MassFrame", body.MassFrame, KinematicMode.Dynamic, parameters);
            for (var i = 0; i < body.Colliders.Count; i++) Collider($"{path}.MovingBody.Colliders[{i}]", body.Colliders[i]);
            if (body.Colliders.Count == 0)
                errors.Add(new(BundleErrorCode.BodyWithoutCollider, $"{source}.{path}.MovingBody.Colliders: a body declares no collider"));
            if (slider.Drive is { } drive)
            {
                // The target lies within the travel; every bound is a finite positive f32 (a zero bound is a drive that cannot move).
                DriveValue(path + ".Drive.Target", drive.Target, parameters, slider.LowerLimit, slider.UpperLimit, positive: false, ParameterUnit.Metre);
                // 64 m/s: the game-grade envelope's linear speed bound.
                DriveValue(path + ".Drive.MaxSpeed", drive.MaxSpeed, parameters, 0f, 64f, positive: true, ParameterUnit.MetrePerSecond);
                DriveValue(path + ".Drive.StoppingAcceleration", drive.StoppingAcceleration, parameters, 0f, float.MaxValue, positive: true,
                    ParameterUnit.MetrePerSecondSquared);
                DriveValue(path + ".Drive.MaxEffort", drive.MaxEffort, parameters, 0f, float.MaxValue, positive: true, ParameterUnit.Newton);
                DriveValue(path + ".Drive.MaxPower", drive.MaxPower, parameters, 0f, float.MaxValue, positive: true, ParameterUnit.Watt);
            }
        }

        private void Hinge(string path, RevoluteHingeDescriptor hinge, IReadOnlyList<ParameterDescriptor> parameters)
        {
            Finite(path + ".BaseFrame.Position", hinge.BaseFrame.Position);
            Finite(path + ".BaseFrame.Rotation", hinge.BaseFrame.Rotation);
            if (float.IsFinite(hinge.BaseFrame.Rotation.LengthSquared()) && MathF.Abs(hinge.BaseFrame.Rotation.LengthSquared() - 1f) > 1e-4f) OutOfRange(path + ".BaseFrame.Rotation");
            Finite(path + ".MovingFrame.Position", hinge.MovingFrame.Position);
            Finite(path + ".MovingFrame.Rotation", hinge.MovingFrame.Rotation);
            if (float.IsFinite(hinge.MovingFrame.Rotation.LengthSquared()) && MathF.Abs(hinge.MovingFrame.Rotation.LengthSquared() - 1f) > 1e-4f) OutOfRange(path + ".MovingFrame.Rotation");
            if (Finite(path + ".LowerLimit", hinge.LowerLimit) & Finite(path + ".UpperLimit", hinge.UpperLimit))
            {
                if (hinge.LowerLimit < -MathF.PI || hinge.LowerLimit > MathF.PI) OutOfRange(path + ".LowerLimit");
                if (hinge.UpperLimit < -MathF.PI || hinge.UpperLimit > MathF.PI || hinge.UpperLimit <= hinge.LowerLimit) OutOfRange(path + ".UpperLimit");
            }
            Value(path + ".Stiffness", hinge.Stiffness, parameters);
            Value(path + ".Damping", hinge.Damping, parameters);
            Value(path + ".FrictionTorque", hinge.FrictionTorque, parameters);
            Value(path + ".InitialAngle", hinge.InitialAngle, parameters, allowNegative: true);
            if (hinge.InitialAngle.Source == MechanicalValueSource.Constant && float.IsFinite(hinge.InitialAngle.Constant))
            {
                if (hinge.InitialAngle.Constant < hinge.LowerLimit || hinge.InitialAngle.Constant > hinge.UpperLimit)
                    OutOfRange(path + ".InitialAngle");
            }
            var body = hinge.MovingBody;
            MassFrame(path + ".MovingBody.MassFrame", body.MassFrame, KinematicMode.Dynamic, parameters);
            for (var i = 0; i < body.Colliders.Count; i++) Collider($"{path}.MovingBody.Colliders[{i}]", body.Colliders[i]);
            if (body.Colliders.Count == 0)
                errors.Add(new(BundleErrorCode.BodyWithoutCollider, $"{source}.{path}.MovingBody.Colliders: a body declares no collider"));
            Defined(path + ".Travel", hinge.Travel);
            // Continuous travel turns without end: its limits are exactly the full turn [−π, π] and it carries no axial spring, whose
            // pull toward a wrapped angle would reverse at ±π.
            if (hinge.Travel == RevoluteTravel.Continuous)
            {
                if (hinge.LowerLimit != -MathF.PI || hinge.UpperLimit != MathF.PI) OutOfRange(path + ".Travel");
                if (hinge.Stiffness.Source != MechanicalValueSource.Constant || hinge.Stiffness.Constant != 0f) OutOfRange(path + ".Stiffness");
            }
            if (hinge.Drive is { } drive)
            {
                // A signed target within the game-grade angular speed envelope (128 rad/s); a positive finite torque bound.
                DriveValue(path + ".Drive.TargetSpeed", drive.TargetSpeed, parameters, -128f, 128f, positive: false, ParameterUnit.RadianPerSecond);
                DriveValue(path + ".Drive.MaxTorque", drive.MaxTorque, parameters, 0f, float.MaxValue, positive: true, ParameterUnit.NewtonMetre);
            }
        }

        /// <summary>One drive value inside [minimum, maximum] (excluding a zero bound when <paramref name="positive"/>): a constant
        /// directly, a parameter by its whole declared range and in the drive value's own <paramref name="unit"/>.</summary>
        private void DriveValue(string path, MechanicalValue value, IReadOnlyList<ParameterDescriptor> parameters, float minimum, float maximum, bool positive,
            ParameterUnit unit)
        {
            if (value.Source == MechanicalValueSource.Constant && value.Parameter is null && float.IsFinite(value.Constant))
            {
                if (value.Constant < minimum || value.Constant > maximum || (positive && value.Constant <= 0f)) OutOfRange(path);
                return;
            }
            Value(path, value, parameters, allowNegative: !positive);
            if (value.Source == MechanicalValueSource.Parameter &&
                parameters.SingleOrDefault(parameter => parameter.Key == value.Parameter && parameter.Kind == ParameterKind.Real) is { Key: not null } parameter &&
                (parameter.Minimum < minimum || parameter.Maximum > maximum || (positive && parameter.Minimum <= 0f) ||
                 parameter.Unit != unit))
                OutOfRange(path);
        }

        private void Value(string path, MechanicalValue value, IReadOnlyList<ParameterDescriptor> parameters, bool allowNegative = false)
        {
            Defined(path + ".Source", value.Source);
            Finite(path + ".Constant", value.Constant);
            if (value.Source == MechanicalValueSource.Parameter)
            {
                if (value.Constant != 0f) OutOfRange(path + ".Constant");
                Real(path + ".Parameter", value.Parameter ?? "", parameters);
            }
            else if (value.Parameter is not null || (!allowNegative && value.Constant < 0f)) OutOfRange(path);
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
                if (string.IsNullOrWhiteSpace(supply.Output.Value)) OutOfRange("Supply.Output");
                if (supply.Input is { } input && string.IsNullOrWhiteSpace(input.Value)) OutOfRange("Supply.Input");
                SupplyPorts(electrical, supply, ports);
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

        private void ValidateTargetSelector(string path, TargetSelector selector)
        {
            Defined($"{path}.Kind", selector.Kind);
            if (selector.Kind == TargetSelectorKind.Named)
            {
                if (string.IsNullOrWhiteSpace(selector.NamedTarget))
                    OutOfRange($"{path}.NamedTarget");
            }
            else if (selector.NamedTarget is not null)
            {
                OutOfRange($"{path}.NamedTarget");
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

        private void Finite(string path, Vector2 value)
        {
            Finite(path + ".x", value.X);
            Finite(path + ".y", value.Y);
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
