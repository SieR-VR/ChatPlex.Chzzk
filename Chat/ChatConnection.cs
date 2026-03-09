using ChatPlex.Chzzk.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPlex.Chzzk.Chat
{
  internal class ChatConnection : IDisposable
  {
    private readonly Uri _uri;
    private readonly string _pingMsg = "{\"ver\":\"2\",\"cmd\":0}";
    private readonly string _pongMsg = "{\"ver\":\"2\",\"cmd\":10000}";
    private readonly ClientWebSocket _client = new();
    private readonly CancellationTokenSource _connectionCancellationTokenSource = new();

    private Task _sendTask = Task.CompletedTask;

    private CancellationToken ConnectionCancellationToken => _connectionCancellationTokenSource.Token;

    public event Action<string> OnMessage = delegate { };

    public ChatConnection()
    {
      int id = new Random().Next(1, 6);
      _uri = new Uri($"wss://kr-ss{id}.chat.naver.com/chat");
      _client.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    }

    public async Task<ChatAccess> Connect()
    {
      Plugin.Log?.Info($"{GetType().Name}: Connect()");
      var channel = await new LiveChannelPoller(PluginConfig.Instance.ChannelId).GetLiveChannel(ConnectionCancellationToken).ConfigureAwait(false);

      Plugin.Log?.Debug($"{GetType().Name}: Connect to {_uri}");
      await _client.ConnectAsync(_uri, CancellationToken.None);

      var connectObj = new JObject(
          new JProperty("ver", "3"),
          new JProperty("cmd", 100),
          new JProperty("svcid", "game"),
          new JProperty("cid", channel.ChannelId),
          new JProperty("bdy", new JObject(
              new JProperty("uid", null),
              new JProperty("devType", 2001),
              new JProperty("accTkn", channel.AccessToken),
              new JProperty("auth", "READ")
              )
          ),
          new JProperty("tid", 1)
          );
      await Send(connectObj.ToString());

      return channel;
    }

    public async Task Listen()
    {
      var bytesReceived = new ArraySegment<byte>(new byte[16384]);

      while (_client.State == WebSocketState.Open)
      {
        try
        {
          WebSocketReceiveResult result = await _client.ReceiveAsync(bytesReceived, ConnectionCancellationToken).ConfigureAwait(false);
          string serverMsg = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
          if (serverMsg == _pingMsg)
          {
            await Send(_pongMsg);
          }
          else
          {
            try
            {
              OnMessage(serverMsg);
            }
            catch
            {
              Plugin.Log.Error($"{GetType().Name}: OnMessage() {serverMsg}");
              throw;
            }
          }
        }
        catch (OperationCanceledException exception)
        {
          bool isQuittingGame = exception.InnerException is ObjectDisposedException;
          if (isQuittingGame)
          {
            break;
          }
          else
          {
            Plugin.Log.Error(exception);
          }
        }
        catch (Exception exception)
        {
          Plugin.Log.Error(exception);
        }
      }

      Plugin.Log?.Info($"{GetType().Name}: Listen() end");
    }

    public void Dispose()
    {
      _connectionCancellationTokenSource.Cancel();
      _connectionCancellationTokenSource.Dispose();
      _client.Dispose();
    }

    private Task Send(string msg)
    {
      _sendTask = SendInternal(_sendTask, msg);
      return _sendTask;
    }

    private async Task SendInternal(Task previousTask, string msg)
    {
      await previousTask.ConfigureAwait(false);

      var bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
      await _client.SendAsync(bytesToSend, WebSocketMessageType.Text, true, ConnectionCancellationToken).ConfigureAwait(false);
    }
  }

  internal record ChatAccess(string ChannelId, string AccessToken, string ExtraToken, string LiveTitle, string? ChannelName = null);

  internal class LiveChannelPoller(string _channelId)
  {
    private readonly HttpApiClient _client = new();
    private readonly string _channelId = _channelId;

    private int _retryCount;

    private int DelaySeconds => Math.Min(10 + _retryCount, 30);

    public async Task<ChatAccess> GetLiveChannel(CancellationToken cancellationToken = default)
    {
      _retryCount = 0;

      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var channel = await _client.GetChannel(_channelId).ConfigureAwait(false);
        if (channel == null)
        {
          Plugin.Log?.Warn($"Channel({_channelId}) is not found. Please check your configuration.");
        }
        else if (!channel.OpenLive)
        {
          LogRetryAfter();
        }
        else
        {
          cancellationToken.ThrowIfCancellationRequested();
          if (await GetChatAccess().ConfigureAwait(false) is ChatAccess chat)
          {
            return chat with { ChannelName = channel.ChannelName };
          }
        }

        await Task.Delay(DelaySeconds * 1000, cancellationToken).ConfigureAwait(false);

        _retryCount++;
      }
    }

    private async Task<ChatAccess?> GetChatAccess()
    {
      var status = await _client.GetLiveStatus(_channelId).ConfigureAwait(false);
      switch (status)
      {
        case Live liveChannel:
          var (accessToken, extraToken) = await _client.GetAccessToken(liveChannel.ChatChannelId).ConfigureAwait(false);
          return new ChatAccess(liveChannel.ChatChannelId, accessToken, extraToken, liveChannel.LiveTitle);
        case NotFound:
          Plugin.Log?.Warn($"Unreachable: Live status of channel({_channelId}) is 404. Please report to developer.");
          break;
        case NotLive:
          LogRetryAfter();
          break;
        case NotCreated:
          Plugin.Log?.Warn("Have you ever streamed on Chzzk? Please stream first.");
          break;
        case Limited:
          Plugin.Log?.Warn("Cannot get channel ID. Are you streaming for adult only?");
          break;
      }

      return null;
    }

    private void LogRetryAfter()
    {
      Plugin.Log?.Info($"Channel is not live. Waiting for {DelaySeconds} seconds...");
    }
  }
}
