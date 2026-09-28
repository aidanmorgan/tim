// Playwright MCP recipe against a diagnostics-enabled build.
// Closed wire values are mapped and validated only at the external boundary.
async (page) => {
 const Probe=Object.freeze({SpinningSlide:"SpinningSlide",BoxSlide:"BoxSlide",OrbitRecontact:"OrbitRecontact",GrazingReturn:"GrazingReturn"});
 const Status=Object.freeze({Clear:"Clear",Contact:"Contact"});
 const GeneralProbe=Object.freeze({PureRotation:"PureRotation",OpposingBodies:"OpposingBodies",CompoundPassage:"CompoundPassage",CompoundWall:"CompoundWall"});
 const prefix="CCGENERALSIGNEDSWEEP ", generalPrefix="CCGENERALCCD ";
 const context=await page.context().browser().newContext();
 const tab=await context.newPage(), reports=[],general=[],errors=[];
 tab.on("console",message=>{
   const value=message.text();
   if(value.startsWith(prefix)) reports.push(JSON.parse(value.slice(prefix.length)));
   if(value.startsWith(generalPrefix)) general.push(JSON.parse(value.slice(generalPrefix.length)));
   if(message.type()==="error") errors.push(value);
 });
 tab.on("pageerror",error=>errors.push(error.message));
 try {
   await tab.goto("http://127.0.0.1:8060");
   const deadline=Date.now()+30000;
   while((reports.length<4||general.length<4) && Date.now()<deadline) await tab.waitForTimeout(100);
   await tab.waitForTimeout(300);
   if(reports.length!==4 || new Set(reports.map(r=>r.Probe)).size!==4) throw Error("Missing or duplicate signed sweep reports: "+JSON.stringify(reports));
   for(const report of reports){
     if(!Object.values(Probe).includes(report.Probe)) throw Error("Unsupported probe");
     const orbit=report.Probe===Probe.OrbitRecontact;
     if(report.Status!==(orbit?Status.Contact:Status.Clear) ||
        report.MinimumSeparation!==-1e-6 || !Number.isFinite(report.Time) ||
        !Number.isFinite(report.Lower) || !Number.isFinite(report.Upper) || report.Lower>report.Upper+1e-12 ||
        report.Iterations<1 || report.Iterations>4096 ||
        report.ExactCommittedPose!==true || report.ExactRestore!==true || report.ExactReplay!==true)
       throw Error("Invalid signed sweep result: "+JSON.stringify(report));
     if(orbit){
       const angle=report.Time*120, expected=2*Math.PI-2*Math.atan(.4);
       if(Math.abs(report.InitialLower)>1e-7 || report.MidLower<=1 ||
          angle<expected-1e-6 || angle>expected+1e-5 ||
          report.Upper<report.MinimumSeparation-1e-7 || report.Upper>report.MinimumSeparation+1e-7 ||
          report.RotationalReach!==1) throw Error("Incorrect release/recontact");
     }else{
       if(report.Time!==report.Duration || Math.abs(report.Upper)>1e-7) throw Error("Incorrect clear horizon");
       if(report.Probe!==Probe.GrazingReturn && report.Iterations!==1) throw Error("Tangential motion should have a full-horizon plane certificate");
       if(report.Probe===Probe.SpinningSlide && report.RotationalReach!==0) throw Error("Centred sphere has false rotational travel");
       if(report.Probe===Probe.GrazingReturn && report.MidLower<=1) throw Error("Grazing orbit did not leave contact");
     }
   }
   if(general.length!==4 || new Set(general.map(r=>r.Probe)).size!==4) throw Error("Missing general sweep regression");
   for(const report of general){
     if(!Object.values(GeneralProbe).includes(report.Probe)) throw Error("Unsupported general sweep probe");
     const clear=report.Probe===GeneralProbe.CompoundPassage;
     if(report.Status!==(clear?Status.Clear:Status.Contact) || !Number.isFinite(report.Time)) throw Error("General sweep regression failed");
     if(report.Probe===GeneralProbe.OpposingBodies && Math.abs(report.Time-(3-.0001)/800)>1e-8) throw Error("Opposing-body time is wrong");
     if(report.Probe===GeneralProbe.PureRotation && (report.Time*120<.3||report.Time*120>.4)) throw Error("Rotation contact is wrong");
   }
   if(errors.length) throw Error(JSON.stringify(errors));
   return {reports,general,errors,scope:"Read-only signed-clearance engine qualification; no gameplay-part UI completion claimed."};
 } finally {await context.close();}
}
