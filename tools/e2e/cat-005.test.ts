// CAT-005 direct finite supply prerequisite; Motor/gated networks remain separate acceptance.
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createRequire } from 'node:module';
const requireMcp=createRequire('/opt/homebrew/lib/node_modules/@playwright/mcp/package.json');
const {PNG}=requireMcp('./node_modules/playwright-core/lib/utilsBundle.js');
import { WorkshopDriver } from './workshop-driver.ts';
import { decodeWorkshopResponse, encodeWorkshopResponse, decodeSavedConstruction, authoredState, WorkEffect, encodeWorkshopCommand, CommandKind } from './workshop-wire.ts';
const ElectricalEnable={Disabled:0,Enabled:1} as const;
type ElectricalEnable=typeof ElectricalEnable[keyof typeof ElectricalEnable];
const WorkshopCommand={ConfigureCadence:6} as const;
const ExpectedRevision={Exact:2} as const;
const CommandOutcome={Applied:0} as const;
const SimulationCadence={Hz120:2} as const;
const PhysicalStepProfile={Canonical480Hz:1} as const;
const AnimationCadence={Hz30:1} as const;
const PresentationCadence={Hz60:2} as const;
// Match the driver's closed numeric union boundaries under Node type-strip mode.
const ConnectionDomain = { Electrical: 2 } as const;
type ConnectionDomain = typeof ConnectionDomain[keyof typeof ConnectionDomain];
const Socket = { Supply: 7, PowerIn: 8 } as const;
type Socket = typeof Socket[keyof typeof Socket];
function assertLinks(bytes: Buffer, sourceSlot: number, targets: number[]) {
    const saved = decodeSavedConstruction(bytes);
    const construction = saved.construction()!;
    assert.equal(construction.connectionsLength(), targets.length);
    const sourceId = construction.instances(sourceSlot)!.id();
    for(let i=0;i<targets.length;i++) {
        const edge = construction.connections(i)!;
        const targetId = construction.instances(targets[i])!.id();
        assert.equal(edge.source(), sourceId);
        assert.equal(edge.target(), targetId);
        assert.equal(edge.domain(), ConnectionDomain.Electrical);
        assert.equal(edge.fromSocket(), Socket.Supply);
        assert.equal(edge.toSocket(), Socket.PowerIn);
    }
}
function ulp(value:number):number {
    const bytes=Buffer.alloc(4);bytes.writeFloatLE(value);
    bytes.writeUInt32LE(bytes.readUInt32LE()+1);return bytes.readFloatLE()-value;
}
function accounting(reads:Buffer[], stores:number) {
    const commits=new Map<string,Buffer>(), unique:Buffer[]=[];
    let debit=0, credit=0;
    for(const read of reads) {
        const res = decodeWorkshopResponse(read);
        const epoch = res.epoch(), tick = res.tick();
        const key = epoch + ':' + tick;
        const previous = commits.get(key);
        if(previous) {assert.deepEqual(read,previous,'Same epoch/tick accounting must be bit-identical');continue;}
        commits.set(key,read);unique.push(read);
        const source = res.electrical(0);
        const spent = source ? source.debit() : 0;
        const remaining = source ? source.remaining() : 0;
        let supplied=0,tolerance=4*ulp(remaining+spent);
        for(let slot=0;slot<stores;slot++) {
            const store = res.contactWorks(slot);
            const balance = store ? store.remainingEnergy() : 0;
            const grant = store ? store.suppliedEnergy() : 0;
            assert.ok(grant>=0 && balance>=0 && balance<=32);
            supplied+=grant;tolerance+=4*(ulp(balance)+ulp(grant));
        }
        // At most two 120 Hz phases per eight-substep commit; bound affected f32 rounding, not gameplay slack.
        assert.ok(Math.abs(spent-supplied)<=tolerance,
            'Finite commit debit='+spent+' credits='+supplied+' tolerance='+tolerance);
        if(spent===0)assert.equal(supplied,0,'Zero debit cannot fund credits');
        debit+=spent;credit+=supplied;
    }
    return {unique,debit,credit};
}
function assertTransfers(reads:Buffer[], stores:number, depleted=false) {
    const transfer=accounting(reads,stores);reads=transfer.unique;
    const seen=new Set<string>(), paid=Array.from({length:stores},()=>[] as {tick:bigint,debit:number}[]);
    const credits=Array(stores).fill(0); let finalTick:bigint|undefined;
    for(const read of reads) {
        const res = decodeWorkshopResponse(read);
        const tick = res.tick();
        const source = res.electrical(0);
        for(let slot=0;slot<stores;slot++) {
            const store = res.contactWorks(slot);
            if (store) credits[slot] += store.suppliedEnergy();
        }
        if(source && source.remaining() === 0 && source.debit() > 0) {
            const store = res.contactWorks(0);
            assert.ok(store && store.suppliedEnergy() > 0, 'Final positive debit must deliver a positive store credit');
            finalTick=tick;
        }
        const occCount = res.occurrencesLength();
        for(let i=0;i<occCount;i++) {
            const occ = res.occurrences(i)!;
            const slot = occ.work();
            const sequence = occ.sequence();
            const key = res.epoch() + ':' + slot + ':' + sequence;
            if(!sequence || seen.has(key))continue; seen.add(key);
            if(occ.effect() === WorkEffect.Paid) {
                const debit = occ.debit();
                assert.ok(debit>0);
                paid[slot].push({tick,debit});
            }
        }
    }
    for(let slot=0;slot<stores;slot++) {
        assert.ok(credits[slot]>0,'Every connected store must receive positive credit');
        assert.ok(paid[slot].length>=3,'Recharge must fund additional paid impacts, not passive contacts');
        assert.ok(paid[slot].reduce((sum,event)=>sum+event.debit,0)>32,'Paid work must exceed the initial preload');
    }
    if(depleted) {
        assert.notEqual(finalTick,undefined,'Observe final positive source-depletion grant');
        assert.ok(paid[0].some(event=>event.tick>=finalTick!),'Delivered final grant remains usable for a paid impact');
    }
    return {credits,paid,debit:transfer.debit,credit:transfer.credit};
}
async function saved(driver: WorkshopDriver): Promise<Buffer> {
    const value = await driver.page.evaluate(() => new Promise<number[]>((resolve, reject) => {
        const request = indexedDB.open('curious-contraptions-workshop');
        request.onupgradeneeded = () => request.transaction?.abort();
        request.onerror = () => reject(new Error('Existing save unavailable'));
        request.onsuccess = () => {
            const db = request.result;
            try {
                const tx = db.transaction('construction', 'readonly'), read = tx.objectStore('construction').get(1);
                tx.oncomplete = () => { db.close(); resolve(Array.from(read.result as Uint8Array)); };
                tx.onabort = () => { db.close(); reject(new Error('Read aborted')); };
            } catch (error) { db.close(); reject(error); }
        };
    }));
    return Buffer.from(value);
}
const authored = (bytes: Buffer) => authoredState(bytes);
// Frame-settled: wall-clock sleeps raced the Godot frame when the page's main thread was slow (Story 9.4b).
async function click(driver: WorkshopDriver, x: number, y: number) {
    await driver.clickAt(x,y);
}
// The settings panel and its dropdown have no DOM: wait for their rendered state at one pixel of the actual page.
async function waitForPixel(driver: WorkshopDriver, x: number, y: number, want: (r: number, g: number, b: number) => boolean, what: string) {
    for (const deadline=Date.now()+10000; Date.now()<deadline; await driver.page.waitForTimeout(50)) {
        const png=PNG.sync.read(await driver.page.screenshot({clip:{x,y,width:1,height:1}}));
        if (want(png.data[0],png.data[1],png.data[2])) return;
    }
    const shot='.anvil/battery-wait-failure.png';await driver.page.screenshot({path:shot});
    throw new Error(`The UI never showed ${what} at (${x}, ${y}); page captured in ${shot}`);
}
const isSettingsField=(r:number,g:number,b:number)=>Math.abs(r-114)<16 && Math.abs(g-113)<16 && Math.abs(b-109)<16;
const isDropdownPopup=(r:number,g:number,b:number)=>r<90 && g<90 && b<90;
async function connect(driver: WorkshopDriver, existing: boolean, targetX = 720) {
    await driver.selectPartAt(920,510);
    await click(driver,130,existing ? 744 : 789);
    await click(driver,targetX,485);
    await click(driver,130,736);
    await driver.page.keyboard.press('Escape'); await driver.page.waitForTimeout(200);
}
for (const depleted of [false,true]) test(depleted ? 'Battery finite60J source exhausts and retains final credit' : 'Battery recharge, disable, Save/Load and Reset', async () => {
    const driver = await WorkshopDriver.launch();
    const reads: Buffer[] = [], retained: Buffer[] = [], errors: string[] = [];
    driver.page.on('console', m => {
        const text=m.text();
        if(text.startsWith('CCGPU_RETAINED ')) retained.push(Buffer.from(text.slice(15),'base64'));
        if(text.startsWith('CCGPU ')) reads.push(Buffer.from(text.slice(6),'base64'));
        if(m.type()==='error'||/CCGPU_TRANSPORT_FAILURE|CCGPU_STARTUP_EXCEPTION|Unhandled exception/.test(text)) errors.push(text);
    });
    driver.page.on('pageerror',error=>errors.push(error.message));
    try {
        await driver.selectTool('bowling'); await driver.placeOnCanvas(720,485);
        await driver.stackUnderLiftedBall('bumper',150);
        await driver.page.keyboard.press('Escape'); await driver.page.waitForTimeout(200);
        await driver.selectTool('battery'); await driver.placeOnCanvas(920,510);
        if(depleted) {
            await driver.selectPartAt(920,510); await click(driver,130,744);
            await driver.page.screenshot({path:'.anvil/battery-settings-before.png'});
            await click(driver,100,586); await driver.page.keyboard.press('Meta+A',{delay:70}); await driver.page.keyboard.type('60',{delay:80}); await driver.page.keyboard.press('Enter',{delay:70}); await click(driver,100,627);
            await driver.page.screenshot({path:'.anvil/battery-settings-edited.png'});
            await click(driver,130,746);
            await driver.page.screenshot({path:'.anvil/battery-settings-after.png'});
            await driver.page.keyboard.press('Escape'); await driver.page.waitForTimeout(200);
        }
        await connect(driver,false);
        await driver.save();
        const initial = await saved(driver);
        const initialFb = decodeSavedConstruction(initial);
        const battery = initialFb.construction()!.instances(2)!;
        assert.equal(battery.capacity(),depleted?60:3600);
        assert.equal(battery.maximumPower(),120);
        assert.equal(battery.initialFraction(),1);
        assert.equal(battery.enabled(),1);
        assertLinks(initial,2,[1]);
        await driver.reload(); await driver.load(); await driver.save();
        assert.deepEqual(authored(await saved(driver)),authored(initial));
        await driver.run(); await driver.page.waitForTimeout(5000); await driver.pauseSimulation();
        const paid=reads.at(-1)!;
        assertTransfers(retained,1,depleted);
        const paidRes = decodeWorkshopResponse(paid);
        assert.equal(paidRes.electricalLength(), 1); assert.equal(paidRes.contactWorksLength(), 1);
        assert.ok(paidRes.contactWorks(0)!.occurrenceCount()>=3,'Recharge must permit repeated heavy-ball impacts');
        if(depleted) {
            assert.equal(paidRes.electrical(0)!.remaining(),0);
            assert.equal(paidRes.electrical(0)!.available(),false);
            assert.equal(paidRes.contactWorks(0)!.remainingEnergy(),0);
            const supply=await driver.readLastAnimationSample(String(4n<<32n));
            assert.equal(supply?.value,0);
            const depletedPicture=await indicatorPicture(driver,'depleted');assert.equal(depletedPicture.terminal,false);assert.deepEqual(depletedPicture.marks,[false,false,false,false]);
        } else {
            const remaining=paidRes.electrical(0)!.remaining();
            assert.ok(remaining<3504 && remaining>0);
            await driver.selectPartAt(920,510); await click(driver,130,823);
            const disableRes = decodeWorkshopResponse(reads.at(-1)!);
            assert.equal(disableRes.contactWorks(0)!.remainingEnergy(),paidRes.contactWorks(0)!.remainingEnergy(),'Disable must preserve stored work');
            await driver.resumeSimulation(); await driver.page.waitForTimeout(3500); await driver.pauseSimulation();
            const off=decodeWorkshopResponse(reads.at(-1)!);
            assert.equal(off.electrical(0)!.enabled(),0); assert.equal(off.electrical(0)!.remaining(),remaining);
            assert.equal(off.contactWorks(0)!.remainingEnergy(),0);
            assert.equal((await driver.readLastAnimationSample(String(4n<<32n)))?.value,0);
            const retainedBeforeEnable=retained.length;
            await driver.selectPartAt(920,510); await click(driver,130,823);
            await driver.resumeSimulation(); await driver.page.waitForTimeout(700); await driver.pauseSimulation();
            const on=decodeWorkshopResponse(reads.at(-1)!);
            assert.equal(on.electrical(0)!.enabled(),1);
            assert.ok(on.electrical(0)!.remaining()<remaining,'Re-enable resumes finite source payment');
            assert.ok(retained.slice(retainedBeforeEnable).some(read=>(decodeWorkshopResponse(read).contactWorks(0)?.suppliedEnergy() ?? 0)>0),'Re-enabled source actually credits its consumer');
            assert.equal((await driver.readLastAnimationSample(String(4n<<32n)))?.value,1);
            // The preceding prolonged unpaid control can leave the ball resting. Reset through UI for a fresh demand cycle.
            await driver.reset();await driver.run();await driver.page.waitForTimeout(1200);await driver.selectPartAt(920,510);
            const runningStart=retained.length;await click(driver,130,823);
            const demandStart=Date.now();while(Date.now()-demandStart<3000 && !retained.slice(runningStart).some(read=>{ const r=decodeWorkshopResponse(read); return r.electrical(0)?.enabled()===0 && (r.contactWorks(0)?.remainingEnergy() ?? 32)<32; }))await driver.page.waitForTimeout(40);
            const disabled=retained.slice(runningStart).filter(read=>decodeWorkshopResponse(read).electrical(0)?.enabled()===0);
            assert.ok(disabled.some(read=>(decodeWorkshopResponse(read).contactWorks(0)?.remainingEnergy() ?? 32)<32),'Running disabled source has actual store demand');
            const stopped=decodeWorkshopResponse(disabled[0]).electrical(0)!.remaining();
            assert.ok(disabled.every(read=>{ const r=decodeWorkshopResponse(read); return r.electrical(0)!.debit()===0 && r.contactWorks(0)!.suppliedEnergy()===0 && r.electrical(0)!.remaining()===stopped; }));
            const resumeStart=retained.length;await click(driver,130,823);
            const supplyStart=Date.now();while(Date.now()-supplyStart<3000 && !retained.slice(resumeStart).some(read=>{ const r=decodeWorkshopResponse(read); return r.electrical(0)?.enabled()===1 && r.electrical(0)!.debit()>0 && r.contactWorks(0)!.suppliedEnergy()>0; }))await driver.page.waitForTimeout(40);
            await driver.pauseSimulation();
            const resumed=retained.slice(resumeStart).filter(read=>decodeWorkshopResponse(read).electrical(0)?.enabled()===1);
            console.log(JSON.stringify({runningTransfer:retained.slice(runningStart).map(read=>{ const r=decodeWorkshopResponse(read); return {tick:r.tick().toString(),enabled:r.electrical(0)?.enabled(),source:r.electrical(0)?.remaining(),debit:r.electrical(0)?.debit(),store:r.contactWorks(0)?.remainingEnergy(),credit:r.contactWorks(0)?.suppliedEnergy()}; })}));
            assert.ok(resumed.some(read=>{ const r=decodeWorkshopResponse(read); return r.electrical(0)!.debit()>0 && r.contactWorks(0)!.suppliedEnergy()>0; }),'Running re-enable resumes funded transfer');
            assert.ok(decodeWorkshopResponse(reads.at(-1)!).tick()>decodeWorkshopResponse(disabled[0]).tick());
            accounting(retained,1);
        }
        await driver.reset();
        const reset=decodeWorkshopResponse(reads.at(-1)!);
        assert.equal(reset.electrical(0)!.remaining(),depleted?60:3600);
        assert.equal(reset.electrical(0)!.enabled(),1); assert.equal(reset.contactWorks(0)!.remainingEnergy(),32);
        assert.equal(reset.contactWorks(0)!.occurrenceCount(),0);
        const restoredPicture=await indicatorPicture(driver,depleted?'depleted-reset':'funded-reset');assert.equal(restoredPicture.terminal,true);assert.deepEqual(restoredPicture.marks,[true,true,true,true]);
        await driver.save(); assert.deepEqual(authored(await saved(driver)),authored(initial));
        assert.deepEqual(errors,[]);
        console.log(JSON.stringify({scenario:depleted?'depleted':'funded-disable',source:paidRes.electrical(0)!.remaining(),store:paidRes.contactWorks(0)!.remainingEnergy(),hits:paidRes.contactWorks(0)!.occurrenceCount()}));
    } catch (error) { console.error(JSON.stringify({errors})); await driver.page.screenshot({path:depleted?'.anvil/battery-depleted-failure.png':'.anvil/battery-funded-failure.png'}); throw error; }
    finally { await driver.close(); }
});

test('Battery fans one finite source out to two existing Bumper stores', async () => {
    const driver=await WorkshopDriver.launch(), reads:Buffer[]=[], retained:Buffer[]=[], errors:string[]=[];
    driver.page.on('console',m=>{const text=m.text();if(text.startsWith('CCGPU_RETAINED '))retained.push(Buffer.from(text.slice(15),'base64'));if(text.startsWith('CCGPU '))reads.push(Buffer.from(text.slice(6),'base64'));if(/CCGPU_TRANSPORT_FAILURE|CCGPU_STARTUP_EXCEPTION|Unhandled exception/.test(text))errors.push(text);});
    driver.page.on('pageerror',error=>errors.push(error.message));
    try {
        for(const x of [560,720]) {
            await driver.page.keyboard.press('Escape');await driver.page.waitForTimeout(200);
            await driver.selectTool('bowling');await driver.placeOnCanvas(x,485);
            await driver.stackUnderLiftedBall('bumper',150,x,485);
        }
        await driver.page.keyboard.press('Escape');await driver.page.waitForTimeout(200);
        await driver.selectTool('battery');await driver.placeOnCanvas(920,510);
        await connect(driver,false,560);await connect(driver,true,720);
        await driver.save();const initial=await saved(driver);assertLinks(initial,4,[1,3]);
        await driver.reload();await driver.load();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));assertLinks(await saved(driver),4,[1,3]);
        await driver.run();await driver.page.waitForTimeout(5000);await driver.pauseSimulation();
        const paid=decodeWorkshopResponse(reads.at(-1)!);
        assertTransfers(retained,2);
        assert.equal(paid.electricalLength(),1);assert.equal(paid.contactWorksLength(),2);
        assert.ok(paid.electrical(0)!.remaining()<3504);
        for(const slot of [0,1]) {
            assert.ok(paid.contactWorks(slot)!.occurrenceCount()>=3);
            const bal = paid.contactWorks(slot)!.remainingEnergy();
            assert.ok(bal>=0 && bal<=32);
        }
        await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
        assert.deepEqual(errors,[]);
        console.log(JSON.stringify({scenario:'fanout',source:paid.electrical(0)!.remaining(),hits:[paid.contactWorks(0)!.occurrenceCount(),paid.contactWorks(1)!.occurrenceCount()]}));
    } catch(error) {console.error(JSON.stringify({errors}));await driver.page.screenshot({path:'.anvil/battery-fanout-failure.png'});throw error;}
    finally {await driver.close();}
});

// Story 9.4b root cause of "3600 !== 120": the Enabled dropdown's keyboard Home/Enter did not always close its popup, so
// the Apply click landed on a popup item and the settings were never applied. Each step now waits for its rendered state,
// the item is chosen by clicking it, and Apply is a command acknowledged by the Simulation.
async function configure(driver:WorkshopDriver,capacity:number,power:number,fraction:number,enabled:boolean) {
    await driver.selectPartAt(920,510);await click(driver,130,744);
    await waitForPixel(driver,190,586,isSettingsField,'the open Battery settings panel'); // field background, right of its text
    for(const [y,value] of [[586,capacity],[627,power],[668,fraction]]) {
        await click(driver,100,y);await driver.page.keyboard.press('Meta+A');
        await driver.page.keyboard.type(String(value),{delay:60});await driver.page.keyboard.press('Enter');await driver.settle();
    }
    await click(driver,100,709);await waitForPixel(driver,200,770,isDropdownPopup,'the open Enabled dropdown');
    await click(driver,100,enabled?764:737);
    await waitForPixel(driver,200,770,(r,g,b)=>!isDropdownPopup(r,g,b),'the closed Enabled dropdown');
    await driver.commitAt(130,746);await driver.page.keyboard.press('Escape');
}
async function indicatorPicture(driver:WorkshopDriver,name:string) {
    const image=await driver.page.screenshot({path:'.anvil/battery-'+name+'.png'});
    const png=PNG.sync.read(image);
    // Fixed actual-UI battery placement; counts are local rendered gold pixels, not animation telemetry.
    let gold=0;
    for(let y=450;y<515;y++)for(let x=890;x<950;x++) {
        const i=(y*png.width+x)*4;
        if(png.data[i]>png.data[i+2]*1.5 && png.data[i+1]>png.data[i+2]*1.25 && png.data[i]>140)gold++;
    }
    const lit=(x:number,y:number)=>{
        const i=(y*png.width+x)*4;
        return png.data[i]>png.data[i+2]*1.5 && png.data[i+1]>png.data[i+2]*1.25;
    };
    return {gold,marks:[[888,499],[898,502],[907,505],[917,508]].map(([x,y])=>lit(x,y)),terminal:lit(918,476)};
}

test('Battery disconnected authored settings and rendered charge thresholds restore exactly',async()=>{
    const driver=await WorkshopDriver.launch(),reads:Buffer[]=[],errors:string[]=[];
    driver.page.on('console',m=>{const text=m.text();if(text.startsWith('CCGPU '))reads.push(Buffer.from(text.slice(6),'base64'));if(m.type()==='error')errors.push(text);});
    driver.page.on('pageerror',e=>errors.push(e.message));
    try {
        await driver.selectTool('bowling');await driver.placeOnCanvas(720,485);
        await driver.stackUnderLiftedBall('bumper',150);
        await driver.page.keyboard.press('Escape');await driver.selectTool('battery');await driver.placeOnCanvas(920,510);
        let previousGold=-1;
        for(const fraction of [0,.25,.5,.75,1]) {
            await configure(driver,120,30,fraction,false);
            await driver.save();const initial=await saved(driver);
            const initialFb=decodeSavedConstruction(initial);
            assert.equal(initialFb.construction()!.connectionsLength(),0,'Disconnected control has no Supply edge');
            const battery=initialFb.construction()!.instances(2)!;
            assert.equal(battery.capacity(),120);assert.equal(battery.maximumPower(),30);
            assert.equal(battery.initialFraction(),fraction);assert.equal(battery.enabled(),0);
            await driver.reload();await driver.load();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
            await driver.run();await driver.page.waitForTimeout(1200);await driver.pauseSimulation();
            const read=decodeWorkshopResponse(reads.at(-1)!);
            assert.equal(read.electrical(0)!.remaining(),120*fraction);assert.equal(read.electrical(0)!.debit(),0);
            assert.equal(read.contactWorks(0)!.suppliedEnergy(),0);assert.equal(read.electrical(0)!.enabled(),0);
            for(let mark=0;mark<5;mark++)assert.equal((await driver.readLastAnimationSample(String((4n<<32n)+BigInt(mark))))?.value,
                mark===0?0:(fraction>=mark*.25?1:0));
            const picture=await indicatorPicture(driver,'fraction-'+fraction);assert.ok(picture.gold>previousGold,'Each charge threshold lights one more rendered mark');previousGold=picture.gold;
            assert.deepEqual(picture.marks,[.25,.5,.75,1].map(threshold=>fraction>=threshold));assert.equal(picture.terminal,false);
            await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
            const reset1=decodeWorkshopResponse(reads.at(-1)!);
            assert.equal(reset1.electrical(0)!.remaining(),120*fraction);assert.equal(reset1.electrical(0)!.enabled(),0);
            const restored=await indicatorPicture(driver,'fraction-reset-'+fraction);assert.deepEqual(restored.marks,picture.marks);assert.equal(restored.terminal,false);
        }
        // Enabled yet disconnected: source is available, but no credit/debit occurs and preload is finite.
        await configure(driver,120,30,.5,true);await driver.save();const initial=await saved(driver);assert.equal(decodeSavedConstruction(initial).construction()!.instances(2)!.enabled(),1);
        await driver.run();await driver.page.waitForTimeout(3000);await driver.pauseSimulation();
        const read=decodeWorkshopResponse(reads.at(-1)!);assert.equal(read.electrical(0)!.remaining(),60);assert.equal(read.electrical(0)!.debit(),0);
        assert.equal(read.contactWorks(0)!.suppliedEnergy(),0);assert.equal(read.contactWorks(0)!.remainingEnergy(),0);
        assert.equal((await driver.readLastAnimationSample(String(4n<<32n)))?.value,1);
        const on=await indicatorPicture(driver,'terminal-on');
        await driver.selectPartAt(920,510);await click(driver,130,823);
        await driver.resumeSimulation();await driver.page.waitForTimeout(500);await driver.pauseSimulation();
        const off=await indicatorPicture(driver,'terminal-off');assert.equal(on.terminal,true);assert.equal(off.terminal,false);assert.deepEqual(on.marks,off.marks,'Disabling does not repaint charge');
        await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
        const reset2=decodeWorkshopResponse(reads.at(-1)!);
        assert.equal(reset2.electrical(0)!.enabled(),1);assert.equal(reset2.electrical(0)!.remaining(),60);
        const restored=await indicatorPicture(driver,'terminal-reset');assert.equal(restored.terminal,true);assert.deepEqual(restored.marks,on.marks);
        await connect(driver,false);await driver.save();const connected=await saved(driver);assertLinks(connected,2,[1]);
        const limited:Buffer[]=[];driver.page.on('console',m=>{const text=m.text();if(text.startsWith('CCGPU_RETAINED '))limited.push(Buffer.from(text.slice(15),'base64'));});
        await driver.run();await driver.page.waitForTimeout(1800);await driver.pauseSimulation();
        const budget=accounting(limited,1);assert.ok(budget.debit>0 && budget.credit>0);
        assert.ok(budget.unique.every(read=>decodeWorkshopResponse(read).electrical(0)!.debit()<=.5),'Authored30W limits each at-most-two-phase commit to0.5J');
        assert.ok(decodeWorkshopResponse(reads.at(-1)!).electrical(0)!.remaining()<60,'Half-full authored source funds actual demand');
        await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(connected));
        assert.equal(decodeWorkshopResponse(reads.at(-1)!).electrical(0)!.remaining(),60);
        assert.deepEqual(errors,[]);
    }finally{await driver.close();}
});

test('Battery paused intent reverses and Running controls preserve stores across continued ticks',async()=>{
    const driver=await WorkshopDriver.launch(),reads:Buffer[]=[],errors:string[]=[];
    driver.page.on('console',m=>{const text=m.text();if(text.startsWith('CCGPU_RETAINED '))reads.push(Buffer.from(text.slice(15),'base64'));if(text.startsWith('CCGPU '))reads.push(Buffer.from(text.slice(6),'base64'));if(m.type()==='error')errors.push(text);});
    driver.page.on('pageerror',e=>errors.push(e.message));
    try{
        await driver.selectTool('bumper');await driver.placeOnCanvas(720,485);
        await driver.page.keyboard.press('Escape');await driver.selectTool('battery');await driver.placeOnCanvas(920,510);
        await driver.save();const initial=await saved(driver);
        await driver.run();await driver.page.waitForTimeout(400);await driver.pauseSimulation();
        const paused=decodeWorkshopResponse(reads.at(-1)!);await driver.selectPartAt(920,510);await click(driver,130,823);
        await driver.page.screenshot({path:'.anvil/battery-paused-disable-queued.png'});
        await driver.page.keyboard.press('Escape');await driver.selectPartAt(920,510);await click(driver,130,823);
        await driver.page.screenshot({path:'.anvil/battery-paused-enable-queued.png'});
        assert.equal(decodeWorkshopResponse(reads.at(-1)!).contactWorks(0)!.remainingEnergy(),32);
        await driver.resumeSimulation();await driver.page.waitForTimeout(400);await driver.pauseSimulation();
        const afterResume=decodeWorkshopResponse(reads.at(-1)!);
        assert.equal(afterResume.electrical(0)!.enabled(),1,'Second paused click reverses queued disable');
        assert.ok(afterResume.tick()>paused.tick());
        await driver.selectPartAt(920,510);await click(driver,130,823);await driver.reset();
        await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
        await driver.run();await driver.page.waitForTimeout(400);
        assert.equal(decodeWorkshopResponse(reads.at(-1)!).electrical(0)!.enabled(),1,'Reset retires pending disable');
        await driver.selectPartAt(920,510);
        for(const enabled of [ElectricalEnable.Disabled,ElectricalEnable.Enabled]) {
            const before=decodeWorkshopResponse(reads.at(-1)!);await driver.page.screenshot({path:'.anvil/battery-running-before-'+enabled+'.png'});await click(driver,130,823);
            const start=Date.now();while(Date.now()-start<2500 && !reads.some(read=>{ const r=decodeWorkshopResponse(read); return r.epoch()===before.epoch() && r.tick()>before.tick() && r.electrical(0)?.enabled()===enabled; }))await driver.page.waitForTimeout(40);
            const after=decodeWorkshopResponse(reads.findLast(read=>{ const r=decodeWorkshopResponse(read); return r.epoch()===before.epoch() && r.tick()>before.tick() && r.electrical(0)?.enabled()===enabled; })!);assert.ok(after,'Observe committed state after Running toggle');await driver.page.screenshot({path:'.anvil/battery-running-after-'+enabled+'.png'});assert.equal(after.electrical(0)!.enabled(),enabled);
            assert.ok(after.tick()>before.tick(),'Running command commits while physics continues');
            assert.equal(after.contactWorks(0)!.remainingEnergy(),32,'Toggle preserves unspent stored work');
            assert.equal(after.electrical(0)!.remaining(),3600);
            await driver.page.waitForTimeout(200);
        }
        await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
        assert.deepEqual(errors,[]);
    }finally{await driver.close();}
});

test('Battery electrical animation survives same-world cadence downshift through existing protocol boundary',async()=>{
    const driver=await WorkshopDriver.launch(),reads:Buffer[]=[],errors:string[]=[];
    driver.page.on('console',m=>{const text=m.text();if(text.startsWith('CCGPU '))reads.push(Buffer.from(text.slice(6),'base64'));if(m.type()==='error')errors.push(text);});
    driver.page.on('pageerror',e=>errors.push(e.message));
    try{
        await driver.selectTool('battery');await driver.placeOnCanvas(920,510);
        await driver.page.waitForTimeout(900);
        const before=await driver.readLastAnimationSample(String(4n<<32n));assert.ok(before);
        const read=decodeWorkshopResponse(reads.at(-1)!);
        const bytes = encodeWorkshopCommand({
            sequence: read.sequence() + 1n,
            kind: CommandKind.ConfigureCadence,
            epoch: read.epoch(),
            revision: read.revision(),
            revisionKind: ExpectedRevision.Exact,
            session: read.sessionArray() ?? undefined,
            cadence: read.cadence(),
            projection: read.projection(),
            settings: {
                simulation: SimulationCadence.Hz120,
                physical: PhysicalStepProfile.Canonical480Hz,
                animation: AnimationCadence.Hz30,
                presentation: PresentationCadence.Hz60,
                presentationRateNumerator: 60,
                presentationRateDenominator: 1,
            }
        });
        // No cadence UI exists. This is explicitly a protocol-boundary Chrome regression, not UI acceptance.
        // Story 9A.2: the transport keys a command by the 8-byte sequence C# hands it, never by parsing the command.
        const sequenceKey=Buffer.alloc(8);sequenceKey.writeBigUInt64LE(read.sequence()+1n);
        const result=await driver.page.evaluate(async ([data,key])=>{
            const modulePath='/workshop-client.js'; // Browser-served module, not a Node package.
            const transport=await import(modulePath) as {
                send(id:number,bytes:Uint8Array,sequence:Uint8Array):Promise<void>;
                acknowledgement(id:number,identity:Uint8Array):Uint8Array;
                completeAcknowledgement(id:number,identity:Uint8Array):void;
            };
            const bytes=new Uint8Array(data),sequence=new Uint8Array(key);
            await transport.send(1,bytes,sequence);
            const reply=transport.acknowledgement(1,sequence);
            transport.completeAcknowledgement(1,sequence);
            return Array.from(reply);
        },[Array.from(bytes),Array.from(sequenceKey)]);
        const response=Buffer.from(result);
        const respFb=decodeWorkshopResponse(response);
        assert.equal(respFb.outcome(), CommandOutcome.Applied,'Existing ConfigureCadence command is applied');
        assert.equal(respFb.epoch(),read.epoch(),'Same physical world');
        assert.ok(respFb.cadence()>read.cadence());
        await driver.page.waitForTimeout(500);
        const after=await driver.readLastAnimationSample(String(4n<<32n));assert.ok(after);
        assert.ok(BigInt(after.ordinal)<BigInt(before.ordinal),'30 Hz sample ordinal is lower than prior60 Hz ordinal');
        assert.equal(after.value,1);
        const lastRead = decodeWorkshopResponse(reads.at(-1)!);
        assert.equal(lastRead.electrical(0)!.remaining(),3600);
        assert.deepEqual(errors,[]);
    }finally{await driver.close();}
});

test('Battery accounting deduplicates exact commits and rejects mutated or unfunded credits',()=>{
    const read=encodeWorkshopResponse({
        epoch: 1n,
        tick: 1n,
        electrical: [{ remaining: 59, debit: 1, enabled: 1 }],
        contactWorks: [
            { remainingEnergy: 16, suppliedEnergy: 0.5 },
            { remainingEnergy: 16, suppliedEnergy: 0.5 }
        ]
    });
    const unique=accounting([read,Buffer.from(read)],2);assert.equal(unique.unique.length,1);assert.equal(unique.credit,1);assert.equal(unique.debit,1);
    const mutated=encodeWorkshopResponse({
        epoch: 1n,
        tick: 1n,
        electrical: [{ remaining: 59, debit: 1, enabled: 1 }],
        contactWorks: [
            { remainingEnergy: 16, suppliedEnergy: 0.75 },
            { remainingEnergy: 16, suppliedEnergy: 0.5 }
        ]
    });
    assert.throws(()=>accounting([read,mutated],2),/bit-identical/);
    assert.throws(()=>accounting([mutated],2),/Finite commit/);
    const free=encodeWorkshopResponse({
        epoch: 1n,
        tick: 1n,
        electrical: [{ remaining: 59, debit: 0, enabled: 1 }],
        contactWorks: [
            { remainingEnergy: 16, suppliedEnergy: 0.5 },
            { remainingEnergy: 16, suppliedEnergy: 0.5 }
        ]
    });
    assert.throws(()=>accounting([free],2),/Finite commit|Zero debit/);
});
