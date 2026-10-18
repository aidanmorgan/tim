using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Elements.Ball;
using CuriousContraptions.Elements.BallDetector;
using CuriousContraptions.Elements.Battery;
using CuriousContraptions.Elements.Bumper;
using CuriousContraptions.Elements.Counter;
using CuriousContraptions.Elements.Domino;
using CuriousContraptions.Elements.PoweredGate;
using CuriousContraptions.Elements.PressurePlate;
using CuriousContraptions.Elements.Ramp;
using CuriousContraptions.Elements.SignalLamp;
using CuriousContraptions.Elements.StateLatch;
using CuriousContraptions.Elements.SystemClock;
using CuriousContraptions.Elements.Wall;

var declarations = new IPuzzleElementDeclaration[]
{
    new BallElementDeclaration(),
    new BumperElementDeclaration(),
    new DominoElementDeclaration(),
    new RampElementDeclaration(),
    new WallElementDeclaration(),
    new BatteryElementDeclaration(),
    new PoweredGateElementDeclaration(),
    new PressurePlateElementDeclaration(),
    new CounterElementDeclaration(),
    new SignalLampElementDeclaration(),
    new BallDetectorElementDeclaration(),
    new SystemClockElementDeclaration(),
    new StateLatchElementDeclaration()
};

var bundle = new
{
    Version = "1.0",
    GeneratedAt = DateTime.UtcNow.ToString("O"),
    Elements = declarations.Select(d => new
    {
        d.Metadata,
        SupportedDomains = (uint)d.SupportedDomains,
        d.Ports,
        d.Colliders,
        d.Events
    }).ToArray()
};

var json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions { WriteIndented = true });

var targetPaths = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), "AppBundle", "elements.bundle.json"),
    Path.Combine(Directory.GetCurrentDirectory(), "CuriousContraptions.web", "AppBundle", "elements.bundle.json"),
    Path.Combine(Directory.GetCurrentDirectory(), "parts", "elements.bundle.json")
};

foreach (var path in targetPaths)
{
    var dir = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    File.WriteAllText(path, json);
    Console.WriteLine($"Emitted element bundle: {path} ({declarations.Length} elements)");
}
