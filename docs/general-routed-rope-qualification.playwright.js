async (page) => {
 /** @enum {number} */
 const Probe=Object.freeze({MovingGuide:0,FixedGuide:1,Shortening:2,SlackEvent:3,ClearProjection:4,BlockedProjection:5,RotatingGuide:6});
 /** @enum {number} */
 const Outcome=Object.freeze({Completed:0,Rejected:1});
 const probes=new Map([["MovingGuide",Probe.MovingGuide],["FixedGuide",Probe.FixedGuide],["Shortening",Probe.Shortening],
   ["SlackEvent",Probe.SlackEvent],["ClearProjection",Probe.ClearProjection],["BlockedProjection",Probe.BlockedProjection],["RotatingGuide",Probe.RotatingGuide]]);
 const outcomes=new Map([["Completed",Outcome.Completed],["Rejected",Outcome.Rejected]]);
 const prefix="CCGENERALROUTEDROPE ";
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
   while(reports.length<7&&Date.now()<deadline) await tab.waitForTimeout(100);
   await tab.waitForTimeout(300);
   if(reports.length!==7||new Set(reports.map(r=>r.Probe)).size!==7) throw Error("Missing or duplicate routed-rope reports: "+JSON.stringify(reports));
   const near=(a,b,t=1e-7)=>{if(!Number.isFinite(a)||Math.abs(a-b)>t) throw Error("Mismatch "+a+" / "+b);};
   for(const report of reports){
     const probe=probes.get(report.Probe),outcome=outcomes.get(report.Outcome);
     if(probe===undefined||outcome===undefined||!report.ExactRestore||!report.ExactReplay||report.Speeds.length!==3||
        [...report.Speeds,report.Length,report.GuideHeight,report.GuideSpin].some(n=>!Number.isFinite(n)))
       throw Error("Invalid routed-rope report: "+JSON.stringify(report));
     if(outcome!==(probe===Probe.BlockedProjection?Outcome.Rejected:Outcome.Completed)) throw Error("Wrong completion state");
     let expected,events=0;
     switch(probe){
       case Probe.MovingGuide:
         expected=[-7/3,-1/3,-4/3]; near(report.Length,4); near(report.GuideHeight,13/15); break;
       case Probe.FixedGuide: expected=[-1,1,0]; near(report.Length,4); near(report.GuideHeight,1); break;
       case Probe.Shortening: expected=[3,1,0]; near(report.Length,3.6); near(report.GuideHeight,1); break;
       case Probe.SlackEvent:
         expected=[-50,50,0]; events=1; near(report.Length,5); near(report.GuideHeight,1);
         near(report.BeforeFlightSpeed,100); near(report.BoundaryTime,.01,1e-9); break;
       case Probe.ClearProjection: expected=[0,0,0]; near(report.Length,4); near(report.GuideHeight,1); break;
       case Probe.BlockedProjection: expected=[0,0,0]; near(report.Length,6); near(report.GuideHeight,2); break;
       case Probe.RotatingGuide:
         expected=[0,0,0]; events=1; near(report.Length,2); near(report.GuideHeight,0);
         near(report.BoundaryTime*120,Math.PI/3); break;
       default: throw Error("Unsupported routed-rope probe");
     }
     report.Speeds.forEach((value,index)=>near(value,expected[index]));
     near(report.GuideSpin,probe===Probe.RotatingGuide?120:0);
     if(report.Events!==events||(events===0&&report.BoundaryTime!==null)||
        (probe!==Probe.SlackEvent&&report.BeforeFlightSpeed!==null)) throw Error("Unexpected routed-rope event state");
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only shared-world point-route qualification. RotatingGuide is a captured-path sweep. Not sheave-arc geometry or gameplay-part UI proof."};
 } finally {await context.close();}
}
