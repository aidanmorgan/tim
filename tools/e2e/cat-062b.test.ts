import assert from 'node:assert/strict';
import { test } from 'node:test';
import { mkdir } from 'node:fs/promises';
import { resolve, sep } from 'node:path';
import { createRequire } from 'node:module';
import { WorkshopDriver } from './workshop-driver.ts';

enum Lesson { SpringForward = 7, ReadyToRebound = 8 }
enum Route { Passive, Charged, Zero, Away, Ready, MissingReceiver }
enum CapturePhase { Clear, Latched }
enum AuthoredLock { Editable, Locked }
const preview = process.env.PRELOAD_PREVIEW_URL ?? 'http://127.0.0.1:8077/';
const evidence = process.env.PRELOAD_EVIDENCE_DIR ?? '.anvil/preload-ui-' + Date.now();
assert.ok(resolve(evidence).startsWith(resolve('.anvil')+sep),'Evidence stays inside tim/.anvil');
const evidenceReady=mkdir(evidence); // Exclusive once: a prior attempt directory is never overwritten.
const bodies = 128, bodyBytes = 80, captures = 1408, instances = 344, instanceBytes = 160;
const f = Math.fround;
type V = [number, number, number];
type Q = [number, number, number, number];
function rotate(v: V, q: Q): V {
    const [x,y,z,w]=q, [a,b,c]=v;
    const tx=2*(y*c-z*b),ty=2*(z*a-x*c),tz=2*(x*b-y*a);
    return [a+w*tx+y*tz-z*ty,b+w*ty+z*tx-x*tz,c+w*tz+x*ty-y*tx];
}
function position(bytes: Buffer, cell: number, local: number): V {
    return [0,1,2].map(i=>(bytes.readInt32LE(cell+i*4)+bytes.readFloatLE(local+i*4))/16) as V;
}
function quaternion(bytes: Buffer, offset: number): Q {
    return [0,1,2,3].map(i=>bytes.readFloatLE(offset+i*4)) as Q;
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
function authored(bytes: Buffer) { return Buffer.concat([bytes.subarray(0,24),bytes.subarray(32)]); }
function body(read: Buffer,id: bigint): Buffer {
    for(let i=0;i<read[104];i++){
        const slot=read.subarray(bodies+i*bodyBytes,bodies+(i+1)*bodyBytes);
        if(slot.readBigUInt64LE(0)===id)return slot;
    }
    throw new Error('Missing physical body '+id);
}
function assertInitial(read: Buffer, save: Buffer, boardSlot: number, compression: number) {
    assert.equal(read.readBigUInt64LE(24),0n);
    const board=save.subarray(boardSlot,boardSlot+instanceBytes), owner=board.readBigUInt64LE(40);
    const plate=body(read,(1n<<32n)+64n+owner*64n+2n);
    const base=position(board,48,72), actual=position(plate,8,20), q=quaternion(board,96);
    const anchor=rotate([0,f(.14),0],q), axis=rotate([0,1,0],q);
    const delta=actual.map((value,i)=>f(f(value-base[i])-anchor[i]));
    const displacement=f(f(f(delta[0]*axis[0])+f(delta[1]*axis[1]))+f(delta[2]*axis[2]));
    assert.ok(Math.abs(displacement+compression)<3e-7,JSON.stringify({displacement,compression}));
    const energy=.5*board.readFloatLE(112)*displacement*displacement;
    if(compression===.2)assert.ok(Math.abs(energy-8)<=1e-5,JSON.stringify({energy,displacement}));
    for(let offset=56;offset<80;offset+=4)assert.equal(plate.readFloatLE(offset),0,'Initial plate velocity/spin is zero');
    return energy;
}
function clearances(save: Buffer, boardSlot: number) {
    const board=save.subarray(boardSlot,boardSlot+instanceBytes),ball=save.subarray(instances,instances+instanceBytes);
    const p=position(board,48,72),b=position(ball,48,72),q=quaternion(board,96);
    const local=rotate(b.map((v,i)=>v-p[i]) as V,[-q[0],-q[1],-q[2],q[3]]);
    // Actual admitted inherited shape lanes: Half(.34), Half(.075), Half(.06), Half(.36), Half(.65), Half(.6).
    const radius=half(0x3571);
    const gap=(center: number, height: number)=>Math.hypot(
        Math.max(Math.abs(local[0])-half(0x3933),0),
        Math.max(Math.abs(local[1]-center)-height,0),
        Math.max(Math.abs(local[2])-half(0x38cd),0))-radius;
    const plate=gap(f(f(.14)-board.readFloatLE(120)),half(0x2ccd));
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
    const bytes=await saved(d),boardSlot=ready?instances+instanceBytes:instances+2*instanceBytes;
    assert.equal(bytes.readUInt32LE(4),13);
    assert.equal(bytes.readUInt32LE(72),ready?Lesson.ReadyToRebound:Lesson.SpringForward);
    const board=bytes.subarray(boardSlot,boardSlot+instanceBytes);
    const expectedCompression=route===Route.Passive||route===Route.Zero?0:.2;
    assert.equal(board.readFloatLE(112),400);assert.equal(board.readFloatLE(116),f(.2));
    assert.equal(board.readFloatLE(120),f(expectedCompression));
    if(!ready){
        assert.equal(half(bytes.readUInt16LE(80)),1,'Actual precision is Precise');
        assert.equal(bytes.readUInt32LE(instances+4),AuthoredLock.Locked);
        assert.equal(bytes.readUInt32LE(instances+instanceBytes+4),AuthoredLock.Locked);
        assert.ok(position(bytes,instances+instanceBytes+48,instances+instanceBytes+72).every((v,i)=>Math.abs(v-[-.7,.55,0][i])<1e-7));
        assert.equal(half(bytes.readUInt16LE(instances+instanceBytes+140)),0,'Precise Receiver guide is zero');
        assert.ok(position(bytes,instances+48,instances+72).every((v,i)=>Math.abs(v-[-3,3.6,0][i])<1e-7));
        const expectedQ: Q=route===Route.Passive?[0,0,-.17364820837974548,.9848077297210693]
            :route===Route.Away?[-.11166255176067352,.9937461614608765,7.4505797087454084e-9,2.980232594040899e-8]
            :[0,0,-.11166252195835114,.9937461614608765];
        assert.deepEqual(quaternion(board,96),expectedQ,'Actual authored orientation, including matched zero and world-Y180 control');
        assert.deepEqual(position(board,48,72),route===Route.Passive?[-3,f(.8),0]:[f(-3.315333366394043),f(3.1000001430511475),0]);
        if(route!==Route.Passive)clearances(bytes,boardSlot);
    }else{
        assert.deepEqual(position(board,48,72),[0,2.5,0]);
        assert.equal(half(bytes.readUInt16LE(80)),half(0x3733),'Actual Ready precision is source Balanced');
        assert.equal(board.readUInt32LE(4),AuthoredLock.Locked);
        assert.equal(bytes.readUInt32LE(instances+4),AuthoredLock.Locked);
        assert.deepEqual(quaternion(board,96),[-0,-0,-.2588190734386444,.9659258723258972]);
        assert.deepEqual(position(bytes,instances+48,instances+72),[.1785000003874302,2.8091710000298917,0]);
        if(route===Route.Ready)assert.deepEqual(position(bytes,instances+2*instanceBytes+48,instances+2*instanceBytes+72),[f(1.8000000715),f(1.3000000715),0]);
    }
    return {bytes,boardSlot,compression:expectedCompression};
}
for(const route of [Route.Passive,Route.Charged,Route.Zero,Route.Away,Route.Ready,Route.MissingReceiver]){
    test('CAT062b route '+Route[route]+' preserves construction and repeats after Reset/Load',async()=>{
        await evidenceReady;
        const d=await WorkshopDriver.launch({baseUrl:preview}),reads:Buffer[]=[],errors:string[]=[];
        d.page.on('console',m=>{if(m.text().startsWith('CCGPU '))reads.push(Buffer.from(m.text().slice(6),'base64'));if(m.type()==='error')errors.push(m.text());});
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
                await d.pauseSimulation();assert.ok(reads.at(-1)!.readBigUInt64LE(24)>0n);
                console.log(JSON.stringify({route:Route[route],replay,motion}));
                const result=(await d.readCaptured())-before;results.push(result);assert.equal(result,expected);
                await d.page.screenshot({path:evidence+'/'+Route[route]+'-run'+replay+'.png'});
                await d.reset();const reset=reads.at(-1)!;
                assertInitial(reset,configuration.bytes,configuration.boardSlot,configuration.compression);
                assert.deepEqual(reset.subarray(bodies,bodies+reset[104]*bodyBytes),initial.subarray(bodies,bodies+initial[104]*bodyBytes),'Reset restores all physical poses and velocities exactly');
                assert.equal(reset[105],initial[105],'Reset retains the exact capture population');
                if(reset[105]){assert.equal(reset.readUInt32LE(captures+8),CapturePhase.Clear);assert.equal(reset.readUInt32LE(captures+12),0);}
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
    const d=await WorkshopDriver.launch({baseUrl:preview}),reads:Buffer[]=[],errors:string[]=[];
    d.page.on('console',m=>{if(m.text().startsWith('CCGPU '))reads.push(Buffer.from(m.text().slice(6),'base64'));if(m.type()==='error')errors.push(m.text());});
    d.page.on('pageerror',e=>errors.push(String(e)));
    try{
        const original=await construct(d,Route.Ready),oldId=original.bytes.readBigUInt64LE(instances+2*instanceBytes+40);
        await d.selectPartAt(837,598);await d.page.keyboard.press('Delete');await d.page.waitForTimeout(300);await d.save();
        assert.equal((await saved(d)).readUInt32LE(32),2,'Receiver actually removed');
        await d.clickAt(130,155);await d.placeOnCanvas(837,598);await d.page.keyboard.press('Escape');await d.save();
        const replacement=await saved(d),newId=replacement.readBigUInt64LE(instances+2*instanceBytes+40);
        assert.ok(newId>oldId,'Replacement owns a new authored identity');
        const expectedSensor=(1n<<32n)+64n+newId*64n+6n;
        assert.equal(replacement.readBigUInt64LE(104),newId);
        assert.equal(replacement.readBigUInt64LE(112),expectedSensor);
        await d.load();await d.save();assert.deepEqual(authored(await saved(d)),authored(replacement));
        const initial=reads.at(-1)!;
        assert.equal(initial.readBigUInt64LE(captures),expectedSensor);
        assertInitial(initial,replacement,instances+instanceBytes,.2);
        const before=await d.readCaptured();await d.run(4000);await d.pauseSimulation();
        assert.equal((await d.readCaptured())-before,1,'Replacement sensor completes the authored goal');
        await d.reset();assert.equal(reads.at(-1)!.readBigUInt64LE(captures),expectedSensor);
        assertInitial(reads.at(-1)!,replacement,instances+instanceBytes,.2);
        await d.page.screenshot({path:evidence+'/ready-replaced-reset.png'});
        assert.deepEqual(errors,[]);console.log(JSON.stringify({oldId:String(oldId),newId:String(newId),expectedSensor:String(expectedSensor),errors}));
    }finally{await d.close();}
});


