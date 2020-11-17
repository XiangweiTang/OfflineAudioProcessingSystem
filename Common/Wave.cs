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
        const string RIFF = "RIFF";
        const string WAVE = "WAVE";
        const string FORMAT = "fmt ";
        const string DATA = "data";
        public WaveChunk FormatChunk { get; private set; }= new WaveChunk { Name = "", Size = -1, Offset = -1 };
        public WaveChunk DataChunk { get; private set; } = new WaveChunk { Name = "", Size = -1, Offset = -1 };
        public List<WaveChunk> ChunkList { get; private set; } = new List<WaveChunk>();
        public Wave() { }
        
        private void ParseHeader(FileStream fs)
        {
            Sanity.Requires(fs.Length >= 44, "Invalid wave file, size too small.");
            Sanity.Requires(fs.Length <= int.MaxValue, "Invalid wave file, size too big.");
            
            string riff = fs.ReadStringFromFileStream(Encoding.ASCII, 4);
            Sanity.Requires(riff == RIFF, "Invalid wave file, broken RIFF header.");
            
            int length = fs.ReadIntFromFileStream();
            Sanity.Requires(length + fs.Position == fs.Length, "Invalid wave file, shorter than expected.");

            string wave = fs.ReadStringFromFileStream(Encoding.ASCII, 4);
            Sanity.Requires(wave == WAVE, "Invalid wave file, broken WAVE header.");


        }

        private void ParseRecursively(FileStream fs)
        {
            if (fs.Position == fs.Length)
                return;
            Sanity.Requires(fs.Position + 8 <= fs.Length, "Invalid wave file, shorter than expected.");
            int offset = (int)fs.Position;
            string chunkName = fs.ReadStringFromFileStream(Encoding.ASCII, 4);
            int chunkSize = fs.ReadIntFromFileStream();
            
        }
    }

    public struct WaveChunk
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public int Offset { get; set; }
    }
}
