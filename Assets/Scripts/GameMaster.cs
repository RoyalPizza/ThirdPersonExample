
using Unity.Netcode;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public Camera StartCamera;

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host"))
        {
            NetworkManager.Singleton.StartHost();
            StartCamera.gameObject.SetActive(false);
        }
            
        if (GUILayout.Button("Client"))
        {
            NetworkManager.Singleton.StartClient();
            StartCamera.gameObject.SetActive(false);
        }
            
        if (GUILayout.Button("Server"))
        {
            NetworkManager.Singleton.StartServer();
            StartCamera.gameObject.SetActive(false);
        }
            
    }

    void StatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
}
