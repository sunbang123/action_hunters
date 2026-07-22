namespace ActionHunters.Runtime
{
    public enum DemoTutorialStep
    {
        Move,
        BasicAttack,
        Skill,
        HuntMonster,
        HireHunter,
        SwitchHunter,
        Complete
    }

    public enum DemoTutorialSignal
    {
        Moved,
        BasicAttackHit,
        SkillUsed,
        MonsterDefeated,
        HunterHired,
        HunterSwitched
    }

    public sealed class DemoTutorialFlow
    {
        private const float RequiredMoveDistance = 1.5f;
        private float _movedDistance;

        public DemoTutorialStep CurrentStep { get; private set; }
        public int CompletedStepCount => (int)CurrentStep;
        public int StepCount => (int)DemoTutorialStep.Complete;
        public float StepProgress => CurrentStep == DemoTutorialStep.Move
            ? UnityEngine.Mathf.Clamp01(_movedDistance / RequiredMoveDistance)
            : CurrentStep == DemoTutorialStep.Complete ? 1f : 0f;

        public void Reset()
        {
            CurrentStep = DemoTutorialStep.Move;
            _movedDistance = 0f;
        }

        public bool Report(DemoTutorialSignal signal, float amount = 1f)
        {
            if (CurrentStep == DemoTutorialStep.Complete)
            {
                return false;
            }

            if (CurrentStep == DemoTutorialStep.Move)
            {
                if (signal != DemoTutorialSignal.Moved || amount <= 0f)
                {
                    return false;
                }

                _movedDistance += amount;
                if (_movedDistance < RequiredMoveDistance)
                {
                    return false;
                }

                Advance();
                return true;
            }

            var expectedSignal = CurrentStep switch
            {
                DemoTutorialStep.BasicAttack => DemoTutorialSignal.BasicAttackHit,
                DemoTutorialStep.Skill => DemoTutorialSignal.SkillUsed,
                DemoTutorialStep.HuntMonster => DemoTutorialSignal.MonsterDefeated,
                DemoTutorialStep.HireHunter => DemoTutorialSignal.HunterHired,
                DemoTutorialStep.SwitchHunter => DemoTutorialSignal.HunterSwitched,
                _ => signal
            };

            if (signal != expectedSignal)
            {
                return false;
            }

            Advance();
            return true;
        }

        public static string GetTitle(DemoTutorialStep step)
        {
            return step switch
            {
                DemoTutorialStep.Move => "1 / 6   MOVE",
                DemoTutorialStep.BasicAttack => "2 / 6   BASIC ATTACK",
                DemoTutorialStep.Skill => "3 / 6   ROLE SKILL",
                DemoTutorialStep.HuntMonster => "4 / 6   EARN GOLD",
                DemoTutorialStep.HireHunter => "5 / 6   HIRE A HUNTER",
                DemoTutorialStep.SwitchHunter => "6 / 6   SWITCH CONTROL",
                _ => "READY FOR THE HUNT"
            };
        }

        public static string GetInstruction(DemoTutorialStep step)
        {
            return step switch
            {
                DemoTutorialStep.Move => "Move 1.5m with WASD or the left stick.",
                DemoTutorialStep.BasicAttack => "Approach a monster and hit it with LMB or RT.",
                DemoTutorialStep.Skill => "Use the Guardian shockwave with SPACE or RB.",
                DemoTutorialStep.HuntMonster => "Defeat a neutral monster. One kill raises 30G to the 60G hire cost.",
                DemoTutorialStep.HireHunter => "Return to the BLUE base and press E or gamepad A.",
                DemoTutorialStep.SwitchHunter => "Press 2 / TAB or D-pad to control the new hunter.",
                _ => "Defeat enemy hunters for 10 points. Hold the lead when the timer reaches zero."
            };
        }

        private void Advance()
        {
            CurrentStep = (DemoTutorialStep)((int)CurrentStep + 1);
        }
    }
}
