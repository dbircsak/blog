using MLAPI;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using System.Collections.Generic;
using UnityEngine;

// Central place to declare classes
// Tells MLAPI how to send custom types over wire
public class CustomTypes : NetworkedBehaviour
{
    // Capture client input
    public class PlayerCmd
    {
        public bool mouseButton0;
        public bool mouseButton1;
        public bool jumpButton;
        public bool leftShiftKey;
        public int horizontal;
        public int vertical;
    }

    // Sent from client to server
    public class PlayerCmdSet
    {
        static public readonly uint Max = 5;
        public uint cmdIndex = 0;
        public PlayerCmd[] playerCmds = new PlayerCmd[Max];
    }

    // Sent from server to clients
    public class PlayerState
    {
        public ulong clientId;
        public Vector3 position;
        public Quaternion rotation;
    }

    // Both client and server use these to create local representations of players
    public class PlayerObject
    {
        public GameObject obj;
        public CharacterController controller;
        public Vector3 moveDirection;
        public float horizontal;
        public float vertical;
    }

    public class PlayerObjectDict
    {
        public Dictionary<ulong, PlayerObject> playerObjects = new Dictionary<ulong, PlayerObject>();

        public void Clear()
        {
            if (playerObjects.Count == 0)
                return;
            foreach (var item in playerObjects)
                Destroy(item.Value.obj);
            playerObjects.Clear();
        }

        public void Remove(ulong clientId)
        {
            if (playerObjects.ContainsKey(clientId))
            {
                Destroy(playerObjects[clientId].obj);
                playerObjects.Remove(clientId);
            }
        }
    }

    public class SeqCheck
    {
        public uint seq = 0;
        public uint miss = 0;

        // If seq new then assign and return true
        // If old then accept it after third time (show mercy)
        public bool AssignNew(uint newSeq)
        {
            // Don't use for now
            //if (newSeq <= seq && miss < 3)
            //{
            //    //Debug.Log($"{newSeq} seq less than {seq}");
            //    miss++;
            //    return false;
            //}
            seq = newSeq;
            miss = 0;
            return true;
        }
    }

    // Client calls this
    void SerializePlayerCmds(System.IO.Stream stream, PlayerCmd[] instance)
    {
        using (PooledBitWriter writer = PooledBitWriter.Get(stream))
        {
            for (int i = 0; i < PlayerCmdSet.Max; i++)
            {
                writer.WriteBool(instance[i].mouseButton0);
                writer.WriteBool(instance[i].mouseButton1);
                writer.WriteBool(instance[i].jumpButton);
                writer.WriteBool(instance[i].leftShiftKey);
                writer.WriteInt32Packed(instance[i].horizontal);
                writer.WriteInt32Packed(instance[i].vertical);
            }
        }
    }

    // Server calls this
    PlayerCmd[] DeserializePlayerCmds(System.IO.Stream stream)
    {
        // FIXME: Not good for memory?
        PlayerCmd[] playerCmds = new PlayerCmd[PlayerCmdSet.Max];
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            for (int i = 0; i < PlayerCmdSet.Max; i++)
            {
                playerCmds[i] = new PlayerCmd();
                playerCmds[i].mouseButton0 = reader.ReadBool();
                playerCmds[i].mouseButton1 = reader.ReadBool();
                playerCmds[i].jumpButton = reader.ReadBool();
                playerCmds[i].leftShiftKey = reader.ReadBool();
                playerCmds[i].horizontal = reader.ReadInt32Packed();
                playerCmds[i].vertical = reader.ReadInt32Packed();
            }
        }
        return playerCmds;
    }

    // Server calls this
    void SerializePlayerStates(System.IO.Stream stream, List<PlayerState> instance)
    {
        using (PooledBitWriter writer = PooledBitWriter.Get(stream))
        {
            writer.WriteInt32Packed(instance.Count);
            foreach (var item in instance)
            {
                writer.WriteUInt64Packed(item.clientId);
                writer.WriteVector3Packed(item.position);
                writer.WriteRotationPacked(item.rotation);
            }
        }
    }

    // Client calls this
    List<PlayerState> DeserializePlayerStates(System.IO.Stream stream)
    {
        // FIXME: Recreate list each time?
        List<PlayerState> playerStates = new List<PlayerState>();
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            int count = reader.ReadInt32Packed();
            for (int i = 0; i < count; i++)
            {
                PlayerState ps = new PlayerState();
                ps.clientId = reader.ReadUInt64Packed();
                ps.position = reader.ReadVector3Packed();
                ps.rotation = reader.ReadRotationPacked();
                playerStates.Add(ps);
            }
        }
        return playerStates;
    }

    public override void NetworkStart()
    {
        base.NetworkStart();

        // Tell MLAPI how to send player cmds and player states
        SerializationManager.RegisterSerializationHandlers(SerializePlayerCmds, DeserializePlayerCmds);
        SerializationManager.RegisterSerializationHandlers(SerializePlayerStates, DeserializePlayerStates);
    }
}
