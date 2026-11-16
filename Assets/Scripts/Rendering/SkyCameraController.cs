using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SkyCameraController : MonoBehaviour
{
    public float minFov = 10f;
    public float maxFov = 90f;
    public float zoomStep = 5f;

    Camera cam;
    float yaw, pitch;
    Vector3 lastMouse;

    void Awake()
    {
        cam = GetComponent<Camera>();
        yaw = transform.eulerAngles.y;
        pitch = 0f;
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minFov, maxFov);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            lastMouse = Input.mousePosition;

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMouse;
            lastMouse = Input.mousePosition;
            float degPerPixel = cam.fieldOfView / Screen.height;
            yaw -= delta.x * degPerPixel;
            pitch = Mathf.Clamp(pitch + delta.y * degPerPixel, -90f, 90f);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f)
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomStep, minFov, maxFov);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
