using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform Planet;
    [SerializeField] private Transform CameraHandle;
    [SerializeField] private Transform CameraDepthHandle;
    [SerializeField] private Transform CameraRotationHandle;
    [SerializeField] private Camera Camera;
    [SerializeField] private float MinOffset = 1f;
    [SerializeField] private float MaxOffset = 100f;
    [SerializeField] private float ZoomSpeed = 10f;
    [SerializeField] private float ZoomPower = 10f;
    [SerializeField] private float ScrollPower = 10f;
    [SerializeField] private float ScrollSmoothness = 10f;
    [SerializeField] private float MaxRotationAngle = 50f;
    [SerializeField] private float RotationDepthStart = -30f;

    private Vector3 ScreenGrabPoint;
    private Vector3 LastRotation;
    private Quaternion HandleGrabRotation;

    private float MinDistance;
    private float MaxDistance;
    private float NextZ;

    private void Start()
    {
        var planetScale = Planet.localScale.x;
        var planetRadius = planetScale / 2f;
        var cameraNearPlane = Camera.nearClipPlane;

        MinDistance = planetRadius + MinOffset + cameraNearPlane;
        MaxDistance = planetRadius + MaxOffset + cameraNearPlane;
        NextZ = CameraDepthHandle.localPosition.z;
    }

    // Update is called once per frame
    void Update()
    {

        var scrollDelta = Input.mouseScrollDelta.y;

        var cameraPosition = CameraDepthHandle.localPosition;
        var distance = -cameraPosition.z;

        if (scrollDelta != 0)
        {
            var speed = ZoomPower * scrollDelta;

            NextZ = -Mathf.Clamp( distance + speed, MinDistance, MaxDistance);
        }

        if (Mathf.Abs(distance + NextZ) > 0.01f)
        {
            CameraDepthHandle.localPosition = new Vector3(0, 0, Mathf.Lerp(-distance, NextZ, Mathf.Clamp01( ZoomSpeed * Time.deltaTime )));
            var z = CameraDepthHandle.localPosition.z;
            var t = Mathf.InverseLerp(RotationDepthStart, -MinDistance, z);
            var angle = Mathf.Lerp(0, MaxRotationAngle, t);
            CameraRotationHandle.transform.localRotation = Quaternion.Euler(angle, 0, 0);
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            ScreenGrabPoint = Input.mousePosition;
            HandleGrabRotation = CameraHandle.transform.rotation;
            LastRotation = Vector3.zero;
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            var screenPosition = Input.mousePosition;
            var drag = screenPosition - ScreenGrabPoint;
            var axis = new Vector3(-drag.y, drag.x, 0);
            var euler = axis * ScrollPower;
            LastRotation = Vector3.Lerp(LastRotation, euler, Mathf.Clamp01( ScrollSmoothness * Time.deltaTime));
            CameraHandle.transform.rotation = HandleGrabRotation * Quaternion.Euler(LastRotation);
        }
    }
}
