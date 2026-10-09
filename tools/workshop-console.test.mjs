import assert from 'node:assert/strict';
import test from 'node:test';
import { isWorkshopConsoleFailure } from './workshop-console.mjs';

const trace = 'CCGPU_TRACE_CHUNK 1 0 1 0 1 NaNA';
test('encoded trace bytes containing NaN are not diagnostic text', () => {
    assert.equal(isWorkshopConsoleFailure('log', trace), false);
    assert.equal(isWorkshopConsoleFailure('log', 'Workshop ready'), false);
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
