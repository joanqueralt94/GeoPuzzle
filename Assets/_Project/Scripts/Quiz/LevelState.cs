namespace GeoPuzzle.Quiz
{
    public enum RoundState
    {
        Playing,
        Won,
        Lost,
    }

    public enum LoseReason
    {
        None,
        OutOfHearts,
        OutOfTime,
    }

    /// <summary>
    /// A level that can be continued after it is lost. Every mode with hearts and a level timer implements
    /// this, so the same play-on rules and screen work for all of them.
    /// </summary>
    public interface IPlayOnLevel
    {
        RoundState State { get; }
        LoseReason LoseReason { get; }
        int HeartsLeft { get; }

        /// <summary>Adds hearts and/or seconds to a lost level and resumes it if both are above zero.</summary>
        void PlayOn(int extraHearts, float extraSeconds);
    }
}
