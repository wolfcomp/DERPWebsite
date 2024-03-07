namespace DERPWebsite.Models;

public record Quote(Guid? Id, string Text, string Title, uint? Chance, ulong Creator, ulong Target, DateTime CreatedAt, uint Color);
