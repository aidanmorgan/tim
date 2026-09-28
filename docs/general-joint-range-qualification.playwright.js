async (page) => {
 const Probe=Object.freeze({HingeLower:"HingeLower",HingeUpper:"HingeUpper",SliderLower:"SliderLower",SliderUpper:"SliderUpper",SliderObstacle:"SliderObstacle"});
 const prefix="CCGENERALJOINTRANGE ";
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
   if(reports.length!==5||new Set(reports.map(r=>r.Probe)).size!==5) throw Error("Missing or duplicate range reports: "+JSON.stringify(reports));
   for(const report of reports){
     if(!Object.values(Probe).includes(report.Probe)||!report.ExactRestore||!report.ExactReplay||
        [report.Coordinate,report.ReleasedCoordinate,report.Speed,report.Error].some(n=>!Number.isFinite(n))||
        report.Speed>1e-7||report.Error>1e-7)
       throw Error("Invalid range report: "+JSON.stringify(report));
     const sign=report.Probe===Probe.HingeLower||report.Probe===Probe.SliderLower?-1:1;
     if(report.Probe===Probe.SliderObstacle){
       if(report.Coordinate<.1988||report.Coordinate>.1991||report.Events<1) throw Error("Obstacle did not stop the slider before its limit");
     }else if(Math.abs(report.Coordinate-sign*.5)>1e-7) throw Error("Incorrect stop coordinate");
     if(sign*(report.Coordinate-report.ReleasedCoordinate)<.009) throw Error("Stop did not release inward");
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only engine range qualification, not catalogue UI proof or exact stop-event timing."};
 } finally {await context.close();}
}
