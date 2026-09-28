async (page) => {
 /** @enum {number} */
 const Probe=Object.freeze({TubePassage:0,TubeWall:1,FunnelPassage:2,FunnelWall:3,Bend45Passage:4,Bend90Passage:5,Bend45Wall:6,Bend90Wall:7,JoinedTube:8});
 /** @enum {number} */
 const Status=Object.freeze({Clear:0,Contact:1,InitialContact:2});
 const probes=new Map([["TubePassage",Probe.TubePassage],["TubeWall",Probe.TubeWall],["FunnelPassage",Probe.FunnelPassage],["FunnelWall",Probe.FunnelWall],["Bend45Passage",Probe.Bend45Passage],["Bend90Passage",Probe.Bend90Passage],["Bend45Wall",Probe.Bend45Wall],["Bend90Wall",Probe.Bend90Wall],["JoinedTube",Probe.JoinedTube]]);
 const statuses=new Map([["Clear",Status.Clear],["Contact",Status.Contact],["InitialContact",Status.InitialContact]]);
 const prefix="CCGENERALHOLLOW ";
 const context=await page.context().browser().newContext();
 const tab=await context.newPage(),reports=[],errors=[];
 tab.on("console",m=>{const text=m.text();if(text.startsWith(prefix))reports.push(JSON.parse(text.slice(prefix.length)));if(m.type()==="error")errors.push(text);});
 tab.on("pageerror",e=>errors.push(e.message));
 const near=(a,b,t=1e-7)=>{if(!Number.isFinite(a)||Math.abs(a-b)>t)throw Error("Mismatch "+a+" / "+b);};
 try {
   await tab.goto("http://127.0.0.1:8060");
   const deadline=Date.now()+30000;
   while(reports.length<9&&Date.now()<deadline)await tab.waitForTimeout(100);
   await tab.waitForTimeout(300);
   if(reports.length!==9||new Set(reports.map(r=>r.Probe)).size!==9)throw Error("Missing/duplicate reports: "+JSON.stringify(reports));
   for(const r of reports){
     const probe=probes.get(r.Probe);
     if(probe===undefined||!Number.isInteger(r.Children)||r.Children<=0||!Number.isFinite(r.MaximumSurfaceError)||r.MaximumSurfaceError<=0||r.MaximumSurfaceError>.005||!Number.isFinite(r.MinimumBoreRadius)||r.MinimumBoreRadius<.645||!r.ExactReplay||!Number.isFinite(r.ElapsedMilliseconds)||r.ElapsedMilliseconds<0)throw Error("Invalid report "+JSON.stringify(r));
     if(probe===Probe.JoinedTube){
       if(r.Status!==null||r.Time!==null||r.NarrowPhaseCalls!==null||!r.ExactRestore||!Number.isInteger(r.WorldEvents)||r.WorldEvents<=0||r.Position.length!==3||r.Velocity.length!==3)throw Error("Invalid world report");
       near(r.Position[0],1.5,1e-6);near(r.Position[2],0,1e-6);
       if(r.Position[1]<-.451||r.Position[1]>-.44)throw Error("Tube support height");
       near(r.Velocity[0],2,1e-6);near(r.Velocity[1],0,1e-6);near(r.Velocity[2],0,1e-6);
       continue;
     }
     if(r.ExactRestore!==null||r.WorldEvents!==null||r.Position!==null||r.Velocity!==null||!Number.isInteger(r.NarrowPhaseCalls)||r.NarrowPhaseCalls<0||!Number.isFinite(r.Time))throw Error("Invalid query report");
     const status=statuses.get(r.Status);
     switch(probe){
       case Probe.TubePassage:case Probe.FunnelPassage:
         if(status!==Status.Clear)throw Error("Blocked axial bore");near(r.Time,.008);break;
       case Probe.Bend45Passage:case Probe.Bend90Passage:
         if(status!==Status.Clear)throw Error("Blocked curved bore");near(r.Time,1);break;
       case Probe.TubeWall:case Probe.FunnelWall:
         if(status!==Status.Contact)throw Error("Missed axial wall");
         near(r.Time,(4-(probe===Probe.TubeWall?2:.9)-.05-.0001)/1000,1e-8);break;
       case Probe.Bend45Wall:case Probe.Bend90Wall:
         if(status!==Status.Contact||r.Time<(.6-r.MaximumSurfaceError-.0001)/1000||r.Time>.0006)throw Error("Missed curved wall");break;
       default:throw Error("Unknown probe");
     }
   }
   if(errors.length)throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only engine qualification, not gameplay UI proof. Timing includes geometry construction and replay; not a frame-rate benchmark."};
 } finally {await context.close();}
}
