// Diagnostic observation only; this does not count as part behavioural proof.
async(page)=>{
 const context=await page.context().browser().newContext({viewport:{width:1440,height:900}});
 const test=await context.newPage(), observations=[],errors=[];
 test.on("console",m=>{const t=m.text();if(m.type()==="error")errors.push(t);
 for(const prefix of ["CCBACKENDQUERY ","CCBACKENDMOTION ","CCGENERALCCD "])if(t.startsWith(prefix))observations.push({prefix,report:JSON.parse(t.slice(prefix.length))});});
 try{
 await test.goto("http://127.0.0.1:8060");
 const end=Date.now()+20000;
 while(observations.length<12 && Date.now()<end)await test.waitForTimeout(50);
 await test.screenshot({path:".playwright-mcp/general-physics-backend-qualification-v2.png"});
 if(observations.length!==12)throw Error("Incomplete backend observations: "+observations.length);
 return {observations,errors};
 }finally{await context.close();}
}
