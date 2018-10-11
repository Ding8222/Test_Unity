using ClientSproto;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMap : MonoBehaviour {

    public GameObject gameObj;
	// Use this for initialization
	void Start () {
        InitPlayer();
        MapReady();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void InitPlayer()
    {
        PlayerInfo.Instance.gameObj = gameObj;
        PlayerInfo.Instance.InitGameObj();
    }

    void MapReady()
    {
        NetSender.Send<ClientProtocol.mapready>(null, (_) =>
        {
            mapready.response rsp = _ as mapready.response;
            Debug.Log("进入地图结果：" + rsp.ok);
        });
    }
}
