// Session close findings for the Chrome suites (Story 9.4b, blind hunter 13). A failure found when a session closes
// (a clock mapping recalibrated, a malformed clock reply, the browser failing to close) must fail its test, but it
// must never replace a failure the test already threw: suites close in `finally` blocks, where a thrown error
// discards the one in flight. WorkshopDriver.close() records each finding here instead of throwing; root hooks raise
// them after each test. A suite's own `after` hook closes with WorkshopDriver.closeAfterSuite(), which raises its
// findings at once (nothing is in flight in a suite hook to mask), so node reports them against that suite (Murdoch-2
// R2-F6). The root `after` hook raises anything still recorded once the file's suites end. A test that already failed
// keeps its own error, and every finding is still printed.
import { after, afterEach } from 'node:test';

/** The session faults a Workshop session can raise (OD7, Story 9R.0a). Every observed fault is a close finding, except the one fault a
 *  test declares it provokes (WorkshopDriver.closeExpecting): that one must occur, and must be the only finding. */
export const SessionFault = Object.freeze({ DebtFault: 1, ClockRecalibration: 2, ClockReplyRejected: 3 });
const FaultName = new Map(Object.entries(SessionFault).map(([name, value]) => [value, name]));

function faultFinding(fault, records) {
    const first = String(records[0]).slice(0, 400);
    switch (fault) {
        case SessionFault.DebtFault: return new Error(`The simulation stopped on ${records.length} debt fault(s); first: ${first}`);
        case SessionFault.ClockRecalibration: return new Error(`The session recalibrated a clock mapping ${records.length} time(s); first: ${first}`);
        case SessionFault.ClockReplyRejected: return new Error(`The session rejected ${records.length} malformed clock reply(ies); first: ${first}`);
        default: throw new TypeError(`Unknown session fault ${fault}.`);
    }
}

/** The findings for a session's observed faults. `observed` maps SessionFault values to the session's records of each; `expected` is
 *  undefined, or the one SessionFault the test declared: it is then required to have occurred, and it alone is not a finding. */
export function sessionFaultFindings(observed, expected) {
    if (!(observed instanceof Map)) throw new TypeError('Observed session faults must be a Map of SessionFault to records.');
    if (expected !== undefined && !FaultName.has(expected)) throw new TypeError(`Unknown expected session fault ${expected}.`);
    const findings = [];
    for (const [fault, records] of observed) {
        if (!FaultName.has(fault) || !Array.isArray(records)) throw new TypeError(`Unknown session fault ${fault}.`);
        if (records.length > 0 && fault !== expected) findings.push(faultFinding(fault, records));
    }
    if (expected !== undefined && !(observed.get(expected)?.length > 0))
        findings.push(new Error(`The session was expected to raise ${FaultName.get(expected)}, but it did not`));
    return findings;
}

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
