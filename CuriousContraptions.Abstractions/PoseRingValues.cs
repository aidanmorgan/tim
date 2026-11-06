using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum PosePublicationPreparation { Ready = 0, StorageUnavailable = 1 }

public readonly record struct PoseRingBody(
    GpuBodyId Id,
    float Px, float Py, float Pz,
    float Qx, float Qy, float Qz, float Qw,
    float Vx, float Vy, float Vz,
    uint Flags);

public readonly record struct PoseRingSlot(
    ulong Sequence,
    long TimestampNanoseconds,
    int BodyCount,
    PoseRingBody[] Bodies);
