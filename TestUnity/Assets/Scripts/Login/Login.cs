using ClientSproto;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using XLua;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Login : MonoBehaviour {

    public static Login Instance;

    public InputField AccountInputField;
    public InputField PasswordInputField;

    public InputField IPInputField;
    public InputField PortInputField;

    public TextAsset luaScript;
    internal static LuaEnv luaEnv = new LuaEnv();
    private LuaTable scriptEnv;
    
    [CSharpCallLua]
    public delegate byte[] FDelegate();

    [CSharpCallLua]
    public delegate byte[] FDelegate1(byte[] a);

    [CSharpCallLua]
    public delegate byte[] FDelegate2(byte[] a, byte[] b);
    

    private FDelegate randomkey;
    private FDelegate1 dhexchange;
    private FDelegate2 dhsecret;
    private FDelegate2 hmac64;
    private FDelegate2 desencode;
    private FDelegate1 hashkey;

    //string loginIP = "47.52.138.32";
    string loginIP = "127.0.0.1";
    string gameIP ;
    int loginPort = 8101;
    int gamePort = 8547;

    byte[] clientkey;
    byte[] serverkey;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitLua();
        }
    }

    void Start()
    {
        Register();
    }

    void Update()
    {

    }

    void InitLua()
    {
        scriptEnv = luaEnv.NewTable();

        LuaTable meta = luaEnv.NewTable();
        meta.Set("__index", luaEnv.Global);
        scriptEnv.SetMetaTable(meta);
        meta.Dispose();

        scriptEnv.Set("self", this);
        luaEnv.DoString(luaScript.text, "LuaBehaviour", scriptEnv);
        scriptEnv.Get("randomkey", out randomkey);
        scriptEnv.Get("dhexchange", out dhexchange);
        scriptEnv.Get("dhsecret", out dhsecret);
        scriptEnv.Get("hmac64", out hmac64);
        scriptEnv.Get("desencode", out desencode);
        scriptEnv.Get("hashkey", out hashkey);
    }

    public void ChangeServerIP()
    {
        if (IPInputField.text.Length > 0)
        {
            loginIP = IPInputField.text;
            loginPort = Convert.ToInt32(PortInputField.text);
        }
        else
        {
            loginIP = "47.52.138.32";
            loginPort = 8101;
        }
    }

    public void Connect()
    {
        PlayerInfo.Instance.Account = AccountInputField.text;
        PlayerInfo.Instance.Password = PasswordInputField.text;
        if (PlayerInfo.Instance.Account.Length == 0)
        {
            PlayerInfo.Instance.Account = "ding";
            PlayerInfo.Instance.Password = "password";
            PlayerInfo.Instance.Server = "sample";
        }
        NetCore.Connect(loginIP, loginPort, LoginConnected);
    }

    void LoginConnected()
    {
        // 连接到登陆服务器之后进行验证
        Handshake();
    }

    public void Handshake()
    {
        // 生成私钥
        clientkey = randomkey();
        handshake.request req = new handshake.request();
        // 发送公钥
        req.clientkey = Convert.ToBase64String(dhexchange(clientkey));
        NetSender.Send<ClientProtocol.handshake>(req, (_) =>
        {
            // 返回消息
            handshake.response rsp = _ as handshake.response;
            serverkey = Convert.FromBase64String(rsp.serverkey);
            PlayerInfo.Instance.Challenge = Convert.FromBase64String(rsp.challenge);
            PlayerInfo.Instance.Secret = dhsecret(serverkey, clientkey);
            Challenge(hmac64(PlayerInfo.Instance.Challenge, PlayerInfo.Instance.Secret));
        });
    }

    string EncodeToken()
    {
        return string.Format("{0}@{1}:{2}",
        Utilities.ToBase64String(PlayerInfo.Instance.Account),
        Utilities.ToBase64String(PlayerInfo.Instance.Server),
        Utilities.ToBase64String(PlayerInfo.Instance.Password));
    }
    
    void Challenge(byte[] hmac)
    {
        // 验证密钥
        challenge.request req = new challenge.request();
        req.hmac = Convert.ToBase64String(hmac);
        NetSender.Send<ClientProtocol.challenge>(req, (_) =>
        {
            challenge.response rsp = _ as challenge.response;
            Debug.Log(rsp.result);
            byte[] etoken = desencode(PlayerInfo.Instance.Secret, Encoding.UTF8.GetBytes(EncodeToken()));
            Auth(etoken);
        });
    }

    void Auth(byte[] etokens)
    {
        // 账号密码服务器验证
        auth.request req = new auth.request();
        req.etokens = Convert.ToBase64String(etokens);
        NetSender.Send<ClientProtocol.auth>(req);
    }

    void Register()
    {
        NetReceiver.AddHandler<ServerProtocol.subid>((_) =>
        {
            // 账号密码服务器验证返回
            ServerSprotoType.subid.request rsp = _ as ServerSprotoType.subid.request;
            Debug.Log(rsp.result.Substring(0, 3));
            int code = Convert.ToInt32(rsp.result.Substring(0, 3));
            if (code == 200)
            {
                // 验证成功时连接至gameserver
                Debug.Log("登陆服务器认证成功!");
                PlayerInfo.Instance.Subid = Utilities.UnBase64String(rsp.result.Substring(4));
                GameConnect();
            }
            return null;
        });
    }

    void GameConnect()
    {
        gameIP = loginIP;
        NetCore.Connect(gameIP, gamePort, GameConnected);
    }

    void GameConnected()
    {
        QueryLogin();
    }

    void QueryLogin()
    {
        //请求登陆gameserver
        string handshake = string.Format("{0}@{1}#{2}:{3}", 
            Utilities.ToBase64String(PlayerInfo.Instance.Account), 
            Utilities.ToBase64String(PlayerInfo.Instance.Server),
            Utilities.ToBase64String(PlayerInfo.Instance.Subid), PlayerInfo.Instance.Index);
        byte[] hmac = hmac64(hashkey(Encoding.UTF8.GetBytes(handshake)), PlayerInfo.Instance.Secret);
        login.request req = new login.request();
        req.handshake = handshake + ":" + Convert.ToBase64String(hmac);
        NetSender.Send<ClientProtocol.login>(req, (_) =>
        {
            login.response rsp = _ as login.response;
            Debug.Log(rsp.result);
            int code = Convert.ToInt32(rsp.result.Substring(0, 3));
            if (code == 200)
            {
                Debug.Log("游戏服务器认证成功!");
                GetCharacterList();
            }
        });
    }

    public void GetCharacterList()
    {
        // 请求角色列表
        NetSender.Send<ClientProtocol.getcharacterlist>(null, (_) =>
        {
            getcharacterlist.response rsp = _ as getcharacterlist.response;
            PlayerInfo.Instance.CharacterList = rsp.character;
            Debug.Log("玩家角色数量：" + PlayerInfo.Instance.CharacterList.Count);
            if (PlayerInfo.Instance.CharacterList.Count == 0)
            {
                // 创建角色
                LoadCreateCharacterScene();
            }
            else
            {
                // 选择角色
                LoadPickCharacterScene();
            }
        });
    }

    void LoadCreateCharacterScene()
    {
        SceneManager.LoadScene("CreateCharacter");
    }

    void LoadPickCharacterScene()
    {
        SceneManager.LoadScene("PickCharacter");
    }
}
