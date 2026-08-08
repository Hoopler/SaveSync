using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using File = Google.Apis.Drive.v3.Data.File;

public static class DriveSync
{
    private static DriveService? _service;

    private static async Task<DriveService> GetServiceAsync()
    {
        if (_service != null)
            return _service;

        string[] scopes = { DriveService.Scope.DriveFile };
        UserCredential credential;
        string credentialsPath = Path.Combine(AppContext.BaseDirectory, "credentials.json");
        await using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                (await GoogleClientSecrets.FromStreamAsync(stream)).Secrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore("token_store", true));
        }

        _service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "SaveSync"
        });

        return _service;
    }

    public static async Task UploadSave(string gameName)
{
    Game.ReadGames();
    var game = Game.Games.Find(g => g.Name == gameName);
    if (game == null)
    {
        Console.WriteLine($"Game '{gameName}' not found. Use 'add' first.");
        return;
    }

    if (!Directory.Exists(game.SaveLocation))
    {
        Console.WriteLine($"Save folder not found: {game.SaveLocation}");
        return;
    }

    var service = await GetServiceAsync();
    string rootId = await GetOrCreateFolderAsync(service, "SaveSync", null);
    string gameFolderId = await GetOrCreateFolderAsync(service, game.Name, rootId);

    int total = Directory.GetFiles(game.SaveLocation, "*", SearchOption.AllDirectories).Length;
    int current = 0;

    await UploadDirectoryRecursive(service, game.SaveLocation, gameFolderId, () =>
    {
        current++;
        PrintProgress(current, total);
    });

    Console.WriteLine();
    Console.WriteLine("Upload complete.");
}

private static async Task UploadDirectoryRecursive(DriveService service, string localDir, string parentFolderId, Action onFileDone)
{
    foreach (string filePath in Directory.GetFiles(localDir))
    {
        var fileMetadata = new File
        {
            Name = Path.GetFileName(filePath),
            Parents = new[] { parentFolderId }
        };

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var request = service.Files.Create(fileMetadata, fileStream, "application/octet-stream");
        request.Fields = "id, name";
        var progress = await request.UploadAsync();

        if (progress.Status != Google.Apis.Upload.UploadStatus.Completed)
            Console.WriteLine($"Failed to upload {fileMetadata.Name}: {progress.Exception?.Message}");

        onFileDone();
    }

    foreach (string subDir in Directory.GetDirectories(localDir))
    {
        string subFolderId = await GetOrCreateFolderAsync(service, Path.GetFileName(subDir), parentFolderId);
        await UploadDirectoryRecursive(service, subDir, subFolderId, onFileDone);
    }
}

public static async Task DownloadSave(string gameName)
{
    Game.ReadGames();
    var game = Game.Games.Find(g => g.Name == gameName);
    if (game == null)
    {
        Console.WriteLine($"Game '{gameName}' not found. Use 'add' first.");
        return;
    }

    var service = await GetServiceAsync();

    string? rootId = await FindFolderAsync(service, "SaveSync", null);
    if (rootId == null)
    {
        Console.WriteLine("No SaveSync folder found on Drive.");
        return;
    }

    string? gameFolderId = await FindFolderAsync(service, game.Name, rootId);
    if (gameFolderId == null)
    {
        Console.WriteLine($"No cloud save found for '{gameName}'.");
        return;
    }

    string backupPath = game.SaveLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".bak";
    if (Directory.Exists(backupPath))
        Directory.Delete(backupPath, true);

    if (Directory.Exists(game.SaveLocation))
    {
        CopyDirectory(game.SaveLocation, backupPath);
        Directory.Delete(game.SaveLocation, true);
        Console.WriteLine($"Backed up existing save to {backupPath}");
    }

    Directory.CreateDirectory(game.SaveLocation);

    int total = await CountFilesRecursive(service, gameFolderId);
    int current = 0;

    await DownloadDirectoryRecursive(service, gameFolderId, game.SaveLocation, () =>
    {
        current++;
        PrintProgress(current, total);
    });

    Console.WriteLine();
    Console.WriteLine("Download complete.");
}

private static async Task<int> CountFilesRecursive(DriveService service, string folderId)
{
    var listRequest = service.Files.List();
    listRequest.Q = $"'{folderId}' in parents and trashed=false";
    listRequest.Fields = "files(id, mimeType)";
    var result = await listRequest.ExecuteAsync();

    int count = 0;
    foreach (var item in result.Files)
    {
        if (item.MimeType == "application/vnd.google-apps.folder")
            count += await CountFilesRecursive(service, item.Id);
        else
            count++;
    }
    return count;
}

private static async Task DownloadDirectoryRecursive(DriveService service, string folderId, string localDir, Action onFileDone)
{
    var listRequest = service.Files.List();
    listRequest.Q = $"'{folderId}' in parents and trashed=false";
    listRequest.Fields = "files(id, name, mimeType)";
    var result = await listRequest.ExecuteAsync();

    foreach (var item in result.Files)
    {
        if (item.MimeType == "application/vnd.google-apps.folder")
        {
            string subLocalDir = Path.Combine(localDir, item.Name);
            Directory.CreateDirectory(subLocalDir);
            await DownloadDirectoryRecursive(service, item.Id, subLocalDir, onFileDone);
        }
        else
        {
            string destPath = Path.Combine(localDir, item.Name);
            await using var outStream = new FileStream(destPath, FileMode.Create);
            await service.Files.Get(item.Id).DownloadAsync(outStream);
            onFileDone();
        }
    }
}

private static void PrintProgress(int current, int total)
{
    int barWidth = 30;
    double pct = total == 0 ? 1 : (double)current / total;
    int filled = (int)(barWidth * pct);

    string bar = "[" + new string('#', filled) + new string('-', barWidth - filled) + "]";
    Console.Write($"\r{bar} {current}/{total} ({pct:P0})");
}


    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (string file in Directory.GetFiles(sourceDir))
            System.IO.File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
        foreach (string dir in Directory.GetDirectories(sourceDir))
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
    }

    private static async Task<string?> FindFolderAsync(DriveService service, string folderName, string? parentId)
    {
        var listRequest = service.Files.List();
        listRequest.Q = parentId == null
            ? $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and 'root' in parents and trashed=false"
            : $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and '{parentId}' in parents and trashed=false";
        listRequest.Fields = "files(id, name)";
        listRequest.Spaces = "drive";

        var result = await listRequest.ExecuteAsync();
        return result.Files.Count > 0 ? result.Files[0].Id : null;
    }

    private static async Task<string> GetOrCreateFolderAsync(DriveService service, string folderName, string? parentId)
    {
        string? existingId = await FindFolderAsync(service, folderName, parentId);
        if (existingId != null)
            return existingId;

        var folderMetadata = new File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = parentId != null ? new[] { parentId } : null
        };

        var createRequest = service.Files.Create(folderMetadata);
        createRequest.Fields = "id";
        var folder = await createRequest.ExecuteAsync();
        return folder.Id;
    }
}