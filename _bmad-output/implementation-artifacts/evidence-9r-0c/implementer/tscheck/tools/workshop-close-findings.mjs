// Session close findings for the Chrome suites (Story 9.4b, blind hunter 13). A failure found when a session closes
// (a clock mapping recalibrated, a malformed clock reply, the browser failing to close) must fail its test, but it
// must never replace a failure the test already threw: suites close in `finally` blocks, where a thrown error
// discards the one in flight. WorkshopDriver.close() records each finding here instead of throwing; root hooks raise
// them after each test. A suite's own `after` hook closes with WorkshopDriver.closeAfterSuite(), which raises its
// findings at once (nothing is in flight in a suite hook to mask), so node reports them against that suite (Murdoch-2
// R2-F6). The root `after` hook raises anything still recorded once the file's suites end. A test that already failed
// keeps its own error, and every finding is still printed.
import { after, afterEach } from 'node:test';

const pending = [];

export function recordCloseFinding(error) {
    if (!(error instanceof Error)) throw new TypeError('A session close finding must be an Error.');
    pending.push(error);
}

/** Prints every finding and throws them as one error (or an AggregateError); returns quietly when there are none. */
export function raiseCloseFindings(findings, scope) {
    if (!Array.isArray(findings) || !findings.every(finding => finding instanceof Error))
        throw new TypeError('Session close findings must be Errors.');
    if (findings.length === 0) return;
    for (const finding of findings) console.error(`Session close finding (${scope}): ${finding.message}`);
    throw findings.length === 1 ? findings[0] : new AggregateError(findings, `${findings.length} session close findings (${scope}): ` +
        findings.map(finding => finding.message).join(' | '));
}

afterEach(() => raiseCloseFindings(pending.splice(0), 'after the test'));
after(() => raiseCloseFindings(pending.splice(0), 'after the suites'));
