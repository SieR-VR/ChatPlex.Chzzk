using CP_SDK;
using CP_SDK.Chat;
using CP_SDK.Chat.Services;
using CP_SDK.Chat.Interfaces;
using UnityEngine;
using CP_SDK.Unity.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using ChatPlex.Chzzk.Chat;

namespace ChatPlex.Chzzk
{
  public class ChzzkService : ChatServiceBase, IChatService
  {
    public string DisplayName { get; } = "Chzzk";
    public Color AccentColor { get; } = ColorU.WithAlpha("#08FFA6", 0.75f);

    public ReadOnlyCollection<(IChatService, IChatChannel)> Channels => m_Channels.Select(x => (this as IChatService, x)).ToList().AsReadOnly();

    private readonly List<IChatChannel> m_Channels = [];
    private ChatListener? listener;

    public ChzzkService()
    {
      Plugin.Log?.Info($"{GetType().Name}: Awake()");
    }

    public void Start()
    {
      listener = new ChatListener();
      listener.OnMessage += ForwardTextMessageReception;
      listener.OnChannelFound += ForwardAsChannelJoin;
      listener.OnChannelNotFound += PrintWarning;
      listener.OnConnect += ForwardAsChannelLive;
      listener.OnDisconnect += ForwardAsChannelNotLive;
      _ = Task.Run(listener.Init);
    }

    public void Stop()
    {
      listener?.Dispose();
      listener = null;
    }

    public void RecacheEmotes()
    {

    }

    public string WebPageHTMLForm()
    {
      return "";
    }

    public string WebPageHTML()
    {
      return "";
    }

    public string WebPageJS()
    {
      return "";
    }

    public string WebPageJSValidate()
    {
      return "";
    }

    public void WebPageOnPost(Dictionary<string, string> data)
    {

    }

    public void JoinTempChannel(string channel, string user, string message, bool notify)
    {

    }

    public void LeaveTempChannel(string channel)
    {

    }

    public bool IsInTempChannel(string channel)
    {
      return false;
    }

    public void LeaveAllTempChannel(string channel)
    {

    }

    public string PrimaryChannelName()
    {
      return "";
    }

    public void SendTextMessage(IChatChannel channel, string message)
    {

    }

    public bool IsConnectedAndLive()
    {
      return true;
    }

    private void ForwardTextMessageReception(ChzzkChatMessage e)
    {
      try
      {
        m_OnTextMessageReceivedCallbacks.InvokeAll(this, e);

        if (!m_Channels.Contains(e.Channel))
        {
          m_Channels.Add(e.Channel);
        }
      }
      catch (Exception ex)
      {
        Plugin.Log.Error(ex.Message);
      }
    }

    private void ForwardAsChannelJoin(string channelName)
    {
      m_OnJoinRoomCallbacks.InvokeAll(this, new ChzzkChatChannel(channelName));
    }

    private void PrintWarning(string channelId)
    {
      m_OnSystemMessageCallbacks.InvokeAll(this, $"<color=orange><b>Channel({channelId}) is not found. Please check your configuration.</b></color>");
    }

    private void ForwardAsChannelLive(string liveTitle)
    {
      Plugin.Log?.Debug($"{nameof(ForwardAsChannelLive)}(): {liveTitle}");
      m_OnLiveStatusUpdatedCallbacks.InvokeAll(this, new ChzzkChatChannel(liveTitle), true, 0);
      m_OnSystemMessageCallbacks.InvokeAll(this, $"<color=green><b>Joining \"{liveTitle}\" live!</b></color>");
    }

    private void ForwardAsChannelNotLive(string liveTitle)
    {
      m_OnLiveStatusUpdatedCallbacks.InvokeAll(this, new ChzzkChatChannel(liveTitle), false, 0);
      m_OnSystemMessageCallbacks.InvokeAll(this, $"<color=green><b>Leaving \"{liveTitle}\" live!</b></color>");
    }
  }
}
