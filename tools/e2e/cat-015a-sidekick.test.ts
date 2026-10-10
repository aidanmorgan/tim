import assert from 'node:assert/strict';
import { test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

const URL=process.env.BUMPER_PREVIEW_URL ?? 'http://127.0.0.1:8069/';
const WORK=3328, OCCURRENCE=3584;
type Scenario='paid'|'unpaid'|'miss';
async function press(driver:WorkshopDriver,x:number,y:number){
    await driver.page.mouse.move(x,y);await driver.page.mouse.down();await driver.page.waitForTimeout(70);
    await driver.page.mouse.up();await driver.page.waitForTimeout(180);
}
async function saved(driver:WorkshopDriver):Promise<Buffer>{
    return Buffer.from(await driver.page.evaluate(()=>new Promise<number[]>((resolve,reject)=>{
        const request=indexedDB.open('curious-contraptions-workshop');
        request.onupgradeneeded=()=>request.transaction?.abort();
        request.onerror=()=>reject(new Error('Existing save unavailable'));
        request.onsuccess=()=>{const db=request.result;try{
            const tx=db.transaction('construction','readonly'),read=tx.objectStore('construction').get(1);
            tx.oncomplete=()=>{db.close();resolve(Array.from(read.result as Uint8Array));};
            tx.onabort=()=>{db.close();reject(new Error('Save read failed'));};
        }catch(error){db.close();reject(error);}};
    })));
}
const authored=(bytes:Buffer)=>Buffer.concat([bytes.subarray(0,24),bytes.subarray(32)]);
function half(bits:number){const sign=bits&32768?-1:1,exponent=(bits>>10)&31,mantissa=bits&1023;
    return sign*(exponent?Math.pow(2,exponent-15)*(1+mantissa/1024):Math.pow(2,-14)*mantissa/1024);}
function position(bytes:Buffer,slot:number,axis:number){return (bytes.readInt32LE(slot+48+axis*4)+half(bytes.readUInt16LE(slot+72+axis*2)))/16;}

for(const scenario of ['paid','unpaid','miss'] as Scenario[])test('Sidekick '+scenario+' route uses actual UI and restores authored state',async()=>{
    const driver=await WorkshopDriver.launch({baseUrl:URL}),reads:Buffer[]=[],events:Buffer[]=[],errors:string[]=[];
    driver.page.on('console',message=>{const text=message.text();
        if(text.startsWith('CCGPU '))reads.push(Buffer.from(text.slice(6),'base64'));
        if(text.startsWith('CCGPU_RETAINED '))events.push(Buffer.from(text.slice(15),'base64'));
        if(message.type()==='error' || /CCGPU_TRANSPORT_FAILURE|CCGPU_STARTUP_EXCEPTION|Unhandled exception/.test(text))errors.push(text);
    });driver.page.on('pageerror',error=>errors.push(error.message));
    try{
        await driver.selectLevel('bumper_sidekick');await driver.selectTool('bumper');await driver.placeOnCanvas(505,620);
        await driver.selectPartAt(505,620);
        if(scenario==='unpaid'){
            await press(driver,130,232);await press(driver,100,230);
            await driver.page.keyboard.press('Meta+A',{delay:70});await driver.page.keyboard.type('0',{delay:80});
            await driver.page.keyboard.press('Enter',{delay:70});await press(driver,130,272);
        }else if(scenario==='miss'){
            await press(driver,60,285);
            await driver.page.mouse.move(585,620);await driver.page.mouse.down();await driver.page.waitForTimeout(70);
            await driver.page.mouse.move(455,620,{steps:12});await driver.page.mouse.up();await driver.page.waitForTimeout(250);
        }
        await driver.page.keyboard.press('Escape');await driver.save();
        const initial=await saved(driver),slot=24+320+2*160,owner=initial.readBigUInt64LE(slot+40);
        assert.equal(initial.readUInt32LE(24+48),4,'Canonical named Sidekick puzzle');
        assert.equal(initial.readUInt32LE(36),0,'Sidekick has no energy supplier');
        if(scenario==='miss') assert.ok(position(initial,slot,0)<-5 && position(initial,slot,0)>-5.6,'Gizmo moves the sphere clear of the falling ball');
        else assert.ok(Math.abs(position(initial,slot,0)+3.3)<.001);
        assert.ok(Math.abs(position(initial,slot,1)-1)<.001);assert.equal(position(initial,slot,2),0);
        assert.equal(initial.readFloatLE(slot+104),scenario==='unpaid'?0:8);
        assert.equal(initial.readFloatLE(slot+112),scenario==='unpaid'?0:32);
        await driver.reload();await driver.load();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));
        await driver.run();const samples:{x:number,y:number,z:number,vx:number,vy:number}[]=[];
        const started=Date.now();while(Date.now()-started<4300){
            const body=(await driver.readLatestPose())?.bodies[0];
            if(body)samples.push({x:body.px,y:body.py,z:body.pz,vx:body.vx,vy:body.vy});
            await driver.page.waitForTimeout(35);
        }
        await driver.pauseSimulation();const after=reads.at(-1)!;
        const rings=await driver.readAnimationSamplesForTarget(String((2n<<32n)+owner));
        const powered=events.filter(read=>read[OCCURRENCE+30]===1 && read.readFloatLE(OCCURRENCE+26)>0);
        if(scenario==='paid'){
            assert.equal(await driver.readCaptured(),1);
            assert.ok(powered.length>0 && after.readFloatLE(WORK+20)<32);
            assert.ok(samples.some(body=>body.vx>2 && body.vy>2),'Radial powered rebound observed');
            assert.ok(rings.some(value=>value.value>.5));assert.equal(rings.at(-1)?.value,0);
        }else{
            assert.equal(await driver.readCaptured(),0);assert.equal(powered.length,0);
            assert.ok(rings.every(value=>value.value===0),'Rejected/unpaid contact has no powered ring');
            if(scenario==='unpaid'){
                assert.ok(after.readUInt32LE(WORK+16)>0,'Ordinary contact occurred');
                assert.equal(after.readFloatLE(OCCURRENCE+26),0);assert.equal(after[OCCURRENCE+30],0);
                assert.ok(samples.some(body=>body.vy>0),'Ordinary bounce remains');
            }else assert.equal(after.readUInt32LE(WORK+16),0);
        }
        const solved=await driver.readCaptured();
        await driver.reset();const reset=reads.at(-1)!;
        assert.equal(reset.readFloatLE(WORK+20),scenario==='unpaid'?0:32);assert.equal(reset.readUInt32LE(WORK+16),0);
        await driver.save();assert.deepEqual(authored(await saved(driver)),authored(initial));assert.deepEqual(errors,[]);
        console.log(JSON.stringify({scenario,solved,debit:after.readFloatLE(OCCURRENCE+26),remaining:after.readFloatLE(WORK+20),samples}));
    }catch(error){console.error(JSON.stringify({scenario,errors}));await driver.page.screenshot({path:'/tmp/sidekick-'+scenario+'-failure.png'});throw error;}
    finally{await driver.close();}
});
