using UnityEngine;

// Standard MMO camera
// Circle around player if left mouse button down
// Snap view behind player if left button released and player moving
// Zoom in/out with mouse wheel
// Face direction you look if right mouse button down
// Strafe if right mouse button down
// Move forward if both buttons down
public class ThirdPersonCamera : MonoBehaviour
{
    // Input
    bool inputMouseButton0;
    bool inputMouseButton1;

    float inputHorizontal;
    float inputVertical;
    float inputMouseX;
    float inputMouseY;
    float inputMouseScrollWheel;

    // Camera
    static public Transform cameraTarget; // Set in another ClientPlayerObjects
    float cameraPitch = 40.0f;
    float cameraYaw = 0.0f;
    float cameraDistance = 5.0f;

    readonly float cameraPitchSpeed = 2.0f;
    readonly float cameraPitchMin = -10.0f;
    readonly float cameraPitchMax = 80.0f;
    readonly float cameraYawSpeed = 5.0f;
    readonly float cameraDistanceSpeed = 5.0f;
    readonly float cameraDistanceMin = 2.0f;
    readonly float cameraDistanceMax = 20.0f;

    bool lerpYaw = false;
    readonly float lerpYawSpeed = 10.0f;
    bool lerpDistance = false;
    readonly float lerpDistanceSpeed = 10.0f;

    // Remember input
    void Update()
    {
        inputMouseButton0 = Input.GetMouseButton(0);
        inputMouseButton1 = Input.GetMouseButton(1);

        inputHorizontal = Input.GetAxis("Horizontal");
        inputVertical = Input.GetAxis("Vertical");
        inputMouseX = Input.GetAxis("Mouse X");
        inputMouseY = Input.GetAxis("Mouse Y");
        inputMouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");
    }

    private void LateUpdate()
    {
        if (cameraTarget == null)
        {
            Camera.main.transform.position = new Vector3(0.0f, 1.0f, -10.0f);
            Camera.main.transform.rotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
            return;
        }

        // If mouse down then allow user to look around
        if (inputMouseButton0 || inputMouseButton1)
        {
            cameraPitch += inputMouseY * cameraPitchSpeed;
            cameraPitch = Mathf.Clamp(cameraPitch, cameraPitchMin, cameraPitchMax);
            cameraYaw += inputMouseX * cameraYawSpeed;
            cameraYaw %= 360.0f;
            lerpYaw = false;
        }
        else
        {
            // Have camera follow if moving
            // Note: keep inside this else so you can turn and move at same time
            if (inputHorizontal != 0 || inputVertical != 0)
                lerpYaw = true;
            else
                lerpYaw = false;
        }
        if (lerpYaw)
            // Fake way to Lerp but I don't care
            cameraYaw = Mathf.LerpAngle(cameraYaw, cameraTarget.eulerAngles.y, lerpYawSpeed * Time.deltaTime) % 360.0f;

        // Distance
        if (inputMouseScrollWheel != 0)
        {
            cameraDistance -= inputMouseScrollWheel * cameraDistanceSpeed;
            cameraDistance = Mathf.Clamp(cameraDistance, cameraDistanceMin, cameraDistanceMax);
            lerpDistance = false;
        }

        // Calculate camera position
        Vector3 newCameraPosition = cameraTarget.position + (Quaternion.Euler(cameraPitch, cameraYaw, 0) * Vector3.back * cameraDistance);

        // Does new position put us inside anything?
        if (Physics.Linecast(cameraTarget.position, newCameraPosition, out RaycastHit hitInfo))
        {
            newCameraPosition = hitInfo.point;
            lerpDistance = true;
        }
        else
        {
            if (lerpDistance)
            {
                // Fake way to Lerp but I don't care
                float newCameraDistance = Mathf.Lerp(Vector3.Distance(cameraTarget.position, Camera.main.transform.position), cameraDistance, lerpDistanceSpeed * Time.deltaTime);
                newCameraPosition = cameraTarget.position + (Quaternion.Euler(cameraPitch, cameraYaw, 0) * Vector3.back * newCameraDistance);
            }
        }

        Camera.main.transform.position = newCameraPosition;
        Camera.main.transform.LookAt(cameraTarget.position);
    }
}
