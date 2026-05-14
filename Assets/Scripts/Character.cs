using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Character : NetworkBehaviour
{
    [SerializeField] public     Faction faction;
    [SerializeField] protected  float speed = 50.0f;

    protected Animator      animator;
    protected Vector3       prevPos;
    protected NetworkObject networkObject;

    protected virtual void Start()
    {
        // animator = GetComponent<Animator>();
        networkObject = GetComponent<NetworkObject>();
    }
}
