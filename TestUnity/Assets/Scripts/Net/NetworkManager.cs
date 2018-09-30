using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Sproto;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour {
    
    public static NetworkManager Instance;
    public bool isConnected
    {
        get { return ClientConnect.Connected; }
    }
    
    private Socket ClientConnect { set; get; }
    private const int MAX_LENGTH = 2048;
    public const int PACKAGE_LENGTH = 2;
    private byte[] buffer = new byte[MAX_LENGTH];
    private MemoryStream _stream;
    private BinaryReader _reader;

    private SprotoMgr S2C;
    private SprotoMgr C2S;
    SprotoRpc ClientSproto;
    SprotoRpc ServerSproto;

    // Use this for initialization
    void Start () {
        if (Instance == null)
        {
            Instance = this;
            InitSocket();
            InitSproto();
            DontDestroyOnLoad(gameObject);
        }
    }

    void InitSproto()
    {
        LoadSproto("Assets/Sproto/proto/client/", out C2S);
        LoadSproto("Assets/Sproto/proto/server/", out S2C);
        ClientSproto = new SprotoRpc(S2C, C2S);
        ServerSproto = new SprotoRpc(C2S, S2C);
        TestSproto();
    }

    private void LoadSproto(string fullPath, out SprotoMgr sproto)
    {
        List<string> ProtoFiles = new List<string> { };
        //获取指定路径下面的所有资源文件  
        if (Directory.Exists(fullPath))
        {
            DirectoryInfo direction = new DirectoryInfo(fullPath);
            FileInfo[] files = direction.GetFiles("*.lua", SearchOption.AllDirectories);
            
            for (int i = 0; i < files.Length; i++)
            {
                ProtoFiles.Add(fullPath + files[i].Name);
            }
        }

        sproto = SprotoParser.ParseFile(ProtoFiles);
    }

    void TestSproto()
    {
        SprotoObject request = ClientSproto.C2S.NewSprotoObject("handshake.request");
        request["clientkey"] = "DingDaLong";
        RpcPackage request_package = ClientSproto.PackRequest("handshake", request, 1);
        RpcMessage message = ServerSproto.UnpackMessage(request_package.data, request_package.size);

        SprotoObject response = ServerSproto.S2C.NewSprotoObject("handshake.response");
        response["challenge"] = "11111";
        response["serverkey"] = "22222";
        RpcPackage response_package = ServerSproto.PackResponse("handshake", response, 1);
        message = ClientSproto.UnpackMessage(response_package.data, response_package.size);
    }

    void InitSocket()
    {
        ClientConnect = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _stream = new MemoryStream();
        _reader = new BinaryReader(_stream);
    }

    public void Click()
    {
        Connect("127.0.0.1", "8101");
    }

    public void Connect(string ip, string port)
    {
        if (!isConnected)
        {
            ClientConnect.BeginConnect(IPAddress.Parse(ip), int.Parse(port), EndConnect, null);
        }
    }

    void EndConnect(IAsyncResult ar)
    {
        BeginReceive();

        SprotoObject request = ClientSproto.C2S.NewSprotoObject("handshake.request");
        request["clientkey"] = "DingDaLong";
        RpcPackage request_package = ClientSproto.PackRequest("handshake", request, 1);
        Send(request_package.data);
    }

    void BeginReceive()
    {
        ClientConnect.BeginReceive(buffer, 0, MAX_LENGTH, SocketFlags.None, OnReceive, null);
    }

    void OnReceive(IAsyncResult ar)
    {
        int length = ClientConnect.EndReceive(ar);

        if (length < 1)
        {
            Disconnect("接收包长度 < 1");
            return;
        }
        try
        {
            HandleMsg(buffer, length);
            Array.Clear(buffer, 0, MAX_LENGTH);
            BeginReceive();
        }
        catch (Exception ex)
        {
            Disconnect(ex.Message);
        }
    }

    void HandleMsg(byte[] buffer, int length)
    {
        _stream.Seek(0, SeekOrigin.End);
        _stream.Write(buffer, 0, length);
        _stream.Seek(0, SeekOrigin.Begin);

        while (RemainBuffLength() > PACKAGE_LENGTH)
        {
            short totalLength = IPAddress.HostToNetworkOrder(_reader.ReadInt16());
            int LeaftLength = totalLength - PACKAGE_LENGTH;

            if (LeaftLength > RemainBuffLength())
            {
                _stream.Position -= PACKAGE_LENGTH;
                break;
            }

            byte[] package = _reader.ReadBytes(LeaftLength);
            //TODO 发送数据包。
        }

        byte[] leftBytes = _reader.ReadBytes((int)RemainBuffLength());
        _stream.SetLength(0);
        _stream.Write(leftBytes, 0, leftBytes.Length);
    }

    long RemainBuffLength()
    {
        return _stream.Length - _stream.Position;
    }

    public void Disconnect(string msg)
    {
        if (isConnected)
            ClientConnect.Disconnect(false);
        
        ClientConnect = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //TODO 发送断开连接的事件。
    }
    
    public void Send(byte[] buffer)
    {
        short totalLength = Convert.ToInt16(buffer.Length + PACKAGE_LENGTH);
        short netLength = IPAddress.HostToNetworkOrder(Convert.ToInt16(buffer.Length));
        byte[] lengthBytes = BitConverter.GetBytes(netLength);

        byte[] sendBuffer = new byte[totalLength];
        Array.Copy(lengthBytes, sendBuffer, PACKAGE_LENGTH);
        Array.Copy(buffer, 0, sendBuffer, PACKAGE_LENGTH, buffer.Length);

        ClientConnect.BeginSend(sendBuffer, 0, totalLength, SocketFlags.None, EndSend, null);
    }

    void EndSend(IAsyncResult ar)
    {
        int length = ClientConnect.EndSend(ar);
    }
}
