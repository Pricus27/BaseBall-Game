using BaseballTyping.Visuals;
using UnityEngine;

namespace BaseballTyping
{
    /// <summary>
    /// Slow iron-studded baseball that the player must type down before it reaches the plate.
    /// </summary>
    public sealed class JuggernautPitch : MonoBehaviour
    {
        private BaseballGame game;
        private SpriteRenderer ball;
        private SpriteRenderer ring;
        private SpriteRenderer studA;
        private SpriteRenderer studB;
        private float elapsed;
        private float travelSeconds;
        private bool resolved;

        public float Progress => Mathf.Clamp01(elapsed / travelSeconds);
        public Vector3 LabelAnchor => transform.position + new Vector3(0f, -1.15f, 0f);
        public Vector3 HpAnchor => transform.position + new Vector3(0f, 1.35f, 0f);

        public void Initialize(BaseballGame owner, float seconds)
        {
            game = owner;
            travelSeconds = Mathf.Max(8f, seconds);

            var ringObject = new GameObject("Ring");
            ringObject.transform.SetParent(transform, false);
            ring = ringObject.AddComponent<SpriteRenderer>();
            ring.sprite = KenneyArt.Sprite(KenneyArt.Props.Baseball);
            ring.sortingOrder = Ballpark.Layer.Ball - 1;
            ring.color = new Color(0.22f, 0.22f, 0.26f, 0.9f);
            ring.transform.localScale = Vector3.one * 3.05f;

            var ballObject = new GameObject("Ball");
            ballObject.transform.SetParent(transform, false);
            ball = ballObject.AddComponent<SpriteRenderer>();
            ball.sprite = KenneyArt.Sprite(KenneyArt.Props.Baseball);
            ball.sortingOrder = Ballpark.Layer.Ball;
            ball.color = new Color(0.62f, 0.64f, 0.7f);

            studA = MakeStud("StudA", new Vector3(-0.28f, 0.22f, 0f));
            studB = MakeStud("StudB", new Vector3(0.3f, -0.18f, 0f));

            UpdateTransform();
        }

        private SpriteRenderer MakeStud(string name, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = KenneyArt.WhitePixel;
            renderer.sortingOrder = Ballpark.Layer.Ball + 1;
            renderer.color = new Color(0.85f, 0.82f, 0.7f);
            go.transform.localScale = Vector3.one * 0.18f;
            return renderer;
        }

        private void Update()
        {
            if (resolved || game == null || !game.PitchesMove) return;

            elapsed += Time.deltaTime;
            UpdateTransform();

            if (Progress >= 1f)
            {
                resolved = true;
                game.RegisterJuggernautCrash();
            }
        }

        private void UpdateTransform()
        {
            float x = PitchTrajectory.HorizontalAt(Progress);
            transform.position = new Vector3(x, 0.15f, 0f);
            float grow = Mathf.Lerp(2.35f, 2.9f, Progress);
            transform.localScale = Vector3.one * grow;
            ball.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * -80f);
            if (ring != null) ring.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * 40f);
            if (studA != null) studA.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * 90f);
            if (studB != null) studB.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * -110f);
        }

        public void MarkResolved()
        {
            resolved = true;
        }
    }
}
