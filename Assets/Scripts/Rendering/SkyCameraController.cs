using UnityEngine;
using UnityEngine.InputSystem;

public class SkyCameraController : MonoBehaviour
{
    float yaw;
    float pitch;
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x > 180f ? e.x - 360f : e.x;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.isPressed || mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            float degPerPixel = cam.fieldOfView / Screen.height;
            yaw += delta.x * degPerPixel;
            pitch -= delta.y * degPerPixel;
            pitch = Mathf.Clamp(pitch, -90f, 90f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
