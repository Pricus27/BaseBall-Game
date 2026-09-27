using System.Collections;
using System.Collections.Generic;
using BaseballTyping.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballTyping
{
    public sealed class BaseballGame : MonoBehaviour
    {
        private static readonly Color SkyColor = new Color(0.31f, 0.65f, 0.86f);

        private readonly List<PitchController> activePitches = new List<PitchController>();
        private readonly ScoreTracker score = new ScoreTracker();

        private Camera pixelCamera;
        private Transform pitchRoot;
        private BatterCharacter batter;
        private PitchingMachine machine;
        private WordLabelLayer wordLabels;
        private AudioHub audioHub;

        private Canvas canvas;
        private GameObject menuPanel;
        private GameObject hudPanel;
        private GameObject inputBar;
        private GameObject resultsPanel;
        private GameObject bossHud;
        private Text timerText;
        private Text scoreText;
        private Text comboText;
        private Text powerText;
        private Text inputText;
        private Text bannerText;
        private Text resultText;
        private Text finalScoreText;
        private Text volumeText;
        private Text bossWordText;
        private Text bossPreviewText;
        private Image[] strikeDots;
        private Image chalkOverlay;
        private Image bossHpFill;
        private RectTransform comboFill;
        private RectTransform bossWordRect;
        private RectTransform bossHpRect;

        private RoundState state = RoundState.Menu;
        private PitchController lockedPitch;
        private JuggernautPitch juggernaut;
        private JuggernautStream juggernautStream;
        private string currentInput = string.Empty;
        private float countdownRemaining;
        private float stunRemaining;
        private float spawnRemaining;
        private float transientBannerRemaining;
        private float shakeRemaining;
        private float corkedRemaining;
        private float rosinRemaining;
        private float frenzyRemaining;
        private int strikes;
        private int pitchesHit;
        private int homeRuns;
        private int level = 1;
        private bool goldenGlove;
        private bool awaitingBoss;

        public bool PitchesMove =>
            state == RoundState.Playing || state == RoundState.Stunned || state == RoundState.Boss;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGame()
        {
            if (FindFirstObjectByType<BaseballGame>() != null) return;
            var root = new GameObject("BaseballTypingGame");
            root.AddComponent<BaseballGame>();
        }

        private void Awake()
        {
            if (FindObjectsByType<BaseballGame>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            audioHub = gameObject.AddComponent<AudioHub>();
            BuildWorld();
            BuildInterface();
            ShowMenu();
        }

        private void OnDestroy()
        {
            RestoreTimeScale();
        }

        private void Update()
        {
            UpdateCameraShake();
            TickPowerUps(Time.unscaledDeltaTime);

            if (transientBannerRemaining > 0f)
            {
                transientBannerRemaining -= Time.unscaledDeltaTime;
                if (transientBannerRemaining <= 0f && (state == RoundState.Playing || state == RoundState.Boss))
                {
                    bannerText.text = string.Empty;
                }
            }

            if (state == RoundState.Countdown)
            {
                UpdateCountdown();
                return;
            }

            if (state == RoundState.Boss)
            {
                ProcessTyping();
                UpdateBatterAnticipation();
                RefreshHud();
                return;
            }

            if (state != RoundState.Playing && state != RoundState.Stunned) return;

            if (state == RoundState.Stunned)
            {
                stunRemaining -= Time.unscaledDeltaTime;
                bannerText.text = "STRIKEOUT!\n" + Mathf.Max(0f, stunRemaining).ToString("0.0") + "s";
                if (stunRemaining <= 0f)
                {
                    EndRound();
                    return;
                }
            }
            else
            {
                if (!awaitingBoss && frenzyRemaining <= 0f && score.Score >= GameRules.BossThreshold(level))
                {
                    BeginApproachToBoss();
                }

                if (awaitingBoss)
                {
                    if (activePitches.Count == 0)
                    {
                        BeginBoss();
                        RefreshHud();
                        return;
                    }
                }
                else
                {
                    TrySpawn();
                }

                ProcessTyping();
                UpdateBatterAnticipation();
            }

            RefreshHud();
        }

        private void LateUpdate()
        {
            RefreshBossHudFollow();
        }

        private void TrySpawn()
        {
            if (awaitingBoss || state != RoundState.Playing) return;

            spawnRemaining -= Time.deltaTime;
            int cap = frenzyRemaining > 0f
                ? GameRules.MaxActivePitches(level) + 2
                : GameRules.MaxActivePitches(level);
            if (spawnRemaining <= 0f && activePitches.Count < cap)
            {
                SpawnPitch();
            }
        }

        private void UpdateCountdown()
        {
            countdownRemaining -= Time.unscaledDeltaTime;
            bannerText.text = countdownRemaining > 0f
                ? Mathf.CeilToInt(countdownRemaining).ToString()
                : "PLAY BALL!";

            if (countdownRemaining <= -0.55f)
            {
                state = RoundState.Playing;
                bannerText.text = string.Empty;
                spawnRemaining = 0.15f;
                ClearTypingTarget();
            }
        }

        public void RegisterMiss(PitchController pitch)
        {
            if (!activePitches.Remove(pitch)) return;

            Vector3 ballPosition = pitch.transform.position;
            if (lockedPitch == pitch) ClearTypingTarget();
            wordLabels.Release(pitch);
            SpawnBurst(ballPosition, new Color(0.85f, 0.3f, 0.25f), 6);
            Destroy(pitch.gameObject);

            if (state != RoundState.Playing) return;

            if (goldenGlove)
            {
                goldenGlove = false;
                audioHub.PlayHit(false);
                ShowBanner("GOLDEN GLOVE!  shield used", 0.9f);
                RefreshHud();
                return;
            }

            strikes++;
            audioHub.PlayStrike();
            ShowBanner("STRIKE " + strikes + "!", 0.7f);

            bool strikeout = strikes >= GameRules.StrikesForOut;
            batter.SwingAt(ballPosition, strikeout);

            if (strikeout)
            {
                state = RoundState.Stunned;
                stunRemaining = GameRules.StrikeoutSeconds;
                currentInput = string.Empty;
                lockedPitch = null;
            }

            RefreshHud();
        }

        public void RegisterJuggernautCrash()
        {
            if (state != RoundState.Boss) return;
            CleanupJuggernaut();
            strikes = GameRules.StrikesForOut;
            batter.SwingAt(new Vector3(Ballpark.PlateX, 0.2f, 0f), true);
            audioHub.PlayStrike();
            ShowBanner("JUGGERNAUT SCORES!", 1.2f);
            EndRound();
        }

        private void StartRound()
        {
            RestoreTimeScale();
            ClearAllPitches();
            CleanupJuggernaut();
            score.Reset();
            strikes = 0;
            pitchesHit = 0;
            homeRuns = 0;
            level = 1;
            awaitingBoss = false;
            goldenGlove = false;
            corkedRemaining = 0f;
            rosinRemaining = 0f;
            frenzyRemaining = 0f;
            countdownRemaining = 3f;
            stunRemaining = 0f;
            transientBannerRemaining = 0f;
            currentInput = string.Empty;
            lockedPitch = null;
            state = RoundState.Countdown;

            machine.Restore();
            batter.ResetPose();
            menuPanel.SetActive(false);
            resultsPanel.SetActive(false);
            hudPanel.SetActive(true);
            inputBar.SetActive(true);
            bossHud.SetActive(false);
            bannerText.text = "3";
            inputText.text = "get ready...";
            RefreshHud();
        }

        private void EndRound()
        {
            RestoreTimeScale();
            state = RoundState.GameOver;
            ClearAllPitches();
            CleanupJuggernaut();
            machine.Restore();
            batter.ResetPose();
            bannerText.text = string.Empty;
            hudPanel.SetActive(false);
            inputBar.SetActive(false);
            bossHud.SetActive(false);

            int best = Mathf.Max(score.Score, PlayerPrefs.GetInt("BaseballTyping.BestScore", 0));
            PlayerPrefs.SetInt("BaseballTyping.BestScore", best);
            PlayerPrefs.Save();

            finalScoreText.text = score.Score.ToString("N0");
            resultText.text =
                "level  " + level + "\n" +
                "hits  " + pitchesHit + "\n" +
                "home runs  " + homeRuns + "\n" +
                "best  " + best.ToString("N0");
            resultsPanel.SetActive(true);
        }

        private void ShowMenu()
        {
            RestoreTimeScale();
            state = RoundState.Menu;
            ClearAllPitches();
            CleanupJuggernaut();
            machine.Restore();
            batter.ResetPose();
            hudPanel.SetActive(false);
            inputBar.SetActive(false);
            resultsPanel.SetActive(false);
            bossHud.SetActive(false);
            menuPanel.SetActive(true);
            bannerText.text = string.Empty;
            UpdateVolumeLabel();
        }

        private void BeginApproachToBoss()
        {
            awaitingBoss = true;
            frenzyRemaining = 0f;
            machine.Retract();
            ShowBanner("BULLPEN CLEARING...", 1.1f);
        }

        private void BeginBoss()
        {
            awaitingBoss = false;
            ClearAllPitches();
            ClearPowerUpsKeepingGlove();
            state = RoundState.Boss;
            machine.Retract();
            juggernautStream = new JuggernautStream(WordBank.BuildJuggernautWords(GameRules.JuggernautLetterTarget(level)));

            var go = new GameObject("Juggernaut");
            go.transform.SetParent(pitchRoot, false);
            juggernaut = go.AddComponent<JuggernautPitch>();
            juggernaut.Initialize(this, GameRules.JuggernautTravelSeconds);
            machine.Restore();
            machine.Fire();
            machine.Retract();
            audioHub.PlayPitch();
            shakeRemaining = 0.4f;
            bossHud.SetActive(true);
            currentInput = string.Empty;
            lockedPitch = null;
            ShowBanner("JUGGERNAUT PITCH!", 1.3f);
            RefreshBossPrompt();
        }

        private void WinBoss()
        {
            Vector3 from = juggernaut != null ? juggernaut.transform.position : Vector3.zero;
            int bonus = score.AddBonus(GameRules.GrandSlamPoints(level));
            homeRuns++;
            pitchesHit++;
            batter.SwingAt(from);
            audioHub.PlayHit(true);
            SpawnBurst(from, new Color(1f, 0.84f, 0.2f), 16);
            LaunchHitBall(from, true);
            shakeRemaining = 0.45f;
            CleanupJuggernaut();
            ClearAllPitches();

            level++;
            strikes = 0;
            awaitingBoss = false;
            machine.Restore();
            state = RoundState.Playing;
            spawnRemaining = 1.35f;
            bossHud.SetActive(false);
            ShowBanner("GRAND SLAM!  +" + bonus.ToString("N0"), 1.4f);
            ClearTypingTarget();

            if (score.Score >= GameRules.BossThreshold(level))
            {
                BeginApproachToBoss();
            }
        }

        private void SpawnPitch()
        {
            float difficulty = GameRules.DifficultyForLevel(level);
            bool frenzy = frenzyRemaining > 0f;
            PitchKind kind = frenzy ? PitchKind.Fastball : ChoosePitchKind(difficulty);
            PitchSpec spec = frenzy
                ? new PitchSpec(PitchKind.Fastball, 2.15f, 80, new Color(0.75f, 1f, 0.7f))
                : GameRules.SpecFor(kind, difficulty);
            string word = frenzy ? WordBank.RandomStealerWord() : WordBank.RandomWord(kind);
            bool juiced = !frenzy && GameRules.ShouldJuice(Random.value);
            float lane = Random.Range(Ballpark.LaneMinY, Ballpark.LaneMaxY);

            var go = new GameObject(kind + "_" + word);
            go.transform.SetParent(pitchRoot, false);
            var pitch = go.AddComponent<PitchController>();
            pitch.Initialize(this, spec, word, lane, juiced);

            activePitches.Add(pitch);
            wordLabels.Track(pitch);
            machine.Fire();
            audioHub.PlayPitch();

            spawnRemaining = frenzy
                ? Random.Range(0.28f, 0.48f)
                : Random.Range(
                    Mathf.Lerp(1.65f, 0.68f, difficulty),
                    Mathf.Lerp(2.15f, 1.05f, difficulty));
        }

        private static PitchKind ChoosePitchKind(float difficulty)
        {
            float roll = Random.value;
            float bossChance = difficulty > 0.35f ? Mathf.Lerp(0.03f, 0.11f, difficulty) : 0f;
            if (roll < bossChance) return PitchKind.FlamingKnuckleball;
            if (roll < bossChance + Mathf.Lerp(0.1f, 0.27f, difficulty)) return PitchKind.Changeup;
            if (roll < bossChance + Mathf.Lerp(0.3f, 0.48f, difficulty)) return PitchKind.Curveball;
            return PitchKind.Fastball;
        }

        private void UpdateBatterAnticipation()
        {
            bool incoming = state == RoundState.Boss;
            for (int i = 0; i < activePitches.Count; i++)
            {
                if (activePitches[i].Progress > 0.55f)
                {
                    incoming = true;
                    break;
                }
            }

            batter.SetWindUp(incoming || lockedPitch != null);
        }

        private void ProcessTyping()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CheckSwing();
                return;
            }

            string input = Input.inputString;
            bool inputIncludedBackspace = false;

            foreach (char raw in input)
            {
                if (raw == '\b')
                {
                    inputIncludedBackspace = true;
                    Backspace();
                }
                else if (char.IsLetter(raw))
                {
                    if (state == RoundState.Boss) TypeBossCharacter(char.ToLowerInvariant(raw));
                    else TypeCharacter(char.ToLowerInvariant(raw));
                }
            }

            if (!inputIncludedBackspace && Input.GetKeyDown(KeyCode.Backspace))
            {
                Backspace();
            }
        }

        private void CheckSwing()
        {
            if (state == RoundState.Boss && juggernautStream != null)
            {
                juggernautStream.CheckSwing();
                currentInput = string.Empty;
                RefreshBossPrompt();
                ShowBanner("CHECK SWING", 0.35f);
                return;
            }

            if (lockedPitch != null) lockedPitch.SetTypedPrefix(0);
            ClearTypingTarget();
            ShowBanner("CHECK SWING", 0.35f);
        }

        private void TypeCharacter(char character)
        {
            if (lockedPitch == null)
            {
                lockedPitch = FindBestTarget(character);
                if (lockedPitch == null)
                {
                    RegisterFoul(character.ToString());
                    return;
                }
            }

            currentInput += character;
            if (!lockedPitch.Word.StartsWith(currentInput))
            {
                score.RegisterTypo();
                audioHub.PlayFoul();
                inputText.text = "<color=#FF7B6B>" + currentInput + "</color>";
                ShowBanner("FOUL! combo reset", 0.6f);
                RefreshHud();
                return;
            }

            lockedPitch.SetTypedPrefix(currentInput.Length);
            if (GameRules.CorkedCompletes(lockedPitch.Word, currentInput, corkedRemaining > 0f))
            {
                currentInput = lockedPitch.Word;
                lockedPitch.SetTypedPrefix(currentInput.Length);
            }

            inputText.text = "<color=#7CFF9B>" + currentInput + "</color>" +
                             lockedPitch.Word.Substring(currentInput.Length);

            if (currentInput == lockedPitch.Word)
            {
                RegisterHit(lockedPitch);
            }
        }

        private void TypeBossCharacter(char character)
        {
            if (juggernautStream == null || juggernautStream.Complete) return;

            string wordBefore = juggernautStream.CurrentWord;
            bool ok = juggernautStream.TypeLetter(character);
            if (!ok)
            {
                score.RegisterTypo();
                audioHub.PlayFoul();
                ShowBanner("FOUL! combo reset", 0.6f);
                RefreshBossPrompt();
                RefreshHud();
                return;
            }

            if (corkedRemaining > 0f &&
                GameRules.CorkedCompletes(wordBefore, juggernautStream.Typed, true) &&
                juggernautStream.CurrentWord == wordBefore)
            {
                while (!juggernautStream.Complete &&
                       juggernautStream.CurrentWord == wordBefore &&
                       juggernautStream.Typed.Length < wordBefore.Length)
                {
                    juggernautStream.TypeLetter(wordBefore[juggernautStream.Typed.Length]);
                }
            }

            RefreshBossPrompt();
            if (juggernautStream.Complete)
            {
                WinBoss();
            }
        }

        private void Backspace()
        {
            if (state == RoundState.Boss && juggernautStream != null)
            {
                juggernautStream.CheckSwing();
                currentInput = string.Empty;
                RefreshBossPrompt();
                return;
            }

            if (currentInput.Length == 0) return;
            currentInput = currentInput.Substring(0, currentInput.Length - 1);

            if (lockedPitch == null)
            {
                inputText.text = currentInput;
                return;
            }

            int validCount = JuggernautStream.ValidPrefixLength(lockedPitch.Word, currentInput);
            lockedPitch.SetTypedPrefix(validCount);
            inputText.text = lockedPitch.Word.StartsWith(currentInput)
                ? "<color=#7CFF9B>" + currentInput + "</color>" + lockedPitch.Word.Substring(currentInput.Length)
                : "<color=#FF7B6B>" + currentInput + "</color>";
        }

        private PitchController FindBestTarget(char firstCharacter)
        {
            var words = new List<string>(activePitches.Count);
            var progress = new List<float>(activePitches.Count);
            for (int i = 0; i < activePitches.Count; i++)
            {
                words.Add(activePitches[i].Word);
                progress.Add(activePitches[i].Progress);
            }

            int index = TargetSelector.SelectNearestMatchingFirst(words, progress, firstCharacter);
            return index >= 0 ? activePitches[index] : null;
        }

        private void RegisterHit(PitchController pitch)
        {
            bool juiced = pitch.IsJuiced;
            Vector3 hitFrom = pitch.transform.position;
            PitchSpec spec = pitch.Spec;
            float distance = pitch.DistanceRemaining01;
            bool homeRun = pitch.Kind == PitchKind.FlamingKnuckleball;

            pitch.MarkResolved();
            activePitches.Remove(pitch);
            wordLabels.Release(pitch);

            int points = score.RegisterHit(spec, distance);
            pitchesHit++;
            if (homeRun) homeRuns++;

            batter.SwingAt(hitFrom);
            audioHub.PlayHit(homeRun);
            SpawnBurst(hitFrom, homeRun ? new Color(1f, 0.55f, 0.15f) : Color.white, homeRun ? 14 : 8);
            LaunchHitBall(hitFrom, homeRun);
            Destroy(pitch.gameObject);

            if (juiced)
            {
                GrantRandomPowerUp(hitFrom);
            }

            if (homeRun)
            {
                shakeRemaining = 0.35f;
                ClearAllPitches();
                ShowBanner("HOME RUN!  +" + points.ToString("N0"), 1.2f);
            }
            else
            {
                string result = distance > 0.55f ? "DOUBLE!" : "SINGLE!";
                ShowBanner(result + "  +" + points.ToString("N0"), 0.7f);
                ClearTypingTarget();
            }

            RefreshHud();
        }

        private void GrantRandomPowerUp(Vector3 from)
        {
            var kind = (PowerUpKind)Random.Range(0, 5);
            switch (kind)
            {
                case PowerUpKind.RosinBag:
                    rosinRemaining = GameRules.RosinDuration;
                    Time.timeScale = GameRules.RosinTimeScale;
                    ShowBanner(PowerUpNames.Display(kind) + "  time slow", 1.1f);
                    break;
                case PowerUpKind.CorkedBat:
                    corkedRemaining = GameRules.CorkedDuration;
                    ShowBanner(PowerUpNames.Display(kind) + "  auto-complete", 1.1f);
                    break;
                case PowerUpKind.GoldenGlove:
                    goldenGlove = true;
                    ShowBanner(PowerUpNames.Display(kind) + "  shield ready", 1.1f);
                    break;
                case PowerUpKind.DoublePlay:
                    ResolveDoublePlay(from);
                    ShowBanner(PowerUpNames.Display(kind) + "!", 1.1f);
                    break;
                default:
                    frenzyRemaining = GameRules.FrenzyDuration;
                    spawnRemaining = 0.05f;
                    ShowBanner(PowerUpNames.Display(PowerUpKind.BaseStealer) + "  frenzy!", 1.1f);
                    break;
            }
        }

        private void ResolveDoublePlay(Vector3 from)
        {
            int extras = Random.Range(2, 4);
            var victims = new List<PitchController>();
            for (int i = 0; i < activePitches.Count && victims.Count < extras; i++)
            {
                victims.Add(activePitches[i]);
            }

            LaunchHitBall(from, false);
            for (int i = 0; i < victims.Count; i++)
            {
                PitchController extra = victims[i];
                if (extra == null) continue;
                extra.MarkResolved();
                activePitches.Remove(extra);
                wordLabels.Release(extra);
                int extraPoints = score.RegisterHit(extra.Spec, extra.DistanceRemaining01);
                pitchesHit++;
                SpawnBurst(extra.transform.position, new Color(0.7f, 1f, 0.75f), 8);
                LaunchHitBall(extra.transform.position, false);
                Destroy(extra.gameObject);
                ShowBanner("DOUBLE PLAY  +" + extraPoints.ToString("N0"), 0.8f);
            }
        }

        private void RegisterFoul(string typed)
        {
            score.RegisterTypo();
            audioHub.PlayFoul();
            inputText.text = "<color=#FF7B6B>" + typed + "</color>";
            ShowBanner("FOUL! combo reset", 0.6f);
            RefreshHud();
        }

        private void ClearTypingTarget()
        {
            lockedPitch = null;
            currentInput = string.Empty;
            if (inputText != null) inputText.text = "type a word";
        }

        private void ClearAllPitches()
        {
            for (int i = 0; i < activePitches.Count; i++)
            {
                if (activePitches[i] == null) continue;
                activePitches[i].MarkResolved();
                wordLabels.Release(activePitches[i]);
                Destroy(activePitches[i].gameObject);
            }

            activePitches.Clear();
            wordLabels.ReleaseAll();
            ClearTypingTarget();
        }

        private void CleanupJuggernaut()
        {
            if (juggernaut != null)
            {
                juggernaut.MarkResolved();
                Destroy(juggernaut.gameObject);
                juggernaut = null;
            }

            juggernautStream = null;
        }

        private void TickPowerUps(float unscaledDt)
        {
            if (corkedRemaining > 0f)
            {
                corkedRemaining = Mathf.Max(0f, corkedRemaining - unscaledDt);
            }

            if (rosinRemaining > 0f)
            {
                rosinRemaining = Mathf.Max(0f, rosinRemaining - unscaledDt);
                if (rosinRemaining <= 0f) RestoreTimeScale();
            }

            if (frenzyRemaining > 0f)
            {
                frenzyRemaining = Mathf.Max(0f, frenzyRemaining - unscaledDt);
            }

            if (chalkOverlay != null)
            {
                float target = rosinRemaining > 0f ? 0.22f : 0f;
                Color color = chalkOverlay.color;
                color.a = Mathf.MoveTowards(color.a, target, unscaledDt * 1.4f);
                chalkOverlay.color = color;
            }
        }

        private void ClearPowerUpsKeepingGlove()
        {
            corkedRemaining = 0f;
            frenzyRemaining = 0f;
            rosinRemaining = 0f;
            RestoreTimeScale();
        }

        private void RestoreTimeScale()
        {
            Time.timeScale = 1f;
            rosinRemaining = 0f;
        }

        private void RefreshHud()
        {
            if (timerText == null) return;

            if (state == RoundState.Boss)
            {
                timerText.text = "BOSS";
            }
            else
            {
                int remain = GameRules.PointsToBoss(score.Score, level);
                timerText.text = "LV " + level + "\n" + remain;
            }

            scoreText.text = score.Score.ToString("N0");
            comboText.text = "x" + score.Multiplier.ToString("0.0");
            if (powerText != null) powerText.text = PowerStatusText();

            for (int i = 0; i < strikeDots.Length; i++)
            {
                bool used = i < strikes;
                strikeDots[i].sprite = KenneyArt.Sprite(used ? KenneyArt.Ui.DotRed : KenneyArt.Ui.DotEmpty);
                strikeDots[i].color = used ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }

            float progressToNextTier = (score.ErrorFreeHits % 5) / 5f;
            if (score.Multiplier >= 3f) progressToNextTier = 1f;
            comboFill.anchorMax = new Vector2(Mathf.Clamp01(progressToNextTier), 1f);

            if (bossHud != null && juggernautStream != null && state == RoundState.Boss)
            {
                float hp = juggernautStream.MaxHp <= 0 ? 0f : (float)juggernautStream.Hp / juggernautStream.MaxHp;
                bossHpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(hp), 1f);
            }
        }

        private string PowerStatusText()
        {
            if (rosinRemaining > 0f) return "ROSIN " + rosinRemaining.ToString("0.0");
            if (corkedRemaining > 0f) return "CORKED " + corkedRemaining.ToString("0.0");
            if (frenzyRemaining > 0f) return "FRENZY " + frenzyRemaining.ToString("0.0");
            if (goldenGlove) return "GLOVE READY";
            return string.Empty;
        }

        private void RefreshBossPrompt()
        {
            if (juggernautStream == null || bossWordText == null) return;

            string word = juggernautStream.CurrentWord;
            string typed = juggernautStream.Typed;
            int valid = JuggernautStream.ValidPrefixLength(word, typed);
            if (valid > 0 && valid == typed.Length)
            {
                bossWordText.text = "<color=#7CFF9B>" + typed + "</color>" + word.Substring(typed.Length);
            }
            else if (typed.Length > 0)
            {
                bossWordText.text = "<color=#FF7B6B>" + typed + "</color>";
            }
            else
            {
                bossWordText.text = word;
            }

            bossPreviewText.text = juggernautStream.Preview;
            inputText.text = bossWordText.text;
        }

        private void RefreshBossHudFollow()
        {
            if (state != RoundState.Boss || juggernaut == null || canvas == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            PlaceUiAtWorld(bossWordRect, canvasRect, juggernaut.LabelAnchor);
            PlaceUiAtWorld(bossHpRect, canvasRect, juggernaut.HpAnchor);
        }

        private void PlaceUiAtWorld(RectTransform rect, RectTransform canvasRect, Vector3 world)
        {
            if (rect == null) return;
            Vector2 screenPoint = pixelCamera.WorldToScreenPoint(world);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 local))
            {
                rect.anchoredPosition = local;
            }
        }

        private void ShowBanner(string message, float seconds)
        {
            bannerText.text = message;
            transientBannerRemaining = seconds;
        }

        private void UpdateCameraShake()
        {
            if (shakeRemaining <= 0f)
            {
                pixelCamera.transform.position = new Vector3(0f, 0f, -10f);
                return;
            }

            shakeRemaining -= Time.unscaledDeltaTime;
            float magnitude = Mathf.Max(0f, shakeRemaining) * 0.35f;
            pixelCamera.transform.position = new Vector3(
                Random.Range(-magnitude, magnitude),
                Random.Range(-magnitude, magnitude),
                -10f);
        }

        private void LaunchHitBall(Vector3 from, bool homeRun)
        {
            StartCoroutine(HitBallRoutine(from, homeRun));
        }

        private IEnumerator HitBallRoutine(Vector3 from, bool homeRun)
        {
            var go = new GameObject("HitBall");
            go.transform.position = from;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = KenneyArt.Sprite(KenneyArt.Props.Baseball);
            renderer.sortingOrder = Ballpark.Layer.Ball + 1;

            Vector3 velocity = homeRun
                ? new Vector3(11f, 9.5f, 0f)
                : new Vector3(9f, 6.2f, 0f);

            float life = 0f;
            while (life < 1.2f)
            {
                life += Time.unscaledDeltaTime;
                float dt = Time.unscaledDeltaTime;
                velocity.y -= 9f * dt;
                go.transform.position += velocity * dt;
                go.transform.Rotate(0f, 0f, -720f * dt);
                if (go.transform.position.x > 12f) break;
                yield return null;
            }

            Destroy(go);
        }

        private void SpawnBurst(Vector3 position, Color color, int count)
        {
            StartCoroutine(BurstRoutine(position, color, count));
        }

        private IEnumerator BurstRoutine(Vector3 position, Color color, int count)
        {
            var pieces = new Transform[count];
            var directions = new Vector2[count];
            var renderers = new SpriteRenderer[count];

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Spark");
                go.transform.position = position;
                go.transform.localScale = Vector3.one * 0.16f;
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = KenneyArt.WhitePixel;
                renderer.color = color;
                renderer.sortingOrder = Ballpark.Layer.Particles;

                pieces[i] = go.transform;
                renderers[i] = renderer;
                float angle = i * Mathf.PI * 2f / count + Random.Range(-0.2f, 0.2f);
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            float elapsed = 0f;
            const float duration = 0.42f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                for (int i = 0; i < count; i++)
                {
                    if (pieces[i] == null) continue;
                    pieces[i].position = position + (Vector3)(directions[i] * (elapsed * 6.5f));
                    pieces[i].localScale = Vector3.one * (0.16f * (1f - t));
                    renderers[i].color = new Color(color.r, color.g, color.b, 1f - t);
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                if (pieces[i] != null) Destroy(pieces[i].gameObject);
            }
        }

        private void BuildWorld()
        {
            pixelCamera = PixelCamera.Create(SkyColor);
            PixelStage.Build(pixelCamera);
            batter = BatterCharacter.Build();
            machine = PitchingMachine.Build();

            var pitches = new GameObject("Pitches");
            pitchRoot = pitches.transform;
        }

        private void BuildInterface()
        {
            canvas = PixelUi.CreateCanvas();
            wordLabels = WordLabelLayer.Build(canvas.transform, pixelCamera);

            chalkOverlay = PixelUi.Region(canvas.transform, "Chalk", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            chalkOverlay.color = new Color(0.95f, 0.95f, 0.9f, 0f);
            chalkOverlay.raycastTarget = false;

            BuildHud();
            BuildBossHud();
            BuildMenu();
            BuildResults();

            bannerText = PixelUi.ShadowLabel(canvas.transform, "Banner", string.Empty, 44, new Color(1f, 0.86f, 0.32f),
                TextAnchor.MiddleCenter, new Vector2(0.15f, 0.55f), new Vector2(0.85f, 0.78f), true);
            bannerText.transform.SetAsLastSibling();
        }

        private void BuildHud()
        {
            hudPanel = PixelUi.Panel(canvas.transform, "HUD", KenneyArt.Ui.PanelBolted,
                new Vector2(0.012f, 0.855f), new Vector2(0.988f, 0.988f)).gameObject;

            var timeRegion = PixelUi.Region(hudPanel.transform, "TimeRegion", new Vector2(0.025f, 0.12f), new Vector2(0.2f, 0.88f));
            timerText = PixelUi.ShadowLabel(timeRegion, "Timer", "LV 1", 22, new Color(1f, 0.98f, 0.9f),
                TextAnchor.MiddleLeft, Vector2.zero, Vector2.one);

            var scorePlate = PixelUi.Panel(hudPanel.transform, "ScorePlate", KenneyArt.Ui.PanelDark,
                new Vector2(0.22f, 0.12f), new Vector2(0.45f, 0.88f));
            scoreText = PixelUi.ShadowLabel(scorePlate.transform, "Score", "0", 30, new Color(1f, 0.84f, 0.33f),
                TextAnchor.MiddleCenter, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), true);

            var strikeRegion = PixelUi.Region(hudPanel.transform, "StrikeRegion", new Vector2(0.47f, 0.1f), new Vector2(0.65f, 0.9f));
            strikeDots = new Image[GameRules.StrikesForOut];
            for (int i = 0; i < strikeDots.Length; i++)
            {
                strikeDots[i] = PixelUi.Sprite(strikeRegion, "Strike" + i, KenneyArt.Ui.DotEmpty,
                    new Vector2(-54f + i * 54f, 2f), new Vector2(48f, 48f), false);
            }

            var comboRegion = PixelUi.Region(hudPanel.transform, "ComboRegion", new Vector2(0.67f, 0.18f), new Vector2(0.88f, 0.82f));
            var comboFrame = PixelUi.Panel(comboRegion, "ComboFrame", KenneyArt.Ui.BarFrame, Vector2.zero, Vector2.one);
            var fill = PixelUi.Panel(comboFrame.transform, "ComboFill", KenneyArt.Ui.BarFill,
                new Vector2(0f, 0f), new Vector2(1f, 1f));
            fill.color = new Color(1f, 0.72f, 0.24f);
            comboFill = fill.rectTransform;
            comboFill.offsetMin = new Vector2(9f, 9f);
            comboFill.offsetMax = new Vector2(-9f, -9f);

            var comboTextRegion = PixelUi.Region(hudPanel.transform, "ComboTextRegion", new Vector2(0.885f, 0.12f), new Vector2(0.99f, 0.88f));
            comboText = PixelUi.ShadowLabel(comboTextRegion, "Combo", "x1.0", 24, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

            inputBar = PixelUi.Panel(canvas.transform, "InputBar", KenneyArt.Ui.PanelDark,
                new Vector2(0.24f, 0.028f), new Vector2(0.76f, 0.13f)).gameObject;
            inputText = PixelUi.ShadowLabel(inputBar.transform, "CurrentInput", "type a word", 28, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f));

            powerText = PixelUi.ShadowLabel(canvas.transform, "PowerUp", string.Empty, 18, new Color(1f, 0.92f, 0.55f),
                TextAnchor.MiddleLeft, new Vector2(0.02f, 0.04f), new Vector2(0.22f, 0.12f));
        }

        private void BuildBossHud()
        {
            bossHud = PixelUi.Region(canvas.transform, "BossHud", Vector2.zero, Vector2.one).gameObject;
            bossHud.SetActive(false);

            var hpRoot = new GameObject("BossHp", typeof(RectTransform));
            hpRoot.transform.SetParent(bossHud.transform, false);
            bossHpRect = hpRoot.GetComponent<RectTransform>();
            bossHpRect.anchorMin = bossHpRect.anchorMax = new Vector2(0.5f, 0.5f);
            bossHpRect.sizeDelta = new Vector2(220f, 28f);

            var hpFrame = PixelUi.Panel(hpRoot.transform, "HpFrame", KenneyArt.Ui.BarFrame, Vector2.zero, Vector2.one);
            bossHpFill = PixelUi.Panel(hpFrame.transform, "HpFill", KenneyArt.Ui.BarFill, Vector2.zero, Vector2.one);
            bossHpFill.color = new Color(0.85f, 0.18f, 0.16f);
            bossHpFill.rectTransform.offsetMin = new Vector2(6f, 6f);
            bossHpFill.rectTransform.offsetMax = new Vector2(-6f, -6f);

            var wordRoot = new GameObject("BossWord", typeof(RectTransform));
            wordRoot.transform.SetParent(bossHud.transform, false);
            bossWordRect = wordRoot.GetComponent<RectTransform>();
            bossWordRect.anchorMin = bossWordRect.anchorMax = new Vector2(0.5f, 0.5f);
            bossWordRect.sizeDelta = new Vector2(520f, 70f);

            bossWordText = PixelUi.ShadowLabel(wordRoot.transform, "Word", string.Empty, 32, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0f, 0.35f), new Vector2(1f, 1f));
            bossPreviewText = PixelUi.Label(wordRoot.transform, "Preview", string.Empty, 18, new Color(0.85f, 0.85f, 0.8f),
                TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0.4f));
        }

        private void BuildMenu()
        {
            menuPanel = PixelUi.Region(canvas.transform, "Menu", Vector2.zero, Vector2.one).gameObject;
            var scrim = menuPanel.AddComponent<Image>();
            scrim.color = new Color(0.04f, 0.06f, 0.1f, 0.42f);

            var card = PixelUi.Panel(menuPanel.transform, "Card", KenneyArt.Ui.PanelCream,
                new Vector2(0.16f, 0.08f), new Vector2(0.84f, 0.93f));

            var titleBanner = PixelUi.Sprite(card.transform, "TitleBanner", KenneyArt.Ui.Banner,
                new Vector2(0f, 188f), new Vector2(470f, 116f), false);
            PixelUi.ShadowLabel(titleBanner.transform, "Title", "BASEBALL\nTYPING LEAGUE", 26, new Color(1f, 0.97f, 0.88f),
                TextAnchor.MiddleCenter, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), true);

            PixelUi.Label(card.transform, "Instructions",
                "type each pitch before it reaches the plate\n" +
                "1 or ESC  check swing  (clear typed letters)\n" +
                "juiced pitches glow and grant power-ups\n" +
                "hit score goals to face the juggernaut\n" +
                "three strikes or a crashed boss ends the run",
                18, new Color(0.26f, 0.2f, 0.16f),
                TextAnchor.UpperCenter, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.68f));

            Button play = PixelUi.Button(card.transform, "PlayButton", "PLAY BALL",
                new Vector2(0f, -150f), new Vector2(360f, 70f));
            play.onClick.AddListener(StartRound);

            Button volumeDown = PixelUi.Button(card.transform, "VolumeDown", "-", new Vector2(-140f, -230f), new Vector2(64f, 56f));
            volumeDown.onClick.AddListener(() => { audioHub.AdjustVolume(-0.1f); UpdateVolumeLabel(); });

            volumeText = PixelUi.Label(card.transform, "Volume", "VOLUME 70%", 20, new Color(0.26f, 0.2f, 0.16f),
                TextAnchor.MiddleCenter, new Vector2(0.34f, 0.05f), new Vector2(0.66f, 0.14f));

            Button volumeUp = PixelUi.Button(card.transform, "VolumeUp", "+", new Vector2(140f, -230f), new Vector2(64f, 56f));
            volumeUp.onClick.AddListener(() => { audioHub.AdjustVolume(0.1f); UpdateVolumeLabel(); });
        }

        private void BuildResults()
        {
            resultsPanel = PixelUi.Region(canvas.transform, "Results", Vector2.zero, Vector2.one).gameObject;
            var scrim = resultsPanel.AddComponent<Image>();
            scrim.color = new Color(0.04f, 0.06f, 0.1f, 0.42f);

            var card = PixelUi.Panel(resultsPanel.transform, "Card", KenneyArt.Ui.PanelCream,
                new Vector2(0.26f, 0.14f), new Vector2(0.74f, 0.88f));

            var resultBanner = PixelUi.Sprite(card.transform, "ResultBanner", KenneyArt.Ui.Banner,
                new Vector2(0f, 158f), new Vector2(380f, 104f), false);
            PixelUi.ShadowLabel(resultBanner.transform, "ResultTitle", "GAME OVER", 24, new Color(1f, 0.97f, 0.88f),
                TextAnchor.MiddleCenter, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f), true);

            var scorePlate = PixelUi.Panel(card.transform, "FinalScorePlate", KenneyArt.Ui.PanelDark,
                new Vector2(0.14f, 0.56f), new Vector2(0.86f, 0.73f));
            finalScoreText = PixelUi.ShadowLabel(scorePlate.transform, "FinalScore", "0", 40, new Color(1f, 0.84f, 0.33f),
                TextAnchor.MiddleCenter, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), true);

            resultText = PixelUi.Label(card.transform, "ResultText", string.Empty, 22, new Color(0.26f, 0.2f, 0.16f),
                TextAnchor.UpperCenter, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.54f));

            Button replay = PixelUi.Button(card.transform, "ReplayButton", "PLAY AGAIN", new Vector2(0f, -110f), new Vector2(300f, 66f));
            replay.onClick.AddListener(StartRound);

            Button menu = PixelUi.Button(card.transform, "MenuButton", "MAIN MENU", new Vector2(0f, -190f), new Vector2(300f, 60f));
            menu.onClick.AddListener(ShowMenu);
        }

        private void UpdateVolumeLabel()
        {
            if (volumeText != null) volumeText.text = "VOLUME " + audioHub.VolumePercent + "%";
        }
    }
}
