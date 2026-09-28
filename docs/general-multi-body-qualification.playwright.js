async (page) => {
 /** @enum {number} */
 const Probe=Object.freeze({MovingGuide:0,Shortening:1,OffsetGuide:2,ContactCoupling:3,BilateralChain:4});
 const wire=new Map([["MovingGuide",Probe.MovingGuide],["Shortening",Probe.Shortening],
   ["OffsetGuide",Probe.OffsetGuide],["ContactCoupling",Probe.ContactCoupling],["BilateralChain",Probe.BilateralChain]]);
 const prefix="CCGENERALMULTIBODY ";
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
   if(reports.length!==5||new Set(reports.map(r=>r.Probe)).size!==5) throw Error("Missing or duplicate multi-body reports: "+JSON.stringify(reports));
   const near=(a,b,t=1e-8)=>{if(!Number.isFinite(a)||Math.abs(a-b)>t) throw Error("Mismatch "+a+" / "+b);};
   for(const report of reports){
     const probe=wire.get(report.Probe);
     if(probe===undefined||!report.ExactRestore||!report.ExactReplay||report.Residual>1e-8||
        report.Residual<0||report.Iterations<1||report.Energy<0||report.Speeds.length!==3||report.Momentum.length!==3||
        [...report.Speeds,...report.Momentum,report.GuideSpin,report.Energy,report.AngularMomentum,report.Residual].some(n=>!Number.isFinite(n)))
       throw Error("Invalid multi-body report: "+JSON.stringify(report));
     let expected;
     switch(probe){
       case Probe.MovingGuide: expected=[7/3,1/3,4/3]; near(report.Energy,11/3); near(report.GuideSpin,0); near(report.AngularMomentum,-2); break;
       case Probe.Shortening: expected=[-3,-1,0]; near(report.Energy,5); near(report.GuideSpin,0); near(report.AngularMomentum,2); break;
       case Probe.OffsetGuide:
         expected=[17/7,3/7,8/7]; near(report.GuideSpin,4/7); near(report.AngularMomentum,-1);
         near(report.Energy,27/7); break;
       case Probe.ContactCoupling:
         expected=[0,-.8,-.4]; near(report.GuideSpin,0); near(report.Energy,.4);
         if(report.Iterations<=1) throw Error("Contact was not coupled"); break;
       case Probe.BilateralChain: expected=[4/3,4/3,4/3]; near(report.Energy,8/3); near(report.GuideSpin,0); near(report.AngularMomentum,0); break;
       default: throw Error("Unsupported multi-body probe");
     }
     report.Speeds.forEach((value,index)=>near(value,expected[index]));
     near(report.Momentum[0],0); near(report.Momentum[2],0);
     near(report.Momentum[1],probe===Probe.Shortening?-4:probe===Probe.ContactCoupling?-1.2:4);
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only generalized constraint qualification; not routed-rope geometry or gameplay-part UI proof."};
 } finally {await context.close();}
}
