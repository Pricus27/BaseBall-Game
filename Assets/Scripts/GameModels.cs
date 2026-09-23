using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaseballTyping
{
    public enum PitchKind
    {
        Fastball,
        Curveball,
        Changeup,
        FlamingKnuckleball
    }

    public enum RoundState
    {
        Menu,
        Countdown,
        Playing,
        Stunned,
        GameOver
    }

    public readonly struct PitchSpec
    {
        public readonly PitchKind Kind;
        public readonly float TravelSeconds;
        public readonly int BasePoints;
        public readonly Color BallColor;

        public PitchSpec(PitchKind kind, float travelSeconds, int basePoints, Color ballColor)
        {
            Kind = kind;
            TravelSeconds = travelSeconds;
            BasePoints = basePoints;
            BallColor = ballColor;
        }
    }

    public static class GameRules
    {
        public const float RoundSeconds = 60f;
        public const float StrikeoutSeconds = 2f;
        public const int StrikesForOut = 3;
        public const int MaxActivePitches = 4;

        public static float MultiplierForStreak(int errorFreeHits)
        {
            if (errorFreeHits >= 20) return 3f;
            if (errorFreeHits >= 15) return 2.5f;
            if (errorFreeHits >= 10) return 2f;
            if (errorFreeHits >= 5) return 1.5f;
            return 1f;
        }

        public static int ScoreForHit(int basePoints, float distanceRemaining01, float multiplier)
        {
            int distanceBonus = Mathf.RoundToInt(Mathf.Clamp01(distanceRemaining01) * 100f);
            return Mathf.RoundToInt((basePoints + distanceBonus) * multiplier);
        }

        public static PitchSpec SpecFor(PitchKind kind, float difficulty01)
        {
            float speedScale = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01(difficulty01));
            switch (kind)
            {
                case PitchKind.Fastball:
                    return new PitchSpec(kind, 3.25f * speedScale, 100, new Color(0.95f, 0.96f, 1f));
                case PitchKind.Curveball:
                    return new PitchSpec(kind, 4.5f * speedScale, 100, new Color(0.55f, 0.85f, 1f));
                case PitchKind.Changeup:
                    return new PitchSpec(kind, 5.1f * speedScale, 150, new Color(1f, 0.83f, 0.35f));
                default:
                    return new PitchSpec(kind, 7.2f * speedScale, 500, new Color(1f, 0.28f, 0.08f));
            }
        }
    }

    public static class WordBank
    {
        private static readonly Dictionary<PitchKind, string[]> Words = new Dictionary<PitchKind, string[]>
        {
            {
                PitchKind.Fastball,
                new[] { "bro", "run", "hit", "ace", "dash", "base", "ball", "safe", "team", "play" ,"satulod" , "ryushi" , "dwin" }
            },
            {
                PitchKind.Curveball,
                new[] { "curve", "slider", "inning", "catcher", "diamond", "dugout", "rally", "pitch" }
            },
            {
                PitchKind.Changeup,s
                new[] { "double", "success", "rhythm", "misspell", "address", "balloon", "vacuum", "parallel" }
            },
            {
                PitchKind.FlamingKnuckleball,
                new[] { "knuckleball", "championship", "extraordinary", "scoreboard", "unpredictable", "grandstand" , "UltraBeggarNeggar" }
            }
        };

        public static string RandomWord(PitchKind kind)
        {
            string[] pool = Words[kind];
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        public static IReadOnlyList<string> GetWords(PitchKind kind)
        {
            return Words[kind];
        }
    }

    public sealed class ScoreTracker
    {
        public int Score { get; private set; }
        public int ErrorFreeHits { get; private set; }
        public float Multiplier => GameRules.MultiplierForStreak(ErrorFreeHits);

        public int RegisterHit(PitchSpec spec, float distanceRemaining01)
        {
            float earnedMultiplier = GameRules.MultiplierForStreak(ErrorFreeHits + 1);
            int awarded = GameRules.ScoreForHit(spec.BasePoints, distanceRemaining01, earnedMultiplier);
            Score += awarded;
            ErrorFreeHits++;
            return awarded;
        }

        public void RegisterTypo()
        {
            ErrorFreeHits = 0;
        }

        public void Reset()
        {
            Score = 0;
            ErrorFreeHits = 0;
        }
    }

    public static class TargetSelector
    {
        public static int SelectNearestMatchingFirst(
            IReadOnlyList<string> words,
            IReadOnlyList<float> progress,
            char firstCharacter)
        {
            int bestIndex = -1;
            float greatestProgress = -1f;
            int count = Mathf.Min(words.Count, progress.Count);
            for (int i = 0; i < count; i++)
            {
                string word = words[i];
                if (string.IsNullOrEmpty(word) || char.ToLowerInvariant(word[0]) != char.ToLowerInvariant(firstCharacter))
                {
                    continue;
                }

                if (progress[i] > greatestProgress)
                {
                    bestIndex = i;
                    greatestProgress = progress[i];
                }
            }

            return bestIndex;
        }
    }

    public static class RoundTransitions
    {
        public static RoundState AfterStrike(int strikes)
        {
            return strikes >= GameRules.StrikesForOut ? RoundState.Stunned : RoundState.Playing;
        }

        public static RoundState AfterStun(float secondsRemaining)
        {
            return secondsRemaining <= 0f ? RoundState.Playing : RoundState.Stunned;
        }

        public static RoundState AfterTimer(float secondsRemaining)
        {
            return secondsRemaining <= 0f ? RoundState.GameOver : RoundState.Playing;
        }
    }
}
