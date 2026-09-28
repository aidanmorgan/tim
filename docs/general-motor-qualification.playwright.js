async (page) => {
 /** @enum {number} */
 const Probe=Object.freeze({SliderSupply:0,HingeSupply:1,BrakeReverse:2,Disabled:3,StopEvent:4});
 const wire=new Map([["SliderSupply",Probe.SliderSupply],["HingeSupply",Probe.HingeSupply],
   ["BrakeReverse",Probe.BrakeReverse],["Disabled",Probe.Disabled],["StopEvent",Probe.StopEvent]]);
 const prefix="CCGENERALMOTOR ";
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
   if(reports.length!==5||new Set(reports.map(r=>r.Probe)).size!==5) throw Error("Missing or duplicate motor reports: "+JSON.stringify(reports));
   const near=(a,b,t=1e-10)=>{if(!Number.isFinite(a)||Math.abs(a-b)>t) throw Error("Mismatch "+a+" / "+b);};
   for(const report of reports){
     const probe=wire.get(report.Probe),use=report.Use;
     if(probe===undefined||!report.ExactRestore||!report.ExactReplay||use.Joint.Index!==0||
        [report.Speed,report.Coordinate,report.Energy,use.AbsoluteImpulse,use.SuppliedWork,use.DissipatedWork,use.RemainingWork].some(n=>!Number.isFinite(n))||
        use.AbsoluteImpulse<0||use.SuppliedWork<0||use.DissipatedWork<0||use.RemainingWork<0)
       throw Error("Invalid motor report: "+JSON.stringify(report));
     switch(probe){
       case Probe.SliderSupply:
       case Probe.HingeSupply:
         near(report.Speed,probe===Probe.SliderSupply?1:Math.sqrt(10));
         near(report.Energy,.5); near(use.SuppliedWork,.5);
         if(use.SuppliedWork>.5||use.AbsoluteImpulse>1||report.Coordinate<=0||report.Events!==0) throw Error("Shared supply overdraw"); break;
       case Probe.BrakeReverse:
         near(report.Speed,-1); near(report.Energy,.5);
         near(use.AbsoluteImpulse,4); near(use.SuppliedWork,.5); near(use.DissipatedWork,4.5);
         if(use.SuppliedWork>.5) throw Error("Braking funded reverse acceleration"); break;
       case Probe.Disabled:
         near(report.Speed,2); near(report.Coordinate,.2); near(use.AbsoluteImpulse,0,0);
         near(use.SuppliedWork,0,0); near(use.DissipatedWork,0,0); near(use.RemainingWork,3,0); break;
       case Probe.StopEvent:
         near(report.Speed,0); near(report.Coordinate,.02,1e-7); near(report.Energy,0);
         near(use.AbsoluteImpulse,1); near(use.SuppliedWork,.5);
         if(report.Events!==1) throw Error("Missing stop event"); break;
       default: throw Error("Unsupported motor probe");
     }
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only engine motor budget qualification, not powered gameplay-part UI proof."};
 } finally {await context.close();}
}
