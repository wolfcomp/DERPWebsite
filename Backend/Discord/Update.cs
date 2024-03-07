using System.Text;
using DERPWebsite.Services;
using Discord;
using Discord.WebSocket;

namespace DERPWebsite.Discord;

[SlashCommand("check-update", "Checks for FFXIV game data updates", GuildPermission.ManageChannels)]
public class Update : ISlashCommandProcessor
{
    private readonly SocketSlashCommand _arg;
    private readonly GameClient _gameClient;

    public Update(ILogger<Update> logger, SocketSlashCommand arg, GameClient gameClient)
    {
        _arg = arg;
        _gameClient = gameClient;
    }

    [SlashCommand("update", "Tries to update FFXIV game data")]
    public async Task Check()
    {
        var message = await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Triggering update.");
        await _gameClient.Update(message);
    }

    [SlashCommand("check", "Checks the status of the FFXIV game data")]
    public async Task Status()
    {
        var builder = new EmbedBuilder();
        var sb = new StringBuilder();
        var status = await _gameClient.CheckVersions();
        foreach (var (_, description, curVer, discoveredVer) in status)
        {
            sb.AppendLine($"{description}");
            sb.AppendLine($"Current Version: {curVer}");
            sb.AppendLine($"Discovered Version: {discoveredVer}");
            sb.AppendLine();
        }
        builder.WithTitle("Update Check");
        builder.WithDescription(sb.ToString().Trim(Environment.NewLine.ToCharArray()));
        builder.WithColor(Color.Blue);
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = builder.Build();
            msg.Content = null;
        });
    }
}
