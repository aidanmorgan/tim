async (page) => {
 const Probe=Object.freeze({Translation:"Translation",Simultaneous:"Simultaneous",FullTurn:"FullTurn",BlockedJoint:"BlockedJoint",ClearJoint:"ClearJoint"});
 const Outcome=Object.freeze({Applied:"Applied",Clipped:"Clipped",Rejected:"Rejected"});
 const prefix="CCGENERALPROJECTION ";
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
   if(reports.length!==5||new Set(reports.map(r=>r.Probe)).size!==5) throw Error("Missing or duplicate projection reports: "+JSON.stringify(reports));
   const near=(a,b,t=1e-7)=>{if(Math.abs(a-b)>t) throw Error("Mismatch "+a+" / "+b);};
   for(const report of reports){
     if(!Object.values(Probe).includes(report.Probe)||!report.ExpectedVelocityState||!report.ExactRestore||!report.ExactReplay||
        [...report.Position,report.Fraction].some(n=>!Number.isFinite(n)))
       throw Error("Invalid correction report: "+JSON.stringify(report));
     switch(report.Probe){
       case Probe.Translation:
         if(report.Outcome!==Outcome.Clipped||report.Fraction<.449||report.Fraction>.451) throw Error("Thin wall not respected");
         if(report.Position[0]<-.1010001||report.Position[0]>-.1009989) throw Error("Incorrect wall contact"); break;
       case Probe.Simultaneous:
         if(report.Outcome!==Outcome.Clipped||report.Fraction<.449||report.Fraction>.451||report.Position[0]>=0) throw Error("Corrections crossed"); break;
       case Probe.FullTurn:
         if(report.Outcome!==Outcome.Clipped||report.Fraction<.04||report.Fraction>.07) throw Error("Intermediate rotation hit was missed"); break;
       case Probe.BlockedJoint:
         if(report.Outcome!==Outcome.Rejected) throw Error("Joint tunneled through obstacle");
         near(report.Position[0],-1,0); break;
       case Probe.ClearJoint:
         if(report.Outcome!==Outcome.Applied) throw Error("Clear correction was blocked");
         near(report.Position[0],1); break;
       default: throw Error("Unsupported projection probe");
     }
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only engine correction-path qualification; not gameplay-part UI proof or complete constrained dynamics."};
 } finally {await context.close();}
}
