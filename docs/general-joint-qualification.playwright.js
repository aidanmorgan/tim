async (page) => {
 const Probe=Object.freeze({HingePendulum:"HingePendulum",Slider:"Slider",SlackRope:"SlackRope",TautRope:"TautRope",CoupledImpact:"CoupledImpact"});
 const prefix="CCGENERALJOINT ";
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
   if(reports.length!==5||new Set(reports.map(r=>r.Probe)).size!==5) throw Error("Missing or duplicate joint reports: "+JSON.stringify(reports));
   const near=(a,b,t=1e-7)=>{if(Math.abs(a-b)>t) throw Error("Mismatch "+a+" / "+b);};
   for(const report of reports){
     if(!Object.values(Probe).includes(report.Probe)||!report.ExactRestore||!report.ExactReplay||
        [...report.Position,...report.Velocity,...report.Spin,report.MaximumError,report.MaximumVelocityResidual].some(n=>!Number.isFinite(n))||
        report.MaximumError>1e-7||report.MaximumVelocityResidual>1e-7)
       throw Error("Invalid joint report: "+JSON.stringify(report));
     switch(report.Probe){
       case Probe.HingePendulum:
         near(report.Steps,240,0); near(report.Events,0,0); near(report.Position[2],0);
         if(report.Position[1]>=-.1) throw Error("Pendulum did not swing"); break;
       case Probe.Slider:
         near(report.Velocity[0],0); near(report.Velocity[1],0); near(report.Velocity[2],7);
         near(Math.hypot(...report.Spin),0);
         if(report.Position[2]<=5) throw Error("Slider did not travel"); break;
       case Probe.SlackRope:
         near(report.Position[0],.6); near(report.Velocity[0],.1); near(report.Events,0,0); break;
       case Probe.TautRope:
         near(report.Position[0],1); near(Math.hypot(...report.Velocity),0); near(report.Events,0,0); break;
       case Probe.CoupledImpact:
         if(report.Events<1||report.Spin[2]>=-.1||report.PayloadVelocityY<=-10) throw Error("Contact and hinge did not exchange momentum");
         near(Math.hypot(...report.Position),0); break;
       default: throw Error("Unsupported joint probe");
     }
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Integrated engine joint/contact diagnostics; not catalogue part UI or constrained-trajectory completion proof."};
 } finally {await context.close();}
}
