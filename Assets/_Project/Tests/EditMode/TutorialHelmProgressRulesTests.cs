using G10.Prototype.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace G10.Prototype.Tests
{
    public sealed class TutorialHelmProgressRulesTests
    {
        [Test] public void OldTutorialJsonLoadsWithZeroAdditiveProgressAndRetainsKnowledge()
        {
            var state=JsonUtility.FromJson<TutorialProgressState>("{\"version\":1,\"completedSteps\":[\"Intro\"],\"presentedSteps\":[\"Helm\"]}");
            state.Normalize();Assert.That(state.Has(TutorialStepId.Intro),Is.True);Assert.That(state.Presented(TutorialStepId.Helm),Is.True);
            Assert.That(state.helmDistance,Is.Zero);Assert.That(state.helmTurn,Is.Zero);Assert.That(state.helmDepth,Is.Zero);
        }
        [Test] public void PartialHelmJsonRoundTripRetainsAllActualAxes()
        {
            var state=new TutorialProgressState {helmDistance=12.5f,helmTurn=8,helmDepth=3};
            var restored=JsonUtility.FromJson<TutorialProgressState>(JsonUtility.ToJson(state));restored.Normalize();
            Assert.That(restored.helmDistance,Is.EqualTo(12.5f));Assert.That(restored.helmTurn,Is.EqualTo(8));Assert.That(restored.helmDepth,Is.EqualTo(3));
        }
        [Test] public void InvalidPartialProgressNormalizesSafely()
        {
            var state=new TutorialProgressState {helmDistance=float.NaN,helmTurn=float.PositiveInfinity,helmDepth=-4};
            state.Normalize();Assert.That(state.helmDistance,Is.Zero);Assert.That(state.helmTurn,Is.Zero);Assert.That(state.helmDepth,Is.Zero);
        }
    }
}
