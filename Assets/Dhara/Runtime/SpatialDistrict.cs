using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace Dhara
{
    public enum DistrictOverlay { Normal, Water, Runoff, Surfaces, Connections, Intervention }
    public sealed class DistrictObject
    {
        public string Id,Zone;
        public Renderer Renderer;
        public double Area;
        public Color Original;
    }
    public sealed class SpatialDistrict : MonoBehaviour
    {
        DharaApplication app;
        MaterialPropertyBlock block;
        readonly List<LineRenderer> paths=new List<LineRenderer>();
        Material pathMaterial;
        public readonly List<DistrictObject> Objects=new List<DistrictObject>();
        public DistrictOverlay Overlay {get;private set;}
        public DistrictHydrology Preview {get;private set;}
        public string HighlightedZone {get;private set;}
        public DistrictObject Selected {get;private set;}
        public event Action SelectionChanged;
        public DistrictHydrology Model => Preview??app.Engine.District;
        public void Initialize(DharaApplication application)
        {
            app=application;
            block=new MaterialPropertyBlock();
            int roof=0,road=0,green=0,collection=0;
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                string zone=null,id=null;double area=0;
                if(renderer.name=="Roof coping"){zone="roof";id="roof-"+(++roof);area=117;}
                else if(renderer.name=="East west avenue"||renderer.name=="North south street") {zone="road";id="road-"+(++road);area=renderer.name=="East west avenue"?1248:880;}
                else if(renderer.name=="Community green strip"){zone="green";id="green-"+(++green);area=280;}
                else if(renderer.name=="Detention basin rim"){zone="collection";id="collection-"+(++collection);area=0;}
                if(zone!=null)Objects.Add(new DistrictObject{Id=id,Zone=zone,Area=area,Renderer=renderer,Original=renderer.sharedMaterial.color});
            }
            pathMaterial=new Material(app.SurfaceTemplate){name="Modeled drainage connections",color=new Color(.08f,.62f,.75f)};
            foreach(var obj in Objects)
            {
                if(obj.Zone=="collection")continue;
                var line=new GameObject(obj.Id+" → collection").AddComponent<LineRenderer>();line.transform.SetParent(transform,false);
                line.sharedMaterial=pathMaterial;line.startWidth=.28f;line.endWidth=.12f;line.positionCount=3;
                var from=obj.Renderer.bounds.center;from.y=obj.Renderer.bounds.max.y+.5f;
                line.SetPositions(new[]{from,new Vector3(from.x,1,9.1f),new Vector3(34,1,42)});line.enabled=false;paths.Add(line);
            }
        }
        public string Legend => Overlay==DistrictOverlay.Normal?"Normal materials · shared city weather":
            Overlay==DistrictOverlay.Water?"Pale → blue: retained volume / capacity; collection / 150 m³":
            Overlay==DistrictOverlay.Runoff?"Pale → orange: runoff / rain; cyan lines: instantaneous modeled routing":
            Overlay==DistrictOverlay.Surfaces?"Blue roofs · gray roads · green strips · cyan collection":
            Overlay==DistrictOverlay.Connections?"Cyan lines: all modeled surfaces → shared collection (schematic)":
            "Green roof tint / teal road tint: uniform intervention fraction; cyan garden at collection";
        public void SetOverlay(DistrictOverlay overlay){Overlay=overlay;Repaint();}
        public void ShowComparison(DistrictComparison c)
        {
            if(c==null){Preview=null;Repaint();return;}
            Preview=new DistrictHydrology(c.Configuration,c.Drainage);for(int i=0;i<600;i++)Preview.Step(c.Input.RainMmPerHour.Value,6);
            SetOverlay(DistrictOverlay.Intervention);
        }
        public void Highlight(string zone){HighlightedZone=zone;Repaint();}
        public bool SelectObject(string id)
        {Selected=Objects.Find(item=>item.Id==id);HighlightedZone=null;Repaint();SelectionChanged?.Invoke();return Selected!=null;}
        public void Deselect(){Selected=null;HighlightedZone=null;Repaint();SelectionChanged?.Invoke();}
        public string InspectSelected()
        {
            if(Selected==null)return "Select a roof, road, green strip or collection area in the 3D view.";
            var o=Selected;var m=Model;double rain=0,runoff=0,retained=0,infiltration=0,area=0;
            foreach(var z in m.Zones)if(z.Id==o.Zone||(o.Zone=="roof"&&z.Id=="green-roof")||(o.Zone=="road"&&z.Id=="permeable"))
            {rain+=z.RainM3;runoff+=z.RunoffM3;retained+=z.RetainedM3;infiltration+=z.InfiltratedM3;area+=z.AreaM2;}
            double fraction=area>0?o.Area/area:0;
            return o.Id+" · "+o.Zone+"\n"+(Preview!=null?"FROZEN INTERVENTION RESULT":"RUNNING "+app.Simulation.Mode.ToString().ToUpperInvariant())+"\n"+
                (o.Zone=="collection"?$"Shared storage {m.Result.StorageM3:0.000} / 150 m³\nDrained {m.Result.DrainedM3:0.000} m³\nGarden stored {m.GardenStoredM3:0.000} / {m.Configuration.GardenCapacityM3:0.0} m³\nOutlet: off-site modeled removal":
                $"Assigned catchment area {o.Area:0} m²\nRain volume {rain*fraction:0.000} m³\nRunoff {runoff*fraction:0.000} m³\nRetained {retained*fraction:0.000} m³\nInfiltrated {infiltration*fraction:0.000} m³\nConnection: shared collection\n"+
                (o.Zone=="roof"?$"Green roof {m.Configuration.GreenRoofFraction*100:0}% · 20 mm capacity":o.Zone=="road"?$"Permeable {m.Configuration.PermeableFraction*100:0}% · 15 mm/h": "Green strip infiltration 8 mm/h"))+"\nArea-weighted conceptual model; shared city weather.";
        }
        void Update()
        {
            if(Mouse.current==null||!Mouse.current.leftButton.wasPressedThisFrame||EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())return;
            var pointer=Mouse.current.position.ReadValue();if(!app.World.ViewCamera.pixelRect.Contains(pointer))return;
            var ray=app.World.ViewCamera.ScreenPointToRay(pointer);DistrictObject found=null;float nearest=float.MaxValue;
            foreach(var obj in Objects)if(obj.Renderer.bounds.IntersectRay(ray,out float distance)&&distance<nearest){nearest=distance;found=obj;}
            if(found==null)Deselect();else SelectObject(found.Id);
        }
        float next;
        void LateUpdate(){if(Time.unscaledTime>=next){next=Time.unscaledTime+.25f;Repaint();}}
        void Repaint()
        {
            if(app==null)return;var m=Model;
            foreach(var obj in Objects)
            {
                Color color=obj.Original;
                if(Overlay==DistrictOverlay.Surfaces)color=obj.Zone=="roof"?new Color(.3f,.56f,.78f):obj.Zone=="road"?Color.gray:obj.Zone=="green"?new Color(.28f,.62f,.3f):Color.cyan;
                if(Overlay==DistrictOverlay.Intervention)
                {if(obj.Zone=="roof")color=Color.Lerp(color,new Color(.14f,.64f,.28f),(float)m.Configuration.GreenRoofFraction);if(obj.Zone=="road")color=Color.Lerp(color,new Color(.1f,.65f,.61f),(float)m.Configuration.PermeableFraction);if(obj.Zone=="collection"&&m.Configuration.GardenCapacityM3>0)color=new Color(.2f,.65f,.6f);}
                if(Overlay==DistrictOverlay.Water)
                {double ratio=obj.Zone=="collection"?m.Result.StorageM3/150:obj.Zone=="roof"?m.Zones[1].RetainedM3/(DistrictHydrology.RoofArea*.02):0;color=Color.Lerp(new Color(.85f,.92f,.95f),new Color(.06f,.35f,.9f),(float)ratio);}
                if(Overlay==DistrictOverlay.Runoff)
                {double runoff=0,input=0;foreach(var z in m.Zones)if(z.Id==obj.Zone||(obj.Zone=="roof"&&z.Id=="green-roof")||(obj.Zone=="road"&&z.Id=="permeable")){runoff+=z.RunoffM3;input+=z.RainM3;}color=Color.Lerp(new Color(.94f,.92f,.86f),new Color(.92f,.38f,.08f),input>0?(float)(runoff/input):0);}
                if(Selected==obj||HighlightedZone==obj.Zone||(HighlightedZone=="green-roof"&&obj.Zone=="roof")||(HighlightedZone=="permeable"&&obj.Zone=="road"))color=Color.Lerp(color,Color.yellow,.65f);
                block.SetColor("_BaseColor",color);obj.Renderer.SetPropertyBlock(block);
            }
            foreach(var path in paths)path.enabled=Overlay==DistrictOverlay.Connections||Overlay==DistrictOverlay.Runoff;
        }
        void OnDestroy(){if(pathMaterial!=null)Destroy(pathMaterial);}
    }
}
