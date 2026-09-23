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
        private Text timerText;
        private Text scoreText;
        private Text comboText;
        private Text inputText;
        private Text bannerText;
        private Text resultText;
        private Text finalScoreText;
        private Text volumeText;
        private Image[] strikeDots;
        private RectTransform comboFill;

        private RoundState state = RoundState.Menu;
        private PitchController lockedPitch;
        private string currentInput = string.Empty;
        private float timeRemaining;
        private float countdownRemaining;
        private float stunRemaining;
        private float spawnRemaining;
        private float transientBannerRemaining;
        private float shakeRemaining;
        private int strikes;
        private int pitchesHit;
        private int homeRuns;

        public bool PitchesMove => state == RoundState.Playing || state == RoundState.Stunned;

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

        private void Update()
        {
            UpdateCameraShake();

            if (transientBannerRemaining > 0f)
            {
                transientBannerRemaining -= Time.deltaTime;
                if (transientBannerRemaining <= 0f && state == RoundState.Playing)
                {
                    bannerText.text = string.Empty;
                }
            }

            if (state == RoundState.Countdown)
            {
                UpdateCountdown();
                return;
            }

            if (state != RoundState.Playing && state != RoundState.Stunned) return;

            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            if (timeRemaining <= 0f)
            {
                EndRound();
                return;
            }

            if (state == RoundState.Stunned)
            {
                stunRemaining -= Time.deltaTime;
                bannerText.text = "STRIKEOUT!\nback in " + Mathf.Max(0f, stunRemaining).ToString("0.0") + "s";
                if (stunRemaining <= 0f)
                {
                    strikes = 0;
                    state = RoundState.Playing;
                    ShowBanner("PLAY BALL!", 0.7f);
                }
            }
            else
            {
                spawnRemaining -= Time.deltaTime;
                if (spawnRemaining <= 0f && activePitches.Count < GameRules.MaxActivePitches)
                {
                    SpawnPitch();
                }

                ProcessTyping();
                UpdateBatterAnticipation();
            }

            RefreshHud();
        }

        private void UpdateCountdown()
        {
            countdownRemaining -= Time.deltaTime;
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

            if (lockedPitch == pitch) ClearTypingTarget();
            wordLabels.Release(pitch);
            SpawnBurst(pitch.transform.position, new Color(0.85f, 0.3f, 0.25f), 6);
            Destroy(pitch.gameObject);

            if (state != RoundState.Playing) return;

            strikes++;
            audioHub.PlayStrike();
            ShowBanner("STRIKE " + strikes + "!", 0.7f);

            if (strikes >= GameRules.StrikesForOut)
            {
                state = RoundState.Stunned;
                stunRemaining = GameRules.StrikeoutSeconds;
                currentInput = string.Empty;
                lockedPitch = null;
                batter.Strikeout(GameRules.StrikeoutSeconds);
            }

            RefreshHud();
        }

        private void StartRound()
        {
            ClearAllPitches();
            score.Reset();
            strikes = 0;
            pitchesHit = 0;
            homeRuns = 0;
            timeRemaining = GameRules.RoundSeconds;
            countdownRemaining = 3f;
            stunRemaining = 0f;
            transientBannerRemaining = 0f;
            currentInput = string.Empty;
            lockedPitch = null;
            state = RoundState.Countdown;

            batter.ResetPose();
            menuPanel.SetActive(false);
            resultsPanel.SetActive(false);
            hudPanel.SetActive(true);
            inputBar.SetActive(true);
            bannerText.text = "3";
            inputText.text = "get ready...";
            RefreshHud();
        }

        private void EndRound()
        {
            state = RoundState.GameOver;
            ClearAllPitches();
            batter.ResetPose();
            bannerText.text = string.Empty;
            hudPanel.SetActive(false);
            inputBar.SetActive(false);

            int best = Mathf.Max(score.Score, PlayerPrefs.GetInt("BaseballTyping.BestScore", 0));
            PlayerPrefs.SetInt("BaseballTyping.BestScore", best);
            PlayerPrefs.Save();

            finalScoreText.text = score.Score.ToString("N0");
            resultText.text =
                "hits  " + pitchesHit + "\n" +
                "home runs  " + homeRuns + "\n" +
                "best  " + best.ToString("N0");
            resultsPanel.SetActive(true);
        }

        private void ShowMenu()
        {
            state = RoundState.Menu;
            ClearAllPitches();
            batter.ResetPose();
            hudPanel.SetActive(false);
            inputBar.SetActive(false);
            resultsPanel.SetActive(false);
            menuPanel.SetActive(true);
            bannerText.text = string.Empty;
            UpdateVolumeLabel();
        }

        private void SpawnPitch()
        {
            float difficulty = 1f - timeRemaining / GameRules.RoundSeconds;
            PitchKind kind = ChoosePitchKind(difficulty);
            PitchSpec spec = GameRules.SpecFor(kind, difficulty);
            string word = WordBank.RandomWord(kind);
            float lane = Random.Range(Ballpark.LaneMinY, Ballpark.LaneMaxY);

            var go = new GameObject(kind + "_" + word);
            go.transform.SetParent(pitchRoot, false);
            var pitch = go.AddComponent<PitchController>();
            pitch.Initialize(this, spec, word, lane);

            activePitches.Add(pitch);
            wordLabels.Track(pitch);
            machine.Fire();
            audioHub.PlayPitch();

            spawnRemaining = Random.Range(
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
            bool incoming = false;
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
                    TypeCharacter(char.ToLowerInvariant(raw));
                }
            }

            if (!inputIncludedBackspace && Input.GetKeyDown(KeyCode.Backspace))
            {
                Backspace();
            }
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
            inputText.text = "<color=#7CFF9B>" + currentInput + "</color>" +
                             lockedPitch.Word.Substring(currentInput.Length);

            if (currentInput == lockedPitch.Word)
            {
                RegisterHit(lockedPitch);
            }
        }

        private void Backspace()
        {
            if (currentInput.Length == 0) return;
            currentInput = currentInput.Substring(0, currentInput.Length - 1);

            if (lockedPitch == null)
            {
                inputText.text = currentInput;
                return;
            }

            int validCount = ValidPrefixLength(lockedPitch.Word, currentInput);
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
            pitch.MarkResolved();
            activePitches.Remove(pitch);
            wordLabels.Release(pitch);

            int points = score.RegisterHit(pitch.Spec, pitch.DistanceRemaining01);
            bool homeRun = pitch.Kind == PitchKind.FlamingKnuckleball;
            pitchesHit++;
            if (homeRun) homeRuns++;

            batter.Swing();
            audioHub.PlayHit(homeRun);
            SpawnBurst(pitch.transform.position, homeRun ? new Color(1f, 0.55f, 0.15f) : Color.white, homeRun ? 14 : 8);
            LaunchHitBall(pitch.transform.position, homeRun);
            Destroy(pitch.gameObject);

            if (homeRun)
            {
                shakeRemaining = 0.35f;
                ClearAllPitches();
                ShowBanner("HOME RUN!  +" + points.ToString("N0"), 1.2f);
            }
            else
            {
                string result = pitch.DistanceRemaining01 > 0.55f ? "DOUBLE!" : "SINGLE!";
                ShowBanner(result + "  +" + points.ToString("N0"), 0.7f);
                ClearTypingTarget();
            }

            RefreshHud();
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
            inputText.text = "type a word";
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

        private void RefreshHud()
        {
            if (timerText == null) return;

            timerText.text = "TIME " + Mathf.CeilToInt(timeRemaining).ToString("00");
            scoreText.text = score.Score.ToString("N0");
            comboText.text = "x" + score.Multiplier.ToString("0.0");

            for (int i = 0; i < strikeDots.Length; i++)
            {
                bool used = i < strikes;
                strikeDots[i].sprite = KenneyArt.Sprite(used ? KenneyArt.Ui.DotRed : KenneyArt.Ui.DotEmpty);
                strikeDots[i].color = used ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }

            float progressToNextTier = (score.ErrorFreeHits % 5) / 5f;
            if (score.Multiplier >= 3f) progressToNextTier = 1f;
            comboFill.anchorMax = new Vector2(Mathf.Clamp01(progressToNextTier), 1f);
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

            shakeRemaining -= Time.deltaTime;
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
                life += Time.deltaTime;
                velocity.y -= 9f * Time.deltaTime;
                go.transform.position += velocity * Time.deltaTime;
                go.transform.Rotate(0f, 0f, -720f * Time.deltaTime);
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
                elapsed += Time.deltaTime;
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

        private static int ValidPrefixLength(string word, string typed)
        {
            int max = Mathf.Min(word.Length, typed.Length);
            int index = 0;
            while (index < max && word[index] == typed[index]) index++;
            return index;
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

            BuildHud();
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

            // Each HUD block owns an anchored region so nothing overlaps at any aspect ratio.
            var timeRegion = PixelUi.Region(hudPanel.transform, "TimeRegion", new Vector2(0.025f, 0.12f), new Vector2(0.2f, 0.88f));
            timerText = PixelUi.ShadowLabel(timeRegion, "Timer", "TIME 60", 26, new Color(1f, 0.98f, 0.9f),
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
        }

        private void BuildMenu()
        {
            menuPanel = PixelUi.Region(canvas.transform, "Menu", Vector2.zero, Vector2.one).gameObject;
            var scrim = menuPanel.AddComponent<Image>();
            scrim.color = new Color(0.04f, 0.06f, 0.1f, 0.42f);

            var card = PixelUi.Panel(menuPanel.transform, "Card", KenneyArt.Ui.PanelCream,
                new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.9f));

            var titleBanner = PixelUi.Sprite(card.transform, "TitleBanner", KenneyArt.Ui.Banner,
                new Vector2(0f, 168f), new Vector2(470f, 116f), false);
            PixelUi.ShadowLabel(titleBanner.transform, "Title", "BASEBALL\nTYPING LEAGUE", 26, new Color(1f, 0.97f, 0.88f),
                TextAnchor.MiddleCenter, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), true);

            PixelUi.Label(card.transform, "Instructions",
                "type each pitch's word before it\nreaches home plate\n\n" +
                "fastball short   curveball hides a letter\nchangeup slows   knuckleball scores 500\n\n" +
                "typos break your combo\nthree strikes stun you for 2 seconds",
                20, new Color(0.26f, 0.2f, 0.16f),
                TextAnchor.UpperCenter, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.7f));

            Button play = PixelUi.Button(card.transform, "PlayButton", "PLAY 60 SECONDS",
                new Vector2(0f, -130f), new Vector2(360f, 70f));
            play.onClick.AddListener(StartRound);

            Button volumeDown = PixelUi.Button(card.transform, "VolumeDown", "-", new Vector2(-140f, -215f), new Vector2(64f, 56f));
            volumeDown.onClick.AddListener(() => { audioHub.AdjustVolume(-0.1f); UpdateVolumeLabel(); });

            volumeText = PixelUi.Label(card.transform, "Volume", "VOLUME 70%", 20, new Color(0.26f, 0.2f, 0.16f),
                TextAnchor.MiddleCenter, new Vector2(0.34f, 0.06f), new Vector2(0.66f, 0.15f));

            Button volumeUp = PixelUi.Button(card.transform, "VolumeUp", "+", new Vector2(140f, -215f), new Vector2(64f, 56f));
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

            resultText = PixelUi.Label(card.transform, "ResultText", string.Empty, 24, new Color(0.26f, 0.2f, 0.16f),
                TextAnchor.UpperCenter, new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.54f));

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
