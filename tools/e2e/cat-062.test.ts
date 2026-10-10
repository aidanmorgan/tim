import assert from 'node:assert/strict';
import { test } from 'node:test';
import { mkdir } from 'node:fs/promises';
import { WorkshopDriver, type WorkshopBodyPose } from './workshop-driver.ts';

enum SpringRoute { Centred, Miss }
const preview = process.env.SPRING_PREVIEW_URL ?? 'http://127.0.0.1:8074/';
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
function authored(bytes: Buffer): Buffer { return Buffer.concat([bytes.subarray(0,24), bytes.subarray(32)]); }
async function construct(driver: WorkshopDriver, route: SpringRoute) {
    await driver.selectTool('basketball'); await driver.placeOnCanvas(720,485);
    await driver.clickAt(60,809); await driver.liftSelectedPart(90);
    await driver.clickAt(130,715); // actual Springboard palette row
    await driver.placeOnCanvas(route === SpringRoute.Centred ? 720 : 960,485);
    await driver.save();
    const bytes = await saved(driver), board=504;
    assert.equal(bytes.readFloatLE(board+104),400);
    assert.equal(bytes.readFloatLE(board+108),Math.fround(.2));
    assert.equal(bytes.readInt32LE(board+52),48);
    assert.equal(bytes.readInt32LE(344+52),73);
    return bytes;
}
async function observe(driver: WorkshopDriver, milliseconds: number, plateId = '4294967490') {
    const samples: { time: number; sequence: string; timestamp: string; ball: WorkshopBodyPose; plate: WorkshopBodyPose }[]=[];
    let previousSequence = -1n, previousTime = -1n;
    const started=Date.now();
    while(Date.now()-started<milliseconds){
        const slot=await driver.readLatestPose();
        const ball=slot?.bodies.find(body=>body.id==='1'),plate=slot?.bodies.find(body=>body.id===plateId);
        if(ball&&plate&&slot&&BigInt(slot.sequence)!==previousSequence){
            assert.ok(BigInt(slot.sequence)>previousSequence&&BigInt(slot.timestamp)>previousTime,'New publications advance both sequence and capture time');
            previousSequence=BigInt(slot.sequence);previousTime=BigInt(slot.timestamp);
            if(plateId!=='4294967490')assert.ok(!slot.bodies.some(body=>body.id==='4294967490'),'Retired plate never reappears');
            samples.push({time:Date.now()-started,sequence:slot.sequence,timestamp:slot.timestamp,ball,plate});
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
        const driver=await WorkshopDriver.launch({baseUrl:preview}),errors:string[]=[];
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
    const driver=await WorkshopDriver.launch({baseUrl:preview});
    const reads:Buffer[]=[];driver.page.on('console',message=>{const text=message.text();if(text.startsWith('CCGPU '))reads.push(Buffer.from(text.slice(6),'base64'));});
    try{
        await construct(driver,SpringRoute.Centred);
        // Ordinary camera gesture lowers the view so moving coil strands remain visible beneath the plate.
        await driver.page.mouse.move(1100,450);await driver.page.mouse.down({button:'right'});
        await driver.page.mouse.move(1100,380,{steps:12});await driver.page.mouse.up({button:'right'});await driver.page.waitForTimeout(100);
        const before=await driver.page.screenshot({path:evidence+'/visual-rest.png'});
        await driver.run();await driver.pauseSimulation();
        const half=(bits:number)=>{const sign=bits&0x8000?-1:1,e=(bits>>10)&31,m=bits&1023;return sign*(e===0?m*2**-24:(1+m/1024)*2**(e-15));};
        const plateY=()=>{const read=reads.at(-1)!;assert.equal(read.readBigUInt64LE(192),4294967490n);return(read.readInt32LE(204)+half(read.readUInt16LE(214)))/16;};
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
enum SpringConfiguration { SoftUndamped, StiffDamped, Tilted }
const configurations:Record<SpringConfiguration,{stiffness:number;damping:number;tilt:number}>={
    [SpringConfiguration.SoftUndamped]:{stiffness:120,damping:0,tilt:0},
    [SpringConfiguration.StiffDamped]:{stiffness:1200,damping:8,tilt:0},
    [SpringConfiguration.Tilted]:{stiffness:400,damping:.2,tilt:10}
};
for(const configuration of [SpringConfiguration.SoftUndamped,SpringConfiguration.StiffDamped,SpringConfiguration.Tilted]){
    test('Springboard authored configuration '+configuration,{timeout:120000},async()=>{
        await mkdir(evidence,{recursive:true});
        const driver=await WorkshopDriver.launch({baseUrl:preview}),errors:string[]=[];
        driver.page.on('console',message=>{if(message.type()==='error')errors.push(message.text());});
        driver.page.on('pageerror',error=>errors.push(error.message));
        try{
            await construct(driver,SpringRoute.Centred);
            const settings=configurations[configuration];
            await driver.selectPartAt(720,485);
            if(settings.tilt!==0)await driver.tiltSelectedByFineRotate(settings.tilt/5);
            else{
                await driver.clickAt(130,790);
                for(const [y,value] of [[662,settings.stiffness],[702,settings.damping]]){
                    await driver.clickAt(100,y);await driver.page.keyboard.press('Meta+A');
                    await driver.page.keyboard.type(String(value),{delay:40});await driver.page.keyboard.press('Enter');
                }
                await driver.clickAt(130,744);
            }
            await driver.save();const construction=await saved(driver);
            assert.equal(construction.readFloatLE(608),Math.fround(settings.stiffness));
            assert.equal(construction.readFloatLE(612),Math.fround(settings.damping));
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
    const driver=await WorkshopDriver.launch({baseUrl:preview}),errors:string[]=[];
    driver.page.on('console',message=>{if(message.type()==='error')errors.push(message.text());});
    driver.page.on('pageerror',error=>errors.push(error.message));
    const edit=async(k:string,c:string)=>{
        await driver.selectPartAt(720,485);await driver.clickAt(130,790);
        for(const [y,text] of [[662,k],[702,c]] as const){
            await driver.clickAt(100,y);await driver.page.keyboard.press('Meta+A');
            await driver.page.keyboard.type(text,{delay:30});
        }
        await driver.clickAt(130,744);await driver.save();
    };
    try{
        await construct(driver,SpringRoute.Centred);
        await edit('400.00003','0.20000002');
        const precise=await saved(driver);
        assert.equal(precise.readFloatLE(608),Math.fround(400.00003));assert.equal(precise.readFloatLE(612),Math.fround(.20000002));
        for(const [k,c] of [['NaN','.2'],['1201','.2'],['400','-.1'],['invalid','.2']]){
            await edit(k,c);assert.deepEqual(authored(await saved(driver)),authored(precise),'invalid text/bounds preserve all construction bytes');
        }
        await driver.clickAt(744,836);await driver.save(); // actual Undo must undo the last valid settings change only
        const undone=await saved(driver);assert.equal(undone.readFloatLE(608),400);assert.equal(undone.readFloatLE(612),Math.fround(.2));
        await driver.selectPartAt(720,485);await driver.clickAt(148,842);await driver.save();
        assert.equal((await saved(driver)).readUInt32LE(32),1,'delete removes authored board');
        await driver.run();await driver.page.waitForTimeout(200);
        assert.deepEqual((await driver.readLatestPose())!.bodies.map(body=>body.id),['1'],'deleted plate is absent from active population');
        await driver.reset();
        await driver.clickAt(130,715);await driver.placeOnCanvas(720,485);await driver.save();
        const replacement=await saved(driver);
        assert.equal(replacement.readUInt32LE(32),2);assert.equal(replacement.readBigUInt64LE(544),3n,'replacement has a fresh owner');
        await driver.reload();await driver.load();await driver.run();
        const samples=await observe(driver,3200,'4294967554');
        await driver.pauseSimulation();assertDrop(samples,SpringRoute.Centred);
        await driver.reset();await driver.save();assert.deepEqual(authored(await saved(driver)),authored(replacement));
        assert.deepEqual(errors,[]);await driver.page.screenshot({path:evidence+'/replacement-reset.png'});
        console.log(JSON.stringify({replacementId:'3',samples}));
    }catch(error){await driver.page.screenshot({path:evidence+'/replacement-failure.png'});console.error(JSON.stringify({errors}));throw error;}
    finally{await driver.close();}
});
