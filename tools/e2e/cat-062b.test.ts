import assert from 'node:assert/strict';
import { test } from 'node:test';
import { mkdir } from 'node:fs/promises';
import { resolve, sep } from 'node:path';
import { createRequire } from 'node:module';
import { WorkshopDriver } from './workshop-driver.ts';
import { decodeWorkshopResponse, decodeSavedConstruction, authoredState, type BodyRead, type WorkshopResponse } from './workshop-wire.ts';

enum Lesson { SpringForward = 7, ReadyToRebound = 8 }
enum Route { Passive, Charged, Zero, Away, Ready, MissingReceiver }
enum CapturePhase { Clear, Latched }
enum AuthoredLock { Editable, Locked }
const evidence = process.env.PRELOAD_EVIDENCE_DIR ?? '.anvil/preload-ui-' + Date.now();
assert.ok(resolve(evidence).startsWith(resolve('.anvil')+sep),'Evidence stays inside tim/.anvil');
const evidenceReady=mkdir(evidence); // Exclusive once: a prior attempt directory is never overwritten.
const instances = 344, instanceBytes = 160;
function bodyState(b: BodyRead) {
    return {
        id: b.id(),
        cell: { x: b.cell()!.x(), y: b.cell()!.y(), z: b.cell()!.z() },
        local: { x: b.local()!.x(), y: b.local()!.y(), z: b.local()!.z() },
        rotation: { x: b.rotation()!.x(), y: b.rotation()!.y(), z: b.rotation()!.z(), w: b.rotation()!.w() },
        com: { x: b.com()!.x(), y: b.com()!.y(), z: b.com()!.z() },
        velocity: { x: b.velocity()!.x(), y: b.velocity()!.y(), z: b.velocity()!.z() },
        angularVelocity: { x: b.angularVelocity()!.x(), y: b.angularVelocity()!.y(), z: b.angularVelocity()!.z() },
    };
}
const f = Math.fround;
type V = [number, number, number];
type Q = [number, number, number, number];
function rotate(v: V, q: Q): V {
    const [x,y,z,w]=q, [a,b,c]=v;
    const tx=2*(y*c-z*b),ty=2*(z*a-x*c),tz=2*(x*b-y*a);
    return [a+w*tx+y*tz-z*ty,b+w*ty+z*tx-x*tz,c+w*tz+x*ty-y*tx];
}
function half(bits: number): number {
    const sign=bits&0x8000?-1:1,e=(bits>>>10)&31,m=bits&1023;
    return sign*(e===0?m*2**-24:(1+m/1024)*2**(e-15));
}
async function saved(d: WorkshopDriver): Promise<Buffer> {
    return Buffer.from(await d.page.evaluate(()=>new Promise<number[]>((resolve,reject)=>{
        const request=indexedDB.open('curious-contraptions-workshop');
        request.onsuccess=()=>{
            const db=request.result,tx=db.transaction('construction','readonly'),value=tx.objectStore('construction').get(1);
            tx.oncomplete=()=>{resolve(Array.from(value.result as Uint8Array));db.close();};
            tx.onabort=()=>reject(new Error('Save read aborted'));
        };
        request.onerror=()=>reject(new Error('Save unavailable'));
    })));
}
const authored = (bytes: Buffer) => authoredState(bytes);
function body(read: WorkshopResponse,id: bigint): BodyRead {
    for(let i=0;i<read.bodiesLength();i++){
        const slot=read.bodies(i)!;
        if(slot.id()===id)return slot;
    }
    throw new Error('Missing physical body '+id);
}
function assertInitial(read: WorkshopResponse, save: Buffer, boardIndex: number, compression: number) {
    assert.equal(read.tick(),0n);
    const savedFb=decodeSavedConstruction(save);
    const board=savedFb.construction()!.instances(boardIndex)!;
    const owner=board.id();
    const plate=body(read,(1n<<32n)+256n+owner*256n+2n);
    const base: V = [(board.cell()!.x()+board.local()!.x())/16, (board.cell()!.y()+board.local()!.y())/16, (board.cell()!.z()+board.local()!.z())/16];
    const actual: V =[(plate.cell()!.x()+plate.local()!.x())/16,(plate.cell()!.y()+plate.local()!.y())/16,(plate.cell()!.z()+plate.local()!.z())/16];
    const q: Q =[board.rotation()!.x(), board.rotation()!.y(), board.rotation()!.z(), board.rotation()!.w()];
    const anchor=rotate([0,f(.14),0],q), axis=rotate([0,1,0],q);
    const delta=actual.map((value,i)=>f(f(value-base[i])-anchor[i]));
    const displacement=f(f(f(delta[0]*axis[0])+f(delta[1]*axis[1]))+f(delta[2]*axis[2]));
    assert.ok(Math.abs(displacement+compression)<3e-7,JSON.stringify({displacement,compression}));
    const energy=.5*board.stiffness()*displacement*displacement;
    if(compression===.2)assert.ok(Math.abs(energy-8)<=1e-5,JSON.stringify({energy,displacement}));
    assert.equal(plate.velocity()!.x(),0,'Initial plate velocity/spin is zero');
    assert.equal(plate.velocity()!.y(),0,'Initial plate velocity/spin is zero');
    assert.equal(plate.velocity()!.z(),0,'Initial plate velocity/spin is zero');
    assert.equal(plate.angularVelocity()!.x(),0,'Initial plate velocity/spin is zero');
    assert.equal(plate.angularVelocity()!.y(),0,'Initial plate velocity/spin is zero');
    assert.equal(plate.angularVelocity()!.z(),0,'Initial plate velocity/spin is zero');
    return energy;
}
function clearances(save: Buffer, boardIndex: number) {
    const savedFb=decodeSavedConstruction(save);
    const board=savedFb.construction()!.instances(boardIndex)!;
    const ball=savedFb.construction()!.instances(0)!;
    const p: V=[(board.cell()!.x()+board.local()!.x())/16,(board.cell()!.y()+board.local()!.y())/16,(board.cell()!.z()+board.local()!.z())/16];
    const b: V=[(ball.cell()!.x()+ball.local()!.x())/16,(ball.cell()!.y()+ball.local()!.y())/16,(ball.cell()!.z()+ball.local()!.z())/16];
    const q: Q=[board.rotation()!.x(),board.rotation()!.y(),board.rotation()!.z(),board.rotation()!.w()];
    const local=rotate(b.map((v,i)=>v-p[i]) as V,[-q[0],-q[1],-q[2],q[3]]);
    const radius=half(0x3571);
    const gap=(center: number, height: number)=>Math.hypot(
        Math.max(Math.abs(local[0])-half(0x3933),0),
        Math.max(Math.abs(local[1]-center)-height,0),
        Math.max(Math.abs(local[2])-half(0x38cd),0))-radius;
    const plate=gap(f(f(.14)-board.initialCompression()),half(0x2ccd));
    const base=gap(-half(0x35c3),half(0x2bae));
    assert.ok(plate>0&&base>0,JSON.stringify({local,plate,base}));
    return {plate,base};
}
function renderedRestoration(before: Buffer, after: Buffer, route: Route) {
    const requireMcp=createRequire('/opt/homebrew/lib/node_modules/@playwright/mcp/package.json');
    const {PNG}=requireMcp('./node_modules/playwright-core/lib/utilsBundle.js');
    const a=PNG.sync.read(before),b=PNG.sync.read(after);
    const [cx,cy]=route===Route.Passive?[524,633]:route===Route.Ready||route===Route.MissingReceiver?[720,534]:[505,483];
    let changed=0;
    for(let y=cy-45;y<cy+45;y++)for(let x=cx-55;x<cx+55;x++){
        const at=(y*a.width+x)*4;
        if([0,1,2].some(channel=>Math.abs(a.data[at+channel]-b.data[at+channel])>2))changed++;
    }
    assert.equal(changed,0,'Reset restores rendered compressed plate and coil');
}
async function choose(d: WorkshopDriver, lesson: Lesson) {
    await d.clickAt(600,47);await d.page.waitForTimeout(350);
    await d.clickAt(600,lesson===Lesson.SpringForward?277:304);await d.page.waitForTimeout(400);
}
async function precision(d: WorkshopDriver, precise: boolean) {
    await d.clickAt(1394,46);await d.clickAt(precise?1396:1040,350);
    await d.page.keyboard.press('Escape');await d.page.waitForTimeout(200);
}
async function construct(d: WorkshopDriver, route: Route) {
    const ready=route===Route.Ready||route===Route.MissingReceiver;
    await choose(d,ready?Lesson.ReadyToRebound:Lesson.SpringForward);
    if(!ready)await precision(d,true);
    if(ready){
        if(route===Route.Ready){await d.clickAt(130,155);await d.placeOnCanvas(837,598);}
    }else if(route===Route.Passive){
        await d.clickAt(130,155);await d.placeOnCanvas(524,633);await d.selectPartAt(524,633);
        await d.tiltSelectedByFineRotate(-4);
    }else{
        await d.clickAt(130,155);await d.placeOnCanvas(505,483);await d.selectPartAt(505,483);
        await d.tiltSelectedByFineRotate(-3);await d.selectPartAt(505,483);
        await d.page.mouse.move(450,533);await d.page.mouse.down();await d.page.waitForTimeout(70);
        await d.page.mouse.move(452,535,{steps:4});await d.page.mouse.up();await d.page.waitForTimeout(250);
        await d.clickAt(60,285);await d.page.mouse.move(570,483);await d.page.mouse.down();await d.page.waitForTimeout(70);
        await d.page.mouse.move(569,483,{steps:4});await d.page.mouse.up();await d.page.waitForTimeout(250);
        await d.clickAt(130,230);await d.clickAt(100,310);await d.page.keyboard.press('Meta+A');
        await d.page.keyboard.type(route===Route.Zero?'0':'0.2',{delay:40});await d.page.keyboard.press('Enter');
        await d.clickAt(130,353);await d.page.waitForTimeout(250);
        if(route===Route.Away){
            await d.openMenu();await d.page.mouse.move(1200,500);await d.page.mouse.wheel(0,2000);await d.page.waitForTimeout(550);
            await d.clickAt(1144,461);await d.page.waitForTimeout(200);await d.clickAt(1144,461);await d.page.waitForTimeout(200);
            await d.page.mouse.wheel(0,-2000);await d.page.waitForTimeout(550);await d.closeMenu();
        }
    }
    await d.page.keyboard.press('Escape');await d.save();
    const bytes=await saved(d);
    const savedFb=decodeSavedConstruction(bytes);
    const boardIndex=ready?1:2;
    assert.equal(savedFb.magic(),0x53574343);
    assert.equal(savedFb.construction()!.puzzle()!.id(),ready?Lesson.ReadyToRebound:Lesson.SpringForward);
    const board=savedFb.construction()!.instances(boardIndex)!;
    const expectedCompression=route===Route.Passive||route===Route.Zero?0:.2;
    assert.equal(board.stiffness(),400);assert.equal(board.damping(),f(.2));
    assert.equal(board.initialCompression(),f(expectedCompression));
    if(!ready){
        assert.equal(savedFb.construction()!.puzzle()!.precision(),1,'Actual precision is Precise');
        assert.equal(savedFb.construction()!.instances(0)!.locked(),true);
        assert.equal(savedFb.construction()!.instances(1)!.locked(),true);
        const rx=savedFb.construction()!.instances(1)!;
        const rxPos: V=[(rx.cell()!.x()+rx.local()!.x())/16,(rx.cell()!.y()+rx.local()!.y())/16,(rx.cell()!.z()+rx.local()!.z())/16];
        assert.ok(rxPos.every((v,i)=>Math.abs(v-[-.7,.55,0][i])<1e-7));
        // The guide is the Receiver force region's maximum acceleration (pre-FlatBuffers save offset +140; cat-015b asserts the same field).
        assert.equal(rx.maxAcceleration(),0,'Precise Receiver guide is zero');
        const b0=savedFb.construction()!.instances(0)!;
        const b0Pos: V=[(b0.cell()!.x()+b0.local()!.x())/16,(b0.cell()!.y()+b0.local()!.y())/16,(b0.cell()!.z()+b0.local()!.z())/16];
        assert.ok(b0Pos.every((v,i)=>Math.abs(v-[-3,3.6,0][i])<1e-7));
        const expectedQ: Q=route===Route.Passive?[0,0,-.17364820837974548,.9848077297210693]
            :route===Route.Away?[-.11166255176067352,.9937461614608765,7.4505797087454084e-9,2.980232594040899e-8]
            :[0,0,-.11166252195835114,.9937461614608765];
        const bQ: Q=[board.rotation()!.x(),board.rotation()!.y(),board.rotation()!.z(),board.rotation()!.w()];
        assert.deepEqual(bQ,expectedQ,'Actual authored orientation, including matched zero and world-Y180 control');
        const bPos: V=[(board.cell()!.x()+board.local()!.x())/16,(board.cell()!.y()+board.local()!.y())/16,(board.cell()!.z()+board.local()!.z())/16];
        assert.deepEqual(bPos,route===Route.Passive?[-3,f(.8),0]:[f(-3.315333366394043),f(3.1000001430511475),0]);
        if(route!==Route.Passive)clearances(bytes,boardIndex);
    }else{
        const bPos: V=[(board.cell()!.x()+board.local()!.x())/16,(board.cell()!.y()+board.local()!.y())/16,(board.cell()!.z()+board.local()!.z())/16];
        assert.deepEqual(bPos,[0,2.5,0]);
        assert.ok(Math.abs(savedFb.construction()!.puzzle()!.precision()-.45)<.01,'Actual Ready precision is source Balanced');
        assert.equal(board.locked(),true);
        assert.equal(savedFb.construction()!.instances(0)!.locked(),true);
        const bQ: Q=[board.rotation()!.x(),board.rotation()!.y(),board.rotation()!.z(),board.rotation()!.w()];
        assert.deepEqual(bQ,[-0,-0,-.2588190734386444,.9659258723258972]);
        const b0=savedFb.construction()!.instances(0)!;
        const b0Pos: V=[(b0.cell()!.x()+b0.local()!.x())/16,(b0.cell()!.y()+b0.local()!.y())/16,(b0.cell()!.z()+b0.local()!.z())/16];
        assert.deepEqual(b0Pos,[.1785000003874302,2.8091710000298917,0]);
        if(route===Route.Ready){
            const b2=savedFb.construction()!.instances(2)!;
            const b2Pos: V=[(b2.cell()!.x()+b2.local()!.x())/16,(b2.cell()!.y()+b2.local()!.y())/16,(b2.cell()!.z()+b2.local()!.z())/16];
            assert.deepEqual(b2Pos,[f(1.8000000715),f(1.3000000715),0]);
        }
    }
    return {bytes,boardSlot:boardIndex,compression:expectedCompression};
}
for(const route of [Route.Passive,Route.Charged,Route.Zero,Route.Away,Route.Ready,Route.MissingReceiver]){
    test('CAT062b route '+Route[route]+' preserves construction and repeats after Reset/Load',async()=>{
        await evidenceReady;
        const d=await WorkshopDriver.launch(),reads:WorkshopResponse[]=[],errors:string[]=[];
        d.page.on('console',m=>{if(m.text().startsWith('CCGPU '))reads.push(decodeWorkshopResponse(Buffer.from(m.text().slice(6),'base64')));if(m.type()==='error')errors.push(m.text());});
        d.page.on('pageerror',e=>errors.push(String(e)));
        try{
            const configuration=await construct(d,route),initial=reads.at(-1)!;
            const initialEnergy=assertInitial(initial,configuration.bytes,configuration.boardSlot,configuration.compression);
            const buildImage=await d.page.screenshot({path:evidence+'/'+Route[route]+'-build.png'});
            const expected=route===Route.Passive||route===Route.Charged||route===Route.Ready?1:0;
            const results=[];
            for(let replay=0;replay<2;replay++){
                const before=await d.readCaptured();await d.run();
                const motion=[];let lastSequence=-1n,lastTime=-1n;
                const started=Date.now();
                while(Date.now()-started<4000){
                    const slot=await d.readLatestPose();
                    if(slot&&BigInt(slot.sequence)!==lastSequence){
                        assert.ok(BigInt(slot.sequence)>lastSequence&&BigInt(slot.timestamp)>lastTime);
                        lastSequence=BigInt(slot.sequence);lastTime=BigInt(slot.timestamp);motion.push(slot);
                    }
                    await d.page.waitForTimeout(30);
                }
                assert.ok(motion.length>20,'Distinct physical publications progress during Run');
                if(route===Route.Ready)assert.ok(Math.max(...motion.flatMap(slot=>slot.bodies.filter(b=>b.id==='1').map(b=>b.py)))>3.009171,'Precharged resting payload rises more than0.2m');
                await d.pauseSimulation();assert.ok(reads.at(-1)!.tick()>0n);
                console.log(JSON.stringify({route:Route[route],replay,motion}));
                const result=(await d.readCaptured())-before;results.push(result);assert.equal(result,expected);
                await d.page.screenshot({path:evidence+'/'+Route[route]+'-run'+replay+'.png'});
                await d.reset();const reset=reads.at(-1)!;
                assertInitial(reset,configuration.bytes,configuration.boardSlot,configuration.compression);
                assert.equal(reset.bodiesLength(),initial.bodiesLength(),'Reset restores all physical poses and velocities exactly');
                for(let i=0;i<reset.bodiesLength();i++)assert.deepEqual(bodyState(reset.bodies(i)!),bodyState(initial.bodies(i)!),'Reset restores all physical poses and velocities exactly');
                assert.equal(reset.capturesLength(),initial.capturesLength(),'Reset retains the exact capture population');
                if(reset.capturesLength()>0){assert.equal(reset.captures(0)!.phase(),CapturePhase.Clear);assert.equal(reset.captures(0)!.eventOrdinal(),0);}
                await d.save();assert.deepEqual(authored(await saved(d)),authored(configuration.bytes));
                const resetImage=await d.page.screenshot({path:evidence+'/'+Route[route]+'-reset'+replay+'.png'});
                renderedRestoration(buildImage,resetImage,route);
                if(replay===0){await d.load();await d.save();assert.deepEqual(authored(await saved(d)),authored(configuration.bytes));}
            }
            assert.deepEqual(errors,[]);console.log(JSON.stringify({route:Route[route],initialEnergy,results,errors}));
        }finally{await d.close();}
    });
}

test('CAT062b Ready Receiver deletion and replacement rebind the saved capture goal',async()=>{
    await evidenceReady;
    const d=await WorkshopDriver.launch(),reads:WorkshopResponse[]=[],errors:string[]=[];
    d.page.on('console',m=>{if(m.text().startsWith('CCGPU '))reads.push(decodeWorkshopResponse(Buffer.from(m.text().slice(6),'base64')));if(m.type()==='error')errors.push(m.text());});
    d.page.on('pageerror',e=>errors.push(String(e)));
    try{
        const original=await construct(d,Route.Ready);
        const originalFb=decodeSavedConstruction(original.bytes).construction()!;
        const oldId=originalFb.instances(2)!.id();
        // The captured ball is identified from the UI-built construction (its saved part identity), never assumed to be 1.
        const ball=Array.from({length:originalFb.instancesLength()},(_,i)=>originalFb.instances(i)!).find(instance=>instance.partId()==='ball');
        assert.ok(ball,'The Ready lesson construction holds its ball');const ballId=ball.id();
        assert.equal(originalFb.puzzle()!.goalBody(),ballId,'Before replacement the captured ball is the goal body');
        await d.selectPartAt(837,598);await d.page.keyboard.press('Delete');await d.page.waitForTimeout(300);await d.save();
        assert.equal(decodeSavedConstruction(await saved(d)).construction()!.instancesLength(),2,'Receiver actually removed');
        await d.clickAt(130,155);await d.placeOnCanvas(837,598);await d.page.keyboard.press('Escape');await d.save();
        const replacementBytes=await saved(d);
        const replacementFb=decodeSavedConstruction(replacementBytes);
        const newId=replacementFb.construction()!.instances(2)!.id();
        assert.ok(newId>oldId,'Replacement owns a new authored identity');
        const expectedSensor=(1n<<32n)+256n+newId*256n+6n;
        // A Captured goal declares Body = ball and Target = Receiver (SpringboardLessons.WithReceiver); the replacement rebinds the target.
        assert.equal(replacementFb.construction()!.puzzle()!.goalBody(),ballId,'The captured ball stays the goal body');
        assert.equal(replacementFb.construction()!.puzzle()!.goalTarget(),newId,'The replacement Receiver becomes the goal target');
        assert.equal(replacementFb.construction()!.puzzle()!.goalEventSource(),expectedSensor);
        await d.load();await d.save();assert.deepEqual(authoredState(await saved(d)),authoredState(replacementBytes));
        const initial=reads.at(-1)!;
        assert.equal(initial.captures(0)!.sensor(),expectedSensor);
        assertInitial(initial,replacementBytes,1,.2);
        const before=await d.readCaptured();await d.run(4000);await d.pauseSimulation();
        assert.equal((await d.readCaptured())-before,1,'Replacement sensor completes the authored goal');
        await d.reset();assert.equal(reads.at(-1)!.captures(0)!.sensor(),expectedSensor);
        assertInitial(reads.at(-1)!,replacementBytes,1,.2);
        await d.page.screenshot({path:evidence+'/ready-replaced-reset.png'});
        assert.deepEqual(errors,[]);console.log(JSON.stringify({oldId:String(oldId),newId:String(newId),expectedSensor:String(expectedSensor),errors}));
    }finally{await d.close();}
});


