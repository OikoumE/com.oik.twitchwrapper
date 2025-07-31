using System;

public static class TwitchApiScopes
{
    public enum EScope
    {
        AnalyticsReadExtensions, // "analytics:read:extensions"
        AnalyticsReadGames, // "analytics:read:games"
        BitsRead, // "bits:read"
        ChannelBot, // "channel:bot"
        ChannelEditCommercial, // "channel:edit:commercial"
        ChannelManageAnnouncements, // "channel:manage:announcements"
        ChannelManageBroadcast, // "channel:manage:broadcast"
        ChannelManageExtensions, // "channel:manage:extensions"
        ChannelManageGoals, // "channel:manage:goals"
        ChannelManagePolls, // "channel:manage:polls"
        ChannelManagePredictions, // "channel:manage:predictions"
        ChannelManageRedemptions, // "channel:manage:redemptions"
        ChannelManageSchedule, // "channel:manage:schedule"
        ChannelManageShieldMode, // "channel:manage:shield_mode"
        ChannelManageShoutouts, // "channel:manage:shoutouts"
        ChannelManageVips, // "channel:manage:vips"
        ChannelManageVideos, // "channel:manage:videos"
        ChannelManageWhispers, // "channel:manage:whispers"
        ChannelReadCommunityPoints, // "channel:read:community_points"
        ChannelReadEditors, // "channel:read:editors"
        ChannelReadGoals, // "channel:read:goals"
        ChannelReadHypeTrain, // "channel:read:hype_train"
        ChannelReadPolls, // "channel:read:polls"
        ChannelReadPredictions, // "channel:read:predictions"
        ChannelReadRedemptions, // "channel:read:redemptions"
        ChannelReadSubscriptions, // "channel:read:subscriptions"
        ChannelReadVips, // "channel:read:vips"
        ChannelReadStreamKey, // "channel:read:stream_key"
        ClipsEdit, // "clips:edit"
        ModerationRead, // "moderation:read"
        ModerationManageAutomodSettings, // "moderation:manage:automod_settings"
        ModerationManageBannedUsers, // "moderation:manage:banned_users"
        ModerationManageBlockedTerms, // "moderation:manage:blocked_terms"
        ModerationManageChatSettings, // "moderation:manage:chat_settings"
        ModerationManageShieldMode, // "moderation:manage:shield_mode"
        ModerationManageSlowMode, // "moderation:manage:slow_mode"
        ModerationManageUnbanRequests, // "moderation:manage:unban_requests"
        UserEdit, // "user:edit"
        UserEditBroadcast, // "user:edit:broadcast"
        UserManageBlockedUsers, // "user:manage:blocked_users"
        UserManageWhispers, // "user:manage:whispers"
        UserReadBlockedUsers, // "user:read:blocked_users"
        UserReadBroadcast, // "user:read:broadcast"
        UserReadEmail, // "user:read:email"
        UserReadFollowers, // "user:read:followers"
        UserReadSubscriptions, // "user:read:subscriptions"
        UserReadFollows, // "user:read:follows"
        UserReadStreamKey, // "user:read:stream_key"
        UserReadVips, // "user:read:subscriptions"
        WhispersRead, // "user:read:vips"
        WhispersEdit // "user:read:broadcast"
    }

    public static string GetScope(EScope scope)
    {
        return scope switch
        {
            EScope.AnalyticsReadExtensions => "analytics:read:extensions",
            EScope.AnalyticsReadGames => "analytics:read:games",
            EScope.BitsRead => "bits:read",
            EScope.ChannelBot => "channel:bot",
            EScope.ChannelEditCommercial => "channel:edit:commercial",
            EScope.ChannelManageAnnouncements => "channel:manage:announcements",
            EScope.ChannelManageBroadcast => "channel:manage:broadcast",
            EScope.ChannelManageExtensions => "channel:manage:extensions",
            EScope.ChannelManageGoals => "channel:manage:goals",
            EScope.ChannelManagePolls => "channel:manage:polls",
            EScope.ChannelManagePredictions => "channel:manage:predictions",
            EScope.ChannelManageRedemptions => "channel:manage:redemptions",
            EScope.ChannelManageSchedule => "channel:manage:schedule",
            EScope.ChannelManageShieldMode => "channel:manage:shield_mode",
            EScope.ChannelManageShoutouts => "channel:manage:shoutouts",
            EScope.ChannelManageVips => "channel:manage:vips",
            EScope.ChannelManageVideos => "channel:manage:videos",
            EScope.ChannelManageWhispers => "channel:manage:whispers",
            EScope.ChannelReadCommunityPoints => "channel:read:community_points",
            EScope.ChannelReadEditors => "channel:read:editors",
            EScope.ChannelReadGoals => "channel:read:goals",
            EScope.ChannelReadHypeTrain => "channel:read:hype_train",
            EScope.ChannelReadPolls => "channel:read:polls",
            EScope.ChannelReadPredictions => "channel:read:predictions",
            EScope.ChannelReadRedemptions => "channel:read:redemptions",
            EScope.ChannelReadSubscriptions => "channel:read:subscriptions",
            EScope.ChannelReadVips => "channel:read:vips",
            EScope.ChannelReadStreamKey => "channel:read:stream_key",
            EScope.ClipsEdit => "clips:edit",
            EScope.ModerationRead => "moderation:read",
            EScope.ModerationManageAutomodSettings => "moderation:manage:automod_settings",
            EScope.ModerationManageBannedUsers => "moderation:manage:banned_users",
            EScope.ModerationManageBlockedTerms => "moderation:manage:blocked_terms",
            EScope.ModerationManageChatSettings => "moderation:manage:chat_settings",
            EScope.ModerationManageShieldMode => "moderation:manage:shield_mode",
            EScope.ModerationManageSlowMode => "moderation:manage:slow_mode",
            EScope.ModerationManageUnbanRequests => "moderation:manage:unban_requests",
            EScope.UserEdit => "user:edit",
            EScope.UserEditBroadcast => "user:edit:broadcast",
            EScope.UserManageBlockedUsers => "user:manage:blocked_users",
            EScope.UserManageWhispers => "user:manage:whispers",
            EScope.UserReadBlockedUsers => "user:read:blocked_users",
            EScope.UserReadBroadcast => "user:read:broadcast",
            EScope.UserReadEmail => "user:read:email",
            EScope.UserReadFollowers => "user:read:followers",
            EScope.UserReadSubscriptions => "user:read:subscriptions",
            EScope.UserReadFollows => "user:read:follows",
            EScope.UserReadStreamKey => "user:read:stream_key",
            EScope.UserReadVips => "user:read:subscriptions",
            EScope.WhispersRead => "user:read:vips",
            EScope.WhispersEdit => "user:read:broadcast",
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
        };
    }

    public static string[] GetUrlScopes(EScope[] extraScopes)
    {
        var scopes = new string[extraScopes.Length];
        for (var i = 0; i < extraScopes.Length; i++)
            scopes[i] = GetScope(extraScopes[i]);
        return scopes;
    }
}