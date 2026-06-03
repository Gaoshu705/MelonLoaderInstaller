using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MelonLoader.Installer.Core.PatchSteps;

internal class DownloadUnityDeps : IPatchStep
{
    private const string LIBUNITY_URL_TEMPLATE = "https://proxy.ehre.top/proxy/github.com/LavaGang/MelonLoader.UnityDependencies/releases/download/{0}/libunity.so.{1}";
    private const string CHINA_LIBUNITY_URL_TEMPLATE = "https://proxy.ehre.top/proxy/github.com/Gaoshu705/MelonLoader.UnityDependencies.China/releases/download/{0}/libunity.so.{1}";

    public bool Run(Patcher patcher)
    {
        if (File.Exists(patcher.Args.UnityDependenciesPath))
        {
            patcher.Logger.Log("Using local Unity dependencies!");
            return true;
        }

        patcher.Logger.Log("Downloading Unity dependencies");

        using HttpClient client = new();

        if (!patcher.Args.UnityVersion.HasValue)
        {
            patcher.Logger.Log("Got to DownloadUnityDeps while UnityVersion is null, how did this happen?");
            return false;
        }   
        
        string unityVersion = patcher.Args.UnityVersion.Value.ToStringWithoutType();

        try
        {
            var originalValidator = ServicePointManager.ServerCertificateValidationCallback;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

            string url = patcher.Args.UnityVersion.Value.Type == AssetRipper.Primitives.UnityVersionType.China ? CHINA_LIBUNITY_URL_TEMPLATE : LIBUNITY_URL_TEMPLATE;
            string formattedUrl = string.Format(url, unityVersion, "arm64-v8a");

            using var response = client.GetAsync(formattedUrl, HttpCompletionOption.ResponseHeadersRead).Result;
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            string libDir = Path.Combine(patcher.Info.UnityNativeDirectory, "arm64-v8a");
            if (!Directory.Exists(libDir))
                Directory.CreateDirectory(libDir);

            using var contentStream = response.Content.ReadAsStreamAsync().Result;
            using var fileStream = File.Create(Path.Combine(libDir, "libunity.so"));

            byte[] buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = contentStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
                totalRead += bytesRead;
                patcher.Logger.LogProgress("Downloading Unity dependencies", totalRead, totalBytes);
            }

            patcher.Logger.LogProgressComplete();

            ServicePointManager.ServerCertificateValidationCallback = originalValidator;
        }
        catch (Exception ex)
        {
            patcher.Logger.Log("Failed to download Unity dependencies. You may need to generate them manually.\n" + ex.ToString());
            return false;
        }

        return true;
    }
}
