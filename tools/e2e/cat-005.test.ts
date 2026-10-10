// CAT-005 direct finite supply prerequisite; Motor/gated networks remain separate acceptance.
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createRequire } from 'node:module';
const requireMcp=createRequire('/opt/homebrew/lib/node_modules/@playwright/mcp/package.json');
const {PNG}=requireMcp('./node_modules/playwright-core/lib/utilsBundle.js');
import { WorkshopDriver } from './workshop-driver.ts';
const URL = process.env.BATTERY_PREVIEW_URL ?? 'http://127.0.0.1:8067/';
const SOURCE = 24080, WORK = 3328;
const ElectricalEnable={Disabled:0,Enabled:1} as const;
type ElectricalEnable=typeof ElectricalEnable[keyof typeof ElectricalEnable];
const WorkshopCommand={ConfigureCadence:6} as const;
const ExpectedRevision={Exact:2} as const;
const CommandOutcome={Applied:0} as const;
const SettingsVersion={SharedMaster:1} as const;
const SimulationCadence={Hz120:2} as const;
const PhysicalStepProfile={Canonical480Hz:1} as const;
const AnimationCadence={Hz30:1} as const;
const PresentationCadence={Hz60:2} as const;
// Match the driver's closed numeric union boundaries under Node type-strip mode.
const ConnectionDomain = { Electrical: 2 } as const;
type ConnectionDomain = typeof ConnectionDomain[keyof typeof ConnectionDomain];
const Socket = { Supply: 7, PowerIn: 8 } as const;
type Socket = typeof Socket[keyof typeof Socket];
const WorkEffect = { Paid: 1 } as const;
type WorkEffect = typeof WorkEffect[keyof typeof WorkEffect];
function assertLinks(bytes: Buffer, sourceSlot: number, targets: number[]) {
    assert.equal(bytes.readUInt32LE(36),targets.length);
    const id=(slot:number)=>bytes.readBigUInt64LE(24+320+slot*160+40);
    for(let i=0;i<targets.length;i++) {
        const edge=24+320+32*160+i*32;
        assert.equal(bytes.readBigUInt64LE(edge),id(sourceSlot));
        assert.equal(bytes.readBigUInt64LE(edge+8),id(targets[i]));
        assert.equal(bytes.readUInt32LE(edge+16),ConnectionDomain.Electrical);
        assert.equal(bytes.readUInt32LE(edge+20),Socket.Supply);
        assert.equal(bytes.readUInt32LE(edge+24),Socket.PowerIn);
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
        const key=read.readBigUInt64LE(16)+':'+read.readBigUInt64LE(24);
        const values=Buffer.concat([read.subarray(WORK,WORK+stores*32),read.subarray(SOURCE,SOURCE+32)]);
        const previous=commits.get(key);
        if(previous) {assert.deepEqual(values,previous,'Same epoch/tick accounting must be bit-identical');continue;}
        commits.set(key,values);unique.push(read);
        const spent=read.readFloatLE(SOURCE+20),remaining=read.readFloatLE(SOURCE+16);
        let supplied=0,tolerance=4*ulp(remaining+spent);
        for(let slot=0;slot<stores;slot++) {
            const balance=read.readFloatLE(WORK+slot*32+20),grant=read.readFloatLE(WORK+slot*32+24);
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
        const tick=read.readBigUInt64LE(24);
        for(let slot=0;slot<stores;slot++) credits[slot]+=read.readFloatLE(WORK+slot*32+24);
        if(read.readFloatLE(SOURCE+16)===0 && read.readFloatLE(SOURCE+20)>0) {
            assert.ok(read.readFloatLE(WORK+24)>0,'Final positive debit must deliver a positive store credit');
            finalTick=tick;
        }
        for(let i=0;i<read[109];i++) {
            const offset=3584+i*32, slot=read.readUInt16LE(offset), sequence=read.readUInt32LE(offset+12);
            const key=read.readBigUInt64LE(16)+':'+slot+':'+sequence;
            if(!sequence || seen.has(key))continue; seen.add(key);
            if(read[offset+30]===WorkEffect.Paid) {
                const debit=read.readFloatLE(offset+26); assert.ok(debit>0);
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
const authored = (bytes: Buffer) => Buffer.concat([bytes.subarray(0,24), bytes.subarray(32)]);
async function click(driver: WorkshopDriver, x: number, y: number) {
    await driver.page.mouse.move(x,y); await driver.page.mouse.down(); await driver.page.waitForTimeout(70); await driver.page.mouse.up(); await driver.page.waitForTimeout(180);
}
async function connect(driver: WorkshopDriver, existing: boolean, targetX = 720) {
    await driver.selectPartAt(920,510);
    await click(driver,130,existing ? 744 : 789);
    await click(driver,targetX,485);
    await click(driver,130,736);
    await driver.page.keyboard.press('Escape'); await driver.page.waitForTimeout(200);
}
for (const depleted of [false,true]) test(depleted ? 'Battery finite60J source exhausts and retains final credit' : 'Battery recharge, disable, Save/Load and Reset', async () => {
    const driver = await WorkshopDriver.launch({baseUrl:URL});
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
        const initial = await saved(driver), battery = 24+320+2*160;
        assert.equal(initial.readFloatLE(battery+104),depleted?60:3600);
        assert.equal(initial.readFloatLE(battery+108),120);
        assert.equal(initial.readFloatLE(battery+112),1);
        assert.equal(initial[battery+116],1);
        assertLinks(initial,2,[1]);
        await driver.reload(); await driver.load(); await driver.save();
        assert.deepEqual(authored(await saved(driver)),authored(initial));
        await driver.run(); await driver.page.waitForTimeout(5000); await driver.pauseSimulation();
        const paid=reads.at(-1)!;
        assertTransfers(retained,1,depleted);
        assert.equal(paid[112],1); assert.equal(paid[108],1);
        assert.ok(paid.readUInt32LE(WORK+16)>=3,'Recharge must permit repeated heavy-ball impacts');
        if(depleted) {
            assert.equal(paid.readFloatLE(SOURCE+16),0);
            assert.equal(paid[SOURCE+25],0);
            assert.equal(paid.readFloatLE(WORK+20),0);
            const supply=await driver.readLastAnimationSample(String(4n<<32n));
            assert.equal(supply?.value,0);
            const depletedPicture=await indicatorPicture(driver,'depleted');assert.equal(depletedPicture.terminal,false);assert.deepEqual(depletedPicture.marks,[false,false,false,false]);
        } else {
            const remaining=paid.readFloatLE(SOURCE+16);
            assert.ok(remaining<3504 && remaining>0);
            await driver.selectPartAt(920,510); await click(driver,130,750);
            assert.equal(reads.at(-1)!.readFloatLE(WORK+20),paid.readFloatLE(WORK+20),'Disable must preserve stored work');
            await driver.resumeSimulation(); await driver.page.waitForTimeout(3500); await driver.pauseSimulation();
            const off=reads.at(-1)!;
            assert.equal(off[SOURCE+24],0); assert.equal(off.readFloatLE(SOURCE+16),remaining);
            assert.equal(off.readFloatLE(WORK+20),0);
            assert.equal((await driver.readLastAnimationSample(String(4n<<32n)))?.value,0);
            const retainedBeforeEnable=retained.length;
            await driver.selectPartAt(920,510); await click(driver,130,750);
            await driver.resumeSimulation(); await driver.page.waitForTimeout(700); await driver.pauseSimulation();
            const on=reads.at(-1)!;
            assert.equal(on[SOURCE+24],1);
            assert.ok(on.readFloatLE(SOURCE+16)<remaining,'Re-enable resumes finite source payment');
            assert.ok(retained.slice(retainedBeforeEnable).some(read=>read.readFloatLE(WORK+24)>0),'Re-enabled source actually credits its consumer');
            assert.equal((await driver.readLastAnimationSample(String(4n<<32n)))?.value,1);
            // The preceding prolonged unpaid control can leave the ball resting. Reset through UI for a fresh demand cycle.
            await driver.reset();await driver.run();await driver.page.waitForTimeout(1200);await driver.selectPartAt(920,510);
            const runningStart=retained.length;await click(driver,130,750);
            const demandStart=Date.now();while(Date.now()-demandStart<3000 && !retained.slice(runningStart).some(read=>read[SOURCE+24]===0 && read.readFloatLE(WORK+20)<32))await driver.page.waitForTimeout(40);
            const disabled=retained.slice(runningStart).filter(read=>read[SOURCE+24]===0);
            assert.ok(disabled.some(read=>read.readFloatLE(WORK+20)<32),'Running disabled source has actual store demand');
            const stopped=disabled[0].readFloatLE(SOURCE+16);
            assert.ok(disabled.every(read=>read.readFloatLE(SOURCE+20)===0 && read.readFloatLE(WORK+24)===0 && read.readFloatLE(SOURCE+16)===stopped));
            const resumeStart=retained.length;await click(driver,130,750);
            const supplyStart=Date.now();while(Date.now()-supplyStart<3000 && !retained.slice(resumeStart).some(read=>read[SOURCE+24]===1 && read.readFloatLE(SOURCE+20)>0 && read.readFloatLE(WORK+24)>0))await driver.page.waitForTimeout(40);
            await driver.pauseSimulation();
            const resumed=retained.slice(resumeStart).filter(read=>read[SOURCE+24]===1);
            console.log(JSON.stringify({runningTransfer:retained.slice(runningStart).map(read=>({tick:read.readBigUInt64LE(24).toString(),enabled:read[SOURCE+24],source:read.readFloatLE(SOURCE+16),debit:read.readFloatLE(SOURCE+20),store:read.readFloatLE(WORK+20),credit:read.readFloatLE(WORK+24)}))}));
            assert.ok(resumed.some(read=>read.readFloatLE(SOURCE+20)>0 && read.readFloatLE(WORK+24)>0),'Running re-enable resumes funded transfer');
            assert.ok(reads.at(-1)!.readBigUInt64LE(24)>disabled[0].readBigUInt64LE(24));
            accounting(retained,1);
        }
        await driver.reset();
        const reset=reads.at(-1)!;
        assert.equal(reset.readFloatLE(SOURCE+16),depleted?60:3600);
        assert.equal(reset[SOURCE+24],1); assert.equal(reset.readFloatLE(WORK+20),32);
        assert.equal(reset.readUInt32LE(WORK+16),0);
        const restoredPicture=await indicatorPicture(driver,depleted?'depleted-reset':'funded-reset');assert.equal(restoredPicture.terminal,true);assert.deepEqual(restoredPicture.marks,[true,true,true,true]);
        await driver.save(); assert.deepEqual(authored(await saved(driver)),authored(initial));
        assert.deepEqual(errors,[]);
        console.log(JSON.stringify({scenario:depleted?'depleted':'funded-disable',source:paid.readFloatLE(SOURCE+16),store:paid.readFloatLE(WORK+20),hits:paid.readUInt32LE(WORK+16)}));
    } catch (error) { console.error(JSON.stringify({errors})); await driver.page.screenshot({path:depleted?'.anvil/battery-depleted-failure.png':'.anvil/battery-funded-failure.png'}); throw error; }
    finally { await driver.close(); }
});

test('Battery fans one finite source out to two existing Bumper stores', async () => {
    const driver=await WorkshopDriver.launch({baseUrl:URL}), reads:Buffer[]=[], retained:Buffer[]=[], errors:string[]=[];
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
        const paid=reads.at(-1)!;
        assertTransfers(retained,2);
        assert.equal(paid[112],1);assert.equal(paid[108],2);
        assert.ok(paid.readFloatLE(SOURCE+16)<3504);
        for(const slot of [0,1]) {
            assert.ok(paid.readUInt32LE(WORK+slot*32+16)>=3);
            assert.ok(paid.readFloatLE(WORK+slot*32+20)>=0 && paid.readFloatLE(WORK+slot*32+20)<=32);
        }
        await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
        assert.deepEqual(errors,[]);
        console.log(JSON.stringify({scenario:'fanout',source:paid.readFloatLE(SOURCE+16),hits:[paid.readUInt32LE(WORK+16),paid.readUInt32LE(WORK+48)]}));
    } catch(error) {console.error(JSON.stringify({errors}));await driver.page.screenshot({path:'.anvil/battery-fanout-failure.png'});throw error;}
    finally {await driver.close();}
});

async function configure(driver:WorkshopDriver,capacity:number,power:number,fraction:number,enabled:boolean) {
    await driver.selectPartAt(920,510);await click(driver,130,744);
    for(const [y,value] of [[586,capacity],[627,power],[668,fraction]]) {
        await click(driver,100,y);await driver.page.keyboard.press('Meta+A');
        await driver.page.keyboard.type(String(value),{delay:60});await driver.page.keyboard.press('Enter');
    }
    await click(driver,100,709);await driver.page.keyboard.press('Home');if(enabled)await driver.page.keyboard.press('ArrowDown');await driver.page.keyboard.press('Enter');
    await click(driver,130,746);await driver.page.keyboard.press('Escape');
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
    const driver=await WorkshopDriver.launch({baseUrl:URL}),reads:Buffer[]=[],errors:string[]=[];
    driver.page.on('console',m=>{const text=m.text();if(text.startsWith('CCGPU '))reads.push(Buffer.from(text.slice(6),'base64'));if(m.type()==='error')errors.push(text);});
    driver.page.on('pageerror',e=>errors.push(e.message));
    try {
        await driver.selectTool('bowling');await driver.placeOnCanvas(720,485);
        await driver.stackUnderLiftedBall('bumper',150);
        await driver.page.keyboard.press('Escape');await driver.selectTool('battery');await driver.placeOnCanvas(920,510);
        let previousGold=-1;
        for(const fraction of [0,.25,.5,.75,1]) {
            await configure(driver,120,30,fraction,false);
            await driver.save();const initial=await saved(driver),slot=24+320+2*160;
            assert.equal(initial.readUInt32LE(36),0,'Disconnected control has no Supply edge');
            assert.equal(initial.readFloatLE(slot+104),120);assert.equal(initial.readFloatLE(slot+108),30);
            assert.equal(initial.readFloatLE(slot+112),fraction);assert.equal(initial[slot+116],0);
            await driver.reload();await driver.load();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
            await driver.run();await driver.page.waitForTimeout(1200);await driver.pauseSimulation();
            const read=reads.at(-1)!;
            assert.equal(read.readFloatLE(SOURCE+16),120*fraction);assert.equal(read.readFloatLE(SOURCE+20),0);
            assert.equal(read.readFloatLE(WORK+24),0);assert.equal(read[SOURCE+24],0);
            for(let mark=0;mark<5;mark++)assert.equal((await driver.readLastAnimationSample(String((4n<<32n)+BigInt(mark))))?.value,
                mark===0?0:(fraction>=mark*.25?1:0));
            const picture=await indicatorPicture(driver,'fraction-'+fraction);assert.ok(picture.gold>previousGold,'Each charge threshold lights one more rendered mark');previousGold=picture.gold;
            assert.deepEqual(picture.marks,[.25,.5,.75,1].map(threshold=>fraction>=threshold));assert.equal(picture.terminal,false);
            await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
            assert.equal(reads.at(-1)!.readFloatLE(SOURCE+16),120*fraction);assert.equal(reads.at(-1)![SOURCE+24],0);
            const restored=await indicatorPicture(driver,'fraction-reset-'+fraction);assert.deepEqual(restored.marks,picture.marks);assert.equal(restored.terminal,false);
        }
        // Enabled yet disconnected: source is available, but no credit/debit occurs and preload is finite.
        await configure(driver,120,30,.5,true);await driver.save();const initial=await saved(driver);assert.equal(initial[24+320+2*160+116],1);
        await driver.run();await driver.page.waitForTimeout(3000);await driver.pauseSimulation();
        const read=reads.at(-1)!;assert.equal(read.readFloatLE(SOURCE+16),60);assert.equal(read.readFloatLE(SOURCE+20),0);
        assert.equal(read.readFloatLE(WORK+24),0);assert.equal(read.readFloatLE(WORK+20),0);
        assert.equal((await driver.readLastAnimationSample(String(4n<<32n)))?.value,1);
        const on=await indicatorPicture(driver,'terminal-on');
        await driver.selectPartAt(920,510);await click(driver,130,750);
        await driver.resumeSimulation();await driver.page.waitForTimeout(500);await driver.pauseSimulation();
        const off=await indicatorPicture(driver,'terminal-off');assert.equal(on.terminal,true);assert.equal(off.terminal,false);assert.deepEqual(on.marks,off.marks,'Disabling does not repaint charge');
        await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
        assert.equal(reads.at(-1)![SOURCE+24],1);assert.equal(reads.at(-1)!.readFloatLE(SOURCE+16),60);
        const restored=await indicatorPicture(driver,'terminal-reset');assert.equal(restored.terminal,true);assert.deepEqual(restored.marks,on.marks);
        await connect(driver,false);await driver.save();const connected=await saved(driver);assertLinks(connected,2,[1]);
        const limited:Buffer[]=[];driver.page.on('console',m=>{const text=m.text();if(text.startsWith('CCGPU_RETAINED '))limited.push(Buffer.from(text.slice(15),'base64'));});
        await driver.run();await driver.page.waitForTimeout(1800);await driver.pauseSimulation();
        const budget=accounting(limited,1);assert.ok(budget.debit>0 && budget.credit>0);
        assert.ok(budget.unique.every(read=>read.readFloatLE(SOURCE+20)<=.5),'Authored30W limits each at-most-two-phase commit to0.5J');
        assert.ok(reads.at(-1)!.readFloatLE(SOURCE+16)<60,'Half-full authored source funds actual demand');
        await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(connected));
        assert.equal(reads.at(-1)!.readFloatLE(SOURCE+16),60);
        assert.deepEqual(errors,[]);
    }finally{await driver.close();}
});

test('Battery paused intent reverses and Running controls preserve stores across continued ticks',async()=>{
    const driver=await WorkshopDriver.launch({baseUrl:URL}),reads:Buffer[]=[],errors:string[]=[];
    driver.page.on('console',m=>{const text=m.text();if(text.startsWith('CCGPU_RETAINED '))reads.push(Buffer.from(text.slice(15),'base64'));if(text.startsWith('CCGPU '))reads.push(Buffer.from(text.slice(6),'base64'));if(m.type()==='error')errors.push(text);});
    driver.page.on('pageerror',e=>errors.push(e.message));
    try{
        await driver.selectTool('bumper');await driver.placeOnCanvas(720,485);
        await driver.page.keyboard.press('Escape');await driver.selectTool('battery');await driver.placeOnCanvas(920,510);
        await driver.save();const initial=await saved(driver);
        await driver.run();await driver.page.waitForTimeout(400);await driver.pauseSimulation();
        const paused=reads.at(-1)!;await driver.selectPartAt(920,510);await click(driver,130,750);
        await driver.page.screenshot({path:'.anvil/battery-paused-disable-queued.png'});
        await driver.page.keyboard.press('Escape');await driver.selectPartAt(920,510);await click(driver,130,750);
        await driver.page.screenshot({path:'.anvil/battery-paused-enable-queued.png'});
        assert.equal(reads.at(-1)!.readFloatLE(WORK+20),32);
        await driver.resumeSimulation();await driver.page.waitForTimeout(400);await driver.pauseSimulation();
        assert.equal(reads.at(-1)![SOURCE+24],1,'Second paused click reverses queued disable');
        assert.ok(reads.at(-1)!.readBigUInt64LE(24)>paused.readBigUInt64LE(24));
        await driver.selectPartAt(920,510);await click(driver,130,750);await driver.reset();
        await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
        await driver.run();await driver.page.waitForTimeout(400);
        assert.equal(reads.at(-1)![SOURCE+24],1,'Reset retires pending disable');
        await driver.selectPartAt(920,510);
        for(const enabled of [ElectricalEnable.Disabled,ElectricalEnable.Enabled]) {
            const before=reads.at(-1)!;await driver.page.screenshot({path:'.anvil/battery-running-before-'+enabled+'.png'});await click(driver,130,750);
            const start=Date.now();while(Date.now()-start<2500 && !reads.some(read=>read.readBigUInt64LE(16)===before.readBigUInt64LE(16) && read.readBigUInt64LE(24)>before.readBigUInt64LE(24) && read[SOURCE+24]===enabled))await driver.page.waitForTimeout(40);
            const after=reads.findLast(read=>read.readBigUInt64LE(16)===before.readBigUInt64LE(16) && read.readBigUInt64LE(24)>before.readBigUInt64LE(24) && read[SOURCE+24]===enabled)!;assert.ok(after,'Observe committed state after Running toggle');await driver.page.screenshot({path:'.anvil/battery-running-after-'+enabled+'.png'});assert.equal(after[SOURCE+24],enabled);
            assert.ok(after.readBigUInt64LE(24)>before.readBigUInt64LE(24),'Running command commits while physics continues');
            assert.equal(after.readFloatLE(WORK+20),32,'Toggle preserves unspent stored work');
            assert.equal(after.readFloatLE(SOURCE+16),3600);
            await driver.page.waitForTimeout(200);
        }
        await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
        assert.deepEqual(errors,[]);
    }finally{await driver.close();}
});

test('Battery electrical animation survives same-world cadence downshift through existing protocol boundary',async()=>{
    const driver=await WorkshopDriver.launch({baseUrl:URL}),reads:Buffer[]=[],errors:string[]=[];
    driver.page.on('console',m=>{const text=m.text();if(text.startsWith('CCGPU '))reads.push(Buffer.from(text.slice(6),'base64'));if(m.type()==='error')errors.push(text);});
    driver.page.on('pageerror',e=>errors.push(e.message));
    try{
        await driver.selectTool('battery');await driver.placeOnCanvas(920,510);
        await driver.page.waitForTimeout(900);
        const before=await driver.readLastAnimationSample(String(4n<<32n));assert.ok(before);
        const read=reads.at(-1)!,bytes=Buffer.alloc(104);
        bytes.writeBigUInt64LE(read.readBigUInt64LE(0)+1n);bytes.writeUInt32LE(WorkshopCommand.ConfigureCadence,8);bytes.writeUInt32LE(ExpectedRevision.Exact,32);read.copy(bytes,12,12,20);
        bytes.writeBigUInt64LE(read.readBigUInt64LE(16),16);bytes.writeBigUInt64LE(read.readBigUInt64LE(32),24);
        read.copy(bytes,40,40,56);read.copy(bytes,56,88,104);
        for(const [offset,value] of [[72,SettingsVersion.SharedMaster],[76,SimulationCadence.Hz120],[80,PhysicalStepProfile.Canonical480Hz],[84,AnimationCadence.Hz30],[88,PresentationCadence.Hz60],[92,60],[96,1]])bytes.writeUInt32LE(value,offset);
        // No cadence UI exists. This is explicitly a protocol-boundary Chrome regression, not UI acceptance.
        const result=await driver.page.evaluate(async data=>{
            const modulePath='/workshop-client.js'; // Browser-served module, not a Node package.
            const transport=await import(modulePath) as {
                send(id:number,bytes:Uint8Array):Promise<void>;
                acknowledgement(id:number,identity:Uint8Array):Uint8Array;
                completeAcknowledgement(id:number,identity:Uint8Array):void;
            };
            const bytes=new Uint8Array(data),identity=bytes.slice(0,8);
            await transport.send(1,bytes);
            const reply=transport.acknowledgement(1,identity);
            transport.completeAcknowledgement(1,identity);
            return Array.from(reply);
        },Array.from(bytes));
        const response=Buffer.from(result);console.log(JSON.stringify({cadenceBoundary:{outcome:response[9],reason:response[10],sent:[0,16,24,56,64].map(offset=>bytes.readBigUInt64LE(offset).toString()),received:[0,16,32,88,96].map(offset=>response.readBigUInt64LE(offset).toString())}}));assert.equal(response[9],CommandOutcome.Applied,'Existing ConfigureCadence command is applied');
        assert.equal(response.readBigUInt64LE(16),read.readBigUInt64LE(16),'Same physical world');
        assert.ok(response.readBigUInt64LE(88)>read.readBigUInt64LE(88));
        await driver.page.waitForTimeout(500);
        const after=await driver.readLastAnimationSample(String(4n<<32n));assert.ok(after);
        assert.ok(BigInt(after.ordinal)<BigInt(before.ordinal),'30 Hz sample ordinal is lower than prior60 Hz ordinal');
        assert.equal(after.value,1);assert.equal(response.readFloatLE(SOURCE+16),3600);
        assert.deepEqual(errors,[]);
    }finally{await driver.close();}
});

test('Battery accounting deduplicates exact commits and rejects mutated or unfunded credits',()=>{
    const read=Buffer.alloc(SOURCE+32);read.writeBigUInt64LE(1n,16);read.writeBigUInt64LE(1n,24);
    read.writeFloatLE(59,SOURCE+16);read.writeFloatLE(1,SOURCE+20);
    for(let slot=0;slot<2;slot++){read.writeFloatLE(16,WORK+slot*32+20);read.writeFloatLE(.5,WORK+slot*32+24);}
    const unique=accounting([read,Buffer.from(read)],2);assert.equal(unique.unique.length,1);assert.equal(unique.credit,1);assert.equal(unique.debit,1);
    const mutated=Buffer.from(read);mutated.writeFloatLE(.75,WORK+24);
    assert.throws(()=>accounting([read,mutated],2),/bit-identical/);
    assert.throws(()=>accounting([mutated],2),/Finite commit/);
    const free=Buffer.from(read);free.writeFloatLE(0,SOURCE+20);
    assert.throws(()=>accounting([free],2),/Finite commit|Zero debit/);
});
