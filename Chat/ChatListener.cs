using ChatPlex.Chzzk.Configuration;
using CP_SDK.Chat.SimpleJSON;
using IPA.Utilities;
using System;
using System.Threading.Tasks;

namespace ChatPlex.Chzzk.Chat
{

  public class ChatListener : IDisposable
  {
    public event Action<ChzzkChatMessage> OnMessage = delegate { };
    public event Action<string> OnChannelFound = delegate { };
    public event Action<string> OnChannelNotFound = delegate { };
    public event Action<string> OnConnect = delegate { };
    public event Action<string> OnDisconnect = delegate { };

    private ChatConnection _connection = new();
    private bool _isFirstConnection = true;
    private bool _isDisposed = false;

    public ChatListener()
    {
      Reconnect();
    }

    public async Task Init()
    {
      int failureCount = 0;

      try
      {
        var channel = await new HttpApiClient().GetChannel(PluginConfig.Instance.ChannelId).ConfigureAwait(false);
        if (channel != null)
        {
          OnChannelFound(channel.ChannelName);
          _isFirstConnection = false;
        }
        else
        {
          OnChannelNotFound(PluginConfig.Instance.ChannelId);
        }
      }
      catch (Exception exception)
      {
        Plugin.Log?.Error(exception);
      }

      while (!_isDisposed)
      {
        try
        {
          var chatAccess = await _connection.Connect().ConfigureAwait(false);
          ForwardConnection(chatAccess);

          try
          {
            await _connection.Listen().ConfigureAwait(false);
          }
          finally
          {
            OnDisconnect(chatAccess.LiveTitle);
          }
        }
        catch (OperationCanceledException)
        {
          Plugin.Log?.Info($"{GetType().Name}: No chat received at least 30 seconds, try reconnecting...");
        }
        catch (Exception e)
        {
          Plugin.Log?.Error(e);
          failureCount++;
          if (failureCount > 3)
          {
            throw;
          }
          await Task.Delay(1000).ConfigureAwait(false);
        }
      }
    }

    public void Dispose()
    {
      _isDisposed = true;
      _connection.Dispose();
      Plugin.Log?.Info($"{GetType().Name}: Dispose()");
    }

    private void ForwardConnection(ChatAccess chatAccess)
    {
      if (_isFirstConnection)
      {
        OnChannelFound(chatAccess.ChannelName ?? "(unknown)");
        _isFirstConnection = false;
      }

      OnConnect(chatAccess.LiveTitle);
    }

    private void Reconnect()
    {
      _connection.Dispose();
      _connection = new ChatConnection();
      _connection.OnMessage += ParseChat;
    }

    private void ParseChat(string json)
    {
      JSONNode ReceiveObject = JSON.Parse(json);
      Plugin.Log.Debug($"{GetType().Name}: ParseChat() {json}");

      if (ReceiveObject["bdy"].IsArray)
      {
        foreach (var (key, chat) in ReceiveObject["bdy"].AsArray)
        {
          var IsAnonymous = JSON.Parse(chat["profile"])["extras"]["isAnonymous"];

          // common chat
          if (IsAnonymous == null)
          {
            try
            {
              var message = ChzzkChatMessage.FromRaw(chat);
              OnMessage.Invoke(message);
            }
            catch (Exception exception)
            {
              Plugin.Log.Error(exception);
            }
          }
          // anonymous donate
          else if (IsAnonymous == true)
          {
            Plugin.Log.Info("Anonymous Donate");
            Plugin.Log.Info(json.ToString());
            // OnMessage?.Invoke(this, ChzzkChatMessage.FromRaw(chat.ToObject<Raw.GameBody>()));
          }
          // donate
          else if (IsAnonymous == false)
          {
            Plugin.Log.Info("Donate");
            Plugin.Log.Info(json.ToString());
            // OnMessage?.Invoke(this, ChzzkChatMessage.FromRaw(chat.ToObject<Raw.GameBody>()));
          }
        }
      }
    }
  }
}
