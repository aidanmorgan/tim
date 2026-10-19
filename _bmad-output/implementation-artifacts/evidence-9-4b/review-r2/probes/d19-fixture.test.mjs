// Murdoch-2 round 2 reviewer fixture for D19 (not part of the deliverable). Imports the shipped module read-only.
import { describe, test, after } from 'node:test';
import { recordCloseFinding } from '/Users/aidan/dev/personal/tim/tools/workshop-close-findings.mjs';

test('A passes but its session recorded a close finding', () => {
    recordCloseFinding(new Error('FINDING-A recalibrated'));
});

test('B fails with its own error and also recorded a close finding', () => {
    recordCloseFinding(new Error('FINDING-B malformed reply'));
    throw new Error('OWN-ERROR-B');
});

test('C passes with no finding (control)', () => {});

describe('D suite whose after hook closes a session with a finding', () => {
    test('D1 passes', () => {});
    after(() => recordCloseFinding(new Error('FINDING-D suite close')));
});
