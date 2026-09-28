// Read-only shared-trajectory qualification; not gameplay part proof.
async(page)=>{
 const context=await page.context().browser().newContext();
 const tab=await context.newPage(),reports=[],errors=[];
 const prefix="CCGENERALTRAJECTORY ";
 const Status=Object.freeze({Contact:"Contact"});
 tab.on("console",message=>{const value=message.text();if(message.type()==="error")errors.push(value);if(value.startsWith(prefix))reports.push(JSON.parse(value.slice(prefix.length)));});
 tab.on("pageerror",error=>errors.push(error.message));
 try{
 await tab.goto("http://127.0.0.1:8060");
 const deadline=Date.now()+20000;
 while(reports.length<1&&Date.now()<deadline)await tab.waitForTimeout(50);
 await tab.waitForTimeout(300);
 const report=reports[0];
 if(reports.length!==1||errors.length||report.Status!==Status.Contact||!report.ExactCommittedPose||
 report.Segments<=1||!Number.isFinite(report.Time)||report.Time<.02||report.Time>.12||
 !Number.isFinite(report.InitialSeparation)||report.InitialSeparation<=.01||
 !Number.isFinite(report.FinalSeparation)||report.FinalSeparation<=.01||
 !Number.isFinite(report.ContactSeparation)||report.ContactSeparation<0||report.ContactSeparation>.0001001)
 throw Error("Shared trajectory assertion failed: "+JSON.stringify({reports,errors}));
 return {report,errors};
 }finally{await context.close();}
}
