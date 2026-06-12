using FishNet.Managing;
using UnityEngine;

public class FishNetStarter : MonoBehaviour
{
    public NetworkManager networkManager;

    void Awake()
    {
        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkManager>();
    }

    void OnGUI()
    {
        if (networkManager == null)
            return;

        if (GUI.Button(new Rect(20, 20, 120, 35), "Start Host"))
        {
            if (networkManager.ServerManager != null) networkManager.ServerManager.StartConnection();
            if (networkManager.ClientManager != null) networkManager.ClientManager.StartConnection();
        }

        if (GUI.Button(new Rect(20, 60, 120, 35), "Start Server"))
        {
            if (networkManager.ServerManager != null) networkManager.ServerManager.StartConnection();
        }

        if (GUI.Button(new Rect(20, 100, 120, 35), "Start Client"))
        {
            if (networkManager.ClientManager != null) networkManager.ClientManager.StartConnection();
        }
    }
}