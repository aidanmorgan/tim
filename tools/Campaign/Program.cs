using CuriousContraptions;
using System.Text.Json;
using System.Numerics;

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
    Goals = [new() { Type = GoalKind.Activated, Target = "end" }]
};
for (var i = 0; i < 4; i++)
    domino.Solution.Add(new() { Id = "domino_" + i, Kind = "domino", Position = [-2.4f + i * .8f, 1, 0] });
modules["domino"] = domino;

modules["conveyor"] = new()
{
    Parts =
    [
        new() { Id = "ball", Kind = "ball", Position = [-3, 5, 0], Locked = true },
        new() { Id = "receiver", Kind = "basket", Position = [2, .9f, 0], Locked = true },
        new() { Id = "battery", Kind = "battery", Position = [-3.4f, .4f, 0], Locked = true },
        new() { Id = "motor", Kind = "motor", Position = [-2, .4f, 0], Locked = true }
    ],
    SolutionConnections =
    [
        new() { From = "battery", To = "motor", Type = ConnectionDomain.Electrical, FromPort = SocketId.Supply, ToPort = SocketId.PowerIn },
        new() { From = "motor", To = "conveyor_1", Type = ConnectionDomain.Mechanical, FromPort = SocketId.Drive, ToPort = SocketId.DriveIn }
    ],
    Inventory = new() { ["conveyor"] = 1 },
    Solution = [new() { Id = "conveyor_1", Kind = "conveyor", Position = [-2, 2.5f, 0] }],
    Goals = [new() { Type = GoalKind.Captured, Target = "receiver", Body = "ball" }]
};

foreach (var name in new[] { "ramps", "spring", "air", "conveyor" })
{
    var module = Copy(modules[name]);
    var receiver = module.Parts.Single(p => p.Id == "receiver");
    receiver.Kind = "switch";
    if (name == "ramps") receiver.Position[0] = 3.2f;
    module.Parts.Add(new() { Id = "lamp", Kind = "lamp", Position = [4.5f, 1, 0], Locked = true });
    module.Goals = [new() { Type = GoalKind.Activated, Target = "lamp" }];
    module.SolutionConnections.Add(new() { Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn, From = "receiver", To = "lamp" });
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
gated.SolutionConnections = [new() { Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn, From = "switch_1", To = fixedFan.Id }];
gated.Goals.Add(new() { Type = GoalKind.Activated, Target = fixedFan.Id });
modules["gated_air"] = gated;
var gatedBelt = Copy(modules["conveyor"]);
var fixedBelt = gatedBelt.Solution.Single();
gatedBelt.Solution.Clear();
gatedBelt.Inventory = new() { ["switch"] = 1 };
fixedBelt.Locked = true;
gatedBelt.Parts.Add(fixedBelt);
gatedBelt.Parts.Add(new() { Id = "trigger", Kind = "bowling", Position = [-5.8f, 2.4f, 0], Locked = true });
gatedBelt.Solution.Add(new() { Id = "switch_1", Kind = "switch", Position = [-5.8f, 1.6f, 0] });
gatedBelt.SolutionConnections =
[
    new() { Type = ConnectionDomain.Electrical, FromPort = SocketId.Supply, ToPort = SocketId.PowerIn, From = "battery", To = "switch_1" },
    new() { Type = ConnectionDomain.Electrical, FromPort = SocketId.Supply, ToPort = SocketId.PowerIn, From = "switch_1", To = "motor" },
    new() { Type = ConnectionDomain.Mechanical, FromPort = SocketId.Drive, ToPort = SocketId.DriveIn, From = "motor", To = fixedBelt.Id }
];
gatedBelt.Goals.Add(new() { Type = GoalKind.PoweredAfter, Target = "motor", Body = "switch_1" });
modules["gated_belt"] = gatedBelt;

modules["bumper"] = new()
{
    Parts = [
        new() { Id = "ball", Kind = "ball", Position = [-3, 5, 0], Locked = true },
        new() { Id = "receiver", Kind = "basket", Position = [2, .9f, 0], Locked = true }
    ],
    Inventory = new() { ["bumper"] = 1 },
    Solution = [new() { Id = "bumper_1", Kind = "bumper", Position = [-3.2f, 1.5f, 0] }],
    Goals = [new() { Type = GoalKind.Captured, Target = "receiver", Body = "ball" }]
};

modules["wall_return"] = new()
{
    Parts = [
        new() { Id = "ball", Kind = "ball", Position = [-3, 5, 0], Locked = true },
        new() { Id = "bumper", Kind = "bumper", Position = [-3.2f, 1.5f, 0], Locked = true },
        new() { Id = "receiver", Kind = "basket", Position = [-5, .9f, 0], Locked = true }
    ],
    Inventory = new() { ["wall"] = 1 },
    Solution = [new() { Id = "wall_1", Kind = "wall", Position = [-1, 4, 0],
        Properties = new() { ["width"] = .4f, ["height"] = 6, ["thickness"] = 1.5f } }],
    Goals = [new() { Type = GoalKind.Captured, Target = "receiver", Body = "ball" }]
};
var combinedWall = Copy(modules["wall_return"]);
var launchBumper = combinedWall.Parts.Single(p => p.Kind == "bumper");
combinedWall.Parts.Remove(launchBumper);
launchBumper.Id = "bumper_1";
launchBumper.Locked = false;
combinedWall.Solution.Insert(0, launchBumper);
combinedWall.Inventory["bumper"] = 1;
modules["wall_and_bumper"] = combinedWall;

modules["battery_motor"] = new()
{
    Inventory = new() { ["battery"] = 1 },
    Parts = [new() { Id = "motor", Kind = "motor", Locked = true, Position = [3, 1, 0] }],
    Solution = [new() { Id = "battery_1", Kind = "battery", Position = [-3, 1, 0] }],
    SolutionConnections = [new() { From = "battery_1", To = "motor", Type = ConnectionDomain.Electrical, FromPort = SocketId.Supply, ToPort = SocketId.PowerIn }],
    Goals = [new() { Type = GoalKind.Turned, Target = "motor" }]
};
modules["switched_motor"] = new()
{
    Inventory = new() { ["switch"] = 1 },
    Parts =
    [
        new() { Id = "battery", Kind = "battery", Locked = true, Position = [-5, 1, 0] },
        new() { Id = "motor", Kind = "motor", Locked = true, Position = [3, 1, 0] },
        new() { Id = "ball", Kind = "bowling", Locked = true, Position = [-2, 5, 0] }
    ],
    Solution = [new() { Id = "switch_1", Kind = "switch", Position = [-2, 1, 0] }],
    SolutionConnections =
    [
        new() { From = "battery", To = "switch_1", Type = ConnectionDomain.Electrical, FromPort = SocketId.Supply, ToPort = SocketId.PowerIn },
        new() { From = "switch_1", To = "motor", Type = ConnectionDomain.Electrical, FromPort = SocketId.Supply, ToPort = SocketId.PowerIn }
    ],
    Goals = [new() { Type = GoalKind.Turned, Target = "motor" }, new() { Type = GoalKind.PoweredAfter, Target = "motor", Body = "switch_1" }]
};

var relayBelt = Copy(modules["conveyor"]);
relayBelt.Parts.Add(new() { Id = "relay", Kind = "conveyor", Locked = true, Position = [1, 4.5f, 0] });
relayBelt.SolutionConnections.Single(c => c.Type == ConnectionDomain.Mechanical).To = "relay";
relayBelt.SolutionConnections.Add(new() { From = "relay", To = "conveyor_1", Type = ConnectionDomain.Mechanical,
    FromPort = SocketId.Drive, ToPort = SocketId.DriveIn });
modules["belt_relay"] = relayBelt;

var reverseBelt = Copy(modules["conveyor"]);
reverseBelt.Parts.Single(p => p.Id == "ball").Position[0] = 3;
reverseBelt.Parts.Single(p => p.Id == "receiver").Position[0] = -2;
reverseBelt.Parts.Single(p => p.Id == "battery").Position = [-5, 1, 0];
reverseBelt.Parts.Single(p => p.Id == "motor").Position = [-3.5f, 1, 0];
reverseBelt.Solution.Single().Position[0] = 2;
reverseBelt.Inventory["reverse_transmission"] = 1;
reverseBelt.Solution.Add(new() { Id = "reverse_1", Kind = "reverse_transmission", Position = [-.3f, 1, 0] });
reverseBelt.SolutionConnections.Single(c => c.Type == ConnectionDomain.Mechanical).To = "reverse_1";
reverseBelt.SolutionConnections.Add(new() { From = "reverse_1", To = "conveyor_1", Type = ConnectionDomain.Mechanical,
    FromPort = SocketId.Drive, ToPort = SocketId.DriveIn });
modules["reverse_belt"] = reverseBelt;

ConnectionSpec Rope(PuzzleData puzzle, string from, string to)
{
    Vector3 Socket(string id)
    {
        var part = puzzle.Parts.Concat(puzzle.Solution).Single(p => p.Id == id);
        var local = part.Kind switch
        {
            "weight" => new Vector3(0, RopeGeometry.WeightTieHeight(part.Properties[WeightParameters.Mass]), 0),
            "pulley" => new Vector3(0, RopeGeometry.PulleyRadius, RopeGeometry.PulleySocketDepth),
            _ => throw new InvalidDataException("No authored rope geometry for " + part.Kind)
        };
        var degrees = MathF.PI / 180;
        var rotation = Quaternion.CreateFromYawPitchRoll(part.Rotation[1] * degrees, part.Rotation[0] * degrees, part.Rotation[2] * degrees);
        return new Vector3(part.Position[0], part.Position[1], part.Position[2]) + Vector3.Transform(local, rotation);
    }
    return new() { From = from, To = to, Type = ConnectionDomain.Rope, FromPort = SocketId.Tie, ToPort = SocketId.Tie,
        RopeLength = Vector3.Distance(Socket(from), Socket(to)) };
}
var lift = new PuzzleData
{
    Inventory = new() { ["weight"] = 1 },
    Parts =
    [
        new() { Id = "load", Kind = "weight", Locked = true, Position = [-2, 1, .12f], Properties = new() { [WeightParameters.Mass] = 1 } },
        new() { Id = "left_pulley", Kind = "pulley", Locked = true, Position = [-2, 6, 0] },
        new() { Id = "right_pulley", Kind = "pulley", Locked = true, Position = [2, 6, 0] },
        new() { Id = "switch", Kind = "switch", Locked = true, Position = [-2, 3.2f, .12f], Rotation = [0, 0, 180] },
        new() { Id = "lamp", Kind = "lamp", Locked = true, Position = [5, 1, .12f] }
    ],
    Solution = [new() { Id = "weight_1", Kind = "weight", Position = [2, 5, .12f], Properties = new() { [WeightParameters.Mass] = 4 } }],
    Goals = [new() { Type = GoalKind.Activated, Target = "lamp" }]
};
lift.SolutionConnections =
[
    Rope(lift, "load", "left_pulley"), Rope(lift, "left_pulley", "right_pulley"), Rope(lift, "right_pulley", "weight_1"),
    new() { From = "switch", To = "lamp", Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn }
];
modules["counterweight"] = lift;
var depthLift = Copy(lift);
var movablePulley = depthLift.Parts.Single(p => p.Id == "right_pulley");
depthLift.Parts.Remove(movablePulley);
movablePulley.Locked = false;
movablePulley.Id = "pulley_1";
movablePulley.Position[2] = 2;
depthLift.Solution.Single().Position[2] = 2.12f;
depthLift.Solution.Add(movablePulley);
depthLift.Inventory["pulley"] = 1;
depthLift.SolutionConnections =
[
    Rope(depthLift, "load", "left_pulley"), Rope(depthLift, "left_pulley", "pulley_1"), Rope(depthLift, "pulley_1", "weight_1"),
    new() { From = "switch", To = "lamp", Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn }
];
modules["pulley_depth"] = depthLift;

var solar = new PuzzleData
{
    Parts =
    [
        new() { Id = "torch", Kind = "flashlight", Locked = true, Position = [-2, 3, 0] },
        new() { Id = "trigger", Kind = "ball", Locked = true, Position = [-2.15f, 5, 0] },
        new() { Id = "motor", Kind = "motor", Locked = true, Position = [4, 1, 2] }
    ],
    Inventory = new() { ["solar_panel"] = 1 },
    Solution = [new() { Id = "panel_1", Kind = "solar_panel", Position = [1, 3, 0] }],
    SolutionConnections = [new() { From = "panel_1", To = "motor", Type = ConnectionDomain.Electrical,
        FromPort = SocketId.Supply, ToPort = SocketId.PowerIn }],
    Goals = [new() { Type = GoalKind.Turned, Target = "motor" }]
};
modules["solar_motor"] = solar;
var shaded = Copy(solar);
shaded.Parts.Single(p => p.Id == "torch").Position[0] = -3;
shaded.Parts.Single(p => p.Id == "trigger").Position[0] = -3.15f;
shaded.Parts.Add(new() { Id = "shade", Kind = "wall", Locked = true, Position = [-.5f, 3, 0],
    Rotation = [0, 90, 0], Properties = new() { ["width"] = 3, ["height"] = 3, ["thickness"] = .3f } });
shaded.Solution[0].Position = [-1.5f, 3, 0];
modules["solar_shadow"] = shaded;

var delay = new PuzzleData
{
    Parts =
    [
        new() { Id = "switch", Kind = "switch", Locked = true, Position = [-3, 1, 0] },
        new() { Id = "trigger", Kind = "ball", Locked = true, Position = [-3, 4, 0] },
        new() { Id = "lamp", Kind = "lamp", Locked = true, Position = [3, 1, 0] }
    ],
    Inventory = new() { ["delay"] = 1 },
    Solution = [new() { Id = "delay_1", Kind = "delay", Position = [0, 1, 0] }],
    SolutionConnections =
    [
        new() { From = "switch", To = "delay_1", Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn },
        new() { From = "delay_1", To = "lamp", Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn }
    ],
    Goals = [new() { Type = GoalKind.ActivatedAfter, Target = "lamp", Body = "switch", MinimumDelaySeconds = 1 }]
};
modules["delayed_signal"] = delay;
var delayedSolar = Copy(solar);
delayedSolar.Parts.Single(p => p.Id == "trigger").Position = [-4, 5, -2];
delayedSolar.Parts.Add(new() { Id = "switch", Kind = "switch", Locked = true, Position = [-4, 3, -2] });
delayedSolar.Solution.Add(new() { Id = "delay_1", Kind = "delay", Position = [0, 1, 2], Rotation = [0, 180, 0] });
delayedSolar.Inventory["delay"] = 1;
delayedSolar.SolutionConnections.AddRange(
[
    new() { From = "switch", To = "delay_1", Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn },
    new() { From = "delay_1", To = "torch", Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn }
]);
delayedSolar.Goals.Add(new() { Type = GoalKind.PoweredAfter, Target = "motor", Body = "switch", MinimumDelaySeconds = 1 });
modules["delayed_solar"] = delayedSolar;

modules["clear_pipe"] = new PuzzleData
{
    Parts = [
        new() { Id = "ball", Kind = "ball", Locked = true, Position = [-1.4f, 6, 0] },
        new() { Id = "receiver", Kind = "basket", Locked = true, Position = [2, .6f, 0] }
    ],
    Inventory = new() { ["pipe"] = 1 },
    Solution = [new() { Id = "pipe_1", Kind = "pipe", Position = [0, 3, 0], Rotation = [0, 0, -45], Properties = new() { [PipeParameters.Length] = 3.6f } }],
    Goals = [new() { Type = GoalKind.Captured, Target = "receiver", Body = "ball" }]
};

foreach (var angle in Enum.GetValues<TubeBendAngle>())
{
    var half = (int)angle * MathF.PI / 360;
    var inletX = 2.4f * (1 - MathF.Cos(half));
    var inletY = 4 + 2.4f * MathF.Sin(half) + .09f;
    var kind = $"pipe_bend_{(int)angle}";
    modules[kind] = new PuzzleData
    {
        Parts = [
            new() { Id = "ball", Kind = "ball", Locked = true, Position = [inletX, inletY + 1.6f, 0] },
            new() { Id = "receiver", Kind = "basket", Locked = true, Position = [angle == TubeBendAngle.Degrees45 ? -2f : -5.5f, .6f, 0] }
        ],
        Inventory = new() { [kind] = 1 },
        Solution = [new() { Id = "bend_1", Kind = kind, Position = [0, 4, 0], Rotation = [0, 0, -90] }],
        Goals = [new() { Type = GoalKind.Captured, Target = "receiver", Body = "ball" }]
    };
}

// A descending straight inlet must join the fixed bend; a vertical fall misses the receiver.
modules["joined_pipe"] = new PuzzleData
{
    Parts = [
        new() { Id = "ball", Kind = "ball", Locked = true, Position = [-1.308076f, 8.402188f, 0] },
        new() { Id = "bend", Kind = "pipe_bend_90", Locked = true, Position = [1, 3.5f, 0], Rotation = [0, 0, -45] },
        new() { Id = "receiver", Kind = "basket", Locked = true, Position = [-.2f, .6f, 0] }
    ],
    Inventory = new() { ["pipe"] = 1 },
    Solution = [new() { Id = "pipe_1", Kind = "pipe", Position = [-.5373296f, 6.031442f, 0], Rotation = [0, 0, -45],
        Properties = new() { [PipeParameters.Length] = 2 } }],
    Goals = [new() { Type = GoalKind.Captured, Target = "receiver", Body = "ball" }]
};

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
Add("spring_signal", "Spring-loaded signal", "Motion into power",
    "Launch the ball onto the elevated switch and light the lamp.",
    "Tilt the spring clockwise. Remember the wire from switch to lamp.", ("spring_signal", 0, 0, 0));
Add("wind_signal", "A breath of electricity", "Motion into power",
    "Use moving air to strike the switch and power the lamp.",
    "The fan's height controls how long the ball receives a push.", ("air_signal", 0, 0, 0));
Add("cold_start", "Cold start", "Causal machines",
    "The fan starts off. Use the bowling ball to power it before the tennis ball falls past.",
    "A switch close beneath the bowling ball starts the fan early. Connect it to the fixed fan.", ("gated_air", 0, 0, 0));

// New mechanics are taught before the older multi-machine combinations.
Add("bumper_sidekick", "A little sidekick", "Round rebounds",
    "Use the pinball bumper to bounce the orange ball into the receiver.",
    "The ball bounces away from the impact point. Put the bumper just left of the falling ball to send it right.", ("bumper", 0, 0, 0));
Add("bumper_depth", "Bounce into depth", "Round rebounds",
    "Send the ball across the workbench's depth with a round bumper.",
    "Orbit with Q/E to see the route. Offset the bumper toward the front so the ball rebounds toward the receiver behind it.", ("bumper", 0, 0, 90));

Add("wall_return", "Return to sender", "Build a barrier",
    "The fixed bumper launches the ball away from the basket. Build a wall that sends it back.",
    "Use the square resize icon. Make a tall, narrow barrier to the right of the bumper; the dashed boxes show its reach.", ("wall_return", 0, 0, 0));
Add("wall_and_bumper", "Build the rebound", "Build a barrier",
    "Place both the launcher and its return wall to deliver the ball into the basket on the left.",
    "First offset the bumper slightly left of the falling ball. Then catch the rising flight with a tall wall to its right.", ("wall_and_bumper", 0, 0, 0));

Add("battery_motor", "A turn for the better", "Electrical supply",
    "Make the motor complete a full turn. It needs a battery, not an activation signal.",
    "Place a battery, select it and choose Connect. Click the motor, then Run.", ("battery_motor", 0, 0, 0));
Add("switched_motor", "Close the circuit", "Switched supply",
    "The motor must wait for the falling ball to trigger its supply switch, then complete a turn.",
    "Place the switch under the ball. Connect battery to switch, then switch to motor. A switch controls electricity but does not create it.", ("switched_motor", 0, 0, 0));

Add("conveyor_courier", "Conveyor courier", "Mechanical drive",
    "Carry the ball on a motor-driven conveyor. A belt needs rotation, not an activation signal.",
    "Place the conveyor beneath the ball. Connect battery to motor, then motor to conveyor.", ("conveyor", 0, 0, 0));
Add("belt_relay", "Pass the rotation", "Mechanical drive",
    "Use the high conveyor's output pulley to drive the delivery conveyor below.",
    "Wire battery to motor. Belt the motor to the high conveyor, then connect that conveyor to your delivery belt.", ("belt_relay", 0, 0, 0));
Add("reverse_belt", "The other way round", "Reverse transmission",
    "The basket is to the left. Reverse the drive so the conveyor carries the ball back toward it.",
    "Wire battery to motor, then belt motor to reverse transmission and transmission to conveyor. Its paired wheels turn in opposite directions.", ("reverse_belt", 0, 0, 0));

Add("counterweight", "A helping weight", "Ropes and loads",
    "Lift the small load into the overhead switch to light the lamp. A heavier weight can pull it upward.",
    "Place the larger weight high under the right pulley. Connect small load → left pulley → right pulley → large weight, then switch → lamp. Rope length is set when you connect.", ("counterweight", 0, 0, 0));
Add("pulley_depth", "Around the corner", "Route a rope",
    "The counterweight belongs in the front depth lane. Add a pulley above it and route the rope back to the small load.",
    "Place the second pulley above the large weight. Connect both pulleys into one continuous rope with a load at each end; a loose end cannot lift anything.", ("pulley_depth", 0, 0, 0));

Add("solar_motor", "A little sunshine", "Light into electricity",
    "Use the flashlight to supply the motor through a solar panel. The falling ball presses the torch's button.",
    "Put the panel in front of the cream lens, with blue cells facing the torch. Connect panel to motor; four gold meter marks mean enough light.", ("solar_motor", 0, 0, 180));
Add("solar_shadow", "Out of the shade", "Light and obstacles",
    "The wooden wall blocks the flashlight. Find a lit position for the panel and turn the motor.",
    "Keep the panel on the flashlight side of the wall, facing the lens. Wires carry electricity around obstacles, but light cannot pass through them.", ("solar_shadow", 0, 0, 180));

Add("delayed_signal", "Wait for it", "Delayed commands",
    "Light the lamp only after the delay box finishes its countdown.",
    "Connect switch → delay → lamp. A trigger starts the one-second countdown; further triggers are ignored until Reset.", ("delayed_signal", 0, 0, 0));
Add("delayed_solar", "A later sunrise", "Timing and power",
    "Wait for the countdown before lighting the torch and turning the motor.",
    "Connect switch → delay → torch, then panel → motor. The delay sends a command, while the lit solar panel supplies electricity.", ("delayed_solar", 0, 0, 180));

Add("clear_pipe", "Through the looking tube", "Hollow routes",
    "Catch the falling ball inside the clear pipe and guide it down to the basket.",
    "Tilt the tube downhill toward the basket. Its cream mouths are open; the clear walls keep the ball inside without adding speed.", ("clear_pipe", 0, 0, 0));

Add("gentle_bend", "A gentle turn", "Hollow routes",
    "Guide the falling ball toward the basket with a 45-degree bend.",
    "Turn the first mouth upward under the ball. The second mouth sends it down and left; gravity carries it through the curve.", ("pipe_bend_45", 0, 0, 0));
Add("quarter_bend", "Around the corner", "Hollow routes",
    "Use a 90-degree bend to turn a vertical fall into a sideways delivery.",
    "Point one mouth upward and the other toward the left-hand basket. The curve redirects motion, but does not add speed.", ("pipe_bend_90", 0, 0, 0));

Add("joined_pipe", "Meet in the middle", "Joined routes",
    "Join a short straight tube to the fixed bend so the falling ball reaches the receiver.",
    "Shorten the tube to fit, turn it downhill, then bring its lower mouth near the bend's upper mouth. A small move aligns the openings; the ball needs a continuous route.", ("joined_pipe", 0, 0, 0));

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
    "Use the green rotation ring to turn each fan. Keep each gust in its own X lane.", ("air", -2, 0, 90), ("air", 2, 0, -90));
Add("deep_springs", "Deep springs", "Spatial reasoning",
    "Launch two balls through depth into elevated receivers.",
    "Rotate each spring around Y, then use the rotation rings to set its launch tilt.", ("spring", -2, 0, 90), ("spring", 2, 0, -90));
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
if (campaign.Count != 58) throw new InvalidDataException($"Expected 58 authored levels in this expansion stage, authored {campaign.Count}.");
// Every authored instance owns its difficulty curve; catalog defaults do not decide puzzle help.
foreach (var puzzle in campaign)
foreach (var part in puzzle.Parts.Concat(puzzle.Solution))
{
    var positionStep = part.Kind switch { "ramp" => .30f, "spring" => .20f, "fan" => .35f, "domino" => .20f, _ => .25f };
    var rotationStep = part.Kind switch { "spring" => 3f, "fan" => 15f, "switch" or "bumper" => 0f, _ => 5f };
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
    if (!part.Locked && (part.Kind is "pipe_bend_45" or "pipe_bend_90" ||
        puzzle.Id == "joined_pipe" && part.Kind == "pipe"))
    {
        // Curved outlets amplify residual lateral error. Bound eligibility and correction together:
        // align fully inside a small authored window, never move a more distant bend.
        part.Difficulty[0].PositionWindow = part.Difficulty[0].MaxPositionCorrection = .6f;
        part.Difficulty[1].PositionWindow = part.Difficulty[1].MaxPositionCorrection = .5f;
        part.Difficulty[0].RotationWindow = part.Difficulty[0].MaxRotationCorrection = 5;
        part.Difficulty[1].RotationWindow = part.Difficulty[1].MaxRotationCorrection = 2;
    }
}
Console.Write(JsonSerializer.Serialize(campaign, MachineJson.Default.ListPuzzleData));
