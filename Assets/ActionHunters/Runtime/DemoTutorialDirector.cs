using UnityEngine;

namespace ActionHunters.Runtime
{
    public sealed class DemoTutorialDirector : MonoBehaviour
    {
        [SerializeField] private DemoHud hud;

        private readonly DemoTutorialFlow _flow = new DemoTutorialFlow();

        public bool IsRulesVisible { get; private set; }
        public DemoTutorialStep CurrentStep => _flow.CurrentStep;

        public void Configure(DemoHud configuredHud)
        {
            hud = configuredHud;
        }

        public void BeginSession()
        {
            _flow.Reset();
            IsRulesVisible = true;
            hud.SetRulesVisible(true);
            RefreshStep();
        }

        public void ToggleRules()
        {
            IsRulesVisible = !IsRulesVisible;
            hud.SetRulesVisible(IsRulesVisible);
        }

        public void DismissRules()
        {
            if (!IsRulesVisible)
            {
                return;
            }

            IsRulesVisible = false;
            hud.SetRulesVisible(false);
            hud.ShowToast("GET READY — THE HUNT STARTS IN 3", new Color(1f, 0.82f, 0.25f));
        }

        public void Report(DemoTutorialSignal signal, float amount = 1f)
        {
            var previousStep = _flow.CurrentStep;
            var advanced = _flow.Report(signal, amount);
            RefreshStep();
            if (!advanced)
            {
                return;
            }

            if (_flow.CurrentStep == DemoTutorialStep.Complete)
            {
                hud.ShowToast("TUTORIAL COMPLETE — SCORE MORE THAN RED", new Color(0.28f, 1f, 0.55f));
            }
            else
            {
                hud.ShowToast($"COMPLETE: {DemoTutorialFlow.GetTitle(previousStep)}", new Color(0.28f, 1f, 0.55f));
            }
        }

        private void RefreshStep()
        {
            hud.SetTutorialStep(
                DemoTutorialFlow.GetTitle(_flow.CurrentStep),
                DemoTutorialFlow.GetInstruction(_flow.CurrentStep),
                _flow.CompletedStepCount,
                _flow.StepCount,
                _flow.StepProgress);
        }
    }
}
