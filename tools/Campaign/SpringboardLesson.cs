using CuriousContraptions;

namespace CuriousContraptions.Authoring;

/// <summary>Passive springboard introduction, authored from the finite elastic plate model.</summary>
internal static class SpringboardLesson
{
    private enum Role { Ball, Receiver, Spring, Catcher }
    private static string Id(Role role) => role switch
    {
        Role.Ball => "ball", Role.Receiver => "receiver", Role.Spring => "spring_1", Role.Catcher => "basket_1",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string CatalogId(Role role) => role switch
    {
        Role.Ball => "ball", Role.Receiver => "basket", Role.Spring => "spring", Role.Catcher => "basket",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static PartSpec Part(Role role, float[] position) => new()
    {
        Id = Id(role), Kind = CatalogId(role), Position = position,
        Locked = role != Role.Spring,
        Orientation = PartOrientation.FromEulerDegrees(role == Role.Spring ? [0, 0, -20] : [0, 0, 0])
    };
    public static PuzzleData Create() => new()
    {
        Id = "spring_forward",
        Title = "Spring forward",
        Subtitle = "04 / Stored energy",
        Description = "Catch the falling ball's energy in a springboard and return it toward the basket.",
        Hint = "Place the springboard below the ball and tilt it clockwise. Compression stores some of the fall's energy; release sends the ball toward the lower basket.",
        Inventory = new() { [CatalogId(Role.Spring)] = 1 },
        Parts = [Part(Role.Ball, [-3, 3.6f, 0]), Part(Role.Receiver, [-.7f, .55f, 0])],
        Solution = [Part(Role.Spring, [-3, .8f, 0])],
        Goals = [new() { Type = GoalKind.Captured, Target = Id(Role.Receiver), Body = Id(Role.Ball) }]
    };

    public static PuzzleData CreatePrecharged(int number)
    {
        var spring = Part(Role.Spring, [0, 2.5f, 0]);
        spring.Locked = true;
        spring.Orientation = PartOrientation.FromEulerDegrees(0, 0, -30);
        spring.Properties[PartParameterName.Of(SpringParameter.InitialCompression)] = .2f;
        // Authored payload clearance above the precompressed plate, not a runtime launch rule.
        var offset = spring.Orientation.Transform(new(0, .357f, 0));
        var ball = Part(Role.Ball, [offset.X, 2.5f + offset.Y, offset.Z]);
        var receiver = Part(Role.Catcher, [1.8f, 1.3f, 0]);
        receiver.Locked = false;
        foreach (var part in new[] { spring, ball, receiver })
            part.Difficulty =
            [
                new() { Precision = 0, PositionWindow = .2f, RotationWindow = 5,
                    MaxPositionCorrection = part.Locked ? 0 : .2f,
                    MaxRotationCorrection = part.Locked ? 0 : 5,
                    BlendSeconds = .4f, CaptureMargin = .3f, CaptureSpeed = 3, CaptureDwell = .15f },
                new() { Precision = .45f, PositionWindow = .1f, RotationWindow = 2,
                    MaxPositionCorrection = part.Locked ? 0 : .1f,
                    MaxRotationCorrection = part.Locked ? 0 : 2,
                    BlendSeconds = .4f, CaptureMargin = .174f, CaptureSpeed = 2.325f, CaptureDwell = .24f },
                new() { Precision = 1 }
            ];
        return new()
        {
            Id = "ready_to_rebound",
            Title = "Ready to rebound",
            Subtitle = $"{number} / Energy already stored",
            Description = "This springboard is already compressed. Place a basket to catch the ball when its stored energy is released.",
            Hint = "Watch the ball's short upward arc, then put the basket beneath its descent. The spring releases on Run; it needs no wire and does not recharge itself.",
            Inventory = new() { [CatalogId(Role.Receiver)] = 1 },
            Parts = [spring, ball],
            Solution = [receiver],
            Goals = [new() { Type = GoalKind.Captured, Target = Id(Role.Catcher), Body = Id(Role.Ball) }]
        };
    }
}
