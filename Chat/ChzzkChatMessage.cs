using System;
using System.Text;
using CP_SDK.Animation;
using CP_SDK.Chat.SimpleJSON;
using CP_SDK.Chat.Interfaces;
using System.Collections.Generic;

namespace ChatPlex.Chzzk.Chat
{
  public class ChzzkChatChannel : IChatChannel
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsTemp { get; set; }
    public string Prefix { get; set; }
    public bool CanSendMessages { get; set; }
    public bool Live { get; set; }
    public int ViewerCount { get; set; }

    static Dictionary<string, ChzzkChatChannel> channels = new Dictionary<string, ChzzkChatChannel>();

    public ChzzkChatChannel(string name)
    {
      Name = name;
      Id = name;
      IsTemp = false;
      Prefix = "Chzzk";
      CanSendMessages = true;
      Live = true;
      ViewerCount = 0;
    }

    public static ChzzkChatChannel GetChannel(string name)
    {
      if (channels.ContainsKey(name))
      {
        return channels[name];
      }

      return new ChzzkChatChannel(name);
    }
  }

  public class ChzzkChatMessage : IChatMessage
  {
    public string Id { get; set; }
    public bool IsSystemMessage { get; set; }
    public bool IsActionMessage { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsGiganticEmote { get; set; }

    public bool IsPing { get; set; }
    public string Message { get; set; }
    public IChatUser Sender { get; set; }
    public IChatChannel Channel { get; set; }
    public IChatEmote[] Emotes { get; set; }

    public ChzzkChatMessage(JSONNode body)
    {
      try
      {
        Id = (string)body["msgTime"];
        Message = (string)body["msg"];

        Sender = new ChzzkChatUser(JSON.Parse(body["profile"]), (string)body["uid"]);
        Channel = ChzzkChatChannel.GetChannel((string)body["cid"]);
        Emotes = new ChzzkChatEmote[] { };
      }
      catch (Exception e)
      {
        Plugin.Log.Error($"Failed to parse chat message: {e.Message}");
      }
    }
  }

  public class ChzzkChatUser : IChatUser
  {
    public string Id { get; set; }

    public string UserName { get; set; }

    public string DisplayName { get; set; }

    public string PaintedName { get; set; }

    public string Color { get; set; }

    public bool IsBroadcaster { get; set; }

    public bool IsModerator { get; set; }

    public bool IsSubscriber { get; set; }

    public bool IsVip { get; set; }

    public IChatBadge[] Badges { get; set; }

    public ChzzkChatUser(JSONNode profile, string uid)
    {
      Id = uid;
      UserName = (string)profile["nickname"];
      DisplayName = UserName;
      PaintedName = UserName;
      Color = "#FFFFFF";
      IsBroadcaster = (string)profile["userRoleCode"] == "streamer";
      IsModerator = false;
      IsSubscriber = false;
      IsVip = false;
      Badges = new IChatBadge[] { };
    }
  }

  public class ChzzkChatBadge : IChatBadge
  {
    public EBadgeType Type { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }

    public ChzzkChatBadge(JSONNode badge)
    {
      Type = EBadgeType.Image;
      Id = (string)badge["imageUrl"];
      Name = (string)badge["imageUrl"];
      Content = (string)badge["imageUrl"];
    }
  }

  public class ChzzkChatEmote : IChatEmote
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Uri { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public EAnimationType Animation { get; set; }
  }
}