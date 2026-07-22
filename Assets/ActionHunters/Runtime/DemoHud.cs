using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ActionHunters.Runtime
{
    public sealed class DemoHud : MonoBehaviour
    {
        [SerializeField] private Text blueScore;
        [SerializeField] private Text redScore;
        [SerializeField] private Text roundTimer;
        [SerializeField] private Text modeLabel;
        [SerializeField] private Text objective;
        [SerializeField] private Text help;
        [SerializeField] private List<Text> blueRoster = new List<Text>();
        [SerializeField] private List<Text> redRoster = new List<Text>();
        [Header("Player feedback")]
        [SerializeField] private Text playerRole;
        [SerializeField] private Text playerStats;
        [SerializeField] private Image playerHealthFill;
        [SerializeField] private Image playerSkillFill;
        [SerializeField] private CanvasGroup toastGroup;
        [SerializeField] private Text toastText;
        [Header("Tutorial")]
        [SerializeField] private GameObject tutorialCard;
        [SerializeField] private Text tutorialTitle;
        [SerializeField] private Text tutorialInstruction;
        [SerializeField] private Text tutorialProgress;
        [SerializeField] private Image tutorialProgressFill;
        [SerializeField] private GameObject rulesOverlay;

        private float _toastRemaining;

        public void Configure(
            Text configuredBlueScore,
            Text configuredRedScore,
            Text configuredRoundTimer,
            Text configuredModeLabel,
            Text configuredObjective,
            Text configuredHelp,
            IReadOnlyList<Text> configuredBlueRoster,
            IReadOnlyList<Text> configuredRedRoster,
            Text configuredPlayerRole,
            Text configuredPlayerStats,
            Image configuredPlayerHealthFill,
            Image configuredPlayerSkillFill,
            CanvasGroup configuredToastGroup,
            Text configuredToastText,
            GameObject configuredTutorialCard,
            Text configuredTutorialTitle,
            Text configuredTutorialInstruction,
            Text configuredTutorialProgress,
            Image configuredTutorialProgressFill,
            GameObject configuredRulesOverlay)
        {
            blueScore = configuredBlueScore;
            redScore = configuredRedScore;
            roundTimer = configuredRoundTimer;
            modeLabel = configuredModeLabel;
            objective = configuredObjective;
            help = configuredHelp;
            blueRoster = new List<Text>(configuredBlueRoster);
            redRoster = new List<Text>(configuredRedRoster);
            playerRole = configuredPlayerRole;
            playerStats = configuredPlayerStats;
            playerHealthFill = configuredPlayerHealthFill;
            playerSkillFill = configuredPlayerSkillFill;
            toastGroup = configuredToastGroup;
            toastText = configuredToastText;
            tutorialCard = configuredTutorialCard;
            tutorialTitle = configuredTutorialTitle;
            tutorialInstruction = configuredTutorialInstruction;
            tutorialProgress = configuredTutorialProgress;
            tutorialProgressFill = configuredTutorialProgressFill;
            rulesOverlay = configuredRulesOverlay;
        }

        public void Refresh(DemoMatchController match)
        {
            blueScore.text = $"BLUE  {match.BlueScore}";
            redScore.text = $"{match.RedScore}  RED";
            roundTimer.text = DemoGameRules.FormatTime(match.RemainingTime);
            modeLabel.text = match.RulesVisible
                ? "RULES PAUSED"
                : match.State switch
                {
                    DemoMatchState.Countdown => $"STARTING IN {Mathf.CeilToInt(match.RemainingTime)}",
                    DemoMatchState.SuddenDeath => "SUDDEN DEATH — NEXT HUNTER KO WINS",
                    DemoMatchState.Result => "MATCH COMPLETE",
                    _ when match.CurrentTutorialStep != DemoTutorialStep.Complete => $"TUTORIAL  |  GOLD {match.BlueGold:000}  |  CLOCK PAUSED",
                    _ => $"GOLD {match.BlueGold:000}   |   HIRE {match.Config.HireCost}G"
                };

            objective.text = match.State == DemoMatchState.Result
                ? match.ResultMessage
                : match.CurrentTutorialStep == DemoTutorialStep.HireHunter && match.IsPlayerNearBase
                    ? "BASE READY — PRESS E / GAMEPAD A TO HIRE"
                    : match.TutorialInstruction;
            help.text = "WASD / LEFT STICK MOVE   |   LMB / RT ATTACK   |   SPACE / RB SKILL   |   1-4 / D-PAD SWITCH   |   E / A HIRE   |   F1 / H / SELECT RULES   |   R / START RESTART";
            RefreshRoster(blueRoster, match.BlueHunters, match.ControlledHunter);
            RefreshRoster(redRoster, match.RedHunters, null);
            RefreshPlayerCard(match.ControlledHunter);
        }

        public void SetTutorialStep(string title, string instruction, int completed, int total, float currentStepProgress)
        {
            if (tutorialCard != null)
            {
                tutorialCard.SetActive(completed < total);
            }

            tutorialTitle.text = title;
            tutorialInstruction.text = instruction;
            tutorialProgress.text = completed >= total ? "6 / 6 COMPLETE" : $"PROGRESS  {completed} / {total}";
            tutorialProgressFill.fillAmount = Mathf.Clamp01((completed + currentStepProgress) / Mathf.Max(1f, total));
        }

        public void SetRulesVisible(bool visible)
        {
            if (rulesOverlay != null)
            {
                rulesOverlay.SetActive(visible);
            }
        }

        public void ShowToast(string message, Color color)
        {
            if (toastGroup == null || toastText == null)
            {
                return;
            }

            toastText.text = message;
            toastText.color = color;
            toastGroup.alpha = 1f;
            _toastRemaining = 2.4f;
        }

        private void Update()
        {
            if (toastGroup == null || _toastRemaining <= 0f)
            {
                return;
            }

            _toastRemaining = Mathf.Max(0f, _toastRemaining - Time.unscaledDeltaTime);
            toastGroup.alpha = Mathf.Clamp01(_toastRemaining * 2f);
        }

        private void RefreshPlayerCard(DemoCombatant hunter)
        {
            if (hunter == null)
            {
                playerRole.text = "NO HUNTER AVAILABLE";
                playerStats.text = "WAITING FOR RESPAWN";
                playerHealthFill.fillAmount = 0f;
                playerSkillFill.fillAmount = 0f;
                return;
            }

            playerRole.text = $"CONTROL  |  {hunter.Role.ToString().ToUpperInvariant()}";
            playerStats.text = $"HP {Mathf.CeilToInt(hunter.Health)} / {Mathf.CeilToInt(hunter.MaxHealth)}     SKILL {(hunter.SkillNormalized >= 0.999f ? "READY" : $"{Mathf.RoundToInt(hunter.SkillNormalized * 100f)}%")}";
            playerHealthFill.fillAmount = hunter.HealthNormalized;
            playerSkillFill.fillAmount = hunter.SkillNormalized;
        }

        private static void RefreshRoster(IReadOnlyList<Text> labels, IReadOnlyList<DemoCombatant> hunters, DemoCombatant selected)
        {
            for (var index = 0; index < labels.Count && index < hunters.Count; index++)
            {
                var hunter = hunters[index];
                var prefix = hunter == selected ? ">" : $"{index + 1}";
                var state = !hunter.IsHired ? "LOCKED" : hunter.IsAlive ? $"HP {Mathf.CeilToInt(hunter.Health):000}" : "RESPAWNING";
                labels[index].text = $"{prefix}  {hunter.Role.ToString().ToUpperInvariant()}   {state}";
                labels[index].color = hunter == selected ? new Color(1f, 0.84f, 0.28f) : Color.white;
            }
        }
    }
}
