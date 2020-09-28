using MLAPI;
using MLAPI.Messaging;
using System.Collections.Generic;
using UnityEngine;

// Client collects input and sends to server
// Server saves all client input
public class HandlePlayerCmds : NetworkedBehaviour
{
    // How fast to send out array of input to server
    static public readonly uint PlayerCmdsSendRate = 20; // Per second

    // Holds local input
    readonly CustomTypes.PlayerCmdSet playerCmdSet = new CustomTypes.PlayerCmdSet();
    float lastPlayerCmdRecordTime = 0.0f;

    ulong clientSeq = 0;
    Dictionary<ulong, ulong> serverSeqDict = new Dictionary<ulong, ulong>();

    void ClientDisconnected(ulong clientId)
    {
        if (playerCmdsDict.ContainsKey(clientId))
            playerCmdsDict.Remove(clientId);
        if (serverSeqDict.ContainsKey(clientId))
            serverSeqDict.Remove(clientId);
    }

    void Start()
    {
        for (int i = 0; i < CustomTypes.PlayerCmdSet.Max; i++)
            playerCmdSet.playerCmds[i] = new CustomTypes.PlayerCmd();
        NetworkingManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
    }

    void Update()
    {
        if (!IsClient || !NetworkedObject.IsSpawned)
            return;

        // If we send 5 inputs every 0.05 s, then record an input every 0.01 s and send on 5th record
        if (lastPlayerCmdRecordTime + (1.0f / PlayerCmdsSendRate / CustomTypes.PlayerCmdSet.Max) > Time.time)
            return;
        lastPlayerCmdRecordTime = Time.time;

        // Record input
        CustomTypes.PlayerCmd playerCmd = playerCmdSet.playerCmds[playerCmdSet.cmdIndex];
        playerCmd.mouseButton0 = Input.GetMouseButton(0);
        playerCmd.mouseButton1 = Input.GetMouseButton(1);
        playerCmd.jumpButton = Input.GetButton("Jump");
        playerCmd.leftShiftKey = Input.GetKey(KeyCode.LeftShift);
        playerCmd.horizontal = Input.GetAxis("Horizontal");
        playerCmd.vertical = Input.GetAxis("Vertical");
        playerCmdSet.cmdIndex++;

        // Send out after recording Max
        if (playerCmdSet.cmdIndex == CustomTypes.PlayerCmdSet.Max)
        {
            clientSeq++;
            // FIXME: Send if cmds are empty?
            InvokeServerRpc(ReceivePlayerCmds, clientSeq, playerCmdSet.playerCmds);
            playerCmdSet.cmdIndex = 0;
        }
    }

    // Remember what input clients send us, key is clientId
    static public Dictionary<ulong, CustomTypes.PlayerCmdSet> playerCmdsDict = new Dictionary<ulong, CustomTypes.PlayerCmdSet>();

    [ServerRPC(RequireOwnership = false)]
    public void ReceivePlayerCmds(ulong seq, CustomTypes.PlayerCmd[] playerCmds)
    {
        CustomTypes.PlayerCmdSet playerCmdSet;
        ulong clientId = ExecutingRpcSender;

        if (serverSeqDict.ContainsKey(clientId))
        {
            if (seq <= serverSeqDict[clientId])
            {
                //Debug.Log($"Client {clientId} cmd seq was {seq}, expected {serverSeqDict[clientId] + 1}. Dropping!");
                return;
            }
            if (seq != serverSeqDict[clientId] + 1)
            {
                //Debug.Log($"Client {clientId} cmd seq was {seq}, expected {serverSeqDict[clientId] + 1}");
            }
            serverSeqDict[clientId] = seq;
        }
        else
        {
            serverSeqDict.Add(clientId, seq);
        }

        if (playerCmdsDict.ContainsKey(clientId))
        {
            playerCmdSet = playerCmdsDict[clientId];
            playerCmdSet.cmdIndex = 0;
            playerCmdSet.playerCmds = playerCmds;
            playerCmdsDict[clientId] = playerCmdSet;
        }
        else
        {
            playerCmdSet = new CustomTypes.PlayerCmdSet();
            playerCmdSet.cmdIndex = 0;
            playerCmdSet.playerCmds = playerCmds;
            playerCmdsDict.Add(clientId, playerCmdSet);
        }

        //Debug.Log($"Recv cmds from {clientId}: h {playerCmdsDict[clientId].playerCmds[0].horizontal} v {playerCmdsDict[clientId].playerCmds[0].vertical}");
    }
}
