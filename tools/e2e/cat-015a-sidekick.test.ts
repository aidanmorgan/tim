import assert from 'node:assert/strict';
import { test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';
import { decodeWorkshopResponse, decodeSavedConstruction, authoredState, parameter, contactHits, type WorkshopResponse } from './workshop-wire.ts';

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

for(const scenario of ['paid','unpaid','miss'] as Scenario[])test('Sidekick '+scenario+' route uses actual UI and restores authored state',async()=>{
    const driver=await WorkshopDriver.launch(),reads:WorkshopResponse[]=[],events:WorkshopResponse[]=[],errors:string[]=[];
    driver.page.on('console',message=>{const text=message.text();
        if(text.startsWith('CCGPU '))reads.push(decodeWorkshopResponse(Buffer.from(text.slice(6),'base64')));
        if(text.startsWith('CCGPU_RETAINED '))events.push(decodeWorkshopResponse(Buffer.from(text.slice(15),'base64')));
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
        const initialBytes=await saved(driver);
        const initialFb=decodeSavedConstruction(initialBytes);
        const construction=initialFb.construction()!;
        const bumper=construction.instances(2)!;
        const owner=bumper.id();
        assert.equal(construction.puzzle()!.id(),4,'Canonical named Sidekick puzzle');
        assert.equal(construction.connectionsLength(),0,'Sidekick has no energy supplier');
        const pos0=(bumper.cell()!.x()+bumper.local()!.x())/16;
        const pos1=(bumper.cell()!.y()+bumper.local()!.y())/16;
        const pos2=(bumper.cell()!.z()+bumper.local()!.z())/16;
        if(scenario==='miss') assert.ok(pos0<-5 && pos0>-5.6,'Gizmo moves the sphere clear of the falling ball');
        else assert.ok(Math.abs(pos0+3.3)<.001);
        assert.ok(Math.abs(pos1-1)<.001);assert.equal(pos2,0);
        assert.equal(parameter(bumper,'strength'),scenario==='unpaid'?0:8);
        await driver.reload();await driver.load();await driver.save();assert.deepEqual(authoredState(await saved(driver)),authoredState(initialBytes));
        await driver.run();const samples:{x:number,y:number,z:number,vx:number,vy:number}[]=[];
        const started=Date.now();while(Date.now()-started<4300){
            const body=(await driver.readLatestPose())?.bodies[0];
            if(body)samples.push({x:body.px,y:body.py,z:body.pz,vx:body.vx,vy:body.vy});
            await driver.page.waitForTimeout(35);
        }
        await driver.pauseSimulation();const after=reads.at(-1)!;
        const rings=await driver.readAnimationSamplesForTarget(String((2n<<32n)+owner));
        // Story 9A.23 D9: hits and their debits from the owner's stores (the occurrence rows are device-private).
        const preload=scenario==='unpaid'?0:32;
        const powered=contactHits(events,preload).filter(hit=>hit.debit>0);
        const lastHit=contactHits(reads,preload).at(-1);
        if(scenario==='paid'){
            assert.equal(await driver.readCaptured(),1);
            assert.ok(powered.length>0 && after.contactWorks(0)!.remainingEnergy()<32);
            assert.ok(samples.some(body=>body.vx>2 && body.vy>2),'Radial powered rebound observed');
            assert.ok(rings.some(value=>value.value>.5));assert.equal(rings.at(-1)?.value,0);
        }else{
            assert.equal(await driver.readCaptured(),0);assert.equal(powered.length,0);
            assert.ok(rings.every(value=>value.value===0),'Rejected/unpaid contact has no powered ring');
            if(scenario==='unpaid'){
                assert.ok(after.contactWorks(0)!.occurrenceCount()>0,'Ordinary contact occurred');
                assert.ok(lastHit,'Ordinary contact is a hit');assert.equal(lastHit.debit,0,'Ordinary contact is passive');
                assert.ok(samples.some(body=>body.vy>0),'Ordinary bounce remains');
            }else assert.equal(after.contactWorks(0)!.occurrenceCount(),0);
        }
        const solved=await driver.readCaptured();
        await driver.reset();const reset=reads.at(-1)!;
        assert.equal(reset.contactWorks(0)!.remainingEnergy(),scenario==='unpaid'?0:32);assert.equal(reset.contactWorks(0)!.occurrenceCount(),0);
        await driver.save();assert.deepEqual(authoredState(await saved(driver)),authoredState(initialBytes));assert.deepEqual(errors,[]);
        console.log(JSON.stringify({scenario,solved,debit:lastHit?.debit??0,remaining:after.contactWorksLength()>0?after.contactWorks(0)!.remainingEnergy():0,samples}));
    }catch(error){console.error(JSON.stringify({scenario,errors}));await driver.page.screenshot({path:'/tmp/sidekick-'+scenario+'-failure.png'});throw error;}
    finally{await driver.close();}
});
