using System.Collections.Generic;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Abstractions.Mechanical;

public readonly record struct LocalPrismaticRetentionId(uint Value);
public enum RetentionMode : byte { Retained, Releasing }
public enum RetentionPhase : byte { Idle, Winding, Armed, Releasing, Blocked }
public enum RetentionRequestResult : byte { Released, Empty, AlreadyReleased }
public enum PhysicalRequestOperation : byte { Release }

/// <summary>Retains negative travel of a zero-rest compressive elastic coordinate.</summary>
public readonly record struct PrismaticRetentionDescriptor(
    LocalPrismaticRetentionId Id, CoordinateReference Coordinate,
    LocalMechanicalTransmissionId WindingTransmission);
