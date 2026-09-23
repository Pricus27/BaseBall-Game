using System.Collections.Generic;
using BaseballTyping.Visuals;
using UnityEngine;

namespace BaseballTyping
{
    /// <summary>
    /// A single pitch in flight: a Kenney baseball sprite travelling from the machine to the
    /// plate, with a spin, a fading trail, and flames on the boss pitch.
    /// </summary>
    public sealed class PitchController : MonoBehaviour
    {
        private const int TrailLength = 5;

        private BaseballGame game;
        private SpriteRenderer ball;
        private SpriteRenderer flame;
        private readonly List<SpriteRenderer> trail = new List<SpriteRenderer>();
        private readonly Queue<Vector3> trailPositions = new Queue<Vector3>();

        private PitchSpec spec;
        private string word;
        private float elapsed;
        private float laneY;
        private float seed;
        private float trailTimer;
        private bool resolved;
        private int typedCount;

        public PitchKind Kind => spec.Kind;
        public string Word => word;
        public PitchSpec Spec => spec;
        public int TypedCount => typedCount;
        public float DistanceRemaining01 => 1f - Progress;
        public float Progress => PitchTrajectory.Progress(spec.Kind, NormalizedTime);
        public Vector3 LabelAnchor => transform.position + new Vector3(0f, 0.62f, 0f);

        private float NormalizedTime => Mathf.Clamp01(elapsed / spec.TravelSeconds);

        public void Initialize(BaseballGame owner, PitchSpec pitchSpec, string pitchWord, float lane)
        {
            game = owner;
            spec = pitchSpec;
            word = pitchWord.ToLowerInvariant();
            laneY = Ballpark.ClampLane(lane);
            seed = Random.Range(0f, 100f);

            if (spec.Kind == PitchKind.FlamingKnuckleball)
            {
                var flameObject = new GameObject("Flame");
                flameObject.transform.SetParent(transform, false);
                flame = flameObject.AddComponent<SpriteRenderer>();
                flame.sprite = KenneyArt.Sprite(KenneyArt.Props.Baseball);
                flame.sortingOrder = Ballpark.Layer.Ball - 2;
                flame.color = new Color(1f, 0.38f, 0.08f, 0.55f);
                flame.transform.localScale = Vector3.one * 1.45f;
            }

            for (int i = 0; i < TrailLength; i++)
            {
                var ghostObject = new GameObject("Trail" + i);
                ghostObject.transform.SetParent(transform.parent, false);
                var ghost = ghostObject.AddComponent<SpriteRenderer>();
                ghost.sprite = KenneyArt.Sprite(KenneyArt.Props.Baseball);
                ghost.sortingOrder = Ballpark.Layer.Ball - 3;
                ghost.color = Color.clear;
                trail.Add(ghost);
            }

            var ballObject = new GameObject("Ball");
            ballObject.transform.SetParent(transform, false);
            ball = ballObject.AddComponent<SpriteRenderer>();
            ball.sprite = KenneyArt.Sprite(KenneyArt.Props.Baseball);
            ball.sortingOrder = Ballpark.Layer.Ball;
            ball.color = spec.BallColor;

            UpdateTransform();
        }

        private void Update()
        {
            if (resolved || game == null || !game.PitchesMove) return;

            elapsed += Time.deltaTime;
            UpdateTransform();
            UpdateTrail();

            if (NormalizedTime >= 1f)
            {
                resolved = true;
                game.RegisterMiss(this);
            }
        }

        private void UpdateTransform()
        {
            float progress = Progress;
            float x = PitchTrajectory.HorizontalAt(progress) + PitchTrajectory.HorizontalWobble(spec.Kind, elapsed, seed);
            float y = laneY + PitchTrajectory.VerticalOffset(spec.Kind, progress, elapsed, seed);

            transform.position = new Vector3(x, y, 0f);
            transform.localScale = Vector3.one * PitchTrajectory.ScaleAt(progress);

            // Spin direction mirrors travel so the stitching reads as backspin.
            ball.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * -520f);

            if (flame != null)
            {
                flame.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * 300f);
                float pulse = 1.3f + Mathf.Sin(elapsed * 14f) * 0.18f;
                flame.transform.localScale = Vector3.one * pulse;
            }
        }

        private void UpdateTrail()
        {
            trailTimer += Time.deltaTime;
            if (trailTimer < 0.035f) return;
            trailTimer = 0f;

            trailPositions.Enqueue(transform.position);
            while (trailPositions.Count > TrailLength) trailPositions.Dequeue();

            int index = 0;
            foreach (Vector3 position in trailPositions)
            {
                SpriteRenderer ghost = trail[index];
                float fade = (index + 1f) / TrailLength;
                ghost.transform.position = position;
                ghost.transform.localScale = Vector3.one * (transform.localScale.x * fade * 0.7f);
                ghost.color = new Color(spec.BallColor.r, spec.BallColor.g, spec.BallColor.b, fade * 0.22f);
                index++;
            }
        }

        public void SetTypedPrefix(int count)
        {
            typedCount = Mathf.Clamp(count, 0, word.Length);
            ball.color = typedCount > 0
                ? Color.Lerp(spec.BallColor, new Color(0.42f, 1f, 0.55f), 0.5f)
                : spec.BallColor;
        }

        public void MarkResolved()
        {
            resolved = true;
        }

        /// <summary>Text shown above the ball, hiding a letter mid-flight on curveballs.</summary>
        public string DisplayWord()
        {
            if (spec.Kind == PitchKind.Curveball && NormalizedTime > 0.38f && NormalizedTime < 0.62f && typedCount < word.Length)
            {
                int hidden = Mathf.Clamp(word.Length / 2, typedCount, word.Length - 1);
                return word.Substring(0, hidden) + "?" + word.Substring(hidden + 1);
            }

            return word;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < trail.Count; i++)
            {
                if (trail[i] != null) Destroy(trail[i].gameObject);
            }
        }
    }
}
