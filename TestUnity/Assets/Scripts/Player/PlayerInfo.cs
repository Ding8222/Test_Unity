using ClientSproto;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour {

    public static PlayerInfo Instance;
    public GameObject CharacterPrefab;

    GameObject gameObj;

    public string Account
    {
        get { return account; }
        set { account = value; }
    }

    public string Password
    {
        get { return password; }
        set { password = value; }
    }

    public string Server
    {
        get { return server; }
        set { server = value; }
    }

    public byte[] Secret
    {
        get { return secret; }
        set { secret = value; }
    }

    public byte[] Challenge
    {
        get { return challenge; }
        set { challenge = value; }
    }

    public string Subid
    {
        get { return subid; }
        set { subid = value; }
    }

    public int Index
    {
        get { return index; }
        set { index = value; }
    }

    public Int64 UUID
    {
        get { return uuid; }
        set { uuid = value; }
    }

    public string Name
    {
        get { return characterName; }
        set { characterName = value; }
    }

    public int Job
    {
        get { return job; }
        set { job = value; }
    }

    public int Sex
    {
        get { return sex; }
        set { sex = value; }
    }

    string account;
    string password;
    string server;
    byte[] secret;
    byte[] challenge;
    string subid;
    int index = 1;
    Int64 uuid;
    string characterName;
    int job;
    int sex;

    // 玩家角色列表
    public Dictionary<Int64, character_overview> CharacterList
    {
        get { return characterList; }
        set { characterList = value; }
    }
    Dictionary<Int64, character_overview> characterList;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}


    public void InitGameObj()
    {
        gameObj = Instantiate(CharacterPrefab);
        lastPosition = gameObj.transform.position;
        // 每0.5秒同步一次坐标（如果坐标发送变化）
        InvokeRepeating("MoveTo", 0, 0.5f);
    }

    Vector3 lastPosition;
    void MoveTo()
    {
        if(lastPosition!= gameObj.transform.position)
        {
            lastPosition = gameObj.transform.position;
            moveto.request req = new moveto.request();
            req.pos = new character_pos();
            req.pos.x = Convert.ToInt32(gameObj.transform.position.x);
            req.pos.y = Convert.ToInt32(gameObj.transform.position.y);
            req.pos.z = Convert.ToInt32(gameObj.transform.position.z);
            NetSender.Send<ClientProtocol.moveto>(req, (_) =>
            {
                moveto.response rsp = _ as moveto.response;
                Debug.Log(rsp.pos.ToString());
            });
        }
    }
}
