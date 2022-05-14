using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ZoneCube : NetworkBehaviour
{
    public NetworkVariable<int> PlayersInZoneTempName = new NetworkVariable<int>();
    public Animator TorchAnimator;

    private void Start()
    {
        //Debug.Log("Zone Start: " + NetworkManager.Singleton.IsServer.ToString());
        //PlayersInZone.Value = 0;
        PlayersInZoneTempName.OnValueChanged = OnPlayersInZoneChanged;
        PlayersInZoneTempName.OnValueChanged = OnPlayersInZoneChanged;
    }

    private void Update()
    {
        StyleCube();
        UpdateText();
    }

    public void OnPlayersInZoneChanged(int previous, int current)
    {
        //StyleCube();
        //UpdateText();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("On Trigger Enter: " + NetworkManager.Singleton.IsServer);
        if (NetworkManager.Singleton.IsServer)
        {
            TorchAnimator.gameObject.GetComponent<NetworkAnimator>().Animator.SetBool("Lit", true);
            //TorchAnimator.SetBool("Lit", true);
            //TorchAnimator.Play("TorchLit");
            PlayersInZoneTempName.Value += 1;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("On Trigger Exit: " + NetworkManager.Singleton.IsServer);

        if (NetworkManager.Singleton.IsServer)
        {
            PlayersInZoneTempName.Value -= 1;

            if (PlayersInZoneTempName.Value == 0)
            {
                TorchAnimator.gameObject.GetComponent<NetworkAnimator>().Animator.SetBool("Lit", false);
                TorchAnimator.SetBool("Lit", false);
                //TorchAnimator.Play("TorchUnlit");
            }
        }
    }

    private void StyleCube()
    {
        var targetColor = Color.red;

        if (PlayersInZoneTempName.Value > 0)
            targetColor = Color.green;

        targetColor.a = .5f;

        GetComponent<Renderer>().material.color = targetColor;
    }

    private void UpdateText()
    {
        transform.GetChild(0).GetComponent<TextMesh>().text = PlayersInZoneTempName.Value.ToString();
    }
}
