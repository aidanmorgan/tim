using CuriousContraptions;
using System.Text.Json;

// Emits authored campaign data to stdout; never overwrites project files.
// The first five tutorial definitions are the independent building blocks.
var source = MachineCodec.ReadPuzzles(File.ReadAllText(args.Length > 0 ? args[0] : "content/puzzles.json")).Take(5).ToList();
if (source.Count != 5) throw new InvalidDataException("Five tutorial definitions are required.");
PuzzleData Copy(PuzzleData puzzle) => JsonSerializer.Deserialize(
    JsonSerializer.Serialize(new List<PuzzleData> { puzzle }, MachineJson.Default.ListPuzzleData),
    MachineJson.Default.ListPuzzleData)![0];
var modules = new Dictionary<string, PuzzleData>
{
    ["ramps"] = Copy(source[0]), ["power"] = Copy(source[1]),
    ["air"] = Copy(source[2]), ["spring"] = Copy(source[3])
};
// Air-mail's extra ramp is a tutorial alternative, not required by composed levels.
modules["air"].Inventory.Remove("ramp");

var domino = new PuzzleData
{
    Parts = [
        new() { Id = "ball", Kind = "bowling", Position = [-2.4f, 3, 0], Locked = true },
        new() { Id = "end", Kind = "domino", Position = [.8f, 1, 0], Locked = true }
    ],
    Inventory = new() { ["domino"] = 4 },
    Goals = [new() { Type = "activated", Target = "end" }]
};
for (var i = 0; i < 4; i++)
    domino.Solution.Add(new() { Id = "domino_" + i, Kind = "domino", Position = [-2.4f + i * .8f, 1, 0] });
modules["domino"] = domino;

modules["conveyor"] = new()
{
    Parts =
    [
        new() { Id = "ball", Kind = "ball", Position = [-3, 5, 0], Locked = true },
        new() { Id = "receiver", Kind = "basket", Position = [2, .9f, 0], Locked = true }
    ],
    Inventory = new() { ["conveyor"] = 1 },
    Solution = [new() { Id = "conveyor_1", Kind = "conveyor", Position = [-2, 2.5f, 0] }],
    Goals = [new() { Type = "captured", Target = "receiver", Body = "ball" }]
};

foreach (var name in new[] { "ramps", "spring", "air", "conveyor" })
{
    var module = Copy(modules[name]);
    var receiver = module.Parts.Single(p => p.Id == "receiver");
    receiver.Kind = "switch";
    if (name == "ramps") receiver.Position[0] = 3.2f;
    module.Parts.Add(new() { Id = "lamp", Kind = "lamp", Position = [4.5f, 1, 0], Locked = true });
    module.Goals = [new() { Type = "activated", Target = "lamp" }];
    module.SolutionConnections.Add(new() { From = "receiver", To = "lamp" });
    modules[name + "_signal"] = module;
}
var gated = Copy(modules["air"]);
var fixedFan = gated.Solution.Single(p => p.Kind == "fan");
gated.Solution.Clear();
gated.Inventory = new() { ["switch"] = 1 };
fixedFan.Locked = true;
fixedFan.Properties["powered"] = 0;
gated.Parts.Add(fixedFan);
gated.Parts.Add(new() { Id = "trigger", Kind = "bowling", Position = [-5.8f, 2.4f, 0], Locked = true });
gated.Solution.Add(new() { Id = "switch_1", Kind = "switch", Position = [-5.8f, 1.6f, 0] });
gated.SolutionConnections = [new() { From = "switch_1", To = fixedFan.Id }];
gated.Goals.Add(new() { Type = "activated", Target = fixedFan.Id });
modules["gated_air"] = gated;
var gatedBelt = Copy(modules["conveyor"]);
var fixedBelt = gatedBelt.Solution.Single();
gatedBelt.Solution.Clear();
gatedBelt.Inventory = new() { ["switch"] = 1 };
fixedBelt.Locked = true;
fixedBelt.Properties["powered"] = 0;
gatedBelt.Parts.Add(fixedBelt);
gatedBelt.Parts.Add(new() { Id = "trigger", Kind = "bowling", Position = [-5.8f, 2.4f, 0], Locked = true });
gatedBelt.Solution.Add(new() { Id = "switch_1", Kind = "switch", Position = [-5.8f, 1.6f, 0] });
gatedBelt.SolutionConnections = [new() { From = "switch_1", To = fixedBelt.Id }];
gatedBelt.Goals.Add(new() { Type = "activated", Target = fixedBelt.Id });
modules["gated_belt"] = gatedBelt;

var campaign = source.Select(Copy).ToList();
void Add(string id, string title, string chapter, string description, string hint,
         params (string Kind, float X, float Z, float Yaw)[] layout)
{
    var puzzle = new PuzzleData
    {
        Id = id, Title = title, Subtitle = $"{campaign.Count + 1:00} / {chapter}",
        Description = description, Hint = hint
    };
    for (var i = 0; i < layout.Length; i++)
    {
        var (kind, x, z, yaw) = layout[i];
        var module = Copy(modules[kind]);
        var prefix = layout.Length == 1 ? "" : $"lane{i + 1}_";
        foreach (var part in module.Parts.Concat(module.Solution))
        {
            var radians = yaw * MathF.PI / 180;
            var px = part.Position[0];
            var pz = part.Position[2];
            part.Position[0] = MathF.Round(x + px * MathF.Cos(radians) + pz * MathF.Sin(radians), 4);
            part.Position[2] = MathF.Round(z - px * MathF.Sin(radians) + pz * MathF.Cos(radians), 4);
            part.Rotation[1] += yaw;
            part.Id = prefix + part.Id;
        }
        foreach (var goal in module.Goals)
        {
            goal.Target = prefix + goal.Target;
            if (goal.Body != "") goal.Body = prefix + goal.Body;
        }
        foreach (var link in module.SolutionConnections)
        { link.From = prefix + link.From; link.To = prefix + link.To; }
        puzzle.Parts.AddRange(module.Parts);
        puzzle.Solution.AddRange(module.Solution);
        puzzle.Goals.AddRange(module.Goals);
        puzzle.SolutionConnections.AddRange(module.SolutionConnections);
        foreach (var (key, count) in module.Inventory)
            puzzle.Inventory[key] = puzzle.Inventory.GetValueOrDefault(key) + count;
    }
    campaign.Add(puzzle);
}
// Chapter 2 introduces propagation and turns familiar motion into signals.
Add("domino_effect", "The domino effect", "Chain reactions",
    "Make the fixed end domino fall. Fill the gap with four dominoes.",
    "Start under the bowling ball. Neighbours must be less than a metre apart.", ("domino", 0, 0, 0));
Add("conveyor_courier", "Conveyor courier", "Moving surfaces",
    "Carry the orange ball toward the receiver on a moving belt.",
    "Place a horizontal conveyor beneath the ball. The gold arrow shows which end it will leave.", ("conveyor", 0, 0, 0));
Add("spring_signal", "Spring-loaded signal", "Motion into power",
    "Launch the ball onto the elevated switch and light the lamp.",
    "Tilt the spring clockwise. Remember the wire from switch to lamp.", ("spring_signal", 0, 0, 0));
Add("wind_signal", "A breath of electricity", "Motion into power",
    "Use moving air to strike the switch and power the lamp.",
    "The fan's height controls how long the ball receives a push.", ("air_signal", 0, 0, 0));
Add("cold_start", "Cold start", "Causal machines",
    "The fan starts off. Use the bowling ball to power it before the tennis ball falls past.",
    "A switch close beneath the bowling ball starts the fan early. Connect it to the fixed fan.", ("gated_air", 0, 0, 0));

// Chapters 3 and 4: independent goals, mixed mechanisms, and power dependencies.
Add("two_deliveries", "Two deliveries", "Parallel machines",
    "Catch both balls, one with ramps and one with air. The receivers are in separate depth planes.",
    "Work on one depth plane at a time; both goals must finish in the same run.", ("ramps", 0, -2, 0), ("air", 0, 2, 0));
Add("bounce_and_roll", "Bounce and roll", "Parallel machines",
    "Complete the ramp route and the elevated spring catch.",
    "The spring route and ramp route use different heights and depth planes.", ("ramps", 0, -2, 0), ("spring", 0, 2, 0));
Add("air_and_belt", "Air and belt", "Parallel machines",
    "Deliver one ball with air and carry the other on a conveyor.",
    "The fan acts at a distance; the conveyor only transports a ball touching its top.", ("air", 0, -2, 0), ("conveyor", 0, 2, 0));
Add("catch_and_signal", "Catch and signal", "Parallel machines",
    "Catch the orange ball and activate the lamp in the other plane.",
    "Use two ramps for the catch; place and wire the switch under the bowling ball.", ("ramps", 0, -2, 0), ("power", 0, 2, 0));
Add("wind_and_dominoes", "Wind and dominoes", "Parallel machines",
    "Catch the tennis ball and topple the end domino.",
    "The four dominoes bridge one plane while the fan works in the other.", ("air", 0, -2, 0), ("domino", 0, 2, 0));
Add("powered_post", "Powered post", "Dependencies",
    "Start the cold fan, deliver its ball, and complete the separate ramp route.",
    "Place the power switch near its trigger ball so air is ready in time.", ("gated_air", 0, -2, 0), ("ramps", 0, 2, 0));
Add("spring_and_chain", "Spring and chain", "Dependencies",
    "Land the spring-launched ball and finish the domino chain.",
    "The spring needs a tilt; the dominoes need close spacing.", ("spring", 0, -2, 0), ("domino", 0, 2, 0));
Add("two_signals", "Two signals", "Dependencies",
    "Light both lamps, using a ramp path and a spring path.",
    "Every fixed switch must be wired to a lamp; completing only one is not enough.", ("ramps_signal", 0, -2, 0), ("spring_signal", 0, 2, 0));
Add("mixed_signals", "Belt and signal", "Dependencies",
    "Deliver the conveyor ball while air drives the other ball onto a switch.",
    "Separate capture from activation: one goal needs a settled ball, the other an impact.", ("conveyor", 0, -2, 0), ("air_signal", 0, 2, 0));
Add("start_and_topple", "Start and topple", "Dependencies",
    "Power the cold fan, catch its ball, and finish the domino chain.",
    "The fan needs a wire; the dominoes propagate through proximity, without wires.", ("gated_air", 0, -2, 0), ("domino", 0, 2, 0));

// Chapter 5 rotates entire machines so trajectories genuinely travel through Z.
Add("deep_routes", "Deep routes", "Spatial reasoning",
    "Guide two balls along depth-oriented ramp routes.",
    "Orbit the camera. Turn the ramps 90 degrees, then tilt them along their local slope.", ("ramps", -2, 0, 90), ("ramps", 2, 0, -90));
Add("cross_breezes", "Cross breezes", "Spatial reasoning",
    "Two fans must send their balls in opposite depth directions.",
    "Use A/D to turn each fan. Keep each gust in its own X lane.", ("air", -2, 0, 90), ("air", 2, 0, -90));
Add("deep_springs", "Deep springs", "Spatial reasoning",
    "Launch two balls through depth into elevated receivers.",
    "Rotate each spring around Y, then use Q/E to set its launch tilt.", ("spring", -2, 0, 90), ("spring", 2, 0, -90));
Add("spatial_signals", "Spatial signals", "Spatial reasoning",
    "Send a rolling ball and an airborne ball onto switches in depth-oriented lanes.",
    "Both switches need wiring. Watch the goal positions while orbiting.", ("ramps_signal", -2, 0, 90), ("air_signal", 2, 0, -90));
Add("three_deliveries", "Three deliveries", "Three-stage workshop",
    "Complete three captures with ramps, air, and a spring.",
    "Use depth planes -3, 0, and +3; tune each route before combining the run.", ("ramps", 0, -3, 0), ("air", 0, 0, 0), ("spring", 0, 3, 0));

// Chapters 6–8 combine all mechanisms with increasing placement and wiring load.
Add("triple_signal", "Triple signal", "Three-stage workshop",
    "Light three lamps using ramps, a spring, and air.",
    "Each fixed switch needs a cable. The ball paths occupy three depth planes.", ("ramps_signal", 0, -3, 0), ("spring_signal", 0, 0, 0), ("air_signal", 0, 3, 0));
Add("cold_front", "Cold front", "Three-stage workshop",
    "Start a cold fan while a ramp route and a spring route deliver their balls.",
    "The powered lane has two required events: activation and capture.", ("gated_air", 0, -3, 0), ("ramps", 0, 0, 0), ("spring", 0, 3, 0));
Add("chain_mail", "Chain mail", "Three-stage workshop",
    "Catch the wind-driven ball, deliver the conveyor ball, and topple the end domino.",
    "Dominoes need a continuous bridge; the other parts belong to the two delivery lanes.", ("air", 0, -3, 0), ("conveyor", 0, 0, 0), ("domino", 0, 3, 0));
Add("bounce_mail", "Bounce mail", "Three-stage workshop",
    "Combine a spring catch, a wind-triggered lamp, and a domino chain.",
    "The three goals use capture, electrical activation, and physical propagation.", ("spring", 0, -3, 0), ("air_signal", 0, 0, 0), ("domino", 0, 3, 0));
Add("relay_workshop", "Relay workshop", "Three-stage workshop",
    "Start the cold fan, light the ramp-triggered lamp, and complete the chain.",
    "Solve the early power trigger first, then the ramp geometry, then domino spacing.", ("gated_air", 0, -3, 0), ("ramps_signal", 0, 0, 0), ("domino", 0, 3, 0));
Add("double_cold_start", "Double cold start", "Master workshop",
    "Start a stopped conveyor and a cold fan, then finish a spring catch in a third lane.",
    "Wire one trigger switch to the belt and another to the fan. The fan needs power before its ball falls past.", ("gated_belt", 0, -3, 0), ("spring", 0, 0, 0), ("gated_air", 0, 3, 0));
Add("triple_chain", "Triple chain", "Master workshop",
    "Finish three separate domino chains with twelve movable dominoes.",
    "Keep every gap below one metre and keep the three chains in their own depth planes.", ("domino", 0, -3, 0), ("domino", 0, 0, 0), ("domino", 0, 3, 0));
Add("signals_and_chain", "Signals and chain", "Master workshop",
    "Light the ramp and spring lamps while a third lane topples its end domino.",
    "Do not confuse the fixed switches with receivers: impact is enough, but wiring is required.", ("ramps_signal", 0, -3, 0), ("spring_signal", 0, 0, 0), ("domino", 0, 3, 0));
Add("cold_signals", "Cold signals", "Master workshop",
    "Power the fan and capture its ball, then light both motion-triggered lamps.",
    "The fan must start early; the other cables may be connected before running.", ("gated_air", 0, -3, 0), ("spring_signal", 0, 0, 0), ("ramps_signal", 0, 3, 0));
Add("depth_delivery", "Depth delivery office", "Master workshop",
    "Complete three differently powered deliveries along depth-oriented lanes.",
    "Use X lanes -3, 0, and +3. Part positions differ in depth even where the front view overlaps.", ("ramps", -3, 0, 90), ("conveyor", 0, 0, 90), ("spring", 3, 0, -90));
Add("depth_telegraph", "Depth telegraph", "Final workshop",
    "Light three lamps using depth-oriented rolling, spring, and wind routes.",
    "Orbit before placing; use the switch positions as the ends of each route.", ("ramps_signal", -3, 0, 90), ("spring_signal", 0, 0, 90), ("air_signal", 3, 0, -90));
Add("double_bridge", "Double bridge", "Final workshop",
    "Build two domino bridges and complete the central conveyor delivery.",
    "Allocate four dominoes to each outside lane and the conveyor to the centre.", ("domino", 0, -3, 0), ("conveyor", 0, 0, 0), ("domino", 0, 3, 0));
Add("bridges_and_signal", "Bridges and signal", "Final workshop",
    "Complete two domino bridges and light the central spring-triggered lamp.",
    "The lamp needs the fixed switch wired; the bridges need physical continuity.", ("domino", 0, -3, 0), ("spring_signal", 0, 0, 0), ("domino", 0, 3, 0));
Add("cold_bridges", "Cold bridges", "Final workshop",
    "Two domino chains frame a cold-start fan delivery. Complete all four objectives.",
    "Start the fan with the nearby trigger ball, then use the eight dominoes to bridge both outside gaps.", ("domino", 0, -3, 0), ("gated_air", 0, 0, 0), ("domino", 0, 3, 0));
Add("grand_contraption", "The grand contraption", "Final workshop",
    "Complete the ramp signal, conveyor signal, and domino chain; all three must succeed in one run.",
    "Build and test each lane. The final machine needs two slopes, a conveyor delivery, four dominoes, and both signal wires.", ("ramps_signal", 0, -3, 0), ("domino", 0, 0, 0), ("conveyor_signal", 0, 3, 0));
if (campaign.Count != 40) throw new InvalidDataException($"Expected 40 levels, authored {campaign.Count}.");
// Every authored instance owns its difficulty curve; catalog defaults do not decide puzzle help.
foreach (var puzzle in campaign)
foreach (var part in puzzle.Parts.Concat(puzzle.Solution))
{
    var positionStep = part.Kind switch { "ramp" => .30f, "spring" => .20f, "fan" => .35f, "domino" => .20f, _ => .25f };
    var rotationStep = part.Kind switch { "spring" => 3f, "fan" => 15f, "switch" => 0f, _ => 5f };
    var window = part.Kind == "domino" ? 1.2f : 3f;
    // Fans must settle before a nearby falling ball enters the airflow (~0.27 s).
    // Keep quintic animation, but author a shorter duration instead of snapping or changing physics.
    var blendSeconds = part.Kind == "fan" ? .15f : .4f;
    part.Difficulty =
    [
        new() { Precision = 0, BlendSeconds = blendSeconds, PositionWindow = window, RotationWindow = 100,
            MaxPositionCorrection = part.Locked ? 0 : positionStep, MaxRotationCorrection = part.Locked ? 0 : rotationStep,
            CaptureMargin = .30f, CaptureSpeed = 3, CaptureDwell = .15f,
            GuideAcceleration = part.Kind == "basket" ? 12 : 0,
            TriggerThreshold = part.Kind == "domino" ? .15f : .2f },
        new() { Precision = .45f, BlendSeconds = blendSeconds, PositionWindow = window * .5f, RotationWindow = 45,
            MaxPositionCorrection = part.Locked ? 0 : positionStep * .4f, MaxRotationCorrection = part.Locked ? 0 : rotationStep * .4f,
            CaptureMargin = .174f, CaptureSpeed = 2.325f, CaptureDwell = .24f,
            GuideAcceleration = part.Kind == "basket" ? 6.6f : 0,
            TriggerThreshold = part.Kind == "domino" ? .3075f : .47f },
        new() { Precision = 1, TriggerThreshold = part.Kind == "domino" ? .5f : .8f }
    ];
}
Console.Write(JsonSerializer.Serialize(campaign, MachineJson.Default.ListPuzzleData));
