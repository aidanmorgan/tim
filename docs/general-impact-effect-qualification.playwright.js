async (page) => {
 const Probe=Object.freeze({Launch:0,Miss:1,Simultaneous:2,Rejected:3});
 const Outcome=Object.freeze({Completed:0,Rejected:1});
 const probeWire=new Map([["Launch",Probe.Launch],["Miss",Probe.Miss],["Simultaneous",Probe.Simultaneous],["Rejected",Probe.Rejected]]);
 const outcomeWire=new Map([["Completed",Outcome.Completed],["Rejected",Outcome.Rejected]]);
 const prefix="CCGENERALIMPACTEFFECT ";
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
   while(reports.length<4&&Date.now()<deadline) await tab.waitForTimeout(100);
   await tab.waitForTimeout(300);
   const near=(a,b,t=1e-6)=>{if(!Number.isFinite(a)||Math.abs(a-b)>t)throw Error("Unexpected numeric result "+a+" / "+b);};
   const seen=new Set();
   for(const report of reports){
     if(!probeWire.has(report.Probe)||!outcomeWire.has(report.Outcome))throw Error("Unknown wire enum");
     const probe=probeWire.get(report.Probe),outcome=outcomeWire.get(report.Outcome);
     if(seen.has(probe)||!report.ExactRestore||!report.ExactReplay)throw Error("Invalid replay or duplicate probe");
     seen.add(probe);
     if([...report.Position,...report.Velocity,...report.ContactTimes,...report.Counts].some(x=>!Number.isFinite(x)))throw Error("Nonfinite report");
     if(outcome!==(probe===Probe.Rejected?Outcome.Rejected:Outcome.Completed))throw Error("Wrong outcome");
     switch(probe){
       case Probe.Launch:
         near(report.Counts[0],1,0); near(report.Velocity[0],-20);
         near(report.ContactTimes[0],.149,2e-5); near(report.Position[0],-1.53,4e-4); break;
       case Probe.Miss:
         near(report.Counts[0],0,0); near(report.Position[0],-1); near(report.Velocity[0],10); break;
       case Probe.Simultaneous:
         if(report.Counts.length!==2)throw Error("Missing simultaneous effect");
         report.Counts.forEach(n=>near(n,1,0)); near(report.ContactTimes[0],report.ContactTimes[1],0);
         near(report.Velocity[0],-10); break;
       case Probe.Rejected:
         near(report.Counts[0],0,0); near(report.Position[0],-2); near(report.Velocity[0],10); break;
       default: throw Error("Unsupported probe");
     }
   }
   if(seen.size!==4||errors.length)throw Error(JSON.stringify({reports,errors}));
   return {reports,errors,scope:"Read-only shared-world effect timing and rollback qualification; not catalogue part UI proof."};
 }finally{await context.close();}
}
