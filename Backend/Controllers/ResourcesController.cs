namespace PDPWebsite.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly Database _database;
    private readonly RedisClient _redisClient;

    public ResourcesController(Database database, RedisClient redisClient)
    {
        _database = database;
        _redisClient = redisClient;
    }

    [HttpGet]
    [Route("")]
    public IActionResult GetResources()
    {
        return Ok(_database.Resources.Include(resource => resource.Category).Include(resource => resource.Expansion).Select(t => new { t.Id, Category = new { t.Category.Id, t.Category.Name, t.Category.Description, t.Category.IconUrl }, Expansion = new { t.Expansion.Id, t.Expansion.Name, t.Expansion.Description, t.Expansion.IconUrl }, t.PageName }));
    }

    [HttpPut]
    [Route("")]
    [ServiceFilter(typeof(AuthFilter))]
    public IActionResult PutResource([FromBody] ResourceHttp resource)
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Split(" ").Last();
        var loginRecord = _redisClient.GetObj<Login>(token)!;
        if (resource.Id == Guid.Empty)
        {
            _database.Resources.Add(resource.GetResource(loginRecord.DiscordId));
        }
        else
        {
            var oldResource = _database.Resources.FirstOrDefault(t => t.Id == resource.Id);
            if (oldResource is null)
            {
                return NotFound();
            }
            _database.Resources.Update(resource.GetUpdate(oldResource));
        }
        _database.SaveChanges();
        return Ok();
    }

    [HttpDelete]
    [Route("{id}")]
    [ServiceFilter(typeof(AuthFilter))]
    public IActionResult DeleteResource(Guid id)
    {
        var resource = _database.Resources.FirstOrDefault(t => t.Id == id);
        if (resource is null)
        {
            return NotFound();
        }
        _database.Resources.Remove(resource);
        _database.SaveChanges();
        return Ok();
    }

    [HttpGet]
    [Route("{id}")]
    public IActionResult GetResource(Guid id)
    {
        var resource = _database.Resources.Include(resource => resource.Category).Include(resource => resource.Expansion).FirstOrDefault(t => t.Id == id);
        if (resource is null)
        {
            return NotFound();
        }
        return Ok(new { resource.Id, resource.Category, resource.Expansion, resource.PageName, resource.HtmlContent, resource.MarkdownContent });
    }

    [HttpGet]
    [Route("categories")]
    public IActionResult GetCategories()
    {
        return Ok(_database.Categories.Select(t => new { t.Id, t.Name, t.Description, t.IconUrl }));
    }

    [HttpGet]
    [Route("expansions")]
    public IActionResult GetExpansions()
    {
        return Ok(_database.Expansions.Select(t => new { t.Id, t.Name, t.Description, t.IconUrl }));
    }
}

public record ResourceHttp(Guid? Id, Guid? CategoryId, Guid? ExpansionId, string? HtmlContent, string? MarkdownContent, string? PageName, bool? Publish)
{
    public Resource GetResource(ulong hostId)
    {
        return new Resource(Id, CategoryId ?? Guid.Empty, ExpansionId ?? Guid.Empty, HtmlContent ?? "", MarkdownContent ?? "", PageName ?? "", hostId, Publish ?? false);
    }

    public Resource GetUpdate(Resource resource)
    {
        return resource with { CategoryId = CategoryId ?? resource.CategoryId, ExpansionId = ExpansionId ?? resource.ExpansionId, HtmlContent = HtmlContent ?? resource.HtmlContent, MarkdownContent = MarkdownContent ?? resource.MarkdownContent, PageName = PageName ?? resource.PageName };
    }
};