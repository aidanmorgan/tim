async (page) => {
 /** @enum {number} */
 const Probe=Object.freeze({SparseQuery:0,DistantHollowBodies:1,LocalWorld:2,BlockedCorrection:3,IndependentLoads:4});
 /** @enum {number} */
 const Outcome=Object.freeze({Completed:0,Rejected:1});
 const probes=new Map([["SparseQuery",Probe.SparseQuery],["DistantHollowBodies",Probe.DistantHollowBodies],["LocalWorld",Probe.LocalWorld],["BlockedCorrection",Probe.BlockedCorrection],["IndependentLoads",Probe.IndependentLoads]]);
 const outcomes=new Map([["Completed",Outcome.Completed],["Rejected",Outcome.Rejected]]);
 const context=await page.context().browser().newContext();
 const tab=await context.newPage(),reports=[],errors=[];
 const prefix="CCGENERALSPATIAL ";
 tab.on("console",m=>{const text=m.text();if(text.startsWith(prefix))reports.push(JSON.parse(text.slice(prefix.length)));if(m.type()==="error")errors.push(text);});
 tab.on("pageerror",e=>errors.push(e.message));
 try {
   await tab.goto("http://127.0.0.1:8060");
   const deadline=Date.now()+30000;
   while(reports.length<5&&Date.now()<deadline)await tab.waitForTimeout(100);
   await tab.waitForTimeout(300);
   if(reports.length!==5||new Set(reports.map(r=>r.Probe)).size!==5)throw Error("Missing/duplicate spatial probes "+JSON.stringify(reports));
   for(const r of reports){
     const probe=probes.get(r.Probe),outcome=outcomes.get(r.Outcome);
     if(probe===undefined||outcome===undefined||!r.ExactReplay||!Number.isFinite(r.ElapsedMilliseconds)||r.ElapsedMilliseconds<0||!Number.isSafeInteger(r.AllocatedBytes)||r.AllocatedBytes<0)throw Error("Invalid report "+JSON.stringify(r));
     if(outcome!==(probe===Probe.BlockedCorrection?Outcome.Rejected:Outcome.Completed))throw Error("Wrong outcome");
     switch(probe){
       case Probe.SparseQuery:
         if(r.CartesianPairs!==4096||r.NodeTests<1||r.NodeTests>60||r.LeafTests!==1||r.Candidates!==1||r.Child!==2000||r.ExactRestore!==null||r.ReferenceMatch!==null||r.RetainedPairs!==null)throw Error("Sparse query failed");break;
       case Probe.DistantHollowBodies:
         if(r.CartesianPairs!==1064*1064||r.NodeTests!==1||r.LeafTests!==0||r.Candidates!==0||r.RetainedPairs!==0||!r.ExactRestore||!r.ReferenceMatch||r.Child!==null)throw Error("Distant roots failed");break;
       case Probe.LocalWorld:
         if(r.CartesianPairs!==4096||r.RetainedPairs<1||r.RetainedPairs>3||!r.ExactRestore||!r.ReferenceMatch)throw Error("Local world/reference mismatch");break;
       case Probe.BlockedCorrection:
         if(r.CartesianPairs!==1||r.RetainedPairs!==0||!r.ExactRestore||r.ReferenceMatch!==null)throw Error("Correction rollback failed");break;
       case Probe.IndependentLoads:
         if(r.CartesianPairs!==4096*4095/2||r.CouplingTests!==0||r.CoupledPairs!==0||!r.ExactRestore||!r.ReferenceMatch||r.RetainedPairs!==null)throw Error("Independent row scheduling failed");break;
       default:throw Error("Unsupported spatial probe");
     }
     if(probe!==Probe.IndependentLoads&&(r.CouplingTests!==null||r.CoupledPairs!==null))throw Error("Unexpected row metrics");
     if(probe!==Probe.SparseQuery&&probe!==Probe.DistantHollowBodies&&(r.NodeTests!==null||r.LeafTests!==null||r.Candidates!==null||r.Child!==null))throw Error("Unexpected hierarchy metrics");
   }
   if(errors.length)throw Error(JSON.stringify(errors));
   return {reports,errors,scope:"Read-only spatial engine qualification. Timings/allocations include construction and replay, not gameplay frame performance or part UI proof."};
 } finally {await context.close();}
}
