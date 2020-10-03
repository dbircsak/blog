using MLAPI;
using MLAPI.Messaging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Receives player states and moves game objects based on those states
public class ClientPlayerObjects : NetworkedBehaviour
{
    // What is used when new player created
    public GameObject playerPrefab;
    // Where to start all players
    public GameObject playerStart;
    // Remember last player states received from server
    List<CustomTypes.PlayerState> playerStates = new List<CustomTypes.PlayerState>();
    // Remember all player states as gameObjects
    CustomTypes.PlayerObjectDict playerObjectDict = new CustomTypes.PlayerObjectDict();
    // Is it time to add/remove client objects?
    bool updatePlayerObjectDict = false;
    // How fast to move player pos/rot to true pos/rot
    readonly float lerpRate = 10.0f;

    void Update()
    {
        if (!IsClient)
        {
            playerObjectDict.Clear();
            return;
        }

        if (updatePlayerObjectDict)
        {
            // Does player state still have our client objects?
            foreach (ulong clientId in playerObjectDict.playerObjects.Keys.ToList())
            {
                if (!playerStates.Exists(x => x.clientId == clientId))
                    playerObjectDict.Remove(clientId);
            }

            // Create any new clients
            foreach (var item in playerStates)
            {
                if (!playerObjectDict.playerObjects.ContainsKey(item.clientId))
                {
                    // New player state so create game object
                    CustomTypes.PlayerObject playerObject = new CustomTypes.PlayerObject();
                    playerObject.obj = Instantiate(playerPrefab, playerStart.transform.position, playerStart.transform.rotation);
                    playerObject.obj.layer = LayerMask.NameToLayer("Client");
                    playerObject.obj.GetComponent<MeshRenderer>().material.color = Color.blue;
                    playerObject.obj.name = $"Client {playerObject.obj.name} {item.clientId}";
                    playerObject.controller = null; // Not used
                    playerObject.moveDirection = Vector3.zero;
                    playerObjectDict.playerObjects.Add(item.clientId, playerObject);

                    // If this is us then attach camera to obj
                    if (item.clientId == NetworkingManager.Singleton.LocalClientId)
                        ThirdPersonCamera.cameraTarget = playerObject.obj.transform;
                }
            }

            updatePlayerObjectDict = false;
        }

        // Loop through player states and move representing game objects
        foreach (var item in playerStates)
        {
            if (!playerObjectDict.playerObjects.ContainsKey(item.clientId))
                continue;

            // Get object representing this player state
            CustomTypes.PlayerObject playerObject = playerObjectDict.playerObjects[item.clientId];
            if (Vector3.Distance(playerObject.obj.gameObject.transform.position, item.position) > 2.0f)
                playerObject.obj.gameObject.transform.position = item.position;
            else
                playerObject.obj.gameObject.transform.position = Vector3.Lerp(playerObject.obj.gameObject.transform.position, item.position, lerpRate * Time.deltaTime); // This is a fake lerp
            playerObject.obj.gameObject.transform.rotation = Quaternion.Lerp(playerObject.obj.gameObject.transform.rotation, item.rotation, lerpRate * Time.deltaTime);
        }

    }

    CustomTypes.SeqCheck seqCheck = new CustomTypes.SeqCheck();

    [ClientRPC]
    public void ReceivePlayerStates(uint seq, List<CustomTypes.PlayerState> ps)
    {
        if (!seqCheck.AssignNew(seq))
            return;
        playerStates = ps;
        updatePlayerObjectDict = true;
    }
}
