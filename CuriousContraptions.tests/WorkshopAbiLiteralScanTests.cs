using System.Text.RegularExpressions;

namespace CuriousContraptions.Tests;

/// <summary>Murdoch-2 R2-F4 (Story 9.4b): the physical world's header, contact-work, work-occurrence and trigger fields are
/// read and written only through the named PhysicsGpuAbi offsets, so the tests that pin those names pin the shipped
/// layout. Story 9A.44 extends the scan to every relaid f32 record field. A literal offset in any of these places fails here.</summary>
public sealed class WorkshopAbiLiteralScanTests(ITestOutputHelper output)
{
    public enum Rule { WorldHeader, ContactWorkRecords, ContactEventLoop, TriggerRead, RecordFields }

    private static string Root => twodog.Engine.ResolveProjectDir();
    private static string Source(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static readonly string[] AbiFiles =
        Directory.GetFiles(Path.Combine(Root, "engine/gpu"), "PhysicsGpuAbi*.cs").Select(path => Path.GetRelativePath(Root, path)).Order().ToArray();

    /// <summary>The text one scan rule covers, with the literal patterns it forbids.</summary>
    private static IEnumerable<(string Where, string Text, Regex Forbidden)> Scope(Rule rule) => rule switch
    {
        // Header: the world spans are `data`, `source`, `candidate` and `previous` in the ABI, `span`, `mSpan` and `mockBuffer`
        // in the engine, `bytes` and `_committedWorld` in the device; a literal first offset into any of them is a header field.
        Rule.WorldHeader => AbiFiles.Select(path => (path, Source(path),
                new Regex(@"\b(?:R32|R64|RF|RH|U32|U64|H)\((?:data|source|candidate|previous), ?\d|\b(?:data|source|candidate|previous)\[(?:\.\.)?\d")))
            .Append(("CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs", Source("CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs"),
                new Regex(@"\b(?:span|mSpan|mockBuffer|source|candidate)\.(?:Slice|AsSpan)\(\d")))
            .Append(("CuriousContraptions.Simulation/WorkshopGpuDevice.cs", Source("CuriousContraptions.Simulation/WorkshopGpuDevice.cs"),
                new Regex(@"\b(?:bytes|_committedWorld)\.AsSpan\(\d"))),
        // The contact-work partial (admission, read, candidate validation): no literal record offset or stride.
        Rule.ContactWorkRecords => [("engine/gpu/PhysicsGpuAbi.ContactWork.cs", Source("engine/gpu/PhysicsGpuAbi.ContactWork.cs"),
            new Regex(@"\b(?:R32|R64|RF|RH|U32|U64)\((?:record|declaration|slot), ?[1-9]|\brecord\[[1-9]|\* ?(?:32|64)\b|\bSlice\([^,]+, ?(?:32|64)\)"))],
        // The engine's contact-event method (triggers and contact work): no literal field offset, stride or target kind.
        Rule.ContactEventLoop => [("WorkshopPhysicsEngine.EmitContactEvents", Method(Source("CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs"), " void EmitContactEvents("),
            new Regex(@"\b[two]Offset \+ \d|\* (?:32|64)\b|\bkind == \d"))],
        // The committed trigger reader: no literal record offset, padding slice or shared body/collider field.
        Rule.TriggerRead => [("PhysicsGpuAbi.ReadTrigger", Method(Source("engine/gpu/PhysicsGpuAbi.cs"), " ContactTriggerRead ReadTrigger("),
            new Regex(@"\b(?:R32|R64|RF|RH)\(record, ?[1-9]|\brecord\[[1-9]|Bytes \+ \d"))],
        // Story 9A.44 D3: every world record (body, collider, material, sensor, guide, trigger, orientation, work occurrence,
        // prismatic, electrical source and binding, contact load, motion piece, body wire, trace capture) is read and written
        // through its named field offsets on both sides: the ABI and read models (record spans `record`, `bodyRecord`,
        // `declaration`, `slot`, `bytes`, `_bytes`, `p`, `piece`) and the worker (record bases `bodyOffset`, ... `prismaticOffset`,
        // `supplyOffset`, `bindingOffset`, `joint.Offset`, or a `layout.…Offset(i)` call). A literal first offset, slice or
        // base + literal is a field; a named field plus a component step (`VelocityOffset + 4`) is not.
        Rule.RecordFields => RecordFiles.Select(path => (path, Source(path), RecordSpanLiteral))
            .Append(("WorkshopTraceRecord.Encode", Method(Source("engine/gpu/WorkshopTraceRecord.cs"), " byte[] Encode("),
                new Regex(@"\boffset ?\+ ?\d")))
            .Append(("CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs", Source("CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs"),
                new Regex(@"\b(?:bodyOffset|colliderOffset|materialOffset|sensorOffset|guideOffset|oOffset|tOffset|wOffset|storeOffset|pieceOffset|motionOffset|rotationOffset|prismaticOffset|supplyOffset|bindingOffset) ?\+ ?\d" +
                    @"|\.Offset ?\+ ?\d|Offset\([^()]*\) ?\+ ?\d" + RotationDriveFootprintLiteral))),
        _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown scan rule.")
    };

    private static readonly string[] RecordFiles = ["engine/gpu/PhysicsGpuAbi.cs", "engine/gpu/PhysicsGpuAbi.Header.cs",
        "engine/gpu/PhysicsGpuAbi.ContactWork.cs", "engine/gpu/PhysicsGpuAbi.Constraints.cs", "engine/gpu/PhysicsGpuAbi.Electrical.cs",
        "engine/gpu/PhysicsGpuAbi.ContactLoad.cs", "engine/gpu/PhysicsMotionRead.cs", "engine/gpu/PhysicsBodyWire.cs"];

    private static readonly Regex RecordSpanLiteral = new(
        @"\b(?:R32|R64|RF|U32|U64|F32|F32Vector|F|Vector|WriteEnergy)\((?:record|bodyRecord|declaration|slot|bytes|_bytes|p|piece), ?\d" +
        @"|\b(?:record|bodyRecord|declaration|slot|bytes|_bytes|p|piece)(?:\[\(?|\.Slice\(|\.AsSpan\()\d" +
        // A rotation's y/z/w, a drive's five values and a footprint's four bounds are named fields, never base + literal.
        RotationDriveFootprintLiteral);

    private const string RotationDriveFootprintLiteral = @"|(?:Rotation|Drive|Footprint)\w*Offset ?\+ ?\d|\b(?:rotation|drive) ?\+ ?\d";

    private static string Method(string source, string declaration)
    {
        var start = source.IndexOf(declaration, StringComparison.Ordinal);
        Assert.True(start >= 0, $"{declaration.Trim()} is not declared.");
        var open = source.IndexOf('{', start);
        for (int depth = 0, at = open; at < source.Length; at++)
        {
            if (source[at] == '{') depth++;
            else if (source[at] == '}' && --depth == 0) return source[open..(at + 1)];
        }
        throw new InvalidOperationException($"{declaration.Trim()} has no closing brace.");
    }

    [Theory]
    [InlineData(Rule.WorldHeader)]
    [InlineData(Rule.ContactWorkRecords)]
    [InlineData(Rule.ContactEventLoop)]
    [InlineData(Rule.TriggerRead)]
    [InlineData(Rule.RecordFields)]
    public void NoLiteralAbiOffsetRemains(Rule rule)
    {
        var hits = new List<string>();
        foreach (var (where, text, forbidden) in Scope(rule))
        {
            Assert.False(string.IsNullOrWhiteSpace(text), $"{where} is empty.");
            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
                if (forbidden.IsMatch(lines[i])) hits.Add($"{where}:{i + 1}: {lines[i].Trim()}");
        }
        output.WriteLine($"{rule}: {Scope(rule).Count()} sources scanned");
        Assert.True(hits.Count == 0, $"Literal ABI offsets ({rule}):\n" + string.Join('\n', hits));
    }

    // Controls: each rule's pattern catches the literal forms it exists to forbid, and accepts the named forms.
    [Theory]
    [InlineData(Rule.WorldHeader, "        var count = R32(data, 104);", true)]
    [InlineData(Rule.WorldHeader, "            !candidate[12..40].SequenceEqual(source[12..40]) ||", true)]
    [InlineData(Rule.WorldHeader, "        var count = R32(data, HeaderContactWorkCountOffset);", false)]
    [InlineData(Rule.ContactWorkRecords, "            var owner=R32(record,8);", true)]
    [InlineData(Rule.ContactWorkRecords, "            var record=data.Slice(ContactWorksOffset+i*64,ContactWorkBytes);", true)]
    [InlineData(Rule.ContactWorkRecords, "            var owner=R32(record,ContactWorkOwnerOffset);", false)]
    [InlineData(Rule.ContactEventLoop, "            var owner = Read(span.Slice(wOffset + 8, 4));", true)]
    [InlineData(Rule.ContactEventLoop, "            if (kind == 1 && named != slot) continue;", true)]
    [InlineData(Rule.ContactEventLoop, "            var owner = Read(span.Slice(wOffset + PhysicsGpuAbi.ContactWorkOwnerOffset, 4));", false)]
    [InlineData(Rule.TriggerRead, "            var colliderSlot = R32(record, 36);", true)]
    [InlineData(Rule.TriggerRead, "            var colliderSlot = R32(record, TriggerColliderOffset);", false)]
    [InlineData(Rule.RecordFields, "            F32(record, 88, body.Mass.Value);", true)]
    [InlineData(Rule.RecordFields, "            stores[i]=new(new(R64(record,0)),", true)]
    [InlineData(Rule.RecordFields, "        var phase = F(p,16);", true)]
    [InlineData(Rule.RecordFields, "        BinaryPrimitives.WriteInt32LittleEndian(bytes[(8 + 4)..], value.Body.Cell.Y);", true)]
    [InlineData(Rule.RecordFields, "            U32(b,offset+8,(uint)c.Phase);", true)]
    [InlineData(Rule.RecordFields, "            var mass = ReadF32(span, bodyOffset + 88);", true)]
    [InlineData(Rule.RecordFields, "            WriteF32(span, pieceOffset + 28, b.Drag);", true)]
    [InlineData(Rule.RecordFields, "            var a = R32(record, 8); var b = R32(record, 12);", true)]
    [InlineData(Rule.RecordFields, "            WriteEnergy(record, 16, source.Capacity.Value);", true)]
    [InlineData(Rule.RecordFields, "            var lower = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(prismaticOffset + 72, 4));", true)]
    [InlineData(Rule.RecordFields, "            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(layout.ElectricalSourceOffset(supply) + 40, 4), 0f);", true)]
    [InlineData(Rule.RecordFields, "                    WriteF32(span, joint.Offset + 96 + row * 4, joint.Impulses[row]);", true)]
    [InlineData(Rule.RecordFields, "            var a = R32(record, PrismaticBodyAOffset);", false)]
    [InlineData(Rule.RecordFields, "            WriteEnergy(record, ElectricalSourceCapacityOffset, source.Capacity.Value);", false)]
    [InlineData(Rule.RecordFields, "            WriteF32(span, layout.ElectricalSourceOffset(supply) + PhysicsGpuAbi.ElectricalSourceDebitOffset, 0f);", false)]
    [InlineData(Rule.RecordFields, "        F(bytes, RotationOffset + 12, value.Rotation.W);", true)]
    [InlineData(Rule.RecordFields, "            RF(record, PrismaticDriveOffset + 16));", true)]
    [InlineData(Rule.RecordFields, "            RF(record, ContactLoadFootprintOffset + 8),", true)]
    [InlineData(Rule.RecordFields, "        WriteEnergy(data, rotation + 4, frame.Rotation.Y);", true)]
    [InlineData(Rule.RecordFields, "                ReadF32(span, drive + 12),", true)]
    [InlineData(Rule.RecordFields, "        F(bytes, RotationOffset + PhysicsGpuAbi.RotationWOffset, value.Rotation.W);", false)]
    [InlineData(Rule.RecordFields, "            RF(record, PrismaticDriveMaxPowerOffset));", false)]
    [InlineData(Rule.RecordFields, "            F32(record, BodyMassOffset, body.Mass.Value);", false)]
    [InlineData(Rule.RecordFields, "        var wy = (double)F(p,AngularVelocityOffset+4);", false)]
    [InlineData(Rule.RecordFields, "            U32(b,offset+CapturePhaseOffset,(uint)c.Phase);", false)]
    [InlineData(Rule.RecordFields, "            var mass = ReadF32(span, bodyOffset + PhysicsGpuAbi.BodyMassOffset);", false)]
    public void EachRuleCatchesItsLiteralForm(Rule rule, string line, bool forbidden) =>
        Assert.Equal(forbidden, Scope(rule).Any(scope => scope.Forbidden.IsMatch(line)));
}
