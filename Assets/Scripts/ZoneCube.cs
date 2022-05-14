using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ZoneCube : NetworkBehaviour
{
    public NetworkVariable<int> PlayersInZone = new NetworkVariable<int>();

    private void Start()
    {
        Debug.Log("Zone Start: " + NetworkManager.Singleton.IsServer.ToString());
        PlayersInZone.Value = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            PlayersInZone.Value += 1;
            StyleCube();
            UpdateText();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            PlayersInZone.Value -= 1;
            StyleCube();
            UpdateText();
        }
    }

    private void StyleCube()
    {
        var targetColor = Color.red;
        
        if (PlayersInZone.Value > 0)
            targetColor = Color.green;

        targetColor.a = .5f;

        GetComponent<Renderer>().material.color = targetColor;
    }

    private void UpdateText()
    {
        transform.GetChild(0).GetComponent<TextMesh>().text = PlayersInZone.Value.ToString();
    }
}
