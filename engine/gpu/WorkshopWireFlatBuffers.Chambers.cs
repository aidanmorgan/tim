using System;
using Google.FlatBuffers;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopWireFlatBuffers
{
    private static VectorOffset EncodeChamberDeclarations(FlatBufferBuilder builder, ReadOnlySpan<ChamberRegionDeclaration> regions)
    {
        var offsets = new Offset<W.ChamberRegionDeclarationRecord>[regions.Length];
        for (var i = 0; i < regions.Length; i++)
        {
            var value = regions[i];
            var targets = EncodeCapabilityBodyTargetSetRecord(builder, value.Targets);
            var pose = EncodeCapabilityRigidLocalPoseRecord(builder, value.Pose);
            W.ChamberRegionDeclarationRecord.StartChamberRegionDeclarationRecord(builder);
            W.ChamberRegionDeclarationRecord.AddId(builder, value.Id.Value);
            W.ChamberRegionDeclarationRecord.AddFrame(builder, value.Frame.Value);
            W.ChamberRegionDeclarationRecord.AddParticipationCollider(builder, value.ParticipationCollider.Value);
            W.ChamberRegionDeclarationRecord.AddTargets(builder, targets);
            W.ChamberRegionDeclarationRecord.AddPose(builder, pose);
            W.ChamberRegionDeclarationRecord.AddRadius(builder, value.Radius);
            W.ChamberRegionDeclarationRecord.AddHalfLength(builder, value.HalfLength);
            W.ChamberRegionDeclarationRecord.AddMinimumWidth(builder, value.MinimumWidth);
            W.ChamberRegionDeclarationRecord.AddMaximumWidth(builder, value.MaximumWidth);
            W.ChamberRegionDeclarationRecord.AddContainmentAllowance(builder, value.ContainmentAllowance);
            W.ChamberRegionDeclarationRecord.AddMaximumRelativeSpeed(builder, value.MaximumRelativeSpeed);
            W.ChamberRegionDeclarationRecord.AddParticipation(builder, (byte)value.Participation);
            offsets[i] = W.ChamberRegionDeclarationRecord.EndChamberRegionDeclarationRecord(builder);
        }
        return W.CompiledSceneRecord.CreateChamberRegionsVector(builder, offsets);
    }

    private static ChamberRegionDeclaration[] DecodeChamberDeclarations(W.CompiledSceneRecord scene)
    {
        var values = new ChamberRegionDeclaration[scene.ChamberRegionsLength];
        for (var i = 0; i < values.Length; i++)
        {
            var row = scene.ChamberRegions(i)!.Value;
            values[i] = new(new(row.Id), new(row.Frame), new(row.ParticipationCollider),
                DecodeCapabilityBodyTargetSetRecord(Required(row.Targets, "Targets")),
                DecodeCapabilityRigidLocalPoseRecord(Required(row.Pose, "Pose")), row.Radius, row.HalfLength,
                row.MinimumWidth, row.MaximumWidth, row.ContainmentAllowance, row.MaximumRelativeSpeed,
                ObservationEnum<SensorParticipation>(row.Participation));
        }
        return values;
    }

    private static VectorOffset EncodeFiniteWorkActuatorDeclarations(FlatBufferBuilder builder, ReadOnlySpan<FiniteWorkActuatorDeclaration> values)
    {
        W.CompiledSceneRecord.StartFiniteWorkActuatorsVector(builder, values.Length);
        for (var i = values.Length - 1; i >= 0; i--)
        {
            var value = values[i];
            W.FiniteWorkActuatorDeclarationRecord.CreateFiniteWorkActuatorDeclarationRecord(builder,
                value.Id.Value, value.Store.Value, value.Region.Value, value.TargetSpeed.Value, value.ExitPadding);
        }
        return builder.EndVector();
    }

    private static FiniteWorkActuatorDeclaration[] DecodeFiniteWorkActuatorDeclarations(W.CompiledSceneRecord scene)
    {
        var values = new FiniteWorkActuatorDeclaration[scene.FiniteWorkActuatorsLength];
        for (var i = 0; i < values.Length; i++)
        {
            var row = scene.FiniteWorkActuators(i)!.Value;
            values[i] = new(new(row.Id), new(row.Store), new(row.Region), new(row.TargetSpeed), row.ExitPadding);
        }
        return values;
    }

    private static VectorOffset EncodeChamberRead(FlatBufferBuilder builder, PhysicsChamberRead values)
    {
        W.WorkshopResponse.StartChamberRegionsVector(builder, values.Count);
        for (var i = values.Count - 1; i >= 0; i--)
        {
            var value = values[i];
            W.ChamberRegionReadRecord.CreateChamberRegionReadRecord(builder, value.Id.Value, value.Frame.Value,
                (byte)value.Phase, value.Occupancy, value.Candidate.Value, value.Departing.Value);
        }
        return builder.EndVector();
    }

    private static PhysicsChamberRead DecodeChamberRead(W.WorkshopResponse response)
    {
        var values = new ChamberRegionRead[response.ChamberRegionsLength];
        for (var i = 0; i < values.Length; i++)
        {
            var row = response.ChamberRegions(i)!.Value;
            values[i] = new(new(row.Id), new(row.Frame), ObservationEnum<ChamberPhase>(row.Phase),
                row.Occupancy, new(row.Candidate), new(row.Departing));
        }
        return new(values);
    }

    private static VectorOffset EncodeFiniteWorkActuatorRead(FlatBufferBuilder builder, PhysicsFiniteWorkActuatorRead values)
    {
        W.WorkshopResponse.StartFiniteWorkActuatorsVector(builder, values.Count);
        for (var i = values.Count - 1; i >= 0; i--)
        {
            var value = values[i];
            W.FiniteWorkActuatorReadRecord.CreateFiniteWorkActuatorReadRecord(builder, value.Id.Value, value.Store.Value, value.Region.Value,
                value.OccurrenceCount, value.LastResult is { } result ? (byte)((byte)result + 1) : (byte)0, value.LastPayload.Value, value.LastDebit.Value, (byte)value.Clearance);
        }
        return builder.EndVector();
    }

    private static PhysicsFiniteWorkActuatorRead DecodeFiniteWorkActuatorRead(W.WorkshopResponse response)
    {
        var values = new FiniteWorkActuatorRead[response.FiniteWorkActuatorsLength];
        for (var i = 0; i < values.Length; i++)
        {
            var row = response.FiniteWorkActuators(i)!.Value;
            values[i] = new(new(row.Id), new(row.Store), new(row.Region), row.OccurrenceCount,
                row.Result == 0 ? null : ObservationEnum<FiniteWorkRequestResult>((uint)row.Result - 1),
                new(row.Payload), new(row.Debit), ObservationEnum<FiniteWorkClearance>(row.Clearance));
        }
        return new(values);
    }
}
