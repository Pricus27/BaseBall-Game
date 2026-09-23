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
        private const float SwingSeconds = 0.26f;
        private const float RecoverSeconds = 0.22f;

        private SpriteRenderer body;
        private SpriteRenderer bat;
        private Sprite[] idleFrames;
        private Vector3 basePosition;
        private Pose pose = Pose.Idle;
        private float poseTime;
        private float swingTime;
        private float bobTime;

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
            if (pose == Pose.Strikeout) return;
            pose = Pose.Swing;
            swingTime = 0f;
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
            if (swingTime <= SwingSeconds)
            {
                float t = Mathf.Clamp01(swingTime / SwingSeconds);
                float eased = 1f - (1f - t) * (1f - t);
                BatAngle(Mathf.Lerp(WindUpBatAngle, SwingEndAngle, eased));
                transform.position = basePosition + new Vector3(0.06f * eased, 0f, 0f);
                body.sprite = idleFrames[1];
                return;
            }

            float recover = Mathf.Clamp01((swingTime - SwingSeconds) / RecoverSeconds);
            BatAngle(Mathf.Lerp(SwingEndAngle, IdleBatAngle, recover));
            transform.position = Vector3.Lerp(basePosition + new Vector3(0.06f, 0f, 0f), basePosition, recover);

            if (recover >= 1f)
            {
                pose = Pose.Idle;
                body.sprite = idleFrames[0];
            }
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
