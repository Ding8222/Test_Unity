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
            NetCore.Init();
            NetSender.Init();
            NetReceiver.Init();
            Subid();
        }
    }

    void Update()
    {
        NetCore.Dispatch();
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
        NetCore.Connect("127.0.0.1", 8101, SocketConnected);
    }

    void SocketConnected()
    {
        Handshake();
    }

    public void Handshake()
    {
        handshake.request req = new handshake.request();
        clientkey = randomkey();
        req.clientkey = Convert.ToBase64String(dhexchange(clientkey));
        NetSender.Send<ClientProtocol.handshake>(req, (_) =>
        {
            handshake.response rsp = _ as handshake.response;
            serverkey = Convert.FromBase64String(rsp.serverkey);
            challenge = Convert.FromBase64String(rsp.challenge);
            secret = dhsecret(serverkey, clientkey);
            Challenge(hmac64(challenge, secret));
        });
    }

    string encode_token(stToken token)
    {
        return string.Format("{0}@{1}:{2}",
        Utilities.ToBase64String(token.user),
        Utilities.ToBase64String(token.server),
        Utilities.ToBase64String(token.pass));
    }
    
    void Challenge(byte[] hmac)
    {
        challenge.request req = new challenge.request();
        req.hmac = Convert.ToBase64String(hmac);
        NetSender.Send<ClientProtocol.challenge>(req, (_) =>
        {
            challenge.response rsp = _ as challenge.response;
            Debug.Log(rsp.result);
            token.server = "sample";
            token.user = account;
            token.pass = password;
            byte[] etoken = desencode(secret, Encoding.UTF8.GetBytes(encode_token(token)));
            Auth(etoken);
        });
    }

    void Auth(byte[] etokens)
    {
        auth.request req = new auth.request();
        req.etokens = Convert.ToBase64String(etokens);
        NetSender.Send<ClientProtocol.auth>(req);
    }

    void Subid()
    {
        NetReceiver.AddHandler<ServerProtocol.subid>((_) =>
        {
            ServerSprotoType.subid.request rsp = _ as ServerSprotoType.subid.request;
            Debug.Log(rsp.result.Substring(0, 3));
            int code = Convert.ToInt32(rsp.result.Substring(0, 3));
            if (code == 200)
            {
                Debug.Log("Auth Success!");
            }
            return null;
        });
    }
}
