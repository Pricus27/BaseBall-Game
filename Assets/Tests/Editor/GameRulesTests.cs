using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BaseballTyping.Tests
{
    public sealed class GameRulesTests
    {
        [TestCase(0, 1f)]
        [TestCase(4, 1f)]
        [TestCase(5, 1.5f)]
        [TestCase(10, 2f)]
        [TestCase(15, 2.5f)]
        [TestCase(20, 3f)]
        [TestCase(100, 3f)]
        public void Multiplier_UsesFiveHitTiers(int streak, float expected)
        {
            Assert.That(GameRules.MultiplierForStreak(streak), Is.EqualTo(expected));
        }

        [Test]
        public void Score_IncludesDistanceBonusAndMultiplier()
        {
            Assert.That(GameRules.ScoreForHit(100, 0.75f, 2f), Is.EqualTo(350));
        }

        [Test]
        public void FifthHit_UsesIncreasedMultiplier()
        {
            var tracker = new ScoreTracker();
            var pitch = new PitchSpec(PitchKind.Fastball, 3f, 100, Color.white);
            for (int i = 0; i < 4; i++) tracker.RegisterHit(pitch, 0f);

            int fifth = tracker.RegisterHit(pitch, 0f);

            Assert.That(fifth, Is.EqualTo(150));
            Assert.That(tracker.Multiplier, Is.EqualTo(1.5f));
        }

        [Test]
        public void Typo_ResetsErrorFreeStreak()
        {
            var tracker = new ScoreTracker();
            var pitch = new PitchSpec(PitchKind.Fastball, 3f, 100, Color.white);
            for (int i = 0; i < 7; i++) tracker.RegisterHit(pitch, 0f);

            tracker.RegisterTypo();

            Assert.That(tracker.ErrorFreeHits, Is.Zero);
            Assert.That(tracker.Multiplier, Is.EqualTo(1f));
        }

        [Test]
        public void TargetSelection_PicksNearestMatchingPitch()
        {
            var words = new List<string> { "ball", "base", "run" };
            var progress = new List<float> { 0.2f, 0.8f, 0.9f };

            int result = TargetSelector.SelectNearestMatchingFirst(words, progress, 'b');

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void StrikeThreshold_EndsRunAfterStun()
        {
            Assert.That(GameRules.StrikesForOut, Is.EqualTo(3));
            Assert.That(GameRules.StrikeoutSeconds, Is.EqualTo(2f));
            Assert.That(RoundTransitions.AfterStrike(2), Is.EqualTo(RoundState.Playing));
            Assert.That(RoundTransitions.AfterStrike(3), Is.EqualTo(RoundState.Stunned));
            Assert.That(RoundTransitions.AfterStrikeoutStun(0.5f), Is.EqualTo(RoundState.Stunned));
            Assert.That(RoundTransitions.AfterStrikeoutStun(0f), Is.EqualTo(RoundState.GameOver));
        }

        [Test]
        public void LevelThresholds_MatchScoreGates()
        {
            Assert.That(GameRules.BossThreshold(1), Is.EqualTo(1000));
            Assert.That(GameRules.BossThreshold(2), Is.EqualTo(2500));
            Assert.That(GameRules.BossThreshold(3), Is.EqualTo(4500));
            Assert.That(GameRules.BossThreshold(4), Is.EqualTo(7000));
            Assert.That(GameRules.BossThreshold(5), Is.EqualTo(10000));
            Assert.That(GameRules.PointsToBoss(400, 1), Is.EqualTo(600));
            Assert.That(GameRules.DifficultyForLevel(1), Is.EqualTo(0f));
            Assert.That(GameRules.MaxActivePitches(1), Is.EqualTo(4));
            Assert.That(GameRules.MaxActivePitches(5), Is.EqualTo(6));
        }

        [Test]
        public void CorkedBat_CompletesWhenTwoLettersRemain()
        {
            Assert.That(GameRules.CorkedCompletes("strike", "stri", true), Is.True);
            Assert.That(GameRules.CorkedCompletes("strike", "str", true), Is.False);
            Assert.That(GameRules.CorkedCompletes("strike", "stri", false), Is.False);
            Assert.That(GameRules.CorkedCompletes("go", "", true), Is.False);
            Assert.That(GameRules.CorkedCompletes("go", "g", true), Is.True);
        }

        [Test]
        public void JuicedRoll_UsesTenPercentChance()
        {
            Assert.That(GameRules.ShouldJuice(0.09f), Is.True);
            Assert.That(GameRules.ShouldJuice(0.10f), Is.False);
        }

        [Test]
        public void JuggernautStream_CorrectLettersChipHp()
        {
            var stream = new JuggernautStream(new[] { "go", "up" });
            Assert.That(stream.MaxHp, Is.EqualTo(4));
            Assert.That(stream.TypeLetter('g'), Is.True);
            Assert.That(stream.Hp, Is.EqualTo(3));
            Assert.That(stream.TypeLetter('x'), Is.False);
            Assert.That(stream.Hp, Is.EqualTo(3));
        }

        [Test]
        public void JuggernautStream_CheckSwingClearsTypedLetters()
        {
            var stream = new JuggernautStream(new[] { "hit" });
            stream.TypeLetter('h');
            stream.TypeLetter('i');
            stream.CheckSwing();
            Assert.That(stream.Typed, Is.EqualTo(string.Empty));
            Assert.That(stream.Hp, Is.EqualTo(stream.MaxHp));
            Assert.That(stream.CurrentWord, Is.EqualTo("hit"));
        }

        [Test]
        public void BonusPoints_IncreaseScoreWithoutCombo()
        {
            var tracker = new ScoreTracker();
            int awarded = tracker.AddBonus(2000);
            Assert.That(awarded, Is.EqualTo(2000));
            Assert.That(tracker.Score, Is.EqualTo(2000));
            Assert.That(tracker.ErrorFreeHits, Is.Zero);
        }

        [Test]
        public void AllPitchTypes_HaveCorrectBaseScores()
        {
            Assert.That(GameRules.SpecFor(PitchKind.Fastball, 0f).BasePoints, Is.EqualTo(100));
            Assert.That(GameRules.SpecFor(PitchKind.Curveball, 0f).BasePoints, Is.EqualTo(100));
            Assert.That(GameRules.SpecFor(PitchKind.Changeup, 0f).BasePoints, Is.EqualTo(150));
            Assert.That(GameRules.SpecFor(PitchKind.FlamingKnuckleball, 0f).BasePoints, Is.EqualTo(500));
        }
    }
}
