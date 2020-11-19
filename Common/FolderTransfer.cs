using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Common
{
    public abstract class FolderTransfer
    {
        public int MaxParallel { get; set; } = 10;
        public FolderTransfer() { }
        protected virtual void PreRun() { }
        public void Run(string inputFolder, string outputFolder, int maxParallel = 10)
        {
            PreRun();
            FolderTransferRecursively(inputFolder, outputFolder);
            PostRun();
        }
        protected virtual void PostRun() { }
        private void FolderTransferRecursively(string inputFolder, string outputFolder)
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);
            var inputList = GetItems(inputFolder);
            Parallel.ForEach(inputList, new ParallelOptions { MaxDegreeOfParallelism = MaxParallel }, inputItemPath =>
                 {
                     string inputItemName = inputItemPath.Split('\\').Last();
                     string outputItemName = ItemRename(inputItemName);
                     string outputItemPath = Path.Combine(outputFolder, outputItemName);
                     try
                     {
                         ItemTransfer(inputItemPath, outputItemPath);
                     }
                     catch(CommonException e)
                     {
                         Logger.WriteLine(e.Message, true, true);
                     }
                 });

            foreach(string inputSubFolder in Directory.EnumerateDirectories(inputFolder))
            {
                string subFolderName = inputSubFolder.Split('\\').Last();
                string outputSubFolderPath = Path.Combine(outputFolder, subFolderName);
                FolderTransferRecursively(inputSubFolder, outputSubFolderPath);
            }
        }
        protected virtual IEnumerable<string> GetItems(string inputFolderPath)
        {
            return Directory.EnumerateFiles(inputFolderPath);
        }
        protected abstract void ItemTransfer(string inputPath, string outputPath);
        public virtual string ItemRename(string inputItemName)
        {
            return inputItemName;
        }
    }
}
