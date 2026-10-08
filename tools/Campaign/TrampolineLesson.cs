using CuriousContraptions;

namespace CuriousContraptions.Authoring;

/// <summary>Single-distinction passive rebound lesson with bounded author-defined placement assistance.</summary>
internal static class TrampolineLesson
{
    private enum Role { Bed, Payload, Receiver }
    // Explicit serialization boundary for authored instance and catalog identities.
    private static string Id(Role role) => role switch
    {
        Role.Bed => "bed", Role.Payload => "payload", Role.Receiver => "receiver",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Kind(Role role) => role switch
    {
        Role.Bed => "trampoline", Role.Payload => "ball", Role.Receiver => "basket",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static PartSpec Part(Role role, float[] position, float[] rotation) => new()
    {
        Id = Id(role), Kind = Kind(role), Position = position, Orientation = PartOrientation.FromEulerDegrees(rotation),
        Locked = role != Role.Bed,
        Difficulty =
        [
            new() { Precision = 0, PositionWindow = .2f, RotationWindow = 5,
                MaxPositionCorrection = role == Role.Bed ? .2f : 0,
                MaxRotationCorrection = role == Role.Bed ? 5 : 0,
                BlendSeconds = .4f, CaptureMargin = .3f, CaptureSpeed = 3, CaptureDwell = .15f },
            new() { Precision = .45f, PositionWindow = .1f, RotationWindow = 2,
                MaxPositionCorrection = role == Role.Bed ? .1f : 0,
                MaxRotationCorrection = role == Role.Bed ? 2 : 0,
                BlendSeconds = .4f, CaptureMargin = .174f, CaptureSpeed = 2.325f, CaptureDwell = .24f },
            new() { Precision = 1 }
        ]
    };
    public static PuzzleData Create(int number) => new()
    {
        Id = "a_gentle_rebound",
        Title = "A gentle rebound",
        Subtitle = $"{number} / Elastic surfaces",
        Description = "Bounce the orange ball into the basket with one trampoline.",
        Hint = "Place the trampoline beneath the ball and tilt its surface toward the basket. It returns some of the falling ball's energy; it needs no motor or trigger.",
        Inventory = new() { [Kind(Role.Bed)] = 1 },
        Parts =
        [
            Part(Role.Payload, [0, 7, 0], [0, 0, 0]),
            Part(Role.Receiver, [5.4f, 1.2f, 0], [0, 0, 0])
        ],
        Solution = [Part(Role.Bed, [0, 3, 0], [0, 0, -30])],
        Goals = [new() { Type = GoalKind.Captured, Target = Id(Role.Receiver), Body = Id(Role.Payload) }]
    };
}
