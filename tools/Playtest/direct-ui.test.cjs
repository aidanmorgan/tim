// Unit tests for the JavaScript-only Playwright MCP adapter, not game logic.
// The fake page exercises event handling; it is not browser-playtest evidence.
const { test } = require("node:test");
const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");
const { runInNewContext } = require("node:vm");

const source = readFileSync(join(__dirname, "direct-ui.js"), "utf8");
for (const [kind, dimensions] of [["ramp",[3,2,.25]], ["wall",[0,2,.25]],
    ["wall",[3,2,4]], ["wall",[3,NaN,.25]], ["wall",[3,2]], ["wall","invalid"], ["pipe",[.5,1.3,1.3]], ["pipe",[3.6,2,1.3]],
    ["pipe",[9,1.3,1.3]], ["pipe",[NaN,1.3,1.3]], ["pipe",[3.6,1.3,2]]]) {
    test("reject invalid resize recipe " + kind + " " + JSON.stringify(dimensions), async () => {
        const fn = runInNewContext("(" + source + ")");
        await assert.rejects(fn({}, {level:1,precision:.45,parts:[{kind,dimensions}]}), /Resize dimensions/);
    });
}
for (const link of [
    {from:"a",to:"b",type:"rope",from_port:"tie",to_port:"tie"},
    {from:"a",to:"b",type:"rope",from_port:"tie",to_port:"tie",rope_length:-1},
    {from:"a",to:"b",type:"rope",from_port:"tie",to_port:"tie",rope_length:Infinity},
    {from:"a",to:"b",type:"rope",from_port:"tie",to_port:"tie",rope_length:201},
    {from:"a",to:"b",type:"electrical",from_port:"supply",to_port:"power_in",rope_length:1},
    {from:"a",to:"b",type:"mechanical"},
    {from:"a",to:"b",type:"mechanical",from_port:"drive_in",to_port:"drive"},
    {from:"a",to:"b",type:"power"},
    {from:"a",to:"b",type:"activation"},
    {from:"a",to:"b",type:"electrical",from_port:"out",to_port:"in"},
    {from:"a",to:"b",type:"activation",from_port:"wrong",to_port:"activation_in"}
]) {
    test("reject unsupported connection recipe " + JSON.stringify(link), async () => {
        const fn = runInNewContext("(" + source + ")");
        await assert.rejects(fn({}, {level:1,precision:.45,connections:[link]}), /explicit supported connection sockets/);
    });
}
async function attemptWith(mode) {
    let now = 0, listener, pointer, releaseShift = 0;
    const screenshots = [];
    const ui = { level: 1, precision: .45, running: false, parts: [],
        buttons: [
            { action: "▶  Run machine", enabled: true, screen: [10,10] },
            { action: "↶  Build again", enabled: true, screen: [20,20] }
        ] };
    const state = { level: 1, precision: .45, parts: [], connections: [] };
    const emit = (prefix, value) => listener?.({
        text: () => prefix + JSON.stringify(value), type: () => "log"
    });
    const page = {
        on: (_, callback) => listener = callback,
        off: (_, callback) => { assert.equal(listener, callback); listener = undefined; },
        setViewportSize: async () => {},
        goto: async () => emit("CCUI ", ui),
        waitForTimeout: async milliseconds => { now += milliseconds; },
        screenshot: async ({ path }) => {
            if (mode === "capture-error") throw new Error("capture unavailable");
            screenshots.push(path);
        },
        keyboard: { press: async () => {}, up: async () => { releaseShift++; } },
        mouse: {
            move: async (x,y) => { pointer = [x,y]; },
            down: async () => {},
            wheel: async () => {},
            up: async () => {
                if (!listener) return;
                if (pointer?.[0] === 10) {
                    if (mode === "missing-run" || mode === "capture-error") return;
                    emit("CCRUN ", state);
                    emit("CCFRAME ", { tick: 0, parts: [] });
                    if (mode === "early-reset") emit("CCRESET ", state);
                    if (mode === "second-run") emit("CCRUN ", { ...state, level: 9 });
                    if (mode === "malformed") listener({ text: () => "CCFRAME {", type: () => "log" });
                    emit("CCRESULT ", { level: 1, precision: .45, tick: 4, outcome: "won" });
                    if (mode === "second-result")
                        emit("CCRESULT ", { level: 1, precision: .45, tick: 4, outcome: "won" });
                }
                if (pointer?.[0] === 20) {
                    emit("CCRESET ", state);
                    if (mode === "second-reset") emit("CCRESET ", state);
                    emit("CCUI ", ui);
                }
            }
        }
    };
    const fn = runInNewContext("(" + source + ")", { Date: { now: () => now } });
    const result = await fn(page, { caseId: "unit-" + mode, level: 1,
        precision: .45, parts: mode === "construction-failure" ? [{ kind: "ramp" }] : [],
        connections: [] });
    assert.equal(listener, undefined, "listener must be detached on every path");
    assert.equal(releaseShift, 1, "Shift must be released");
    return { result, screenshots };
}

test("one Run/result/Reset completes and preserves ordered lifecycle", async () => {
    const { result, screenshots } = await attemptWith("normal");
    assert.equal(result.failure, undefined);
    assert.equal(result.lifecycle.map(e => e.kind).join(","), "run,result,reset");
    assert.equal(result.frames.length, 1);
    assert.equal(result.reset.level, 1);
    assert.equal(screenshots.length, 1);
});
for (const [mode, message] of [
    ["early-reset", "Unexpected reset event during running"],
    ["second-run", "Unexpected run event during running"],
    ["second-result", "Unexpected result event during running"],
    ["second-reset", "Unexpected reset event during resetting"],
    ["malformed", "Invalid diagnostic event:"],
    ["missing-run", "Timed out: run start diagnostics"],
    ["construction-failure", "Palette part unavailable: ramp"]
]) {
    test(mode + " captures failure instead of reporting a completed attempt", async () => {
        const { result, screenshots } = await attemptWith(mode);
        assert.ok(result.failure.message.startsWith(message), result.failure.message);
        assert.ok(screenshots.some(p => p.endsWith("-failure.png")));
        assert.ok(result.actions.length > 0);
        if (mode === "second-run") assert.equal(result.run.level, 1, "first run must survive");
    });
}
test("screenshot errors preserve the original failure and cleanup", async () => {
    const { result } = await attemptWith("capture-error");
    assert.equal(result.failure.message, "Timed out: run start diagnostics");
    assert.ok(result.errors.includes("Failure screenshot: capture unavailable"));
});
