async (page) => {
 /** @enum {number} */
 const Probe=Object.freeze({HingeStop:0,SliderStop:1,RopeStop:2,RotatingSlider:3,RotatingRope:4});
 // Validated diagnostic JSON boundary; internal decisions use numeric enum values.
 const probeWire=new Map([["HingeStop",Probe.HingeStop],["SliderStop",Probe.SliderStop],
   ["RopeStop",Probe.RopeStop],["RotatingSlider",Probe.RotatingSlider],["RotatingRope",Probe.RotatingRope]]);
 const prefix="CCGENERALJOINTBOUNDARY ";
 const context=await page.context().browser().newContext();
 const tab=await context.newPage(),reports=[],errors=[];
 tab.on("console",message=>{
   const value=message.text();
   if(value.startsWith(prefix)) reports.push(JSON.parse(value.slice(prefix.length)));
   if(message.type()==="error") errors.push(value);
 });
 tab.on("pageerror",error=>errors.push(error.message));
 try {
   await tab.goto("http://127.0.0.1:8060");
   const deadline=Date.now()+30000;
   while(reports.length<5&&Date.now()<deadline) await tab.waitForTimeout(100);
   await tab.waitForTimeout(300);
   if(reports.length!==5||new Set(reports.map(r=>r.Probe)).size!==5) throw Error("Missing or duplicate boundary reports: "+JSON.stringify(reports));
   const near=(a,b,t)=>{if(!Number.isFinite(a)||Math.abs(a-b)>t) throw Error("Mismatch "+a+" / "+b);};
   for(const report of reports){
     const probe=probeWire.get(report.Probe);
     if(probe===undefined||!report.ExactRestore||!report.ExactReplay||report.Events!==1)
       throw Error("Invalid boundary report: "+JSON.stringify(report));
     switch(probe){
       case Probe.HingeStop:
       case Probe.SliderStop:
       case Probe.RopeStop:
         near(report.InitialSpeed,100,1e-10);
         near(report.AfterInitialSolveSpeed,100,1e-10);
         near(report.FinalSpeed,0,1e-8);
         near(report.HitTime,.005,1e-9);
         near(report.Coordinate,probe===Probe.RopeStop?1:.5,1e-7); break;
       case Probe.RotatingSlider:
         near(report.HitTime*120,Math.PI/6,1e-7);
         near(report.Coordinate,.5,1e-7);
         if(report.AfterInitialSolveSpeed!==null) throw Error("Sweep-only fixture claimed a solve"); break;
       case Probe.RotatingRope:
         near(report.HitTime*120,Math.PI/3,1e-7);
         near(report.Coordinate,1,1e-7);
         if(report.AfterInitialSolveSpeed!==null) throw Error("Sweep-only fixture claimed a solve"); break;
       default: throw Error("Unsupported boundary probe");
     }
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only engine event-timing qualification, not catalogue part UI proof."};
 } finally {await context.close();}
}
