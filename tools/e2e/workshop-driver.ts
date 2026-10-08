// Pure TypeScript Playwright driver for CuriousContraptions Workshop E2E testing.
// Encapsulates browser interaction, canvas placement, UI actions, and pose ring reading via Playwright.
import { createRequire } from 'node:module';
import type { Browser, BrowserContext, ConsoleMessage, Page, Worker } from 'playwright';

const requireMcp = createRequire('/opt/homebrew/lib/node_modules/@playwright/mcp/package.json');
const { chromium } = requireMcp('playwright') as typeof import('playwright');

export interface WorkshopBodyPose {
    id: number;
    px: number;
    py: number;
    pz: number;
    qx: number;
    qy: number;
    qz: number;
    qw: number;
    vx: number;
    vy: number;
    vz: number;
    flags: number;
}

export interface WorkshopPoseSlot {
    sequence: string;
    timestamp: string;
    bodyCount: number;
    bodies: WorkshopBodyPose[];
}

// One 60 Hz animation-worker sample as recorded by workshop-client.js telemetry.
export interface AnimationSampleRecord {
    target: string;
    kind: number;
    ordinal: string;
    property: number;
    valBits: number;
    value: number;
    timestamp: number;
}

export interface WorkshopDriverOptions {
    baseUrl?: string;
    viewport?: { width: number; height: number };
}

// Telemetry globals installed by workshop-client.js; read inside page.evaluate only (type-only, erased at runtime).
declare global {
    var WorkshopPoseRing: undefined | {
        hasPoseRing(id: number): boolean;
        readLatestPoseSlot(id: number): { sequence: bigint; timestamp: bigint; bodies: WorkshopBodyPose[] } | null;
    };
    var WorkshopAnimation: undefined | {
        isAnimationQualified(id: number): boolean;
        readAnimationSampleCount(id: number): number;
        readLastAnimationSample(id: number, target: string): AnimationSampleRecord | null;
        readAllAnimationSamples(id: number): AnimationSampleRecord[];
        readAnimationSamplesForTarget(id: number, target: string): AnimationSampleRecord[];
    };
}

// Fixed UI element anchor points in standard 1440x900 viewport
const UI_ANCHORS = {
    palette: {
        basketball: { x: 130, y: 155 },
        receiver: { x: 130, y: 202 },
        switch: { x: 130, y: 249 },
        lamp: { x: 130, y: 296 },
        wall: { x: 130, y: 343 },
        delay: { x: 130, y: 390 },
        bumper: { x: 130, y: 450 },
        ramp: { x: 130, y: 155 },
    },
    picker: {
        button: { x: 600, y: 47 },
        first_principles: { x: 600, y: 95 },
        free_workshop: { x: 600, y: 125 },
        delayed_signal: { x: 600, y: 155 },
    },
    menu: {
        button: { x: 1394, y: 46 },
        save: { x: 1056, y: 178 },
        load: { x: 1098, y: 178 },
        pause: { x: 1070, y: 240 },
        resume: { x: 1130, y: 240 },
    },
    dock: {
        // Free workshop lists eight unlimited rows (Ramp appended last), so its part dock sits one row lower than before.
        free_workshop: {
            move: { x: 60, y: 605 },
            rotate: { x: 104, y: 605 },
            resize: { x: 148, y: 605 },
            delete: { x: 192, y: 605 },
        },
        first_principles: {
            move: { x: 40, y: 275 },
            rotate: { x: 72, y: 275 },
            resize: { x: 104, y: 275 },
            delete: { x: 136, y: 275 },
        },
        delayed_signal: {
            move: { x: 40, y: 275 },
            rotate: { x: 72, y: 275 },
            resize: { x: 104, y: 275 },
            delete: { x: 136, y: 275 },
        }
    },
    // Activation wiring buttons rendered by ui/WorkshopConnections.cs inside the parts panel (Godot canvas).
    connections: {
        // "Connect ActivationOut" row: a locked level part shows no configuration rows above it; a Delay shows its duration row first.
        connect: {
            locked: { x: 133, y: 236 },
            delay: { x: 133, y: 277 },
        },
        // "ActivationOut → ActivationIn" choice offered after clicking the target part.
        choice: { x: 133, y: 232 },
    },
    // "Show hint" lightbulb button inside the objective panel (ui/Workshop.cs ShowHint); the panel height follows the task text.
    hint: {
        first_principles: { x: 1186, y: 260 },
        delayed_signal: { x: 1186, y: 214 },
    }
} as const;

export class WorkshopDriver {
    readonly browser: Browser;
    readonly context: BrowserContext;
    readonly page: Page;
    readonly baseUrl: string;
    currentLevel: 'free_workshop' | 'first_principles' | 'delayed_signal' = 'free_workshop';
    capturedCount: number = 0;

    private constructor(browser: Browser, context: BrowserContext, page: Page, baseUrl: string) {
        this.browser = browser;
        this.context = context;
        this.page = page;
        this.baseUrl = baseUrl;
    }

    static async launch(options: WorkshopDriverOptions = {}): Promise<WorkshopDriver> {
        const baseUrl = options.baseUrl ?? 'http://127.0.0.1:8060/';
        const viewport = options.viewport ?? { width: 1440, height: 900 };

        // E2E_HEADED=1 opens a visible Chrome window so a run can be watched live; E2E_SLOWMO=<ms> slows each Playwright action.
        const headed = process.env.E2E_HEADED === '1';
        const slowMo = Number(process.env.E2E_SLOWMO ?? 0);
        const browser = await chromium.launch({
            channel: 'chrome',
            headless: !headed,
            slowMo: Number.isFinite(slowMo) && slowMo > 0 ? slowMo : undefined,
            // Occluded or unfocused headed windows are otherwise throttled, starving the clock replies and the Godot loop.
            args: ['--enable-features=SharedArrayBuffer', '--disable-backgrounding-occluded-windows', '--disable-renderer-backgrounding']
        });
        const context = await browser.newContext({ viewport });
        const page = await context.newPage();
        page.setDefaultTimeout(120000);
        page.setDefaultNavigationTimeout(120000);
        page.on('pageerror', (err: Error) => console.error('PAGE ERROR:', err));
        page.on('worker', (worker: Worker) => {
            worker.on('close', () => undefined);
        });

        const driver = new WorkshopDriver(browser, context, page, baseUrl);

        page.on('console', (msg: ConsoleMessage) => {
            const text = msg.text();
            if (text.includes('CCGOAL_SOLVED')) {
                driver.capturedCount++;
            }
        });

        await page.goto(baseUrl);
        await driver.waitForReady();
        return driver;
    }

    async close(): Promise<void> {
        await this.browser.close();
    }

    async reload(): Promise<void> {
        this.capturedCount = 0;
        this.currentLevel = 'free_workshop';
        await this.page.reload();
        await this.waitForReady();
    }

    async waitForReady(timeoutMs: number = 120000): Promise<void> {
        await this.page.waitForFunction(() => {
            const globals = globalThis;
            return globals.crossOriginIsolated === true &&
                !document.getElementById('status') &&
                globals.WorkshopPoseRing !== undefined &&
                globals.WorkshopPoseRing.hasPoseRing(1) &&
                globals.WorkshopAnimation !== undefined &&
                globals.WorkshopAnimation.isAnimationQualified(1);
        }, undefined, { timeout: timeoutMs });
        await this.page.waitForTimeout(400);
    }

    private async clickAt(x: number, y: number, waitAfterMs: number = 200): Promise<void> {
        await this.page.mouse.move(x, y);
        await this.page.mouse.down();
        await this.page.waitForTimeout(60);
        await this.page.mouse.up();
        await this.page.waitForTimeout(waitAfterMs);
    }

    // High-level UI operations (encapsulating all UI button locations)
    async selectTool(kind: 'basketball' | 'wall' | 'ramp' | 'bumper' | 'switch' | 'delay' | 'lamp' | 'receiver'): Promise<void> {
        let anchor: { x: number; y: number } = UI_ANCHORS.palette[kind];
        if (this.currentLevel === 'delayed_signal' && kind === 'delay') {
            anchor = { x: 130, y: 155 };
        }
        if (this.currentLevel === 'free_workshop' && kind === 'ramp') {
            anchor = { x: 130, y: 511 }; // Ramp is the appended eighth free-workshop palette row.
        }
        if (!anchor) throw new Error(`Unknown tool: ${kind}`);
        await this.clickAt(anchor.x, anchor.y, 200);
    }

    async selectLevel(level: 'first_principles' | 'free_workshop' | 'delayed_signal'): Promise<void> {
        await this.clickAt(UI_ANCHORS.picker.button.x, UI_ANCHORS.picker.button.y, 300);
        await this.clickAt(UI_ANCHORS.picker[level].x, UI_ANCHORS.picker[level].y, 300);
        this.currentLevel = level;
        // Choosing a level disposes the worker client and creates a new one; wait for the same readiness as a fresh page.
        await this.waitForReady();
    }

    // Toggle the authored puzzle's hint through its real button (reveal on the first press, hide on the next).
    async showHint(): Promise<void> {
        const anchor = this.currentLevel === 'first_principles' ? UI_ANCHORS.hint.first_principles
            : this.currentLevel === 'delayed_signal' ? UI_ANCHORS.hint.delayed_signal : null;
        if (!anchor) throw new Error(`Level ${this.currentLevel} has no hint button`);
        await this.clickAt(anchor.x, anchor.y, 300);
    }

    async selectPartAt(x: number, y: number): Promise<void> {
        await this.page.keyboard.press('Escape', { delay: 70 });
        await this.page.waitForTimeout(200);
        await this.clickAt(x, y, 300);
    }

    async setPartMode(mode: 'move' | 'rotate' | 'resize' | 'delete'): Promise<void> {
        const anchor = UI_ANCHORS.dock[this.currentLevel][mode];
        if (!anchor) throw new Error(`Unknown part mode ${mode} for level ${this.currentLevel}`);
        await this.clickAt(anchor.x, anchor.y, 300);
    }

    async tiltSelectedRamp(centerX: number, centerY: number, degrees: number = -20): Promise<void> {
        const radius = 106;
        const startRad = 3.8;
        const targetRad = startRad + (degrees * Math.PI / 180);

        const startX = Math.round(centerX + radius * Math.cos(startRad));
        const startY = Math.round(centerY - radius * Math.sin(startRad));
        const targetX = Math.round(centerX + radius * Math.cos(targetRad));
        const targetY = Math.round(centerY - radius * Math.sin(targetRad));

        await this.page.mouse.move(startX, startY);
        await this.page.mouse.down();
        await this.page.waitForTimeout(100);
        await this.page.mouse.move(targetX, targetY, { steps: 10 });
        await this.page.waitForTimeout(100);
        await this.page.mouse.up();
        await this.page.waitForTimeout(400);
    }

    async liftSelectedPart(deltaY: number = 100, centerX: number = 720, centerY: number = 485): Promise<void> {
        const handleX = centerX;
        const handleY = centerY - 58;
        await this.page.mouse.move(handleX, handleY);
        await this.page.mouse.down();
        await this.page.waitForTimeout(100);
        await this.page.mouse.move(handleX, handleY - deltaY, { steps: 10 });
        await this.page.waitForTimeout(100);
        await this.page.mouse.up();
        await this.page.waitForTimeout(400);
    }

    async resizeSelectedWall(deltaX: number = 70, centerX: number = 720, centerY: number = 485): Promise<void> {
        const fromX = centerX + 102;
        const fromY = centerY + 33;
        const deltaY = Math.round(deltaX * 0.3);
        await this.page.mouse.move(fromX, fromY);
        await this.page.mouse.down();
        await this.page.waitForTimeout(100);
        await this.page.mouse.move(fromX + deltaX, fromY + deltaY, { steps: 10 });
        await this.page.waitForTimeout(100);
        await this.page.mouse.up();
        await this.page.waitForTimeout(400);
    }

    async readCaptured(): Promise<number> {
        return this.capturedCount;
    }

    async openMenu(): Promise<void> {
        await this.clickAt(UI_ANCHORS.menu.button.x, UI_ANCHORS.menu.button.y, 300);
    }

    async closeMenu(): Promise<void> {
        await this.page.keyboard.press('Escape', { delay: 70 });
        await this.page.waitForTimeout(300);
    }

    async save(): Promise<void> {
        await this.openMenu();
        await this.clickAt(UI_ANCHORS.menu.save.x, UI_ANCHORS.menu.save.y, 600);
        await this.closeMenu();
    }

    async load(): Promise<void> {
        await this.openMenu();
        await this.clickAt(UI_ANCHORS.menu.load.x, UI_ANCHORS.menu.load.y, 600);
        await this.closeMenu();
    }

    async pauseSimulation(): Promise<void> {
        await this.openMenu();
        await this.clickAt(UI_ANCHORS.menu.pause.x, UI_ANCHORS.menu.pause.y, 300);
        await this.closeMenu();
    }

    async resumeSimulation(): Promise<void> {
        await this.openMenu();
        await this.clickAt(UI_ANCHORS.menu.resume.x, UI_ANCHORS.menu.resume.y, 300);
        await this.closeMenu();
    }

    async toggleRun(waitMs: number = 300): Promise<void> {
        await this.page.keyboard.press('Space', { delay: 70 });
        await this.page.waitForTimeout(waitMs);
    }

    // Wire an activation signal source → target purely through the Godot connection buttons.
    async connectActivation(source: { x: number; y: number; panel: 'locked' | 'delay' }, target: { x: number; y: number }): Promise<void> {
        await this.selectPartAt(source.x, source.y);
        const connect = UI_ANCHORS.connections.connect[source.panel];
        await this.clickAt(connect.x, connect.y, 300);
        await this.clickAt(target.x, target.y, 600);
        await this.clickAt(UI_ANCHORS.connections.choice.x, UI_ANCHORS.connections.choice.y, 800);
    }

    // Lift a freshly placed part and drop a different part kind directly beneath it (free workshop placement plane is y = 3 m).
    async stackUnderLiftedBall(kind: 'wall' | 'bumper' | 'receiver', liftPixels: number = 150, x: number = 720, y: number = 485): Promise<void> {
        await this.selectPartAt(x, y);
        await this.setPartMode('move');
        await this.liftSelectedPart(liftPixels, x, y);
        await this.selectTool(kind);
        await this.placeOnCanvas(x, y);
    }

    // Placing puzzle elements on the canvas (x/y coordinates are reserved exclusively for 3D canvas coordinates)
    async placeOnCanvas(x: number, y: number, waitAfterMs: number = 400): Promise<void> {
        await this.clickAt(x, y, waitAfterMs);
    }

    // Read the lock-free zero-copy pose ring from SharedArrayBuffer
    async readLatestPose(retries: number = 5, retryDelayMs: number = 10): Promise<WorkshopPoseSlot | null> {
        for (let attempt = 0; attempt <= retries; attempt++) {
            const result = await this.page.evaluate((): WorkshopPoseSlot | null => {
                const slot = globalThis.WorkshopPoseRing?.readLatestPoseSlot(1);
                if (!slot) return null;
                return {
                    sequence: slot.sequence.toString(),
                    timestamp: slot.timestamp.toString(),
                    bodyCount: slot.bodies.length,
                    bodies: slot.bodies.map((b: WorkshopBodyPose) => ({
                        id: b.id,
                        px: b.px,
                        py: b.py,
                        pz: b.pz,
                        qx: b.qx,
                        qy: b.qy,
                        qz: b.qz,
                        qw: b.qw,
                        vx: b.vx,
                        vy: b.vy,
                        vz: b.vz,
                        flags: b.flags,
                    }))
                };
            });
            if (result !== null) return result;
            if (attempt < retries) {
                await this.page.waitForTimeout(retryDelayMs);
            }
        }
        return null;
    }

    // Animation telemetry methods driven by the dedicated 60 Hz WebAssembly animation worker
    async isAnimationQualified(): Promise<boolean> {
        return await this.page.evaluate(() => {
            return globalThis.WorkshopAnimation?.isAnimationQualified(1) === true;
        });
    }

    async readAnimationSampleCount(): Promise<number> {
        return await this.page.evaluate(() => {
            return globalThis.WorkshopAnimation?.readAnimationSampleCount(1) ?? 0;
        });
    }

    async readLastAnimationSample(targetId: number | string = 2): Promise<AnimationSampleRecord | null> {
        return await this.page.evaluate((tid: string) => {
            return globalThis.WorkshopAnimation?.readLastAnimationSample(1, tid) ?? null;
        }, targetId.toString());
    }

    async readAllAnimationSamples(): Promise<AnimationSampleRecord[]> {
        return await this.page.evaluate(() => {
            return globalThis.WorkshopAnimation?.readAllAnimationSamples(1) ?? [];
        });
    }

    async readAnimationSamplesForTarget(targetId: number | string = 2): Promise<AnimationSampleRecord[]> {
        return await this.page.evaluate((tid: string) => {
            return globalThis.WorkshopAnimation?.readAnimationSamplesForTarget(1, tid) ?? [];
        }, targetId.toString());
    }
}
