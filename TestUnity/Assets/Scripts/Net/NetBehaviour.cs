using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetBehaviour : MonoBehaviour {

    void Awake()
    {
        NetCore.Init();
        NetSender.Init();
        NetReceiver.Init();
    }

	// Update is called once per frame
	void Update ()
    {
        NetCore.Dispatch();
    }
}
