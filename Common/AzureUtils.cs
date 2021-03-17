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

        private static Dictionary<string, string> SasTokenDict = new Dictionary<string, string>();        
        static AzureUtils()
        {
            SasTokenDict = File.ReadLines(@"C:\Files\Azure\SASToken.txt")
                .ToDictionary(x => x.Split('\t')[0], x => x.Split('\t')[1]);
        }
        public static void Test()
        {
            string path = "https://marksystemapistorage.blob.core.windows.net/dechcollectionsachived/300hrsRecordingContent/St.%20Gallen/ID_01_Toman/";
            var r = ListCurrentBlobs(path);
        }
        public static string SetSasToken(string fullUriString)
        {
            Uri uri = new Uri(fullUriString);
            string blobContainer = uri.LocalPath.GetFirstNPart('/', 0);
            string sasToken = SasTokenDict[blobContainer];
            return $"{fullUriString}{sasToken}";
        }
        public static string GetFullUriString(string uriString, string blobContainerName="")
        {
            if (IsValidFullUri(uriString))
                return uriString;
            return PathCombine($"{Constants.AZURE_ROOT_PATH}{uriString}", uriString);
        }        

        public static List<string> ListCurrentBlobs(string blobString)
        {
            return ListCurrentItems(blobString, true);
        }

        public static List<string> ListCurrentDirectories(string blobString)
        {
            return ListCurrentItems(blobString, false);
        }

        public static List<string> ListCurrentItems(string blobString, bool listFile)
        {
            Uri uri = new Uri(blobString);
            string rootUriString = $"https://{uri.Host}{uri.Segments[0]}{uri.Segments[1]}";
            string prefix = string.Join("", uri.Segments.Skip(2));
            Uri rootUri = new Uri(rootUriString);
            BlobContainerClient client = new BlobContainerClient(rootUri);
            var pages = client.GetBlobsByHierarchy(prefix: prefix, delimiter: "/").AsPages();
            List<string> list = new List<string>();
            foreach (Page<BlobHierarchyItem> page in pages)
            {
                var r = page.Values.ToArray();
                var currentContent = listFile
                    ? page.Values.Where(x => !x.IsPrefix).Select(x => $"{uri.OriginalString}{x.Blob.Name}")
                    : page.Values.Where(x => x.IsPrefix).Select(x => $"{uri.OriginalString}{x.Prefix}");
                list.AddRange(currentContent);
            }
            return list;

        }
        public static Stream ReadBlobToStream(string uriString)
        {
            Uri uri = new Uri(uriString);
            BlobClient client = new BlobClient(uri);
            return client.OpenRead(new BlobOpenReadOptions(false));
        }

        

        public static void DownloadFile(string uriString, string localPath)
        {
            Uri uri = new Uri(uriString);
            BlobClient client = new BlobClient(uri);
            client.DownloadTo(localPath);
        }
        public static void Upload(string localPath, string uriString, string blobContainerName= "/dechcollections/")
        {
            string fullUriString = GetFullUriString(uriString, blobContainerName);
            Uri uri = new Uri(fullUriString + SasTokenDict[blobContainerName]);
            BlobClient client = new BlobClient(uri);
            client.Upload(localPath);
        }        
        public static void Delete(string uriString, string blobContainerName)
        {
            string uriFullString = GetFullUriString(uriString, blobContainerName);
            Uri uri = new Uri(uriFullString + SasTokenDict[blobContainerName]);
            BlobClient client = new BlobClient(uri);
            client.Delete();
        }

        public static string PathCombine(params string[] paths)
        {
            return paths.Aggregate((x, y) => $"{x.TrimEnd('/')}/{y.TrimStart('/')}");
        }

        public static string GetShort(string uriString)
        {
            if (uriString.StartsWith("https"))
            {
                Uri uri = new Uri(uriString);
                return uri.LocalPath;
            }
            return uriString;
        }
        public static bool BlobExists(string uriString)
        {
            Uri uri = new Uri(uriString);
            BlobClient client = new BlobClient(uri);
            return client.Exists();
        }

        public static bool IsValidFullUri(string uri)
        {
            return uri.StartsWith(Constants.AZURE_ROOT_PATH);
        }        
    }
}
