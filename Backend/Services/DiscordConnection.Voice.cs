using Discord.Net;
using Discord.WebSocket;
using Discord;

namespace PDPWebsite.Services;

public partial class DiscordConnection
{
    private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        try
        {
            if (before.VoiceChannel == after.VoiceChannel)
                return;
            _logger.LogTrace($"UserVoiceStateUpdated: {user}, {before}, {after}");
            if (before.VoiceChannel != null && (after.VoiceChannel == null || before.VoiceChannel.Id != after.VoiceChannel.Id))
            {
                _logger.LogTrace($"User: {user} disconnected from voice channel {before}");
                if (before.VoiceChannel.ConnectedUsers.Count == 0 && TempChannels.ContainsKey(before.VoiceChannel.Id))
                {
                    _logger.LogTrace($"This was the last user that left a temp channel");
                    await before.VoiceChannel.DeleteAsync();
                    TempChannels.Remove(before.VoiceChannel.Id, out _);
                }
                else if (before.VoiceChannel.ConnectedUsers.Count > 0 && TempChannels.ContainsKey(before.VoiceChannel.Id))
                {
                    _logger.LogTrace($"This was not the last user that left a temp channel");
                    _logger.LogTrace($"Users discovered: {string.Join(", ", before.VoiceChannel.ConnectedUsers.Select(t => t.ToString()))}");
                }
                else if (!TempChannels.ContainsKey(before.VoiceChannel.Id))
                {
                    _logger.LogTrace($"This was not a temp channel");
                }
            }
            if (after.VoiceChannel?.Id == _tempVoiceChannel.Id)
            {
                _logger.LogTrace($"User: {user} connected to temp voice setup.");
                var names = _redisClient.GetObj<Dictionary<ulong, string>>($"voice_names");
                if (names == null || !names.TryGetValue(user.Id, out var name))
                    name = user.Username;
                var channel = await Guild!.CreateVoiceChannelAsync(name, x =>
                {
                    x.CategoryId = _tempVoiceChannel.CategoryId;
                    x.PermissionOverwrites = new List<Overwrite>
                    {
                    new(Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(connect: PermValue.Allow, viewChannel: PermValue.Allow, speak: PermValue.Allow, sendMessages: PermValue.Allow)),
                    new(user.Id, PermissionTarget.User, new OverwritePermissions(connect: PermValue.Allow, viewChannel: PermValue.Allow, speak: PermValue.Allow, sendMessages: PermValue.Allow))
                    };
                });
                TempChannels.TryAdd(channel.Id, user.Id);
                await ((SocketGuildUser)user).ModifyAsync(x => x.Channel = channel);
            }
            _redisClient.SetObj("discord_temp_channels", TempChannels);
        }
        catch (HttpException exception)
        {
            _logger.LogError(exception, "UserVoiceStateUpdated");
        }
    }

    private async Task CheckVoice()
    {
        try
        {
            if (Guild != null)
            {
                var toRemove = new List<ulong>();
                foreach (var (channelId, _) in TempChannels)
                {
                    var channel = Guild!.GetVoiceChannel(channelId);
                    if (channel == null)
                    {
                        toRemove.Add(channelId);
                        continue;
                    }
                    if (channel.ConnectedUsers.Count != 0)
                        continue;
                    toRemove.Add(channelId);
                    await channel.DeleteAsync();
                }
                foreach (var id in toRemove)
                    TempChannels.Remove(id, out _);
                _redisClient.SetObj("discord_temp_channels", TempChannels);
            }
            await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            CheckVoice();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CheckVoice");
        }
    }
}
