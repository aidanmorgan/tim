import assert from 'node:assert/strict';
import {test} from 'node:test';
import {mkdirSync} from 'node:fs';
import {resolve,sep,join} from 'node:path';
import {WorkshopDriver} from './workshop-driver.ts';
import {decodeWorkshopResponse, decodeSavedConstruction, authoredState, type BodyRead, type WorkshopResponse, type SavedConstruction} from './workshop-wire.ts';

// Closed numeric sets; external level names are converted only at the driver boundary.
enum Lesson { Depth=5, Wall=6 }
enum Route { Paid, Miss }
enum WorkEffect { Passive, Paid }
enum CapturePhase { Clear, Latched }
function bodyState(b: BodyRead) {
    return {
        id: b.id(),
        cell: { x: b.cell()!.x(), y: b.cell()!.y(), z: b.cell()!.z() },
        local: { x: b.local()!.x(), y: b.local()!.y(), z: b.local()!.z() },
        rotation: { x: b.rotation()!.x(), y: b.rotation()!.y(), z: b.rotation()!.z(), w: b.rotation()!.w() },
        velocity: { x: b.velocity()!.x(), y: b.velocity()!.y(), z: b.velocity()!.z() },
        angularVelocity: { x: b.angularVelocity()!.x(), y: b.angularVelocity()!.y(), z: b.angularVelocity()!.z() },
    };
}
const evidence=resolve(process.env.CAT015B_EVIDENCE_DIR??('.anvil/cat015b-'+Date.now()+'-'+process.pid));
if(!evidence.startsWith(resolve('.anvil')+sep))throw new Error('Evidence must remain inside tim/.anvil');
mkdirSync(evidence); // Refuse an existing run directory: never overwrite earlier attempts.
console.log('CAT015B_EVIDENCE',evidence);
async function press(d:WorkshopDriver,x:number,y:number){
    await d.page.mouse.move(x,y);await d.page.mouse.down();await d.page.waitForTimeout(70);
    await d.page.mouse.up();await d.page.waitForTimeout(200);
}
async function drag(d:WorkshopDriver,x:number,y:number,a:number,b:number){
    await d.page.mouse.move(x,y);await d.page.mouse.down();await d.page.waitForTimeout(70);
    await d.page.mouse.move(a,b,{steps:12});await d.page.mouse.up();await d.page.waitForTimeout(250);
}
async function saved(d:WorkshopDriver):Promise<Buffer>{
    return Buffer.from(await d.page.evaluate(()=>new Promise<number[]>((resolve,reject)=>{
        const request=indexedDB.open('curious-contraptions-workshop');
        request.onerror=()=>reject(new Error('Save unavailable'));
        request.onsuccess=()=>{const db=request.result;const tx=db.transaction('construction','readonly');
            const read=tx.objectStore('construction').get(1);
            tx.oncomplete=()=>{db.close();resolve(Array.from(read.result as Uint8Array));};
            tx.onabort=()=>{db.close();reject(new Error('Save read failed'));};};
    })));
}
function position(saved: SavedConstruction, i: number, a: number) {
    const inst = saved.construction()!.instances(i)!;
    const c = inst.cell()!;
    const l = inst.local()!;
    const cv = [c.x(), c.y(), c.z()][a];
    const lv = [l.x(), l.y(), l.z()][a];
    return (cv + lv) / 16;
}
async function difficultyControls(d:WorkshopDriver){
    // The existing slider is a real UI control. Verify committed saved precision at each endpoint,
    // then restore the original Balanced45 profile before construction.
    for(const [x,value,margin,speed,dwell,guide] of [[1040,0,.3,3,.15,12],[1396,1,.02,1.5,.35,0],[1200,.45,.174,2.325,.24,6.6]]){
        await press(d,1394,46);await press(d,x,350);
        await d.page.keyboard.press('Escape',{delay:70});await d.save();
        const bytes=await saved(d);
        const savedFb = decodeSavedConstruction(bytes);
        assert.ok(Math.abs(savedFb.construction()!.puzzle()!.precision()-value)<.001);
        const rx = savedFb.construction()!.instances(1)!;
        assert.ok(Math.abs(rx.margin()-margin)<.002, 'Receiver setting at precision '+value+' margin');
        assert.ok(Math.abs(rx.speedLimit()-speed)<.002, 'Receiver setting at precision '+value+' speed');
        assert.ok(Math.abs(rx.dwell()-dwell)<.002, 'Receiver setting at precision '+value+' dwell');
        assert.ok(Math.abs(rx.maxAcceleration()-guide)<.002, 'Receiver setting at precision '+value+' guide');
    }
}
for(const lesson of [Lesson.Depth,Lesson.Wall])for(const route of [Route.Paid,Route.Miss])
test('CAT015b lesson '+lesson+' route '+route+' real UI and exact lifecycle',async()=>{
    const d=await WorkshopDriver.launch(),reads:WorkshopResponse[]=[],events:WorkshopResponse[]=[],errors:string[]=[];
    d.page.on('console',m=>{const t=m.text();
        if(t.startsWith('CCGPU '))reads.push(decodeWorkshopResponse(Buffer.from(t.slice(6),'base64')));
        if(t.startsWith('CCGPU_RETAINED '))events.push(decodeWorkshopResponse(Buffer.from(t.slice(15),'base64')));
        // A clock recalibration fails every suite through WorkshopDriver.close (Story 9.4b).
        if(m.type()==='error')errors.push(t);
    });d.page.on('pageerror',e=>errors.push(e.message));
    try{
        await d.selectLevel(lesson===Lesson.Depth?'bumper_depth':'wall_and_bumper');
        await difficultyControls(d);
        await d.selectTool('bumper');await d.placeOnCanvas(lesson===Lesson.Depth?720:505,620);
        if(lesson===Lesson.Depth&&route===Route.Paid){
            await d.selectPartAt(720,620);await d.page.keyboard.press('q',{delay:1300});
            await d.page.waitForTimeout(300);await d.setPartMode('move');
            await d.page.keyboard.down('Shift');await drag(d,636,607,421,607);await d.page.keyboard.up('Shift');
        }
        if(lesson===Lesson.Wall&&route===Route.Paid){
            await d.selectTool('wall');await d.placeOnCanvas(655,424);await d.selectPartAt(655,424);
            await d.setPartMode('resize');await d.page.keyboard.down('Shift');
            await drag(d,785,424,695,424);await drag(d,655,350,655,180);await d.page.keyboard.up('Shift');
            await d.page.keyboard.press('q',{delay:1300});await d.page.waitForTimeout(300);
            await d.page.keyboard.down('Shift');await drag(d,556,404,515,404);await d.page.keyboard.up('Shift');
        }
        await d.page.keyboard.press('Escape',{delay:70});await d.save();const initialBytes=await saved(d);
        const initial=decodeSavedConstruction(initialBytes);
        assert.equal(initial.construction()!.puzzle()!.id(),lesson);assert.equal(initial.construction()!.connectionsLength(),0);
        const fixture=lesson===Lesson.Depth?[[0,5,3],[0,.9,-2]]:[[-3,5,0],[-5,.9,0]];
        for(let i=0;i<2;i++)for(let a=0;a<3;a++)assert.ok(Math.abs(position(initial,i,a)-fixture[i][a])<.001);
        const bumperInst=initial.construction()!.instances(2)!;
        assert.equal(bumperInst.strength(),8);assert.equal(bumperInst.preload(),32);
        assert.ok(Math.abs(position(initial,2,1)-1)<.001);
        if(lesson===Lesson.Depth){
            assert.equal(position(initial,2,0),0);
            assert.ok(Math.abs(position(initial,2,2)-(route===Route.Paid?3.3:0))<.02);
        }else{
            assert.ok(Math.abs(position(initial,2,0)+3.3)<.001);assert.equal(position(initial,2,2),0);
            if(route===Route.Paid){
                for(let a=0;a<3;a++)assert.ok(Math.abs(position(initial,3,a)-[-1,4,0][a])<.001);
                const wallInst=initial.construction()!.instances(3)!;
                const dims=[wallInst.width(),wallInst.height(),wallInst.thickness()];
                for(let a=0;a<3;a++)assert.ok(Math.abs(dims[a]-[.4,6,1.5][a])<.001);
            }
        }
        await d.page.screenshot({path:join(evidence,lesson+'-'+route+'-authored.png')});
        await d.reload();await d.load();await d.save();assert.deepEqual(authoredState(await saved(d)),authoredState(initialBytes));
        const initialRead=reads.at(-1)!;
        await d.run();const samples=[];const start=Date.now();
        while(Date.now()-start<5200){const b=(await d.readLatestPose())?.bodies[0];if(b)samples.push(b);await d.page.waitForTimeout(40);}
        await d.pauseSimulation();const after=reads.at(-1)!;const solved=await d.readCaptured();
        const paid=events.filter(res=>{for(let i=0;i<res.occurrencesLength();i++){const occ=res.occurrences(i)!;if(occ.effect()===WorkEffect.Paid&&occ.debit()>0)return true;}return false;});
        const owner=initial.construction()!.instances(2)!.id();
        const rings=await d.readAnimationSamplesForTarget(String((2n<<32n)+owner));
        assert.equal(solved,route===Route.Paid?1:0);
        if(lesson===Lesson.Depth&&route===Route.Miss){
            assert.equal(paid.length,0);assert.equal(after.contactWorks(0)!.remainingEnergy(),32);
            assert.equal(after.contactWorks(0)!.occurrenceCount(),0);assert.ok(rings.every(r=>r.value===0));
        }else{
            assert.ok(paid.length>0);assert.ok(after.contactWorks(0)!.remainingEnergy()<32);
            const firstPaid=paid[0];
            const spin=Math.hypot(firstPaid.bodies(0)!.angularVelocity()!.x(),firstPaid.bodies(0)!.angularVelocity()!.y(),firstPaid.bodies(0)!.angularVelocity()!.z());
            assert.ok(spin>1,'Actual paid WASM contact retains friction-generated angular motion');
            assert.ok(rings.some(r=>r.value>.5));assert.equal(rings.at(-1)?.value,0);
        }
        if(route===Route.Paid&&lesson===Lesson.Depth)assert.ok(samples.some(b=>b.vz < -2&&b.vy>2));
        if(route===Route.Paid&&lesson===Lesson.Wall){
            assert.ok(samples.some(b=>b.vx>2),'Bumper launches toward Wall');
            const returning=samples.find(b=>b.vx<0);
            assert.ok(returning&&returning.px>-1.8&&returning.px<-1.5,
                'Direction reverses at the declared Wall face plus ball radius, then reaches Receiver');
        }
        await d.page.screenshot({path:join(evidence,lesson+'-'+route+'-outcome.png')});
        await d.reset();const reset=reads.at(-1)!;
        assert.equal(reset.contactWorks(0)!.remainingEnergy(),32);assert.equal(reset.contactWorks(0)!.occurrenceCount(),0);
        assert.equal(reset.bodiesLength(),initialRead.bodiesLength());
        for(let i=0;i<reset.bodiesLength();i++)assert.deepEqual(bodyState(reset.bodies(i)!),bodyState(initialRead.bodies(i)!),'Every committed live body pose and velocity is restored exactly');
        assert.equal(reset.captures(0)!.phase(),CapturePhase.Clear);
        assert.equal(reset.captures(0)!.eventOrdinal(),0,'Captured event cleared');
        assert.equal((await d.readAnimationSamplesForTarget(String((2n<<32n)+owner))).at(-1)?.value??0,0,
            'Bumper animation neutral after Reset');
        await d.page.screenshot({path:join(evidence,lesson+'-'+route+'-reset.png')});
        await d.save();assert.deepEqual(authoredState(await saved(d)),authoredState(initialBytes));assert.deepEqual(errors,[]);
        const solvedBeforeReplay=await d.readCaptured();
        await d.run();await d.page.waitForTimeout(5200);await d.pauseSimulation();
        assert.equal((await d.readCaptured())-solvedBeforeReplay,route===Route.Paid?1:0,'Repeat Run reproduces intended outcome');
        await d.reset();await d.save();assert.deepEqual(authoredState(await saved(d)),authoredState(initialBytes));assert.deepEqual(errors,[]);
        console.log(JSON.stringify({lesson,route,solved,paid:paid.length,remaining:after.contactWorksLength()>0?after.contactWorks(0)!.remainingEnergy():0,
            bumper:[0,1,2].map(a=>position(initial,2,a)),samples}));
    }catch(e){console.error(JSON.stringify({lesson,route,errors}));
        await d.page.screenshot({path:join(evidence,lesson+'-'+route+'-failure.png')});throw e;}
    finally{await d.close();}
});
