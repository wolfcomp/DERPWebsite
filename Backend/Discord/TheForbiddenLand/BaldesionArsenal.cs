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
        var embed = new EmbedBuilder();

        switch (portalOption)
        {
            case BAPortal.MAP:
                embed.WithTitle("All locations portals spawn");
                embed.WithDescription("These are all the locations of the portals leading into the Baldesion Arsenal\n" +
                                      "Support portals only have 8 portals, " +
                                      "and they spawn randomly out of all 48 possible location, " +
                                      "for this reason we allow Aetherial Stabilizers to be used for Support portals.");
                embed.WithImageUrl("https://i.imgur.com/r9mxDnT.png");
                embed.WithFooter("Image credited to: Noranda Lailuna of Excalibur and The Help Lines");
                break;

            case BAPortal.ASSIGNMENT:
                embed.WithTitle("Portal Assignment Macro");
                embed.WithDescription("```/macroicon \"Chain Stratagem\"\n" +
                                      "/party Portal Assignments!\n" +
                                      "/party 1 <1>\n" +
                                      "/party 2 <2>\n" +
                                      "/party 3 <3>\n" +
                                      "/party 4 <4>\n" +
                                      "/party 5 <5>\n" +
                                      "/party 6 <6>\n" +
                                      "/party 7 <7>\n" +
                                      "/party 8 <8> <se.6>```\n\n" +
                                      "Feel free to customize this to your own liking!");
                break;
        }

        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = embed.Build();
        });
    }

    [SlashCommand("first", "What to do as a newcomer to the Baldesion Arsenal")]
    public async Task FirstTimer()
    {
        var embed = new EmbedBuilder();
        embed.WithTitle("First time doing BA?");
        embed.WithDescription("Use the following website to find your Logos Actions!\nhttps://lynn.pet/ba/");
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = embed.Build();
        });
    }

    [SlashCommand("encounters")]
    public async Task Fights([SlashCommand("fight")] Fight fight)
    {
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "TODO: Implement the fights of BA");
    }

}

public enum BAPortal
{
    MAP = 0,
    ASSIGNMENT,
}

public enum Fight
{
    ART = 0,
    OWAIN,
    RAIDEN,
    ABSOLUTE_VIRTUE,
    OZMA,
}
