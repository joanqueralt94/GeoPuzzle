using System.Collections.Generic;
using System.Linq;
using GeoPuzzle.Data;
using GeoPuzzle.Quiz;
using NUnit.Framework;

namespace GeoPuzzle.Tests
{
    public class FlagQuizRoundTests
    {
        static readonly string[] Ids = { "ESP", "PRT", "ITA", "GRC", "FRA", "DEU", "JPN", "KEN", "BRA", "CAN", "AUS", "IND" };

        static FlagQuizRound MakeRound(int questions = 5, int hearts = 3, float timer = 60f)
        {
            var database = new CountryDatabase(Ids.Select(id => new CountryRecord
            {
                id = id, nameKey = id, flag = id, continent = "World", subregion = "World",
            }));
            var settings = new FlagQuizSettings
            {
                Questions = questions,
                Hearts = hearts,
                TimerSeconds = timer,
                ThreeStarMaxHeartsLost = 0,
                TwoStarMaxHeartsLost = 1,
            };
            return new FlagQuizRound(new QuestionGenerator(database, seed: 7), settings);
        }

        static int Wrong(FlagQuizRound round) => (round.Current.CorrectIndex + 1) % round.Current.Options.Count;

        [Test]
        public void AnsweringEverythingRightWinsWithThreeStars()
        {
            var round = MakeRound(questions: 5);
            RoundState? ended = null;
            round.Ended += state => ended = state;

            for (var i = 0; i < 5; i++)
                Assert.IsTrue(round.Answer(round.Current.CorrectIndex).Correct);

            Assert.AreEqual(RoundState.Won, round.State);
            Assert.AreEqual(RoundState.Won, ended);
            Assert.AreEqual(5, round.CorrectAnswers);
            Assert.AreEqual(3, round.Stars);
        }

        [Test]
        public void StarsDropWithHeartsLost()
        {
            var oneMistake = MakeRound(questions: 3);
            oneMistake.Answer(Wrong(oneMistake));
            while (oneMistake.State == RoundState.Playing)
                oneMistake.Answer(oneMistake.Current.CorrectIndex);
            Assert.AreEqual(2, oneMistake.Stars);

            var twoMistakes = MakeRound(questions: 3);
            twoMistakes.Answer(Wrong(twoMistakes));
            twoMistakes.Answer(Wrong(twoMistakes));
            twoMistakes.Answer(twoMistakes.Current.CorrectIndex);
            Assert.AreEqual(RoundState.Won, twoMistakes.State);
            Assert.AreEqual(1, twoMistakes.Stars);
        }

        [Test]
        public void LosingAllHeartsLosesTheLevel()
        {
            var round = MakeRound(questions: 10, hearts: 2);
            var first = round.Answer(Wrong(round));
            Assert.IsFalse(first.Correct);
            Assert.AreEqual(1, round.HeartsLeft);

            round.Answer(Wrong(round));
            Assert.AreEqual(RoundState.Lost, round.State);
            Assert.AreEqual(LoseReason.OutOfHearts, round.LoseReason);
            Assert.AreEqual(0, round.Stars);
        }

        [Test]
        public void RunningOutOfTimeLosesTheLevel()
        {
            var round = MakeRound(timer: 10f);
            round.Tick(9f);
            Assert.AreEqual(RoundState.Playing, round.State);
            round.Tick(2f);
            Assert.AreEqual(RoundState.Lost, round.State);
            Assert.AreEqual(LoseReason.OutOfTime, round.LoseReason);
            Assert.AreEqual(0f, round.TimeLeft);
        }

        [Test]
        public void PlayOnAfterTimeKeepsTheSameQuestion()
        {
            var round = MakeRound(timer: 5f);
            var question = round.Current;
            round.Tick(6f);

            round.PlayOn(0, 10f);
            Assert.AreEqual(RoundState.Playing, round.State);
            Assert.AreEqual(LoseReason.None, round.LoseReason);
            Assert.AreSame(question, round.Current);
            Assert.AreEqual(10f, round.TimeLeft, 0.001f);
        }

        [Test]
        public void PlayOnAfterHeartsMovesToTheNextQuestion()
        {
            var round = MakeRound(questions: 5, hearts: 1);
            round.Answer(Wrong(round));
            Assert.AreEqual(RoundState.Lost, round.State);

            round.PlayOn(3, 0f);
            Assert.AreEqual(RoundState.Playing, round.State);
            Assert.AreEqual(2, round.QuestionNumber);
            Assert.AreEqual(3, round.HeartsLeft);
        }

        [Test]
        public void PlayOnOnTheLastQuestionWins()
        {
            var round = MakeRound(questions: 1, hearts: 1);
            round.Answer(Wrong(round));
            round.PlayOn(1, 0f);
            Assert.AreEqual(RoundState.Won, round.State);
            Assert.AreEqual(2, round.Stars);
        }

        [Test]
        public void PlayOnIsOnlyForLostLevels()
        {
            var round = MakeRound();
            Assert.Throws<System.InvalidOperationException>(() => round.PlayOn(1, 0f));
        }

        [Test]
        public void TargetsDoNotRepeatWithinALevel()
        {
            var round = MakeRound(questions: Ids.Length);
            var targets = new List<string>();
            while (round.State == RoundState.Playing)
            {
                targets.Add(round.Current.TargetId);
                round.Answer(round.Current.CorrectIndex);
            }
            Assert.AreEqual(Ids.Length, targets.Distinct().Count());
        }
    }
}
