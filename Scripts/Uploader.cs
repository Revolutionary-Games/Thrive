namespace Scripts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevCenterCommunication;
using DevCenterCommunication.Models;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

/// <summary>
///   Manages uploading dehydrated devbuilds and the object cache entries missing from the server
/// </summary>
public class Uploader
{
    public const string DEFAULT_DEVCENTER_URL = "https://dev.revolutionarygamesstudio.com";
    public const int DEFAULT_PARALLEL_UPLOADS = 5;

    private readonly Program.UploadOptions options;
    private readonly string dehydratedFolder;
    private readonly string cacheFolder;
    private readonly string? accessKey;

    private readonly Uri url;

    private readonly object outputLock = new();

    /// <summary>
    ///   Contains sha3 identifiers
    /// </summary>
    private readonly HashSet<string> dehydratedToUpload = new();

    private readonly List<DehydratedInfo> devbuildsToUpload = new();

    private readonly List<(string Archive, string Meta)> alreadyUploadedToDelete = new();

    public Uploader(Program.UploadOptions options, string dehydratedFolder = Dehydration.DEVBUILDS_FOLDER,
        string cacheFolder = Dehydration.DEHYDRATE_CACHE)
    {
        this.options = options;
        this.dehydratedFolder = dehydratedFolder;
        this.cacheFolder = cacheFolder;

        accessKey = string.IsNullOrWhiteSpace(options.Key) ?
            Environment.GetEnvironmentVariable("THRIVE_DEVCENTER_ACCESS_KEY") :
            options.Key;

        if (string.IsNullOrEmpty(accessKey))
        {
            accessKey = null;
            ColourConsole.WriteInfoLine("Uploading anonymous devbuilds");
        }

        url = new Uri(options.Url);
    }

    public async Task<bool> Run(CancellationToken cancellationToken)
    {
        if (options.ParallelUploads < 1)
        {
            ColourConsole.WriteErrorLine("Parallel uploads needs to be at least 1");
            return false;
        }

        if (options.Retries < 0)
        {
            ColourConsole.WriteErrorLine("Retries needs to be a non-negative number");
            return false;
        }

        ColourConsole.WriteInfoLine("Starting devbuild upload");

        if (!await FindBuildsToUpload(cancellationToken))
            return false;

        if (devbuildsToUpload.Count < 1 && dehydratedToUpload.Count < 1)
        {
            ColourConsole.WriteWarningLine("Nothing to upload");
            DeleteAlreadyExisting();
            return true;
        }

        ColourConsole.WriteInfoLine($"Beginning upload of {"devbuild".PrintCount(devbuildsToUpload.Count)} with "
            + $"{"dehydrated object".PrintCount(dehydratedToUpload.Count)}");

        await PerformUploads(cancellationToken);

        ColourConsole.WriteSuccessLine("DevBuild upload finished.");

        DeleteAlreadyExisting();

        return true;
    }

    private async Task<bool> FindBuildsToUpload(CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();

        foreach (var metaFile in Directory.EnumerateFiles(dehydratedFolder, $"*{Dehydration.DEHYDRATED_META_SUFFIX}",
                     SearchOption.TopDirectoryOnly))
        {
            await CheckBuildForUpload(metaFile, client, cancellationToken);
        }

        if (devbuildsToUpload.GroupBy(d => (d.Platform, d.Version)).Any(g => g.Count() > 1))
        {
            ColourConsole.WriteErrorLine("Duplicate devbuilds to upload have been detected");
            return false;
        }

        return true;
    }

    private async Task CheckBuildForUpload(string metaFile, HttpClient client, CancellationToken cancellationToken)
    {
        await using var reader = File.OpenRead(metaFile);
        var meta = await JsonSerializer.DeserializeAsync<DehydratedInfo>(reader,
            new JsonSerializerOptions(), cancellationToken) ?? throw new NullDecodedJsonException();
        meta.MetaFile = metaFile;

        if (!File.Exists(meta.ArchiveFile))
        {
            ColourConsole.WriteWarningLine($"Archive file doesn't exist ({meta.ArchiveFile}), deleting meta as well");
            File.Delete(metaFile);
            return;
        }

        ColourConsole.WriteNormalLine($"Found devbuild: {meta.Version}, {meta.Platform}, {meta.Branch}");

        var data = await PerformWithRetry(async cancellation =>
        {
            var response = await client.PostAsJsonAsync("api/v1/devbuild/offer_devbuild",
                new DevBuildOfferRequest
                {
                    BuildHash = meta.Version,
                    BuildPlatform = meta.Platform,
                }, cancellation);

            return await GetJsonFromResponse<DevBuildOfferResult>(response, cancellation);
        }, cancellationToken);

        if (!data.Upload)
        {
            alreadyUploadedToDelete.Add((meta.ArchiveFile, metaFile));
            return;
        }

        ColourConsole.WriteNormalLine("Server doesn't have it");
        devbuildsToUpload.Add(meta);

        // Determine related objects to upload
        foreach (var chunk in meta.DehydratedObjects.Chunk(CommunicationConstants.MAX_DEHYDRATED_OBJECTS_PER_OFFER))
        {
            var dehydratedData = await PerformWithRetry(async cancellation =>
            {
                var response = await client.PostAsJsonAsync("api/v1/devbuild/offer_objects",
                    new ObjectOfferRequest
                    {
                        Objects = chunk.Select(i =>
                            new DehydratedObjectRequest(i, (int)new FileInfo(Sha3ToPath(i)).Length)).ToList(),
                    }, cancellation);

                return await GetJsonFromResponse<DevObjectOfferResult>(response, cancellation);
            }, cancellationToken);

            foreach (var toUpload in dehydratedData.Upload)
            {
                dehydratedToUpload.Add(toUpload);
            }
        }
    }

    private void DeleteAlreadyExisting()
    {
        foreach (var (archive, meta) in alreadyUploadedToDelete)
        {
            ColourConsole.WriteWarningLine($"Deleting build server didn't want: {meta}");

            try
            {
                if (!string.IsNullOrEmpty(meta) && File.Exists(meta))
                    File.Delete(meta);

                if (!string.IsNullOrEmpty(archive) && File.Exists(archive))
                    File.Delete(archive);
            }
            catch (Exception e)
            {
                ColourConsole.WriteErrorLine($"Failed to delete {meta} or {archive}: {e}");
            }
        }
    }

    private async Task PerformUploads(CancellationToken cancellationToken)
    {
        ColourConsole.WriteInfoLine("Fetching tokens for dehydrated objects");

        // TODO: if there is ton to upload, it might not be a good idea to fetch all of the tokens at once
        var thingsToUpload = await FetchObjectUploadTokens(cancellationToken);

        ColourConsole.WriteInfoLine("Uploading dehydrated objects");
        await UploadThingsInChunks(thingsToUpload, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        ColourConsole.WriteInfoLine("Fetching tokens for devbuilds");
        thingsToUpload = await FetchDevBuildUploadTokens(cancellationToken);

        ColourConsole.WriteInfoLine("Uploading devbuilds");
        await UploadThingsInChunks(thingsToUpload, cancellationToken);

        ColourConsole.WriteSuccessLine("Done uploading");
    }

    private async Task<List<ThingToUpload>> FetchObjectUploadTokens(CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();

        var result = new List<ThingToUpload>();

        foreach (var chunk in dehydratedToUpload.Chunk(CommunicationConstants.MAX_DEHYDRATED_OBJECTS_PER_OFFER))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = await PerformWithRetry(async cancellation =>
            {
                var response = await client.PostAsJsonAsync("api/v1/devbuild/upload_objects",
                    new DehydratedUploadRequest
                    {
                        Objects = chunk.Select(d =>
                            new DehydratedObjectRequest(d, (int)new FileInfo(Sha3ToPath(d)).Length)).ToList(),
                    }, cancellation);

                return await GetJsonFromResponse<DehydratedUploadResult>(response, cancellation);
            }, cancellationToken);

            foreach (var toUpload in data.Upload)
            {
                result.Add(
                    new ThingToUpload(Sha3ToPath(toUpload.Sha3), toUpload.UploadUrl, toUpload.VerifyToken, false));
            }
        }

        return result;
    }

    private async Task<List<ThingToUpload>> FetchDevBuildUploadTokens(CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();

        var result = new List<ThingToUpload>();

        foreach (var devbuildToUpload in devbuildsToUpload)
        {
            if (devbuildToUpload.MetaFile == null)
                throw new Exception("Meta file path not set in devbuild object");

            cancellationToken.ThrowIfCancellationRequested();

            DevBuildUploadResult data;
            try
            {
                data = await PerformWithRetry(async cancellation =>
                {
                    var response = await client.PostAsJsonAsync("api/v1/devbuild/upload_devbuild",
                        new DevBuildUploadRequest
                        {
                            BuildHash = devbuildToUpload.Version,
                            BuildBranch = devbuildToUpload.Branch,
                            BuildPlatform = devbuildToUpload.Platform,
                            BuildSize = (int)new FileInfo(devbuildToUpload.ArchiveFile).Length,
                            BuildZipHash = devbuildToUpload.BuildZipHash,
                            RequiredDehydratedObjects = devbuildToUpload.DehydratedObjects.ToList(),
                        }, cancellation);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        // Give a different exception if we got an error message indicating that
                        var content = await response.Content.ReadAsStringAsync(cancellation);

                        ColourConsole.WriteNormalLine($"Unauthorized response, server said: {content}");

                        if (content.Contains(CommunicationStrings.ERROR_UPLOADING_DEVBUILD_ANONYMOUSLY_OVER_EXISTING))
                            throw new DevBuildOverwriteFailedException();

                        response.EnsureSuccessStatusCode();
                    }

                    return await GetJsonFromResponse<DevBuildUploadResult>(response, cancellation);
                }, cancellationToken);
            }
            catch (DevBuildOverwriteFailedException)
            {
                ColourConsole.WriteWarningLine(
                    "Deleting a build that was attempted to be uploaded over a build we can't " +
                    $"overwrite: {devbuildToUpload.MetaFile}");

                File.Delete(devbuildToUpload.MetaFile);
                File.Delete(devbuildToUpload.ArchiveFile);

                continue;
            }

            result.Add(new ThingToUpload(devbuildToUpload.ArchiveFile, data.UploadUrl, data.VerifyToken,
                options.DeleteAfterUpload == true)
            {
                ExtraDelete = devbuildToUpload.MetaFile,
            });
        }

        return result;
    }

    private async Task UploadThingsInChunks(IReadOnlyCollection<ThingToUpload> thingsToUpload,
        CancellationToken cancellationToken)
    {
        if (thingsToUpload.Count < 1)
            return;

        var tasks = new List<Task>();

        foreach (var toUpload in thingsToUpload.Chunk(
                     (int)Math.Ceiling(thingsToUpload.Count / (float)options.ParallelUploads)))
        {
            tasks.Add(Upload(toUpload, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task Upload(IEnumerable<ThingToUpload> things, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();

        // Separate client to not send our headers there
        using var uploadClient = new HttpClient();

        foreach (var upload in things)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileSize = Math.Round(new FileInfo(upload.File).Length / (float)GlobalConstants.MEBIBYTE, 2);

            lock (outputLock)
            {
                ColourConsole.WriteNormalLine($"Uploading file {upload.File} with size of {fileSize} MiB");
            }

            await PutFile(upload.File, upload.UploadUrl, uploadClient, cancellationToken);

            // Tell the server about upload success
            // We don't want to cancel this, so we'll at least run this if we are canceled (and delete it), before
            // exiting
            await PerformWithRetry(async cancellation =>
            {
                var response = await client.PostAsJsonAsync("api/v1/devbuild/finish", new TokenForm
                {
                    Token = upload.VerifyToken,
                }, cancellation);

                response.EnsureSuccessStatusCode();

                return true;
            }, CancellationToken.None);

            lock (outputLock)
            {
                ColourConsole.WriteDebugLine($"Finished uploading {upload.File}");
            }

            if (upload.DeleteAfterUpload)
            {
                lock (outputLock)
                {
                    ColourConsole.WriteNormalLine($"Deleting successfully uploaded file: {upload.File}");
                }

                File.Delete(upload.File);

                if (!string.IsNullOrEmpty(upload.ExtraDelete))
                    File.Delete(upload.ExtraDelete);
            }
        }
    }

    private async Task PutFile(string file, string fullUrl, HttpClient client, CancellationToken cancellationToken)
    {
        await PerformWithRetry(async cancellation =>
        {
            await using var reader = File.OpenRead(file);

            var content = new StreamContent(reader);

            // Needed for the launcher to work
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            var response = await client.PutAsync(fullUrl, content, cancellation);

            response.EnsureSuccessStatusCode();

            return true;
        }, cancellationToken);
    }

    private async Task<T> PerformWithRetry<T>(Func<CancellationToken, Task<T>> func,
        CancellationToken cancellationToken)
    {
        int timeToWait = 20;

        int attempts = options.Retries + 1;

        while (attempts > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            --attempts;
            var task = func(cancellationToken);

            try
            {
                var result = await task;
                return result;
            }
            catch (DevBuildOverwriteFailedException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                lock (outputLock)
                {
                    ColourConsole.WriteErrorLine($"Web request failed: {e}");
                }

                if (e is HttpRequestException httpRequestException)
                {
                    // This problem doesn't get better, so we just ignore retrying
                    if (httpRequestException.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        break;
                    }

                    // Remote side might be rebooting so we wait here a bit (or we are doing things too fast)
                    if (httpRequestException.StatusCode is HttpStatusCode.InternalServerError
                        or HttpStatusCode.GatewayTimeout or HttpStatusCode.BadGateway
                        or HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests)
                    {
                        if (attempts > 0)
                        {
                            lock (outputLock)
                            {
                                ColourConsole.WriteNormalLine("Sleeping and retrying the request");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(timeToWait), cancellationToken);
                            timeToWait *= 2;
                        }
                    }
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        throw new Exception("HTTP request ran out of retries");
    }

    private async Task<T> GetJsonFromResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        return await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken) ??
            throw new NullDecodedJsonException();
    }

    private string Sha3ToPath(string sha3)
    {
        return Path.Join(cacheFolder, $"{sha3}.gz");
    }

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = url,
            Timeout = TimeSpan.FromMinutes(1),
        };

        if (accessKey != null)
            client.DefaultRequestHeaders.Add("X-Access-Code", accessKey);

        return client;
    }

    private class ThingToUpload
    {
        public ThingToUpload(string file, string uploadUrl, string verifyToken, bool deleteAfterUpload)
        {
            File = file;
            UploadUrl = uploadUrl;
            VerifyToken = verifyToken;
            DeleteAfterUpload = deleteAfterUpload;
        }

        public string File { get; }
        public string UploadUrl { get; }
        public string VerifyToken { get; }

        public bool DeleteAfterUpload { get; }
        public string? ExtraDelete { get; init; }
    }

    private class DevBuildOverwriteFailedException : Exception;
}
