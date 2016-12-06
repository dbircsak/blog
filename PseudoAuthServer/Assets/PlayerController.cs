using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour
{
    private Collider m_collider;
    private string m_log;

    void Start()
    {
        m_collider = GetComponent<Collider>();
    }

    // Get client input
    void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        var x = Input.GetAxis("Horizontal") * Time.deltaTime * 150.0f;
        var z = Input.GetAxis("Vertical") * Time.deltaTime * 3.0f;
        if (Input.GetButton("Jump")) // Move fast like we're cheating
            z *= 10.0f;

        transform.Rotate(0, x, 0);
        // Don't move into things
        Collider[] colliders = Physics.OverlapBox(transform.TransformPoint(0, 0, z), m_collider.bounds.extents);
        if (colliders.Length == 1) // We'll always detect our own collider
            transform.Translate(0, 0, z);
    }

    // Only called on one client
    // Call if client was found in illegal spot
    // Fix me: can this be abused?
    [TargetRpc]
    void TargetSetPosition(NetworkConnection target, Vector3 position)
    {
        Debug.Log("Setting position to " + position);
        transform.position = position;
    }

    // Only called by the server
    public bool ValidateMove(ref Vector3 position, ref Vector3 velocity, ref Quaternion rotation)
    {
        if (position == transform.position) // Don't bother if they didn't move
            return true;

        // Did they move too far away?
        // Fix me: what if we want them to transport?
        // Fix me: what if they are falling?
        if (Vector3.Distance(transform.position, position) > 0.5f)
        {
            // Tell client to move to last known good position
            TargetSetPosition(connectionToClient, transform.position);
            return false;
        }

        // Are they moving inside anything?
        // Fix me: what if they're trapped inside something already?
        Collider[] colliders = Physics.OverlapBox(position, m_collider.bounds.extents);
        if (colliders.Length > 1)
        {
            // Tell client to move to last known good position
            TargetSetPosition(connectionToClient, transform.position);
            return false;
        }

        // Everything's good, accept client move
        return true;
    }

    public override void OnStartServer()
    {
        GetComponent<NetworkTransform>().clientMoveCallback3D = ValidateMove;
    }

    public override void OnStartLocalPlayer()
    {
        GetComponent<MeshRenderer>().material.color = Color.blue;

        // Hook up for Debug messages
        Application.logMessageReceived += HandleLog;
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Log)
        {
            if (m_log.Split('\n').Length > 20)
                m_log = "";
            m_log += "\n" + condition;
        }
    }

    public void OnGUI()
    {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), m_log);
    }
}
