using ClientSproto;
using UnityEngine;
using Elliptic;
using System.Text;
using System.Security.Cryptography;

public class Login : MonoBehaviour {

    public static Login Instance;

    private string account;
    private string password;

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            NetCore.Init();
            NetSender.Init();
        }
    }

    void Update()
    {
        NetCore.Dispatch();
    }

    public void Connect()
    {
        NetCore.Connect("127.0.0.1", 8101, SocketConnected);
    }

    void SocketConnected()
    {
        Handshake();
    }

    public void Handshake()
    {
        handshake.request req = new handshake.request();
        byte[] clientbyte = new byte[8];
        RNGCryptoServiceProvider.Create().GetBytes(clientbyte);
        byte[] clientkey = Curve25519.GetPublicKey(clientbyte);
        req.clientkey = Utilities.ToBase64String(Encoding.UTF8.GetString(clientkey));
        NetSender.Send<ClientProtocol.handshake>(req, (_) =>
        {
            handshake.response rsp = _ as handshake.response;
            string challenge = Utilities.UnBase64String(rsp.challenge);
            string serverkey = Utilities.UnBase64String(rsp.serverkey);
            Debug.Log("challenge: " + challenge);
            Debug.Log("serverkey: " + serverkey);
            byte[] secret = Curve25519.GetSharedSecret(Encoding.UTF8.GetBytes(serverkey), Encoding.UTF8.GetBytes(challenge));
            Debug.Log(secret);
            Challenge(Encoding.UTF8.GetString(secret));
        });
    }

    void Challenge(string hmac)
    {
        Debug.Log("hmac: " + hmac);
        challenge.request req = new challenge.request();
        req.hmac = Utilities.ToBase64String(hmac);
        NetSender.Send<ClientProtocol.challenge>(req, (_) =>
        {
            challenge.response rsp = _ as challenge.response;
        });
    }

    void Auth()
    {

    }
}
