using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DisableIfNotOwner : MonoBehaviour
{
    [SerializeField]
    private NetworkObject NetObject;

    private void Start()
    {

        if (NetworkManager.Singleton.LocalClient.PlayerObject.IsOwner == false)
            this.gameObject.SetActive(false);

        //if (NetObject.IsOwner == false)
        //    this.gameObject.SetActive(false);
    }
}
