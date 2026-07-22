using ActionHunters.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace ActionHunters.Tests
{
    public sealed class DemoGameRulesTests
    {
        [TestCase(60, 60, 1, 4, true)]
        [TestCase(59, 60, 1, 4, false)]
        [TestCase(500, 60, 4, 4, false)]
        public void CanHire_RequiresGoldAndAnOpenSlot(int gold, int cost, int active, int maximum, bool expected)
        {
            Assert.That(DemoGameRules.CanHire(gold, cost, active, maximum), Is.EqualTo(expected));
        }

        [TestCase(10, 0, DemoTeam.Blue)]
        [TestCase(0, 10, DemoTeam.Red)]
        [TestCase(10, 10, DemoTeam.Neutral)]
        public void DetermineWinner_UsesScoreOrReturnsDraw(int blue, int red, DemoTeam expected)
        {
            Assert.That(DemoGameRules.DetermineWinner(blue, red), Is.EqualTo(expected));
        }

        [TestCase(300f, "05:00")]
        [TestCase(0f, "00:00")]
        [TestCase(61.01f, "01:02")]
        public void FormatTime_RoundsUpRemainingSeconds(float seconds, string expected)
        {
            Assert.That(DemoGameRules.FormatTime(seconds), Is.EqualTo(expected));
        }

        [Test]
        public void TutorialFlow_AdvancesOnlyForTheExpectedGameplaySignal()
        {
            var flow = new DemoTutorialFlow();
            flow.Reset();

            Assert.That(flow.Report(DemoTutorialSignal.BasicAttackHit), Is.False);
            Assert.That(flow.CurrentStep, Is.EqualTo(DemoTutorialStep.Move));
            Assert.That(flow.Report(DemoTutorialSignal.Moved, 1f), Is.False);
            Assert.That(flow.Report(DemoTutorialSignal.Moved, 0.5f), Is.False);
            Assert.That(flow.Report(DemoTutorialSignal.Jumped), Is.True);
            Assert.That(flow.CurrentStep, Is.EqualTo(DemoTutorialStep.BasicAttack));
            Assert.That(flow.Report(DemoTutorialSignal.BasicAttackHit), Is.True);
            Assert.That(flow.Report(DemoTutorialSignal.SkillUsed), Is.True);
            Assert.That(flow.Report(DemoTutorialSignal.MonsterDefeated), Is.True);
            Assert.That(flow.Report(DemoTutorialSignal.HunterHired), Is.True);
            Assert.That(flow.Report(DemoTutorialSignal.HunterSwitched), Is.True);
            Assert.That(flow.CurrentStep, Is.EqualTo(DemoTutorialStep.Complete));
        }

        [Test]
        public void TutorialFlow_CompletedStateIgnoresFurtherSignals()
        {
            var flow = new DemoTutorialFlow();
            flow.Reset();
            flow.Report(DemoTutorialSignal.Moved, 2f);
            flow.Report(DemoTutorialSignal.Jumped);
            flow.Report(DemoTutorialSignal.BasicAttackHit);
            flow.Report(DemoTutorialSignal.SkillUsed);
            flow.Report(DemoTutorialSignal.MonsterDefeated);
            flow.Report(DemoTutorialSignal.HunterHired);
            flow.Report(DemoTutorialSignal.HunterSwitched);

            Assert.That(flow.Report(DemoTutorialSignal.Moved, 50f), Is.False);
            Assert.That(flow.CurrentStep, Is.EqualTo(DemoTutorialStep.Complete));
            Assert.That(flow.StepProgress, Is.EqualTo(1f));
        }

        [Test]
        public void DemoDefaults_ProvideJumpAndFlagRules()
        {
            var config = ScriptableObject.CreateInstance<DemoGameConfig>();
            try
            {
                config.ApplyDemoDefaults();
                Assert.That(config.JumpHeight, Is.EqualTo(3.2f));
                Assert.That(config.GravityMultiplier, Is.EqualTo(2.4f));
                Assert.That(config.FlagCaptureScore, Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
