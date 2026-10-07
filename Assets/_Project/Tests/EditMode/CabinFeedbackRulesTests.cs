using G10.Prototype.Atmosphere;
using G10.Prototype.Core;
using G10.Prototype.Feedback;
using NUnit.Framework;

namespace G10.Prototype.Tests
{
    public sealed class CabinFeedbackRulesTests
    {
        [TestCase(.51f, 0)] [TestCase(.50f, 0)] [TestCase(.25f, .15f)] [TestCase(.10f, .25f)] [TestCase(0, .25f)]
        public void HullThresholds(float ratio, float alpha) => Assert.That(CabinFeedbackRules.HullAlpha(ratio), Is.EqualTo(alpha));
        [TestCase(.21f, 0)] [TestCase(.20f, .15f)] [TestCase(.10f, .22f)] [TestCase(0, .28f)]
        public void EnergyThresholds(float ratio, float alpha) => Assert.That(CabinFeedbackRules.EnergyAlpha(ratio), Is.EqualTo(alpha));
        [TestCase(1, 1, false, CabinMood.Normal)] [TestCase(1, 1, true, CabinMood.ScanActive)]
        [TestCase(.25f, 1, true, CabinMood.Danger)] [TestCase(1, .2f, true, CabinMood.LowPower)]
        [TestCase(.08f, .09f, true, CabinMood.Danger)]
        public void MoodPriority(float hull, float energy, bool scan, CabinMood mood) => Assert.That(CabinFeedbackRules.Mood(hull, energy, scan), Is.EqualTo(mood));
        [TestCase(0, 0)] [TestCase(1, 0)] [TestCase(2, 1)] [TestCase(3, 2)] [TestCase(4, 3)] [TestCase(20, 3)]
        public void CaptureEscalationIsBounded(int captures, int tier) => Assert.That(CabinFeedbackRules.CaptureTier(captures), Is.EqualTo(tier));
        [TestCase(4, 1.2f, 1)] [TestCase(3, 1.45f, 1)] [TestCase(2, 1.45f, 1.06f)]
        [TestCase(1, 1.45f, 1.12f)] [TestCase(0, 1.45f, 1)]
        public void DeadlineTensionRetainsTextAndSinglePulse(int days, float duration, float pulse)
        {
            Assert.That(SceneFlowController.FormatDayLeft(days), Is.EqualTo("DAY LEFT: " + days));
            Assert.That(SceneFlowController.DayLeftHoldSeconds(days, 1.2f), Is.EqualTo(duration).Within(.0001f));
            Assert.That(SceneFlowController.DayLeftPulseScale(days, .5f), Is.EqualTo(pulse).Within(.0001f));
            Assert.That(SceneFlowController.DayLeftPulseScale(days, 0), Is.EqualTo(1));
            Assert.That(SceneFlowController.DayLeftPulseScale(days, 1), Is.EqualTo(1).Within(.0001f));
        }
    }
}
