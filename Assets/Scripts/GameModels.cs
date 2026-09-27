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
        Boss,
        GameOver
    }

    public enum PowerUpKind
    {
        RosinBag,
        CorkedBat,
        GoldenGlove,
        DoublePlay,
        BaseStealer
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
        public const float StrikeoutSeconds = 2f;
        public const int StrikesForOut = 3;
        public const float JuicedChance = 0.10f;
        public const float RosinDuration = 5f;
        public const float RosinTimeScale = 0.35f;
        public const float CorkedDuration = 10f;
        public const int CorkedAutoLetters = 2;
        public const float FrenzyDuration = 5f;
        public const float JuggernautTravelSeconds = 20f;
        public const int GrandSlamBase = 2000;

        public static int MaxActivePitches(int level)
        {
            return Mathf.Clamp(4 + (level - 1) / 2, 4, 6);
        }

        public static float DifficultyForLevel(int level)
        {
            return Mathf.Clamp01((level - 1) / 4f);
        }

        public static int BossThreshold(int level)
        {
            if (level <= 1) return 1000;
            if (level == 2) return 2500;
            if (level == 3) return 4500;
            if (level == 4) return 7000;
            return 7000 + (level - 4) * 3000;
        }

        public static int PointsToBoss(int score, int level)
        {
            return Mathf.Max(0, BossThreshold(level) - score);
        }

        public static int JuggernautLetterTarget(int level)
        {
            return 80 + Mathf.Max(0, level - 1) * 20;
        }

        public static int GrandSlamPoints(int level)
        {
            return GrandSlamBase + Mathf.Max(0, level - 1) * 500;
        }

        public static bool ShouldJuice(float roll)
        {
            return roll < JuicedChance;
        }

        public static bool CorkedCompletes(string word, string typed, bool corkedActive)
        {
            if (!corkedActive || string.IsNullOrEmpty(word) || string.IsNullOrEmpty(typed)) return false;
            if (!word.StartsWith(typed, StringComparison.Ordinal)) return false;
            return word.Length - typed.Length <= CorkedAutoLetters;
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
    }

    public static class WordBank
    {
        private static readonly Dictionary<PitchKind, string[]> Words = new Dictionary<PitchKind, string[]>
        {
            {
                PitchKind.Fastball,
                new[] { "bro", "run", "hit", "ace", "dash", "base", "ball", "safe", "team", "play", "satulod", "ryushi", "dwin" }
            },
            {
                PitchKind.Curveball,
                new[] { "curve", "slider", "inning", "catcher", "diamond", "dugout", "rally", "pitch" }
            },
            {
                PitchKind.Changeup,
                new[] { "double", "success", "rhythm", "misspell", "address", "balloon", "vacuum", "parallel" }
            },
            {
                PitchKind.FlamingKnuckleball,
                new[] { "knuckleball", "championship", "extraordinary", "scoreboard", "unpredictable", "grandstand", "UltraBeggarNeggar" }
            }
        };

        public static readonly string[] StealerWords =
        {
            "go", "up", "on", "in", "at", "to", "it", "we", "he", "run"
        };

        public static string RandomWord(PitchKind kind)
        {
            string[] pool = Words[kind];
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        public static string RandomStealerWord()
        {
            return StealerWords[UnityEngine.Random.Range(0, StealerWords.Length)];
        }

        public static IReadOnlyList<string> GetWords(PitchKind kind)
        {
            return Words[kind];
        }

        public static List<string> BuildJuggernautWords(int letterTarget)
        {
            var list = new List<string>();
            int total = 0;
            var kinds = (PitchKind[])Enum.GetValues(typeof(PitchKind));
            int guard = 0;
            while (total < letterTarget && guard < 200)
            {
                PitchKind kind = kinds[UnityEngine.Random.Range(0, kinds.Length)];
                string word = RandomWord(kind).ToLowerInvariant();
                list.Add(word);
                total += word.Length;
                guard++;
            }

            return list;
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

        public int AddBonus(int points)
        {
            int awarded = Mathf.Max(0, points);
            Score += awarded;
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

        public static RoundState AfterStrikeoutStun(float secondsRemaining)
        {
            return secondsRemaining <= 0f ? RoundState.GameOver : RoundState.Stunned;
        }
    }

    public sealed class JuggernautStream
    {
        private readonly Queue<string> upcoming = new Queue<string>();

        public int MaxHp { get; }
        public int Hp { get; private set; }
        public string CurrentWord { get; private set; }
        public string Typed { get; private set; }
        public bool Complete => Hp <= 0;

        public JuggernautStream(IEnumerable<string> words)
        {
            MaxHp = 0;
            foreach (string raw in words)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                string word = raw.ToLowerInvariant();
                upcoming.Enqueue(word);
                MaxHp += word.Length;
            }

            Hp = MaxHp;
            Typed = string.Empty;
            CurrentWord = upcoming.Count > 0 ? upcoming.Dequeue() : string.Empty;
        }

        public string Preview
        {
            get
            {
                if (upcoming.Count == 0) return string.Empty;
                string first = upcoming.Peek();
                if (upcoming.Count == 1) return first;
                string[] rest = upcoming.ToArray();
                return first + "  " + rest[1];
            }
        }

        public bool TypeLetter(char character)
        {
            if (Complete || string.IsNullOrEmpty(CurrentWord)) return false;

            char letter = char.ToLowerInvariant(character);
            string next = Typed + letter;
            if (!CurrentWord.StartsWith(next, StringComparison.Ordinal))
            {
                Typed = next;
                return false;
            }

            Typed = next;
            Hp = Mathf.Max(0, Hp - 1);
            if (Typed == CurrentWord)
            {
                Typed = string.Empty;
                CurrentWord = upcoming.Count > 0 ? upcoming.Dequeue() : string.Empty;
            }

            return true;
        }

        public void CheckSwing()
        {
            int valid = ValidPrefixLength(CurrentWord, Typed);
            Hp = Mathf.Min(MaxHp, Hp + valid);
            Typed = string.Empty;
        }

        public static int ValidPrefixLength(string word, string typed)
        {
            if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(typed)) return 0;
            int max = Mathf.Min(word.Length, typed.Length);
            int index = 0;
            while (index < max && word[index] == typed[index]) index++;
            return index;
        }
    }

    public static class PowerUpNames
    {
        public static string Display(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.RosinBag: return "ROSIN BAG";
                case PowerUpKind.CorkedBat: return "CORKED BAT";
                case PowerUpKind.GoldenGlove: return "GOLDEN GLOVE";
                case PowerUpKind.DoublePlay: return "DOUBLE PLAY";
                case PowerUpKind.BaseStealer: return "BASE STEALER";
                default: return kind.ToString();
            }
        }
    }
}
