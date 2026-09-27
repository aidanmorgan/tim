// Thin JavaScript adapter required by Playwright MCP's runner.
// Gameplay and read-only diagnostic instrumentation remain C#.
// Use as: await (directUiAttempt source)(page, attemptRecipe).
// This adapter never evaluates game code, edits storage, loads saves, sets transforms,
// uses numeric placement menus, invokes game methods, or synthesizes success.
async function directUiAttempt(page, attempt) {
    if (!Number.isInteger(attempt.level) || attempt.level < 1 || attempt.level > 75)
        throw new Error("Expected a campaign level in the planned range 1..75");
    if (![0, 0.45, 1].includes(attempt.precision))
        throw new Error("Use Forgiving, Balanced or Precise");
    for (const part of attempt.parts ?? []) {
        if (part.finalMoves != null && (!Array.isArray(part.finalMoves) || part.finalMoves.some(m =>
            !m || ![0,1,2].includes(m.axis) || !Number.isFinite(m.amount) || Math.abs(m.amount) > 14)))
            throw new Error("Final moves require a valid axis and finite bounded distance");
        if (part.dimensions == null) continue;
        const bounds = {
            wall: { min: [.4,.4,.12], max: [8,6,2] },
            pipe: { min: [1,1.3,1.3], max: [8,1.3,1.3] }
        }[part.kind];
        if (!bounds || !Array.isArray(part.dimensions) || part.dimensions.length !== 3 ||
            part.dimensions.some((n, axis) => !Number.isFinite(n) || n < bounds.min[axis] || n > bounds.max[axis]))
            throw new Error("Resize dimensions must match the part's supported axes and limits");
    }
    for (const link of attempt.connections ?? []) {
        const activation = link.type === "activation" && link.from_port === "activation_out" &&
            ["activation_in", "set_in", "reset_in"].includes(link.to_port);
        const motorSupply = link.type === "electrical" && link.from_port === "supply" &&
            ["power_in", "first_in", "second_in"].includes(link.to_port);
        const mechanical = link.type === "mechanical" && link.from_port === "drive" &&
            link.to_port === "drive_in";
        const rope = link.type === "rope" && link.from_port === "tie" && link.to_port === "tie" &&
            Number.isFinite(link.rope_length) && link.rope_length >= .05 && link.rope_length <= 200;
        if ((link.type !== "rope" && link.rope_length != null) ||
            (!activation && !motorSupply && !mechanical && !rope))
            throw new Error("UI driver requires explicit supported connection sockets");
    }
    const evidence = { caseId: attempt.caseId, level: attempt.level, precision: attempt.precision,
        method: "palette-and-3d-handles", recipe: attempt, actions: [], frames: [], errors: [] };
    if (!/^[a-zA-Z0-9_-]+$/.test(attempt.caseId)) throw new Error("Invalid case ID");
    let ui, run, outcome, reset, observerFailure;
    let phase = "building";
    evidence.lifecycle = [];
    const checkObserver = () => {
        if (observerFailure) throw new Error(observerFailure);
    };
    const onConsole = message => {
        const text = message.text();
        if (message.type() === "error") evidence.errors.push(text);
        try {
            if (text.startsWith("CCUI ")) ui = JSON.parse(text.slice(5));
            if (text.startsWith("CCFRAME ")) evidence.frames.push(JSON.parse(text.slice(8)));
            for (const [prefix, kind] of [["CCRUN ", "run"], ["CCRESULT ", "result"], ["CCRESET ", "reset"]]) {
                if (!text.startsWith(prefix)) continue;
                const value = JSON.parse(text.slice(prefix.length));
                evidence.lifecycle.push({ kind, phase, value });
                const expected = kind === "run" ? phase === "running" && !run :
                    kind === "result" ? phase === "running" && run && !outcome :
                    phase === "resetting" && outcome && !reset;
                if (!expected) {
                    observerFailure ??= "Unexpected " + kind + " event during " + phase;
                    continue; // Preserve the first run rather than overwriting its identity.
                }
                if (kind === "run") run = value;
                if (kind === "result") outcome = value;
                if (kind === "reset") reset = value;
            }
        } catch (error) {
            observerFailure ??= "Invalid diagnostic event: " + error.message;
        }
    };
    const waitFor = async (predicate, description, timeout = 12000) => {
        const end = Date.now() + timeout;
        while (true) {
            checkObserver();
            if (predicate()) return;
            if (Date.now() >= end) throw new Error("Timed out: " + description);
            await page.waitForTimeout(40);
        }
    };
    const click = async (at, label) => {
        evidence.actions.push({ type: "click", label, at });
        await page.mouse.move(...at);
        await page.waitForTimeout(100);
        await page.mouse.down();
        await page.waitForTimeout(65);
        await page.mouse.up();
        await page.waitForTimeout(120);
    };
    const action = async label => {
        await waitFor(() => ui?.buttons.some(b => b.action === label && b.enabled && !b.clipped), label);
        await click(ui.buttons.find(b => b.action === label && b.enabled && !b.clipped).screen, label);
    };
    const dragPath = async (points, label, snap = false) => {
        evidence.actions.push({ type: "drag", label, points, snap });
        await page.mouse.move(...points[0]);
        await page.waitForTimeout(100);
        if (snap) await page.keyboard.down("Shift");
        try {
            await page.mouse.down();
            await page.waitForTimeout(65);
            for (const point of points.slice(1)) {
                await page.mouse.move(...point);
                await page.waitForTimeout(20);
            }
            await page.mouse.up();
        } finally {
            if (snap) await page.keyboard.up("Shift");
        }
        await page.waitForTimeout(150);
    };
    const project = ([x,y,z]) => [
        720 + (x*Math.cos(.6) - z*Math.sin(.6))*900/13.8,
        450 - ((y-3.6)*Math.cos(.48) - (x*Math.sin(.6)+z*Math.cos(.6))*Math.sin(.48))*900/13.8
    ];
    const move = async (axis, amount) => {
        if (Math.abs(amount) < 0.00001) return;
        await action("Move mode");
        await waitFor(() => ui.mode === "move" && ui.handles.length === 3, "move handles");
        const h = ui.handles.find(handle => handle.axis === axis);
        const points = Array.from({length: 17}, (_, i) => [
            h.screen[0]+h.unit[0]*amount*i/16, h.screen[1]+h.unit[1]*amount*i/16
        ]);
        await dragPath(points, "move axis " + axis + " by " + amount, attempt.snapMovement === true);
    };
    const rotate = async (axis, degrees) => {
        if (Math.abs(degrees) < 0.00001) return;
        await action("Rotate mode");
        await waitFor(() => ui.mode === "rotate" && ui.handles.length === 3, "rotation handles");
        const h = ui.handles.find(handle => handle.axis === axis), center = ui.center;
        const steps = Math.max(12, Math.ceil(Math.abs(degrees)/5));
        const points = Array.from({length: steps+1}, (_, i) => {
            const angle = degrees*Math.PI/180*i/steps;
            return [0,1].map(k => center[k]+(h.screen[k]-center[k])*Math.cos(angle)+
                (h.quarter[k]-center[k])*Math.sin(angle));
        });
        await dragPath(points, "rotate axis " + axis + " by " + degrees,
            Math.abs(degrees/15-Math.round(degrees/15)) < 0.0001);
    };
    page.on("console", onConsole);
    try {
        await page.setViewportSize({ width: 1440, height: 900 });
        await page.goto("http://127.0.0.1:8060");
        await waitFor(() => ui?.buttons.length > 0, "rendered workshop");
        await page.waitForTimeout(200);
        await click([700,47], "puzzle selector");
        await page.keyboard.press("Home", { delay: 70 });
        // The popup initially has no keyboard-focused item; first Down focuses row 1.
        for (let i=0;i<attempt.level;i++) {
            await page.keyboard.press("ArrowDown", { delay: 70 });
            await page.waitForTimeout(45);
        }
        await page.keyboard.press("Enter", { delay: 70 });
        await waitFor(() => ui.level === attempt.level, "requested puzzle");
        if (attempt.precision !== 0.45) {
            await action("Menu");
            await waitFor(() => ui.menuOpen, "difficulty menu");
            await click(attempt.precision === 0 ? ui.difficultyLeft : ui.difficultyRight, "difficulty slider");
            await waitFor(() => Math.abs(ui.precision-attempt.precision) < 0.001, "difficulty value");
            await action("Menu");
        }
        const placed = new Map();
        for (const part of attempt.parts) {
            const before = new Set(ui.parts.map(p=>p.id));
            // Scroll the actual inventory, never set its value or the game's inventory.
for (let tries=0;tries<40;tries++) {
                const target = ui.buttons.find(b=>b.kind===part.kind && b.enabled);
                if (target && !target.clipped) { await click(target.screen, "palette "+part.kind); break; }
                if (!target || tries === 39) throw new Error("Palette part unavailable: "+part.kind);
                await page.mouse.move(130,190);
                await page.waitForTimeout(100);
                await page.mouse.wheel(0,target.screen[1]<190 ? -120 : 120);
                await page.waitForTimeout(120);
            }
            // Pixel rounding can put an exact boundary click outside the build area.
            // Stage just inside, then use the real arrows for the remaining displacement.
            const stageX = Math.max(-6.9, Math.min(6.9, part.position[0]));
            const stageZ = Math.max(-3.9, Math.min(3.9, part.position[2]));
            await click(project([stageX,3,stageZ]), "place "+part.slot);
            await waitFor(() => ui.parts.some(p=>!before.has(p.id)), "placed part");
            const created = ui.parts.find(p=>!before.has(p.id));
            placed.set(part.slot, created.id);
            await waitFor(() => ui.selected === created.id && ui.handles.length===3, "selected part handles");
            const offset = part.offset ?? [0,0,0];
            await move(1, part.position[1]-3+offset[1]);
            await move(0, part.position[0]-stageX+offset[0]);
            await move(2, part.position[2]-stageZ+offset[2]);
            const rotation = part.rotation ?? [0,0,0], error = part.rotationOffset ?? [0,0,0];
            // Godot uses Y-X-Z Euler composition: apply world Z, X, then Y.
            for (const axis of [2,0,1]) await rotate(axis,rotation[axis]+error[axis]);
        }
        for (const part of attempt.parts ?? []) {
            if (!part.dimensions) continue;
            const id = placed.get(part.slot);
            if (ui.selected !== id) await click(ui.parts.find(p => p.id === id).screen, "select part to resize");
            await action("Resize mode");
            await waitFor(() => ui.selected === id && ui.mode === "resize" && ui.dimensions?.length === 3, "part resize handles");
            for (const axis of [0,1,2]) {
                // Read rendered handle geometry and drag; never set game dimensions.
                for (let pass = 0; pass < 3; pass++) {
                    const difference = part.dimensions[axis] - ui.dimensions[axis];
                    if (Math.abs(difference) < .03) break;
                    const h = ui.handles.find(handle => handle.axis === axis);
                    const points = Array.from({length:17}, (_,i) => [
                        h.screen[0] + h.unit[0] * difference * .5 * i/16,
                        h.screen[1] + h.unit[1] * difference * .5 * i/16]);
                    await dragPath(points, "resize local axis " + axis + " toward " + part.dimensions[axis]);
                }
                if (Math.abs(part.dimensions[axis] - ui.dimensions[axis]) >= .03)
                    throw new Error("Resize did not reach requested dimension on axis " + axis);
            }
        }
        for (const part of attempt.parts ?? []) {
            if (!part.finalMoves?.length) continue;
            const id = placed.get(part.slot);
            if (ui.selected !== id) await click(ui.parts.find(p => p.id === id).screen, "select part for final movement");
            for (const adjustment of part.finalMoves) await move(adjustment.axis, adjustment.amount);
        }
        for (const link of attempt.connections ?? []) {
            await page.keyboard.press("Escape");
            await page.waitForTimeout(120);
            const from = placed.get(link.from) ?? link.from, to = placed.get(link.to) ?? link.to;
            const source = ui.parts.find(p=>p.id===from), target = ui.parts.find(p=>p.id===to);
            if (!source || !target) throw new Error("Missing visible wiring endpoint");
            await click(source.screen, "select wire source "+from);
            await waitFor(() => ui.selected===from, "wire source selection");
            await action("Connect");
            await click(target.screen, "connect to "+to);
            const choice = {activation: {activation_in:"Connect activation", set_in:"Connect set", reset_in:"Connect reset"}[link.to_port],electrical:{power_in:"Connect electricity",first_in:"Connect first input",second_in:"Connect second input"}[link.to_port],
                mechanical:"Connect drive",rope:"Connect rope"}[link.type];
            if (ui.buttons.some(b => b.action === choice && b.enabled && !b.clipped))
                await action(choice);
        }
        phase = "running";
        await action("▶  Run machine");
        await waitFor(() => run, "run start diagnostics");
        if (run.level !== attempt.level || Math.abs(run.precision-attempt.precision)>0.001)
            throw new Error("Run started with the wrong level or difficulty");
        for (const link of attempt.connections ?? []) {
            const from = placed.get(link.from) ?? link.from, to = placed.get(link.to) ?? link.to;
            if (!run.connections.some(c => c.from === from && c.to === to &&
                c.type === link.type && c.fromPort === link.from_port && c.toPort === link.to_port &&
                (link.type !== "rope" || Math.abs(c.ropeLength - link.rope_length) < .0001)))
                throw new Error("Required UI connection missing or incorrect: " + from + " -> " + to);
        }
        await waitFor(() => outcome, "win or simulation timeout", 55000);
        await page.waitForTimeout(150);
        evidence.slots = Object.fromEntries(placed);
        evidence.run = run;
        evidence.outcome = outcome;
        evidence.ui = ui;
        evidence.outcomeScreenshot = ".playwright-mcp/" + attempt.caseId + "-outcome.png";
        await page.screenshot({ path: evidence.outcomeScreenshot });
        checkObserver();
        phase = "resetting";
        await action(outcome.outcome === "won" ? "↶  Build again" : "■  Back to building");
        await waitFor(() => reset && ui && !ui.running, "reset to building");
        evidence.reset = reset;
        evidence.resetUi = ui;
        return evidence;
    } catch (error) {
        evidence.failure = { phase, message: error.message };
        evidence.run = run;
        evidence.outcome = outcome;
        evidence.reset = reset;
        evidence.ui = ui;
        evidence.failureScreenshot = ".playwright-mcp/" + attempt.caseId + "-failure.png";
        try { await page.screenshot({ path: evidence.failureScreenshot }); }
        catch (captureError) { evidence.errors.push("Failure screenshot: " + captureError.message); }
        return evidence; // Callers must persist failures and stop the batch.
    } finally {
        page.off("console", onConsole);
        try { await page.mouse.up(); }
        catch (error) { evidence.errors.push("Release mouse: " + error.message); }
        try { await page.keyboard.up("Shift"); }
        catch (error) { evidence.errors.push("Release Shift: " + error.message); }
    }
}
