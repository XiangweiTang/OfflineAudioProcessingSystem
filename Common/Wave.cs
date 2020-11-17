using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Common
{
    public class Wave
    {
        public short WaveType { get; private set; } = 0;
        public short NumChannels { get; private set; } = 0;
        public int SampleRate { get; private set; } = 1;
        public int ByteRate { get; private set; } = 1;
        public short BitsPerSample { get; private set; } = 1;
        public short BlockAlign { get; private set; } = 1;
        public double AudioLength { get; private set; } = 0;

        private const string RIFF = "RIFF";
        private const string WAVE = "WAVE";
        private const string FORMAT = "fmt ";
        private const string DATA = "data";
        public WaveChunk FormatChunk { get; private set; }= new WaveChunk { Name = "", Length = -1, Offset = -1 };
        public WaveChunk DataChunk { get; private set; } = new WaveChunk { Name = "", Length = -1, Offset = -1 };
        public List<WaveChunk> ChunkList { get; private set; } = new List<WaveChunk>();
        public Wave() { }
        
        public void Load(FileStream fs)
        {
            ParseWave(fs);
        }
        public void Load(string filePath)
        {
            using(FileStream fs=new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                Load(fs);
            }
        }
        private void ParseWave(FileStream fs)
        {
            Sanity.Requires(fs.Length >= 44, "Invalid wave file, size too small.");
            Sanity.Requires(fs.Length <= int.MaxValue, "Invalid wave file, size too big.");
            
            string riff = fs.ReadStringFromFileStream(Encoding.ASCII, 4);
            Sanity.Requires(riff == RIFF, "Invalid wave file, broken RIFF header.");
            
            int length = fs.ReadIntFromFileStream();
            Sanity.Requires(length + fs.Position == fs.Length, "Invalid wave file, shorter than expected.");

            string wave = fs.ReadStringFromFileStream(Encoding.ASCII, 4);
            Sanity.Requires(wave == WAVE, "Invalid wave file, broken WAVE header.");

            ParseRecursively(fs);

            PostCheck(fs);
        }

        private void ParseRecursively(FileStream fs)
        {
            if (fs.Position == fs.Length)
                return;
            
            Sanity.Requires(fs.Position + 8 <= fs.Length, "Invalid wave file, shorter than expected.");
            string chunkName = fs.ReadStringFromFileStream(Encoding.ASCII, 4);
            int chunkSize = fs.ReadIntFromFileStream();
            int chunkOffset = (int)fs.Position;
            Sanity.Requires(chunkOffset + chunkSize <= fs.Length, $"Invalid wave file, shorter than expected in {chunkName}.");
            WaveChunk chunk = new WaveChunk
            {
                Name = chunkName,
                Offset = chunkOffset,
                Length = chunkSize
            };
            switch (chunk.Name)
            {
                case FORMAT:
                    Sanity.Requires(FormatChunk.Name == "", "Invalid wave file, more than one format chunk.");
                    FormatChunk = chunk;
                    break;
                case DATA:
                    Sanity.Requires(DataChunk.Name == "", "Invalid wave file, more than one data chunk.");
                    DataChunk = chunk;
                    break;
                default:
                    break;
            }
            ChunkList.Add(chunk);

            fs.Seek(chunk.Length, SeekOrigin.Current);

            ParseRecursively(fs);
        }

        private void PostCheck(FileStream fs)
        {
            Sanity.Requires(!string.IsNullOrEmpty(FormatChunk.Name), "Invalid wave file, missing format chunk.");
            Sanity.Requires(!string.IsNullOrEmpty(DataChunk.Name), "Invalid wave file, missing data chunk.");
            PostCheckFormatChunk(fs);
        }

        private void PostCheckFormatChunk(FileStream fs)
        {
            fs.Seek(FormatChunk.Offset, SeekOrigin.Begin);
            WaveType = fs.ReadShortFromFileStream();
            NumChannels = fs.ReadShortFromFileStream();
            SampleRate = fs.ReadIntFromFileStream();
            ByteRate = fs.ReadIntFromFileStream();
            BlockAlign = fs.ReadShortFromFileStream();
            BitsPerSample = fs.ReadShortFromFileStream();

            Sanity.Requires(ByteRate == SampleRate * BlockAlign, $"Invalid audio: ByteRate: {ByteRate}, SampleRate: {SampleRate}, BlockAlign: {BlockAlign}.");
            Sanity.Requires(BitsPerSample * NumChannels == 8 * BlockAlign, $"Invalid audio: BitsPerSample: {BitsPerSample}, NumChannels: {NumChannels}, BlockAlign: {BlockAlign}");

            AudioLength = (double)DataChunk.Length / ByteRate;
        }
    }

    public struct WaveChunk
    {
        public string Name { get; set; }
        public int Length { get; set; }
        public int Offset { get; set; }
    }
}
