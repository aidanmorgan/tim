import re,math,json,subprocess,xml.etree.ElementTree as E,hashlib,pathlib
root=E.parse('docs/verification/P0-007/coupled-probe-replay-scenes-r1.trx').getroot()
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
nums=lambda s:[float(a) for a in s.split(',')]
vec=lambda s,k:tuple(map(float,re.search(re.escape(k)+r' CollisionVector \{ X = ([^,]+), Y = ([^,]+), Z = ([^,]+)',s).groups()))
dot=lambda a,b:math.fsum(x*y for x,y in zip(a,b))
cross=lambda a,b:(a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def rotate(q,v):
 c=cross(q[:3],v);cc=cross(q[:3],c)
 return tuple((v[i]+c[i]*(2*q[3]))+cc[i]*2 for i in range(3))
def normalize(q):
 scale=max(map(abs,q));v=[x/scale for x in q];n=math.sqrt(sum(x*x for x in v))
 return tuple(x/n for x in v)
for t in root.findall('.//t:UnitTestResult',ns):
 if 'y: 0' not in t.attrib['testName']:continue
 m=t.find('.//t:Message',ns).text;s=m.split('original Newton solve [',1)[1]
 arr=lambda key:nums(re.search(re.escape(key)+r' \[([^\]]+)\]',s).group(1))
 state=arr('state');res=arr('residual');original=arr('direction')
 jt=s.split('Jacobian [',1)[1].split(']], full trial',1)[0]
 J=[nums(a) for a in re.findall(r'\[([^\]]+)\]',jt+']')]
 replay=m.split('finite-difference replay [',1)[1];cols=re.split(r'column (\d+), original ',replay)[1:];data=[]
 for k in range(0,len(cols),2):
  j=int(cols[k]);c=cols[k+1];w=float(re.search(r'width ([^,]+)',c).group(1))
  def read(text):
   return (vec(text,'physical slip'),vec(text,'U'),vec(text,'V'),float(re.search(r'radius ([^;\]]+)',text).group(1)),float(re.search(r'projected U ([^,]+)',text).group(1)),float(re.search(r'projected V ([^,]+)',text).group(1)))
  hi=read(c.split('high contacts [',1)[1].split('; contact 1',1)[0]);lo=read(c.split('low contacts [',1)[1].split('; contact 1',1)[0])
  data.append((j,w,hi,lo))
 base=data[0][2];q=(dot(base[0],base[1]),dot(base[0],base[2]));L=math.hypot(*q);n=[a/L for a in q];R=base[3]
 candidate=m.split('candidate [',1)[1].split('candidate coupled residual',1)[0]
 contact=candidate.split('contacts [',1)[1].split('regime Sticking',1)[0]
 def gradient(label):
  body=contact.split('tangent '+label+' [',1)[1].split('body 6, ',1)[1]
  return vec(body,'linear'),vec(body,'angular')
 gu=gradient('U');gv=gradient('V')
 body=m.split('initial bodies [',1)[1].split('PhysicsBodySnapshot { Id = PhysicsBodyId { Index = 6',1)[1]
 quat=tuple(map(float,re.search(r'Rotation = RigidRotation \{ X = ([^,]+), Y = ([^,]+), Z = ([^,]+), W = ([^,]+)',body).groups()))
 inertia=nums(re.search(r'local inertia \(([^)]+)\)',body).group(1));a,b,c,xy,xz,yz=inertia
 det=a*(b*c-yz*yz)-xy*(xy*c-xz*yz)+xz*(xy*yz-xz*b)
 local=((b*c-yz*yz)/det,(a*c-xz*xz)/det,(a*b-xy*xy)/det,(xz*yz-xy*c)/det,(xy*yz-xz*b)/det,(xy*xz-a*yz)/det)
 def apply(t,v):
  a,b,c,xy,xz,yz=t
  return (a*v[0]+xy*v[1]+xz*v[2],xy*v[0]+b*v[1]+yz*v[2],xz*v[0]+yz*v[1]+c*v[2])
 invq=normalize((-quat[0],-quat[1],-quat[2],quat[3]))
 columns=[rotate(quat,apply(local,rotate(invq,v))) for v in [(1,0,0),(0,1,0),(0,0,1)]]
 a,b,c=columns
 inverse=(a[0],b[1],c[2],(a[1]+b[0])*.5,(a[2]+c[0])*.5,(b[2]+c[1])*.5)
 angularU=apply(inverse,gu[1]);angularV=apply(inverse,gv[1])
 scale=res[44]/(state[44]-base[4])
 corrected=[row[:] for row in J]
 for j,w,h,l in data:
  ds=((dot(h[0],h[1])-dot(l[0],l[1]))/w,(dot(h[0],h[2])-dot(l[0],l[2]))/w)
  dr=(h[3]-l[3])/w;rad=dot(n,ds)
  chain=[-dr*n[i]-R*(ds[i]-n[i]*rad)/L for i in range(2)]
  fd=[(h[4]-l[4])/w,(h[5]-l[5])/w];change=[chain[i]-fd[i] for i in range(2)]
  for i in range(3):
   corrected[18+i][j]-=gu[0][i]*change[0]+gv[0][i]*change[1]
   corrected[21+i][j]-=angularU[i]*change[0]+angularV[i]*change[1]
  corrected[44][j]-=scale*change[0];corrected[45][j]-=scale*change[1]
 probe='tools/P0-007-newton-review/bin/Release/net10.0/Probe.dll'
 payload='\n'.join(json.dumps(a+[res]) for a in [J,corrected])+'\n'
 result=subprocess.run(['dotnet',probe],input=payload,text=True,capture_output=True)
 print('probe',hashlib.sha256(pathlib.Path(probe).read_bytes()).hexdigest(),'exit',result.returncode)
 print('inverse tensor',inverse,'tangent scale',scale)
 if result.returncode:print(result.stderr);raise SystemExit(result.returncode)
 responses=[json.loads(line) for line in result.stdout.splitlines()]
 for name,mat,response in zip(['original','corrected'],[J,corrected],responses):
  d=response.get('Value')
  if d is None:print(name,response);continue
  jd=[dot(row,d) for row in mat]
  print(name,'rank',response.get('Rank'),'norm',response.get('Norm'),'resolution',response.get('Resolution'))
  print(name,'stepnorm',math.sqrt(dot(d,d)),'rDotJd',dot(res,jd),'linearResidual',math.sqrt(sum((a+b)**2 for a,b in zip(res,jd))))
  print(name,'direction',d)
 print('assumptions: captured basis/radius/slip secants; base contact0 metadata from unaffected column0; exact source-order initial inverse tensor reconstruction; other geometry derivatives retained from original J; no nonlinear reevaluation')
