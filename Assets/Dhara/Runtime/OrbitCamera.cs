using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Dhara
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        Vector3 focus = Vector3.zero;
        float yaw = -38, pitch = 44, distance = 145;
        float framedAspect;
        public void Home()
        {
            yaw=-38; pitch=44;
            var view=GetComponent<Camera>();
            var meshes=transform.parent.GetComponentsInChildren<MeshRenderer>();
            if(meshes.Length==0) return;
            Bounds bounds=meshes[0].bounds;foreach(var mesh in meshes)bounds.Encapsulate(mesh.bounds);
            focus=bounds.center;framedAspect=view.aspect;
            var inverse=Quaternion.Inverse(Quaternion.Euler(pitch,yaw,0));
            // Center the actual projected district, rather than the empty upper half of its world AABB.
            float low=float.PositiveInfinity,high=float.NegativeInfinity;
            foreach(var mesh in meshes)
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                var point=inverse*(mesh.bounds.center-focus+Vector3.Scale(mesh.bounds.extents,new Vector3(x,y,z)));
                low=Mathf.Min(low,point.y);high=Mathf.Max(high,point.y);
            }
            focus+=Quaternion.Euler(pitch,yaw,0)*Vector3.up*((low+high)*.5f-8f);
            float tan=Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad)*.91f;
            float required=0;
            foreach(var mesh in meshes)
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                var point=inverse*(mesh.bounds.center-focus+Vector3.Scale(mesh.bounds.extents,new Vector3(x,y,z)));
                required=Mathf.Max(required,Mathf.Abs(point.x)/(tan*view.aspect)-point.z,Mathf.Abs(point.y)/tan-point.z);
            }
            distance=Mathf.Clamp(required,60,300);
        }
        void LateUpdate()
        {
            if(Mathf.Abs(GetComponent<Camera>().aspect-framedAspect)>.03f) Home();
            var mouse=Mouse.current; var keyboard=Keyboard.current;
            bool overUi=EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse != null && !overUi)
            {
                if(mouse.rightButton.isPressed) { var delta=mouse.delta.ReadValue(); yaw+=delta.x*.2f; pitch=Mathf.Clamp(pitch-delta.y*.2f,18,78); }
                if(mouse.middleButton.isPressed) { var delta=mouse.delta.ReadValue(); focus+=Quaternion.Euler(0,yaw,0)*new Vector3(-delta.x,0,-delta.y)*distance*.001f; }
                distance=Mathf.Clamp(distance-mouse.scroll.ReadValue().y*.045f,40,300);
            }
            if (keyboard != null && !overUi)
            {
                Vector3 movement=Vector3.zero;
                if(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) movement.z++;
                if(keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) movement.z--;
                if(keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) movement.x--;
                if(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) movement.x++;
                focus+=Quaternion.Euler(0,yaw,0)*movement*Time.unscaledDeltaTime*22;
                if(keyboard.homeKey.wasPressedThisFrame) Home();
            }
            focus.x=Mathf.Clamp(focus.x,-55,55); focus.z=Mathf.Clamp(focus.z,-45,45);
            transform.rotation=Quaternion.Euler(pitch,yaw,0);
            transform.position=focus-transform.forward*distance;
        }
    }
}
