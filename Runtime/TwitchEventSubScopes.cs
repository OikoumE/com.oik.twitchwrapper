using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//TODO 
// https://dev.twitch.tv/docs/authentication/scopes/
// make sure scopes are right!
//TODO
// proper conditions for each event
// https://dev.twitch.tv/docs/eventsub/eventsub-reference/#conditions
public static class TwitchEventSubScopes
{
    public enum EScope
    {
        AutomodMessageHold,
        AutomodMessageUpdate,
        AutomodSettingsUpdate,
        AutomodTermsUpdate,
        ChannelBitsUse,
        ChannelUpdate,
        ChannelFollow,
        ChannelAdBreakBegin,
        ChannelChatClear,
        ChannelChatClearUserMessages,
        ChannelChatMessage,
        ChannelChatMessageDelete,
        ChannelChatNotification,
        ChannelChatSettingsUpdate,
        ChannelChatUserMessageHold,
        ChannelChatUserMessageUpdate,
        ChannelSharedChatBegin,
        ChannelSharedChatUpdate,
        ChannelSharedChatEnd,
        ChannelSubscribe,
        ChannelSubscriptionEnd,
        ChannelSubscriptionGift,
        ChannelSubscriptionMessage,
        ChannelCheer,
        ChannelRaid,
        ChannelBan,
        ChannelUnban,
        ChannelUnbanRequestCreate,
        ChannelUnbanRequestResolve,
        ChannelModerate,
        ChannelModeratorAdd,
        ChannelModeratorRemove,
        ChannelGuestStarSessionBegin,
        ChannelGuestStarSessionEnd,
        ChannelGuestStarGuestUpdate,
        ChannelGuestStarSettingsUpdate,
        ChannelPointsAutomaticRewardRedemptionAdd,
        ChannelPointsCustomRewardAdd,
        ChannelPointsCustomRewardUpdate,
        ChannelPointsCustomRewardRemove,
        ChannelPointsCustomRewardRedemptionAdd,
        ChannelPointsCustomRewardRedemptionUpdate,
        ChannelPollBegin,
        ChannelPollProgress,
        ChannelPollEnd,
        ChannelPredictionBegin,
        ChannelPredictionProgress,
        ChannelPredictionLock,
        ChannelPredictionEnd,
        ChannelSuspiciousUserMessage,
        ChannelSuspiciousUserUpdate,
        ChannelVipAdd,
        ChannelVipRemove,
        ChannelWarningAcknowledge,
        ChannelWarningSend,
        ChannelCharityCampaignDonate,
        ChannelCharityCampaignStart,
        ChannelCharityCampaignProgress,
        ChannelCharityCampaignStop,
        ExtensionBitsTransactionCreate,
        ChannelGoalBegin,
        ChannelGoalProgress,
        ChannelGoalEnd,
        ChannelHypeTrainBegin,
        ChannelHypeTrainProgress,
        ChannelHypeTrainEnd,
        ChannelShieldModeBegin,
        ChannelShieldModeEnd,
        ChannelShoutoutCreate,
        ChannelShoutoutReceive,
        StreamOnline,
        StreamOffline,
        UserWhisperMessage
    }

    private static readonly ScopeApiVersion[] EventSubScopes =
    {
        new(EScope.AutomodMessageHold,
            "automod.message.hold", "2", "moderator:manage:automod"),
        new(EScope.AutomodMessageUpdate,
            "automod.message.update", "2", "moderator:manage:automod"),
        new(EScope.AutomodSettingsUpdate,
            "automod.settings.update", "1", "moderator:manage:automod_settings"),
        new(EScope.AutomodTermsUpdate,
            "automod.terms.update", "1", "moderator:manage:automod_settings"),
        new(EScope.ChannelBitsUse,
            "channel.bits.use", "1", "bits:read"),
        new(EScope.ChannelUpdate,
            "channel.update", "2", "channel:manage:broadcast"),
        new(EScope.ChannelFollow,
            "channel.follow", "2", "moderator:read:followers"),
        new(EScope.ChannelAdBreakBegin,
            "channel.ad_break.begin", "1", "channel:read:ads"),
        new(EScope.ChannelChatClear,
            "channel.chat.clear", "1", "channel:moderate"),
        new(EScope.ChannelChatClearUserMessages,
            "channel.chat.clear_user_messages", "1", "channel:moderate"),
        new(EScope.ChannelChatMessage,
            "channel.chat.message", "1", "user:read:chat"), //"channel:bot"),
        new(EScope.ChannelChatMessageDelete,
            "channel.chat.message_delete", "1", "channel:moderate"),
        new(EScope.ChannelChatNotification,
            "channel.chat.notification", "1", "channel:bot"),
        new(EScope.ChannelChatSettingsUpdate,
            "channel.chat_settings.update", "1", "moderator:manage:chat_settings"),
        new(EScope.ChannelChatUserMessageHold,
            "channel.chat.user_message_hold", "1", "channel:moderate"),
        new(EScope.ChannelChatUserMessageUpdate,
            "channel.chat.user_message_update", "1", "channel:moderate"),
        new(EScope.ChannelSharedChatBegin,
            "channel.shared_chat.begin", "1", "channel:manage:extensions"),
        new(EScope.ChannelSharedChatUpdate,
            "channel.shared_chat.update", "1", "channel:manage:extensions"),
        new(EScope.ChannelSharedChatEnd,
            "channel.shared_chat.end", "1", "channel:manage:extensions"),
        new(EScope.ChannelSubscribe,
            "channel.subscribe", "1", "channel:read:subscriptions"),
        new(EScope.ChannelSubscriptionEnd,
            "channel.subscription.end", "1", "channel:read:subscriptions"),
        new(EScope.ChannelSubscriptionGift,
            "channel.subscription.gift", "1", "channel:read:subscriptions"),
        new(EScope.ChannelSubscriptionMessage,
            "channel.subscription.message", "1", "channel:read:subscriptions"),
        new(EScope.ChannelCheer,
            "channel.cheer", "1", "bits:read"),
        new(EScope.ChannelRaid,
            "channel.raid", "1", "channel:manage:raids"),
        new(EScope.ChannelBan,
            "channel.ban", "1", "moderator:manage:banned_users"),
        new(EScope.ChannelUnban,
            "channel.unban", "1", "moderator:manage:banned_users"),
        new(EScope.ChannelUnbanRequestCreate,
            "channel.unban_request.create", "1", "moderator:manage:unban_requests"),
        new(EScope.ChannelUnbanRequestResolve,
            "channel.unban_request.resolve", "1", "moderator:manage:unban_requests"),
        new(EScope.ChannelModerate,
            "channel.moderate", "2", "channel:moderate"),
        new(EScope.ChannelModeratorAdd,
            "channel.moderator.add", "1", "channel:manage:moderators"),
        new(EScope.ChannelModeratorRemove,
            "channel.moderator.remove", "1", "channel:manage:moderators"),
        new(EScope.ChannelGuestStarSessionBegin,
            "channel.guest_star_session.begin", "beta", "channel:manage:guest_star"),
        new(EScope.ChannelGuestStarSessionEnd,
            "channel.guest_star_session.end", "beta", "channel:manage:guest_star"),
        new(EScope.ChannelGuestStarGuestUpdate,
            "channel.guest_star_guest.update", "beta", "channel:manage:guest_star"),
        new(EScope.ChannelGuestStarSettingsUpdate,
            "channel.guest_star_settings.update", "beta", "channel:manage:guest_star"),
        new(EScope.ChannelPointsAutomaticRewardRedemptionAdd,
            "channel.channel_points_automatic_reward_redemption.add", "2", "channel:manage:redemptions"),
        new(EScope.ChannelPointsCustomRewardAdd,
            "channel.channel_points_custom_reward.add", "1", "channel:manage:redemptions"),
        new(EScope.ChannelPointsCustomRewardUpdate,
            "channel.channel_points_custom_reward.update", "1", "channel:manage:redemptions"),
        new(EScope.ChannelPointsCustomRewardRemove,
            "channel.channel_points_custom_reward.remove", "1", "channel:manage:redemptions"),
        new(EScope.ChannelPointsCustomRewardRedemptionAdd,
            "channel.channel_points_custom_reward_redemption.add", "1", "channel:manage:redemptions"),
        new(EScope.ChannelPointsCustomRewardRedemptionUpdate,
            "channel.channel_points_custom_reward_redemption.update", "1", "channel:manage:redemptions"),
        new(EScope.ChannelPollBegin,
            "channel.poll.begin", "1", "channel:manage:polls"),
        new(EScope.ChannelPollProgress,
            "channel.poll.progress", "1", "channel:manage:polls"),
        new(EScope.ChannelPollEnd,
            "channel.poll.end", "1", "channel:manage:polls"),
        new(EScope.ChannelPredictionBegin,
            "channel.prediction.begin", "1", "channel:manage:predictions"),
        new(EScope.ChannelPredictionProgress,
            "channel.prediction.progress", "1", "channel:manage:predictions"),
        new(EScope.ChannelPredictionLock,
            "channel.prediction.lock", "1", "channel:manage:predictions"),
        new(EScope.ChannelPredictionEnd,
            "channel.prediction.end", "1", "channel:manage:predictions"),
        new(EScope.ChannelSuspiciousUserMessage,
            "channel.suspicious_user.message", "1", "moderator:read:suspicious_users"),
        new(EScope.ChannelSuspiciousUserUpdate,
            "channel.suspicious_user.update", "1", "moderator:read:suspicious_users"),
        new(EScope.ChannelVipAdd,
            "channel.vip.add", "1", "channel:manage:vips"),
        new(EScope.ChannelVipRemove,
            "channel.vip.remove", "1", "channel:manage:vips"),
        new(EScope.ChannelWarningAcknowledge,
            "channel.warning.acknowledge", "1", "moderator:manage:warnings"),
        new(EScope.ChannelWarningSend,
            "channel.warning.send", "1", "moderator:manage:warnings"),
        new(EScope.ChannelCharityCampaignDonate,
            "channel.charity_campaign.donate", "1", "channel:read:charity"),
        new(EScope.ChannelCharityCampaignStart,
            "channel.charity_campaign.start", "1", "channel:read:charity"),
        new(EScope.ChannelCharityCampaignProgress,
            "channel.charity_campaign.progress", "1", "channel:read:charity"),
        new(EScope.ChannelCharityCampaignStop,
            "channel.charity_campaign.stop", "1", "channel:read:charity"),
        new(EScope.ExtensionBitsTransactionCreate,
            "extension.bits_transaction.create", "1", "analytics:read:extensions"),
        new(EScope.ChannelGoalBegin,
            "channel.goal.begin", "1", "channel:read:goals"),
        new(EScope.ChannelGoalProgress,
            "channel.goal.progress", "1", "channel:read:goals"),
        new(EScope.ChannelGoalEnd,
            "channel.goal.end", "1", "channel:read:goals"),
        new(EScope.ChannelHypeTrainBegin,
            "channel.hype_train.begin", "1", "channel:read:hype_train"),
        new(EScope.ChannelHypeTrainProgress,
            "channel.hype_train.progress", "1", "channel:read:hype_train"),
        new(EScope.ChannelHypeTrainEnd,
            "channel.hype_train.end", "1", "channel:read:hype_train"),
        new(EScope.ChannelShieldModeBegin,
            "channel.shield_mode.begin", "1", "moderator:manage:shield_mode"),
        new(EScope.ChannelShieldModeEnd,
            "channel.shield_mode.end", "1", "moderator:manage:shield_mode"),
        new(EScope.ChannelShoutoutCreate,
            "channel.shoutout.create", "1", "moderator:manage:shoutouts"),
        new(EScope.ChannelShoutoutReceive,
            "channel.shoutout.receive", "1", "moderator:read:shoutouts"),
        new(EScope.StreamOnline,
            "stream.online", "1", "user:read:broadcast"),
        new(EScope.StreamOffline,
            "stream.offline", "1", "user:read:broadcast"),

        new(EScope.UserWhisperMessage,
            "user.whisper.message", "1", "user:manage:whispers")
    };

    public static object GetScopeCondition(EScope s, string broadcasterId)
    {
        //TODO edge case - conditions
        return s switch
        {
            EScope.ChannelRaid => new
            {
                to_broadcaster_user_id = broadcasterId
                //from_broadcaster_user_id = _broadcasterId,
            },
            EScope.ChannelUnbanRequestCreate
                or EScope.ChannelUnbanRequestResolve
                or EScope.ChannelGuestStarGuestUpdate
                or EScope.ChannelGuestStarSessionBegin
                or EScope.ChannelGuestStarSessionEnd
                or EScope.ChannelGuestStarSettingsUpdate
                or EScope.ChannelFollow => new
                {
                    broadcaster_user_id = broadcasterId, moderator_user_id = broadcasterId
                },
            EScope.ChannelPointsCustomRewardAdd
                or EScope.ChannelPointsCustomRewardRedemptionAdd
                or EScope.ChannelPointsCustomRewardRedemptionUpdate
                or EScope.ChannelPointsCustomRewardRemove
                or EScope.ChannelPointsCustomRewardUpdate => new
                {
                    broadcaster_user_id = broadcasterId
                    //optionally reward_id
                },
            _ => new { broadcaster_user_id = broadcasterId, user_id = broadcasterId }
        };
    }

    public static ScopeApiVersion GetApiVersion(EScope eScope, string broadcasterId, out object condition)
    {
        condition = GetScopeCondition(eScope, broadcasterId);
        return EventSubScopes.First(x => x.Scope == eScope);
    }


    public static string GetUrlScopes(EScope[] eScope)
    {
        var scopes = new List<string> { "user:write:chat" };
        foreach (var scopeApiVersion in EventSubScopes)
            if (eScope.Contains(scopeApiVersion.Scope) && !scopes.Contains(scopeApiVersion.UrlScope))
                scopes.Add(scopeApiVersion.UrlScope);

        var scopeString = string.Join(" ", scopes);
        Debugs.Log($"Scopes: {scopeString}");
        return scopeString;
    }

    public static EScope GetScope(string apiName)
    {
        if (string.IsNullOrEmpty(apiName))
            throw new ArgumentNullException(nameof(apiName));
        return EventSubScopes.First(x => x.ApiName == apiName).Scope;
    }
}

public class ScopeApiVersion
{
    public readonly string ApiName;
    public readonly TwitchEventSubScopes.EScope Scope;
    public readonly string UrlScope;
    public readonly string Version;

    public ScopeApiVersion(TwitchEventSubScopes.EScope scope, string apiName, string version, string urlScope)
    {
        Scope = scope;
        ApiName = apiName;
        Version = version;
        UrlScope = urlScope;
    }
}