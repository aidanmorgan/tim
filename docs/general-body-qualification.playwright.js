// Read-only isolated body integration and replay qualification.
async(page)=>{
 const context=await page.context().browser().newContext();
 const tab=await context.newPage(),reports=[],errors=[];
 const prefix="CCGENERALBODY ";
 tab.on("console",message=>{const value=message.text();if(message.type()==="error")errors.push(value);if(value.startsWith(prefix))reports.push(JSON.parse(value.slice(prefix.length)));});
 tab.on("pageerror",error=>errors.push(error.message));
 try{
 await tab.goto("http://127.0.0.1:8060");
 const deadline=Date.now()+20000;
 while(reports.length<1&&Date.now()<deadline)await tab.waitForTimeout(50);
 const report=reports[0];
 if(reports.length!==1||errors.length||report.Steps!==4800||report.Duration!==10||
 !report.MomentumExact||!report.Restored||!report.Replayed||
 !Number.isFinite(report.MaximumRelativeEnergyError)||report.MaximumRelativeEnergyError>2e-6||
 report.Center.some((v,i)=>!Number.isFinite(v)||Math.abs(v-[2,3,4][i])>1e-10))
 throw Error("Body integration assertion failed: "+JSON.stringify({reports,errors}));
 return {report,errors};
 }finally{await context.close();}
}
