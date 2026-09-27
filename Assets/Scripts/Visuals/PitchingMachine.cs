using UnityEngine;

namespace BaseballTyping.Visuals
{
    /// <summary>
    /// The relentless pitching machine: idles with spinning wheels and recoils on every pitch.
    /// </summary>
    public sealed class PitchingMachine : MonoBehaviour
    {
        private const float RecoilSeconds = 0.22f;

        private SpriteRenderer body;
        private SpriteRenderer muzzleFlash;
        private Sprite[] frames;
        private Vector3 basePosition;
        private Vector3 hiddenPosition;
        private float idleTime;
        private float recoilTime = -1f;
        private bool retracted;

        public static PitchingMachine Build()
        {
            var root = new GameObject("PitchingMachine");
            root.transform.position = new Vector3(Ballpark.MachineX, Ballpark.DirtTopY + 0.66f, 0f);

            var machine = root.AddComponent<PitchingMachine>();
            machine.Construct();
            return machine;
        }

        private void Construct()
        {
            basePosition = transform.position;
            hiddenPosition = basePosition + new Vector3(5.4f, 0.15f, 0f);
            frames = new[]
            {
                KenneyArt.Sprite(KenneyArt.World.MachineA),
                KenneyArt.Sprite(KenneyArt.World.MachineB),
                KenneyArt.Sprite(KenneyArt.World.MachineC)
            };

            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(transform, false);
            body = bodyObject.AddComponent<SpriteRenderer>();
            body.sprite = frames[0];
            body.sortingOrder = Ballpark.Layer.Machine;
            body.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            var flashObject = new GameObject("MuzzleFlash");
            flashObject.transform.SetParent(transform, false);
            flashObject.transform.localPosition = new Vector3(-0.62f, 0.05f, 0f);
            muzzleFlash = flashObject.AddComponent<SpriteRenderer>();
            muzzleFlash.sprite = KenneyArt.WhitePixel;
            muzzleFlash.sortingOrder = Ballpark.Layer.Machine + 1;
            muzzleFlash.color = Color.clear;
            muzzleFlash.transform.localScale = new Vector3(0.28f, 0.16f, 1f);
        }

        public void Fire()
        {
            if (retracted) return;
            recoilTime = 0f;
        }

        public void Retract()
        {
            retracted = true;
            recoilTime = -1f;
        }

        public void Restore()
        {
            retracted = false;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            idleTime += delta;
            body.sprite = frames[SpriteAnimator.FrameAt(idleTime, 7f, frames.Length, true)];

            Vector3 home = retracted ? hiddenPosition : basePosition;
            transform.position = Vector3.Lerp(transform.position, home, 1f - Mathf.Exp(-8f * delta));

            if (retracted || recoilTime < 0f)
            {
                muzzleFlash.color = Color.clear;
                return;
            }

            recoilTime += delta;
            float t = Mathf.Clamp01(recoilTime / RecoilSeconds);
            float kick = Mathf.Sin(t * Mathf.PI) * 0.22f;
            transform.position = home + new Vector3(kick, 0f, 0f);
            muzzleFlash.color = new Color(1f, 0.92f, 0.55f, (1f - t) * 0.85f);

            if (t >= 1f) recoilTime = -1f;
        }
    }
}
