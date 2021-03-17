using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace OfflineAudioProcessingSystem
{
    class LineSets
    {
    }
    class AudioMappingLine : Line
    {
        public AudioMappingLine(string s) : base(s) { }
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string AudioId { get; set; }
        public string WavName { get; set; }
        public string SpeakerId { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        protected override IEnumerable<object> GetLine()
        {
            yield return TaskId;
            yield return TaskName;
            yield return AudioId;
            yield return WavName;
            yield return SpeakerId;
            yield return Gender;
            yield return Age;
        }

        protected override void SetLine(string[] split)
        {
            TaskId = split[0];
            TaskName = split[1];
            AudioId = split[2];
            WavName = split[3];
            SpeakerId = split[4];
            Gender = split[5];
            Age = split[6];
        }
    }
    class OverallMappingLine : Line
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string AudioId { get; set; }
        public string AudioName { get; set; }
        public string Speaker { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public string Dialect { get; set; }
        public string AudioFolder { get; set; }
        public string AudioPath { get; set; }
        public string TeamName { get; set; }
        public string DupeGroup { get; set; }
        public bool ValidFlag { get; set; }
        public string AudioTime { get; set; }
        public string SpeechRatio { get; set; }
        public string MergedId => $"{Dialect}_{Speaker}";
        public OverallMappingLine() { }
        public OverallMappingLine(string lineStr) : base(lineStr)
        {
        }

        protected override IEnumerable<object> GetLine()
        {
            yield return TaskId;
            yield return TaskName;
            yield return AudioId;
            yield return AudioName;
            yield return Speaker;
            yield return Gender;
            yield return Age == "0" ? "" : Age.ToString();
            yield return Dialect;
            yield return AudioFolder;
            yield return AudioPath;
            yield return TeamName;
            yield return DupeGroup;
            yield return ValidFlag;
            yield return AudioTime;
            yield return SpeechRatio;
        }

        protected override void SetLine(string[] split)
        {
            TaskId = split[0];
            TaskName = split[1];
            AudioId = split[2];
            AudioName = split[3];
            Speaker = split[4].ToLower();
            Gender = split[5];
            Age = split[6];
            Dialect = split[7].ToLower();
            AudioFolder = split[8];
            AudioPath = split[9];
            TeamName = split[10];
            if (string.IsNullOrWhiteSpace(TeamName))
                TeamName = "NotAssigned";
            DupeGroup = split[11];
            ValidFlag = split[12] != "" ? bool.Parse(split[12]) : true;
            AudioTime = split[13];
            SpeechRatio = split[14];
        }
    }

    class ReportLine : Line
    {
        public string Dialect { get; set; }
        public string SpeakerId { get; set; }
        public string AudioId { get; set; }
        public string AzurePath { get; set; }
        public string RecordedBy { get; set; }
        public string AnnotatedBy { get; set; }
        public ReportLine(string s) : base(s) { }
        public ReportLine() : base() { }
        protected override IEnumerable<object> GetLine()
        {
            yield return Dialect;
            yield return SpeakerId;
            yield return AudioId;
            yield return AzurePath;
            yield return RecordedBy;
            yield return AnnotatedBy;
        }

        protected override void SetLine(string[] split)
        {
            Dialect = split[0];
            SpeakerId = split[1];
            AudioId = split[2];
            AzurePath = split[3];
            RecordedBy = split[4];
            AnnotatedBy = split[5];
        }
    }

    class MetaDataLine : Line
    {
        public string Locale { get; set; }
        public string SpeakerId { get; set; }
        public string AudioId { get; set; }
        public string RelativePath { get; set; }
        public string RecordedBy { get; set; }
        public string AnnotatedBy { get; set; }
        public MetaDataLine(string s) : base(s) { }
        public MetaDataLine() : base() { }
        protected override IEnumerable<object> GetLine()
        {
            yield return Locale;
            yield return SpeakerId;
            yield return AudioId;
            yield return RelativePath;
            yield return RecordedBy;
            yield return AnnotatedBy;
        }

        protected override void SetLine(string[] split)
        {
            Locale = split[0];
            SpeakerId = split[1];
            AudioId = split[2];
            RelativePath = split[3];
            RecordedBy = split[4];
            AnnotatedBy = split[5];
        }
    }

    class NewAddedLine : Line
    {
        public int AudioId { get; set; }
        public string LocalAudioPath { get; set; }
        public string Locale { get; set; }
        public string InternalSpeakerId { get; set; }
        public NewAddedLine(string s) : base(s) { }
        public NewAddedLine() : base() { }

        protected override void SetLine(string[] split)
        {
            AudioId = int.Parse(split[0]);
            LocalAudioPath = split[1];
            Locale = split[2].ToLower();
            InternalSpeakerId = split[3].ToLower();
        }

        protected override IEnumerable<object> GetLine()
        {
            yield return AudioId;
            yield return LocalAudioPath;
            yield return Locale;
            yield return InternalSpeakerId;
        }
    }

    class FullMappingLine : Line
    {
        public int AudioPlatformId { get; set; }
        public string OldPath { get; set; }
        public string Locale { get; set; }
        public string InternalSpeakerId { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public int UniversalSpeakerId { get; set; }
        public string UniversalSpeakerString => $"{UniversalSpeakerId:00000}";
        public int UniversalAudioId { get; set; }
        public string UniversalAudioString => $"{UniversalAudioId:00000}";
        public string NewPath { get; set; }
        public FullMappingLine(string s) : base(s) { }
        public string MergedKey => $"{Locale.ToLower()}_{InternalSpeakerId}";
        public FullMappingLine() : base() { }
        protected override IEnumerable<object> GetLine()
        {
            yield return AudioPlatformId;
            yield return OldPath;
            yield return Locale;
            yield return InternalSpeakerId;
            yield return Gender;
            yield return Age == 0 ? "" : Age.ToString();
            yield return UniversalSpeakerId.ToString("00000");
            yield return UniversalAudioId.ToString("00000");
            yield return NewPath;
        }

        protected override void SetLine(string[] split)
        {
            AudioPlatformId = int.Parse(split[0]);
            OldPath = split[1];
            Locale = split[2].ToLower();
            InternalSpeakerId = split[3].ToLower();
            Gender = split[4];
            Age = int.Parse(split[5] == "" ? "0" : split[5]);
            UniversalSpeakerId = int.Parse(split[6]);
            UniversalAudioId = int.Parse(split[7]);
            NewPath = split[8];
        }
    }

    class IdDictLine : Line
    {
        public string UniversalId { get; set; }
        public string MergedId { get; set; }
        public string InternalId { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public IdDictLine(string s) : base(s) { }
        public IdDictLine() : base() { }
        protected override IEnumerable<object> GetLine()
        {
            yield return UniversalId;
            yield return MergedId;
            yield return InternalId;
            yield return Gender;
            yield return Age;
        }

        protected override void SetLine(string[] split)
        {
            UniversalId = split[0];
            MergedId = split[1];
            InternalId = split[2];
            Gender = split[3];
            Age = split[4];
        }
    }


    class TmpLine : Line
    {
        public string TmpId { get; set; }
        public string LocalPath { get; set; }
        public string Locale { get; set; }
        public string Speaker { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public string MergeSpeakerId => $"{Locale}_{Speaker}";
        public TmpLine(string s) : base(s) { }
        public TmpLine() : base() { }
        protected override IEnumerable<object> GetLine()
        {
            yield return TmpId;
            yield return LocalPath;
            yield return Locale;
            yield return Speaker;
            yield return Gender;
            yield return Age;
        }

        protected override void SetLine(string[] split)
        {
            TmpId = split[0];
            LocalPath = split[1].ToLower();
            Locale = split[2].ToLower();
            Speaker = split[3].ToLower();
            Gender = split[4].ToLower();
            Age = split[5];
        }
    }

    class AnnotationLine : Line
    {
        public int TaskId { get; set; } 
        public string TaskName { get; set; }
        public string TaskStatus { get; set; }
        public int AudioPlatformId { get; set; }
        public string AudioName { get; set; }
        public AnnotationLine(string s) : base(s) { }
        public AnnotationLine() : base() { }
        protected override IEnumerable<object> GetLine()
        {
            yield return TaskId;
            yield return TaskName;
            yield return TaskStatus;
            yield return AudioPlatformId;
            yield return AudioName;

        }

        protected override void SetLine(string[] split)
        {
            TaskId = int.Parse(split[0]);
            TaskName = split[1];
            TaskStatus = split[2];
            AudioPlatformId = int.Parse(split[3]);
            AudioName = split[4];
        }
    }
}
