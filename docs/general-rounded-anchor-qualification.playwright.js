async (page) => {
 const Probe=Object.freeze({ThinWall:"ThinWall",RepeatedBounce:"RepeatedBounce",RestingBox:"RestingBox",RotatingBeam:"RotatingBeam"});
 const prefix="CCGENERALWORLD ";
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
   while(reports.length<4 && Date.now()<deadline) await tab.waitForTimeout(100);
   await tab.waitForTimeout(300);
   if(reports.length!==4||new Set(reports.map(r=>r.Probe)).size!==4) throw Error("Missing or duplicate world reports: "+JSON.stringify(reports));
   const near=(a,b,t=1e-6)=>{if(Math.abs(a-b)>t) throw Error("Mismatch "+a+" / "+b);};
   for(const report of reports){
     if(!Object.values(Probe).includes(report.Probe)||!report.ExactRestore||!report.ExactReplay||
        [...report.Position,...report.Velocity,report.Time,report.Spin].some(n=>!Number.isFinite(n)))
       throw Error("Invalid world report: "+JSON.stringify(report));
     switch(report.Probe){
       case Probe.ThinWall:
         near(report.Events,1,0); near(report.Velocity[0],-1000); near(report.Position[0],-6.0202,1e-5); near(report.Spin,0,1e-8); near(report.Time,.01); break;
       case Probe.RepeatedBounce:
         near(report.Events,3,0); near(report.Velocity[0],-10); near(report.Spin,0,1e-8); near(report.Time,.8); break;
       case Probe.RestingBox:
         near(report.Steps,600,0); near(report.Events,0,0); near(report.Time,5);
         near(report.Position[1],.5); near(Math.hypot(...report.Velocity),0,1e-7); near(report.Spin,0,1e-7); break;
       case Probe.RotatingBeam:
         if(report.Events<1||Math.hypot(...report.Velocity)<=1) throw Error("Rotating beam did not transfer momentum");
         near(report.Time,.01); near(report.Spin,0,1e-8); break;
       default: throw Error("Unsupported world probe");
     }
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only integrated physics-world qualification, not gameplay-part UI proof."};
 } finally {await context.close();}
}
