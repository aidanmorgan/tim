using System.Buffers.Binary;
using System.Text.Json;
using CuriousContraptions.Gpu;

using var raw = JsonDocument.Parse(File.ReadAllText(args[0]));
var source = raw.RootElement.GetProperty("readbacks")[0].EnumerateArray().Select(x => x.GetByte()).ToArray();
using var replay = JsonDocument.Parse(File.ReadAllText(args[1]));
var candidate = replay.RootElement.GetProperty("candidate").EnumerateArray().Select(x => x.GetByte()).ToArray();
var tick = new SimulationTick(BinaryPrimitives.ReadUInt64LittleEndian(source.AsSpan(40)) + 1);
var motion = PhysicsGpuAbi.ValidateCandidate(candidate, source, tick);
var body = PhysicsGpuAbi.ReadDynamicBody(candidate);
Console.WriteLine(JsonSerializer.Serialize(new { valid = true, tick = tick.Value, motion.Count, body }));
