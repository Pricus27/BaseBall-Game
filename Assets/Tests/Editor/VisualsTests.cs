using BaseballTyping.Visuals;
using NUnit.Framework;
using UnityEngine;

namespace BaseballTyping.Tests
{
    public sealed class VisualsTests
    {
        [Test]
        public void FrameAt_LoopsThroughEveryFrame()
        {
            Assert.That(SpriteAnimator.FrameAt(0f, 10f, 3, true), Is.Zero);
            Assert.That(SpriteAnimator.FrameAt(0.15f, 10f, 3, true), Is.EqualTo(1));
            Assert.That(SpriteAnimator.FrameAt(0.25f, 10f, 3, true), Is.EqualTo(2));
            Assert.That(SpriteAnimator.FrameAt(0.35f, 10f, 3, true), Is.Zero);
        }

        [Test]
        public void FrameAt_ClampsWhenNotLooping()
        {
            Assert.That(SpriteAnimator.FrameAt(5f, 10f, 3, false), Is.EqualTo(2));
        }

        [Test]
        public void FrameAt_HandlesEmptyAnimation()
        {
            Assert.That(SpriteAnimator.FrameAt(1f, 10f, 0, true), Is.Zero);
            Assert.That(SpriteAnimator.FrameAt(1f, 0f, 3, true), Is.Zero);
        }

        [Test]
        public void IsComplete_OnlyForOneShotAnimations()
        {
            Assert.That(SpriteAnimator.IsComplete(0.5f, 10f, 3, false), Is.True);
            Assert.That(SpriteAnimator.IsComplete(0.1f, 10f, 3, false), Is.False);
            Assert.That(SpriteAnimator.IsComplete(50f, 10f, 3, true), Is.False);
        }

        [Test]
        public void Progress_IsLinearForNonChangeups()
        {
            Assert.That(PitchTrajectory.Progress(PitchKind.Fastball, 0.4f), Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(PitchTrajectory.Progress(PitchKind.Curveball, 1f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Changeup_StartsFastThenSlowsButStillArrives()
        {
            float early = PitchTrajectory.Progress(PitchKind.Changeup, 0.2f);
            float atBreak = PitchTrajectory.Progress(PitchKind.Changeup, 0.35f);
            float end = PitchTrajectory.Progress(PitchKind.Changeup, 1f);

            Assert.That(early, Is.GreaterThan(0.2f), "changeup should burst ahead early");
            Assert.That(atBreak, Is.EqualTo(0.56f).Within(0.0001f), "piecewise curve must stay continuous");
            Assert.That(end, Is.EqualTo(1f).Within(0.0001f), "changeup must still reach the plate");
        }

        [Test]
        public void Changeup_ProgressNeverMovesBackwards()
        {
            float previous = 0f;
            for (float t = 0f; t <= 1f; t += 0.02f)
            {
                float current = PitchTrajectory.Progress(PitchKind.Changeup, t);
                Assert.That(current, Is.GreaterThanOrEqualTo(previous - 0.0001f));
                previous = current;
            }
        }

        [Test]
        public void HorizontalTravel_RunsFromMachineToPlate()
        {
            Assert.That(PitchTrajectory.HorizontalAt(0f), Is.EqualTo(Ballpark.PitchStartX).Within(0.0001f));
            Assert.That(PitchTrajectory.HorizontalAt(1f), Is.EqualTo(Ballpark.PlateX).Within(0.0001f));
            Assert.That(PitchTrajectory.HorizontalAt(0.5f), Is.LessThan(Ballpark.PitchStartX));
        }

        [Test]
        public void CurveballArcsUpwardAndReturnsToTheLane()
        {
            float middle = PitchTrajectory.VerticalOffset(PitchKind.Curveball, 0.5f, 0.5f, 1f);
            float atPlate = PitchTrajectory.VerticalOffset(PitchKind.Curveball, 1f, 1f, 1f);

            Assert.That(middle, Is.GreaterThan(0.5f));
            Assert.That(atPlate, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void StraightPitchesHaveNoVerticalOffsetOrWobble()
        {
            Assert.That(PitchTrajectory.VerticalOffset(PitchKind.Fastball, 0.5f, 0.5f, 3f), Is.Zero);
            Assert.That(PitchTrajectory.HorizontalWobble(PitchKind.Fastball, 0.5f, 3f), Is.Zero);
            Assert.That(PitchTrajectory.HorizontalWobble(PitchKind.FlamingKnuckleball, 0.5f, 3f), Is.Not.Zero);
        }

        [Test]
        public void Lanes_StayInsideTheHittableBand()
        {
            Assert.That(Ballpark.ClampLane(99f), Is.EqualTo(Ballpark.LaneMaxY));
            Assert.That(Ballpark.ClampLane(-99f), Is.EqualTo(Ballpark.LaneMinY));
            Assert.That(Ballpark.LaneMaxY, Is.LessThan(Ballpark.GrassTopY), "pitches must stay below the outfield wall");
            Assert.That(Ballpark.LaneMinY, Is.GreaterThan(Ballpark.DirtTopY), "pitches must fly above the dirt");
        }

        [Test]
        public void PixelCamera_SizesFromTheReferenceResolution()
        {
            float size = PixelCamera.OrthographicSizeFor(PixelCamera.ReferenceHeight, KenneyArt.PixelsPerUnit);

            Assert.That(size, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(PixelCamera.HorizontalExtent(size, 16f / 9f), Is.EqualTo(8.888f).Within(0.01f));
            Assert.That(PixelCamera.OrthographicSizeFor(180, 0), Is.EqualTo(1f));
        }

        [Test]
        public void BallScale_GrowsAsThePitchApproaches()
        {
            Assert.That(PitchTrajectory.ScaleAt(1f), Is.GreaterThan(PitchTrajectory.ScaleAt(0f)));
        }

        [Test]
        public void SpritePathsResolveToRealResources()
        {
            Assert.That(Resources.Load<Sprite>(KenneyArt.World.BatterA), Is.Not.Null, "batter sprite missing from Resources");
            Assert.That(Resources.Load<Sprite>(KenneyArt.Props.Baseball), Is.Not.Null, "baseball sprite missing from Resources");
            Assert.That(Resources.Load<Sprite>(KenneyArt.Ui.PanelCream), Is.Not.Null, "UI panel sprite missing from Resources");
            Assert.That(Resources.Load<Font>("Fonts/KenneyPixel"), Is.Not.Null, "KenPixel font missing from Resources");
        }
    }
}
