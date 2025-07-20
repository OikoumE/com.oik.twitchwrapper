using Newtonsoft.Json.Linq;

public class TwitchChannelPointHandler
{
    /*
    // automatic v2
    //https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchannel_points_automatic_reward_redemptionadd-v2
   {
  "subscription": {
    "id": "7297f7eb-3bf5-461f-8ae6-7cd7781ebce3",
    "status": "enabled",
    "type": "channel.channel_points_automatic_reward_redemption.add",
    "version": "2",
    "condition": {
      "broadcaster_user_id": "12826"
    },
    "transport": {
      "method": "webhook",
      "callback": "https://example.com/webhooks/callback"
    },
    "created_at": "2024-02-23T21:12:33.771005262Z",
    "cost": 0
  },
  "event": {
    "broadcaster_user_id": "12826",
    "broadcaster_user_name": "Twitch",
    "broadcaster_user_login": "twitch",
    "user_id": "141981764",
    "user_name": "TwitchDev",
    "user_login": "twitchdev",
    "id": "f024099a-e0fe-4339-9a0a-a706fb59f353",
    "reward": {
      "type": "send_highlighted_message",
      "channel_points": 100,
      "emote": null
    },
    "message": {
      "text": "Hello world! VoHiYo",
      "fragments": [
        {
          "type": "text",
          "text": "Hello world! ",
          "emote": null
        },
        {
          "type": "emote",
          "text": "VoHiYo",
          "emote": {
            "id": "81274"
          }
        }
      ]
    },
    "redeemed_at": "2024-08-12T21:14:34.260398045Z"
  }
  }
  }*/
    /*
    // automatic v1
    //https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchannel_points_automatic_reward_redemptionadd
   {
  "subscription": {
    "id": "7297f7eb-3bf5-461f-8ae6-7cd7781ebce3",
    "status": "enabled",
    "type": "channel.channel_points_automatic_reward_redemption.add",
    "version": "1",
    "condition": {
      "broadcaster_user_id": "12826"
    },
    "transport": {
      "method": "webhook",
      "callback": "https://example.com/webhooks/callback"
    },
    "created_at": "2024-02-23T21:12:33.771005262Z",
    "cost": 0
  },
  "event": {
    "broadcaster_user_id": "12826",
    "broadcaster_user_name": "Twitch",
    "broadcaster_user_login": "twitch",
    "user_id": "141981764",
    "user_name": "TwitchDev",
    "user_login": "twitchdev",
    "id": "f024099a-e0fe-4339-9a0a-a706fb59f353",
    "reward": {
      "type": "send_highlighted_message",
      "cost": 100,
      "unlocked_emote": null
    },
    "message": {
      "text": "Hello world! VoHiYo",
      "emotes": [
        {
          "id": "81274",
          "begin": 13,
          "end": 18
        }
      ]
    },
    "user_input": "Hello world! VoHiYo ",
    "redeemed_at": "2024-02-23T21:14:34.260398045Z"
  }
  }
  }*/
    /*
    // regular
    //https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchannel_points_custom_reward_redemptionadd
  {
    "subscription": {
        "id": "f1c2a387-161a-49f9-a165-0f21d7a4e1c4",
        "type": "channel.channel_points_custom_reward_redemption.add",
        "version": "1",
        "status": "enabled",
        "cost": 0,
        "condition": {
            "broadcaster_user_id": "1337",
            "reward_id": "92af127c-7326-4483-a52b-b0da0be61c01" // optional; gets notifications for a specific reward
        },
         "transport": {
            "method": "webhook",
            "callback": "https://example.com/webhooks/callback"
        },
        "created_at": "2019-11-16T10:11:12.634234626Z"
    },
    "event": {
        "id": "17fa2df1-ad76-4804-bfa5-a40ef63efe63",
        "broadcaster_user_id": "1337",
        "broadcaster_user_login": "cool_user",
        "broadcaster_user_name": "Cool_User",
        "user_id": "9001",
        "user_login": "cooler_user",
        "user_name": "Cooler_User",
        "user_input": "pogchamp",
        "status": "unfulfilled",
        "reward": {
            "id": "92af127c-7326-4483-a52b-b0da0be61c01",
            "title": "title",
            "cost": 100,
            "prompt": "reward prompt"
        },
        "redeemed_at": "2020-07-15T17:16:03.17106713Z"
    }
  }
  }*/


    public static ChannelPointRewardRedemption ParseChannelPointRewardPayload(JObject payload)
    {
        var eventNotification = payload?["event"];
        // var message = eventNotification?["message"];
        // var messageText = message?["text"]?.ToString();

        //TODO separate between auto and not
        // fuck donkey!

        var chatterUserId = eventNotification?["user_id"]?.ToString();
        var chatterUserLogin = eventNotification?["user_login"]?.ToString();
        var chatterUserName = eventNotification?["user_name"]?.ToString();

        var reward = eventNotification?["reward"];
        var rewardTitle = reward?["title"]?.ToString();
        var prompt = reward?["promt"]?.ToString();

        return new ChannelPointRewardRedemption(
            chatterUserId,
            chatterUserLogin,
            chatterUserName,
            rewardTitle,
            prompt);
    }
}


public class ChannelPointRewardRedemption
{
    public string RewardPrompt;
    public string RewardTitle;
    public string UserId;
    public string UserLogin;
    public string UserName;

    public ChannelPointRewardRedemption(string userID, string userLogin, string userName, string rewardTitle,
        string rewardPrompt)
    {
        UserId = userID;
        UserLogin = userLogin;
        UserName = userName;
        RewardTitle = rewardTitle;
        RewardPrompt = rewardPrompt;
    }
}