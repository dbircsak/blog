using MLAPI;
using UnityEngine;

// Standard MMO camera
// Circle around player if left mouse button down
// Snap view behind player if left button released and player moving
// Zoom in/out with mouse wheel
// Face direction you look if right mouse button down
// Strafe if right mouse button down
// Move forward if both buttons down
public class ThirdPersonController : NetworkedBehaviour
{
    // Input
    bool inputMouseButton0;
    bool inputMouseButton1;
    bool inputJumpButton;
    bool inputLeftShiftKey;

    float inputHorizontal;
    float inputVertical;
    float inputMouseX;
    float inputMouseY;
    float inputMouseScrollWheel;

    // Controller
    CharacterController controller;
    Vector3 moveDirection = Vector3.zero;

    readonly float moveDirectionSpeed = 8.0f;
    readonly float turnSpeed = 3.0f;
    readonly float jumpSpeed = 8.0f;
    readonly float runSpeed = 10.0f;
    readonly float gravitySpeed = 20.0f;

    // Camera
    Transform cameraTarget;
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

    public override void NetworkStart()
    {
        base.NetworkStart();

        if (!IsLocalPlayer)
            return;

        controller = GetComponent<CharacterController>();
        controller.enabled = false; // Hack?
        controller.transform.position = GameObject.Find("PlayerStart").transform.position;
        controller.enabled = true;
    }

    void Start()
    {
        if (!IsLocalPlayer)
            return;

        GetComponent<MeshRenderer>().material.color = Color.blue;
        cameraTarget = transform; // Camera will always face this
    }

    // Remember input
    void Update()
    {
        inputMouseButton0 = Input.GetMouseButton(0);
        inputMouseButton1 = Input.GetMouseButton(1);
        inputJumpButton = Input.GetButton("Jump");
        inputLeftShiftKey = Input.GetKey(KeyCode.LeftShift);

        inputHorizontal = Input.GetAxis("Horizontal");
        inputVertical = Input.GetAxis("Vertical");
        inputMouseX = Input.GetAxis("Mouse X");
        inputMouseY = Input.GetAxis("Mouse Y");
        inputMouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");
    }

    // All controller stuff
    private void FixedUpdate()
    {
        if (!IsLocalPlayer)
            return;

        var h = inputHorizontal;
        var v = inputVertical;

        if (inputMouseButton1)
            transform.rotation = Quaternion.Euler(0, cameraYaw, 0); // Face camera
        else
            transform.Rotate(0, h * turnSpeed, 0); // Turn left/right

        // Only allow user control when on ground
        if (controller.isGrounded)
        {
            if (inputMouseButton1)
            {
                if (inputMouseButton0)
                    v = 1; // Move player forward if both buttons down
                moveDirection = new Vector3(h, 0, v); // Strafe
            }
            else
                moveDirection = Vector3.forward * v; // Move forward/backward

            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= moveDirectionSpeed;
            if (inputLeftShiftKey)
                moveDirection *= runSpeed;
            if (inputJumpButton)
                moveDirection.y = jumpSpeed;
        }

        moveDirection.y -= gravitySpeed * Time.deltaTime; // Apply gravity
        controller.Move(moveDirection * Time.deltaTime);
    }

    // All camera stuff
    private void LateUpdate()
    {
        if (!IsLocalPlayer)
            return;

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
