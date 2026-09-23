using System;
using UnityEngine;

namespace BaseballTyping.Visuals
{
    /// <summary>
    /// Frame-stepping animator for the Kenney sprites. The frame math is static so it can be
    /// unit tested without instantiating scene objects.
    /// </summary>
    public sealed class SpriteAnimator : MonoBehaviour
    {
        private SpriteRenderer target;
        private Sprite[] frames;
        private float framesPerSecond = 6f;
        private float elapsed;
        private bool looping = true;
        private bool finished;
        private Action onComplete;

        public bool IsPlaying => !finished;

        public static int FrameAt(float elapsed, float framesPerSecond, int frameCount, bool loop)
        {
            if (frameCount <= 0) return 0;
            if (framesPerSecond <= 0f || elapsed <= 0f) return 0;

            int step = Mathf.FloorToInt(elapsed * framesPerSecond);
            if (loop) return ((step % frameCount) + frameCount) % frameCount;
            return Mathf.Clamp(step, 0, frameCount - 1);
        }

        public static bool IsComplete(float elapsed, float framesPerSecond, int frameCount, bool loop)
        {
            if (loop || frameCount <= 0 || framesPerSecond <= 0f) return false;
            return elapsed * framesPerSecond >= frameCount;
        }

        public void Bind(SpriteRenderer renderer)
        {
            target = renderer;
        }

        public void Play(Sprite[] animationFrames, float fps, bool loop, Action completed = null)
        {
            frames = animationFrames;
            framesPerSecond = fps;
            looping = loop;
            elapsed = 0f;
            finished = false;
            onComplete = completed;
            Apply();
        }

        private void Update()
        {
            if (finished || frames == null || frames.Length == 0) return;

            elapsed += Time.deltaTime;
            Apply();

            if (IsComplete(elapsed, framesPerSecond, frames.Length, looping))
            {
                finished = true;
                Action callback = onComplete;
                onComplete = null;
                callback?.Invoke();
            }
        }

        private void Apply()
        {
            if (target == null || frames == null || frames.Length == 0) return;
            target.sprite = frames[FrameAt(elapsed, framesPerSecond, frames.Length, looping)];
        }
    }
}
