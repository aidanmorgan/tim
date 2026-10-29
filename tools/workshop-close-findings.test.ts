import { CloseFindingScenario } from './runtime/close-finding-scenario.js';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { readdirSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { SessionFault, sessionFaultFindings } from './workshop-close-findings.js';

// Blind hunter 13 (Story 9.4b): WorkshopDriver.close() threw from each suite's `finally`, replacing the test's own failure.
// Its findings now go through workshop-close-findings.ts; these fixtures run under a real node:test runner.
function run(scenario: CloseFindingScenario, reporter = 'spec') {
    const { NODE_TEST_CONTEXT: _, ...parentEnvironment } = process.env;
    const env = { ...parentEnvironment, CLOSE_FINDING_SCENARIO: String(scenario) };
    const result = spawnSync(process.execPath, ['--test', `--test-reporter=${reporter}`, new URL('./runtime/close-findings-fixture.js', import.meta.url).pathname], { encoding: 'utf8', env });
    return { status: result.status, output: result.stdout + result.stderr };
}

test('a close finding fails a test that otherwise passed', () => {
    const { status, output } = run(CloseFindingScenario.Finding);
    assert.equal(status, 1);
    assert.match(output, /recalibrated a clock mapping/);
});

test("a close finding never replaces the test's own failure, and is still reported", () => {
    const { status, output } = run(CloseFindingScenario.PrimaryFailure);
    assert.equal(status, 1);
    assert.match(output, /the primary failure/);
    assert.match(output, /Session close finding \(after the test\): rejected a clock reply/);
});

test('several findings of one test are aggregated, not dropped', () => {
    const { status, output } = run(CloseFindingScenario.MultipleFindings);
    assert.equal(status, 1);
    assert.match(output, /2 session close findings/);
    assert.match(output, /first finding/);
    assert.match(output, /second finding/);
});

test("a recorded finding with no test to fail still fails the file (catch-all)", () => {
    const { status, output } = run(CloseFindingScenario.UnownedFinding);
    assert.equal(status, 1);
    assert.match(output, /closed after the suite/);
});

// Murdoch-2 R2-F6: a suite's own `after` hook closes with closeAfterSuite, which fails that suite (nothing is in flight in
// a suite hook to mask), so the finding is reported against the suite, not against workshop-close-findings.ts.
test("a finding raised in a suite's after hook is reported against that suite", () => {
    const { status, output } = run(CloseFindingScenario.SuiteFinding, 'tap');
    assert.equal(status, 1);
    assert.match(output, /^not ok \d+ - the suite$/m);
    assert.match(output, /closed after the suite/);
    assert.doesNotMatch(output, /^not ok \d+ - .*workshop-close-findings/m);
    assert.match(output, /^ {4}ok \d+ - passes$/m, 'its tests keep their own result');
});
test('closeAfterSuite with no findings passes (control)', () => {
    assert.equal(run(CloseFindingScenario.CleanSuite).status, 0);
});

// The whole class: every Chrome suite that closes its driver in a suite `after` hook uses closeAfterSuite, so no suite-level
// finding can fall through to the file-level catch-all.
// The text of every `after(...)` hook call, by balanced parentheses (any hook form: async or not, braces or an expression).
function afterHooks(source: string) {
    const hooks = [];
    for (const match of source.matchAll(/\bafter\(/g)) {
        let depth = 0, at = match.index + match[0].length - 1;
        for (; at < source.length; at++) {
            if (source[at] === '(') depth++;
            else if (source[at] === ')' && --depth === 0) break;
        }
        hooks.push(source.slice(match.index, at + 1));
    }
    return hooks;
}
const closesInHook = (source: string) => afterHooks(source).some(hook => /\.close\(\)/.test(hook));
test('no Chrome suite closes its driver with close() in a suite after hook', () => {
    const directory = new URL('../../tools/e2e/', import.meta.url);
    const offenders = readdirSync(directory).filter(name => name.endsWith('.test.ts'))
        .filter(name => closesInHook(readFileSync(new URL(name, directory), 'utf8')));
    assert.deepEqual(offenders, []);
    // Controls: every hook form is seen, and closeAfterSuite is not close().
    for (const form of ['after(async () => { await driver?.close(); });', 'after(() => driver.close());',
        'after(async () => { if (driver) { await driver.close(); } });'])
        assert.equal(closesInHook(form), true, form);
    assert.equal(closesInHook('after(async () => { await driver?.closeAfterSuite(); });'), false);
});

test('a clean close passes (control), and a finding is raised once, not again for later tests', () => {
    assert.equal(run(CloseFindingScenario.CleanTest).status, 0);
    const { output } = run(CloseFindingScenario.FindingOnce);
    assert.equal(output.match(/Session close finding \(after the test\): only once/g)?.length, 1);
    assert.match(output, /✔ second/);
});

// OD7 (Story 9R.0a): a test that deliberately provokes one session fault declares it (WorkshopDriver.closeExpecting). The declared
// fault must occur and is then the only fault that is not a finding; every other fault, and an undeclared one, still fails.
const observed = (debt: string[], recalibrations: string[] = [], rejections: string[] = []) => new Map([[SessionFault.DebtFault, debt],
    [SessionFault.ClockRecalibration, recalibrations], [SessionFault.ClockReplyRejected, rejections]]);
test('a declared session fault that occurred is not a finding', () => {
    assert.deepEqual(sessionFaultFindings(observed(['CCGPU_DEBT_FAULT: committedTick=9']), SessionFault.DebtFault), []);
});
test('a declared session fault that did not occur is a finding', () => {
    const findings = sessionFaultFindings(observed([]), SessionFault.DebtFault);
    assert.deepEqual(findings.map(finding => finding.message), ['The session was expected to raise DebtFault, but it did not']);
});
test('a different fault than the declared one is a finding, and the declared one is still required', () => {
    const findings = sessionFaultFindings(observed([], ['CCGPU_CLOCK_RECALIBRATED {}']), SessionFault.DebtFault).map(finding => finding.message);
    assert.equal(findings.length, 2);
    assert.match(findings[0], /recalibrated a clock mapping 1 time/);
    assert.match(findings[1], /expected to raise DebtFault, but it did not/);
});
test('a declared fault does not excuse another fault that also occurred', () => {
    const findings = sessionFaultFindings(observed(['CCGPU_DEBT_FAULT: x'], [], ['CCGPU_CLOCK_REPLY_REJECTED {}']), SessionFault.DebtFault);
    assert.deepEqual(findings.map(finding => finding.message), ['The session rejected 1 malformed clock reply(ies); first: CCGPU_CLOCK_REPLY_REJECTED {}']);
});
test('an undeclared debt fault is a finding', () => {
    const line = 'CCGPU_DEBT_FAULT: committedTick=9 reason=ReliableStalled oldestUnreceipted=4 ageMs=501';
    const findings = sessionFaultFindings(observed([line]));
    assert.deepEqual(findings.map(finding => finding.message), [`The simulation stopped on 1 debt fault(s); first: ${line}`]);
    assert.deepEqual(sessionFaultFindings(observed([])), [], 'no fault, nothing declared: no finding (control)');
});
test('unknown session faults are rejected', () => {
    assert.throws(() => Reflect.apply(sessionFaultFindings, undefined, [observed([]), 99]), TypeError);
    assert.throws(() => Reflect.apply(sessionFaultFindings, undefined, [new Map([[99, ['x']]])]), TypeError);
    assert.throws(() => Reflect.apply(sessionFaultFindings, undefined, [{}]), TypeError);
});
