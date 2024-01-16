using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Logging;
using PDPWebsite.Patching;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("Patcher");

var patchInstaller = new PatchInstaller(loggerFactory.CreateLogger<PatchInstaller>());

var updateTask = patchInstaller.Update();
while (!updateTask.IsCompleted)
{
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    var sb = new StringBuilder();
    if (patchInstaller.DownloadProgress.Any())
    {
        sb.AppendLine("Download progress:");
        foreach (var (_, download) in patchInstaller.DownloadProgress.ToImmutableDictionary())
        {
            var (desc, ver, progress) = download;
            sb.AppendLine($"{desc} - {ver}\n{progress:P}");
        }
    }

    {
        if (sb.Length > 0) sb.AppendLine();
        sb.AppendLine("Install progress:");
        var (desc, ver, goalVer, progress, chunkProgress, fileProgress) = patchInstaller.CurrentInstallProgress;
        sb.AppendLine($"{desc} - {ver} {chunkProgress:P} {fileProgress:P} -> {goalVer}\n{progress:P}");
    }
    logger.LogInformation(sb.ToString());
}

if (updateTask.IsFaulted)
{
    updateTask.Exception?.Flatten().Handle(exception =>
    {
        logger.LogError(exception, "Update failed.");
        return true;
    });
    logger.LogError("Update failed.");
}
else
{
    logger.LogInformation("Update Complete");
}