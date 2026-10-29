using System;
using System.Collections.Immutable;
using System.Linq;
using Google.FlatBuffers;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopWireFlatBuffers
{
    private static Offset<W.CapabilityRigidBodyDeclarationRecord> EncodeCapabilityRigidBodyDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.RigidBodyDeclaration value)
    {
        var cell = EncodeCapabilityCellOriginRecord(b, value.Cell);
        var local = EncodeCapabilityLocalPositionRecord(b, value.Local);
        var rotation = EncodeCapabilityCanonicalRotationRecord(b, value.Rotation);
        var velocity = EncodeCapabilityLinearVelocityRecord(b, value.Velocity);
        var angular_velocity = EncodeCapabilityAngularVelocityRecord(b, value.AngularVelocity);
        var gravity = EncodeCapabilityAccelerationVectorRecord(b, value.Gravity);
        var mass_properties = EncodeCapabilityRigidMassPropertiesRecord(b, value.MassProperties);
        W.CapabilityRigidBodyDeclarationRecord.StartCapabilityRigidBodyDeclarationRecord(b);
        W.CapabilityRigidBodyDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityRigidBodyDeclarationRecord.AddMotion(b, (uint)value.Motion);
        W.CapabilityRigidBodyDeclarationRecord.AddCell(b, cell);
        W.CapabilityRigidBodyDeclarationRecord.AddLocal(b, local);
        W.CapabilityRigidBodyDeclarationRecord.AddRotation(b, rotation);
        W.CapabilityRigidBodyDeclarationRecord.AddVelocity(b, velocity);
        W.CapabilityRigidBodyDeclarationRecord.AddAngularVelocity(b, angular_velocity);
        W.CapabilityRigidBodyDeclarationRecord.AddMass(b, value.Mass.Value);
        W.CapabilityRigidBodyDeclarationRecord.AddGravity(b, gravity);
        W.CapabilityRigidBodyDeclarationRecord.AddLinearDrag(b, value.LinearDrag.Value);
        W.CapabilityRigidBodyDeclarationRecord.AddMassProperties(b, mass_properties);
        W.CapabilityRigidBodyDeclarationRecord.AddPayload(b, (uint)value.Payload);
        return W.CapabilityRigidBodyDeclarationRecord.EndCapabilityRigidBodyDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.RigidBodyDeclaration DecodeCapabilityRigidBodyDeclarationRecord(W.CapabilityRigidBodyDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuBodyId(row.Id),
        ObservationEnum<CuriousContraptions.Gpu.RigidMotionKind>(row.Motion),
        DecodeCapabilityCellOriginRecord(Required(row.Cell, "Cell")),
        DecodeCapabilityLocalPositionRecord(Required(row.Local, "Local")),
        DecodeCapabilityCanonicalRotationRecord(Required(row.Rotation, "Rotation")),
        DecodeCapabilityLinearVelocityRecord(Required(row.Velocity, "Velocity")),
        DecodeCapabilityAngularVelocityRecord(Required(row.AngularVelocity, "AngularVelocity")),
        new CuriousContraptions.Gpu.Kilograms(row.Mass),
        DecodeCapabilityAccelerationVectorRecord(Required(row.Gravity, "Gravity")),
        new CuriousContraptions.Gpu.InverseSeconds(row.LinearDrag),
        DecodeCapabilityRigidMassPropertiesRecord(Required(row.MassProperties, "MassProperties")),
        ObservationEnum<CuriousContraptions.Abstractions.Mechanical.PayloadPolicy>(row.Payload));

    private static Offset<W.CapabilityCellOriginRecord> EncodeCapabilityCellOriginRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.CellOrigin value)
    {
        W.CapabilityCellOriginRecord.StartCapabilityCellOriginRecord(b);
        W.CapabilityCellOriginRecord.AddX(b, value.X);
        W.CapabilityCellOriginRecord.AddY(b, value.Y);
        W.CapabilityCellOriginRecord.AddZ(b, value.Z);
        return W.CapabilityCellOriginRecord.EndCapabilityCellOriginRecord(b);
    }

    private static CuriousContraptions.Gpu.CellOrigin DecodeCapabilityCellOriginRecord(W.CapabilityCellOriginRecord row) => new(
        row.X,
        row.Y,
        row.Z);

    private static Offset<W.CapabilityLocalPositionRecord> EncodeCapabilityLocalPositionRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.LocalPosition value)
    {
        W.CapabilityLocalPositionRecord.StartCapabilityLocalPositionRecord(b);
        W.CapabilityLocalPositionRecord.AddX(b, value.X);
        W.CapabilityLocalPositionRecord.AddY(b, value.Y);
        W.CapabilityLocalPositionRecord.AddZ(b, value.Z);
        return W.CapabilityLocalPositionRecord.EndCapabilityLocalPositionRecord(b);
    }

    private static CuriousContraptions.Gpu.LocalPosition DecodeCapabilityLocalPositionRecord(W.CapabilityLocalPositionRecord row) => new(
        row.X,
        row.Y,
        row.Z);

    private static Offset<W.CapabilityCanonicalRotationRecord> EncodeCapabilityCanonicalRotationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.CanonicalRotation value)
    {
        W.CapabilityCanonicalRotationRecord.StartCapabilityCanonicalRotationRecord(b);
        W.CapabilityCanonicalRotationRecord.AddX(b, value.X);
        W.CapabilityCanonicalRotationRecord.AddY(b, value.Y);
        W.CapabilityCanonicalRotationRecord.AddZ(b, value.Z);
        W.CapabilityCanonicalRotationRecord.AddW(b, value.W);
        return W.CapabilityCanonicalRotationRecord.EndCapabilityCanonicalRotationRecord(b);
    }

    private static CuriousContraptions.Gpu.CanonicalRotation DecodeCapabilityCanonicalRotationRecord(W.CapabilityCanonicalRotationRecord row) => new(
        row.X,
        row.Y,
        row.Z,
        row.W);

    private static Offset<W.CapabilityLinearVelocityRecord> EncodeCapabilityLinearVelocityRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.LinearVelocity value)
    {
        W.CapabilityLinearVelocityRecord.StartCapabilityLinearVelocityRecord(b);
        W.CapabilityLinearVelocityRecord.AddX(b, value.X);
        W.CapabilityLinearVelocityRecord.AddY(b, value.Y);
        W.CapabilityLinearVelocityRecord.AddZ(b, value.Z);
        return W.CapabilityLinearVelocityRecord.EndCapabilityLinearVelocityRecord(b);
    }

    private static CuriousContraptions.Gpu.LinearVelocity DecodeCapabilityLinearVelocityRecord(W.CapabilityLinearVelocityRecord row) => new(
        row.X,
        row.Y,
        row.Z);

    private static Offset<W.CapabilityAngularVelocityRecord> EncodeCapabilityAngularVelocityRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.AngularVelocity value)
    {
        W.CapabilityAngularVelocityRecord.StartCapabilityAngularVelocityRecord(b);
        W.CapabilityAngularVelocityRecord.AddX(b, value.X);
        W.CapabilityAngularVelocityRecord.AddY(b, value.Y);
        W.CapabilityAngularVelocityRecord.AddZ(b, value.Z);
        return W.CapabilityAngularVelocityRecord.EndCapabilityAngularVelocityRecord(b);
    }

    private static CuriousContraptions.Gpu.AngularVelocity DecodeCapabilityAngularVelocityRecord(W.CapabilityAngularVelocityRecord row) => new(
        row.X,
        row.Y,
        row.Z);

    private static Offset<W.CapabilityAccelerationVectorRecord> EncodeCapabilityAccelerationVectorRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.AccelerationVector value)
    {
        W.CapabilityAccelerationVectorRecord.StartCapabilityAccelerationVectorRecord(b);
        W.CapabilityAccelerationVectorRecord.AddX(b, value.X);
        W.CapabilityAccelerationVectorRecord.AddY(b, value.Y);
        W.CapabilityAccelerationVectorRecord.AddZ(b, value.Z);
        return W.CapabilityAccelerationVectorRecord.EndCapabilityAccelerationVectorRecord(b);
    }

    private static CuriousContraptions.Gpu.AccelerationVector DecodeCapabilityAccelerationVectorRecord(W.CapabilityAccelerationVectorRecord row) => new(
        row.X,
        row.Y,
        row.Z);

    private static Offset<W.CapabilityRigidMassPropertiesRecord> EncodeCapabilityRigidMassPropertiesRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.RigidMassProperties value)
    {
        var local_centre_of_mass = EncodeCapabilityMetreVectorRecord(b, value.LocalCentreOfMass);
        var principal_frame = EncodeCapabilityCanonicalRotationRecord(b, value.PrincipalFrame);
        W.CapabilityRigidMassPropertiesRecord.StartCapabilityRigidMassPropertiesRecord(b);
        W.CapabilityRigidMassPropertiesRecord.AddLocalCentreOfMass(b, local_centre_of_mass);
        W.CapabilityRigidMassPropertiesRecord.AddPrincipalFrame(b, principal_frame);
        W.CapabilityRigidMassPropertiesRecord.AddX(b, value.X.Moment);
        W.CapabilityRigidMassPropertiesRecord.AddY(b, value.Y.Moment);
        W.CapabilityRigidMassPropertiesRecord.AddZ(b, value.Z.Moment);
        return W.CapabilityRigidMassPropertiesRecord.EndCapabilityRigidMassPropertiesRecord(b);
    }

    private static CuriousContraptions.Gpu.RigidMassProperties DecodeCapabilityRigidMassPropertiesRecord(W.CapabilityRigidMassPropertiesRecord row) => new(
        DecodeCapabilityMetreVectorRecord(Required(row.LocalCentreOfMass, "LocalCentreOfMass")),
        DecodeCapabilityCanonicalRotationRecord(Required(row.PrincipalFrame, "PrincipalFrame")),
        new CuriousContraptions.Gpu.PrincipalInertia(row.X),
        new CuriousContraptions.Gpu.PrincipalInertia(row.Y),
        new CuriousContraptions.Gpu.PrincipalInertia(row.Z));

    private static Offset<W.CapabilityMetreVectorRecord> EncodeCapabilityMetreVectorRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.MetreVector value)
    {
        W.CapabilityMetreVectorRecord.StartCapabilityMetreVectorRecord(b);
        W.CapabilityMetreVectorRecord.AddX(b, value.X);
        W.CapabilityMetreVectorRecord.AddY(b, value.Y);
        W.CapabilityMetreVectorRecord.AddZ(b, value.Z);
        return W.CapabilityMetreVectorRecord.EndCapabilityMetreVectorRecord(b);
    }

    private static CuriousContraptions.Gpu.MetreVector DecodeCapabilityMetreVectorRecord(W.CapabilityMetreVectorRecord row) => new(
        row.X,
        row.Y,
        row.Z);

    private static Offset<W.CapabilityColliderDeclarationRecord> EncodeCapabilityColliderDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ColliderDeclaration value)
    {
        var pose = EncodeCapabilityRigidLocalPoseRecord(b, value.Pose);
        var half_extents = EncodeCapabilityMetreVectorRecord(b, value.HalfExtents);
        W.CapabilityColliderDeclarationRecord.StartCapabilityColliderDeclarationRecord(b);
        W.CapabilityColliderDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityColliderDeclarationRecord.AddBody(b, value.Body.Value);
        W.CapabilityColliderDeclarationRecord.AddMaterial(b, value.Material.Value);
        W.CapabilityColliderDeclarationRecord.AddShape(b, (uint)value.Shape);
        W.CapabilityColliderDeclarationRecord.AddPose(b, pose);
        W.CapabilityColliderDeclarationRecord.AddRadius(b, value.Radius.Value);
        W.CapabilityColliderDeclarationRecord.AddHalfExtents(b, half_extents);
        return W.CapabilityColliderDeclarationRecord.EndCapabilityColliderDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.ColliderDeclaration DecodeCapabilityColliderDeclarationRecord(W.CapabilityColliderDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuColliderId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.Body),
        new CuriousContraptions.Gpu.GpuMaterialId(row.Material),
        ObservationEnum<CuriousContraptions.Gpu.ColliderShapeKind>(row.Shape),
        DecodeCapabilityRigidLocalPoseRecord(Required(row.Pose, "Pose")),
        new CuriousContraptions.Gpu.Metres(row.Radius),
        DecodeCapabilityMetreVectorRecord(Required(row.HalfExtents, "HalfExtents")));

    private static Offset<W.CapabilityRigidLocalPoseRecord> EncodeCapabilityRigidLocalPoseRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.RigidLocalPose value)
    {
        var translation = EncodeCapabilityMetreVectorRecord(b, value.Translation);
        var rotation = EncodeCapabilityCanonicalRotationRecord(b, value.Rotation);
        W.CapabilityRigidLocalPoseRecord.StartCapabilityRigidLocalPoseRecord(b);
        W.CapabilityRigidLocalPoseRecord.AddTranslation(b, translation);
        W.CapabilityRigidLocalPoseRecord.AddRotation(b, rotation);
        return W.CapabilityRigidLocalPoseRecord.EndCapabilityRigidLocalPoseRecord(b);
    }

    private static CuriousContraptions.Gpu.RigidLocalPose DecodeCapabilityRigidLocalPoseRecord(W.CapabilityRigidLocalPoseRecord row) => new(
        DecodeCapabilityMetreVectorRecord(Required(row.Translation, "Translation")),
        DecodeCapabilityCanonicalRotationRecord(Required(row.Rotation, "Rotation")));

    private static Offset<W.CapabilityContactMaterialDeclarationRecord> EncodeCapabilityContactMaterialDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ContactMaterialDeclaration value)
    {
        W.CapabilityContactMaterialDeclarationRecord.StartCapabilityContactMaterialDeclarationRecord(b);
        W.CapabilityContactMaterialDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityContactMaterialDeclarationRecord.AddRestitution(b, value.Restitution.Value);
        W.CapabilityContactMaterialDeclarationRecord.AddBounceThreshold(b, value.BounceThreshold.Value);
        W.CapabilityContactMaterialDeclarationRecord.AddFriction(b, value.Friction.Value);
        W.CapabilityContactMaterialDeclarationRecord.AddRollingResistance(b, value.RollingResistance.Value);
        return W.CapabilityContactMaterialDeclarationRecord.EndCapabilityContactMaterialDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.ContactMaterialDeclaration DecodeCapabilityContactMaterialDeclarationRecord(W.CapabilityContactMaterialDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuMaterialId(row.Id),
        new CuriousContraptions.Gpu.Restitution(row.Restitution),
        new CuriousContraptions.Gpu.LinearSpeed(row.BounceThreshold),
        new CuriousContraptions.Gpu.FrictionCoefficient(row.Friction),
        new CuriousContraptions.Gpu.RollingResistance(row.RollingResistance));

    private static Offset<W.CapabilityResidenceSensorDeclarationRecord> EncodeCapabilityResidenceSensorDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ResidenceSensorDeclaration value)
    {
        var targets = EncodeCapabilityBodyTargetSetRecord(b, value.Targets);
        var pose = EncodeCapabilityRigidLocalPoseRecord(b, value.Pose);
        var minimum = EncodeCapabilityMetreVectorRecord(b, value.Minimum);
        var maximum = EncodeCapabilityMetreVectorRecord(b, value.Maximum);
        W.CapabilityResidenceSensorDeclarationRecord.StartCapabilityResidenceSensorDeclarationRecord(b);
        W.CapabilityResidenceSensorDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityResidenceSensorDeclarationRecord.AddFrame(b, value.Frame.Value);
        W.CapabilityResidenceSensorDeclarationRecord.AddTargets(b, targets);
        W.CapabilityResidenceSensorDeclarationRecord.AddPose(b, pose);
        W.CapabilityResidenceSensorDeclarationRecord.AddMinimum(b, minimum);
        W.CapabilityResidenceSensorDeclarationRecord.AddMaximum(b, maximum);
        W.CapabilityResidenceSensorDeclarationRecord.AddSpeedLimit(b, value.SpeedLimit.Value);
        W.CapabilityResidenceSensorDeclarationRecord.AddDwell(b, value.Dwell.Value);
        W.CapabilityResidenceSensorDeclarationRecord.AddParticipation(b, (uint)value.Participation);
        return W.CapabilityResidenceSensorDeclarationRecord.EndCapabilityResidenceSensorDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.ResidenceSensorDeclaration DecodeCapabilityResidenceSensorDeclarationRecord(W.CapabilityResidenceSensorDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuSensorId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.Frame),
        DecodeCapabilityBodyTargetSetRecord(Required(row.Targets, "Targets")),
        DecodeCapabilityRigidLocalPoseRecord(Required(row.Pose, "Pose")),
        DecodeCapabilityMetreVectorRecord(Required(row.Minimum, "Minimum")),
        DecodeCapabilityMetreVectorRecord(Required(row.Maximum, "Maximum")),
        new CuriousContraptions.Gpu.LinearSpeed(row.SpeedLimit),
        new CuriousContraptions.Gpu.DurationSeconds(row.Dwell),
        ObservationEnum<CuriousContraptions.Gpu.SensorParticipation>(row.Participation));

    private static Offset<W.CapabilityBodyTargetSetRecord> EncodeCapabilityBodyTargetSetRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.BodyTargetSet value)
    {
        W.CapabilityBodyTargetSetRecord.StartCapabilityBodyTargetSetRecord(b);
        W.CapabilityBodyTargetSetRecord.AddKind(b, (uint)value.Kind);
        W.CapabilityBodyTargetSetRecord.AddBody(b, value.Body.Value);
        return W.CapabilityBodyTargetSetRecord.EndCapabilityBodyTargetSetRecord(b);
    }

    private static CuriousContraptions.Gpu.BodyTargetSet DecodeCapabilityBodyTargetSetRecord(W.CapabilityBodyTargetSetRecord row) => new(
        ObservationEnum<CuriousContraptions.Gpu.BodyTargetKind>(row.Kind),
        new CuriousContraptions.Gpu.GpuBodyId(row.Body));

    private static Offset<W.CapabilityPlanarGuideDeclarationRecord> EncodeCapabilityPlanarGuideDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.PlanarGuideDeclaration value)
    {
        var targets = EncodeCapabilityBodyTargetSetRecord(b, value.Targets);
        var pose = EncodeCapabilityRigidLocalPoseRecord(b, value.Pose);
        var minimum = EncodeCapabilityMetreVectorRecord(b, value.Minimum);
        var maximum = EncodeCapabilityMetreVectorRecord(b, value.Maximum);
        W.CapabilityPlanarGuideDeclarationRecord.StartCapabilityPlanarGuideDeclarationRecord(b);
        W.CapabilityPlanarGuideDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityPlanarGuideDeclarationRecord.AddFrame(b, value.Frame.Value);
        W.CapabilityPlanarGuideDeclarationRecord.AddTargets(b, targets);
        W.CapabilityPlanarGuideDeclarationRecord.AddPose(b, pose);
        W.CapabilityPlanarGuideDeclarationRecord.AddMinimum(b, minimum);
        W.CapabilityPlanarGuideDeclarationRecord.AddMaximum(b, maximum);
        W.CapabilityPlanarGuideDeclarationRecord.AddSupportHeight(b, value.SupportHeight.Value);
        W.CapabilityPlanarGuideDeclarationRecord.AddSupportMargin(b, value.SupportMargin.Value);
        W.CapabilityPlanarGuideDeclarationRecord.AddMaximumAcceleration(b, value.MaximumAcceleration.Value);
        return W.CapabilityPlanarGuideDeclarationRecord.EndCapabilityPlanarGuideDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.PlanarGuideDeclaration DecodeCapabilityPlanarGuideDeclarationRecord(W.CapabilityPlanarGuideDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuForceId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.Frame),
        DecodeCapabilityBodyTargetSetRecord(Required(row.Targets, "Targets")),
        DecodeCapabilityRigidLocalPoseRecord(Required(row.Pose, "Pose")),
        DecodeCapabilityMetreVectorRecord(Required(row.Minimum, "Minimum")),
        DecodeCapabilityMetreVectorRecord(Required(row.Maximum, "Maximum")),
        new CuriousContraptions.Gpu.Metres(row.SupportHeight),
        new CuriousContraptions.Gpu.Metres(row.SupportMargin),
        new CuriousContraptions.Gpu.Acceleration(row.MaximumAcceleration));

    private static Offset<W.CapabilityContactTriggerDeclarationRecord> EncodeCapabilityContactTriggerDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ContactTriggerDeclaration value)
    {
        var targets = EncodeCapabilityBodyTargetSetRecord(b, value.Targets);
        W.CapabilityContactTriggerDeclarationRecord.StartCapabilityContactTriggerDeclarationRecord(b);
        W.CapabilityContactTriggerDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityContactTriggerDeclarationRecord.AddOwner(b, value.Owner.Value);
        W.CapabilityContactTriggerDeclarationRecord.AddTargets(b, targets);
        W.CapabilityContactTriggerDeclarationRecord.AddThreshold(b, value.Threshold.Value);
        return W.CapabilityContactTriggerDeclarationRecord.EndCapabilityContactTriggerDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.ContactTriggerDeclaration DecodeCapabilityContactTriggerDeclarationRecord(W.CapabilityContactTriggerDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuContactTriggerId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.Owner),
        DecodeCapabilityBodyTargetSetRecord(Required(row.Targets, "Targets")),
        new CuriousContraptions.Gpu.ContactSpeed(row.Threshold));

    private static Offset<W.CapabilityContactWorkDeclarationRecord> EncodeCapabilityContactWorkDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ContactWorkDeclaration value)
    {
        var targets = EncodeCapabilityBodyTargetSetRecord(b, value.Targets);
        W.CapabilityContactWorkDeclarationRecord.StartCapabilityContactWorkDeclarationRecord(b);
        W.CapabilityContactWorkDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityContactWorkDeclarationRecord.AddOwner(b, value.Owner.Value);
        W.CapabilityContactWorkDeclarationRecord.AddTargets(b, targets);
        W.CapabilityContactWorkDeclarationRecord.AddTargetSpeed(b, value.TargetSpeed.Value);
        W.CapabilityContactWorkDeclarationRecord.AddInitialEnergy(b, value.InitialEnergy.Value);
        W.CapabilityContactWorkDeclarationRecord.AddThreshold(b, value.Threshold.Value);
        W.CapabilityContactWorkDeclarationRecord.AddCooldownPhysicalSteps(b, value.CooldownPhysicalSteps);
        return W.CapabilityContactWorkDeclarationRecord.EndCapabilityContactWorkDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.ContactWorkDeclaration DecodeCapabilityContactWorkDeclarationRecord(W.CapabilityContactWorkDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuContactWorkId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.Owner),
        DecodeCapabilityBodyTargetSetRecord(Required(row.Targets, "Targets")),
        new CuriousContraptions.Gpu.ContactSpeed(row.TargetSpeed),
        new CuriousContraptions.Gpu.Joules(row.InitialEnergy),
        new CuriousContraptions.Gpu.ContactSpeed(row.Threshold),
        row.CooldownPhysicalSteps);

    private static Offset<W.CapabilityOrientationSensorDeclarationRecord> EncodeCapabilityOrientationSensorDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.OrientationSensorDeclaration value)
    {
        var initial = EncodeCapabilityCanonicalRotationRecord(b, value.Initial);
        W.CapabilityOrientationSensorDeclarationRecord.StartCapabilityOrientationSensorDeclarationRecord(b);
        W.CapabilityOrientationSensorDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityOrientationSensorDeclarationRecord.AddBody(b, value.Body.Value);
        W.CapabilityOrientationSensorDeclarationRecord.AddInitial(b, initial);
        W.CapabilityOrientationSensorDeclarationRecord.AddThreshold(b, value.Threshold.CosineHalfAngle);
        return W.CapabilityOrientationSensorDeclarationRecord.EndCapabilityOrientationSensorDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.OrientationSensorDeclaration DecodeCapabilityOrientationSensorDeclarationRecord(W.CapabilityOrientationSensorDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuOrientationSensorId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.Body),
        DecodeCapabilityCanonicalRotationRecord(Required(row.Initial, "Initial")),
        new CuriousContraptions.Gpu.OrientationThreshold(row.Threshold));

    private static Offset<W.CapabilityElectricalSourceDeclarationRecord> EncodeCapabilityElectricalSourceDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ElectricalSourceDeclaration value)
    {
        W.CapabilityElectricalSourceDeclarationRecord.StartCapabilityElectricalSourceDeclarationRecord(b);
        W.CapabilityElectricalSourceDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityElectricalSourceDeclarationRecord.AddOwner(b, value.Owner.Value);
        W.CapabilityElectricalSourceDeclarationRecord.AddCapacity(b, value.Capacity.Value);
        W.CapabilityElectricalSourceDeclarationRecord.AddMaximumPower(b, value.MaximumPower.Value);
        W.CapabilityElectricalSourceDeclarationRecord.AddInitialFraction(b, value.InitialFraction);
        W.CapabilityElectricalSourceDeclarationRecord.AddEnabled(b, (uint)value.Enabled);
        return W.CapabilityElectricalSourceDeclarationRecord.EndCapabilityElectricalSourceDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.ElectricalSourceDeclaration DecodeCapabilityElectricalSourceDeclarationRecord(W.CapabilityElectricalSourceDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.ElectricalSourceId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.Owner),
        new CuriousContraptions.Gpu.Joules(row.Capacity),
        new CuriousContraptions.Gpu.Watts(row.MaximumPower),
        row.InitialFraction,
        ObservationEnum<CuriousContraptions.Gpu.ElectricalEnable>(row.Enabled));

    private static Offset<W.CapabilityElectricalStorageBindingRecord> EncodeCapabilityElectricalStorageBindingRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ElectricalStorageBinding value)
    {
        W.CapabilityElectricalStorageBindingRecord.StartCapabilityElectricalStorageBindingRecord(b);
        W.CapabilityElectricalStorageBindingRecord.AddSource(b, value.Source.Value);
        W.CapabilityElectricalStorageBindingRecord.AddStorage(b, value.Storage.Value);
        return W.CapabilityElectricalStorageBindingRecord.EndCapabilityElectricalStorageBindingRecord(b);
    }

    private static CuriousContraptions.Gpu.ElectricalStorageBinding DecodeCapabilityElectricalStorageBindingRecord(W.CapabilityElectricalStorageBindingRecord row) => new(
        new CuriousContraptions.Gpu.ElectricalSourceId(row.Source),
        new CuriousContraptions.Gpu.GpuContactWorkId(row.Storage));

    private static Offset<W.CapabilityPrismaticConstraintDeclarationRecord> EncodeCapabilityPrismaticConstraintDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.PrismaticConstraintDeclaration value)
    {
        var frame_a = EncodeCapabilityConstraintFrameRecord(b, value.FrameA);
        var frame_b = EncodeCapabilityConstraintFrameRecord(b, value.FrameB);
        W.CapabilityPrismaticConstraintDeclarationRecord.StartCapabilityPrismaticConstraintDeclarationRecord(b);
        W.CapabilityPrismaticConstraintDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityPrismaticConstraintDeclarationRecord.AddBodyA(b, value.BodyA.Value);
        W.CapabilityPrismaticConstraintDeclarationRecord.AddBodyB(b, value.BodyB.Value);
        W.CapabilityPrismaticConstraintDeclarationRecord.AddFrameA(b, frame_a);
        W.CapabilityPrismaticConstraintDeclarationRecord.AddFrameB(b, frame_b);
        W.CapabilityPrismaticConstraintDeclarationRecord.AddLower(b, value.Lower);
        W.CapabilityPrismaticConstraintDeclarationRecord.AddUpper(b, value.Upper);
        W.CapabilityPrismaticConstraintDeclarationRecord.AddStiffness(b, value.Stiffness);
        W.CapabilityPrismaticConstraintDeclarationRecord.AddDamping(b, value.Damping);
        W.CapabilityPrismaticConstraintDeclarationRecord.AddCollision(b, (uint)value.Collision);
        return W.CapabilityPrismaticConstraintDeclarationRecord.EndCapabilityPrismaticConstraintDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.PrismaticConstraintDeclaration DecodeCapabilityPrismaticConstraintDeclarationRecord(W.CapabilityPrismaticConstraintDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.PrismaticConstraintId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.BodyA),
        new CuriousContraptions.Gpu.GpuBodyId(row.BodyB),
        DecodeCapabilityConstraintFrameRecord(Required(row.FrameA, "FrameA")),
        DecodeCapabilityConstraintFrameRecord(Required(row.FrameB, "FrameB")),
        row.Lower,
        row.Upper,
        row.Stiffness,
        row.Damping,
        ObservationEnum<CuriousContraptions.Gpu.ConnectedCollision>(row.Collision));

    private static Offset<W.CapabilityConstraintFrameRecord> EncodeCapabilityConstraintFrameRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ConstraintFrame value)
    {
        var position = EncodeCapabilityVector3Record(b, value.Position);
        var rotation = EncodeCapabilityQuaternionRecord(b, value.Rotation);
        W.CapabilityConstraintFrameRecord.StartCapabilityConstraintFrameRecord(b);
        W.CapabilityConstraintFrameRecord.AddPosition(b, position);
        W.CapabilityConstraintFrameRecord.AddRotation(b, rotation);
        return W.CapabilityConstraintFrameRecord.EndCapabilityConstraintFrameRecord(b);
    }

    private static CuriousContraptions.Gpu.ConstraintFrame DecodeCapabilityConstraintFrameRecord(W.CapabilityConstraintFrameRecord row) => new(
        DecodeCapabilityVector3Record(Required(row.Position, "Position")),
        DecodeCapabilityQuaternionRecord(Required(row.Rotation, "Rotation")));

    private static Offset<W.CapabilityVector3Record> EncodeCapabilityVector3Record(FlatBufferBuilder b, System.Numerics.Vector3 value)
    {
        W.CapabilityVector3Record.StartCapabilityVector3Record(b);
        W.CapabilityVector3Record.AddX(b, value.X);
        W.CapabilityVector3Record.AddY(b, value.Y);
        W.CapabilityVector3Record.AddZ(b, value.Z);
        return W.CapabilityVector3Record.EndCapabilityVector3Record(b);
    }

    private static System.Numerics.Vector3 DecodeCapabilityVector3Record(W.CapabilityVector3Record row) => new(
        row.X,
        row.Y,
        row.Z);

    private static Offset<W.CapabilityQuaternionRecord> EncodeCapabilityQuaternionRecord(FlatBufferBuilder b, System.Numerics.Quaternion value)
    {
        W.CapabilityQuaternionRecord.StartCapabilityQuaternionRecord(b);
        W.CapabilityQuaternionRecord.AddX(b, value.X);
        W.CapabilityQuaternionRecord.AddY(b, value.Y);
        W.CapabilityQuaternionRecord.AddZ(b, value.Z);
        W.CapabilityQuaternionRecord.AddW(b, value.W);
        return W.CapabilityQuaternionRecord.EndCapabilityQuaternionRecord(b);
    }

    private static System.Numerics.Quaternion DecodeCapabilityQuaternionRecord(W.CapabilityQuaternionRecord row) => new(
        row.X,
        row.Y,
        row.Z,
        row.W);

    private static Offset<W.CapabilitySupplyNodeDeclarationRecord> EncodeCapabilitySupplyNodeDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.SupplyNodeDeclaration value)
    {
        var consumers = W.CapabilitySupplyNodeDeclarationRecord.CreateConsumersVector(b, (value.Consumers.IsDefault ? [] : value.Consumers).Select(item => item.Value).ToArray());
        W.CapabilitySupplyNodeDeclarationRecord.StartCapabilitySupplyNodeDeclarationRecord(b);
        W.CapabilitySupplyNodeDeclarationRecord.AddOwner(b, value.Owner.Value);
        W.CapabilitySupplyNodeDeclarationRecord.AddKind(b, (uint)value.Kind);
        W.CapabilitySupplyNodeDeclarationRecord.AddSource(b, value.Source.Value);
        W.CapabilitySupplyNodeDeclarationRecord.AddConsumers(b, consumers);
        if (value.Operation is { } operation) W.CapabilitySupplyNodeDeclarationRecord.AddOperation(b, (uint)operation);
        return W.CapabilitySupplyNodeDeclarationRecord.EndCapabilitySupplyNodeDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.SupplyNodeDeclaration DecodeCapabilitySupplyNodeDeclarationRecord(W.CapabilitySupplyNodeDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuBodyId(row.Owner),
        ObservationEnum<CuriousContraptions.Gpu.SupplyNodeKind>(row.Kind),
        new CuriousContraptions.Gpu.ElectricalSourceId(row.Source),
        Enumerable.Range(0, row.ConsumersLength).Select(i => new CuriousContraptions.Gpu.PrismaticConstraintId(row.Consumers(i))).ToImmutableArray(),
        row.Operation is { } operation ? ObservationEnum<CuriousContraptions.Abstractions.Electrical.LogicGateOperation>(operation) : null);

    private static Offset<W.CapabilitySupplyEdgeRecord> EncodeCapabilitySupplyEdgeRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.SupplyEdge value)
    {
        W.CapabilitySupplyEdgeRecord.StartCapabilitySupplyEdgeRecord(b);
        W.CapabilitySupplyEdgeRecord.AddSource(b, value.Source.Value);
        W.CapabilitySupplyEdgeRecord.AddTarget(b, value.Target.Value);
        W.CapabilitySupplyEdgeRecord.AddInput(b, (uint)value.Input);
        return W.CapabilitySupplyEdgeRecord.EndCapabilitySupplyEdgeRecord(b);
    }

    private static CuriousContraptions.Gpu.SupplyEdge DecodeCapabilitySupplyEdgeRecord(W.CapabilitySupplyEdgeRecord row) => new(
        new CuriousContraptions.Gpu.GpuBodyId(row.Source),
        new CuriousContraptions.Gpu.GpuBodyId(row.Target),
        ObservationEnum<CuriousContraptions.Gpu.WorkshopSocket>(row.Input));

    private static Offset<W.CapabilityContactLoadSensorDeclarationRecord> EncodeCapabilityContactLoadSensorDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ContactLoadSensorDeclaration value)
    {
        var local_normal = EncodeCapabilityVector3Record(b, value.LocalNormal);
        var supporting_colliders = W.CapabilityContactLoadSensorDeclarationRecord.CreateSupportingCollidersVector(b, (value.SupportingColliders.IsDefault ? [] : value.SupportingColliders).Select(item => item.Value).ToArray());
        W.CapabilityContactLoadSensorDeclarationRecord.StartCapabilityContactLoadSensorDeclarationRecord(b);
        W.CapabilityContactLoadSensorDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityContactLoadSensorDeclarationRecord.AddFrame(b, value.Frame.Value);
        W.CapabilityContactLoadSensorDeclarationRecord.AddRegion(b, value.Region.Value);
        W.CapabilityContactLoadSensorDeclarationRecord.AddMinimumX(b, value.MinimumX);
        W.CapabilityContactLoadSensorDeclarationRecord.AddMaximumX(b, value.MaximumX);
        W.CapabilityContactLoadSensorDeclarationRecord.AddMinimumZ(b, value.MinimumZ);
        W.CapabilityContactLoadSensorDeclarationRecord.AddMaximumZ(b, value.MaximumZ);
        W.CapabilityContactLoadSensorDeclarationRecord.AddLocalNormal(b, local_normal);
        W.CapabilityContactLoadSensorDeclarationRecord.AddMinimumAlignment(b, value.MinimumAlignment);
        W.CapabilityContactLoadSensorDeclarationRecord.AddMaximumGap(b, value.MaximumGap);
        W.CapabilityContactLoadSensorDeclarationRecord.AddMinimumMass(b, value.MinimumMass);
        W.CapabilityContactLoadSensorDeclarationRecord.AddSupportingColliders(b, supporting_colliders);
        return W.CapabilityContactLoadSensorDeclarationRecord.EndCapabilityContactLoadSensorDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.ContactLoadSensorDeclaration DecodeCapabilityContactLoadSensorDeclarationRecord(W.CapabilityContactLoadSensorDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuContactLoadSensorId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.Frame),
        new CuriousContraptions.Gpu.GpuColliderId(row.Region),
        row.MinimumX,
        row.MaximumX,
        row.MinimumZ,
        row.MaximumZ,
        DecodeCapabilityVector3Record(Required(row.LocalNormal, "LocalNormal")),
        row.MinimumAlignment,
        row.MaximumGap,
        row.MinimumMass,
        Enumerable.Range(0, row.SupportingCollidersLength).Select(i => new CuriousContraptions.Gpu.GpuColliderId(row.SupportingColliders(i))).ToImmutableArray());

    private static Offset<W.CapabilityPrismaticDriveDeclarationRecord> EncodeCapabilityPrismaticDriveDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.PrismaticDriveDeclaration value)
    {
        W.CapabilityPrismaticDriveDeclarationRecord.StartCapabilityPrismaticDriveDeclarationRecord(b);
        W.CapabilityPrismaticDriveDeclarationRecord.AddJoint(b, value.Joint.Value);
        W.CapabilityPrismaticDriveDeclarationRecord.AddTarget(b, value.Target);
        W.CapabilityPrismaticDriveDeclarationRecord.AddMaxSpeed(b, value.MaxSpeed);
        W.CapabilityPrismaticDriveDeclarationRecord.AddStoppingAcceleration(b, value.StoppingAcceleration);
        W.CapabilityPrismaticDriveDeclarationRecord.AddMaxEffort(b, value.MaxEffort);
        W.CapabilityPrismaticDriveDeclarationRecord.AddMaxPower(b, value.MaxPower);
        return W.CapabilityPrismaticDriveDeclarationRecord.EndCapabilityPrismaticDriveDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.PrismaticDriveDeclaration DecodeCapabilityPrismaticDriveDeclarationRecord(W.CapabilityPrismaticDriveDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.PrismaticConstraintId(row.Joint),
        row.Target,
        row.MaxSpeed,
        row.StoppingAcceleration,
        row.MaxEffort,
        row.MaxPower);

    private static Offset<W.CapabilityActivationNodeDeclarationRecord> EncodeCapabilityActivationNodeDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ActivationNodeDeclaration value)
    {
        W.CapabilityActivationNodeDeclarationRecord.StartCapabilityActivationNodeDeclarationRecord(b);
        W.CapabilityActivationNodeDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityActivationNodeDeclarationRecord.AddOwner(b, value.Owner.Value);
        W.CapabilityActivationNodeDeclarationRecord.AddKind(b, (uint)value.Kind);
        W.CapabilityActivationNodeDeclarationRecord.AddTrigger(b, value.Trigger.Value);
        W.CapabilityActivationNodeDeclarationRecord.AddDurationTicks(b, value.DurationTicks);
        W.CapabilityActivationNodeDeclarationRecord.AddSensor(b, value.Sensor.Value);
        W.CapabilityActivationNodeDeclarationRecord.AddPassage(b, value.Passage.Value);
        W.CapabilityActivationNodeDeclarationRecord.AddTargetCount(b, value.TargetCount);
        W.CapabilityActivationNodeDeclarationRecord.AddIntervalTicks(b, value.IntervalTicks);
        W.CapabilityActivationNodeDeclarationRecord.AddPowerSource(b, value.PowerSource.Value);
        W.CapabilityActivationNodeDeclarationRecord.AddCollider(b, value.Collider.Value);
        W.CapabilityActivationNodeDeclarationRecord.AddContactLoad(b, value.ContactLoad.Value);
        W.CapabilityActivationNodeDeclarationRecord.AddReleaseHoldTicks(b, value.ReleaseHoldTicks);
        W.CapabilityActivationNodeDeclarationRecord.AddCompletion(b, (uint)value.Completion);
        W.CapabilityActivationNodeDeclarationRecord.AddBoundary(b, (uint)value.Boundary);
        W.CapabilityActivationNodeDeclarationRecord.AddElapsedSignal(b, (uint)value.ElapsedSignal);
        return W.CapabilityActivationNodeDeclarationRecord.EndCapabilityActivationNodeDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.ActivationNodeDeclaration DecodeCapabilityActivationNodeDeclarationRecord(W.CapabilityActivationNodeDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.ActivationNodeId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.Owner),
        ObservationEnum<CuriousContraptions.Gpu.ActivationNodeKind>(row.Kind),
        new CuriousContraptions.Gpu.GpuContactTriggerId(row.Trigger),
        row.DurationTicks,
        new CuriousContraptions.Gpu.GpuOrientationSensorId(row.Sensor),
        new CuriousContraptions.Gpu.GpuPassageSensorId(row.Passage),
        row.TargetCount,
        row.IntervalTicks,
        new CuriousContraptions.Gpu.GpuBodyId(row.PowerSource),
        new CuriousContraptions.Gpu.GpuColliderId(row.Collider),
        new CuriousContraptions.Gpu.GpuContactLoadSensorId(row.ContactLoad),
        row.ReleaseHoldTicks,
        ObservationEnum<CuriousContraptions.Abstractions.Signal.TimerCompletionPolicy>(row.Completion),
        ObservationEnum<CuriousContraptions.Abstractions.Signal.TimerBoundary>(row.Boundary),
        ObservationEnum<CuriousContraptions.Abstractions.Signal.TimerElapsedSignal>(row.ElapsedSignal));

    private static Offset<W.CapabilityActivationEdgeRecord> EncodeCapabilityActivationEdgeRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.ActivationEdge value)
    {
        W.CapabilityActivationEdgeRecord.StartCapabilityActivationEdgeRecord(b);
        W.CapabilityActivationEdgeRecord.AddSource(b, value.Source.Value);
        W.CapabilityActivationEdgeRecord.AddTarget(b, value.Target.Value);
        W.CapabilityActivationEdgeRecord.AddInput(b, (uint)value.Input);
        return W.CapabilityActivationEdgeRecord.EndCapabilityActivationEdgeRecord(b);
    }

    private static CuriousContraptions.Gpu.ActivationEdge DecodeCapabilityActivationEdgeRecord(W.CapabilityActivationEdgeRecord row) => new(
        new CuriousContraptions.Gpu.ActivationNodeId(row.Source),
        new CuriousContraptions.Gpu.ActivationNodeId(row.Target),
        ObservationEnum<CuriousContraptions.Gpu.WorkshopSocket>(row.Input));

    private static Offset<W.CapabilityPassageSensorDeclarationRecord> EncodeCapabilityPassageSensorDeclarationRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.PassageSensorDeclaration value)
    {
        var cell = EncodeCapabilityCellOriginRecord(b, value.Cell);
        var local = EncodeCapabilityLocalPositionRecord(b, value.Local);
        var rotation = EncodeCapabilityCanonicalRotationRecord(b, value.Rotation);
        W.CapabilityPassageSensorDeclarationRecord.StartCapabilityPassageSensorDeclarationRecord(b);
        W.CapabilityPassageSensorDeclarationRecord.AddId(b, value.Id.Value);
        W.CapabilityPassageSensorDeclarationRecord.AddFrame(b, value.Frame.Value);
        W.CapabilityPassageSensorDeclarationRecord.AddCell(b, cell);
        W.CapabilityPassageSensorDeclarationRecord.AddLocal(b, local);
        W.CapabilityPassageSensorDeclarationRecord.AddRotation(b, rotation);
        W.CapabilityPassageSensorDeclarationRecord.AddRadius(b, value.Radius.Value);
        W.CapabilityPassageSensorDeclarationRecord.AddRearmClearance(b, value.RearmClearance.Value);
        return W.CapabilityPassageSensorDeclarationRecord.EndCapabilityPassageSensorDeclarationRecord(b);
    }

    private static CuriousContraptions.Gpu.PassageSensorDeclaration DecodeCapabilityPassageSensorDeclarationRecord(W.CapabilityPassageSensorDeclarationRecord row) => new(
        new CuriousContraptions.Gpu.GpuPassageSensorId(row.Id),
        new CuriousContraptions.Gpu.GpuBodyId(row.Frame),
        DecodeCapabilityCellOriginRecord(Required(row.Cell, "Cell")),
        DecodeCapabilityLocalPositionRecord(Required(row.Local, "Local")),
        DecodeCapabilityCanonicalRotationRecord(Required(row.Rotation, "Rotation")),
        new CuriousContraptions.Gpu.Metres(row.Radius),
        new CuriousContraptions.Gpu.Metres(row.RearmClearance));

    private static Offset<W.CapabilityPresentationBindingRecord> EncodeCapabilityPresentationBindingRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.CompiledPresentationBinding value)
    {
        W.CapabilityPresentationBindingRecord.StartCapabilityPresentationBindingRecord(b);
        W.CapabilityPresentationBindingRecord.AddProperty(b, (uint)value.Property);
        W.CapabilityPresentationBindingRecord.AddDriver(b, (uint)value.Driver);
        W.CapabilityPresentationBindingRecord.AddFrom(b, value.From);
        W.CapabilityPresentationBindingRecord.AddTo(b, value.To);
        W.CapabilityPresentationBindingRecord.AddCurve(b, (uint)value.Curve);
        W.CapabilityPresentationBindingRecord.AddDuration(b, value.Duration);
        W.CapabilityPresentationBindingRecord.AddOverlap(b, (uint)value.Overlap);
        W.CapabilityPresentationBindingRecord.AddThreshold(b, value.Threshold);
        W.CapabilityPresentationBindingRecord.AddPhase(b, (uint)value.Phase);
        W.CapabilityPresentationBindingRecord.AddResponse(b, value.Response);
        if (value.Intermediate is { } intermediate) W.CapabilityPresentationBindingRecord.AddIntermediate(b, intermediate);
        if (value.Socket is { } socket) W.CapabilityPresentationBindingRecord.AddSocket(b, (uint)socket);
        W.CapabilityPresentationBindingRecord.AddEvent(b, (uint)value.Event);
        W.CapabilityPresentationBindingRecord.AddEventMagnitude(b, (uint)value.EventMagnitude);
        return W.CapabilityPresentationBindingRecord.EndCapabilityPresentationBindingRecord(b);
    }

    private static CuriousContraptions.Gpu.CompiledPresentationBinding DecodeCapabilityPresentationBindingRecord(W.CapabilityPresentationBindingRecord row) => new(
        ObservationEnum<CuriousContraptions.Abstractions.PresentationProperty>(row.Property),
        ObservationEnum<CuriousContraptions.Abstractions.PresentationDriver>(row.Driver),
        row.From,
        row.To,
        ObservationEnum<CuriousContraptions.Abstractions.PresentationCurve>(row.Curve),
        row.Duration,
        ObservationEnum<CuriousContraptions.Abstractions.PresentationOverlap>(row.Overlap),
        row.Threshold,
        ObservationEnum<CuriousContraptions.Abstractions.PresentationTimerPhase>(row.Phase),
        row.Response,
        row.Intermediate is { } intermediate ? intermediate : null,
        row.Socket is { } socket ? ObservationEnum<CuriousContraptions.Abstractions.PortSocket>(socket) : null,
        ObservationEnum<CuriousContraptions.Abstractions.SimulationEventKind>(row.Event),
        ObservationEnum<CuriousContraptions.Abstractions.PresentationEventMagnitude>(row.EventMagnitude));

    private static Offset<W.CapabilityWorkshopCadenceSettingsRecord> EncodeCapabilityWorkshopCadenceSettingsRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.WorkshopCadenceSettings value)
    {
        var presentation_rate = EncodeCapabilityPulseRateRecord(b, value.PresentationRate);
        W.CapabilityWorkshopCadenceSettingsRecord.StartCapabilityWorkshopCadenceSettingsRecord(b);
        W.CapabilityWorkshopCadenceSettingsRecord.AddSimulation(b, (uint)value.Simulation);
        W.CapabilityWorkshopCadenceSettingsRecord.AddPhysical(b, (uint)value.Physical);
        W.CapabilityWorkshopCadenceSettingsRecord.AddAnimation(b, (uint)value.Animation);
        W.CapabilityWorkshopCadenceSettingsRecord.AddPresentation(b, (uint)value.Presentation);
        W.CapabilityWorkshopCadenceSettingsRecord.AddPresentationRate(b, presentation_rate);
        return W.CapabilityWorkshopCadenceSettingsRecord.EndCapabilityWorkshopCadenceSettingsRecord(b);
    }

    private static CuriousContraptions.Gpu.WorkshopCadenceSettings DecodeCapabilityWorkshopCadenceSettingsRecord(W.CapabilityWorkshopCadenceSettingsRecord row) => new(
        ObservationEnum<CuriousContraptions.Gpu.SimulationCadence>(row.Simulation),
        ObservationEnum<CuriousContraptions.Gpu.PhysicalStepProfile>(row.Physical),
        ObservationEnum<CuriousContraptions.Gpu.AnimationCadence>(row.Animation),
        ObservationEnum<CuriousContraptions.Gpu.PresentationCadence>(row.Presentation),
        DecodeCapabilityPulseRateRecord(Required(row.PresentationRate, "PresentationRate")));

    private static Offset<W.CapabilityPulseRateRecord> EncodeCapabilityPulseRateRecord(FlatBufferBuilder b, CuriousContraptions.Gpu.PulseRate value)
    {
        W.CapabilityPulseRateRecord.StartCapabilityPulseRateRecord(b);
        W.CapabilityPulseRateRecord.AddNumerator(b, value.Numerator);
        W.CapabilityPulseRateRecord.AddDenominator(b, value.Denominator);
        return W.CapabilityPulseRateRecord.EndCapabilityPulseRateRecord(b);
    }

    private static CuriousContraptions.Gpu.PulseRate DecodeCapabilityPulseRateRecord(W.CapabilityPulseRateRecord row) => new(
        row.Numerator,
        row.Denominator);

    internal static Offset<W.CompiledSceneRecord> EncodeCompiledScene(FlatBufferBuilder b, WorkshopCompiledScene scene)
    {
        scene.Validate();
        var settings = EncodeCapabilityWorkshopCadenceSettingsRecord(b, scene.Settings);
        var bodies = W.CompiledSceneRecord.CreateBodiesVector(b, scene.Physics.Bodies.ToArray().Select(value => EncodeCapabilityRigidBodyDeclarationRecord(b, value)).ToArray());
        var colliders = W.CompiledSceneRecord.CreateCollidersVector(b, scene.Physics.Colliders.ToArray().Select(value => EncodeCapabilityColliderDeclarationRecord(b, value)).ToArray());
        var materials = W.CompiledSceneRecord.CreateMaterialsVector(b, scene.Physics.Materials.ToArray().Select(value => EncodeCapabilityContactMaterialDeclarationRecord(b, value)).ToArray());
        var sensors = W.CompiledSceneRecord.CreateSensorsVector(b, scene.Physics.Sensors.ToArray().Select(value => EncodeCapabilityResidenceSensorDeclarationRecord(b, value)).ToArray());
        var guides = W.CompiledSceneRecord.CreateGuidesVector(b, scene.Physics.Guides.ToArray().Select(value => EncodeCapabilityPlanarGuideDeclarationRecord(b, value)).ToArray());
        var triggers = W.CompiledSceneRecord.CreateTriggersVector(b, scene.Physics.Triggers.ToArray().Select(value => EncodeCapabilityContactTriggerDeclarationRecord(b, value)).ToArray());
        var contact_works = W.CompiledSceneRecord.CreateContactWorksVector(b, scene.Physics.ContactWorks.ToArray().Select(value => EncodeCapabilityContactWorkDeclarationRecord(b, value)).ToArray());
        var orientation_sensors = W.CompiledSceneRecord.CreateOrientationSensorsVector(b, scene.Physics.OrientationSensors.ToArray().Select(value => EncodeCapabilityOrientationSensorDeclarationRecord(b, value)).ToArray());
        var electrical_sources = W.CompiledSceneRecord.CreateElectricalSourcesVector(b, scene.Physics.Electrical.Sources.ToArray().Select(value => EncodeCapabilityElectricalSourceDeclarationRecord(b, value)).ToArray());
        var electrical_bindings = W.CompiledSceneRecord.CreateElectricalBindingsVector(b, scene.Physics.Electrical.Bindings.ToArray().Select(value => EncodeCapabilityElectricalStorageBindingRecord(b, value)).ToArray());
        var prismatics = W.CompiledSceneRecord.CreatePrismaticsVector(b, scene.Physics.Prismatics.ToArray().Select(value => EncodeCapabilityPrismaticConstraintDeclarationRecord(b, value)).ToArray());
        var supply_nodes = W.CompiledSceneRecord.CreateSupplyNodesVector(b, scene.Physics.SupplyGraph.Nodes.ToArray().Select(value => EncodeCapabilitySupplyNodeDeclarationRecord(b, value)).ToArray());
        var supply_edges = W.CompiledSceneRecord.CreateSupplyEdgesVector(b, scene.Physics.SupplyGraph.Edges.ToArray().Select(value => EncodeCapabilitySupplyEdgeRecord(b, value)).ToArray());
        var contact_load_sensors = W.CompiledSceneRecord.CreateContactLoadSensorsVector(b, scene.Physics.ContactLoadSensors.ToArray().Select(value => EncodeCapabilityContactLoadSensorDeclarationRecord(b, value)).ToArray());
        var prismatic_drives = W.CompiledSceneRecord.CreatePrismaticDrivesVector(b, scene.Physics.PrismaticDrives.ToArray().Select(value => EncodeCapabilityPrismaticDriveDeclarationRecord(b, value)).ToArray());
        var activation_nodes = W.CompiledSceneRecord.CreateActivationNodesVector(b, scene.Activation.Nodes.ToArray().Select(value => EncodeCapabilityActivationNodeDeclarationRecord(b, value)).ToArray());
        var activation_edges = W.CompiledSceneRecord.CreateActivationEdgesVector(b, scene.Activation.Edges.ToArray().Select(value => EncodeCapabilityActivationEdgeRecord(b, value)).ToArray());
        var passage_sensors = W.CompiledSceneRecord.CreatePassageSensorsVector(b, scene.Activation.PassageSensors.ToArray().Select(value => EncodeCapabilityPassageSensorDeclarationRecord(b, value)).ToArray());
        var contacts = W.CompiledSceneRecord.CreateContactOwnersVector(b, scene.Activation.ContactOwners.Select(owner => owner.Value).ToArray());
        var owners = W.CompiledSceneRecord.CreateOwnersVector(b, scene.Owners.Select(owner => owner.Value).ToArray());
        var editable = W.CompiledSceneRecord.CreateEditableSourcesVector(b, scene.EditableSources.Select(owner => owner.Value).ToArray());
        var presentation = W.CompiledSceneRecord.CreatePresentationVector(b, scene.Presentation.Select(track =>
        {
            var declaration = EncodeCapabilityPresentationBindingRecord(b, track.Declaration);
            var activation = track.Activation == default ? default : EncodeCapabilityActivationNodeDeclarationRecord(b, track.Activation);
            W.CompiledPresentationRecord.StartCompiledPresentationRecord(b);
            W.CompiledPresentationRecord.AddTarget(b, track.Target.Value);
            W.CompiledPresentationRecord.AddOwner(b, track.Owner.Value);
            W.CompiledPresentationRecord.AddBinding(b, track.Binding);
            W.CompiledPresentationRecord.AddDeclaration(b, declaration);
            W.CompiledPresentationRecord.AddActivation(b, activation);
            W.CompiledPresentationRecord.AddSourceCapacity(b, track.SourceCapacity);
            return W.CompiledPresentationRecord.EndCompiledPresentationRecord(b);
        }).ToArray());
        W.CompiledSceneRecord.StartCompiledSceneRecord(b);
        W.CompiledSceneRecord.AddIdentity(b, scene.Identity.Value);
        W.CompiledSceneRecord.AddRevision(b, scene.Revision.Value);
        W.CompiledSceneRecord.AddSettings(b, settings);
        W.CompiledSceneRecord.AddDocumentLow(b, scene.Physics.Document.Low);
        W.CompiledSceneRecord.AddDocumentHigh(b, scene.Physics.Document.High);
        W.CompiledSceneRecord.AddNextIdentity(b, scene.Physics.NextIdentity);
        W.CompiledSceneRecord.AddBodies(b, bodies);
        W.CompiledSceneRecord.AddColliders(b, colliders);
        W.CompiledSceneRecord.AddMaterials(b, materials);
        W.CompiledSceneRecord.AddSensors(b, sensors);
        W.CompiledSceneRecord.AddGuides(b, guides);
        W.CompiledSceneRecord.AddTriggers(b, triggers);
        W.CompiledSceneRecord.AddContactWorks(b, contact_works);
        W.CompiledSceneRecord.AddOrientationSensors(b, orientation_sensors);
        W.CompiledSceneRecord.AddElectricalSources(b, electrical_sources);
        W.CompiledSceneRecord.AddElectricalBindings(b, electrical_bindings);
        W.CompiledSceneRecord.AddPrismatics(b, prismatics);
        W.CompiledSceneRecord.AddSupplyNodes(b, supply_nodes);
        W.CompiledSceneRecord.AddSupplyEdges(b, supply_edges);
        W.CompiledSceneRecord.AddContactLoadSensors(b, contact_load_sensors);
        W.CompiledSceneRecord.AddPrismaticDrives(b, prismatic_drives);
        W.CompiledSceneRecord.AddActivationNodes(b, activation_nodes);
        W.CompiledSceneRecord.AddActivationEdges(b, activation_edges);
        W.CompiledSceneRecord.AddPassageSensors(b, passage_sensors);
        W.CompiledSceneRecord.AddActivationSubsteps(b, scene.Activation.Substeps);
        W.CompiledSceneRecord.AddContactOwners(b, contacts);
        W.CompiledSceneRecord.AddOwners(b, owners);
        W.CompiledSceneRecord.AddEditableSources(b, editable);
        W.CompiledSceneRecord.AddPresentation(b, presentation);
        return W.CompiledSceneRecord.EndCompiledSceneRecord(b);
    }

    internal static WorkshopCompiledScene DecodeCompiledScene(W.CompiledSceneRecord row)
    {
        try
        {
            var bodies = Enumerable.Range(0, row.BodiesLength).Select(i => DecodeCapabilityRigidBodyDeclarationRecord(Required(row.Bodies(i), "Bodies"))).ToArray();
            var colliders = Enumerable.Range(0, row.CollidersLength).Select(i => DecodeCapabilityColliderDeclarationRecord(Required(row.Colliders(i), "Colliders"))).ToArray();
            var materials = Enumerable.Range(0, row.MaterialsLength).Select(i => DecodeCapabilityContactMaterialDeclarationRecord(Required(row.Materials(i), "Materials"))).ToArray();
            var sensors = Enumerable.Range(0, row.SensorsLength).Select(i => DecodeCapabilityResidenceSensorDeclarationRecord(Required(row.Sensors(i), "Sensors"))).ToArray();
            var guides = Enumerable.Range(0, row.GuidesLength).Select(i => DecodeCapabilityPlanarGuideDeclarationRecord(Required(row.Guides(i), "Guides"))).ToArray();
            var triggers = Enumerable.Range(0, row.TriggersLength).Select(i => DecodeCapabilityContactTriggerDeclarationRecord(Required(row.Triggers(i), "Triggers"))).ToArray();
            var contact_works = Enumerable.Range(0, row.ContactWorksLength).Select(i => DecodeCapabilityContactWorkDeclarationRecord(Required(row.ContactWorks(i), "ContactWorks"))).ToArray();
            var orientation_sensors = Enumerable.Range(0, row.OrientationSensorsLength).Select(i => DecodeCapabilityOrientationSensorDeclarationRecord(Required(row.OrientationSensors(i), "OrientationSensors"))).ToArray();
            var electrical_sources = Enumerable.Range(0, row.ElectricalSourcesLength).Select(i => DecodeCapabilityElectricalSourceDeclarationRecord(Required(row.ElectricalSources(i), "ElectricalSources"))).ToArray();
            var electrical_bindings = Enumerable.Range(0, row.ElectricalBindingsLength).Select(i => DecodeCapabilityElectricalStorageBindingRecord(Required(row.ElectricalBindings(i), "ElectricalBindings"))).ToArray();
            var prismatics = Enumerable.Range(0, row.PrismaticsLength).Select(i => DecodeCapabilityPrismaticConstraintDeclarationRecord(Required(row.Prismatics(i), "Prismatics"))).ToArray();
            var supply_nodes = Enumerable.Range(0, row.SupplyNodesLength).Select(i => DecodeCapabilitySupplyNodeDeclarationRecord(Required(row.SupplyNodes(i), "SupplyNodes"))).ToArray();
            var supply_edges = Enumerable.Range(0, row.SupplyEdgesLength).Select(i => DecodeCapabilitySupplyEdgeRecord(Required(row.SupplyEdges(i), "SupplyEdges"))).ToArray();
            var contact_load_sensors = Enumerable.Range(0, row.ContactLoadSensorsLength).Select(i => DecodeCapabilityContactLoadSensorDeclarationRecord(Required(row.ContactLoadSensors(i), "ContactLoadSensors"))).ToArray();
            var prismatic_drives = Enumerable.Range(0, row.PrismaticDrivesLength).Select(i => DecodeCapabilityPrismaticDriveDeclarationRecord(Required(row.PrismaticDrives(i), "PrismaticDrives"))).ToArray();
            var activation_nodes = Enumerable.Range(0, row.ActivationNodesLength).Select(i => DecodeCapabilityActivationNodeDeclarationRecord(Required(row.ActivationNodes(i), "ActivationNodes"))).ToArray();
            var activation_edges = Enumerable.Range(0, row.ActivationEdgesLength).Select(i => DecodeCapabilityActivationEdgeRecord(Required(row.ActivationEdges(i), "ActivationEdges"))).ToArray();
            var passage_sensors = Enumerable.Range(0, row.PassageSensorsLength).Select(i => DecodeCapabilityPassageSensorDeclarationRecord(Required(row.PassageSensors(i), "PassageSensors"))).ToArray();
            var physics = new PhysicsSceneDeclaration(new(row.DocumentLow, row.DocumentHigh), row.NextIdentity,
                bodies, colliders, materials, sensors, guides, triggers, contact_works, orientation_sensors,
                electrical_sources, electrical_bindings, prismatics, supply_nodes, supply_edges, contact_load_sensors, prismatic_drives);
            var activation = new ActivationNetwork(activation_nodes, activation_edges, row.ActivationSubsteps, passage_sensors,
                Enumerable.Range(0, row.ContactOwnersLength).Select(i => new GpuBodyId(row.ContactOwners(i))).ToArray());
            var presentation = Enumerable.Range(0, row.PresentationLength).Select(i =>
            {
                var track = Required(row.Presentation(i), "presentation");
                return new CompiledPresentationTrack(new(track.Target), new(track.Owner), track.Binding,
                    DecodeCapabilityPresentationBindingRecord(Required(track.Declaration, "declaration")),
                    track.Activation is { } node ? DecodeCapabilityActivationNodeDeclarationRecord(node) : default, track.SourceCapacity);
            });
            return new(new(row.Identity), new(row.Revision), DecodeCapabilityWorkshopCadenceSettingsRecord(Required(row.Settings, "settings")),
                physics, activation, presentation,
                Enumerable.Range(0, row.OwnersLength).Select(i => new GpuBodyId(row.Owners(i))),
                Enumerable.Range(0, row.EditableSourcesLength).Select(i => new GpuBodyId(row.EditableSources(i))));
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or OverflowException)
        { throw new WireFaultException(WireFaultReason.OutOfRange, "Invalid compiled scene capabilities.", error); }
    }
}
