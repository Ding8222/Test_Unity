using ClientSproto;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using XLua;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class Login : MonoBehaviour {

    public static Login Instance;

    public InputField AccountInputField;
    public InputField PasswordInputField;
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
    private FDelegate1 hexencode;
    private FDelegate2 hmac64;
    private FDelegate2 desencode;
    private FDelegate1 hashkey;

    private string account;
    private string password;
    string loginIP = "127.0.0.1";
    string gameIP = "127.0.0.1";
    int loginPort = 8101;
    int gamePort = 8547;
    string subid;
    int Index = 1;

    byte[] clientkey;
    byte[] challenge;
    byte[] serverkey;
    byte[] secret;

    struct stToken
    {
        public string server;
        public string user;
        public string pass;
    }

    stToken token;

    private void Awake()
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
        scriptEnv.Get("hexencode", out hexencode);
        scriptEnv.Get("hmac64", out hmac64);
        scriptEnv.Get("desencode", out desencode);
        scriptEnv.Get("hashkey", out hashkey);

    }

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Register();
        }
    }

    void Update()
    {

    }

    public void Connect()
    {
        account = AccountInputField.text;
        password = PasswordInputField.text;
        if (account.Length == 0)
        {
            account = "ding";
            password = "password";
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
            challenge = Convert.FromBase64String(rsp.challenge);
            secret = dhsecret(serverkey, clientkey);
            Challenge(hmac64(challenge, secret));
        });
    }

    string EncodeToken()
    {
        return string.Format("{0}@{1}:{2}",
        Utilities.ToBase64String(token.user),
        Utilities.ToBase64String(token.server),
        Utilities.ToBase64String(token.pass));
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
            token.server = "sample";
            token.user = account;
            token.pass = password;
            byte[] etoken = desencode(secret, Encoding.UTF8.GetBytes(EncodeToken()));
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
                Debug.Log("Auth Success!");
                subid = Utilities.UnBase64String(rsp.result.Substring(4));
                GameConnect();
            }
            return null;
        });
    }

    void GameConnect()
    {
        NetCore.Connect(gameIP, gamePort, GameConnected);
    }

    void GameConnected()
    {
        QueryLogin();
    }

    void QueryLogin()
    {
        //请求登陆gameserver
        string handshake = string.Format("{0}@{1}#{2}:{3}", Utilities.ToBase64String(token.user), Utilities.ToBase64String(token.server), Utilities.ToBase64String(subid), Index);
        byte[] hmac = hmac64(hashkey(Encoding.UTF8.GetBytes(handshake)), secret);
        login.request req = new login.request();
        req.handshake = handshake + ":" + Convert.ToBase64String(hmac);
        NetSender.Send<ClientProtocol.login>(req, (_) =>
        {
            login.response rsp = _ as login.response;
            Debug.Log(rsp.result);
            int code = Convert.ToInt32(rsp.result.Substring(0, 3));
            if (code == 200)
            {
                GetCharacterList();
            }
        });
    }

    void GetCharacterList()
    {
        // 请求角色列表
        NetSender.Send<ClientProtocol.getcharacterlist>(null, (_) =>
        {
            getcharacterlist.response rsp = _ as getcharacterlist.response;
            Debug.Log(rsp.character);
            if (rsp.character.Count == 0)
            {
                // 创建角色
                CreateCharacter();
            }
            else
            {
                // 选择角色
                foreach(var item in rsp.character)
                {
                    PickCharacte(item.Key);
                }
            }
        });
    }

    void CreateCharacter()
    {

    }

    void PickCharacte(Int64 uuid)
    {

    }
}
