// Read-only isolated solver qualification; not a gameplay part proof.
async(page)=>{
 const context=await page.context().browser().newContext();
 const tab=await context.newPage(),reports=[],errors=[];
 const prefix="CCGENERALCONSTRAINT ";
 const Probe=Object.freeze({SlidingFriction:"SlidingFriction",MovingSlider:"MovingSlider",OffsetJoint:"OffsetJoint"});
 const expected=new Map([
 [Probe.SlidingFriction,{linear:[1.8,0,2.4],angular:[0,0,0],friction:[-.6,0,-.8]}],
 [Probe.MovingSlider,{linear:[3,0,0],angular:[0,1,0],friction:[0,0,0]}],
 [Probe.OffsetJoint,{linear:[0,0,1],angular:[0,0,0],friction:[0,0,0]}]]);
 tab.on("console",message=>{const value=message.text();if(message.type()==="error")errors.push(value);if(value.startsWith(prefix))reports.push(JSON.parse(value.slice(prefix.length)));});
 tab.on("pageerror",error=>errors.push(error.message));
 try{
 await tab.goto("http://127.0.0.1:8060");
 const deadline=Date.now()+20000;
 while(reports.length<3&&Date.now()<deadline)await tab.waitForTimeout(50);
 if(reports.length!==3||new Set(reports.map(r=>r.Probe)).size!==3||errors.length)throw Error("Incomplete constraint qualification: "+JSON.stringify({reports,errors}));
 for(const report of reports){
 const e=expected.get(report.Probe);
 if(!e||!Number.isFinite(report.Residual)||report.Residual>1e-8)throw Error("Invalid constraint report");
 for(const [actual,target] of [[report.Linear,e.linear],[report.Angular,e.angular],[report.FrictionImpulse,e.friction]])
 if(actual.length!==3||actual.some((v,i)=>!Number.isFinite(v)||Math.abs(v-target[i])>1e-8))
 throw Error("Constraint assertion failed: "+JSON.stringify(report));
 }
 return {reports,errors};
 }finally{await context.close();}
}
