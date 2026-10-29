import { CloseFindingScenario } from './close-finding-scenario.js';
import assert from 'node:assert/strict';
import { describe, after, test } from 'node:test';
import { recordCloseFinding, raiseCloseFindings } from '../workshop-close-findings.js';
const close = (message: string) => recordCloseFinding(new Error(message));
const closeAfterSuite = (message: string) => raiseCloseFindings([new Error(message)], 'after the suite');
const scenario: CloseFindingScenario = Number(process.env.CLOSE_FINDING_SCENARIO);
if (!Object.values(CloseFindingScenario).includes(scenario)) throw Error('Unknown close-findings fixture scenario');
switch (scenario) {
case CloseFindingScenario.Finding: { test('passes', () => { try { } finally { close('recalibrated a clock mapping'); } }); break; }
case CloseFindingScenario.PrimaryFailure: { test('fails', () => { try { assert.equal(1, 2, 'the primary failure'); } finally { close('rejected a clock reply'); } }); break; }
case CloseFindingScenario.MultipleFindings: { test('two', () => { close('first finding'); close('second finding'); }); break; }
case CloseFindingScenario.UnownedFinding: { describe('suite', () => { test('passes', () => {}); after(() => close('closed after the suite')); }); break; }
case CloseFindingScenario.SuiteFinding: { describe('the suite', () => { test('passes', () => {}); after(() => closeAfterSuite('closed after the suite')); }); break; }
case CloseFindingScenario.CleanSuite: { describe('the suite', () => { test('passes', () => {}); after(() => raiseCloseFindings([], 'after the suite')); }); break; }
case CloseFindingScenario.CleanTest: { test('clean', () => { try { } finally { } }); break; }
case CloseFindingScenario.FindingOnce: { test('first', () => close('only once')); test('second', () => {}); break; }
default: throw Error('Unknown close-findings fixture scenario');
}
