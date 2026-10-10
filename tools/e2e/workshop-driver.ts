// Pure TypeScript Playwright driver for CuriousContraptions Workshop E2E testing.
// Encapsulates browser interaction, canvas placement, UI actions, and pose ring reading via Playwright.
// Waits are condition waits on signals the page already exposes (read-only observation, never control): rendered animation frames,
// the CCGPU command acknowledgement console line, pose-ring publications, the IndexedDB save slot and the CCGOAL_SOLVED console line.
import { createRequire } from 'node:module';
import type { Browser, BrowserContext, ConsoleMessage, Page, Worker } from 'playwright';

const requireMcp = createRequire('/opt/homebrew/lib/node_modules/@playwright/mcp/package.json');
const { chromium } = requireMcp('playwright') as typeof import('playwright');

export interface WorkshopBodyPose {
    /** Lossless decimal UInt64 at the browser observation boundary. */
    id: string;
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

// Closed sets of the CCGPU acknowledgement (engine/gpu/WorkshopSimulation.cs, engine/gpu/WorkshopWire.cs). Node runs these suites in
// type-strip mode, which rejects TypeScript `enum`, so each set is a frozen value table with its derived union type.
const CommandOutcome = { Applied: 0, Rejected: 1, Superseded: 2, Faulted: 3, Cancelled: 4 } as const;
type CommandOutcome = typeof CommandOutcome[keyof typeof CommandOutcome];
const SimulationPhase = {
    Uninitialized: 0, Initializing: 1, Building: 2, Admitting: 3, Starting: 4, Running: 5,
    Paused: 6, Completed: 7, Resetting: 8, Faulted: 9, Disposed: 10,
} as const;
type SimulationPhase = typeof SimulationPhase[keyof typeof SimulationPhase];
const CommandRejection = {
    None: 0, Busy: 1, InvalidConstruction: 2, WrongRevision: 3, WrongPhase: 4, GpuAdmission: 5, DeviceLost: 6, InvalidRead: 7,
    IdentityExhausted: 8, StaleGeneration: 9, Cancelled: 10, AlreadyCommitted: 11, Capacity: 12, ReliableStalled: 13, Transport: 14,
} as const;
type CommandRejection = typeof CommandRejection[keyof typeof CommandRejection];
const ResponseKindAcknowledgement = 0;

// The commands the Workshop UI issues (values of WorkshopCommandKind) and the phase each leaves the simulation in when Applied. The
// acknowledgement carries no command kind, so a command is matched by order (a newer command sequence and authority revision than the
// last consumed acknowledgement, exactly one arrival) and by the phase its kind implies.
enum WorkshopCommand { Construct = 1, Run = 2, Reset = 3, Pause = 7, Resume = 8, Step = 9, Save = 10 }
const PHASE_AFTER: Readonly<Record<WorkshopCommand, SimulationPhase>> = {
    [WorkshopCommand.Construct]: SimulationPhase.Building,
    [WorkshopCommand.Run]: SimulationPhase.Running,
    [WorkshopCommand.Reset]: SimulationPhase.Building,
    [WorkshopCommand.Pause]: SimulationPhase.Paused,
    [WorkshopCommand.Resume]: SimulationPhase.Running,
    [WorkshopCommand.Step]: SimulationPhase.Paused,
    [WorkshopCommand.Save]: SimulationPhase.Building,
};

interface WorkshopAcknowledgement {
    sequence: bigint;
    revision: bigint;
    outcome: CommandOutcome;
    reason: CommandRejection;
    phase: SimulationPhase;
}
// One CCGPU line: its decoded acknowledgement, or the reason it could not be decoded (scoped to that line only).
type AcknowledgementEntry = { ack: WorkshopAcknowledgement } | { error: string };

function nameOf(table: Readonly<Record<string, string | number>>, value: number): string {
    return Object.keys(table).find(key => table[key] === value) ?? `#${value}`;
}

function member<T extends number>(table: Readonly<Record<string, T>>, value: number, field: string): T {
    const found = Object.values(table).find(entry => entry === value);
    if (found === undefined) throw new Error(`unknown ${field} ${value}`);
    return found;
}

// MachineWorld.LogWorkshop prints "CCGPU <base64>" for every command the Workshop UI issues (Construct for placement, edits, wiring,
// level selection and Load; Run; Reset; Save; Pause; Resume). Byte layout: WorkshopWire.Write(WorkshopResponse) — 0 command sequence
// (u64), 8 kind, 9 outcome, 10 reason, 11 phase, 32 authority revision (u64). Unknown values reject at this boundary.
const ACK_PREFIX = 'CCGPU ';
function decodeAcknowledgement(text: string): WorkshopAcknowledgement {
    const bytes = Buffer.from(text.slice(ACK_PREFIX.length), 'base64');
    if (bytes.length < 40) throw new Error(`acknowledgement of ${bytes.length} bytes`);
    if (bytes[8] !== ResponseKindAcknowledgement) throw new Error(`response kind ${bytes[8]} is not an acknowledgement`);
    const sequence = bytes.readBigUInt64LE(0);
    if (sequence === 0n) throw new Error('acknowledgement without a command sequence');
    return {
        sequence,
        revision: bytes.readBigUInt64LE(32),
        outcome: member(CommandOutcome, bytes[9], 'outcome'),
        reason: member(CommandRejection, bytes[10], 'rejection'),
        phase: member(SimulationPhase, bytes[11], 'phase'),
    };
}

// Godot reads queued input at the start of each rAF iteration and runs C# await continuations on its process frame, so a few rendered
// frames after an input event (or after an acknowledgement) mean the UI has applied it.
const UI_SETTLE_FRAMES = 3;
const READY_SETTLE_FRAMES = 5;
// Godot drops a key press that is released too soon: probes on the 6.1d bundle showed a zero-length and a two-frame Space press both
// ignored while a machine ran, and a 70 ms hold (the previous driver's value) acknowledged. Nothing observable marks the key as seen
// before release, so the hold stays a fixed input duration.
const KEY_HOLD_MS = 70;
const SCROLL_SETTLE_FRAMES = 30;
// Godot's PopupMenu (the level picker) discards a click that arrives within a fixed engine time threshold after it opens, to avoid
// accidental selection. Nothing observable marks the end of that guard, so the level item is clicked only after this hold.
const PICKER_OPEN_GUARD_MS = 250;
// ui/RotationGizmo.cs keeps one handle angle per ring for the page's lifetime: the Z ring handle starts at 3.8 rad and stays where
// the last rotate drag released it (selection, placement and level changes do not reset it; a page reload does).
const GIZMO_Z_HANDLE_START_RAD = 3.8;
// Free-workshop camera (azimuth 0.6, elevation 0.48, orthographic 65.2 px/m, as cat-014): a world offset (x, y, z) from the
// placement origin (0, 3, 0) projects to the screen offset (53.8 x − 36.8 z, 17.0 x + 24.8 z − 57.9 y) from (720, 485). A Z ring
// lies in the z = 0 plane, so only the x and y terms are needed.
const FREE_VIEW_ORIGIN = { x: 720, y: 485 } as const;
const FREE_VIEW_PX_PER_M = { xx: 53.8, xy: 17.0, up: 57.9 } as const;
// Rotation-ring radius of a stock Wall (RotationGizmo: 0.55 × the artwork bounds diagonal), measured from the projected Z handle.
const WALL_GIZMO_RADIUS_M = 1.97;
const FRAME_TIMEOUT_MS = 10000;
const ACK_TIMEOUT_MS = 15000;
const PUBLICATION_TIMEOUT_MS = 10000;
const CONSTRUCTION_DATABASE = 'curious-contraptions-workshop';
const CONSTRUCTION_STORE = 'construction';
const CONSTRUCTION_SLOT = 1;
// WorkshopSaveCodec: magic 'CCWS' (u32 LE @0), declared length (u32 LE @8), ByteLength = 24 + WorkshopWire.ConstructionBytes
// (320 + 32 instances × 160 + 8 connections × 32) = 5720.
const SAVE_MAGIC = 0x53574343;
const SAVE_BYTES = 5720;

async function bounded<T>(work: Promise<T>, timeoutMs: number, message: string): Promise<T> {
    let timer: ReturnType<typeof setTimeout> | undefined;
    const expired = new Promise<never>((_, reject) => { timer = setTimeout(() => reject(new Error(message)), timeoutMs); });
    try {
        return await Promise.race([work, expired]);
    } finally {
        clearTimeout(timer);
    }
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
        domino: { x: 130, y: 562 },
        bowling: { x: 130, y: 612 },
        battery: { x: 130, y: 664 },
        ramp: { x: 130, y: 155 },
    },
    picker: {
        button: { x: 600, y: 47 },
        first_principles: { x: 600, y: 95 },
        free_workshop: { x: 600, y: 125 },
        delayed_signal: { x: 600, y: 155 },
        domino_effect: { x: 600, y: 172 },
        bumper_sidekick: { x: 600, y: 195 },
        bumper_depth: { x: 600, y: 220 },
        wall_and_bumper: { x: 600, y: 245 },
    },
    menu: {
        button: { x: 1394, y: 46 },
        save: { x: 1056, y: 178 },
        load: { x: 1098, y: 178 },
        pause: { x: 1070, y: 240 },
        resume: { x: 1130, y: 240 },
    },
    dock: {
        // Free workshop lists twelve unlimited rows; the Springboard row shifts the Build dock by51px.
        free_workshop: {
            move: { x: 60, y: 809 },
            rotate: { x: 104, y: 809 },
            resize: { x: 148, y: 809 },
            delete: { x: 192, y: 809 },
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
        },
        bumper_sidekick: {
            move: { x: 60, y: 285 }, rotate: { x: 104, y: 285 },
            resize: undefined, delete: { x: 148, y: 285 },
        },
        bumper_depth: {
            move: { x: 60, y: 285 }, rotate: { x: 104, y: 285 },
            resize: undefined, delete: { x: 148, y: 285 },
        },
        wall_and_bumper: {
            move: { x: 60, y: 292 }, rotate: { x: 104, y: 292 },
            resize: { x: 148, y: 292 }, delete: { x: 192, y: 292 },
        },
        domino_effect: {
            move: { x: 40, y: 275 },
            rotate: { x: 72, y: 275 },
            resize: { x: 104, y: 275 },
            delete: { x: 136, y: 275 },
        }
    },
    // Each "Connect ActivationOut" / "Disconnect signal" row above the dock pushes the part dock down by one row.
    signalRowHeight: 41,
    // Activation wiring buttons rendered by ui/WorkshopConnections.cs inside the parts panel (Godot canvas).
    connections: {
        // "Connect ActivationOut" row: a locked level part shows no configuration rows above it; a Delay shows its duration row first;
        // a Domino in the ten-row free workshop lists it under the palette.
        connect: {
            locked: { x: 133, y: 236 },
            delay: { x: 133, y: 277 },
            domino: { x: 133, y: 790 },
        },
        // "ActivationOut → ActivationIn" choice offered after clicking the target part: the row takes the connect row's place.
        choice: {
            authored: { x: 133, y: 232 },
            free_workshop: { x: 133, y: 746 },
        },
    },
    // "Show hint" lightbulb button inside the objective panel (ui/Workshop.cs ShowHint); the panel height follows the task text.
    hint: {
        first_principles: { x: 1186, y: 260 },
        delayed_signal: { x: 1186, y: 214 },
        domino_effect: { x: 1186, y: 260 },
    },
    // Fine rotate (ui/WorkshopGuidance.cs): the toggle sits at the foot of the Workshop menu; once the menu is scrolled to its end the
    // "Tilt − / +" row (±5° about Z per press) is at a fixed position.
    fineRotate: {
        toggle: { x: 1056, y: 698 },
        scrollAt: { x: 1200, y: 500 },
        tilt: { decrease: { x: 1115, y: 413 }, increase: { x: 1159, y: 413 } },
    }
} as const;

export type WorkshopLevel = 'free_workshop' | 'first_principles' | 'delayed_signal' | 'domino_effect' | 'bumper_sidekick' | 'bumper_depth' | 'wall_and_bumper';

export class WorkshopDriver {
    readonly browser: Browser;
    readonly context: BrowserContext;
    readonly page: Page;
    readonly baseUrl: string;
    currentLevel: WorkshopLevel = 'free_workshop';
    capturedCount: number = 0;
    private fineRotateOpen = false; // the Fine rotate toggle flips; a page reload or level change rebuilds the menu closed
    private zRingHandleAngle: number = GIZMO_Z_HANDLE_START_RAD;
    private acknowledgements: AcknowledgementEntry[] = []; // CCGPU lines of the current page, in arrival order
    private lastAcknowledged: WorkshopAcknowledgement | undefined; // the newest acknowledgement a command consumed on this page
    private phase: SimulationPhase = SimulationPhase.Building; // the phase the last consumed acknowledgement reported
    private readonly consoleWaiters = new Set<() => void>();

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
        page.on('console', (msg: ConsoleMessage) => driver.observeConsole(msg.text()));

        await page.goto(baseUrl);
        await driver.waitForReady();
        return driver;
    }

    private observeConsole(text: string): void {
        if (text.includes('CCGOAL_SOLVED')) this.capturedCount++;
        if (text.startsWith(ACK_PREFIX)) {
            try {
                this.acknowledgements.push({ ack: decodeAcknowledgement(text) });
            } catch (error) {
                this.acknowledgements.push({ error: error instanceof Error ? error.message : String(error) });
            }
        }
        for (const waiter of [...this.consoleWaiters]) waiter();
    }

    // Resolves true as soon as the condition holds after a console line, false if it still fails when the timeout expires.
    private waitForConsole(condition: () => boolean, timeoutMs: number): Promise<boolean> {
        if (condition()) return Promise.resolve(true);
        return new Promise<boolean>(resolve => {
            const check = (): void => {
                if (!condition()) return;
                clearTimeout(timer);
                this.consoleWaiters.delete(check);
                resolve(true);
            };
            const timer = setTimeout(() => {
                this.consoleWaiters.delete(check);
                resolve(condition());
            }, timeoutMs);
            this.consoleWaiters.add(check);
        });
    }

    async close(): Promise<void> {
        await this.browser.close();
    }

    async reload(): Promise<void> {
        this.capturedCount = 0;
        this.currentLevel = 'free_workshop';
        this.fineRotateOpen = false;
        this.zRingHandleAngle = GIZMO_Z_HANDLE_START_RAD;
        this.acknowledgements = [];
        this.lastAcknowledged = undefined;
        this.phase = SimulationPhase.Building;
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
        // The overlay is removed once the client is qualified; Godot then enables the Build UI on its next process frame.
        await this.frames(READY_SETTLE_FRAMES);
    }

    // Wait until the page has rendered `count` more animation frames.
    private async frames(count: number): Promise<void> {
        const rendered = this.page.evaluate(async (n: number) => {
            // Anonymous callbacks remain self-contained when serialized by Playwright,
            // including when the TypeScript runner preserves function names.
            for (let frame = 0; frame < n; frame++)
                await new Promise<number>(resolve => requestAnimationFrame(resolve));
        }, count);
        await bounded(rendered, FRAME_TIMEOUT_MS, `${count} animation frames did not render within ${FRAME_TIMEOUT_MS} ms`);
    }

    // Press and release the left button at a point, holding it across one rendered frame.
    private async press(x: number, y: number): Promise<void> {
        await this.page.mouse.move(x, y);
        await this.page.mouse.down();
        await this.frames(1);
        await this.page.mouse.up();
    }

    // A click that only changes UI state (no Workshop command): wait until Godot has applied it.
    async clickAt(x: number, y: number): Promise<void> {
        await this.press(x, y);
        await this.frames(UI_SETTLE_FRAMES);
    }

    private async pressKey(key: 'Escape' | 'Space'): Promise<void> {
        await this.page.keyboard.press(key, { delay: KEY_HOLD_MS });
    }

    // Drag with the left button through a gizmo handle; the release is the commit.
    private async drag(fromX: number, fromY: number, toX: number, toY: number): Promise<void> {
        await this.page.mouse.move(fromX, fromY);
        await this.page.mouse.down();
        await this.frames(2);
        await this.page.mouse.move(toX, toY, { steps: 10 });
        await this.frames(2);
        await this.page.mouse.up();
    }

    // Perform an input that must issue exactly one Workshop command, then wait for its CCGPU acknowledgement: newer than the last
    // consumed one, Applied, in the phase the command's kind implies, and the only acknowledgement the input produced. A missing
    // acknowledgement means the UI ignored the input; a rejection reports its reason.
    private async command(action: string, kind: WorkshopCommand, operate: () => Promise<void>, settle: boolean = true): Promise<void> {
        const mark = this.acknowledgements.length;
        await operate();
        const arrived = await this.waitForConsole(() => this.acknowledgements.length > mark, ACK_TIMEOUT_MS);
        if (!arrived) throw new Error(`${action}: no Workshop command acknowledgement (CCGPU) within ${ACK_TIMEOUT_MS} ms; the UI did not issue the command`);
        const entry = this.acknowledgements[mark];
        if ('error' in entry) throw new Error(`${action}: unreadable CCGPU acknowledgement (${entry.error})`);
        const ack = entry.ack;
        if (ack.outcome !== CommandOutcome.Applied)
            throw new Error(`${action}: command ${nameOf(CommandOutcome, ack.outcome)} (${nameOf(CommandRejection, ack.reason)}) in phase ${nameOf(SimulationPhase, ack.phase)}`);
        const last = this.lastAcknowledged;
        if (last !== undefined && (ack.sequence <= last.sequence || ack.revision <= last.revision))
            throw new Error(`${action}: acknowledgement (sequence ${ack.sequence}, revision ${ack.revision}) is not newer than the last consumed one (sequence ${last.sequence}, revision ${last.revision})`);
        const phase = PHASE_AFTER[kind];
        if (ack.phase !== phase)
            throw new Error(`${action}: ${nameOf(WorkshopCommand, kind)} acknowledged in phase ${nameOf(SimulationPhase, ack.phase)}, expected ${nameOf(SimulationPhase, phase)}`);
        this.lastAcknowledged = ack;
        this.phase = ack.phase;
        if (settle) await this.frames(UI_SETTLE_FRAMES);
        if (this.acknowledgements.length !== mark + 1)
            throw new Error(`${action}: ${this.acknowledgements.length - mark} acknowledgements arrived; the input should issue exactly one ${nameOf(WorkshopCommand, kind)}`);
    }

    // A construction edit (placement, gizmo commit, wiring, level selection, Load) is installed once its Construct is acknowledged.
    private async construct(action: string, operate: () => Promise<void>): Promise<void> {
        await this.command(action, WorkshopCommand.Construct, operate);
    }

    // High-level UI operations (encapsulating all UI button locations)
    async selectTool(kind: 'basketball' | 'bowling' | 'wall' | 'ramp' | 'bumper' | 'switch' | 'delay' | 'lamp' | 'receiver' | 'domino' | 'battery'): Promise<void> {
        // The Bowling ball row exists only in the free-workshop palette; no authored level offers it.
        if ((kind === 'bowling' || kind === 'battery') && this.currentLevel !== 'free_workshop') throw new Error(`Level ${this.currentLevel} does not offer this free-workshop part`);
        let anchor: { x: number; y: number } = UI_ANCHORS.palette[kind];
        if ((this.currentLevel === 'delayed_signal' && kind === 'delay') || (this.currentLevel === 'domino_effect' && kind === 'domino') || ((this.currentLevel === 'bumper_sidekick' || this.currentLevel === 'bumper_depth') && kind === 'bumper')) {
            anchor = { x: 130, y: 155 }; // the authored inventory's single palette row
        }
        if (this.currentLevel === 'wall_and_bumper' && (kind === 'wall' || kind === 'bumper')) {
            anchor = { x: 130, y: kind === 'wall' ? 155 : 202 };
        }
        if (this.currentLevel === 'free_workshop' && kind === 'ramp') {
            anchor = { x: 130, y: 511 }; // Ramp is the appended eighth free-workshop palette row.
        }
        if (!anchor) throw new Error(`Unknown tool: ${kind}`);
        await this.clickAt(anchor.x, anchor.y);
    }

    async selectLevel(level: WorkshopLevel): Promise<void> {
        // Re-choosing the shown level is not a level change, and the picker only submits a construction in Build mode.
        if (level === this.currentLevel) throw new Error(`selectLevel(${level}): already the current level (a reload lands on free_workshop)`);
        if (this.phase !== SimulationPhase.Building) throw new Error(`selectLevel(${level}): the picker only changes level in Build mode, not ${nameOf(SimulationPhase, this.phase)}`);
        await this.press(UI_ANCHORS.picker.button.x, UI_ANCHORS.picker.button.y);
        await this.frames(1); // the picker popup opens on this frame
        const opened = Date.now();
        await this.frames(UI_SETTLE_FRAMES);
        await this.holdUntil(opened + PICKER_OPEN_GUARD_MS);
        // Choosing a level submits the level's construction to the page's existing worker (the worker client is created once per page
        // and is never recreated), so the page readiness predicate is already true here: wait for that Construct's acknowledgement.
        await this.construct(`selectLevel(${level})`, () => this.press(UI_ANCHORS.picker[level].x, UI_ANCHORS.picker[level].y));
        this.currentLevel = level;
        this.fineRotateOpen = false;
    }

    // Toggle the authored puzzle's hint through its real button (reveal on the first press, hide on the next).
    async showHint(): Promise<void> {
        const anchor = this.currentLevel === 'first_principles' ? UI_ANCHORS.hint.first_principles
            : this.currentLevel === 'delayed_signal' ? UI_ANCHORS.hint.delayed_signal
            : this.currentLevel === 'domino_effect' ? UI_ANCHORS.hint.domino_effect : null;
        if (!anchor) throw new Error(`Level ${this.currentLevel} has no hint button`);
        await this.clickAt(anchor.x, anchor.y);
    }

    async selectPartAt(x: number, y: number): Promise<void> {
        await this.pressKey('Escape');
        await this.frames(UI_SETTLE_FRAMES);
        await this.clickAt(x, y);
    }

    // signalRows: connection rows the selected part shows above its dock (a Domino shows "Connect ActivationOut"; a wired part adds "Disconnect signal").
    async setPartMode(mode: 'move' | 'rotate' | 'resize' | 'delete', signalRows: number = 0): Promise<void> {
        const anchor = UI_ANCHORS.dock[this.currentLevel][mode];
        if (!anchor) throw new Error(`Unknown part mode ${mode} for level ${this.currentLevel}`);
        const y = anchor.y + signalRows * UI_ANCHORS.signalRowHeight;
        if (mode === 'delete') await this.construct('setPartMode(delete)', () => this.press(anchor.x, y));
        else await this.clickAt(anchor.x, y);
    }

    // Rotate the selected part about Z in 5° steps through the Workshop menu's Fine rotate row (positive steps press "Tilt +").
    // Closing the menu with Escape also deselects the part, so select it again afterwards. Idempotent: the toggle is pressed only while closed.
    async tiltSelectedByFineRotate(steps: number): Promise<void> {
        await this.openMenu();
        if (!this.fineRotateOpen) {
            await this.clickAt(UI_ANCHORS.fineRotate.toggle.x, UI_ANCHORS.fineRotate.toggle.y);
            this.fineRotateOpen = true;
        }
        await this.page.mouse.move(UI_ANCHORS.fineRotate.scrollAt.x, UI_ANCHORS.fineRotate.scrollAt.y);
        await this.page.mouse.wheel(0, 2000);
        await this.frames(SCROLL_SETTLE_FRAMES);
        const button = steps >= 0 ? UI_ANCHORS.fineRotate.tilt.increase : UI_ANCHORS.fineRotate.tilt.decrease;
        for (let i = 0; i < Math.abs(steps); i++) await this.construct('tiltSelectedByFineRotate', () => this.press(button.x, button.y));
        await this.page.mouse.wheel(0, -2000);
        await this.frames(SCROLL_SETTLE_FRAMES);
        await this.closeMenu();
    }

    // Drag the Z ring's handle (front view: a screen circle around the part) by `degrees`.
    async tiltSelectedRamp(centerX: number, centerY: number, degrees: number = -20): Promise<void> {
        const radius = 106;
        const startRad = this.zRingHandleAngle;
        const targetRad = startRad + (degrees * Math.PI / 180);

        const startX = Math.round(centerX + radius * Math.cos(startRad));
        const startY = Math.round(centerY - radius * Math.sin(startRad));
        const targetX = Math.round(centerX + radius * Math.cos(targetRad));
        const targetY = Math.round(centerY - radius * Math.sin(targetRad));

        try {
            await this.construct('tiltSelectedRamp', () => this.drag(startX, startY, targetX, targetY));
        } finally {
            this.zRingHandleAngle = targetRad; // a rotate drag leaves the handle at its release angle even when the commit fails
        }
    }

    // Rotate the selected free-workshop Wall at the placement origin (0, 3, 0) about Z by `degrees` with its Z ring handle (3D view:
    // the ring projects to an ellipse). A placed part inherits the gizmo mode of the previous selection (Move after a lift), so the
    // dock's Rotate mode is chosen first.
    async tiltSelectedWall(degrees: number): Promise<void> {
        if (this.currentLevel !== 'free_workshop') throw new Error(`tiltSelectedWall projects through the free-workshop camera, not ${this.currentLevel}`);
        await this.setPartMode('rotate');
        const ringPoint = (angle: number): { x: number; y: number } => {
            const x = WALL_GIZMO_RADIUS_M * Math.cos(angle), y = WALL_GIZMO_RADIUS_M * Math.sin(angle);
            return {
                x: Math.round(FREE_VIEW_ORIGIN.x + FREE_VIEW_PX_PER_M.xx * x),
                y: Math.round(FREE_VIEW_ORIGIN.y + FREE_VIEW_PX_PER_M.xy * x - FREE_VIEW_PX_PER_M.up * y),
            };
        };
        const targetRad = this.zRingHandleAngle + degrees * Math.PI / 180;
        const from = ringPoint(this.zRingHandleAngle), to = ringPoint(targetRad);
        try {
            await this.construct('tiltSelectedWall', () => this.drag(from.x, from.y, to.x, to.y));
        } finally {
            this.zRingHandleAngle = targetRad; // a rotate drag leaves the handle at its release angle even when the commit fails
        }
    }

    async liftSelectedPart(deltaY: number = 100, centerX: number = 720, centerY: number = 485): Promise<void> {
        const handleX = centerX;
        const handleY = centerY - 58;
        await this.construct('liftSelectedPart', () => this.drag(handleX, handleY, handleX, handleY - deltaY));
    }

    async resizeSelectedWall(deltaX: number = 70, centerX: number = 720, centerY: number = 485): Promise<void> {
        const fromX = centerX + 102;
        const fromY = centerY + 33;
        const deltaY = Math.round(deltaX * 0.3);
        await this.construct('resizeSelectedWall', () => this.drag(fromX, fromY, fromX + deltaX, fromY + deltaY));
    }

    async readCaptured(): Promise<number> {
        return this.capturedCount;
    }

    // Resolves as soon as CCGOAL_SOLVED prints after this call (then lets the UI and animation outputs of that solve land for a few
    // frames), or when the timeout expires; returns how many solves printed during the wait.
    async waitForGoalSolved(timeoutMs: number): Promise<number> {
        const mark = this.capturedCount;
        if (await this.waitForConsole(() => this.capturedCount > mark, timeoutMs)) await this.frames(UI_SETTLE_FRAMES);
        return this.capturedCount - mark;
    }

    async openMenu(): Promise<void> {
        await this.clickAt(UI_ANCHORS.menu.button.x, UI_ANCHORS.menu.button.y);
    }

    async closeMenu(): Promise<void> {
        await this.pressKey('Escape');
        await this.frames(UI_SETTLE_FRAMES);
    }

    async save(): Promise<void> {
        await this.openMenu();
        // Save stores the IndexedDB slot first and only then sends its Save command, so the acknowledgement follows the stored slot.
        await this.command('save', WorkshopCommand.Save, () => this.press(UI_ANCHORS.menu.save.x, UI_ANCHORS.menu.save.y));
        await this.expectSavedSlot();
        await this.closeMenu();
    }

    // Read-only check of the construction slot written by workshop-client.js saveConstruction; never creates or upgrades the database.
    private async expectSavedSlot(): Promise<void> {
        const saved = await this.page.evaluate(({ database, store, slot }: { database: string; store: string; slot: number }) =>
            new Promise<{ length: number; magic: number; declared: number }>((resolve, reject) => {
                const request = indexedDB.open(database);
                request.onupgradeneeded = () => request.transaction?.abort();
                request.onerror = () => reject(new Error(`Construction database ${database} is unavailable: ${request.error?.message ?? 'no detail'}`));
                request.onsuccess = () => {
                    const connection = request.result;
                    try {
                        const transaction = connection.transaction(store, 'readonly');
                        const read = transaction.objectStore(store).get(slot);
                        transaction.oncomplete = () => {
                            connection.close();
                            const bytes = read.result;
                            if (!(bytes instanceof Uint8Array) || bytes.length < 12) { resolve({ length: 0, magic: 0, declared: 0 }); return; }
                            const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
                            resolve({ length: bytes.length, magic: view.getUint32(0, true), declared: view.getUint32(8, true) });
                        };
                        transaction.onabort = () => {
                            connection.close();
                            reject(new Error(`Construction slot read aborted: ${transaction.error?.message ?? 'no detail'}`));
                        };
                    } catch (error) {
                        connection.close();
                        reject(error);
                    }
                };
            }), { database: CONSTRUCTION_DATABASE, store: CONSTRUCTION_STORE, slot: CONSTRUCTION_SLOT });
        if (saved.length !== SAVE_BYTES || saved.magic !== SAVE_MAGIC || saved.declared !== SAVE_BYTES)
            throw new Error(`save: the Save command was acknowledged but the IndexedDB slot is not a ${SAVE_BYTES}-byte construction save (length ${saved.length}, magic 0x${saved.magic.toString(16)}, declared ${saved.declared})`);
    }

    async load(): Promise<void> {
        await this.openMenu();
        await this.construct('load', () => this.press(UI_ANCHORS.menu.load.x, UI_ANCHORS.menu.load.y));
        await this.closeMenu();
    }

    async pauseSimulation(): Promise<void> {
        await this.openMenu();
        await this.command('pauseSimulation', WorkshopCommand.Pause, () => this.press(UI_ANCHORS.menu.pause.x, UI_ANCHORS.menu.pause.y));
        await this.closeMenu();
    }

    async stepSimulation(): Promise<void> {
        await this.openMenu();
        await this.command('stepSimulation', WorkshopCommand.Step, () => this.press(1212,246));
        await this.closeMenu();
    }

    async resumeSimulation(): Promise<void> {
        await this.openMenu();
        await this.command('resumeSimulation', WorkshopCommand.Resume, () => this.press(UI_ANCHORS.menu.resume.x, UI_ANCHORS.menu.resume.y));
        await this.closeMenu();
    }

    // Space from Build mode: wait for the Run acknowledgement and the first pose-ring publication of the new run (the ring is written
    // only while running, so until then it still holds the previous run). windowMs is a deliberate real-time physics window: the call
    // returns no earlier than windowMs after the key press.
    async run(windowMs: number = 0): Promise<void> {
        const before = await this.readLatestPose();
        if (!before) throw new Error('run: the pose ring is unreadable in Build mode');
        const pressed = Date.now();
        await this.command('run (Space)', WorkshopCommand.Run, () => this.pressKey('Space'), false);
        await this.waitForPublicationAfter(before);
        await this.holdUntil(pressed + windowMs);
    }

    // Space while running: wait until the Reset is acknowledged and the Build UI is back. holdMs is a deliberate real-time window
    // measured from the key press (for example a drain for cosmetic pulses already in flight from the retired world).
    async reset(holdMs: number = 0): Promise<void> {
        await this.frames(UI_SETTLE_FRAMES); // the preceding Run's UI continuation has released the command gate
        const pressed = Date.now();
        await this.command('reset (Space)', WorkshopCommand.Reset, () => this.pressKey('Space'));
        await this.holdUntil(pressed + holdMs);
    }

    private async holdUntil(deadline: number): Promise<void> {
        const remaining = deadline - Date.now();
        if (remaining > 0) await this.page.waitForTimeout(remaining);
    }

    // The new run owns the latest slot once that slot was rewritten after the key press: its sequence exceeds every slot sequence read
    // before the press (the writer has one monotonic publication sequence across all slots) and its capture time is later than
    // the pre-Run slot's. The master clock is monotonic and Build mode publishes nothing.
    private async waitForPublicationAfter(before: WorkshopPoseSlot): Promise<void> {
        try {
            await this.page.waitForFunction(({ sequence, timestamp }: { sequence: string; timestamp: string }) => {
                const slot = globalThis.WorkshopPoseRing?.readLatestPoseSlot(1);
                return slot !== null && slot !== undefined && slot.sequence > BigInt(sequence) && slot.timestamp > BigInt(timestamp);
            }, { sequence: before.sequence, timestamp: before.timestamp }, { timeout: PUBLICATION_TIMEOUT_MS, polling: 5 });
        } catch (error) {
            throw new Error(`run: no pose-ring publication newer than sequence ${before.sequence} / capture ${before.timestamp} within ${PUBLICATION_TIMEOUT_MS} ms of the Run acknowledgement`, { cause: error });
        }
    }

    // Wire an activation signal source → target purely through the Godot connection buttons.
    async connectActivation(source: { x: number; y: number; panel: 'locked' | 'delay' | 'domino' }, target: { x: number; y: number }): Promise<void> {
        await this.selectPartAt(source.x, source.y);
        const connect = UI_ANCHORS.connections.connect[source.panel];
        await this.clickAt(connect.x, connect.y);
        await this.clickAt(target.x, target.y);
        const choice = this.currentLevel === 'free_workshop' ? UI_ANCHORS.connections.choice.free_workshop : UI_ANCHORS.connections.choice.authored;
        await this.construct('connectActivation', () => this.press(choice.x, choice.y));
    }

    // Lift a freshly placed part and drop a different part kind directly beneath it (free workshop placement plane is y = 3 m).
    async stackUnderLiftedBall(kind: 'wall' | 'bumper' | 'receiver', liftPixels: number = 150, x: number = 720, y: number = 485): Promise<void> {
        await this.selectPartAt(x, y);
        await this.setPartMode('move');
        await this.liftSelectedPart(liftPixels, x, y);
        await this.selectTool(kind);
        await this.placeOnCanvas(x, y);
    }

    // Placing puzzle elements on the canvas (x/y coordinates are reserved exclusively for 3D canvas coordinates); the placed part is
    // then selected.
    async placeOnCanvas(x: number, y: number): Promise<void> {
        await this.construct(`placeOnCanvas(${x}, ${y})`, () => this.press(x, y));
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
                // Lock-free reader backoff: a slot being rewritten is retried after the writer finishes.
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
