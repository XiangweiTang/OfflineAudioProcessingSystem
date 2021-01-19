using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Common;

namespace OfflineAudioProcessingSystem.TranscriptValidation
{
    static class Mapping
    {
        public static void MapToMappingLine(string inputPath, string outputPath, string localRootPath, string azureRootPath)
        {
            var list = File.ReadLines(inputPath)
                .Select(x => MappingTransfer(localRootPath, azureRootPath, new MappingLine(x)).Output());
            File.WriteAllLines(outputPath, list);
        }

        private static MappingLine MappingTransfer(string localRootPath, string azureRootPath, MappingLine line)
        {            
            string localFolderPath = Directory.EnumerateDirectories(localRootPath, $"{line.TaskId}*").Single();
            string filePath = Path.Combine(localFolderPath, "Speaker", line.TaskInternalName.Replace("ü","u_"));
            if (File.Exists(filePath))
                line.LocalPath = filePath;
            string azureName = AzureUtils.PathCombine(azureRootPath, line.Dialect, localFolderPath.Split('\\').Last().Replace($"{line.TaskId}_", "").Replace("uu","u"), line.TaskInternalName.Replace(' ', '+').Replace(".txt", ".wav"));
            if (AzureUtils.BlobExists(azureName))
                line.AzureName = AzureUtils.GetShort(azureName);
            else;
            return line;
        }
    }
}
