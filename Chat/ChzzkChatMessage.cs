using CP_SDK.Animation;
using CP_SDK.Chat.SimpleJSON;
using CP_SDK.Chat.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace ChatPlex.Chzzk.Chat
{
  public class ChzzkChatChannel : IChatChannel
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsTemp { get; set; }
    public string Prefix { get; set; } = "";
    public bool CanSendMessages { get; set; } = true;
    public bool Live { get; set; } = true;
    public int ViewerCount { get; set; }

    public ChzzkChatChannel(string name)
    {
      Name = name;
      Id = name;
    }
  }

  public record ChzzkChatMessage : IChatMessage
  {
    public string Id { get; set; }
    public bool IsSystemMessage { get; set; }
    public bool IsActionMessage { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsGiganticEmote { get; set; }
    public bool IsPing { get; set; }

    public string Message { get; set; }
    public IChatUser Sender { get; set; } = new ChzzkChatUser();
    public IChatChannel Channel { get; set; } = new ChzzkChatChannel("");
    public IChatEmote[] Emotes { get; set; } = [];

    public ChzzkChatMessage(string id, string message, IChatUser sender, IChatChannel channel)
    {
      Id = id;
      Message = message;
      Sender = sender;
      Channel = channel;
    }

    public static ChzzkChatMessage FromRaw(JSONNode body)
    {
      string id = body["msgTime"].Value;
      string message = body["msg"].Value;

      string userId = body["uid"].Value;
      string channelId = body["cid"].Value;
      var sender = ChzzkChatUser.FromRaw(body["profile"].Value, userId, channelId);
      var channel = new ChzzkChatChannel(channelId);

      var emotes = new List<ChzzkChatEmote>();
      var emojis = JSON.Parse(body["extras"])?["emojis"];
      if (emojis != null)
      {
        foreach (var emoji in emojis.AsObject)
        {
          string name = $"{{:{emoji.Key}:}}";
          foreach (var index in FindAllIndexes(message, name))
          {
            var emote = new ChzzkChatEmote()
            {
              Id = $"chzzk-{emoji.Key}",
              Name = name,
              Uri = emoji.Value.Value,
              StartIndex = index,
              EndIndex = index + emoji.Key.Length + 3,
            };
            emotes.Add(emote);
          }
        }
      }
      emotes.Sort((a, b) => b.StartIndex.CompareTo(a.StartIndex));

      return new ChzzkChatMessage(id, message, sender, channel)
      {
        Emotes = [.. emotes]
      };
    }

    private static IEnumerable<int> FindAllIndexes(string text, string searchString)
    {
      int index = 0;
      while ((index = text.IndexOf(searchString, index)) != -1)
      {
        yield return index;
        index += searchString.Length;
      }
    }
  }

  public record ChzzkChatUser : IChatUser
  {
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PaintedName { get; set; } = "";
    public string Color { get; set; } = "#FFFFFF";
    public bool IsBroadcaster { get; set; }
    public bool IsModerator { get; set; }
    public bool IsSubscriber { get; set; }
    public bool IsVip { get; set; }
    public IChatBadge[] Badges { get; set; } = [];

    private static readonly string[] _darkThemeNameColors = [
      "#EEA05D",
      "#EAA35F",
      "#E98158",
      "#E97F58",
      "#E76D53",
      "#E66D5F",
      "#E16490",
      "#E481AE",
      "#E481AE",
      "#D25FAC",
      "#D263AE",
      "#D66CB4",
      "#D071B6",
      "#AF71B5",
      "#A96BB2",
      "#905FAA",
      "#B38BC2",
      "#9D78B8",
      "#8D7AB8",
      "#7F68AE",
      "#9F99C8",
      "#717DC6",
      "#7E8BC2",
      "#5A90C0",
      "#628DCC",
      "#81A1CA",
      "#ADD2DE",
      "#83C5D6",
      "#8BC8CB",
      "#91CBC6",
      "#83C3BB",
      "#7DBFB2",
      "#AAD6C2",
      "#84C194",
      "#92C896",
      "#94C994",
      "#9FCE8E",
      "#A6D293",
      "#ABD373",
      "#BFDE73",
    ];

    public ChzzkChatUser(string id = "", string name = "", bool isBroadcaster = false, string color = "#FFFFFF")
    {
      Id = id;
      UserName = name;
      DisplayName = name;
      PaintedName = name;
      IsBroadcaster = isBroadcaster;
      Color = color;
    }

    public static ChzzkChatUser FromRaw(string profileJson, string userId, string chatChannelId = "")
    {
      var profile = JSON.Parse(profileJson);
      string userName = profile["nickname"].Value;
      bool isBroadcaster = profile["userRoleCode"].Value == "streamer";
      string color = GetNameColor(profile, chatChannelId);
      var badges = profile["viewerBadges"].AsArray.Children.Select(ChzzkChatBadge.FromRaw).ToArray();

      return new ChzzkChatUser(userId, userName, isBroadcaster, color)
      {
        Badges = badges
      };
    }

    private static string GetNameColor(JSONNode profile, string chatChannelId)
    {
      string? titleColor = profile["title"]?["color"]?.Value;
      if (titleColor != null)
      {
        return titleColor;
      }

      string hashes = profile["userIdHash"].Value + chatChannelId;
      int charSum = 0;
      foreach (var c in hashes)
      {
        charSum += c;
      }

      return _darkThemeNameColors[charSum % _darkThemeNameColors.Length];
    }
  }

  public class ChzzkChatBadge : IChatBadge
  {
    public EBadgeType Type { get; set; } = EBadgeType.Image;
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Content { get; set; } = "";

    public ChzzkChatBadge(string id, string content)
    {
      Id = id;
      Name = id;
      Content = content;
    }

    public static ChzzkChatBadge FromRaw(JSONNode badgeNode)
    {
      var badge = badgeNode["badge"].AsObject;
      string id = badge["badgeId"].Value;
      string content = badge["imageUrl"].Value;
      return new ChzzkChatBadge(id, content);
    }
  }

  public record ChzzkChatEmote : IChatEmote
  {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Uri { get; set; } = "";
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public EAnimationType Animation { get; set; } = EAnimationType.AUTODETECT;
  }
}
