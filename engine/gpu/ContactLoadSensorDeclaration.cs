using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public readonly record struct GpuContactLoadSensorId(ulong Value);

public enum ContactLoadPhase : uint
{
    Empty = 0,
    Underweight = 1,
    Loaded = 2
}

public readonly record struct ContactLoadSensorDeclaration(
    GpuContactLoadSensorId Id,
    GpuBodyId Frame,
    MetreVector RegionMinimum,
    MetreVector RegionMaximum,
    MetreVector LocalNormal,
    Half MinimumAlignment,
    Kilograms MinimumMass)
{
    public void Validate()
    {
        if (Id.Value == 0 || Frame.Value == 0)
            throw new ArgumentException("Sensor and frame identities are required.");
        if (RegionMinimum.X >= RegionMaximum.X || RegionMinimum.Y >= RegionMaximum.Y || RegionMinimum.Z >= RegionMaximum.Z)
            throw new ArgumentOutOfRangeException(nameof(RegionMinimum), "Invalid sensor region bounds.");
        if (!Half.IsFinite(MinimumAlignment) || MinimumAlignment <= (Half)0 || MinimumAlignment > (Half)1)
            throw new ArgumentOutOfRangeException(nameof(MinimumAlignment), "Minimum alignment must be in (0, 1].");
        if (!Half.IsFinite(MinimumMass.Value) || MinimumMass.Value <= (Half)0)
            throw new ArgumentOutOfRangeException(nameof(MinimumMass), "Minimum mass must be positive.");
    }
}

public readonly record struct ContactLoadSensorRead(
    GpuContactLoadSensorId Sensor,
    GpuBodyId Frame,
    Kilograms MeasuredMass,
    ContactLoadPhase Phase,
    IReadOnlyList<GpuBodyId> TouchingBodies);

public readonly record struct ContactPointRecord(
    GpuBodyId BodyA,
    GpuBodyId BodyB,
    Vector3 Point,
    Vector3 NormalTowardB);

public sealed class ContactLoadSensorTracker
{
    public static readonly ContactLoadSensorTracker Empty = new([]);

    private readonly ContactLoadSensorDeclaration[] _sensors;
    public IReadOnlyList<ContactLoadSensorDeclaration> Sensors => _sensors;

    public ContactLoadSensorTracker(ContactLoadSensorDeclaration[] sensors)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        _sensors = (ContactLoadSensorDeclaration[])sensors.Clone();
    }

    public static ContactLoadSensorTracker Create(ReadOnlySpan<ContactLoadSensorDeclaration> sensors)
    {
        var array = sensors.ToArray();
        foreach (var s in array) s.Validate();
        return new ContactLoadSensorTracker(array);
    }

    public ContactLoadSensorRead Measure(
        GpuContactLoadSensorId sensorId,
        Vector3 framePosition,
        Quaternion frameRotation,
        IReadOnlyList<ContactPointRecord> contacts,
        IReadOnlyDictionary<GpuBodyId, float> dynamicBodyMasses)
    {
        var sensor = Array.Find(_sensors, s => s.Id == sensorId);
        if (sensor.Id.Value == 0)
            throw new ArgumentException($"Unknown contact load sensor identity '{sensorId}'.");

        var qInv = Quaternion.Conjugate(frameRotation);
        var expectedNormal = Vector3.Normalize(new Vector3((float)sensor.LocalNormal.X, (float)sensor.LocalNormal.Y, (float)sensor.LocalNormal.Z));
        var minAlignment = (float)sensor.MinimumAlignment;

        var minX = (float)sensor.RegionMinimum.X;
        var minY = (float)sensor.RegionMinimum.Y;
        var minZ = (float)sensor.RegionMinimum.Z;
        var maxX = (float)sensor.RegionMaximum.X;
        var maxY = (float)sensor.RegionMaximum.Y;
        var maxZ = (float)sensor.RegionMaximum.Z;

        var touchedBodies = new Dictionary<GpuBodyId, float>();

        foreach (var c in contacts)
        {
            GpuBodyId otherId;
            Vector3 normalTowardOther;

            if (c.BodyA == sensor.Frame)
            {
                otherId = c.BodyB;
                normalTowardOther = c.NormalTowardB;
            }
            else if (c.BodyB == sensor.Frame)
            {
                otherId = c.BodyA;
                normalTowardOther = -c.NormalTowardB;
            }
            else
            {
                continue;
            }

            if (!dynamicBodyMasses.TryGetValue(otherId, out var mass) || mass <= 0f)
                continue;

            // Transform point and normal to frame local space
            var localPoint = Vector3.Transform(c.Point - framePosition, qInv);
            var localNormal = Vector3.Normalize(Vector3.Transform(normalTowardOther, qInv));

            var alignment = Vector3.Dot(localNormal, expectedNormal);
            if (alignment < minAlignment)
                continue;

            if (localPoint.X < minX || localPoint.X > maxX ||
                localPoint.Y < minY || localPoint.Y > maxY ||
                localPoint.Z < minZ || localPoint.Z > maxZ)
                continue;

            touchedBodies.TryAdd(otherId, mass);
        }

        var orderedBodies = touchedBodies.Keys.OrderBy(k => k.Value).ToArray();
        var totalMass = 0f;
        foreach (var id in orderedBodies) totalMass += touchedBodies[id];

        var phase = totalMass == 0f ? ContactLoadPhase.Empty
            : totalMass < (float)sensor.MinimumMass.Value ? ContactLoadPhase.Underweight
            : ContactLoadPhase.Loaded;

        return new ContactLoadSensorRead(sensor.Id, sensor.Frame, new Kilograms((Half)totalMass), phase, orderedBodies);
    }
}
