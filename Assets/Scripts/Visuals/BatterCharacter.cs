using UnityEngine;

namespace BaseballTyping.Visuals
{
    /// <summary>
    /// The player's batter: idle bob, wind-up as a pitch closes in, a swing on contact,
    /// and a slumped strikeout pose while the player is stunned.
    /// </summary>
    public sealed class BatterCharacter : MonoBehaviour
    {
        private enum Pose
        {
            Idle,
            WindUp,
            Swing,
            Strikeout
        }

        private const float IdleBatAngle = 58f;
        private const float WindUpBatAngle = 88f;
        private const float SwingEndAngle = -104f;
        private const float ApproachSeconds = 0.12f;
        private const float SwingSeconds = 0.22f;
        private const float RecoverSeconds = 0.28f;

        private SpriteRenderer body;
        private SpriteRenderer bat;
        private Sprite[] idleFrames;
        private Vector3 basePosition;
        private Vector3 swingFrom;
        private Vector3 swingTarget;
        private Pose pose = Pose.Idle;
        private float poseTime;
        private float swingTime;
        private float bobTime;
        private bool strikeoutAfterSwing;

        public static BatterCharacter Build()
        {
            var root = new GameObject("Batter");
            root.transform.position = new Vector3(Ballpark.BatterX, Ballpark.DirtTopY + 0.62f, 0f);

            var batter = root.AddComponent<BatterCharacter>();
            batter.Construct();
            return batter;
        }

        private void Construct()
        {
            basePosition = transform.position;
            idleFrames = new[]
            {
                KenneyArt.Sprite(KenneyArt.World.BatterA),
                KenneyArt.Sprite(KenneyArt.World.BatterB)
            };

            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(transform, false);
            body = bodyObject.AddComponent<SpriteRenderer>();
            body.sprite = idleFrames[0];
            body.sortingOrder = Ballpark.Layer.Batter;
            body.transform.localScale = new Vector3(1.45f, 1.45f, 1f);

            // The pivot sits at the batter's hands so the bat swings from the grip.
            var batPivot = new GameObject("BatPivot");
            batPivot.transform.SetParent(transform, false);
            batPivot.transform.localPosition = new Vector3(0.2f, -0.18f, 0f);

            var batObject = new GameObject("Bat");
            batObject.transform.SetParent(batPivot.transform, false);
            bat = batObject.AddComponent<SpriteRenderer>();
            bat.sprite = KenneyArt.Sprite(KenneyArt.Props.Bat);
            bat.sortingOrder = Ballpark.Layer.Bat;
            batObject.transform.localScale = Vector3.one * 0.78f;
            batObject.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            batPivot.transform.localRotation = Quaternion.Euler(0f, 0f, IdleBatAngle);
        }

        public void ResetPose()
        {
            pose = Pose.Idle;
            poseTime = 0f;
            swingTime = 0f;
            strikeoutAfterSwing = false;
            body.color = Color.white;
            transform.position = basePosition;
            transform.localRotation = Quaternion.identity;
        }

        public void SetWindUp(bool active)
        {
            if (pose == Pose.Swing || pose == Pose.Strikeout) return;
            pose = active ? Pose.WindUp : Pose.Idle;
        }

        public void Swing()
        {
            SwingAt(basePosition + new Vector3(0.85f, 0.15f, 0f));
        }

        /// <summary>
        /// Steps toward the pitch, swings through it, then returns to the batter's box.
        /// </summary>
        public void SwingAt(Vector3 ballWorldPosition, bool strikeoutAfter = false)
        {
            if (pose == Pose.Strikeout && !strikeoutAfter) return;

            pose = Pose.Swing;
            swingTime = 0f;
            swingFrom = transform.position;
            swingTarget = ApproachPoint(ballWorldPosition);
            strikeoutAfterSwing = strikeoutAfter;
            body.color = Color.white;
            transform.localRotation = Quaternion.identity;
        }

        public void Strikeout(float seconds)
        {
            pose = Pose.Strikeout;
            poseTime = seconds;
            body.color = new Color(0.72f, 0.72f, 0.78f);
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            bobTime += delta;

            switch (pose)
            {
                case Pose.Strikeout:
                    poseTime -= delta;
                    transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
                    transform.position = basePosition + new Vector3(-0.12f, -0.12f, 0f);
                    BatAngle(IdleBatAngle - 70f);
                    if (poseTime <= 0f) ResetPose();
                    return;

                case Pose.Swing:
                    swingTime += delta;
                    UpdateSwing();
                    return;

                case Pose.WindUp:
                    Bob(6.5f, 0.035f);
                    BatAngle(Mathf.LerpAngle(CurrentBatAngle(), WindUpBatAngle, delta * 12f));
                    return;

                default:
                    Bob(3.4f, 0.025f);
                    BatAngle(Mathf.LerpAngle(CurrentBatAngle(), IdleBatAngle, delta * 8f));
                    return;
            }
        }

        private void UpdateSwing()
        {
            body.sprite = idleFrames[1];
            transform.localRotation = Quaternion.identity;

            if (swingTime <= ApproachSeconds)
            {
                float t = EaseOut(Mathf.Clamp01(swingTime / ApproachSeconds));
                transform.position = Vector3.Lerp(swingFrom, swingTarget, t);
                BatAngle(Mathf.LerpAngle(CurrentBatAngle(), WindUpBatAngle, t));
                return;
            }

            float swingElapsed = swingTime - ApproachSeconds;
            if (swingElapsed <= SwingSeconds)
            {
                float t = EaseOut(Mathf.Clamp01(swingElapsed / SwingSeconds));
                transform.position = swingTarget;
                BatAngle(Mathf.Lerp(WindUpBatAngle, SwingEndAngle, t));
                return;
            }

            float recover = Mathf.Clamp01((swingElapsed - SwingSeconds) / RecoverSeconds);
            float recoverEase = recover * recover;
            BatAngle(Mathf.Lerp(SwingEndAngle, IdleBatAngle, recoverEase));
            transform.position = Vector3.Lerp(swingTarget, basePosition, recoverEase);

            if (recover >= 1f)
            {
                transform.position = basePosition;
                if (strikeoutAfterSwing)
                {
                    strikeoutAfterSwing = false;
                    Strikeout(1.4f);
                    return;
                }

                pose = Pose.Idle;
                body.sprite = idleFrames[0];
            }
        }

        private Vector3 ApproachPoint(Vector3 ballWorldPosition)
        {
            Vector3 target = ballWorldPosition + new Vector3(-0.55f, -0.12f, 0f);
            target.x = Mathf.Clamp(target.x, basePosition.x + 0.15f, Ballpark.PlateX + 1.15f);
            target.y = Mathf.Clamp(target.y, Ballpark.DirtTopY + 0.35f, Ballpark.LaneMaxY + 0.35f);
            target.z = 0f;
            return target;
        }

        private static float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private void Bob(float speed, float amount)
        {
            int frame = SpriteAnimator.FrameAt(bobTime, speed * 0.5f, idleFrames.Length, true);
            body.sprite = idleFrames[frame];
            transform.position = basePosition + new Vector3(0f, Mathf.Sin(bobTime * speed) * amount, 0f);
            transform.localRotation = Quaternion.identity;
        }

        private float CurrentBatAngle()
        {
            return bat.transform.parent.localEulerAngles.z;
        }

        private void BatAngle(float angle)
        {
            bat.transform.parent.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
