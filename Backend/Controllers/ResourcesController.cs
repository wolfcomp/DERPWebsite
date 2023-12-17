using System.Diagnostics;

namespace PDPWebsite.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly Database _database;
    private readonly RedisClient _redisClient;
    private readonly EnvironmentContainer _environmentContainer;

    public ResourcesController(Database database, RedisClient redisClient, EnvironmentContainer environmentContainer)
    {
        _database = database;
        _redisClient = redisClient;
        _environmentContainer = environmentContainer;
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
        if (!resource.Id.HasValue || resource.Id == Guid.Empty)
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
    [Route("{id}/files")]
    public IActionResult GetResourceFiles(Guid id)
    {
        var resource = _database.Resources.Include(resource => resource.Files).FirstOrDefault(t => t.Id == id);
        if (resource is null)
        {
            return NotFound();
        }
        return Ok(resource.Files.Select(t => new { t.Id, t.Name, t.Path }));
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

    [HttpPost]
    [Route("upload")]
    public IActionResult UploadFile([FromForm] FileUpload upload)
    {
        var resource = _database.Resources.Include(t => t.Category).Include(t => t.Expansion).FirstOrDefault(t => t.Id == upload.Id);
        if (resource is null)
        {
            return NotFound("Could not find connected resource. Have you saved a draft yet?");
        }
        var file = upload.File;
        var path = Path.Combine(resource.Expansion.Path, resource.Category.Path, Path.GetFileNameWithoutExtension(file.FileName));
        if (!Directory.Exists(Path.Combine(_environmentContainer.Get("EDITOR_RESOURCE_PATH"), resource.Expansion.Path, resource.Category.Path)))
        {
            Directory.CreateDirectory(Path.Combine(_environmentContainer.Get("EDITOR_RESOURCE_PATH"), resource.Expansion.Path, resource.Category.Path));
        }
        var stream = new FileStream(Path.Combine(_environmentContainer.Get("EDITOR_RESOURCE_PATH"), path), FileMode.Create);
        file.CopyTo(stream);
        stream.Close();
        stream.Dispose();
        var resourceFile = new ResourceFile(null, upload.Id, file.FileName, ConvertFile(path, Path.GetExtension(file.FileName)[1..]).Replace(@"\", "/"));
        _database.ResourceFiles.Add(resourceFile);
        _database.SaveChanges();
        return Ok(new { resourceFile.Id, resourceFile.Name, Path = resourceFile.Path });
    }

    private string ConvertFile(string path, string ext)
    {
        var process = new Process();
        var ret = path;
        switch (ext)
        {
            case "png" or "jpg" or "jpeg":
                process.StartInfo.FileName = "convert";
                ret += ".webp";
                process.StartInfo.Arguments = $"{path}.{ext} {path}.webp";
                break;
            case "mp4":
                process.StartInfo.FileName = "ffprobe";
                process.StartInfo.Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 {path}";
                process.Start();
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                var size = int.Parse(output.Split("x")[1]);
                var crf = size switch
                {
                    >= 2160 => 15,
                    >= 1440 => 24,
                    >= 1080 => 30,
                    >= 720 => 32,
                    >= 480 => 33,
                    >= 360 => 36,
                    >= 240 => 37,
                    _ => 40
                };
                process.StartInfo.FileName = "ffmpeg";
                ret += ".webm";
                process.StartInfo.Arguments = $"-i {path}.{ext} -c:v libvpx-vp9 -crf {crf} -b:v 0 -lossless 1 -c:a copy {path}.webm";
                break;
        }
        process.Start();
        process.WaitForExit();
        return ret;
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

public record FileUpload(IFormFile File, Guid Id);