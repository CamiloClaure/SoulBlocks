using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    public sealed class PathPopulation : MonoBehaviour
    {
        public WanderingSpirit spiritPrefab;
        public BreakableCrystal crystalPrefab;
        public GuidingTorch torch;
        [Min(4)] public float spiritSpacing = 10;
        [Min(5)] public float crystalSpacing = 14;
        [Min(1)] public int maxSpirits = 120;
        [Min(1)] public int maxCrystals = 120;
        public int SpiritCount { get; private set; }
        public int CrystalCount { get; private set; }
        bool built;
        struct Road { public AuthoredPath path; public List<Vector3> points; public float halfWidth; }
        readonly List<Road> roads = new List<Road>();
        public bool InsideRoad(Vector3 position, float clearance = .5f)
        {
            foreach(var road in roads) foreach(var point in road.points)
            {
                var delta=point-position;delta.y=0;
                if(delta.sqrMagnitude<(road.halfWidth+clearance)*(road.halfWidth+clearance))return true;
            }
            return false;
        }
        void Start() => Populate();
        public void Populate()
        {
            if(built || spiritPrefab==null || crystalPrefab==null || torch==null)return;
            built=true;
            foreach(var path in FindObjectsByType<AuthoredPath>(FindObjectsSortMode.InstanceID))
            {
                if(path.points==null || path.points.Count<2)continue;
                path.Rebuild();
                var points=new List<Vector3>();
                for(int i=0;i<=1000;i++)points.Add(path.transform.TransformPoint(path.Evaluate(i/1000f)));
                roads.Add(new Road{path=path,points=points,halfWidth=path.width*Mathf.Max(Mathf.Abs(path.transform.lossyScale.x),Mathf.Abs(path.transform.lossyScale.z))*.5f});
            }
            Physics.SyncTransforms();
            var souls=new GameObject("Almas de los caminos").transform;souls.SetParent(transform,false);
            var crystals=new GameObject("Grupos de cristales").transform;crystals.SetParent(transform,false);
            foreach(var road in roads)
            {
                float distance=0,nextSpirit=8,nextCrystal=12;
                for(int i=1;i<road.points.Count-1;i++)
                {
                    distance+=Vector3.Distance(road.points[i-1],road.points[i]);
                    Vector3 forward=(road.points[i+1]-road.points[i-1]).normalized;
                    Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
                    if(distance>=nextSpirit)
                    {
                        nextSpirit+=Mathf.Max(4,spiritSpacing);
                        for(int sign=-1;sign<=1;sign+=2)
                        {
                            if(SpiritCount>=maxSpirits)break;
                            Vector3 point=road.points[i]+side*sign*(road.halfWidth+3.5f);
                            if(InsideRoad(point,2.2f))continue;
                            if(Physics.Raycast(point+Vector3.up,Vector3.down,out var floor,3,(1<<8)|(1<<9)))point.y=floor.point.y;
                            var soul=Instantiate(spiritPrefab,point,Quaternion.identity,souls);
                            soul.name="Alma del camino "+(++SpiritCount);soul.torch=torch;soul.phase=SpiritCount*2.4f;soul.allowFloatingOutsideGround=true;
                        }
                    }
                    if(distance>=nextCrystal)
                    {
                        nextCrystal+=Mathf.Max(5,crystalSpacing);
                        int sign=((int)(distance/Mathf.Max(5,crystalSpacing))%2==0)?1:-1;
                        for(int c=0;c<3 && CrystalCount<maxCrystals;c++)
                        {
                            var point=road.points[i]+side*sign*Mathf.Max(.5f,road.halfWidth-.65f)+forward*((c-1)*1.05f);
                            if(!road.path.GetComponent<MeshCollider>().Raycast(new Ray(point+Vector3.up*2,Vector3.down),out var hit,4))continue;
                            bool occupied=false;
                            foreach(var other in Physics.OverlapSphere(hit.point+Vector3.up*.45f,.55f))if(other.GetComponentInParent<BreakableCrystal>()!=null){occupied=true;break;}
                            if(occupied)continue;
                            var crystal=Instantiate(crystalPrefab,hit.point,Quaternion.identity,crystals);
                            crystal.name="Cristal de grupo "+(++CrystalCount);crystal.color=CrystalColor.Yellow;crystal.size=CrystalSize.Small;crystal.startsHidden=false;crystal.ResetCrystal();
                            Physics.SyncTransforms();
                        }
                    }
                }
            }
        }
    }
}
