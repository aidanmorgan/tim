using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>Each detector rule's positive cases, the negative controls, and the lexer's comment, string, template and regex handling.</summary>
public sealed class TimingDetectorTests
{
    private static TimingRule[] Rules(string code) => [.. TimingDetector.Scan("fixture.ts", code).Select(hit => hit.Rule)];

    [Theory]
    [InlineData("await page.evaluate('document.title');")]
    [InlineData("await worker.evaluate(`while (true) {}`);")]
    [InlineData("await page.addInitScript(\"window.x = 1\");")]
    [InlineData("await page.waitForFunction('globalThis.ready');")]
    [InlineData("const f = new Function('return 1');")]
    [InlineData("eval('1 + 1');")]
    public void StringPageCodeIsUnscannable(string code) => Assert.Equal([TimingRule.UnscannableCode], Rules(code));

    [Fact]
    public void FunctionPageCodeIsScannable() =>
        Assert.Empty(Rules("await page.evaluate(() => document.title); await page.waitForFunction(() => globalThis.ready);"));

    [Theory]
    [InlineData("const end = performance.now() + 20; while (performance.now() < end) { /* spin */ }")]
    [InlineData("for (const end = Date.now() + 5; Date.now() < end;) {}")]
    [InlineData("let n = 0; do { n++; } while (performance.now() < deadline);")]
    [InlineData("while (Date.now() < end) n++;")]
    public void BusyWaitLoopIsAStall(string code) => Assert.Contains(TimingRule.BusyWaitLoop, Rules(code));

    [Theory]
    [InlineData("for (let i = 0; Date.now() < deadline; await driver.page.waitForTimeout(50)) lit = true;")]
    [InlineData("while (Date.now() < deadline) { await driver.page.waitForTimeout(25); }")]
    [InlineData("do { await tick(); } while (performance.now() < deadline);")]
    [InlineData("for (let t = Date.now(); i < 3; i++) {}")]
    [InlineData("for (const sample of samples) { if (Date.now() > 0) break; }")]
    public void PollLoopOrNonClockConditionIsNotAStall(string code) => Assert.DoesNotContain(TimingRule.BusyWaitLoop, Rules(code));

    [Theory]
    [InlineData("performance.now = () => original() + 5;")]
    [InlineData("Date.now = () => 0;")]
    [InlineData("globalThis.Date = FakeDate;")]
    [InlineData("Object.defineProperty(performance, 'now', { value: () => 0 });")]
    [InlineData("Object.defineProperty(globalThis, 'Date', { value: FakeDate });")]
    [InlineData("await page.clock.install();")]
    [InlineData("await driver.context.clock.fastForward(1000);")]
    public void ClockTamperingIsDetected(string code) => Assert.Contains(TimingRule.ClockTampering, Rules(code));

    [Theory]
    [InlineData("const same = performance.now() === start;")]
    [InlineData("Object.defineProperty(target, 'value', { value: 1 });")]
    [InlineData("const lamp = driver.clockLateRenewals;")]
    public void ClockReadsAreNotTampering(string code) => Assert.DoesNotContain(TimingRule.ClockTampering, Rules(code));

    [Theory]
    [InlineData("import { spawn } from 'node:child_process';")]
    [InlineData("import { exec } from \"child_process\";")]
    [InlineData("const threads = await import('node:worker_threads');")]
    [InlineData("const worker = new Worker(url);")]
    public void ProcessesAndThreadsInjectLoad(string code) => Assert.Contains(TimingRule.ProcessOrThreadSpawn, Rules(code));

    [Theory]
    [InlineData("const elapsed = Date.now() - start;")]
    [InlineData("samples.push({ t: performance.now() - started, py });")]
    [InlineData("await wait(Math.max(0, REST - (Date.now() - runStarted)));")]
    [InlineData("console.log(Date.now() - a - b);")]
    public void WallClockDifferenceIsMeasured(string code) => Assert.Equal([TimingRule.WallClockDifference], Rules(code));

    [Theory]
    [InlineData("while (Date.now() - start < 4000) { await tick(); }")]
    [InlineData("if (limit <= performance.now() - start) stop();")]
    [InlineData("const remaining = deadline - Date.now();")]
    [InlineData("const ok = Date.now() - start >= 10;")]
    public void RelationalOperandOrDeadlineIsNotMeasured(string code) => Assert.DoesNotContain(TimingRule.WallClockDifference, Rules(code));

    [Theory]
    [InlineData("// while (performance.now() < end) {} Date.now() - t")]
    [InlineData("/* performance.now = () => 0; */ const a = 1;")]
    [InlineData("const message = 'Date.now() - t and while (Date.now() < end) {}';")]
    [InlineData("const message = \"import x from 'node:child_process'\";")]
    [InlineData("const text = `performance.now = 0; Date.now() - start`;")]
    [InlineData("const pattern = /Date.now() - x/g;")]
    public void RuleTokensInCommentsStringsAndRegexesAreIgnored(string code) => Assert.Empty(Rules(code));

    [Fact]
    public void TemplateSubstitutionIsScannedAsCode() =>
        Assert.Equal([TimingRule.WallClockDifference], Rules("console.log(`elapsed ${Date.now() - start} ms`);"));

    [Fact]
    public void NestedTemplateSubstitutionIsScannedAsCode() =>
        Assert.Equal([TimingRule.WallClockDifference], Rules("const s = `a ${`b ${performance.now() - t}`} c`;"));

    [Fact]
    public void DivisionIsNotARegexLiteral() =>
        Assert.Equal([TimingRule.WallClockDifference], Rules("const half = total / 2, elapsed = Date.now() - start;"));

    [Fact]
    public void RegexAfterKeywordIsALiteral() => Assert.Empty(Rules("function f(x) { return /performance.now = 1/.test(x); }"));

    [Fact]
    public void HitCarriesLineAndExcerpt()
    {
        var hit = Assert.Single(TimingDetector.Scan("suite.ts", "const a = 1;\n\nconst elapsed = Date.now() - start;\n"));
        Assert.Equal(("suite.ts", 3, "const elapsed = Date.now() - start;"), (hit.File, hit.Line, hit.Excerpt));
    }

    [Fact]
    public void SharedModuleHitIsAttributedToEveryImportingSuite()
    {
        var root = Directory.CreateTempSubdirectory("e2e-detector-");
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, "tools", "e2e"));
            File.WriteAllText(Path.Combine(root.FullName, "tools", "e2e", "a.test.ts"), "import { spin } from './shared.ts';\nspin();\n");
            File.WriteAllText(Path.Combine(root.FullName, "tools", "e2e", "b.test.ts"), "import x from '../helper.mjs';\n");
            File.WriteAllText(Path.Combine(root.FullName, "tools", "helper.mjs"), "import { spin } from './e2e/shared.ts';\nexport default spin;\n");
            File.WriteAllText(Path.Combine(root.FullName, "tools", "e2e", "shared.ts"),
                "export function spin() {\n  const end = performance.now() + 5;\n  while (performance.now() < end) {}\n}\n");
            foreach (var suite in new[] { "tools/e2e/a.test.ts", "tools/e2e/b.test.ts" })
            {
                var hit = Assert.Single(TimingDetector.ScanSuite(root.FullName, suite));
                Assert.Equal(("tools/e2e/shared.ts", 3, TimingRule.BusyWaitLoop), (hit.File, hit.Line, hit.Rule));
            }
            Assert.Equal(["tools/e2e/b.test.ts", "tools/e2e/shared.ts", "tools/helper.mjs"],
                TimingDetector.ImportClosure(root.FullName, "tools/e2e/b.test.ts"));
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void MissingImportedModuleFails()
    {
        var root = Directory.CreateTempSubdirectory("e2e-detector-");
        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "a.test.ts"), "import './gone.ts';\n");
            Assert.Throws<FileNotFoundException>(() => TimingDetector.ImportClosure(root.FullName, "a.test.ts"));
        }
        finally { root.Delete(true); }
    }
}
