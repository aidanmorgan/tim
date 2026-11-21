using System;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int RotaryAirflowCaptureBytes = 64;
    public const int RotaryAirflowOwnerOffset = 8;
    public const int RotaryAirflowFrameOffset = 12;
    public const int RotaryAirflowCoordinateOffset = 16;
    public const int RotaryAirflowSampleStartOffset = 20;
    public const int RotaryAirflowSampleCountOffset = 24;
    public const int RotaryAirflowExclusionStartOffset = 28;
    public const int RotaryAirflowExclusionCountOffset = 32;
    public const int RotaryAirflowPitchOffset = 36;
    public const int RotaryAirflowResponseOffset = 40;
    public const int RotaryAirflowCutInOffset = 44;
    public const int RotaryAirflowMaximumRateOffset = 48;

    private static void AdmitRotaryAirflow(Span<byte> data, IPhysicsSceneDeclaration scene, int sample, int exclusion)
    {
        for (var i = 0; i < scene.RotaryAirflowCaptures.Length; i++)
        {
            var capture = scene.RotaryAirflowCaptures[i];
            var row = data.Slice(scene.World.RotaryAirflowCaptureOffset(i), RotaryAirflowCaptureBytes);
            U64(row, RecordIdentityOffset, capture.Id.Value);
            U32(row, RotaryAirflowOwnerOffset, BodySlot(scene, capture.Owner));
            U32(row, RotaryAirflowFrameOffset, BodySlot(scene, capture.Frame));
            U32(row, RotaryAirflowCoordinateOffset, checked((uint)scene.Layout.GetRevoluteSlot(capture.Coordinate.Value)));
            U32(row, RotaryAirflowSampleStartOffset, checked((uint)sample));
            U32(row, RotaryAirflowSampleCountOffset, checked((uint)capture.Samples.Length));
            U32(row, RotaryAirflowExclusionStartOffset, checked((uint)exclusion));
            U32(row, RotaryAirflowExclusionCountOffset, checked((uint)capture.InternalOccluders.Length));
            F32(row, RotaryAirflowPitchOffset, capture.MaximumPitch);
            F32(row, RotaryAirflowResponseOffset, capture.Response);
            F32(row, RotaryAirflowCutInOffset, capture.CutInForce);
            F32(row, RotaryAirflowMaximumRateOffset, capture.MaximumRate);
            foreach (var point in capture.Samples)
            {
                var offset = scene.World.AirflowSampleOffset(sample++);
                AirflowVector(data, offset, point.Position);
                F32(data, offset + 12, point.Weight);
            }
            foreach (var collider in capture.InternalOccluders)
                U32(data, scene.World.AirflowExclusionOffset(exclusion++), checked((uint)scene.Layout.GetColliderSlot(collider)));
        }
    }
}
