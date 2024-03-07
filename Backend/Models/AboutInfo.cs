using Discord.WebSocket;

namespace DERPWebsite.Models;

public record AboutInfo(ulong Id, string Description, string? VisualName)
{
    public ulong Id { get; set; } = Id;
    public string Description { get; set; } = Description;
    public string? VisualName { get; set; } = VisualName;

    public void Deconstruct(out ulong id, out string description, out string? visualName)
    {
        id = this.Id;
        description = this.Description;
        visualName = this.VisualName;
    }
}

public record AboutInfoExtended(ulong Id, string Description, string Avatar, string OriginalName, string? VisualName, AboutInfoRoles[] Roles)
{
    public static explicit operator AboutInfo(AboutInfoExtended aboutInfoExtended)
    {
        return new AboutInfo(aboutInfoExtended.Id, aboutInfoExtended.Description, aboutInfoExtended.VisualName);
    }

    public static AboutInfoExtended FromInfo(AboutInfo aboutInfo, SocketGuildUser user, AboutInfoRoles[] roles)
    {
        return new AboutInfoExtended(aboutInfo.Id, aboutInfo.Description, user.GetDisplayAvatarUrl(), user.DisplayName, aboutInfo.VisualName, roles);
    }
}

public record AboutInfoRoles(ulong Id, string Name, string Color);