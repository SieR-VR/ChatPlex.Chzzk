using CP_SDK.Chat.SimpleJSON;
using IPA.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPlex.Chzzk.Chat
{
  public class ChatListener
  {
    private readonly ClientWebSocket client = new ClientWebSocket();
    private readonly Uri uri;
    private readonly string pingMsg = "{\"ver\":\"2\",\"cmd\":0}";
    private readonly string pongMsg = "{\"ver\":\"2\",\"cmd\":10000}";
    private readonly Random rand = new Random();

    public event EventHandler<ChzzkChatMessage> OnMessage;
    public event EventHandler<string> OnError;
    public event EventHandler<(string, string)> OnDonate;

    public ChatListener()
    {
      ThreadPool.GetMaxThreads(out int workerThreads, out int completionPortThreads);
      ThreadPool.SetMinThreads(workerThreads + 1, completionPortThreads + 1);
      ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
      ThreadPool.SetMaxThreads(workerThreads + 5, completionPortThreads + 5);

      int id = rand.Next(1, 11);
      uri = new Uri($"wss://kr-ss{id}.chat.naver.com/chat");
    }

    public async Task Init()
    {
      Plugin.Log?.Info($"{GetType().Name}: Init()");
      GetChannelInfo userInfo = new GetChannelInfo();

      try
      {
        await client.ConnectAsync(uri, CancellationToken.None);
        Plugin.Log?.Info($"{GetType().Name}: Connect to {uri}");

        JObject connectObj = new JObject(
            new JProperty("ver", "2"),
            new JProperty("cmd", 100),
            new JProperty("svcid", "game"),
            new JProperty("cid", userInfo.ChatChannelId),
            new JProperty("bdy", new JObject(
                new JProperty("uid", userInfo.UId == "" ? "" : userInfo.UId),
                new JProperty("devType", 2001),
                new JProperty("accTkn", userInfo.AccessToken),
                new JProperty("auth", (userInfo.UId != "") ? "SEND" : "READ")
                )
            ),
            new JProperty("tid", 1)
            );

        // first touch
        string jsonString = connectObj.ToString();
        ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonString));
        await client.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);

        ThreadPool.QueueUserWorkItem(Listen);
        Plugin.Log?.Info($"{GetType().Name}: Init() success");
      }
      catch (Exception e)
      {
        Plugin.Log.Error(e.Message);
      }
    }

    private async void Listen(Object obj)
    {
      ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[16384]);

      if (client.State == WebSocketState.Open)
      {
        Plugin.Log?.Info("Socket Link Success");
      }
      else
      {
        Plugin.Log?.Error("Socket Link Fail");
      }

      while (client.State == WebSocketState.Open)
      {
        WebSocketReceiveResult result = await client.ReceiveAsync(bytesReceived, CancellationToken.None);
        string serverMsg = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
        try
        {
          if (serverMsg == pingMsg) Send(pongMsg);
          else
          {
            ThreadPool.QueueUserWorkItem(ParseChat, serverMsg);
          }
        }
        catch (Exception e)
        {
          Plugin.Log.Error(serverMsg);
          Plugin.Log.Error(e.Message);

          OnError?.Invoke(this, e.Message);
        }
      }

    }

    public async void CloseClient()
    {
      await client.CloseOutputAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
    }

    private async void Send(string msg)
    {
      ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
      await client.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private void ParseChat(object ReceiveString)
    {
      JSONNode ReceiveObject = JSON.Parse((string)ReceiveString);

      if (ReceiveObject["bdy"].IsArray)
      {
        foreach (var (key, chat) in ReceiveObject["bdy"].AsArray)
        {
          var IsAnonymous = JSON.Parse(chat["profile"])["extras"]["isAnonymous"];

          // common chat
          if (IsAnonymous == null)
          {
            ChzzkChatMessage message = new ChzzkChatMessage(chat);
            OnMessage?.Invoke(this, message);
          }
          // anonymous donate
          else if (IsAnonymous == true)
          {
            Plugin.Log.Info("Anonymous Donate");
            Plugin.Log.Info(ReceiveString.ToString());
            // OnMessage?.Invoke(this, ChzzkChatMessage.FromRaw(chat.ToObject<Raw.GameBody>()));
          }
          // donate
          else if (IsAnonymous == false)
          {
            Plugin.Log.Info("Donate");
            Plugin.Log.Info(ReceiveString.ToString());
            // OnMessage?.Invoke(this, ChzzkChatMessage.FromRaw(chat.ToObject<Raw.GameBody>()));
          }
        }
      }
    }
  }
}
