using System.Diagnostics;
using Swashbuckle.AspNetCore.Annotations;

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
    [SwaggerOperation("Gets all resources")]
    [SwaggerResponse(StatusCodes.Status200OK, "All resources", typeof(IEnumerable<Resource>))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error", typeof(string))]
    public IActionResult GetResources()
    {
        return Ok(_database.Resources.Include(resource => resource.Category).Include(resource => resource.Tier).Select(resource => new
        {
            resource.Id,
            Category = new { resource.Category.Id, resource.Category.Name, resource.Category.Description, resource.Category.IconUrl, resource.Category.HasTiers },
            Tier = new { resource.Tier.Id, resource.Tier.Name, resource.Tier.IconUrl },
            resource.PageName,
            resource.Published
        }));
    }

    [HttpPost]
    [Route("")]
    [ServiceFilter(typeof(AuthFilter))]
    [SwaggerOperation("Creates a resource")]
    [SwaggerResponse(StatusCodes.Status200OK, "The created resource", typeof(Resource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body", typeof(string))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid or missing authorization token", typeof(string))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error", typeof(string))]
    public IActionResult PutResource([FromBody] ResourceHttp resourceHttp)
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Split(" ").Last();
        var loginRecord = _redisClient.GetObj<Login>(token)!;
        Resource? resource;
        if (!resourceHttp.Id.HasValue || resourceHttp.Id == Guid.Empty)
        {
            resource = resourceHttp.GetResource(loginRecord.DiscordId);
            _database.Resources.Add(resource);
        }
        else
        {
            resource = _database.Resources.Include(resource1 => resource1.Category).Include(resource1 => resource1.Tier).FirstOrDefault(t => t.Id == resourceHttp.Id);
            if (resource is null)
            {
                return Problem();
            }
            _database.Database.ExecuteSqlRaw(@"UPDATE ""Resources"" SET ""ExpansionId"" = {0}, ""CategoryId"" = {1}, ""HtmlContent"" = {2}, ""MarkdownContent"" = {3}, ""PageName"" = {4}, ""Published"" = {5} WHERE ""Id"" = {6}", resourceHttp.TierId ?? resource.TierId, resourceHttp.CategoryId ?? resource.CategoryId, resourceHttp.HtmlContent ?? resource.HtmlContent, resourceHttp.MarkdownContent ?? resource.MarkdownContent, resourceHttp.PageName ?? resource.PageName, resourceHttp.Publish ?? resource.Published, resourceHttp.Id);
        }
        _database.ChangeTracker.Clear();
        _database.SaveChanges();
        resource = _database.Resources.Include(resource1 => resource1.Category).Include(resource1 => resource1.Tier).First(t => t.Id == resource.Id);
        return Ok(new
        {
            resource.Id,
            Category = new { resource.Category.Id, resource.Category.Name, resource.Category.Description, resource.Category.IconUrl, resource.Category.HasTiers },
            Tier = new { resource.Tier.Id, resource.Tier.Name, resource.Tier.IconUrl },
            resource.PageName,
            resource.Published,
            resource.HtmlContent,
            resource.MarkdownContent
        });
    }

    [HttpDelete]
    [Route("{id}")]
    [ServiceFilter(typeof(AuthFilter))]
    [SwaggerOperation("Deletes a resource")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Returns if resource id isn't in database")]
    [SwaggerResponse(StatusCodes.Status200OK, "Returns if successful")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error", typeof(string))]
    public IActionResult DeleteResource(Guid id)
    {
        var resource = _database.Resources.Include(t => t.Files).FirstOrDefault(t => t.Id == id);
        if (resource is null)
        {
            return NotFound();
        }
        resource.Files.ForEach(DeleteFile);
        _database.Resources.Remove(resource);
        _database.SaveChanges();
        return Ok();
    }

    [HttpGet]
    [Route("{id}")]
    [SwaggerOperation("Gets a resource")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Returns if resource id isn't in database")]
    [SwaggerResponse(StatusCodes.Status200OK, "Returns if successful", typeof(Resource))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error", typeof(string))]
    public IActionResult GetResource(Guid id)
    {
        var resource = _database.Resources.Include(resource => resource.Category).Include(resource => resource.Tier).FirstOrDefault(t => t.Id == id);
        if (resource is null)
        {
            return NotFound();
        }
        return Ok(new
        {
            resource.Id,
            Category = new { resource.Category.Id, resource.Category.Name, resource.Category.Description, resource.Category.IconUrl, resource.Category.HasTiers },
            Tier = new { resource.Tier.Id, resource.Tier.Name, resource.Tier.IconUrl },
            resource.PageName,
            resource.HtmlContent,
            resource.MarkdownContent
        });
    }

    [HttpGet]
    [Route("{id}/files")]
    [SwaggerOperation("Gets all files for a resource")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Returns if resource id isn't in database")]
    [SwaggerResponse(StatusCodes.Status200OK, "Returns if successful", typeof(IEnumerable<FileReturn>))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error", typeof(string))]
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
    [SwaggerOperation("Gets all categories")]
    [SwaggerResponse(StatusCodes.Status200OK, "Returns if successful", typeof(IEnumerable<Category>))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error", typeof(string))]
    public IActionResult GetCategories()
    {
        return Ok(_database.Categories.Select(t => new { t.Id, t.Name, t.Description, t.IconUrl, t.HasTiers }));
    }

    [HttpGet]
    [Route("tiers")]
    [SwaggerOperation("Gets all tiers")]
    [SwaggerResponse(StatusCodes.Status200OK, "Returns if successful", typeof(IEnumerable<Tier>))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error", typeof(string))]
    public IActionResult GetTiers()
    {
        return Ok(_database.Tiers.Select(t => new { t.Id, t.Name, t.IconUrl }));
    }

    [HttpPost]
    [Route("upload")]
    [ServiceFilter(typeof(AuthFilter))]
    [SwaggerOperation("Uploads a file for a resource post")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body", typeof(string))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid or missing authorization token", typeof(string))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Returns if resource id isn't in database")]
    [SwaggerResponse(StatusCodes.Status200OK, "Returns if successful", typeof(FileReturn))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error", typeof(string))]
    public IActionResult UploadFile([FromForm] FileUpload upload)
    {
        var fileId = Guid.NewGuid().ToString();
        var resource = _database.Resources.Include(t => t.Category).Include(t => t.Tier).FirstOrDefault(t => t.Id == upload.Id);
        if (resource is null)
        {
            return NotFound("Could not find connected resource. Have you saved a draft yet?");
        }
        var file = upload.File;
        var path = Path.Combine(resource.Tier.Path, resource.Category.Path, fileId);
        if (!Directory.Exists(Path.Combine(_environmentContainer.Get("EDITOR_RESOURCE_PATH"), resource.Tier.Path, resource.Category.Path)))
        {
            Directory.CreateDirectory(Path.Combine(_environmentContainer.Get("EDITOR_RESOURCE_PATH"), resource.Tier.Path, resource.Category.Path));
        }
        var stream = new FileStream(Path.Combine(_environmentContainer.Get("EDITOR_RESOURCE_PATH"), path), FileMode.Create);
        file.CopyTo(stream);
        stream.Close();
        stream.Dispose();
        var resourceFile = new ResourceFile(null, upload.Id, file.FileName, ConvertFile(path, Path.GetExtension(file.FileName)[1..]).Replace(@"\", "/"));
        _database.ResourceFiles.Add(resourceFile);
        _database.SaveChanges();
        return Ok(new { resourceFile.Id, resourceFile.Name, resourceFile.Path });
    }

    [HttpDelete]
    [Route("files/{id}")]
    [ServiceFilter(typeof(AuthFilter))]
    [SwaggerOperation("Deletes a file for a resource post")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid or missing authorization token", typeof(string))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Returns if resource id isn't in database")]
    [SwaggerResponse(StatusCodes.Status200OK, "Returns if successful", typeof(FileReturn))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error", typeof(string))]
    public IActionResult DeleteFile(Guid id)
    {
        var resourceFile = _database.ResourceFiles.FirstOrDefault(t => t.Id == id);
        if (resourceFile is null)
        {
            return NotFound("Could not find connected resource. Have you saved a draft yet?");
        }

        DeleteFile(resourceFile);
        _database.ResourceFiles.Remove(resourceFile);
        _database.SaveChanges();
        return Ok();
    }

    private void DeleteFile(ResourceFile file)
    {
        var path = Path.Combine(_environmentContainer.Get("EDITOR_RESOURCE_PATH"), file.Path);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }

    private string ConvertFile(string path, string ext)
    {
        var process = new Process();
        var ret = path;
        path = Path.Combine(_environmentContainer.Get("EDITOR_RESOURCE_PATH"), path);
        switch (ext)
        {
            case "png" or "jpg" or "jpeg":
                process.StartInfo.FileName = "convert";
                ret += ".webp";
                process.StartInfo.Arguments = $"{path} {path}.webp";
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
                process.StartInfo.Arguments = $"-i {path} -c:v libvpx-vp9 -crf {crf} -b:v 0 -lossless 1 -c:a copy {path}.webm";
                break;
            default:
                ret += "." + ext;
                System.IO.File.Move(path, path + "." + ext);
                return ret;
        }
        process.Start();
        process.WaitForExit();
        System.IO.File.Delete(path);
        return ret;
    }
}

public record ResourceHttp(Guid? Id, Guid? CategoryId, Guid? TierId, string? HtmlContent, string? MarkdownContent, string? PageName, bool? Publish)
{
    public Resource GetResource(ulong hostId)
    {
        return new Resource(Id, CategoryId ?? Guid.Empty, TierId ?? Guid.Empty, HtmlContent ?? "", MarkdownContent ?? "", PageName ?? "", hostId, Publish ?? false);
    }

    public Resource GetUpdate(Resource resource)
    {
        return resource with { CategoryId = CategoryId ?? resource.CategoryId, TierId = TierId ?? resource.TierId, HtmlContent = HtmlContent ?? resource.HtmlContent, MarkdownContent = MarkdownContent ?? resource.MarkdownContent, PageName = PageName ?? resource.PageName };
    }
};

public record FileUpload(IFormFile File, Guid Id);

public record FileReturn(Guid Id, string Name, string Path);