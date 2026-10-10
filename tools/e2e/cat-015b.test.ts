import assert from 'node:assert/strict';
import {test} from 'node:test';
import {mkdirSync} from 'node:fs';
import {resolve,sep,join} from 'node:path';
import {WorkshopDriver} from './workshop-driver.ts';

// Closed numeric sets; external level names are converted only at the driver boundary.
enum Lesson { Depth=5, Wall=6 }
enum Route { Paid, Miss }
enum WorkEffect { Passive, Paid }
enum CapturePhase { Clear, Latched }
const URL=process.env.BUMPER_PREVIEW_URL ?? 'http://127.0.0.1:8069/';
const WORK=3328,OCCURRENCE=3584,CAPTURES=2176;
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
function half(bits:number){const sign=bits&32768?-1:1,e=bits>>10&31,m=bits&1023;
    return sign*(e?2**(e-15)*(1+m/1024):2**-14*m/1024);}
const slot=(i:number)=>24+320+i*160;
function position(b:Buffer,i:number,a:number){const o=slot(i);
    return (b.readInt32LE(o+48+a*4)+half(b.readUInt16LE(o+72+a*2)))/16;}
const authored=(b:Buffer)=>Buffer.concat([b.subarray(0,24),b.subarray(32)]);
async function difficultyControls(d:WorkshopDriver){
    // The existing slider is a real UI control. Verify committed saved precision at each endpoint,
    // then restore the original Balanced45 profile before construction.
    for(const [x,value,margin,speed,dwell,guide] of [[1040,0,.3,3,.15,12],[1396,1,.02,1.5,.35,0],[1200,.45,.174,2.325,.24,6.6]]){
        await press(d,1394,46);await press(d,x,350);
        await d.page.keyboard.press('Escape',{delay:70});await d.save();
        const bytes=await saved(d);
        assert.ok(Math.abs(half(bytes.readUInt16LE(24+48+8))-value)<.001);
        for(const [offset,expected] of [[104,margin],[106,speed],[108,dwell],[132,guide]])
            assert.ok(Math.abs(half(bytes.readUInt16LE(slot(1)+offset))-expected)<.002,
                'Receiver setting at precision '+value+' field '+offset+' expected '+expected);
    }
}
for(const lesson of [Lesson.Depth,Lesson.Wall])for(const route of [Route.Paid,Route.Miss])
test('CAT015b lesson '+lesson+' route '+route+' real UI and exact lifecycle',async()=>{
    const d=await WorkshopDriver.launch({baseUrl:URL}),reads:Buffer[]=[],events:Buffer[]=[],errors:string[]=[];
    d.page.on('console',m=>{const t=m.text();
        if(t.startsWith('CCGPU '))reads.push(Buffer.from(t.slice(6),'base64'));
        if(t.startsWith('CCGPU_RETAINED '))events.push(Buffer.from(t.slice(15),'base64'));
        if(m.type()==='error'||t.includes('CCGPU_CLOCK_RECOVERY_FAULT'))errors.push(t);
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
        await d.page.keyboard.press('Escape',{delay:70});await d.save();const initial=await saved(d);
        assert.equal(initial.readUInt32LE(24+48),lesson);assert.equal(initial.readUInt32LE(36),0);
        const fixture=lesson===Lesson.Depth?[[0,5,3],[0,.9,-2]]:[[-3,5,0],[-5,.9,0]];
        for(let i=0;i<2;i++)for(let a=0;a<3;a++)assert.ok(Math.abs(position(initial,i,a)-fixture[i][a])<.001);
        assert.equal(initial.readFloatLE(slot(2)+104),8);assert.equal(initial.readFloatLE(slot(2)+112),32);
        assert.ok(Math.abs(position(initial,2,1)-1)<.001);
        if(lesson===Lesson.Depth){
            assert.equal(position(initial,2,0),0);
            assert.ok(Math.abs(position(initial,2,2)-(route===Route.Paid?3.3:0))<.02);
        }else{
            assert.ok(Math.abs(position(initial,2,0)+3.3)<.001);assert.equal(position(initial,2,2),0);
            if(route===Route.Paid){
                for(let a=0;a<3;a++)assert.ok(Math.abs(position(initial,3,a)-[-1,4,0][a])<.001);
                for(let a=0;a<3;a++)assert.ok(Math.abs(half(initial.readUInt16LE(slot(3)+104+a*2))-[.4,6,1.5][a])<.001);
            }
        }
        await d.page.screenshot({path:join(evidence,lesson+'-'+route+'-authored.png')});
        await d.reload();await d.load();await d.save();assert.deepEqual(authored(await saved(d)),authored(initial));
        const initialRead=reads.at(-1)!;
        await d.run();const samples=[];const start=Date.now();
        while(Date.now()-start<5200){const b=(await d.readLatestPose())?.bodies[0];if(b)samples.push(b);await d.page.waitForTimeout(40);}
        await d.pauseSimulation();const after=reads.at(-1)!;const solved=await d.readCaptured();
        const paid=events.filter(b=>b[OCCURRENCE+30]===WorkEffect.Paid&&b.readFloatLE(OCCURRENCE+26)>0);
        const owner=initial.readBigUInt64LE(slot(2)+40);
        const rings=await d.readAnimationSamplesForTarget(String((2n<<32n)+owner));
        assert.equal(solved,route===Route.Paid?1:0);
        if(lesson===Lesson.Depth&&route===Route.Miss){
            assert.equal(paid.length,0);assert.equal(after.readFloatLE(WORK+20),32);
            assert.equal(after.readUInt32LE(WORK+16),0);assert.ok(rings.every(r=>r.value===0));
        }else{
            assert.ok(paid.length>0);assert.ok(after.readFloatLE(WORK+20)<32);
            const firstPaid=paid[0];
            const spin=Math.hypot(firstPaid.readFloatLE(128+52),firstPaid.readFloatLE(128+56),firstPaid.readFloatLE(128+60));
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
        assert.equal(reset.readFloatLE(WORK+20),32);assert.equal(reset.readUInt32LE(WORK+16),0);
        assert.equal(reset[104],initialRead[104]);
        assert.deepEqual(reset.subarray(128,128+reset[104]*64),initialRead.subarray(128,128+initialRead[104]*64),
            'Every committed live body pose and velocity is restored exactly');
        assert.equal(reset.readUInt32LE(CAPTURES+8),CapturePhase.Clear);
        assert.equal(reset.readUInt32LE(CAPTURES+12),0,'Captured event cleared');
        assert.equal((await d.readAnimationSamplesForTarget(String((2n<<32n)+owner))).at(-1)?.value??0,0,
            'Bumper animation neutral after Reset');
        await d.page.screenshot({path:join(evidence,lesson+'-'+route+'-reset.png')});
        await d.save();assert.deepEqual(authored(await saved(d)),authored(initial));assert.deepEqual(errors,[]);
        const solvedBeforeReplay=await d.readCaptured();
        await d.run();await d.page.waitForTimeout(5200);await d.pauseSimulation();
        assert.equal((await d.readCaptured())-solvedBeforeReplay,route===Route.Paid?1:0,'Repeat Run reproduces intended outcome');
        await d.reset();await d.save();assert.deepEqual(authored(await saved(d)),authored(initial));assert.deepEqual(errors,[]);
        console.log(JSON.stringify({lesson,route,solved,paid:paid.length,remaining:after.readFloatLE(WORK+20),
            bumper:[0,1,2].map(a=>position(initial,2,a)),samples}));
    }catch(e){console.error(JSON.stringify({lesson,route,errors}));
        await d.page.screenshot({path:join(evidence,lesson+'-'+route+'-failure.png')});throw e;}
    finally{await d.close();}
});
