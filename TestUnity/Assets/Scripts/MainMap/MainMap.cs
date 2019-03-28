using ClientSprotoType;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;
using UnityEngine.UI;

public class MainMap : MonoBehaviour {

    public GameObject gameObj;
    public GameObject monsterObj;
    Dictionary<long, GameObject> monsterObjList = new Dictionary<long, GameObject> { };

    private void Awake()
    {
        Register();
    }

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

    void Register()
    {
        NetReceiver.AddHandler<ServerProtocol.characterupdate>((_) =>
        {
            ServerSprotoType.characterupdate.request rsp = _ as ServerSprotoType.characterupdate.request;
            if (monsterObjList.ContainsKey(rsp.info.tempid))
            {
                Debug.Log("角色进入视野：" + rsp.info.tempid);
                monsterObjList[rsp.info.tempid].SetActive(true);
            }
            else
            {
                Debug.Log("添加角色：" + rsp.info.tempid + " 坐标：" + rsp.info.pos.x + "," + rsp.info.pos.y + "," + rsp.info.pos.z);
                GameObject obj = Instantiate(monsterObj);
                ThirdPersonCharacter tpc = obj.GetComponent<ThirdPersonCharacter>();
                tpc.characterName.text = rsp.info.name;
                obj.transform.position = new Vector3(rsp.info.pos.x / 10, rsp.info.pos.y / 10, rsp.info.pos.z / 10);
                obj.SetActive(true);
                monsterObjList[rsp.info.tempid] = obj;
            }

            return null;
        });

        NetReceiver.AddHandler<ServerProtocol.characterleave>((_) =>
        {
            ServerSprotoType.characterleave.request rsp = _ as ServerSprotoType.characterleave.request;
            foreach(var item in rsp.tempid)
            {
                if(monsterObjList.ContainsKey(item))
                {
                    Debug.Log("角色离开视野：" + item);
                    monsterObjList[item].SetActive(false);
                }
            }
            return null;
        });

        NetReceiver.AddHandler<ServerProtocol.moveto>((_) =>
        {
            ServerSprotoType.moveto.request rsp = _ as ServerSprotoType.moveto.request;
            foreach (var item in rsp.move)
            {
                if (monsterObjList.ContainsKey(item.tempid))
                {
                    AICharacterControl tpc = monsterObjList[item.tempid].GetComponent<AICharacterControl>();                    
                    tpc.SetTarget(new Vector3(item.pos.x / 10, item.pos.y / 10, item.pos.z / 10));
                }
            }
            return null;
        });
    }
}
