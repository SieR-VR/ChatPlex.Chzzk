using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatPlex.Chzzk.Chat
{
  record LiveStatus();
  sealed record NotFound() : LiveStatus();
  sealed record NotCreated() : LiveStatus();
  sealed record Limited() : LiveStatus();
  sealed record NotLive() : LiveStatus();
  sealed record Live(string ChatChannelId, string LiveTitle) : LiveStatus();

  record Channel(string ChannelId, string ChannelName, bool OpenLive);

  class HttpApiClient
  {
    private readonly WebClient _client = GetWebClient();

    public async Task<Channel?> GetChannel(string channelId)
    {
      var channel = await GetChannelObject(channelId).ConfigureAwait(false);
      var content = channel?["content"];
      string? id = content?["channelId"]?.Value<string>();
      if (id == null)
      {
        return null;
      }

      string? channelName = content?["channelName"]?.Value<string>() ?? "(unknown)";
      bool openLive = content?["openLive"]?.Value<bool>() ?? false;
      return new Channel(channelId, channelName, openLive);
    }

    public async Task<LiveStatus> GetLiveStatus(string channelId)
    {
      Plugin.Log?.Debug($"{typeof(HttpApiClient).Name}: SendRequest()");

      // Get Channel Information
      JObject? liveStatus = await GetLiveStatusObject(channelId).ConfigureAwait(false);
      if (liveStatus == null)
      {
        return new NotFound();
      }

      var content = liveStatus["content"];
      if (content == null)
      {
        return new NotCreated();
      }

      string? livePollingStatusJson = content["livePollingStatusJson"]?.Value<string>();
      if (livePollingStatusJson == null)
      {
        Plugin.Log?.Warn("Channel Info Access Fail! - livePollingStatusJson is null");
      }
      else
      {
        JObject livePollingStatus = JObject.Parse(livePollingStatusJson);
        string? status = livePollingStatus["status"]?.Value<string>();
        if (status != "STARTED")
        {
          return new NotLive();
        }
      }

      string? chatChannelId = content["chatChannelId"]?.Value<string>();
      if (chatChannelId == null)
      {
        return new Limited();
      }

      string? liveTitle = content["liveTitle"]?.Value<string>();
      return new Live(chatChannelId, liveTitle ?? "(unknown)");
    }

    public async Task<(string AccessToken, string ExtraToken)> GetAccessToken(string chatChannelId)
    {
      string tokenResponse = await _client.DownloadStringTaskAsync($"https://comm-api.game.naver.com/nng_main/v1/chats/access-token?channelId={chatChannelId}&chatType=STREAMING").ConfigureAwait(false);
      JObject tokenJson = JObject.Parse(tokenResponse);

      var tokenContent = tokenJson["content"];
      string? accessToken = tokenContent?["accessToken"]?.Value<string>();
      string? extraToken = tokenContent?["extraToken"]?.Value<string>();
      if (accessToken == null)
      {
        throw new Exception($"Access Token is null for {chatChannelId}");
      }
      if (extraToken == null)
      {
        throw new Exception($"Extra Token is null for {chatChannelId}");
      }

      return (accessToken, extraToken);
    }

    private async Task<JObject?> GetChannelObject(string channelId)
    {
      string channelResponse = await _client.DownloadStringTaskAsync($"https://api.chzzk.naver.com/service/v1/channels/{channelId}").ConfigureAwait(false);
      return JObject.Parse(channelResponse);
    }

    private async Task<JObject?> GetLiveStatusObject(string channelId)
    {
      try
      {
        string channelResponse = await _client.DownloadStringTaskAsync($"https://api.chzzk.naver.com/polling/v3.1/channels/{channelId}/live-status").ConfigureAwait(false);
        return JObject.Parse(channelResponse);
      }
      catch (WebException exception)
      {
        if (exception.Message.Contains("404"))
        {
          return null;
        }
        else
        {
          throw;
        }
      }
    }

    private static WebClient GetWebClient()
    {
      var client = new WebClient();
      client.Headers.Clear();
      client.Headers.Add("Accept", "application/json");
      client.Encoding = Encoding.UTF8;
      return client;
    }
  }
}
