using CuriousContraptions;
using System.Text.Json;

namespace CuriousContraptions.Authoring;

/// <summary>Typed authoring for the introductory finite-energy launcher lessons.</summary>
internal static class WoundSpringLesson
{
    private enum Role { Launcher, Payload, Battery, Motor, Trigger, Striker, Delay, Receiver, HoldTimer, ReleaseDelay }
    private enum CatalogPart { WoundSpring, Ball, Battery, Motor, Switch, Delay, Basket, HoldTimer }

    // These mappings are the campaign serialization boundary, not runtime selectors.
    private static string WireId(Role role)
    {
        if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role));
        return JsonNamingPolicy.SnakeCaseLower.ConvertName(role.ToString());
    }
    private static string CatalogId(CatalogPart part) => part switch
    {
        CatalogPart.WoundSpring => "wound_spring",
        CatalogPart.Ball => "ball",
        CatalogPart.Battery => "battery",
        CatalogPart.Motor => "motor",
        CatalogPart.Switch => "switch",
        CatalogPart.Delay => "delay",
        CatalogPart.Basket => "basket",
        CatalogPart.HoldTimer => "hold_timer",
        _ => throw new ArgumentOutOfRangeException(nameof(part))
    };

    private static PartSpec Part(Role role, CatalogPart kind, float[] position, bool locked = true,
        float[]? rotation = null) => new()
    {
        Id = WireId(role), Kind = CatalogId(kind), Position = position,
        Locked = locked, Orientation = PartOrientation.FromEulerDegrees(rotation ?? [0, 0, 0]),
        Difficulty =
        [
            new() { Precision = 0, PositionWindow = .2f, RotationWindow = 5,
                MaxPositionCorrection = locked ? 0 : .2f, MaxRotationCorrection = locked ? 0 : 5,
                BlendSeconds = .4f, CaptureMargin = .3f, CaptureSpeed = 3, CaptureDwell = .15f,
                TriggerThreshold = .2f },
            new() { Precision = .45f, PositionWindow = .1f, RotationWindow = 2,
                MaxPositionCorrection = locked ? 0 : .1f, MaxRotationCorrection = locked ? 0 : 2,
                BlendSeconds = .4f, CaptureMargin = .174f, CaptureSpeed = 2.325f, CaptureDwell = .24f,
                TriggerThreshold = .47f },
            new() { Precision = 1 }
        ]
    };

    private static ConnectionSpec Link(Role from, Role to, ConnectionDomain domain,
        SocketId output, SocketId input) => new()
    {
        From = WireId(from), To = WireId(to), Type = domain, FromPort = output, ToPort = input
    };

    public static PuzzleData Create(int number) => new()
    {
        Id = "wind_then_release",
        Title = "Wind, then release",
        Subtitle = $"{number} / Stored energy",
        Description = "Wind the spring, then release it to send the orange ball into the basket.",
        Hint = "Place the spring beneath the orange ball, leaning slightly toward the basket. Wire battery to motor, belt motor to spring, and connect switch to delay to spring. The signal releases energy; it does not supply it.",
        Inventory = new() { [CatalogId(CatalogPart.WoundSpring)] = 1 },
        Parts =
        [
            Part(Role.Payload, CatalogPart.Ball, [.4063459f, 4.5165035f, 0]),
            Part(Role.Battery, CatalogPart.Battery, [-4, 3, 2]),
            Part(Role.Motor, CatalogPart.Motor, [-3, 5, 2]),
            Part(Role.Trigger, CatalogPart.Switch, [4, 2, -2]),
            Part(Role.Striker, CatalogPart.Ball, [4, 7, -2]),
            Part(Role.Delay, CatalogPart.Delay, [-3, 6, -2]),
            Part(Role.Receiver, CatalogPart.Basket, [3.6f, 1.2f, 0])
        ],
        Solution = [Part(Role.Launcher, CatalogPart.WoundSpring, [0, 3, 0], false, [0, 0, -15])],
        SolutionConnections =
        [
            Link(Role.Battery, Role.Motor, ConnectionDomain.Electrical, SocketId.Supply, SocketId.PowerIn),
            Link(Role.Motor, Role.Launcher, ConnectionDomain.Mechanical, SocketId.Drive, SocketId.DriveIn),
            Link(Role.Trigger, Role.Delay, ConnectionDomain.Activation, SocketId.ActivationOut, SocketId.ActivationIn),
            Link(Role.Delay, Role.Launcher, ConnectionDomain.Activation, SocketId.ActivationOut, SocketId.ActivationIn)
        ],
        Goals = [new() { Type = GoalKind.Captured, Target = WireId(Role.Receiver), Body = WireId(Role.Payload) }]
    };
    public static PuzzleData CreateRetained(int number)
    {
        var puzzle = Create(number);
        puzzle.Id = "saved_for_later";
        puzzle.Title = "Saved for later";
        puzzle.Description = "Use a brief burst of motor power to wind the spring. Its stored energy can deliver the ball after the motor stops.";
        puzzle.Hint = "Put a hold timer between battery and motor. Connect the first delay to both the timer and the three-second delay; connect the latter to the spring. Belt motor to spring. Watch the motor stop before the latch opens.";
        puzzle.Inventory = new() { [CatalogId(CatalogPart.HoldTimer)] = 1 };
        puzzle.Parts.Add(Part(Role.Launcher, CatalogPart.WoundSpring, [0, 3, 0], true, [0, 0, -15]));
        var releaseDelay = Part(Role.ReleaseDelay, CatalogPart.Delay, [3, 6, 2]);
        releaseDelay.Properties[PartParameterName.Of(DelayParameter.DelaySeconds)] = 3;
        puzzle.Parts.Add(releaseDelay);
        puzzle.Solution = [Part(Role.HoldTimer, CatalogPart.HoldTimer, [-5, 6, 0], false)];
        puzzle.SolutionConnections =
        [
            Link(Role.Battery, Role.HoldTimer, ConnectionDomain.Electrical, SocketId.Supply, SocketId.PowerIn),
            Link(Role.HoldTimer, Role.Motor, ConnectionDomain.Electrical, SocketId.Supply, SocketId.PowerIn),
            Link(Role.Motor, Role.Launcher, ConnectionDomain.Mechanical, SocketId.Drive, SocketId.DriveIn),
            Link(Role.Trigger, Role.Delay, ConnectionDomain.Activation, SocketId.ActivationOut, SocketId.ActivationIn),
            Link(Role.Delay, Role.HoldTimer, ConnectionDomain.Activation, SocketId.ActivationOut, SocketId.ActivationIn),
            Link(Role.Delay, Role.ReleaseDelay, ConnectionDomain.Activation, SocketId.ActivationOut, SocketId.ActivationIn),
            Link(Role.ReleaseDelay, Role.Launcher, ConnectionDomain.Activation, SocketId.ActivationOut, SocketId.ActivationIn)
        ];
        return puzzle;
    }
}
