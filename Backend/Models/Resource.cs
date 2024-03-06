namespace PDPWebsite.Models;

public record Resource(Guid? Id, Guid CategoryId, Guid? TierId, string HtmlContent, string MarkdownContent, string PageName, ulong WriterId, bool Published)
{
    public Category Category { get; set; }
    public Tier? Tier { get; set; }
    public List<ResourceFile> Files { get; set; }
}

public record Category(Guid? Id, string Name, string Description, string IconUrl, string Path, bool HasTiers)
{
    public List<Resource> Resources { get; set; }
    public List<Tier> Tiers { get; set; }
}

public record Tier(Guid? Id, Guid CategoryId, string Name, string IconUrl, string Path)
{
    public List<Resource> Resources { get; set; }
    public Category Category { get; set; }
}

public record ResourceFile(Guid? Id, Guid ResourceId, string Name, string Path)
{
    public Resource Resource { get; set; }
}