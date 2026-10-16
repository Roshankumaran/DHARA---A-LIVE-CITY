using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace Dhara
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        Vector3 focus, pathStart, pathEnd, pathLookStart, transitionFocusEnd, moveVelocity;
        float yaw=-38, pitch=44, distance=145, framedAspect, transitionTime, transitionDuration;
        float transitionYawEnd, transitionPitchEnd, transitionDistanceEnd;
        bool transitioning, streetView, tour, paused, sweeping;
        bool inputActive=true;
        public bool InputActive => inputActive;
        public void ReleaseInput(){inputActive=false;moveVelocity=Vector3.zero;Cursor.lockState=CursorLockMode.None;}
        public void CaptureInput(){inputActive=true;}
        public void ResumeFromViewport(){if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);CaptureInput();}
        float sweepTime, sweepDuration, sweepYawStart, sweepYawEnd, tourSegmentTime;
        int tourSegment;
        readonly List<Bounds> walkingObstacles=new List<Bounds>();
        readonly List<Vector3> tourPoints=new List<Vector3>();
        public bool IsTransitioning => transitioning;
        public bool TourRunning => tour;
        public bool TourPaused => paused;
        public bool IsExploring => streetView && !transitioning && !tour;
        public float HorizontalAngle => yaw;
        public float VerticalAngle => pitch;
        public float OverviewDistance => distance;
        public IReadOnlyList<Vector3> TourWaypoints => tourPoints;
        public string NavigationStatus => tour ? (paused?"TOUR PAUSED":"TOUR IN PROGRESS") : transitioning?"CAMERA TRANSITION":streetView?"FREE ROAM / STREET LEVEL":"CITY OVERVIEW";
        public void CacheWalkingObstacles()
        {
            walkingObstacles.Clear();
            foreach(var mesh in transform.parent.GetComponentsInChildren<MeshRenderer>())
                if(mesh.name.StartsWith("Building ") || mesh.name=="Stone ground floor" || mesh.name=="Trunk" || mesh.name=="Streetlight" || mesh.name=="Park bench seat" || mesh.name=="Detention basin rim")
                {var bounds=mesh.bounds;bounds.Expand(new Vector3(1.2f,0,1.2f));walkingObstacles.Add(bounds);}
        }
        public bool CanWalk(Vector3 position)
        {
            if(!Finite(position) || Mathf.Abs(position.x)>50.5f || Mathf.Abs(position.z)>44) return false;
            foreach(var b in walkingObstacles)if(position.x>=b.min.x&&position.x<=b.max.x&&position.z>=b.min.z&&position.z<=b.max.z)return false;
            return true;
        }
        static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.z);
        public void EnterExplorer(Vector3 position)
        {
            CacheWalkingObstacles();inputActive=true;tour=paused=transitioning=sweeping=false;streetView=true;moveVelocity=Vector3.zero;
            if(!CanWalk(position))position=new Vector3(0,2.2f,-34);
            position.y=2.2f;transform.position=position;yaw=0;pitch=0;transform.rotation=Quaternion.identity;
        }
        public void LookExplorer(Vector2 delta,bool blocked=false)
        {
            if(!IsExploring||!inputActive||blocked||!Finite(new Vector3(delta.x,delta.y,0)))return;
            yaw=Mathf.Repeat(yaw+delta.x*.2f,360);pitch=Mathf.Clamp(pitch-delta.y*.2f,-65,65);
            transform.rotation=Quaternion.Euler(pitch,yaw,0);
        }
        public void MoveExplorer(Vector2 input,float seconds,bool fast=false,bool blocked=false)
        {
            if(!IsExploring||!inputActive||blocked||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds)||!Finite(new Vector3(input.x,input.y,0))){moveVelocity=Vector3.zero;return;}
            float dt=Mathf.Min(seconds,.1f); input=Vector2.ClampMagnitude(input,1);
            Vector3 target=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y)*(fast?10:5);
            // Trapezoidal integration keeps acceleration independent of render subdivisions.
            Vector3 nextVelocity=Vector3.MoveTowards(moveVelocity,target,30*dt);
            Vector3 movement=(moveVelocity+nextVelocity)*.5f*dt;moveVelocity=nextVelocity;
            int steps=Mathf.Max(1,Mathf.CeilToInt(movement.magnitude/.15f));var position=transform.position;
            if(!CanWalk(position)){EnterExplorer(new Vector3(0,2.2f,-34));return;}
            for(int i=0;i<steps;i++)
            {
                var next=position+new Vector3(movement.x/steps,0,0);if(CanWalk(next))position=next;
                next=position+new Vector3(0,0,movement.z/steps);if(CanWalk(next))position=next;
            }
            position.y=2.2f;transform.position=position;focus=position+transform.forward*distance;
        }
        public void Zoom(float scroll){if(streetView||tour||transitioning||float.IsNaN(scroll)||float.IsInfinity(scroll))return;distance=Mathf.Clamp(distance-scroll*.045f,12,300);}
        void ApplyOrbit(){transform.rotation=Quaternion.Euler(pitch,yaw,0);var pos=focus-transform.forward*distance;pos.y=Mathf.Max(1.5f,pos.y);transform.position=pos;}
        public void Home()
        {
            transitioning = sweeping = tour = paused = false;
            moveVelocity=Vector3.zero;
            streetView = false;
            yaw = -38; pitch = 44;
            var view = GetComponent<Camera>();
            var meshes = transform.parent.GetComponentsInChildren<MeshRenderer>();
            if (meshes.Length == 0) return;
            Bounds bounds = meshes[0].bounds; foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
            focus = bounds.center; framedAspect = view.aspect;
            var inverse = Quaternion.Inverse(Quaternion.Euler(pitch, yaw, 0));
            // Center the actual projected district, rather than the empty upper half of its world AABB.
            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            foreach (var mesh in meshes)
                foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                        {
                            var point = inverse * (mesh.bounds.center - focus + Vector3.Scale(mesh.bounds.extents, new Vector3(x, y, z)));
                            low = Mathf.Min(low, point.y); high = Mathf.Max(high, point.y);
                        }
            focus += Quaternion.Euler(pitch, yaw, 0) * Vector3.up * ((low + high) * .5f - 8f);
            float tan = Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad) * .91f;
            float required = 0;
            foreach (var mesh in meshes)
                foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                        {
                            var point = inverse * (mesh.bounds.center - focus + Vector3.Scale(mesh.bounds.extents, new Vector3(x, y, z)));
                            required = Mathf.Max(required, Mathf.Abs(point.x) / (tan * view.aspect) - point.z, Mathf.Abs(point.y) / tan - point.z);
                        }
            distance = Mathf.Clamp(required, 60, 300);
        }

        public void ToOverview()
        {
            streetView = false; tour=false; paused=false;
            // Cache overview position before transition.
            float overviewYaw = -38, overviewPitch = 44;
            var view = GetComponent<Camera>();
            var meshes = transform.parent.GetComponentsInChildren<MeshRenderer>();
            if (meshes.Length == 0) return;
            Bounds bounds = meshes[0].bounds; foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
            Vector3 overviewFocus = bounds.center;
            var inverse = Quaternion.Inverse(Quaternion.Euler(overviewPitch, overviewYaw, 0));
            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            foreach (var mesh in meshes)
                foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                        {
                            var point = inverse * (mesh.bounds.center - overviewFocus + Vector3.Scale(mesh.bounds.extents, new Vector3(x, y, z)));
                            low = Mathf.Min(low, point.y); high = Mathf.Max(high, point.y);
                        }
            overviewFocus += Quaternion.Euler(overviewPitch, overviewYaw, 0) * Vector3.up * ((low + high) * .5f - 8f);
            float tan = Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad) * .91f;
            float required = 0;
            foreach (var mesh in meshes)
                foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                        {
                            var point = inverse * (mesh.bounds.center - overviewFocus + Vector3.Scale(mesh.bounds.extents, new Vector3(x, y, z)));
                            required = Mathf.Max(required, Mathf.Abs(point.x) / (tan * view.aspect) - point.z, Mathf.Abs(point.y) / tan - point.z);
                        }
            float overviewDistance = Mathf.Clamp(required, 60, 300);
            BeginTransition(overviewFocus, overviewYaw, overviewPitch, overviewDistance, 2f);
        }


        public void ToStreet(Vector3? targetFocus=null)
        {inputActive=true;tour=paused=false;streetView=true;CacheWalkingObstacles();moveVelocity=Vector3.zero;BeginTransition(new Vector3(0,2.2f,0),0,0,34,5);}
        public void StartTour()
        {
            Home();ApplyOrbit();CacheWalkingObstacles();tourPoints.Clear();tourPoints.Add(transform.position);
            // Roads and open frontage lanes of the actual currently generated district.
            tourPoints.Add(new Vector3(0,32,-34));tourPoints.Add(new Vector3(0,2.2f,-34));
            tourPoints.Add(new Vector3(0,2.2f,0));tourPoints.Add(new Vector3(-50,2.2f,0));
            tourPoints.Add(new Vector3(-50,2.2f,-32));tourPoints.Add(new Vector3(0,2.2f,-32));
            tourPoints.Add(new Vector3(50,2.2f,-32));tourPoints.Add(new Vector3(50,2.2f,0));
            tourPoints.Add(new Vector3(50,2.2f,32));tourPoints.Add(new Vector3(0,2.2f,32));
            tourPoints.Add(new Vector3(-50,2.2f,32));tourPoints.Add(new Vector3(-50,2.2f,0));
            tourPoints.Add(new Vector3(0,2.2f,0));tourPoints.Add(new Vector3(0,2.2f,40));
            tourPoints.Add(new Vector3(0,32,40));tourPoints.Add(tourPoints[0]);
            tour=true;paused=false;tourSegment=0;tourSegmentTime=0;
        }
        public void PauseTour(){if(tour)paused=true;}
        public void ResumeTour(){if(tour)paused=false;}
        public void RestartTour()=>StartTour();
        public void CancelTour(){Home();ApplyOrbit();}
        public void StartSweep(float duration=20){sweeping=true;sweepTime=0;sweepDuration=duration;sweepYawStart=yaw;sweepYawEnd=yaw+360;}
        public void StopSweep(){sweeping=false;}
        static float SmoothStep(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
        Vector3 PathPoint(float t)
        {
            var a=new Vector3(pathStart.x,Mathf.Max(32,pathStart.y),pathStart.z);
            var b=new Vector3(pathEnd.x,Mathf.Max(32,pathEnd.y),pathEnd.z);
            if(t<.25f)return Vector3.Lerp(pathStart,a,SmoothStep(t*4));
            if(t<.7f)return Vector3.Lerp(a,b,SmoothStep((t-.25f)/.45f));
            return Vector3.Lerp(b,pathEnd,SmoothStep((t-.7f)/.3f));
        }
        void BeginTransition(Vector3 targetFocus,float targetYaw,float targetPitch,float targetDistance,float duration)
        {
            sweeping=false;pathStart=transform.position;pathEnd=targetFocus-Quaternion.Euler(targetPitch,targetYaw,0)*Vector3.forward*targetDistance;
            pathLookStart=transform.position+transform.forward*20;transitioning=true;transitionTime=0;transitionDuration=Mathf.Max(.3f,duration);
            transitionFocusEnd=targetFocus;transitionYawEnd=targetYaw;transitionPitchEnd=targetPitch;transitionDistanceEnd=targetDistance;
        }
        public void AdvanceCamera(float seconds)
        {
            if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            if(tour)
            {
                if(paused)return;
                float remaining=seconds;
                while(tour&&remaining>0)
                {
                    var from=tourPoints[tourSegment];var to=tourPoints[tourSegment+1];
                    float duration=Mathf.Max(2,Vector3.Distance(from,to)/9);
                    float take=Mathf.Min(remaining,duration-tourSegmentTime);tourSegmentTime+=take;remaining-=take;
                    float t=SmoothStep(tourSegmentTime/duration);transform.position=Vector3.Lerp(from,to,t);
                    Vector3 heading=to-from;
                    if(heading.sqrMagnitude>.01f){var desired=Quaternion.LookRotation(heading);transform.rotation=Quaternion.Slerp(transform.rotation,desired,1-Mathf.Exp(-take*3));}
                    if(tourSegmentTime>=duration-.00001f){tourSegment++;tourSegmentTime=0;if(tourSegment>=tourPoints.Count-1){tour=false;Home();ApplyOrbit();}}
                }
                return;
            }
            if(transitioning)
            {
                transitionTime+=seconds;float t=Mathf.Clamp01(transitionTime/transitionDuration);transform.position=PathPoint(t);
                var look=Vector3.Lerp(pathLookStart,transitionFocusEnd,SmoothStep(t));if((look-transform.position).sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(look-transform.position);
                if(t<1)return;
                transitioning=false;focus=transitionFocusEnd;yaw=transitionYawEnd;pitch=transitionPitchEnd;distance=transitionDistanceEnd;
            }
            if(streetView)return;
            if(sweeping){sweepTime+=seconds;yaw=Mathf.Lerp(sweepYawStart,sweepYawEnd,sweepTime/Mathf.Max(.3f,sweepDuration));if(sweepTime>=sweepDuration)sweeping=false;}
            ApplyOrbit();
        }
        bool UiFocused()
        {
            if(EventSystem.current==null)return false;
            var selected=EventSystem.current.currentSelectedGameObject;
            return EventSystem.current.IsPointerOverGameObject() || selected!=null&&(selected.GetComponent<UnityEngine.UI.Slider>()!=null||selected.GetComponent<TMPro.TMP_InputField>()!=null);
        }
        bool appViewportContains(Vector2 pointer)=>GetComponent<Camera>().pixelRect.Contains(pointer);
        void LateUpdate()
        {
            var keys=Keyboard.current;
            var clickMouse=Mouse.current;
            if(clickMouse!=null&&clickMouse.leftButton.wasPressedThisFrame&&appViewportContains(clickMouse.position.ReadValue())&&(EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject()))ResumeFromViewport();
            bool ui=UiFocused();
            if(keys!=null&&keys.escapeKey.wasPressedThisFrame){if(tour)CancelTour();else if(streetView){ReleaseInput();}else Home();return;}
            if(keys!=null&&keys.homeKey.wasPressedThisFrame)Home();
            if(!ui&&keys!=null){if(keys.tKey.wasPressedThisFrame){if(tour)CancelTour();else StartTour();}if(keys.digit1Key.wasPressedThisFrame)ToOverview();if(keys.digit2Key.wasPressedThisFrame)ToStreet();if(keys.digit3Key.wasPressedThisFrame)EnterExplorer(transform.position);}
            AdvanceCamera(Time.unscaledDeltaTime);if(tour||transitioning)return;
            var mouse=Mouse.current;
            if(mouse!=null&&!ui&&mouse.leftButton.wasPressedThisFrame&&EventSystem.current!=null){EventSystem.current.SetSelectedGameObject(null);CaptureInput();}
            if(streetView)
            {
                if(mouse!=null&&mouse.rightButton.isPressed)LookExplorer(mouse.delta.ReadValue(),ui);
                Vector2 input=Vector2.zero;
                if(keys!=null){input.x=(keys.dKey.isPressed||keys.rightArrowKey.isPressed?1:0)-(keys.aKey.isPressed||keys.leftArrowKey.isPressed?1:0);input.y=(keys.wKey.isPressed||keys.upArrowKey.isPressed?1:0)-(keys.sKey.isPressed||keys.downArrowKey.isPressed?1:0);}
                MoveExplorer(input,Time.unscaledDeltaTime,keys!=null&&(keys.leftShiftKey.isPressed||keys.rightShiftKey.isPressed),ui||!Application.isFocused);return;
            }
            if(Mathf.Abs(GetComponent<Camera>().aspect-framedAspect)>.03f)Home();
            if(mouse!=null&&!ui){if(mouse.rightButton.isPressed){var d=mouse.delta.ReadValue();yaw+=d.x*.2f;pitch=Mathf.Clamp(pitch-d.y*.2f,8,82);sweeping=false;}if(mouse.middleButton.isPressed){var d=mouse.delta.ReadValue();focus+=Quaternion.Euler(0,yaw,0)*new Vector3(-d.x,0,-d.y)*distance*.001f;}Zoom(mouse.scroll.ReadValue().y);}
            if(keys!=null&&!ui){var m=new Vector3((keys.dKey.isPressed?1:0)-(keys.aKey.isPressed?1:0),0,(keys.wKey.isPressed?1:0)-(keys.sKey.isPressed?1:0));focus+=Quaternion.Euler(0,yaw,0)*m*Time.unscaledDeltaTime*22;}
            focus.x=Mathf.Clamp(focus.x,-55,55);focus.z=Mathf.Clamp(focus.z,-48,48);ApplyOrbit();
        }
        void OnApplicationFocus(bool focused){if(!focused)ReleaseInput();}
        void OnDisable(){Cursor.lockState=CursorLockMode.None;moveVelocity=Vector3.zero;}
    }
}
