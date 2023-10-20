using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Discord.TheForbiddenLand;

[SlashCommand("ba")]
public class BaldesionArsenal : ISlashCommandProcessor
{
    private ILogger<BaldesionArsenal> _logger;
    private SocketSlashCommand _arg;

    public BaldesionArsenal(ILogger<BaldesionArsenal> logger, SocketSlashCommand arg)
    {
        _logger = logger;
        _arg = arg;
    }

    [SlashCommand("portals", "The different portal groups and positions")]
    public async Task Portal([SlashCommand("option")] BAPortal portalOption)
    {
        await _arg.RespondAsync("Thinking...");
        var embed = new EmbedBuilder();

        switch (portalOption)
        {
            case BAPortal.MAP:
                embed.WithTitle("Map");
                embed.WithDescription("These are all the locations of the portals leading into the Baldesion Arsenal");
                embed.WithImageUrl("https://i.imgur.com/r9mxDnT.png");
                embed.WithFooter("Image credited to: Noranda Lailuna of Excalibur and The Help Lines");
                break;
            
            case BAPortal.ASSIGNMENT:
                embed.WithTitle("Portal Assignment macro");
                embed.WithDescription("```/macroicon \"Chain Stratagem\"\n/party Portal Assignments!\n/party 1 <1>\n/party 2 <2>\n/party 3 <3>\n/party 4 <4>\n/party 5 <5>\n/party 6 <6>\n/party 7 <7>\n/party 8 <8> <se.6>```");
                break;
            default: await _arg.ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = "you've done something that shouldn't be possible, gj, let Raine or Lex know.";
            });
                break;
        }

        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = embed.Build();
        });
    }
    
    [SlashCommand("first", "What to do as a newcomer to the content")]
    public async Task FirstTimer()
    {
        await _arg.RespondAsync("Thinking...");
        var embed = new EmbedBuilder();
        embed.WithTitle("First time doing BA?");
        embed.WithDescription("Use the following website to find your Logos Actions!\nhttps://lynn.pet/ba/");
    }
}

public enum BAPortal
{
    MAP = 0,
    ASSIGNMENT,
}
