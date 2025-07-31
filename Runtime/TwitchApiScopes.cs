using System;

public static class TwitchApiScopes
{
    public enum EScope
    {
        AnalyticsReadExtensions,
        AnalyticsReadGames,
        BitsRead,
        ChannelBot,
        ChannelEditCommercial,
        ModeratorManageAnnouncements,
        ChannelManageBroadcast,
        ChannelManagePolls,
        ChannelManagePredictions,
        ChannelManageRedemptions,
        ChannelManageSchedule,
        ModeratorManageShoutouts,
        ChannelManageVips,
        ChannelManageVideos,
        ChannelManageWhispers,
        ChannelReadGoals,
        ChannelReadHypeTrain,
        ChannelReadPolls,
        ChannelReadPredictions,
        ChannelReadRedemptions,
        ChannelReadSubscriptions,
        ChannelReadVips,
        ChannelReadStreamKey,
        ClipsEdit,
        ModeratorManageAutomodSettings,
        ModeratorManageBannedUsers,
        ModeratorManageBlockedTerms,
        ModeratorManageChatSettings,
        ModeratorManageShieldMode,
        ModeratorManageUnbanRequests,
        UserEditBroadcast,
        UserManageBlockedUsers,
        UserManageWhispers,
        UserReadBlockedUsers,
        UserReadBroadcast,
        UserReadEmail,
        UserReadSubscriptions,
        ModeratorReadFollowers,
        UserReadFollows,
        UserReadVips
    }

    public static string GetScope(EScope scope)
    {
        //moderator:manage:announcements
        return scope switch
        {
            EScope.AnalyticsReadExtensions => "analytics:read:extensions",
            EScope.AnalyticsReadGames => "analytics:read:games",
            EScope.BitsRead => "bits:read",
            EScope.ChannelBot => "channel:bot",
            EScope.ChannelEditCommercial => "channel:edit:commercial",
            EScope.ChannelManageBroadcast => "channel:manage:broadcast",
            EScope.ChannelManagePolls => "channel:manage:polls",
            EScope.ChannelManagePredictions => "channel:manage:predictions",
            EScope.ChannelManageRedemptions => "channel:manage:redemptions",
            EScope.ChannelManageSchedule => "channel:manage:schedule",
            EScope.ChannelManageVips => "channel:manage:vips",
            EScope.ChannelManageVideos => "channel:manage:videos",
            EScope.ChannelManageWhispers => "user:manage:whispers",
            EScope.ChannelReadGoals => "channel:read:goals",
            EScope.ChannelReadHypeTrain => "channel:read:hype_train",
            EScope.ChannelReadPolls => "channel:read:polls",
            EScope.ChannelReadPredictions => "channel:read:predictions",
            EScope.ChannelReadRedemptions => "channel:read:redemptions",
            EScope.ChannelReadSubscriptions => "channel:read:subscriptions",
            EScope.ChannelReadVips => "channel:read:vips",
            EScope.ChannelReadStreamKey => "channel:read:stream_key",
            EScope.ClipsEdit => "clips:edit",
            EScope.ModeratorManageAutomodSettings => "moderator:manage:automod_settings",
            EScope.ModeratorManageBannedUsers => "moderator:manage:banned_users",
            EScope.ModeratorManageBlockedTerms => "moderator:manage:blocked_terms",
            EScope.ModeratorManageChatSettings => "moderator:manage:chat_settings",
            EScope.ModeratorManageShieldMode => "moderator:manage:shield_mode",
            EScope.ModeratorManageUnbanRequests => "moderator:manage:unban_requests",
            EScope.ModeratorManageShoutouts => "moderator:manage:shoutouts",
            EScope.ModeratorManageAnnouncements => "moderator:manage:announcements",
            EScope.ModeratorReadFollowers => "moderator:read:followers",
            EScope.UserEditBroadcast => "user:edit:broadcast",
            EScope.UserManageBlockedUsers => "user:manage:blocked_users",
            EScope.UserManageWhispers => "user:manage:whispers",
            EScope.UserReadBlockedUsers => "user:read:blocked_users",
            EScope.UserReadBroadcast => "user:read:broadcast",
            EScope.UserReadEmail => "user:read:email",
            EScope.UserReadSubscriptions => "user:read:subscriptions",
            EScope.UserReadFollows => "user:read:follows",
            EScope.UserReadVips => "user:read:subscriptions",
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