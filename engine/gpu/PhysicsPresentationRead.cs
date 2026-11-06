using CuriousContraptions.Abstractions;
using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public static class ActuatorObservations
{
    /// <summary>The game-grade envelope's resting contact slop, 0.5 mm: a coordinate within it of a stop is at that stop.</summary>
    public const float StopSlop = 5e-4f;
    /// <summary>The game-grade envelope's at-rest linear speed, 0.01 m/s (its sleep threshold): slower axial motion is at rest.</summary>
    public const float RestSpeed = .01f;

    /// <summary>Something is in the way (CAT-051 Q3, coordinator decision 2, 22 Oct 2026): the moving body is in solver contact with
    /// another body, held at rest short of both stops. A slider resting in its interior with nothing touching it (an inverted or
    /// tilted mount at its spring-gravity equilibrium) is not blocked. A single state for both directions (Q4).</summary>
    public static bool IsBlocked(this ActuatorDriveRead drive) => drive.MovingContact && drive.Coordinate > drive.Lower + StopSlop && drive.Coordinate < drive.Upper - StopSlop && MathF.Abs(drive.Speed) < RestSpeed;
}

public static partial class PhysicsGpuAbi
{
    /// <summary>Projection of validated output plus the very inputs supplied to this tick; no supply reconstruction.</summary>
    private static ActuatorDriveRead[] ReadActuatorPresentation(ReadOnlySpan<byte> bytes, PhysicsTickInputs inputs, bool admission, PhysicsWorldLayout layout)
    {
        // One row per drive slot: every prismatic slot, then every revolute slot. A slot's supply is the very input of this tick.
        var actuators = new ActuatorDriveRead[layout.Prismatics + layout.Revolutes];
        Span<PrismaticRow> rows = stackalloc PrismaticRow[PrismaticRows.Count];
        for (var i = 0; i < layout.Prismatics; i++)
        {
            var joint = ReadPrismatic(bytes, i);
            var record = bytes.Slice(layout.PrismaticOffset(i), PrismaticBytes);
            var a = bytes.Slice(layout.BodyOffset((int)R32(record, PrismaticBodyAOffset)), BodyBytes);
            var b = bytes.Slice(layout.BodyOffset((int)R32(record, PrismaticBodyBOffset)), BodyBytes);
            PrismaticRows.Prepare(PresentationFrame(a), PresentationFrame(b), joint.FrameA, joint.FrameB,
                joint.Lower, joint.Upper, rows);
            var axial = rows[(int)PrismaticRowKind.Spring];
            var speed = Vector3.Dot(axial.LinearA, PresentationVector(a, BodyVelocityOffset)) +
                Vector3.Dot(axial.LinearB, PresentationVector(b, BodyVelocityOffset)) +
                Vector3.Dot(axial.AngularA, PresentationVector(a, BodyAngularVelocityOffset)) +
                Vector3.Dot(axial.AngularB, PresentationVector(b, BodyAngularVelocityOffset));
            actuators[i] = new(SupplyConsumer.Prismatic(joint.Id), joint.BodyA, !admission && inputs.SupplyClosed[i],
                ReadPrismaticMovingContact(bytes, i), axial.Error, speed, joint.Lower, joint.Upper);
        }
        Span<RevoluteRow> hinge = stackalloc RevoluteRow[RevoluteRows.Count];
        for (var i = 0; i < layout.Revolutes; i++)
        {
            var slot = layout.Prismatics + i;
            var joint = ReadRevolute(bytes, i);
            var record = bytes.Slice(layout.RevoluteOffset(i), RevoluteBytes);
            var a = bytes.Slice(layout.BodyOffset((int)R32(record, RevoluteBodyAOffset)), BodyBytes);
            var b = bytes.Slice(layout.BodyOffset((int)R32(record, RevoluteBodyBOffset)), BodyBytes);
            RevoluteRows.Prepare(PresentationFrame(a), PresentationFrame(b), joint.FrameA, joint.FrameB, joint.Lower, joint.Upper, hinge);
            var axial = hinge[(int)RevoluteRowKind.Axial];
            var spin = Vector3.Dot(axial.AngularA, PresentationVector(a, BodyAngularVelocityOffset)) +
                Vector3.Dot(axial.AngularB, PresentationVector(b, BodyAngularVelocityOffset));
            actuators[slot] = new(SupplyConsumer.Revolute(joint.Id), joint.BodyA, !admission && inputs.SupplyClosed[slot], false,
                ReadRevoluteState(bytes, i).Travel, spin, joint.Lower, joint.Upper);
        }
        return actuators;
    }

    public static PhysicsPresentationRead ReadPresentation(ReadOnlySpan<byte> bytes, PhysicsTickInputs inputs, bool admission)
    {
        var layout = Layout(bytes);
        var actuators = ReadActuatorPresentation(bytes, inputs, admission, layout);
        var loads = new ContactLoadSensorRead[ReadContactLoadCount(bytes)];
        for (var i = 0; i < loads.Length; i++) loads[i] = ReadContactLoadSensor(bytes, i);
        var ropes = new RopeSpanRead[layout.TensileSpans];
        var guideMap = new Dictionary<RopeGuideId, (float Travel, float Angle)>();
        if (inputs.Guides is { Length: > 0 })
        {
            foreach (var g in inputs.Guides)
                guideMap.TryAdd(g.Id, (0f, 0f));
        }

        var rawSpans = new (TensileSpanDeclaration Span, Vector3 From, Vector3 To, float Length, RopeIntegrity Integrity, float Impulse, float Travel, RopeSpanId CutSpan, Vector3 FrameBPos, Quaternion FrameBRot)[layout.TensileSpans];
        for (var i = 0; i < layout.TensileSpans; i++)
        {
            var span = ReadTensileSpan(bytes, i);
            var record = bytes.Slice(layout.TensileSpanOffset(i), TensileSpanBytes);
            var a = PresentationFrame(bytes.Slice(layout.BodyOffset((int)R32(record, TensileBodyAOffset)), BodyBytes));
            var b = PresentationFrame(bytes.Slice(layout.BodyOffset((int)R32(record, TensileBodyBOffset)), BodyBytes));
            var from = a.Position + Vector3.Transform(span.LocalA, a.Rotation);
            var to = b.Position + Vector3.Transform(span.LocalB, b.Rotation);
            var length = Vector3.Distance(from, to);
            var integrity = (RopeIntegrity)R32(record, TensileIntegrityOffset);
            var impulse = RF(record, TensileImpulseOffset);
            var travel = RF(record, TensileTravelOffset);
            var cutSpan = new RopeSpanId(R64(record, TensileCutSpanOffset));
            rawSpans[i] = (span, from, to, length, integrity, impulse, travel, cutSpan, b.Position, b.Rotation);
        }

        var routeGroups = rawSpans.Select((raw, index) => (raw, index))
            .GroupBy(x => x.raw.Span.Route);

        foreach (var group in routeGroups)
        {
            var ordered = group.OrderBy(x => x.raw.Span.Order).ToArray();
            var travel = ordered.Length > 0 ? ordered[0].raw.Travel : 0f;
            var impulse = ordered.Length > 0 ? ordered[0].raw.Impulse : 0f;

            var routeGuides = new RouteGuideFrame[ordered.Length > 1 ? ordered.Length - 1 : 0];
            for (var i = 0; i < ordered.Length - 1; i++)
            {
                var radius = ordered[i].raw.Span.GuideRadius;
                var offset = ordered[i].raw.Span.GuideOffset;
                if (ordered[i].raw.Span.BodyB == ordered[i + 1].raw.Span.BodyA)
                {
                    var angle = radius > 0f
                        ? ((travel / radius) % MathF.Tau + MathF.Tau) % MathF.Tau
                        : 0f;
                    guideMap[ordered[i].raw.Span.Guide] = (travel, angle);
                }
                routeGuides[i] = new(ordered[i].raw.FrameBPos, ordered[i].raw.FrameBRot, radius, offset);
            }

            var terminalA = ordered[0].raw.From;
            var terminalB = ordered[^1].raw.To;
            var evaluated = RopeGeometry.EvaluateRoute(terminalA, terminalB, routeGuides);
            var totalLength = evaluated.TotalLength;
            var totalMax = ordered.Sum(x => x.raw.Span.MaximumLength);
            var isCutOrIncomplete = ordered.Any(x => x.raw.Integrity == RopeIntegrity.Cut || !x.raw.Span.Complete);
            var routeState = isCutOrIncomplete
                ? RopeState.Open
                : (totalLength >= totalMax - 1e-3f || impulse > 0f ? RopeState.Taut : RopeState.Slack);

            foreach (var item in ordered)
            {
                var r = item.raw;
                ropes[item.index] = new(r.Span.Id, r.Span.Route, r.Span.Order, r.Span.BodyA, r.Span.BodyB,
                    r.From, r.To, r.Span.MaximumLength, r.Length, r.Integrity, routeState, r.CutSpan);
            }
        }

        var guides = guideMap.OrderBy(kv => kv.Key.Value)
            .Select(kv => new RopeGuideRead(kv.Key, kv.Value.Travel, kv.Value.Angle)).ToArray();
        var surfaces = new CompliantSurfaceRead[layout.CompliantSurfaces];
        for (var i = 0; i < surfaces.Length; i++) surfaces[i] = ReadCompliantSurfaceDeflection(bytes, i);
        Array.Sort(surfaces, (a, b) => a.Frame.Value.CompareTo(b.Frame.Value));
        return new(actuators, loads, inputs.Logic, ropes, guides, surfaces);
    }
    private static Vector3 PresentationVector(ReadOnlySpan<byte> body, int offset) =>
        new(RF(body, offset), RF(body, offset + 4), RF(body, offset + 8));
    private static ConstraintBodyFrame PresentationFrame(ReadOnlySpan<byte> body)
    {
        var value = ReadRotation(body, BodyRotationOffset);
        var rotation = new Quaternion(value.X, value.Y, value.Z, value.W);
        var position = PresentationPosition(body);
        var centre = ReadCentreOfMass(body);
        return new(position, rotation, position + Vector3.Transform(new((float)centre.X, (float)centre.Y, (float)centre.Z), rotation));
    }
    private static Vector3 PresentationPosition(ReadOnlySpan<byte> body)
    {
        var cell = ReadCell(body, BodyCellOffset); var local = ReadLocal(body, BodyLocalOffset);
        return new((cell.X + local.X) / 16f, (cell.Y + local.Y) / 16f, (cell.Z + local.Z) / 16f);
    }
}

