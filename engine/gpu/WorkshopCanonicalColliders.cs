using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CuriousContraptions.Gpu;

public readonly record struct ExtractedCollider(
    ColliderShapeKind ShapeKind,
    RigidLocalPose LocalPose,
    Half Radius,
    MetreVector HalfExtents);

public static class WorkshopCanonicalColliders
{
    private static IReadOnlyList<ExtractedCollider>? _pipeColliders;
    public static IReadOnlyList<ExtractedCollider> PipeColliders => _pipeColliders ??= CreatePipeColliders();

    private static IReadOnlyList<ExtractedCollider>? _ballDetectorColliders;
    public static IReadOnlyList<ExtractedCollider> BallDetectorColliders => _ballDetectorColliders ??= CreateBallDetectorColliders();

    public static IReadOnlyList<ExtractedCollider> For(PartId partId) => partId.Value switch
    {
        "pipe" => PipeColliders,
        "ball_detector" => BallDetectorColliders,
        "pipe_bend_45" => PipeBend45Colliders,
        "pipe_bend_90" => PipeBend90Colliders,
        "funnel" => FunnelColliders,
        "powered_gate" => PoweredGateColliders,
        "pressure_plate" => PressurePlateColliders,
        "counter" => CounterColliders,
        "clock" => ClockColliders,
        "latch" => LatchColliders,
        _ => throw new ArgumentException($"No canonical colliders declared for part '{partId}'.")
    };

    private static IReadOnlyList<ExtractedCollider>? _latchColliders;
    public static IReadOnlyList<ExtractedCollider> LatchColliders => _latchColliders ??= CreateLatchColliders();

    private static ReadOnlyCollection<ExtractedCollider> CreateLatchColliders()
    {
        var housingPose = new RigidLocalPose(new MetreVector((Half)0, (Half)0, (Half)0), CanonicalRotation.Identity);
        housingPose.Validate();
        var footPose = new RigidLocalPose(new MetreVector((Half)0, (Half)(-0.72f), (Half)0), CanonicalRotation.Identity);
        footPose.Validate();
        return new List<ExtractedCollider>(2)
        {
            new(ColliderShapeKind.Box, housingPose, (Half)0, new MetreVector((Half)0.825f, (Half)0.625f, (Half)0.325f)),
            new(ColliderShapeKind.Box, footPose, (Half)0, new MetreVector((Half)0.9f, (Half)0.08f, (Half)0.425f))
        }.AsReadOnly();
    }

    private static IReadOnlyList<ExtractedCollider>? _clockColliders;
    public static IReadOnlyList<ExtractedCollider> ClockColliders => _clockColliders ??= CreateClockColliders();

    private static ReadOnlyCollection<ExtractedCollider> CreateClockColliders()
    {
        var housingPose = new RigidLocalPose(new MetreVector((Half)0, (Half)0, (Half)0), CanonicalRotation.Identity);
        housingPose.Validate();
        var footPose = new RigidLocalPose(new MetreVector((Half)0, (Half)(-0.95f), (Half)0), CanonicalRotation.Identity);
        footPose.Validate();
        return new List<ExtractedCollider>(2)
        {
            new(ColliderShapeKind.Box, housingPose, (Half)0, new MetreVector((Half)0.7f, (Half)0.85f, (Half)0.3f)),
            new(ColliderShapeKind.Box, footPose, (Half)0, new MetreVector((Half)0.8f, (Half)0.1f, (Half)0.425f))
        }.AsReadOnly();
    }

    private static IReadOnlyList<ExtractedCollider>? _counterColliders;
    public static IReadOnlyList<ExtractedCollider> CounterColliders => _counterColliders ??= CreateCounterColliders();

    private static ReadOnlyCollection<ExtractedCollider> CreateCounterColliders()
    {
        var housingPose = new RigidLocalPose(new MetreVector((Half)0, (Half)0, (Half)0), CanonicalRotation.Identity);
        housingPose.Validate();
        var footPose = new RigidLocalPose(new MetreVector((Half)0, (Half)(-0.72f), (Half)0), CanonicalRotation.Identity);
        footPose.Validate();
        return new List<ExtractedCollider>(2)
        {
            new(ColliderShapeKind.Box, housingPose, (Half)0, new MetreVector((Half)0.825f, (Half)0.625f, (Half)0.325f)),
            new(ColliderShapeKind.Box, footPose, (Half)0, new MetreVector((Half)0.9f, (Half)0.08f, (Half)0.425f))
        }.AsReadOnly();
    }

    private static IReadOnlyList<ExtractedCollider>? _pressurePlateColliders;
    public static IReadOnlyList<ExtractedCollider> PressurePlateColliders => _pressurePlateColliders ??= CreatePressurePlateColliders();

    private static IReadOnlyList<ExtractedCollider>? _poweredGateColliders;
    public static IReadOnlyList<ExtractedCollider> PoweredGateColliders => _poweredGateColliders ??= CreatePoweredGateColliders();

    private static IReadOnlyList<ExtractedCollider>? _funnelColliders;
    public static IReadOnlyList<ExtractedCollider> FunnelColliders => _funnelColliders ??= CreateFunnelColliders();

    private static ReadOnlyCollection<ExtractedCollider> CreatePipeColliders()
    {
        var list = new List<ExtractedCollider>(24);

        const float rMid = 0.675f;
        var circumferenceHalfExtents = new MetreVector((Half)1.8, (Half)0.025, (Half)0.14);

        for (var i = 0; i < 16; i++)
        {
            var theta = i * (MathF.Tau / 16f);
            var cos = MathF.Cos(theta);
            var sin = MathF.Sin(theta);

            var y = rMid * cos;
            var z = rMid * sin;

            var halfTheta = theta * 0.5f;
            var rot = CanonicalRotation.Normalize(MathF.Sin(halfTheta), 0, 0, MathF.Cos(halfTheta));

            var pose = new RigidLocalPose(
                new MetreVector((Half)0, (Half)y, (Half)z),
                rot);
            pose.Validate();

            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, circumferenceHalfExtents));
        }

        var rimTbHalfExtents = new MetreVector((Half)0.05, (Half)0.15, (Half)0.8);
        var rimLrHalfExtents = new MetreVector((Half)0.05, (Half)0.65, (Half)0.15);

        foreach (var x in new[] { -1.8f, 1.8f })
        {
            var topPose = new RigidLocalPose(new MetreVector((Half)x, (Half)0.8, (Half)0), CanonicalRotation.Identity);
            topPose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, topPose, (Half)0, rimTbHalfExtents));

            var botPose = new RigidLocalPose(new MetreVector((Half)x, (Half)(-0.8), (Half)0), CanonicalRotation.Identity);
            botPose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, botPose, (Half)0, rimTbHalfExtents));

            var leftPose = new RigidLocalPose(new MetreVector((Half)x, (Half)0, (Half)(-0.8)), CanonicalRotation.Identity);
            leftPose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, leftPose, (Half)0, rimLrHalfExtents));

            var rightPose = new RigidLocalPose(new MetreVector((Half)x, (Half)0, (Half)0.8), CanonicalRotation.Identity);
            rightPose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, rightPose, (Half)0, rimLrHalfExtents));
        }

        return list.AsReadOnly();
    }

    private static ReadOnlyCollection<ExtractedCollider> CreateBallDetectorColliders()
    {
        var list = new List<ExtractedCollider>(17);

        const float rMid = 0.675f;
        var circumferenceHalfExtents = new MetreVector((Half)0.28, (Half)0.025, (Half)0.14);

        for (var i = 0; i < 16; i++)
        {
            var theta = i * (MathF.Tau / 16f);
            var cos = MathF.Cos(theta);
            var sin = MathF.Sin(theta);

            var y = rMid * cos;
            var z = rMid * sin;

            var halfTheta = theta * 0.5f;
            var rot = CanonicalRotation.Normalize(MathF.Sin(halfTheta), 0, 0, MathF.Cos(halfTheta));

            var pose = new RigidLocalPose(
                new MetreVector((Half)0, (Half)y, (Half)z),
                rot);
            pose.Validate();

            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, circumferenceHalfExtents));
        }

        var housingHalfExtents = new MetreVector((Half)0.25, (Half)0.12, (Half)0.25);
        var housingPose = new RigidLocalPose(new MetreVector((Half)0, (Half)0.93, (Half)0), CanonicalRotation.Identity);
        housingPose.Validate();
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, housingPose, (Half)0, housingHalfExtents));

        return list.AsReadOnly();
    }

    private static IReadOnlyList<ExtractedCollider>? _pipeBend45Colliders;
    public static IReadOnlyList<ExtractedCollider> PipeBend45Colliders => _pipeBend45Colliders ??= CreatePipeBend45Colliders();

    private static ReadOnlyCollection<ExtractedCollider> CreatePipeBend45Colliders()
    {
        var list = new List<ExtractedCollider>(40);
        var wallHalfExtents = new MetreVector((Half)0.26, (Half)0.025, (Half)0.29);
        var rimTbHalfExtents = new MetreVector((Half)0.05, (Half)0.15, (Half)0.8);
        var rimLrHalfExtents = new MetreVector((Half)0.05, (Half)0.65, (Half)0.15);

        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.0490677f, 0.9987955f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.6170375f), (Half)0.8428822f, (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3822225f, -0.0187774f, -0.0453326f, 0.9227667f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.6364158f), (Half)0.6461312f, (Half)0.4772971f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.7062550f, -0.0346961f, -0.0346961f, 0.7062550f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.6831991f), (Half)0.1711325f, (Half)0.6750000f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9227667f, -0.0453326f, -0.0187774f, 0.3822225f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.7299824f), (Half)(-0.3038663f), (Half)0.4772971f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9987955f, -0.0490677f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.7493607f), (Half)(-0.5006172f), (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9227667f, -0.0453326f, 0.0187774f, -0.3822225f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.7299824f), (Half)(-0.3038663f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.7062550f, 0.0346961f, -0.0346961f, 0.7062550f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.6831991f), (Half)0.1711325f, (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3822225f, 0.0187774f, -0.0453326f, 0.9227667f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.6364158f), (Half)0.6461312f, (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.1467305f, 0.9891765f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.0258149f), (Half)0.7252807f, (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3785415f, -0.0561513f, -0.1355613f, 0.9138799f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.0832050f), (Half)0.5360908f, (Half)0.4772971f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.6994534f, -0.1037541f, -0.1037541f, 0.6994534f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.2217570f), (Half)0.0793459f, (Half)0.6750000f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9138799f, -0.1355613f, -0.0561513f, 0.3785415f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.3603090f), (Half)(-0.3773989f), (Half)0.4772971f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9891765f, -0.1467305f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.4176992f), (Half)(-0.5665888f), (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9138799f, -0.1355613f, 0.0561513f, -0.3785415f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.3603090f), (Half)(-0.3773989f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.6994534f, 0.1037541f, -0.1037541f, 0.6994534f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.2217570f), (Half)0.0793459f, (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3785415f, 0.0561513f, -0.1355613f, 0.9138799f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.0832050f), (Half)0.5360908f, (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.2429802f, 0.9700313f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.5311047f, (Half)0.4945970f, (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3712149f, -0.0929845f, -0.2244844f, 0.8961920f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.4379082f, (Half)0.3202386f, (Half)0.4772971f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.6859157f, -0.1718129f, -0.1718129f, 0.6859157f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.2129119f, (Half)(-0.1006998f), (Half)0.6750000f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8961920f, -0.2244844f, -0.0929845f, 0.3712149f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.0120844f), (Half)(-0.5216383f), (Half)0.4772971f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9700313f, -0.2429802f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.1052809f), (Half)(-0.6959967f), (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8961920f, -0.2244844f, 0.0929845f, -0.3712149f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.0120844f), (Half)(-0.5216383f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.6859157f, 0.1718129f, -0.1718129f, 0.6859157f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.2129119f, (Half)(-0.1006998f), (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3712149f, 0.0929845f, -0.2244844f, 0.8961920f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.4379082f, (Half)0.3202386f, (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.3368899f, 0.9415441f);
            var pose = new RigidLocalPose(new MetreVector((Half)1.0323191f, (Half)0.1596963f, (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3603133f, -0.1289222f, -0.3112456f, 0.8698733f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.9068977f, (Half)0.0068698f, (Half)0.4772971f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.6657722f, -0.2382171f, -0.2382171f, 0.6657722f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.6041036f, (Half)(-0.3620858f), (Half)0.6750000f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8698733f, -0.3112456f, -0.1289222f, 0.3603133f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.3013096f, (Half)(-0.7310414f), (Half)0.4772971f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9415441f, -0.3368899f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.1758882f, (Half)(-0.8838678f), (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8698733f, -0.3112456f, 0.1289222f, -0.3603133f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.3013096f, (Half)(-0.7310414f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.6657722f, 0.2382171f, -0.2382171f, 0.6657722f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.6041036f, (Half)(-0.3620858f), (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3603133f, 0.1289222f, -0.3112456f, 0.8698733f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.9068977f, (Half)0.0068698f, (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, 0.0000000f, 1.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.9184402f), (Half)0.9826891f, (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimTbHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, 0.0000000f, 1.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.9184402f), (Half)(-0.6173109f), (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimTbHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, 0.0000000f, 1.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.9184402f), (Half)0.1826891f, (Half)(-0.8000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimLrHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, 0.0000000f, 1.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.9184402f), (Half)0.1826891f, (Half)0.8000000f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimLrHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.3826834f, 0.9238795f);
            var pose = new RigidLocalPose(new MetreVector((Half)1.3443015f, (Half)0.0454308f, (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimTbHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.3826834f, 0.9238795f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.2129306f, (Half)(-1.0859400f), (Half)0), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimTbHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.3826834f, 0.9238795f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.7786160f, (Half)(-0.5202546f), (Half)(-0.8000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimLrHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.3826834f, 0.9238795f);
            var pose = new RigidLocalPose(new MetreVector((Half)0.7786160f, (Half)(-0.5202546f), (Half)0.8000000f), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimLrHalfExtents));
        }
        return list.AsReadOnly();
    }

    private static IReadOnlyList<ExtractedCollider>? _pipeBend90Colliders;
    public static IReadOnlyList<ExtractedCollider> PipeBend90Colliders => _pipeBend90Colliders ??= CreatePipeBend90Colliders();

    private static ReadOnlyCollection<ExtractedCollider> CreatePipeBend90Colliders()
    {
        var list = new List<ExtractedCollider>(72);
        var wallHalfExtents = new MetreVector((Half)0.26, (Half)0.025, (Half)0.29);
        var rimTbHalfExtents = new MetreVector((Half)0.05, (Half)0.15, (Half)0.8);
        var rimLrHalfExtents = new MetreVector((Half)0.05, (Half)0.65, (Half)0.15);
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.0490677f, 0.9987955f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.3956536f), (Half)(1.3631368f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3822225f, -0.0187774f, -0.0453326f, 0.9227667f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.4150318f), (Half)(1.1663858f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.7062550f, -0.0346961f, -0.0346961f, 0.7062550f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.4618151f), (Half)(0.6913871f), (Half)(0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9227667f, -0.0453326f, -0.0187774f, 0.3822225f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.5085984f), (Half)(0.2163883f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9987955f, -0.0490677f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.5279767f), (Half)(0.0196374f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9227667f, -0.0453326f, 0.0187774f, -0.3822225f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.5085984f), (Half)(0.2163883f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.7062550f, 0.0346961f, -0.0346961f, 0.7062550f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.4618151f), (Half)(0.6913871f), (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3822225f, 0.0187774f, -0.0453326f, 0.9227667f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.4150318f), (Half)(1.1663858f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.1467305f, 0.9891765f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.8044309f), (Half)(1.2455353f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3785415f, -0.0561513f, -0.1355613f, 0.9138799f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.8618210f), (Half)(1.0563454f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.6994534f, -0.1037541f, -0.1037541f, 0.6994534f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.0003730f), (Half)(0.5996005f), (Half)(0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9138799f, -0.1355613f, -0.0561513f, 0.3785415f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.1389251f), (Half)(0.1428557f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9891765f, -0.1467305f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.1963152f), (Half)(-0.0463342f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9138799f, -0.1355613f, 0.0561513f, -0.3785415f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.1389251f), (Half)(0.1428557f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.6994534f, 0.1037541f, -0.1037541f, 0.6994534f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.0003730f), (Half)(0.5996005f), (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3785415f, 0.0561513f, -0.1355613f, 0.9138799f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.8618210f), (Half)(1.0563454f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.2429802f, 0.9700313f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.2475113f), (Half)(1.0148516f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3712149f, -0.0929845f, -0.2244844f, 0.8961920f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.3407078f), (Half)(0.8404932f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.6859157f, -0.1718129f, -0.1718129f, 0.6859157f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.5657041f), (Half)(0.4195548f), (Half)(0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8961920f, -0.2244844f, -0.0929845f, 0.3712149f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.7907004f), (Half)(-0.0013837f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9700313f, -0.2429802f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.8838969f), (Half)(-0.1757421f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8961920f, -0.2244844f, 0.0929845f, -0.3712149f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.7907004f), (Half)(-0.0013837f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.6859157f, 0.1718129f, -0.1718129f, 0.6859157f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.5657041f), (Half)(0.4195548f), (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3712149f, 0.0929845f, -0.2244844f, 0.8961920f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.3407078f), (Half)(0.8404932f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.3368899f, 0.9415441f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.2537031f), (Half)(0.6799509f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3603133f, -0.1289222f, -0.3112456f, 0.8698733f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.1282817f), (Half)(0.5271244f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.6657722f, -0.2382171f, -0.2382171f, 0.6657722f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.1745124f), (Half)(0.1581688f), (Half)(0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8698733f, -0.3112456f, -0.1289222f, 0.3603133f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.4773065f), (Half)(-0.2107868f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9415441f, -0.3368899f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.6027279f), (Half)(-0.3636132f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8698733f, -0.3112456f, 0.1289222f, -0.3603133f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.4773065f), (Half)(-0.2107868f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.6657722f, 0.2382171f, -0.2382171f, 0.6657722f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.1745124f), (Half)(0.1581688f), (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3603133f, 0.1289222f, -0.3112456f, 0.8698733f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.1282817f), (Half)(0.5271244f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.4275551f, 0.9039893f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.6799509f), (Half)(0.2537031f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3459417f, -0.1636183f, -0.3950094f, 0.8351772f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.5271244f), (Half)(0.1282817f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.6392170f, -0.3023271f, -0.3023271f, 0.6392170f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.1581688f), (Half)(-0.1745124f), (Half)(0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8351772f, -0.3950094f, -0.1636183f, 0.3459417f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.2107868f), (Half)(-0.4773065f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.9039893f, -0.4275551f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.3636132f), (Half)(-0.6027279f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8351772f, -0.3950094f, 0.1636183f, -0.3459417f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.2107868f), (Half)(-0.4773065f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.6392170f, 0.3023271f, -0.3023271f, 0.6392170f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.1581688f), (Half)(-0.1745124f), (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3459417f, 0.1636183f, -0.3950094f, 0.8351772f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.5271244f), (Half)(0.1282817f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.5141027f, 0.8577286f);
            var pose = new RigidLocalPose(new MetreVector((Half)(1.0148516f), (Half)(-0.2475113f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3282385f, -0.1967386f, -0.4749690f, 0.7924379f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.8404932f), (Half)(-0.3407078f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.6065057f, -0.3635255f, -0.3635255f, 0.6065057f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.4195548f), (Half)(-0.5657041f), (Half)(0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.7924379f, -0.4749690f, -0.1967386f, 0.3282385f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.0013837f), (Half)(-0.7907004f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8577286f, -0.5141027f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.1757421f), (Half)(-0.8838969f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.7924379f, -0.4749690f, 0.1967386f, -0.3282385f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.0013837f), (Half)(-0.7907004f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.6065057f, 0.3635255f, -0.3635255f, 0.6065057f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.4195548f), (Half)(-0.5657041f), (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3282385f, 0.1967386f, -0.4749690f, 0.7924379f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.8404932f), (Half)(-0.3407078f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.5956993f, 0.8032075f);
            var pose = new RigidLocalPose(new MetreVector((Half)(1.2455353f), (Half)(-0.8044309f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.3073742f, -0.2279643f, -0.5503544f, 0.7420670f);
            var pose = new RigidLocalPose(new MetreVector((Half)(1.0563454f), (Half)(-0.8618210f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.5679535f, -0.4212230f, -0.4212230f, 0.5679535f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.5996005f), (Half)(-1.0003730f), (Half)(0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.7420670f, -0.5503544f, -0.2279643f, 0.3073742f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.1428557f), (Half)(-1.1389251f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.8032075f, -0.5956993f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.0463342f), (Half)(-1.1963152f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.7420670f, -0.5503544f, 0.2279643f, -0.3073742f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.1428557f), (Half)(-1.1389251f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.5679535f, 0.4212230f, -0.4212230f, 0.5679535f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.5996005f), (Half)(-1.0003730f), (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.3073742f, 0.2279643f, -0.5503544f, 0.7420670f);
            var pose = new RigidLocalPose(new MetreVector((Half)(1.0563454f), (Half)(-0.8618210f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.0000000f, 0.0000000f, -0.6715590f, 0.7409511f);
            var pose = new RigidLocalPose(new MetreVector((Half)(1.3631368f), (Half)(-1.3956536f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.2835497f, -0.2569945f, -0.6204396f, 0.6845496f);
            var pose = new RigidLocalPose(new MetreVector((Half)(1.1663858f), (Half)(-1.4150318f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.5239316f, -0.4748639f, -0.4748639f, 0.5239316f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.6913871f), (Half)(-1.4618151f), (Half)(0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.6845496f, -0.6204396f, -0.2569945f, 0.2835497f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.2163883f), (Half)(-1.5085984f), (Half)(0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.7409511f, -0.6715590f, -0.0000000f, 0.0000000f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.0196374f), (Half)(-1.5279767f), (Half)(0.0000000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(0.6845496f, -0.6204396f, 0.2569945f, -0.2835497f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.2163883f), (Half)(-1.5085984f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.5239316f, 0.4748639f, -0.4748639f, 0.5239316f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.6913871f), (Half)(-1.4618151f), (Half)(-0.6750000f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var rot = CanonicalRotation.Normalize(-0.2835497f, 0.2569945f, -0.6204396f, 0.6845496f);
            var pose = new RigidLocalPose(new MetreVector((Half)(1.1663858f), (Half)(-1.4150318f), (Half)(-0.4772971f)), rot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, wallHalfExtents));
        }
        {
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.6970563f), (Half)(1.5029437f), (Half)0), CanonicalRotation.Identity);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimTbHalfExtents));
        }
        {
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.6970563f), (Half)(-0.0970563f), (Half)0), CanonicalRotation.Identity);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimTbHalfExtents));
        }
        {
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.6970563f), (Half)(0.7029437f), (Half)(-0.8f)), CanonicalRotation.Identity);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimLrHalfExtents));
        }
        {
            var pose = new RigidLocalPose(new MetreVector((Half)(-1.6970563f), (Half)(0.7029437f), (Half)0.8f), CanonicalRotation.Identity);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimLrHalfExtents));
        }
        {
            var endRot = CanonicalRotation.Normalize(0f, 0f, -0.7071068f, 0.7071068f);
            var pose = new RigidLocalPose(new MetreVector((Half)(1.5029437f), (Half)(-1.6970563f), (Half)0), endRot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimTbHalfExtents));
        }
        {
            var endRot = CanonicalRotation.Normalize(0f, 0f, -0.7071068f, 0.7071068f);
            var pose = new RigidLocalPose(new MetreVector((Half)(-0.0970563f), (Half)(-1.6970563f), (Half)0), endRot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimTbHalfExtents));
        }
        {
            var endRot = CanonicalRotation.Normalize(0f, 0f, -0.7071068f, 0.7071068f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.7029437f), (Half)(-1.6970563f), (Half)(-0.8f)), endRot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimLrHalfExtents));
        }
        {
            var endRot = CanonicalRotation.Normalize(0f, 0f, -0.7071068f, 0.7071068f);
            var pose = new RigidLocalPose(new MetreVector((Half)(0.7029437f), (Half)(-1.6970563f), (Half)0.8f), endRot);
            pose.Validate();
            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, rimLrHalfExtents));
        }

        return list.AsReadOnly();
    }

    private static ReadOnlyCollection<ExtractedCollider> CreateFunnelColliders()
    {
        var list = new List<ExtractedCollider>(72);

        const int Nx = 4;
        const int Ntheta = 16;
        const float L = 1.8f;
        const float rInlet = 1.3f;
        const float rOutlet = 0.65f;
        const float s = (rInlet - rOutlet) / L;
        var alpha = MathF.Atan(s);
        var cosAlphaFull = MathF.Cos(alpha);

        const float dx = L / Nx;
        const float lt = 0.485f;
        const float thickness = 0.05f;
        var halfThickAlongN = thickness * 0.5f;

        var wallHalfExtents = new[]
        {
            new MetreVector((Half)(lt * 0.5f), (Half)(thickness * 0.5f), (Half)(0.52f * 0.5f)),
            new MetreVector((Half)(lt * 0.5f), (Half)(thickness * 0.5f), (Half)(0.46f * 0.5f)),
            new MetreVector((Half)(lt * 0.5f), (Half)(thickness * 0.5f), (Half)(0.39f * 0.5f)),
            new MetreVector((Half)(lt * 0.5f), (Half)(thickness * 0.5f), (Half)(0.32f * 0.5f)),
        };

        var halfAlpha = alpha * 0.5f;
        var sinA = MathF.Sin(halfAlpha);
        var cosA = MathF.Cos(halfAlpha);

        for (var k = 0; k < Nx; k++)
        {
            var xk = -0.9f + (k + 0.5f) * dx;
            var rIn = 0.975f - s * xk;
            var rMid = rIn + halfThickAlongN / cosAlphaFull;
            var halfExtents = wallHalfExtents[k];

            for (var i = 0; i < Ntheta; i++)
            {
                var theta = i * (MathF.Tau / Ntheta);
                var halfTheta = theta * 0.5f;
                var sinT = MathF.Sin(halfTheta);
                var cosT = MathF.Cos(halfTheta);

                var ox = xk;
                var oy = rMid * MathF.Cos(theta);
                var oz = rMid * MathF.Sin(theta);

                var qx = sinT * cosA;
                var qy = sinT * sinA;
                var qz = -cosT * sinA;
                var qw = cosT * cosA;

                var rot = CanonicalRotation.Normalize(qx, qy, qz, qw);
                var pose = new RigidLocalPose(new MetreVector((Half)ox, (Half)oy, (Half)oz), rot);
                pose.Validate();
                list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, halfExtents));
            }
        }

        var rimStartTb = new MetreVector((Half)0.05f, (Half)0.15f, (Half)1.45f);
        var rimStartLr = new MetreVector((Half)0.05f, (Half)1.30f, (Half)0.15f);
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, new RigidLocalPose(new MetreVector((Half)(-0.9f), (Half)1.45f, (Half)0), CanonicalRotation.Identity), (Half)0, rimStartTb));
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, new RigidLocalPose(new MetreVector((Half)(-0.9f), (Half)(-1.45f), (Half)0), CanonicalRotation.Identity), (Half)0, rimStartTb));
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, new RigidLocalPose(new MetreVector((Half)(-0.9f), (Half)0, (Half)(-1.45f)), CanonicalRotation.Identity), (Half)0, rimStartLr));
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, new RigidLocalPose(new MetreVector((Half)(-0.9f), (Half)0, (Half)1.45f), CanonicalRotation.Identity), (Half)0, rimStartLr));

        var rimEndTb = new MetreVector((Half)0.05f, (Half)0.15f, (Half)0.8f);
        var rimEndLr = new MetreVector((Half)0.05f, (Half)0.65f, (Half)0.15f);
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, new RigidLocalPose(new MetreVector((Half)0.9f, (Half)0.8f, (Half)0), CanonicalRotation.Identity), (Half)0, rimEndTb));
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, new RigidLocalPose(new MetreVector((Half)0.9f, (Half)(-0.8f), (Half)0), CanonicalRotation.Identity), (Half)0, rimEndTb));
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, new RigidLocalPose(new MetreVector((Half)0.9f, (Half)0, (Half)(-0.8f)), CanonicalRotation.Identity), (Half)0, rimEndLr));
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, new RigidLocalPose(new MetreVector((Half)0.9f, (Half)0, (Half)0.8f), CanonicalRotation.Identity), (Half)0, rimEndLr));

        return list.AsReadOnly();
    }

    private static ReadOnlyCollection<ExtractedCollider> CreatePoweredGateColliders()
    {
        var list = new List<ExtractedCollider>(19);

        const float rMid = 0.675f;
        var circumferenceHalfExtents = new MetreVector((Half)0.4f, (Half)0.025f, (Half)0.14f);

        for (var i = 0; i < 16; i++)
        {
            var theta = i * (MathF.Tau / 16f);
            var cos = MathF.Cos(theta);
            var sin = MathF.Sin(theta);

            var y = rMid * cos;
            var z = rMid * sin;

            var halfTheta = theta * 0.5f;
            var rot = CanonicalRotation.Normalize(MathF.Sin(halfTheta), 0, 0, MathF.Cos(halfTheta));

            var pose = new RigidLocalPose(
                new MetreVector((Half)0, (Half)y, (Half)z),
                rot);
            pose.Validate();

            list.Add(new ExtractedCollider(ColliderShapeKind.Box, pose, (Half)0, circumferenceHalfExtents));
        }

        var housingHalfExtents = new MetreVector((Half)0.4f, (Half)0.325f, (Half)0.9f);
        var housingPose = new RigidLocalPose(new MetreVector((Half)0, (Half)1.65f, (Half)0), CanonicalRotation.Identity);
        housingPose.Validate();
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, housingPose, (Half)0, housingHalfExtents));

        var railHalfExtents = new MetreVector((Half)0.12f, (Half)0.85f, (Half)0.06f);
        var railStartPose = new RigidLocalPose(new MetreVector((Half)0, (Half)0.65f, (Half)(-0.82f)), CanonicalRotation.Identity);
        railStartPose.Validate();
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, railStartPose, (Half)0, railHalfExtents));

        var railEndPose = new RigidLocalPose(new MetreVector((Half)0, (Half)0.65f, (Half)0.82f), CanonicalRotation.Identity);
        railEndPose.Validate();
        list.Add(new ExtractedCollider(ColliderShapeKind.Box, railEndPose, (Half)0, railHalfExtents));

        return list.AsReadOnly();
    }

    private static ReadOnlyCollection<ExtractedCollider> CreatePressurePlateColliders()
    {
        var basePose = new RigidLocalPose(new MetreVector((Half)0, (Half)(-0.12f), (Half)0), CanonicalRotation.Identity);
        basePose.Validate();
        var topPose = new RigidLocalPose(new MetreVector((Half)0, (Half)0.03f, (Half)0), CanonicalRotation.Identity);
        topPose.Validate();

        return new List<ExtractedCollider>(2)
        {
            new(ColliderShapeKind.Box, basePose, (Half)0, new MetreVector((Half)1.1f, (Half)0.1f, (Half)1.1f)),
            new(ColliderShapeKind.Box, topPose, (Half)0, new MetreVector((Half)0.9f, (Half)0.05f, (Half)0.9f))
        }.AsReadOnly();
    }
}
