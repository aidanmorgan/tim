// Read-only analysis: attribute each ✔/✖ line of a cumulative node --test log to its suite file by test title.
import { readFileSync, readdirSync } from 'node:fs';
const root = '/Users/aidan/dev/personal/tim/tools/e2e/';
const files = readdirSync(root).filter(f => f.endsWith('.test.ts')).sort();
const prefixes = files.map(file => {
    const src = readFileSync(root + file, 'utf8');
    const titles = [...src.matchAll(/\btest\(\s*(['"`])((?:(?!\1)[^\\]|\\.)*)/g)].map(m => m[2].split('${')[0].split("'+")[0].split('"+')[0]);
    return { file, titles: titles.filter(t => t.length > 3) };
});
for (const log of process.argv.slice(2)) {
    const lines = readFileSync(log, 'utf8').split('\n');
    const end = lines.findIndex(l => l.startsWith('✖ failing tests:'));
    const body = end < 0 ? lines : lines.slice(0, end);
    const counts = new Map(files.map(f => [f, { pass: 0, fail: 0 }]));
    const unmatched = [];
    for (const line of body) {
        const m = line.match(/^\s*([✔✖]) (.*) \(\d+(?:\.\d+)?ms\)$/);
        if (!m) continue;
        const title = m[2];
        const owner = prefixes.find(p => p.titles.some(t => title.startsWith(t)));
        if (!owner) { if (!/^CAT-023b: Domino cascade/.test(title)) unmatched.push(title); continue; }
        const c = counts.get(owner.file); if (m[1] === '✔') c.pass++; else c.fail++;
    }
    console.log(`== ${log.split('/').pop()}`);
    for (const [f, c] of counts) console.log(`${f.padEnd(28)} ${c.pass}/${c.pass + c.fail}${c.fail ? '  FAIL' : ''}`);
    console.log(`total ${[...counts.values()].reduce((s, c) => s + c.pass, 0)}/${[...counts.values()].reduce((s, c) => s + c.pass + c.fail, 0)}; unmatched: ${unmatched.length ? unmatched.join(' | ') : 'none'}`);
}
