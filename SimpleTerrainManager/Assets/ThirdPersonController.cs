using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : NetworkBehaviour
{
    CharacterController controller;
    Vector3 moveDirection = Vector3.zero;

    Transform cameraTarget;
    float cameraPitch = 40.0f;
    float cameraYaw = 0;
    float cameraDistance = 5.0f;
    bool lerpYaw = false;
    bool lerpDistance = false;

    public float cameraPitchSpeed = 2.0f;
    public float cameraPitchMin = -10.0f;
    public float cameraPitchMax = 80.0f;
    public float cameraYawSpeed = 5.0f;
    public float cameraDistanceSpeed = 5.0f;
    public float cameraDistanceMin = 2.0f;
    public float cameraDistanceMax = 12.0f;
    public float moveDirectionSpeed = 6.0f;
    public float turnSpeed = 3.0f;
    public float jumpSpeed = 8.0f;
    public float gravitySpeed = 20.0f;

    public void Start()
    {
        if (isLocalPlayer || isServer)
            StartCoroutine(reportLocation());
    }

    // Needed to load correct terrain
    IEnumerator reportLocation()
    {
        while (true)
        {
            TerrainManager.reportLocation(transform.position);

            yield return new WaitForSeconds(1.0f);
        }
    }

    public override void OnStartLocalPlayer()
    {
        GetComponent<MeshRenderer>().material.color = Color.blue;

        controller = GetComponent<CharacterController>();
        cameraTarget = transform; // Camera will always face this
    }

    // Fixme: save all Inputs in Update, then look at saved values here and in FixedUpdate
    // Fixme: camera code should be in its own script probably
    public void LateUpdate()
    {
        if (!isLocalPlayer)
            return;

        // If mouse button down then allow user to look around
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            cameraPitch += Input.GetAxis("Mouse Y") * cameraPitchSpeed;
            cameraPitch = Mathf.Clamp(cameraPitch, cameraPitchMin, cameraPitchMax);
            cameraYaw += Input.GetAxis("Mouse X") * cameraYawSpeed;
            cameraYaw = cameraYaw % 360.0f;
            lerpYaw = false;
        }
        else
        {
            // If moving then make camera follow
            if (lerpYaw)
                cameraYaw = Mathf.LerpAngle(cameraYaw, cameraTarget.eulerAngles.y, 5.0f * Time.deltaTime);
        }

        // Zoom
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            cameraDistance -= Input.GetAxis("Mouse ScrollWheel") * cameraDistanceSpeed;
            cameraDistance = Mathf.Clamp(cameraDistance, cameraDistanceMin, cameraDistanceMax);
            lerpDistance = false;
        }

        // Calculate camera position
        Vector3 newCameraPosition = cameraTarget.position + (Quaternion.Euler(cameraPitch, cameraYaw, 0) * Vector3.back * cameraDistance);

        // Does new position put us inside anything?
        RaycastHit hitInfo;
        if (Physics.Linecast(cameraTarget.position, newCameraPosition, out hitInfo))
        {
            newCameraPosition = hitInfo.point;
            lerpDistance = true;
        }
        else
        {
            if (lerpDistance)
            {
                float newCameraDistance = Mathf.Lerp(Vector3.Distance(cameraTarget.position, Camera.main.transform.position), cameraDistance, 5.0f * Time.deltaTime);
                newCameraPosition = cameraTarget.position + (Quaternion.Euler(cameraPitch, cameraYaw, 0) * Vector3.back * newCameraDistance);
            }
        }

        Camera.main.transform.position = newCameraPosition;
        Camera.main.transform.LookAt(cameraTarget.position);
    }

    // Fixme: add running
    public void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical");

        // Have camera follow if moving
        if (!lerpYaw && (h != 0 || v != 0))
            lerpYaw = true;

        if (Input.GetMouseButton(1))
            transform.rotation = Quaternion.Euler(0, cameraYaw, 0); // Face camera
        else
            transform.Rotate(0, h * turnSpeed, 0); // Turn left/right

        // Only allow user control when on ground
        if (controller.isGrounded)
        {
            if (Input.GetMouseButton(1))
                moveDirection = new Vector3(h, 0, v); // Strafe
            else
                moveDirection = Vector3.forward * v; // Move forward/backward

            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= moveDirectionSpeed;
            if (Input.GetButton("Jump"))
                moveDirection.y = jumpSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
                moveDirection *= 10;
        }

        moveDirection.y -= gravitySpeed * Time.deltaTime; // Apply gravity
        controller.Move(moveDirection * Time.deltaTime);
    }
}
