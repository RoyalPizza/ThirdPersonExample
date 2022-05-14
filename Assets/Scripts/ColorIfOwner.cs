using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ColorIfOwner : MonoBehaviour
{
    [SerializeField]
    private NetworkObject NetObject;
    
    [SerializeField]
    private Renderer Renderer;

    void Start()
    {
        if (NetObject.IsOwner)
        {
            Renderer.material.color = Color.green;
        }
        else
        {
            Renderer.material.color = Color.red;
        }
    }
}
