using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Automatically starts the network connection when the game scene loads.
/// - In a build: starts as client only (connects to dedicated server)
/// - In the editor: starts as host (server + client) for easy testing
/// Attach this to a GameObject in the game scene.
/// </summary>
public class NetworkAutoStart : MonoBehaviour
{
    [Header("Server Address")]
    public string serverAddress = "localhost";
    public ushort serverPort = 7777;

    [Header("Editor Behaviour")]
    [Tooltip("Start as Host (server+client) in the editor. In a build always connects as client.")]
    public bool editorStartAsHost = true;

    void Start()
    {
        NetworkManager networkManager = InstanceFinder.NetworkManager;
        if (networkManager == null)
        {
            Debug.LogError("[NetworkAutoStart] No NetworkManager found in scene.");
            return;
        }

        // Set address and port on the transport
        Transport transport = networkManager.TransportManager.Transport;
        if (transport != null)
        {
            transport.SetClientAddress(serverAddress);
            transport.SetPort(serverPort);
        }

#if UNITY_EDITOR
        if (editorStartAsHost)
        {
            Debug.Log("[NetworkAutoStart] Editor mode — starting as Host.");
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();
        }
        else
        {
            Debug.Log("[NetworkAutoStart] Editor mode — starting as Client.");
            networkManager.ClientManager.StartConnection();
        }
#else
        // In a real build, always connect as client to the dedicated server
        Debug.Log($"[NetworkAutoStart] Build mode — connecting to {serverAddress}:{serverPort}");
        networkManager.ClientManager.StartConnection();
#endif
    }
}
