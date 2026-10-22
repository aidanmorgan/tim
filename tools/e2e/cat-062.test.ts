import assert from 'node:assert/strict';
import { test } from 'node:test';
import { mkdir } from 'node:fs/promises';
import { WorkshopDriver, type WorkshopBodyPose } from './workshop-driver.ts';
import { decodeSavedConstruction, authoredState, parameter } from './workshop-wire.ts';
import { decodeWorkshopResponse, type WorkshopResponse } from './workshop-wire.ts';

const SpringRoute = { Centred: 'Centred', Miss: 'Miss' } as const;
type SpringRoute = typeof SpringRoute[keyof typeof SpringRoute];
const evidence = process.env.SPRING_EVIDENCE_DIR ?? '.anvil/springboard-ui-' + Date.now();
async function saved(driver: WorkshopDriver): Promise<Buffer> {
    return Buffer.from(await driver.page.evaluate(() => new Promise<number[]>((resolve, reject) => {
        const request = indexedDB.open('curious-contraptions-workshop');
        request.onsuccess = () => {
            const db = request.result, tx = db.transaction('construction', 'readonly');
            const value = tx.objectStore('construction').get(1);
            tx.oncomplete = () => { resolve(Array.from(value.result as Uint8Array)); db.close(); };
            tx.onabort = () => reject(new Error('Save read aborted'));
        };
        request.onerror = () => reject(new Error('Save database unavailable'));
    })));
}
const authored = (bytes: Buffer) => authoredState(bytes);
async function construct(driver: WorkshopDriver, route: SpringRoute) {
    await driver.selectTool('basketball'); await driver.placeOnCanvas(720,485);
    await driver.clickAt(60,844); await driver.liftSelectedPart(90);
    await driver.clickAt(130,715); // actual Springboard palette row
    await driver.placeOnCanvas(route === SpringRoute.Centred ? 720 : 960,485);
    await driver.save();
    const bytes = await saved(driver);
    const fb = decodeSavedConstruction(bytes);
    const boardInst = fb.construction()!.instances(1)!;
    const ballInst = fb.construction()!.instances(0)!;
    assert.equal(parameter(boardInst, 'stiffness'), 400);
    assert.equal(parameter(boardInst, 'damping'), Math.fround(.2));
    assert.equal(boardInst.cell()!.y(), 48);
    assert.equal(ballInst.cell()!.y(), 73);
    return bytes;
}
async function observe(driver: WorkshopDriver, milliseconds: number, plateId = '4294968066') {
    const samples: { sequence: string; timestamp: string; ball: WorkshopBodyPose; plate: WorkshopBodyPose }[]=[];
    let previousSequence = -1n, previousTime = -1n;
    const started=Date.now();
    while(Date.now()-started<milliseconds){
        const slot=await driver.readLatestPose();
        const ball=slot?.bodies.find(body=>body.id==='1'),plate=slot?.bodies.find(body=>body.id===plateId);
        if(ball&&plate&&slot&&BigInt(slot.sequence)!==previousSequence){
            assert.ok(BigInt(slot.sequence)>previousSequence&&BigInt(slot.timestamp)>previousTime,'New publications advance both sequence and capture time');
            previousSequence=BigInt(slot.sequence);previousTime=BigInt(slot.timestamp);
            if(plateId!=='4294968066')assert.ok(!slot.bodies.some(body=>body.id==='4294968066'),'Retired plate never reappears');
            samples.push({sequence:slot.sequence,timestamp:slot.timestamp,ball,plate});
        }
        await driver.page.waitForTimeout(15);
    }
    return samples;
}
function assertDrop(samples: Awaited<ReturnType<typeof observe>>, route: SpringRoute) {
    assert.ok(samples.length>60,'More than60 distinct physical publications');
    assert.ok(BigInt(samples.at(-1)!.timestamp)-BigInt(samples[0].timestamp)>2_000_000_000n,'Committed capture time progresses over two seconds');
    const compressed=Math.min(...samples.map(sample=>sample.plate.py));
    if(route===SpringRoute.Miss){
        assert.ok(compressed>3.11,'unloaded board only sags under its own weight');
        assert.ok(Math.min(...samples.map(sample=>sample.ball.py))<1,'missed ball falls past board');
        return;
    }
    assert.ok(compressed<3.02&&compressed>=2.889,'real plate compression stays inside travel');
    const apexes:number[]=[];
    for(let i=1;i<samples.length;i++)if(samples[i-1].ball.vy>0&&samples[i].ball.vy<=0)apexes.push(Math.max(samples[i-1].ball.py,samples[i].ball.py));
    assert.ok(apexes.length>=2,JSON.stringify(apexes));
    assert.ok(apexes[0]<4.56&&apexes[1]<apexes[0]-.05,JSON.stringify(apexes));
}
for(const route of [SpringRoute.Centred,SpringRoute.Miss]){
    test('Springboard actual UI drop/lifecycle '+route,{timeout:120000},async()=>{
        await mkdir(evidence,{recursive:true});
        const driver=await WorkshopDriver.launch(),errors:string[]=[];
        driver.page.on('console',message=>{if(message.type()==='error')errors.push(message.text());});
        driver.page.on('pageerror',error=>errors.push(error.message));
        try{
            const construction=await construct(driver,route);
            await driver.page.screenshot({path:evidence+'/build-'+route+'.png'});
            await driver.reload();await driver.load();await driver.save();
            assert.deepEqual(authored(await saved(driver)),authored(construction),'Load restores exact board configuration and poses');
            await driver.run();const samples=await observe(driver,3200);await driver.pauseSimulation();
            assertDrop(samples,route);
            await driver.page.screenshot({path:evidence+'/paused-'+route+'.png'});
            await driver.reset();await driver.save();
            assert.deepEqual(authored(await saved(driver)),authored(construction),'Reset preserves exact construction');
            await driver.page.screenshot({path:evidence+'/reset-'+route+'.png'});
            await driver.run();const repeated=await observe(driver,3200);await driver.pauseSimulation();
            assertDrop(repeated,route);
            assert.deepEqual(errors,[]);
            console.log(JSON.stringify({route,samples,repeated}));
        }catch(error){await driver.page.screenshot({path:evidence+'/failure-'+route+'.png'});console.error(JSON.stringify({route,errors}));throw error;}
        finally{await driver.close();}
    });
}

test('Springboard actual Step shows compressed plate/coil and Reset restores rendered construction',{timeout:120000},async()=>{
    await mkdir(evidence,{recursive:true});
    const driver=await WorkshopDriver.launch();
    const reads:WorkshopResponse[]=[];driver.page.on('console',message=>{const text=message.text();if(text.startsWith('CCGPU '))reads.push(decodeWorkshopResponse(Buffer.from(text.slice(6),'base64')));});
    try{
        await construct(driver,SpringRoute.Centred);
        // Ordinary camera gesture lowers the view so moving coil strands remain visible beneath the plate.
        await driver.page.mouse.move(1100,450);await driver.page.mouse.down({button:'right'});
        await driver.page.mouse.move(1100,380,{steps:12});await driver.page.mouse.up({button:'right'});await driver.page.waitForTimeout(100);
        const before=await driver.page.screenshot({path:evidence+'/visual-rest.png'});
        await driver.run();await driver.pauseSimulation();
        const plateY=()=>{const read=reads.at(-1)!;const b=read.bodies(1)!;assert.equal(b.id(),4294968066n);return(b.cell()!.y()+b.local()!.y())/16;};
        let steps=0;while(plateY()>3.02&&steps<90){await driver.stepSimulation();steps++;}
        assert.ok(plateY()<3.02,'actual UI stepping reaches compressed plate');
        const compressed=await driver.page.screenshot({path:evidence+'/visual-compressed.png'});
        await driver.reset();await driver.save();
        const reset=await driver.page.screenshot({path:evidence+'/visual-reset.png'});
        const {createRequire}=await import('node:module');
        const requireMcp=createRequire('/opt/homebrew/lib/node_modules/@playwright/mcp/package.json');
        const {PNG}=requireMcp('./node_modules/playwright-core/lib/utilsBundle.js');
        const a=PNG.sync.read(before),b=PNG.sync.read(compressed),c=PNG.sync.read(reset);
        let changed=0,restored=0;
        for(let y=440;y<540;y++)for(let x=660;x<780;x++){
            const at=(y*a.width+x)*4;
            if([0,1,2].some(channel=>Math.abs(a.data[at+channel]-b.data[at+channel])>20))changed++;
            if([0,1,2].some(channel=>Math.abs(a.data[at+channel]-c.data[at+channel])>2))restored++;
        }
        assert.ok(changed>150,'visible plate/coil region changes under compression');
        assert.equal(restored,0,'Reset restores exact rendered plate/coil region');
        const silver=(png:typeof a,at:number)=>{const r=png.data[at],g=png.data[at+1],b=png.data[at+2];return r>100&&Math.abs(r-g)<24&&Math.abs(g-b)<24&&Math.abs(r-b)<28;};
        let restCoil=0,compressedCoil=0,newCoil=0;
        // This lower strand crop excludes the payload and both gold plate positions.
        for(let y=495;y<515;y++)for(let x=700;x<740;x++){
            const at=(y*a.width+x)*4,rest=silver(a,at),compressed=silver(b,at);
            if(rest)restCoil++;if(compressed)compressedCoil++;if(compressed&&!rest)newCoil++;
        }
        assert.ok(restCoil>12&&compressedCoil>3,'Silver coil strands are visibly present in both states');
        assert.ok(newCoil>3,'Compressed coil creates silver strand pixels at new positions; plate motion/occlusion alone cannot pass');
        console.log(JSON.stringify({steps,changed,restored,restCoil,compressedCoil,newCoil}));
    }catch(error){await driver.page.screenshot({path:evidence+'/visual-failure.png'});throw error;}
    finally{await driver.close();}
});
const SpringConfiguration = { SoftUndamped: 'SoftUndamped', StiffDamped: 'StiffDamped', Tilted: 'Tilted' } as const;
type SpringConfiguration = typeof SpringConfiguration[keyof typeof SpringConfiguration];
const configurations:Record<SpringConfiguration,{stiffness:number;damping:number;tilt:number}>={
    [SpringConfiguration.SoftUndamped]:{stiffness:120,damping:0,tilt:0},
    [SpringConfiguration.StiffDamped]:{stiffness:1200,damping:8,tilt:0},
    [SpringConfiguration.Tilted]:{stiffness:400,damping:.2,tilt:10}
};
for(const configuration of [SpringConfiguration.SoftUndamped,SpringConfiguration.StiffDamped,SpringConfiguration.Tilted]){
    test('Springboard authored configuration '+configuration,{timeout:120000},async()=>{
        await mkdir(evidence,{recursive:true});
        const driver=await WorkshopDriver.launch(),errors:string[]=[];
        driver.page.on('console',message=>{if(message.type()==='error')errors.push(message.text());});
        driver.page.on('pageerror',error=>errors.push(error.message));
        try{
            await construct(driver,SpringRoute.Centred);
            const settings=configurations[configuration];
            await driver.selectPartAt(720,485);
            if(settings.tilt!==0)await driver.tiltSelectedByFineRotate(settings.tilt/5);
            else{
                await driver.clickAt(130,790);
                for(const [y,value] of [[622,settings.stiffness],[662,settings.damping]]){
                    await driver.clickAt(100,y);await driver.page.keyboard.press('Meta+A');
                    await driver.page.keyboard.type(String(value),{delay:40});await driver.page.keyboard.press('Enter');
                }
                await driver.clickAt(130,744);
            }
            await driver.save();const construction=await saved(driver);
            const constFb=decodeSavedConstruction(construction);
            const boardInst=constFb.construction()!.instances(1)!;
            assert.equal(parameter(boardInst,'stiffness'),Math.fround(settings.stiffness));
            assert.equal(parameter(boardInst,'damping'),Math.fround(settings.damping));
            await driver.page.screenshot({path:evidence+'/configuration-'+configuration+'.png'});
            await driver.reload();await driver.load();await driver.save();
            assert.deepEqual(authored(await saved(driver)),authored(construction));
            await driver.run();const samples=await observe(driver,2200);await driver.pauseSimulation();
            assert.ok(samples.length>60);
            const angle=settings.tilt*Math.PI/180,axis=[-Math.sin(angle),Math.cos(angle)];
            let minTravel=0;
            for(const sample of samples){
                const d=[sample.plate.px,sample.plate.py-3];
                const travel=d[0]*axis[0]+d[1]*axis[1]-.14;minTravel=Math.min(minTravel,travel);
                assert.ok(travel>=-.251&&travel<=.001,JSON.stringify({configuration,travel}));
                assert.ok(Math.abs(d[0]*axis[1]-d[1]*axis[0])<.002,'plate remains on authored local-Y slider');
                assert.ok(Math.abs(sample.plate.qz-Math.sin(angle/2))<.002,'plate rotation remains locked to tilted base');
            }
            assert.ok(minTravel<-.02,'payload actually loads the configured spring');
            if(configuration===SpringConfiguration.Tilted)assert.ok(samples.some(sample=>sample.ball.vx<-.2),'tilted physical normal redirects payload sideways');
            await driver.reset();await driver.save();
            assert.deepEqual(authored(await saved(driver)),authored(construction));
            assert.deepEqual(errors,[]);
            console.log(JSON.stringify({configuration,minTravel,samples}));
        }catch(error){await driver.page.screenshot({path:evidence+'/configuration-failure-'+configuration+'.png'});console.error(JSON.stringify({configuration,errors}));throw error;}
        finally{await driver.close();}
    });
}
test('Springboard text settings reject atomically and delete/recreate retires the old plate',{timeout:120000},async()=>{
    await mkdir(evidence,{recursive:true});
    const driver=await WorkshopDriver.launch(),errors:string[]=[];
    driver.page.on('console',message=>{if(message.type()==='error')errors.push(message.text());});
    driver.page.on('pageerror',error=>errors.push(error.message));
    let editAttempt=0;
    const edit=async(k:string,c:string,compression:string)=>{
        await driver.selectPartAt(720,485);await driver.clickAt(130,790);
        for(const [y,text] of [[622,k],[662,c],[702,compression]] as const){
            await driver.clickAt(100,y);await driver.page.keyboard.press('Meta+A');
            await driver.page.keyboard.type(text,{delay:30});
        }
        await driver.clickAt(130,744);await driver.page.waitForTimeout(250);
        await driver.page.screenshot({path:evidence+'/settings-feedback-'+editAttempt++ +'.png'});
        await driver.save();
    };
    try{
        await construct(driver,SpringRoute.Centred);
        await edit('400.00003','0.20000002','0.1234567');
        const precise=await saved(driver);
        const preciseFb=decodeSavedConstruction(precise);
        const preciseBoard=preciseFb.construction()!.instances(1)!;
        assert.equal(parameter(preciseBoard,'stiffness'),Math.fround(400.00003));assert.equal(parameter(preciseBoard,'damping'),Math.fround(.20000002));
        assert.equal(parameter(preciseBoard,'initial_compression'),Math.fround(.1234567));
        for(const [k,c,compression] of [['NaN','.2','.1'],['1201','.2','.1'],['400','-.1','.1'],['invalid','.2','.1'],['400','.2','NaN'],['400','.2','-.01'],['400','.2','.200001'],['400','.2','Infinity']]){
            await edit(k,c,compression);assert.deepEqual(authored(await saved(driver)),authored(precise),'invalid text/bounds preserve all construction bytes');
        }
        await edit('400.00003','0.20000002','0.2'); // Valid retry through the same editor after rejected input.
        assert.equal(parameter(decodeSavedConstruction(await saved(driver)).construction()!.instances(1)!,'initial_compression'),Math.fround(.2));
        await driver.clickAt(744,836);await driver.save();
        assert.deepEqual(authored(await saved(driver)),authored(precise),'Undo returns to the preceding accepted settings');
        await driver.clickAt(744,836);await driver.save(); // Rejected edits never entered the undo history.
        const undone=await saved(driver);
        const undoneFb=decodeSavedConstruction(undone);
        const undoneBoard=undoneFb.construction()!.instances(1)!;
        assert.equal(parameter(undoneBoard,'stiffness'),400);assert.equal(parameter(undoneBoard,'damping'),Math.fround(.2));assert.equal(parameter(undoneBoard,'initial_compression'),0);
        await driver.selectPartAt(720,485);await driver.clickAt(148,842);await driver.save();
        assert.equal(decodeSavedConstruction(await saved(driver)).construction()!.instancesLength(),1,'delete removes authored board');
        await driver.run();await driver.page.waitForTimeout(200);
        assert.deepEqual((await driver.readLatestPose())!.bodies.map(body=>body.id),['1'],'deleted plate is absent from active population');
        await driver.reset();
        await driver.clickAt(130,715);await driver.placeOnCanvas(720,485);await driver.save();
        const replacement=await saved(driver);
        const repFb=decodeSavedConstruction(replacement);
        assert.equal(repFb.construction()!.instancesLength(),2);assert.equal(repFb.construction()!.instances(1)!.id(),3n,'replacement has a fresh owner');
        await driver.reload();await driver.load();await driver.run();
        const samples=await observe(driver,3200,'4294968322');
        await driver.pauseSimulation();assertDrop(samples,SpringRoute.Centred);
        await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(replacement));
        assert.deepEqual(errors,[]);await driver.page.screenshot({path:evidence+'/replacement-reset.png'});
        console.log(JSON.stringify({replacementId:'3',samples}));
    }catch(error){await driver.page.screenshot({path:evidence+'/replacement-failure.png'});console.error(JSON.stringify({errors}));throw error;}
    finally{await driver.close();}
});
