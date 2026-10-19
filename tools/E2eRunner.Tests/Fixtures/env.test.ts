// Browserless runner fixture: the suite process sees exactly the runner's explicit environment. Node and the test runner add their
// own NODE_* variables to a child, and macOS adds __CF_USER_TEXT_ENCODING to every process; nothing else may come from the shell that
// started the runner.
import assert from 'node:assert/strict';
import { test } from 'node:test';

const Expected = ['CAT015B_EVIDENCE_DIR', 'CLOCK_DERIVATION_LOG', 'CLOCK_EVIDENCE_SCREENSHOT', 'E2E_PREVIEW_URL', 'HOME', 'PATH',
    'PRELOAD_EVIDENCE_DIR', 'SPRING_EVIDENCE_DIR', 'TMPDIR'];

test('the environment is explicit', () => {
    const names = Object.keys(process.env).filter(name => !name.startsWith('NODE_') && name !== '__CF_USER_TEXT_ENCODING').sort();
    console.log(`fixture environment ${JSON.stringify(names)}`);
    assert.deepEqual(names, Expected);
    assert.equal(process.env.E2E_SLOWMO, undefined);
    assert.equal(process.env.BUMPER_PREVIEW_URL, undefined);
    assert.match(process.env.CLOCK_DERIVATION_LOG ?? '', /env\.derivations\.jsonl$/);
});
