// Thin JavaScript adapter required by Playwright MCP's runner.
// Gameplay and read-only diagnostic instrumentation remain C#.
// Use as: await (directUiAttempt source)(page, attemptRecipe).
// This adapter never evaluates game code, edits storage, loads saves, sets transforms,
// uses numeric placement menus, invokes game methods, or synthesizes success.
async function directUiAttempt(page, attempt) {
    if (!Number.isInteger(attempt.level) || attempt.level < 1 || attempt.level > 40)
        throw new Error("Expected campaign level 1..40");
    if (![0, 0.45, 1].includes(attempt.precision))
        throw new Error("Use Forgiving, Balanced or Precise");
    const evidence = { caseId: attempt.caseId, level: attempt.level, precision: attempt.precision,
        method: "palette-and-3d-handles", recipe: attempt, actions: [], frames: [], errors: [] };
    if (!/^[a-zA-Z0-9_-]+$/.test(attempt.caseId)) throw new Error("Invalid case ID");
    let ui, run, outcome, reset;
    const onConsole = message => {
        const text = message.text();
        if (text.startsWith("CCUI ")) ui = JSON.parse(text.slice(5));
        if (text.startsWith("CCRUN ")) run = JSON.parse(text.slice(6));
        if (text.startsWith("CCFRAME ")) evidence.frames.push(JSON.parse(text.slice(8)));
        if (text.startsWith("CCRESET ")) reset = JSON.parse(text.slice(8));
        if (text.startsWith("CCRESULT ")) outcome = JSON.parse(text.slice(9));
        if (message.type() === "error") evidence.errors.push(text);
    };
    const waitFor = async (predicate, description, timeout = 12000) => {
        const end = Date.now() + timeout;
        while (!predicate()) {
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
        const h = ui.handles[axis];
        const points = Array.from({length: 17}, (_, i) => [
            h.screen[0]+h.unit[0]*amount*i/16, h.screen[1]+h.unit[1]*amount*i/16
        ]);
        await dragPath(points, "move axis " + axis + " by " + amount, attempt.snapMovement === true);
    };
    const rotate = async (axis, degrees) => {
        if (Math.abs(degrees) < 0.00001) return;
        await action("Rotate mode");
        await waitFor(() => ui.mode === "rotate" && ui.handles.length === 3, "rotation handles");
        const h = ui.handles[axis], center = ui.center;
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
        await page.keyboard.press("Home");
        // The popup initially has no keyboard-focused item; first Down focuses row 1.
        for (let i=0;i<attempt.level;i++) {
            await page.keyboard.press("ArrowDown");
            await page.waitForTimeout(45);
        }
        await page.keyboard.press("Enter");
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
            for (let tries=0;tries<12;tries++) {
                const button = ui.buttons.find(b=>b.kind===part.kind && b.enabled && !b.clipped);
                if (button) { await click(button.screen, "palette "+part.kind); break; }
                if (tries === 11) throw new Error("Palette part unavailable: "+part.kind);
                await page.mouse.move(130,190);
                await page.waitForTimeout(100);
                await page.mouse.wheel(0,tries<6 ? 55 : -55);
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
        }
        await action("▶  Run machine");
        await waitFor(() => run, "run start diagnostics");
        if (run.level !== attempt.level || Math.abs(run.precision-attempt.precision)>0.001)
            throw new Error("Run started with the wrong level or difficulty");
        await waitFor(() => outcome, "win or simulation timeout", 55000);
        await page.waitForTimeout(150);
        evidence.slots = Object.fromEntries(placed);
        evidence.run = run;
        evidence.outcome = outcome;
        evidence.ui = ui;
        evidence.outcomeScreenshot = ".playwright-mcp/" + attempt.caseId + "-outcome.png";
        await page.screenshot({ path: evidence.outcomeScreenshot });
        await action(outcome.outcome === "won" ? "↶  Build again" : "■  Back to building");
        await waitFor(() => reset && ui && !ui.running, "reset to building");
        evidence.reset = reset;
        evidence.resetUi = ui;
        return evidence;
    } finally {
        await page.mouse.up();
        await page.keyboard.up("Shift");
        page.off("console", onConsole);
    }
}
