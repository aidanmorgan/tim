// Run with the Playwright MCP code runner against a diagnostics-enabled build.
// Enum wire values are mapped only at this external diagnostic boundary.
async (page) => {
 const Probe=Object.freeze({Sphere:"Sphere",Box:"Box",Hull:"Hull"});
 const Status=Object.freeze({Penetrating:"Penetrating"});
 const prefix="CCGENERALPENETRATION ";
 const context=await page.context().browser().newContext();
 const tab=await context.newPage(), reports=[], errors=[];
 tab.on("console", message=>{
   const value=message.text();
   if(value.startsWith(prefix)) reports.push(JSON.parse(value.slice(prefix.length)));
   if(message.type()==="error") errors.push(value);
 });
 tab.on("pageerror",error=>errors.push(error.message));
 try {
   await tab.goto("http://127.0.0.1:8060");
   const deadline=Date.now()+20000;
   while(reports.length<3 && Date.now()<deadline) await tab.waitForTimeout(100);
   await tab.waitForTimeout(300);
   if(reports.length!==3 || new Set(reports.map(r=>r.Probe)).size!==3) throw Error("Missing or duplicate penetration reports: "+JSON.stringify(reports));
   for(const report of reports){
     if(!Object.values(Probe).includes(report.Probe)) throw Error("Unsupported probe");
     const expected=report.Probe===Probe.Sphere ? 1.6-Math.sqrt(.21) : 1.6;
     if(report.Status!==Status.Penetrating ||
        !Number.isFinite(report.LowerDepth) || !Number.isFinite(report.UpperDepth) ||
        Math.abs(report.LowerDepth-expected)>2e-7 ||
        report.UpperDepth<report.LowerDepth-1e-12 ||
        report.UpperDepth-report.LowerDepth>1e-7+1e-12 ||
        !Number.isFinite(report.WitnessError) || report.WitnessError>2e-7)
       throw Error("Invalid penetration result: "+JSON.stringify(report));
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only engine diagnostics; not gameplay part proof."};
 } finally {await context.close();}
}
