import assert from 'node:assert/strict';
import test from 'node:test';
import { isWorkshopConsoleFailure, reportsNaN } from './workshop-console.mjs';

const trace = 'CCGPU_TRACE_CHUNK 1 0 1 0 1 NaNA';
// Murdoch-2 F1 (Story 9.4b): the exact acknowledgement that failed cat-051 test 4 in his cumulative run 1; its base64
// payload contains the substring "NaN" at offset 93.
const acknowledgement = 'CCGPU NAAAADAAaABcAAAAAAAAAFsAVABMAAAARAAAADwANAAsACgADAAIAAAAAAAAAAAAAAAEADAAAAAcAQAAYAAAANhNaN0CAAAAoIYBAAAAAAACAAAAAAAAAAAAAAAgAQAADwAAAAAAAAABAAAAAAAAAAEAAAAAAAAADQIAAAAAAAALAAAAAAAAABQAAAAAAAACEAAAAAAAAAAAAAAAAgAAAAIAAAAAAAAA/v///zAAAAAAAAAAzMzMPgAAAAAAAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAgAAAQAAAAAAAAAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAqqgFvlXPfT8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEAAAAGBAAAAQAAAAMAAAAAAAAAAABhRQAAAAABAQAAAAAAAAAAAAAQAAAAXDvi3f+KOeg60i1KET+bUQ==';

test('encoded trace bytes containing NaN are not diagnostic text', () => {
    assert.equal(isWorkshopConsoleFailure('log', trace), false);
    assert.equal(isWorkshopConsoleFailure('log', 'Workshop ready'), false);
});

test('a typed base64 envelope never fails on a NaN substring of its payload (Murdoch-2 F1)', () => {
    assert.ok(acknowledgement.includes('NaN'));
    assert.equal(isWorkshopConsoleFailure('log', acknowledgement), false, 'the exact logged acknowledgement');
    for (const text of ['CCGPU_RETAINED AAAANaNAAAAA', 'CCGPU_TICK_TIMES_CHUNK 1 0 4 1 128 AANaNAAA',
        'CCGPU_TICK_TIMES_BEGIN 1 2 2 8 2 1 1 3600 436 4 1 AQAANaN=',
        'CCGPU_CLOCK_DERIVATION {"report":0,"detail":"a NaN in a string field"}'])
        assert.equal(isWorkshopConsoleFailure('log', text), false, text);
    // Controls: a NaN outside a well-formed envelope still fails.
    for (const text of ['CCGPU position=NaN', 'CCGPU AAAA NaN', 'CCGPU_RETAINED NaN', 'ccgpu AAAANaNA', 'XCCGPU AAAANaNA',
        'CCGPU_CLOCK_DERIVATION {"value":NaN}', 'CCGPU_CLOCK_DERIVATION {"x":1} NaN', acknowledgement + ' NaN',
        acknowledgement + '\nNaN', 'CCGPU_RETAINED AAAANaNA====', 'value NaN'])
        assert.equal(isWorkshopConsoleFailure('log', text), true, text);
    assert.equal(isWorkshopConsoleFailure('error', acknowledgement), true, 'an error line is always a failure');
});

test('no Chrome suite checks a raw NaN substring of console text; each goes through reportsNaN (whole class of F1)', async () => {
    const { readdir, readFile } = await import('node:fs/promises');
    const directory = new URL('./e2e/', import.meta.url);
    const offenders = [];
    for (const name of (await readdir(directory)).filter(file => file.endsWith('.ts')))
        if (/\.includes\(\s*['"]NaN['"]\s*\)/.test(await readFile(new URL(name, directory), 'utf8'))) offenders.push(name);
    assert.deepEqual(offenders, []);
    assert.equal(reportsNaN('log', acknowledgement), false);
    assert.equal(reportsNaN('log', 'position=NaN'), true);
});

test('actual errors survive encoded-looking messages and ordinary diagnostics', () => {
    for (const text of [trace, 'GPU device lost', 'value NaN']) {
        assert.equal(isWorkshopConsoleFailure('error', text), true);
    }
    for (const text of ['CCGPU_TRANSPORT_FAILURE', 'CCGPU_STARTUP_EXCEPTION',
        'Unhandled exception', 'position=NaN', trace + ' NaN', trace + '\nNaN',
        'CCGPU_TRACE_CHUNK 1 0 1 0 NaN AAAA',
        'CCGPU_TRACE_CHUNK 1 0 1 0 1 NaN',
        'CCGPU_TRACE_CHUNK 1 0 1 0 1 NaN!',
        'CCGPU_TRACE_CHUNK 1\n 0 1 0 1 NaNA',
        'CCGPU_TRACE_CHUNK 1\r 0 1 0 1 NaNA',
        'CCGPU_TRACE_CHUNK 1 0 1 0 1 NaN\n',
        'CCGPU_TRACE_CHUNK 1 0 1 0 1 NaNA\r\n',
        'CCGPU_TRACE_CHUNK 1 0 1 0 1 NaNA====']) {
        assert.equal(isWorkshopConsoleFailure('log', text), true, text);
    }
    assert.equal(isWorkshopConsoleFailure('warning', trace), true);
});
