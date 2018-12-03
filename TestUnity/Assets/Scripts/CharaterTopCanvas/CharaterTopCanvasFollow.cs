using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharaterTopCanvasFollow : MonoBehaviour {

    GameObject target;

    // Use this for initialization
    void Start ()
    {
        target = GameObject.Find("Main Camera");
        Debug.Log(target == null);
    }
	
	// Update is called once per frame
	void Update () {
        transform.LookAt(target.transform);
        transform.Rotate(0, 180, 0);
    }
}
