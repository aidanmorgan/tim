// Playwright MCP recipe for a diagnostics-enabled local build.
// Closed protocol values are mapped at this read-only external boundary.
async (page) => {
 /** @enum {number} */
 const Probe=Object.freeze({SingleSupport:0,TwoBodyStack:1});
 const probes=new Map([["SingleSupport",Probe.SingleSupport],["TwoBodyStack",Probe.TwoBodyStack]]);
 const prefix="CCGENERALPERSISTENCE ";
 const context=await page.context().browser().newContext();
 const tab=await context.newPage(), reports=[], errors=[];
 tab.on("console",message=>{
   const value=message.text();
   if(value.startsWith(prefix)) reports.push(JSON.parse(value.slice(prefix.length)));
   if(message.type()==="error") errors.push(value);
 });
 tab.on("pageerror",error=>errors.push(error.message));
 try {
   await tab.goto("http://127.0.0.1:8060");
   const deadline=Date.now()+30000;
   while(reports.length<2 && Date.now()<deadline) await tab.waitForTimeout(100);
   await tab.waitForTimeout(300);
   if(reports.length!==2 || new Set(reports.map(r=>r.Probe)).size!==2) throw Error("Missing or duplicate persistence reports: "+JSON.stringify(reports));
   for(const report of reports){
     const probe=probes.get(report.Probe);
     if(probe===undefined) throw Error("Unsupported probe");
     const single=probe===Probe.SingleSupport;
     if(report.Steps!==(single?600:240) || report.SolverTolerance!==(single?1e-8:1e-10) ||
        !Number.isFinite(report.MaximumDrift) || report.MaximumDrift>1e-6 ||
        !Number.isFinite(report.MaximumSpeed) || report.MaximumSpeed>2e-8 ||
        !Number.isFinite(report.MaximumSpin) || report.MaximumSpin>2e-8 ||
        !Number.isFinite(report.MaximumResidual) || report.MaximumResidual>report.SolverTolerance ||
        report.FirstIterations<1 || report.LaterMaximumIterations<1 ||
        report.LaterMaximumIterations>report.FirstIterations ||
        !report.StableIds || !report.ExactRestore || !report.ExactReplay || !report.ReleaseCleared)
       throw Error("Invalid persistent-contact result: "+JSON.stringify(report));
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Controlled engine support loops and read-only diagnostics, not a collision-complete world or gameplay-part UI proof."};
 } finally {await context.close();}
}
