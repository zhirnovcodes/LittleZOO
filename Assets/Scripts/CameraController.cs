using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform Planet;
    [SerializeField] private Transform CameraHandle;
    [SerializeField] private Camera Camera;
    [SerializeField] private float MinOffset = 1f;
    [SerializeField] private float MaxOffset = 100f;
    [SerializeField] private float ZoomSpeed = 10f;
    [SerializeField] private float ScrollPower = 10f;

    private Vector3 ScreenGrabPoint;
    private Quaternion HandleGrabRotation;

    private float MinDistance;
    private float MaxDistance;
    private float NextDistance;

    private void Start()
    {
        var planetScale = Planet.localScale.x;
        var planetRadius = planetScale / 2f;
        var cameraNearPlane = Camera.nearClipPlane;

        MinDistance = planetRadius + MinOffset + cameraNearPlane;
        MaxDistance = planetRadius + MaxOffset + cameraNearPlane;
    }

    // Update is called once per frame
    void Update()
    {

        var scrollDelta = Input.mouseScrollDelta.y;

        var cameraPosition = Camera.transform.localPosition;
        var distance = -cameraPosition.z;

        if (scrollDelta != 0)
        {
            var speed = ZoomSpeed * Time.deltaTime * scrollDelta;

            NextDistance = Mathf.Clamp( distance + speed, MinDistance, MaxDistance);
            Camera.transform.localPosition = new Vector3(0, 0, -Mathf.Lerp(distance, NextDistance, ZoomSpeed * Time.deltaTime));
        }

        if (Mathf.Abs(distance - NextDistance) <= 0.01f)
        {
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            ScreenGrabPoint = Input.mousePosition;
            HandleGrabRotation = CameraHandle.transform.rotation;
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            var screenPosition = Input.mousePosition;
            var drag = screenPosition - ScreenGrabPoint;
            var axis = new Vector3(-drag.y, drag.x, 0);
            var euler = axis * ScrollPower;
            CameraHandle.transform.rotation = HandleGrabRotation * Quaternion.Euler(euler);
            /*
            var angle = ScrollSpeed * Time.deltaTime * Mathf.Clamp01(drag.magnitude);

            var rotation = Quaternion.AngleAxis(angle, axis);

            var planetPosition = Planet.position;

            var direction = cameraPosition - planetPosition;
            var directionNew = rotation * direction;
            var positionNew = planetPosition + directionNew;

            Camera.transform.position = positionNew;
            Camera.transform.LookAt(Planet, Vector3.up);
            */
        }
    }
}
