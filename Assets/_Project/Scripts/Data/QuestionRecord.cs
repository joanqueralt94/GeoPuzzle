using System;

namespace GeoPuzzle.Data
{
    /// <summary>A hand-written question from questions.json. Text fields are Localization keys.</summary>
    [Serializable]
    public class QuestionRecord
    {
        public string id;
        public string type;
        public string promptKey;
        public string[] answers = Array.Empty<string>();
        public int correct;
        public string[] countries = Array.Empty<string>();
        public QuestionMedia media = new QuestionMedia();
        public int difficulty = 1;
        public string[] tags = Array.Empty<string>();
    }

    [Serializable]
    public class QuestionMedia
    {
        public string flag;
        public string mapFocus;
    }

    [Serializable]
    public class QuestionList
    {
        public QuestionRecord[] questions = Array.Empty<QuestionRecord>();
    }
}
