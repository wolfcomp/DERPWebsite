namespace PDPWebsite.Models;

public record Resource(Guid? Id, Guid CategoryId, Guid ExpansionId, string HtmlContent, string MarkdownContent, string PageName, ulong WriterId, bool Published)
{
    public Category Category { get; set; }
    public Expansion Expansion { get; set; }
};

public record Category(Guid? Id, string Name, string Description, string IconUrl)
{
    public List<Resource> Resources { get; set; }
};

public record Expansion(Guid? Id, string Name, string Description, string IconUrl)
{
    public List<Resource> Resources { get; set; }
};