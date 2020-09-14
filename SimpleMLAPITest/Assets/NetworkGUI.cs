using MLAPI;
using MLAPI.Profiling;
using System.Collections;
using System.Linq;
using UnityEngine;

public class NetworkGUI : MonoBehaviour
{
    readonly int profilerHistoryLength = 100;

    struct byteCountStruct
    {
        public long send;
        public long receive;
    }
    static readonly int byteCountArrayMax = 10;
    byteCountStruct[] byteCountArray = new byteCountStruct[byteCountArrayMax];

    // Run every 1/10 sec and store 10 for one sec of data
    IEnumerator calcByteCount()
    {
        int lastFrameCount = 0;
        bool historyTooSmall;
        byteCountStruct byteCount = new byteCountStruct();
        int byteCountArrayIndex = 0;
        while (true)
        {
            byteCount.send = 0;
            byteCount.receive = 0;
            if (lastFrameCount != 0 && NetworkProfiler.IsRunning && NetworkProfiler.Ticks != null && NetworkProfiler.Ticks.Count == profilerHistoryLength)
            {
                historyTooSmall = true;
                for (int i = 0; i < NetworkProfiler.Ticks.Count; i++)
                {
                    ProfilerTick tick = NetworkProfiler.Ticks.ElementAt(i);
                    // Only count ticks that happened since last
                    if (tick.Frame < lastFrameCount)
                    {
                        // If we have something before last frame count then we have a big enough buffer
                        historyTooSmall = false;
                        continue;
                    }
                    byteCount.send += tick.Events.Where(n => n.EventType == TickType.Send).Sum(n => n.Bytes);
                    byteCount.receive += tick.Events.Where(n => n.EventType == TickType.Receive).Sum(n => n.Bytes);
                }
                if (historyTooSmall)
                    Debug.LogError("NetworkProfiler history too small for network traffic. Increase profilerHistoryLength");
            }
            byteCountArray[byteCountArrayIndex] = byteCount;
            byteCountArrayIndex++;
            if (byteCountArrayIndex >= byteCountArrayMax)
                byteCountArrayIndex = 0;
            lastFrameCount = Time.frameCount;

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Start()
    {
        NetworkingManager.Singleton.OnClientConnectedCallback += id => { Debug.Log($"Client connected with id: {id}"); };
        NetworkingManager.Singleton.OnClientDisconnectCallback += id => { Debug.Log($"Client disconnect with id: {id}"); };

        StartCoroutine(calcByteCount());
    }

    private void OnGUI()
    {
        NetworkingManager nm = NetworkingManager.Singleton;
        if (!(nm.IsHost || nm.IsServer || nm.IsClient))
        {
            if (GUILayout.Button("Start Host"))
            {
                nm.StartHost();
                NetworkProfiler.Start(profilerHistoryLength);
            }
            if (GUILayout.Button("Start Server"))
            {
                nm.StartServer();
                NetworkProfiler.Start(profilerHistoryLength);
            }
            if (GUILayout.Button("Start Client"))
            {
                nm.StartClient();
                NetworkProfiler.Start(profilerHistoryLength);
            }
        }
        else if (nm.IsHost)
        {
            if (GUILayout.Button("Stop Host"))
                nm.StopHost();
        }
        else if (nm.IsServer)
        {
            if (GUILayout.Button("Stop Server"))
                nm.StopServer();
        }
        else if (nm.IsClient)
        {
            if (GUILayout.Button("Stop Client"))
                nm.StopClient();
        }

        // Counts do not include MLAPI overhead!
        GUILayout.Label($"Sent: {byteCountArray.Sum(n => n.send)} bps Recv: {byteCountArray.Sum(n => n.receive)} bps");
    }
}
