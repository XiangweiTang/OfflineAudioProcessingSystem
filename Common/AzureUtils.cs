using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Common
{
    public static class AzureUtils
    {
        public const string BLOB_CONTAINER_NAME= "/chdatacollections/";
        public static readonly string SAS_TOKEN = "";
        static AzureUtils()
        {
            SAS_TOKEN= File.ReadAllText(@"C:\Files\Azure\SASToken.txt").Trim();
        }
        public static string SetDataUri(string uriString)
        {
            if (uriString.StartsWith("https://"))
                return uriString;
            return PathCombine("https://marksystemapistorage.blob.core.windows.net/chdatacollections/", uriString);
        }
        public static List<string> ListDirectories(string uriString)
        {
            Uri tmpUri = new Uri(uriString);
            string prefix = tmpUri.LocalPath.Substring(BLOB_CONTAINER_NAME.Length);

            Uri uri = new Uri($"https://{tmpUri.Host}{BLOB_CONTAINER_NAME}");
            BlobContainerClient client = new BlobContainerClient(uri);

            var pages = client.GetBlobsByHierarchy(prefix: prefix, delimiter: "/").AsPages();
            List<string> list = new List<string>();
            foreach(Page<BlobHierarchyItem> page in pages)
            {
                list.AddRange(page.Values.Where(x => x.IsPrefix).Select(x => $"{uri.OriginalString}{x.Prefix}"));
            }
            return list;
        }

        public static List<string> ListBlobs(string uriString)
        {
            Uri tmpUri = new Uri(uriString);
            string prefix = tmpUri.LocalPath.Substring(BLOB_CONTAINER_NAME.Length);

            Uri uri = new Uri($"https://{tmpUri.Host}{BLOB_CONTAINER_NAME}");
            BlobContainerClient client = new BlobContainerClient(uri);
            var pages = client.GetBlobs(prefix: prefix).AsPages();

            List<string> list = new List<string>();
            foreach(Page<BlobItem> page in pages)
            {
                list.AddRange(page.Values.Select(x => $"{uri.OriginalString}{x.Name}"));                
            }
            return list;
        }

        public static Stream ReadBlobToString(string uriString)
        {
            Uri uri = new Uri(uriString);
            BlobClient client = new BlobClient(uri);
            return client.OpenRead(new BlobOpenReadOptions(false));
        }

        public static void Download(string uriString, string localPath)
        {
            Uri uri = new Uri(uriString);
            BlobClient client = new BlobClient(uri);
            client.DownloadTo(localPath);
        }

        public static void Upload(string localPath, string uriString)
        {
            Uri uri = new Uri(uriString + SAS_TOKEN);
            BlobClient client = new BlobClient(uri);
            client.Upload(localPath);
        }

        public static string PathCombine(params string[] paths)
        {
            return paths.Aggregate((x, y) => $"{x.TrimEnd('/')}/{y.TrimStart('/')}");
        }
    }
}
