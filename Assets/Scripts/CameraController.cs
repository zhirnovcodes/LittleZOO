using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform Planet;
    [SerializeField] private Camera Camera;
    [SerializeField] private float MinOffset = 1f;
    [SerializeField] private float MaxOffset = 100f;
    [SerializeField] private float ZoomSpeed = 10f;
    [SerializeField] private float ScrollSpeed = 10f;

    private Vector3 ScreenGrabPoint;

    // Update is called once per frame
    void Update()
    {

        var scrollDelta = Input.mouseScrollDelta.y;

        if (scrollDelta != 0)
        {
            var planetPosition = Planet.position;
            var planetScale = Planet.localScale.x;
            var planetRadius = planetScale / 2f;
            var cameraPosition = Camera.transform.position;
            var cameraNearPlane = Camera.nearClipPlane;
            var direction = planetPosition - cameraPosition;

            var minDistance = planetRadius + MinOffset + cameraNearPlane;
            var maxDistance = planetRadius + MaxOffset + cameraNearPlane;
            var distance = direction.magnitude;
            var speed = ZoomSpeed * Time.deltaTime * scrollDelta;

            var newDistance = Mathf.Clamp( distance + speed, minDistance, maxDistance);
            var newPositon = planetPosition - direction / distance * newDistance;

            Camera.transform.position = newPositon;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            ScreenGrabPoint = Input.mousePosition;
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            var screenPosition = Input.mousePosition;
            var drag = screenPosition - ScreenGrabPoint;
            var axis = new Vector3(-drag.x, drag.y, 0);
            var angle = ScrollSpeed * Time.deltaTime * Mathf.Clamp01(drag.magnitude);

            var rotation = Quaternion.AngleAxis(angle, axis);

            var planetPosition = Planet.position;
            var cameraPosition = Camera.transform.position;

            var direction = cameraPosition - planetPosition;
            var directionNew = rotation * direction;
            var positionNew = planetPosition + directionNew;

            Camera.transform.position = positionNew;
            Camera.transform.LookAt(Planet, Vector3.up);
        }
    }
}
