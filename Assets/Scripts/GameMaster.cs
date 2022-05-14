
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
    }

    private void Singleton_OnClientDisconnectCallback(ulong obj)
    {
        Debug.Log("Client Disconnected: " + obj);
        if (NetworkManager.Singleton.LocalClient.PlayerObject.OwnerClientId == obj)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                //Debug.Log("Unloading Scene");
                //NetworkManager.Singleton.SceneManager.UnloadScene("Playground");
                //SceneManager.UnloadSceneAsync("Playground");
                StartCamera.gameObject.SetActive(true);
            }
        }
    }

    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        Debug.Log("Client Connected: " + obj);
        if (NetworkManager.Singleton.LocalClient.PlayerObject.OwnerClientId == obj)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                //Debug.Log("Loading Scene");
                //NetworkManager.Singleton.SceneManager.LoadScene("Playground", LoadSceneMode.Additive);
                //SceneManager.LoadScene("Playground", LoadSceneMode.Additive);
            }
        }
    }
}
