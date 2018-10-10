using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Vector3 offset;
    public Transform player;

    private void Awake()
    {
        offset = transform.position - player.position;
    }

    void FixedUpdate()
    {
        Vector3 movement = player.position + offset;
        transform.position = movement;
    }
}
