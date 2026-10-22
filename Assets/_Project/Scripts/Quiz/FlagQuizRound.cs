using System;
using System.Collections.Generic;

namespace GeoPuzzle.Quiz
{
    /// <summary>Numbers for one Flag Quizz level, read from the economy config by the caller.</summary>
    public class FlagQuizSettings
    {
        public int Questions = 10;
        public int Hearts = 3;
        public float TimerSeconds = 90f;
        public int ThreeStarMaxHeartsLost;
        public int TwoStarMaxHeartsLost = 1;
        public int Options = 4;
    }

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

    public struct AnswerResult
    {
        public readonly bool Correct;
        public readonly int CorrectIndex;

        public AnswerResult(bool correct, int correctIndex)
        {
            Correct = correct;
            CorrectIndex = correctIndex;
        }
    }

    /// <summary>
    /// One Flag Quizz level: a flag and four country options per question, a timer for the whole level,
    /// and hearts that a wrong answer costs. Losing all hearts or running out of time ends the level;
    /// <see cref="PlayOn"/> resumes it, for a play-on paid with coins or an ad.
    /// </summary>
    public class FlagQuizRound
    {
        readonly QuestionGenerator generator;
        readonly FlagQuizSettings settings;
        readonly HashSet<string> asked = new HashSet<string>(StringComparer.Ordinal);
        bool currentSpent;

        public GeneratedQuestion Current { get; private set; }
        public int QuestionNumber { get; private set; }
        public int QuestionCount => settings.Questions;
        public int HeartsLeft { get; private set; }
        public int HeartsLost { get; private set; }
        public float TimeLeft { get; private set; }
        public int CorrectAnswers { get; private set; }
        public RoundState State { get; private set; }
        public LoseReason LoseReason { get; private set; }

        public event Action<RoundState> Ended;

        public FlagQuizRound(QuestionGenerator generator, FlagQuizSettings settings)
        {
            this.generator = generator;
            this.settings = settings;
            HeartsLeft = settings.Hearts;
            TimeLeft = settings.TimerSeconds;
            NextQuestion();
        }

        /// <summary>0 when the level was lost; otherwise 1 to 3 depending on hearts lost.</summary>
        public int Stars
        {
            get
            {
                if (State != RoundState.Won)
                    return 0;
                if (HeartsLost <= settings.ThreeStarMaxHeartsLost)
                    return 3;
                return HeartsLost <= settings.TwoStarMaxHeartsLost ? 2 : 1;
            }
        }

        public AnswerResult Answer(int optionIndex)
        {
            if (State != RoundState.Playing)
                throw new InvalidOperationException($"The level is over ({State})");

            var correct = optionIndex == Current.CorrectIndex;
            if (correct)
            {
                CorrectAnswers++;
            }
            else
            {
                HeartsLeft--;
                HeartsLost++;
            }

            var result = new AnswerResult(correct, Current.CorrectIndex);
            if (HeartsLeft <= 0)
            {
                currentSpent = true;
                End(RoundState.Lost, LoseReason.OutOfHearts);
            }
            else
            {
                AdvanceOrWin();
            }
            return result;
        }

        public void Tick(float deltaSeconds)
        {
            if (State != RoundState.Playing)
                return;
            TimeLeft = Math.Max(0f, TimeLeft - deltaSeconds);
            if (TimeLeft <= 0f)
                End(RoundState.Lost, LoseReason.OutOfTime);
        }

        /// <summary>
        /// Continues a lost level with extra hearts and/or seconds. After running out of time the same
        /// question stays; after losing the last heart the level moves on, since that question was answered.
        /// </summary>
        public void PlayOn(int extraHearts, float extraSeconds)
        {
            if (State != RoundState.Lost)
                throw new InvalidOperationException("Only a lost level can be continued");
            HeartsLeft += extraHearts;
            TimeLeft += extraSeconds;
            if (HeartsLeft <= 0 || TimeLeft <= 0f)
                return;

            State = RoundState.Playing;
            LoseReason = LoseReason.None;
            if (currentSpent)
                AdvanceOrWin();
        }

        void AdvanceOrWin()
        {
            if (QuestionNumber >= settings.Questions)
                End(RoundState.Won, LoseReason.None);
            else
                NextQuestion();
        }

        void NextQuestion()
        {
            QuestionNumber++;
            currentSpent = false;
            Current = generator.CreateRandom(QuestionKind.WhichFlag, asked, settings.Options);
            asked.Add(Current.TargetId);
        }

        void End(RoundState state, LoseReason reason)
        {
            State = state;
            LoseReason = reason;
            Ended?.Invoke(state);
        }
    }
}
