using System.Collections.Generic;

namespace GeoPuzzle.Quiz
{
    public enum QuestionKind
    {
        FindOnMap,
        WhichFlag,
        CapitalOf,
    }

    /// <summary>A question built at runtime from country data. Options are country ids.</summary>
    public class GeneratedQuestion
    {
        public QuestionKind Kind { get; }
        public string TargetId { get; }
        public IReadOnlyList<string> Options { get; }
        public int CorrectIndex { get; }

        public GeneratedQuestion(QuestionKind kind, string targetId, IReadOnlyList<string> options, int correctIndex)
        {
            Kind = kind;
            TargetId = targetId;
            Options = options;
            CorrectIndex = correctIndex;
        }

        public bool IsCorrect(string countryId) => countryId == TargetId;
    }
}
