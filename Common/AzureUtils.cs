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
            string uriString = "https://marksystemapistorage.blob.core.windows.net/chdelivery2021/";
            string uriWithSas = SetSasToken(uriString);
            Uri uri = new Uri(uriWithSas);
            BlobContainerClient client = new BlobContainerClient(uri, new BlobClientOptions { });
            var r = client.Exists().Value;
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
        public static List<string> ListDirectories(string blobString, string blobContainerName="")
        {
            Uri tmpUri = new Uri(blobString);
            string prefix = tmpUri.LocalPath.Substring(blobString.Length);

            Uri uri = new Uri($"https://{tmpUri.Host}{blobString}");
            BlobContainerClient client = new BlobContainerClient(uri);
            
            var pages = client.GetBlobsByHierarchy(prefix: prefix, delimiter: "/").AsPages();
            List<string> list = new List<string>();
            foreach(Page<BlobHierarchyItem> page in pages)
            {
                list.AddRange(page.Values.Where(x => x.IsPrefix).Select(x => $"{uri.OriginalString}{x.Prefix}"));
            }
            return list;
        }
        public static List<string> ListCurrentBlobs(string blobString, string blobContainerName="")
        {
            Uri tmpUri = new Uri(blobString);
            string prefix = tmpUri.LocalPath.Substring(blobString.Length);

            Uri uri = new Uri($"https://{tmpUri.Host}{blobString}");
            BlobContainerClient client = new BlobContainerClient(uri);
            var pages = client.GetBlobs(prefix: prefix).AsPages();

            List<string> list = new List<string>();
            foreach(Page<BlobItem> page in pages)
            {                
                list.AddRange(page.Values.Select(x => $"{uri.OriginalString}{x.Name}"));                
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
