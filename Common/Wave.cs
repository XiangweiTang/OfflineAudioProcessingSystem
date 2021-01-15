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
        public double RMS
        {
            get
            {
                Sanity.Requires(IsDeep, $"Please deep parse the wave file.");
                return _RMS;
            }
        }
        private double _RMS = 0;

        private const string RIFF = "RIFF";
        private const string WAVE = "WAVE";
        private const string FORMAT = "fmt ";
        private const string DATA = "data";
        // This is the number of sample, not number of bytes.
        private const int BUFFER_SIZE = 10_000;

        private bool IsDeep = false;
        public WaveChunk FormatChunk { get; private set; }= new WaveChunk { Name = "", Length = -1, Offset = -1 };
        public WaveChunk DataChunk { get; private set; } = new WaveChunk { Name = "", Length = -1, Offset = -1 };
        public List<WaveChunk> ChunkList { get; private set; } = new List<WaveChunk>();
        public Wave() { }

        public static void CreateDummyPCMWave(string audioPath, double audioLength, short numChannels, short sampleRate, short bitsPerSample)
        {
            var bytes = CreateDummyPCMWave(audioLength, numChannels, sampleRate, bitsPerSample);
            File.WriteAllBytes(audioPath, bytes);
        }
        public static byte[] CreateDummyPCMWave(double audioLength, short numChannels, short sampleRate, short bitsPerSample)
        {
            int dataLength = (int)(audioLength * sampleRate * numChannels * bitsPerSample / 8);
            byte[] bytes = new byte[44 + dataLength];
            return SetHeader(bytes, numChannels, 1, sampleRate, bitsPerSample, dataLength);
        }
        private static byte[] SetHeader(byte[] bytes, short numChannels, short audioType, int sampleRate, short bitsPerSample, int dataLength)
        {
            Sanity.Requires(bytes.Length >= 44, "Wave has to be at least 44 bytes.");
            Array.Copy(Encoding.ASCII.GetBytes("RIFF"), 0, bytes, 0, 4);
            Array.Copy(Encoding.ASCII.GetBytes("WAVE"), 0, bytes, 8, 4);
            Array.Copy(Encoding.ASCII.GetBytes("fmt "), 0, bytes, 12, 4);
            Array.Copy(Encoding.ASCII.GetBytes("data"), 0, bytes, 36, 4);
            Array.Copy(BitConverter.GetBytes(dataLength + 44 - 8), 0, bytes, 4, 4);
            Array.Copy(BitConverter.GetBytes(16), 0, bytes, 16, 4);
            Array.Copy(BitConverter.GetBytes(audioType), 0, bytes, 20, 2);
            Array.Copy(BitConverter.GetBytes(numChannels), 0, bytes, 22, 2);
            Array.Copy(BitConverter.GetBytes(sampleRate), 0, bytes, 24, 4);
            int byteRate = (sampleRate * numChannels * bitsPerSample / 8);
            Array.Copy(BitConverter.GetBytes(byteRate), 0, bytes, 28, 4);
            short blockAlign = (short)(numChannels * bitsPerSample / 8);
            Array.Copy(BitConverter.GetBytes(blockAlign), 0, bytes, 32, 2);
            Array.Copy(BitConverter.GetBytes(bitsPerSample), 0, bytes, 34, 2);
            Array.Copy(BitConverter.GetBytes(dataLength), 0, bytes, 40, 4);
            return bytes;
        }
        public void ShallowParse(string filePath)
        {
            using(FileStream fs=new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                ShallowParse(fs);
            }
        }
        public void ShallowParse(Stream fs)
        {
            IsDeep = false;
            ParseWave(fs);
        }
        public void DeepParse(string filePath)
        {
            using(FileStream fs=new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                DeepParse(fs);
            }
        }

        public void DeepParse(FileStream fs)
        {
            IsDeep = true;
            ParseWave(fs);
        }
        private void ParseWave(Stream fs)
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

        private void ParseRecursively(Stream fs)
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

        private void PostCheck(Stream fs)
        {
            Sanity.Requires(!string.IsNullOrEmpty(FormatChunk.Name), "Invalid wave file, missing format chunk.");
            Sanity.Requires(!string.IsNullOrEmpty(DataChunk.Name), "Invalid wave file, missing data chunk.");
            PostCheckFormatChunk(fs);
            if (IsDeep)
                PostCheckDataChunk(fs);
        }

        private void PostCheckFormatChunk(Stream fs)
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

        private void PostCheckDataChunk(Stream fs)
        {
            fs.Seek(DataChunk.Offset, SeekOrigin.Begin);
            long totalSampleEnergy = 0;
            int totalSamples = 0;
            int divisor = 0;
            Func<Stream, int> GetSampleValue = null;
            switch (BitsPerSample)
            {
                case 8:
                    GetSampleValue = x => fs.ReadByte();
                    divisor = 256;
                    break;
                case 16:
                    GetSampleValue = x => x.ReadShortFromFileStream();
                    divisor = 65536;
                    break;
                default:
                    break;
            }
            Sanity.Requires(GetSampleValue != null, $"Unspported wave file. Bits per sample: {BitsPerSample}");
            while (fs.Position < fs.Length)
            {
                int sampleValue = GetSampleValue(fs);
                totalSampleEnergy += sampleValue * sampleValue;
                totalSamples++;
            }
            _RMS = Math.Sqrt((double)totalSampleEnergy / totalSamples) / divisor;
        }

        private void PostCheckDataChunkBlock(Stream fs)
        {
            fs.Seek(DataChunk.Offset + 8, SeekOrigin.Begin);
            byte[] buffer = new byte[BitsPerSample / 8 * BUFFER_SIZE];
            Func<byte[], int, long> readSquareSum = null;
            switch (BitsPerSample)
            {
                case 8:
                    readSquareSum = ReadBytesSquareSum;
                    break;
                case 16:
                    readSquareSum = ReadShortsSquareSum;
                    break;
                default:
                    break;
            }
            Sanity.Requires(readSquareSum != null, $"Unsupported bits per sample: {BitsPerSample}");
            long l = 0;
            int totalSamples = 0;
            while (fs.Position < fs.Length)
            {
                int n = fs.Read(buffer, 0, buffer.Length);
                l += readSquareSum(buffer, n);
                totalSamples += n;
            }
            _RMS = Math.Sqrt((double)l / totalSamples);
        }

        private long ReadBytesSquareSum(byte[] bytes, int n)
        {
            long l = 0;
            for(int i = 0; i < n; i++)
            {
                l += bytes[i] * bytes[i];
            }
            return l;
        }

        private long ReadShortsSquareSum(byte[] bytes, int n)
        {
            long l = 0;
            for(int i = 0; i < n; i += 2)
            {
                short s = BitConverter.ToInt16(bytes, i);
                l += s * s;
            }
            return l;
        }

        public IEnumerable<double> Energies(Stream st)
        {
            double frameLength = 1;
            int byteLength = (int)(frameLength * SampleRate);
            Func<byte[], int, double> localRMS = null;
            switch(BitsPerSample)
            {
                case 8:
                    localRMS = LocalRMS8Bits;
                    break;
                case 16:
                    byteLength *= 2;
                    localRMS = LocalRMS16Bits;
                    break;
                default:
                    throw new CommonException();
            }
            byte[] buffer = new byte[byteLength];
            st.Seek(DataChunk.Offset + 8, SeekOrigin.Begin);            
            while (byteLength + st.Position < st.Length)
            {
                int length = Math.Min(byteLength, (int)(st.Length - st.Position));
                st.Read(buffer, 0, byteLength);
                yield return localRMS(buffer, length);
            }
        }

        private double LocalRMS8Bits(byte[] buffer, int length)
        {
            long total = 0;            
            for(int i = 0; i < length; i += 1)
            {
                total += buffer[i] * buffer[i];
            }
            return Math.Sqrt((double)total / length);
        }

        private double LocalRMS16Bits(byte[] buffer, int length)
        {
            long total = 0;
            for (int i = 0; i < length; i += 2)
            {
                short v = BitConverter.ToInt16(buffer, i);
                total += v * v;
                
            }
            return Math.Sqrt((double)total * 2 / length);
        }
    }

    public struct WaveChunk
    {
        public string Name { get; set; }
        public int Length { get; set; }
        public int Offset { get; set; }
    }
}
