import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { SessionFault, sessionFaultFindings } from './workshop-close-findings.mjs';

// Blind hunter 13 (Story 9.4b): WorkshopDriver.close() threw from each suite's `finally`, replacing the test's own failure.
// Its findings now go through workshop-close-findings.mjs; these fixtures run under a real node:test runner.
const findingsModule = new URL('./workshop-close-findings.mjs', import.meta.url).href;

function run(body, reporter = 'spec') {
    const directory = mkdtempSync(join(tmpdir(), 'close-findings-'));
    try {
        const fixture = join(directory, 'fixture.test.mjs');
        writeFileSync(fixture, `import assert from 'node:assert/strict';\nimport { describe, after, test } from 'node:test';\n` +
            `import { recordCloseFinding, raiseCloseFindings } from ${JSON.stringify(findingsModule)};\n` +
            `const close = message => recordCloseFinding(new Error(message));\n` +
            `const closeAfterSuite = message => raiseCloseFindings([new Error(message)], 'after the suite');\n${body}\n`);
        // An independent runner: without NODE_TEST_CONTEXT the child reports its own exit status instead of streaming to us.
        const { NODE_TEST_CONTEXT: _, ...env } = process.env;
        const result = spawnSync(process.execPath, ['--test', `--test-reporter=${reporter}`, fixture], { encoding: 'utf8', env });
        return { status: result.status, output: result.stdout + result.stderr };
    } finally { rmSync(directory, { recursive: true, force: true }); }
}

test('a close finding fails a test that otherwise passed', () => {
    const { status, output } = run(`test('passes', () => { try { } finally { close('recalibrated a clock mapping'); } });`);
    assert.equal(status, 1);
    assert.match(output, /recalibrated a clock mapping/);
});

test("a close finding never replaces the test's own failure, and is still reported", () => {
    const { status, output } = run(`test('fails', () => { try { assert.equal(1, 2, 'the primary failure'); } finally { close('rejected a clock reply'); } });`);
    assert.equal(status, 1);
    assert.match(output, /the primary failure/);
    assert.match(output, /Session close finding \(after the test\): rejected a clock reply/);
});

test('several findings of one test are aggregated, not dropped', () => {
    const { status, output } = run(`test('two', () => { close('first finding'); close('second finding'); });`);
    assert.equal(status, 1);
    assert.match(output, /2 session close findings/);
    assert.match(output, /first finding/);
    assert.match(output, /second finding/);
});

test("a recorded finding with no test to fail still fails the file (catch-all)", () => {
    const { status, output } = run(`describe('suite', () => { test('passes', () => {}); after(() => close('closed after the suite')); });`);
    assert.equal(status, 1);
    assert.match(output, /closed after the suite/);
});

// Murdoch-2 R2-F6: a suite's own `after` hook closes with closeAfterSuite, which fails that suite (nothing is in flight in
// a suite hook to mask), so the finding is reported against the suite, not against workshop-close-findings.mjs.
test("a finding raised in a suite's after hook is reported against that suite", () => {
    const { status, output } = run(`describe('the suite', () => { test('passes', () => {}); after(() => closeAfterSuite('closed after the suite')); });`, 'tap');
    assert.equal(status, 1);
    assert.match(output, /^not ok \d+ - the suite$/m);
    assert.match(output, /closed after the suite/);
    assert.doesNotMatch(output, /^not ok \d+ - .*workshop-close-findings/m);
    assert.match(output, /^ {4}ok \d+ - passes$/m, 'its tests keep their own result');
});
test('closeAfterSuite with no findings passes (control)', () => {
    assert.equal(run(`describe('the suite', () => { test('passes', () => {}); after(() => raiseCloseFindings([], 'after the suite')); });`).status, 0);
});

// The whole class: every Chrome suite that closes its driver in a suite `after` hook uses closeAfterSuite, so no suite-level
// finding can fall through to the file-level catch-all.
// The text of every `after(...)` hook call, by balanced parentheses (any hook form: async or not, braces or an expression).
function afterHooks(source) {
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
const closesInHook = source => afterHooks(source).some(hook => /\.close\(\)/.test(hook));
test('no Chrome suite closes its driver with close() in a suite after hook', () => {
    const directory = new URL('./e2e/', import.meta.url);
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
    assert.equal(run(`test('clean', () => { try { } finally { } });`).status, 0);
    const { output } = run(`test('first', () => close('only once')); test('second', () => {});`);
    assert.equal(output.match(/Session close finding \(after the test\): only once/g)?.length, 1);
    assert.match(output, /✔ second/);
});

// OD7 (Story 9R.0a): a test that deliberately provokes one session fault declares it (WorkshopDriver.closeExpecting). The declared
// fault must occur and is then the only fault that is not a finding; every other fault, and an undeclared one, still fails.
const observed = (debt, recalibrations = [], rejections = []) => new Map([[SessionFault.DebtFault, debt],
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
    const findings = sessionFaultFindings(observed(['CCGPU_DEBT_FAULT: committedTick=9 reliableStalled=False']));
    assert.deepEqual(findings.map(finding => finding.message),
        ['The simulation stopped on 1 debt fault(s); first: CCGPU_DEBT_FAULT: committedTick=9 reliableStalled=False']);
    assert.deepEqual(sessionFaultFindings(observed([])), [], 'no fault, nothing declared: no finding (control)');
});
test('unknown session faults are rejected', () => {
    assert.throws(() => sessionFaultFindings(observed([]), 99), TypeError);
    assert.throws(() => sessionFaultFindings(new Map([[99, ['x']]])), TypeError);
    assert.throws(() => sessionFaultFindings({}), TypeError);
});
