namespace CuriousContraptions.E2eRunner;

/// <summary>The suite-source boundary tables: the driver's selectTool kinds and the driver methods that exercise a Workshop area.
/// They are the only place suite source strings become typed parts and areas (TimingDeclarationTests (e)).</summary>
internal static class DriverVocabulary
{
    /// <summary>workshop-driver.ts selectTool / stackUnderLiftedBall kinds to their part. The driver's kinds basketball and
    /// receiver differ from their PartId values (ball, basket).</summary>
    public static readonly IReadOnlyDictionary<string, PartId> ToolKinds = new Dictionary<string, PartId>(StringComparer.Ordinal)
    {
        ["basketball"] = PartId.Basketball, ["bowling"] = PartId.BowlingBall, ["wall"] = PartId.Wall, ["ramp"] = PartId.Ramp,
        ["bumper"] = PartId.PinballBumper, ["switch"] = PartId.ImpactSwitch, ["delay"] = PartId.Delay, ["lamp"] = PartId.SignalLamp,
        ["receiver"] = PartId.Receiver, ["domino"] = PartId.Domino, ["battery"] = PartId.Battery, ["pipe"] = PartId.Pipe,
        ["ball_detector"] = PartId.BallDetector, ["pipe_bend_45"] = PartId.PipeBend45, ["pipe_bend_90"] = PartId.PipeBend90,
        ["funnel"] = PartId.Funnel, ["powered_gate"] = PartId.PoweredGate, ["pressure_plate"] = PartId.PressurePlate,
        ["counter"] = PartId.Counter, ["clock"] = PartId.Clock, ["latch"] = PartId.Latch,
    };

    /// <summary>The driver calls whose string-literal arguments name parts (through <see cref="ToolKinds"/>).</summary>
    public static readonly IReadOnlyList<string> PlacingMethods = ["selectTool", "stackUnderLiftedBall"];

    /// <summary>The driver call whose string-literal arguments name levels.</summary>
    public const string LevelMethod = "selectLevel";

    /// <summary>Driver methods that exercise one area beyond placement. selectTool and placeOnCanvas are used by every suite, so the
    /// Palette and BenchPlacement areas are declared only by palette-placement, the suite that proves every palette part.</summary>
    public static readonly IReadOnlyDictionary<string, WorkshopArea> MethodAreas = new Dictionary<string, WorkshopArea>(StringComparer.Ordinal)
    {
        ["selectPartAt"] = WorkshopArea.Gizmo, ["commitAt"] = WorkshopArea.Gizmo, ["setPartMode"] = WorkshopArea.Gizmo,
        ["liftSelectedPart"] = WorkshopArea.Gizmo, ["stackUnderLiftedBall"] = WorkshopArea.Gizmo, ["tiltSelectedRamp"] = WorkshopArea.Gizmo,
        ["tiltSelectedWall"] = WorkshopArea.Gizmo, ["resizeSelectedWall"] = WorkshopArea.Gizmo,
        ["tiltSelectedByFineRotate"] = WorkshopArea.Guidance, ["showHint"] = WorkshopArea.Guidance,
        ["adjustPreview"] = WorkshopArea.Guidance, ["liftSelectedByHeight"] = WorkshopArea.Guidance,
        ["dragPart"] = WorkshopArea.Gizmo, ["dragPartRefused"] = WorkshopArea.Gizmo,
        ["connectActivation"] = WorkshopArea.Connections,
        ["selectLevel"] = WorkshopArea.LevelPicker, ["openMenu"] = WorkshopArea.LevelPicker, ["closeMenu"] = WorkshopArea.LevelPicker,
        ["readCaptured"] = WorkshopArea.LevelPicker, ["waitForGoalSolved"] = WorkshopArea.LevelPicker,
        ["isAnimationQualified"] = WorkshopArea.Presentation, ["readAnimationSampleCount"] = WorkshopArea.Presentation,
        ["readLastAnimationSample"] = WorkshopArea.Presentation, ["readAllAnimationSamples"] = WorkshopArea.Presentation,
        ["readAnimationSamplesForTarget"] = WorkshopArea.Presentation,
    };
}
