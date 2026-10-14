using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Dhara
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        Vector3 focus = Vector3.zero;
        float yaw = -38, pitch = 44, distance = 145;
        public void Home() { focus=Vector3.zero; yaw=-38; pitch=44; distance=145; }
        void LateUpdate()
        {
            var mouse=Mouse.current; var keyboard=Keyboard.current;
            bool overUi=EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse != null && !overUi)
            {
                if(mouse.rightButton.isPressed) { var delta=mouse.delta.ReadValue(); yaw+=delta.x*.2f; pitch=Mathf.Clamp(pitch-delta.y*.2f,18,78); }
                if(mouse.middleButton.isPressed) { var delta=mouse.delta.ReadValue(); focus+=Quaternion.Euler(0,yaw,0)*new Vector3(-delta.x,0,-delta.y)*distance*.001f; }
                distance=Mathf.Clamp(distance-mouse.scroll.ReadValue().y*.045f,40,210);
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
