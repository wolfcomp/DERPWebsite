namespace PDPWebsite.Models;

public record Quote(Guid? Id, string Text, string Title, float? Chance, ulong Creator, ulong Target, DateTime CreatedAt, uint Color);
