// Read-only isolated solver qualification; no game-state setters.
async(page)=>{
 const context=await page.context().browser().newContext();
 const tab=await context.newPage(),reports=[],errors=[];
 const prefix="CCGENERALIMPULSE ";
 const Probe=Object.freeze({ElasticImpact:"ElasticImpact",CoupledHinge:"CoupledHinge"});
 tab.on("console",message=>{const value=message.text();if(message.type()==="error")errors.push(value);if(value.startsWith(prefix))reports.push(JSON.parse(value.slice(prefix.length)));});
 try{
 await tab.goto("http://127.0.0.1:8060");
 const deadline=Date.now()+20000;
 while(reports.length<2&&Date.now()<deadline)await tab.waitForTimeout(50);
 if(reports.length!==2||errors.length)throw Error("Incomplete impulse qualification: "+JSON.stringify({reports,errors}));
 for(const report of reports){
 const expected=report.Probe===Probe.ElasticImpact?[-1.8,2.2,0]:report.Probe===Probe.CoupledHinge?[-12/7,-6/7,-6/7]:null;
 if(!expected||[report.LinearA,report.LinearB,report.AngularB].some((v,i)=>Math.abs(v-expected[i])>1e-8)||report.Residual>1e-8)
 throw Error("Impulse response assertion failed: "+JSON.stringify(report));
 }
 return {reports,errors};
 }finally{await context.close();}
}
