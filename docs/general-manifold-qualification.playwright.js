// Playwright MCP recipe for a diagnostics-enabled local build.
// Enum wire values are mapped at this read-only external boundary.
async (page) => {
 const Probe=Object.freeze({FlatImpact:"FlatImpact",RotatedFaces:"RotatedFaces",Clear:"Clear"});
 const Status=Object.freeze({Contact:"Contact",Clear:"Clear"});
 const prefix="CCGENERALMANIFOLD ";
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
   const deadline=Date.now()+20000;
   while(reports.length<3 && Date.now()<deadline) await tab.waitForTimeout(100);
   await tab.waitForTimeout(300);
   if(reports.length!==3 || new Set(reports.map(r=>r.Probe)).size!==3) throw Error("Missing or duplicate manifold reports: "+JSON.stringify(reports));
   for(const report of reports){
     if(!Object.values(Probe).includes(report.Probe)) throw Error("Unsupported probe");
     const clear=report.Probe===Probe.Clear;
     const expectedCount=clear?0:report.Probe===Probe.FlatImpact?4:8;
     if(report.Status!==(clear?Status.Clear:Status.Contact) || report.Points.length!==expectedCount ||
        !Number.isFinite(report.MaximumAnchorError) || report.MaximumAnchorError>2e-7 ||
        !Number.isFinite(report.LinearSpeed) || Math.abs(report.LinearSpeed-(clear?3:0))>1e-8 ||
        !Number.isFinite(report.AngularSpeed) || report.AngularSpeed>1e-8 ||
        !Number.isFinite(report.Residual) || report.Residual>1e-8 ||
        !report.ExactRestore || !report.ExactReplay)
       throw Error("Invalid manifold result: "+JSON.stringify(report));
     for(const point of report.Points){
       if(point.length!==4 || point.some(x=>!Number.isFinite(x)) ||
          Math.abs(point[1])>1e-7 || Math.abs(point[3])>1e-7) throw Error("Invalid contact point");
       const magnitudes=[Math.abs(point[0]),Math.abs(point[2])].sort((a,b)=>a-b);
       const smaller=report.Probe===Probe.FlatImpact?1:Math.sqrt(2)-1;
       if(Math.abs(magnitudes[0]-smaller)>1e-7 || Math.abs(magnitudes[1]-1)>1e-7) throw Error("Incorrect clipped corner");
     }
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only engine diagnostics. Instantaneous response and body restore/replay, not resting-world or gameplay-part proof."};
 } finally {await context.close();}
}
