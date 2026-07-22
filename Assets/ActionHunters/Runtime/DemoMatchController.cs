using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ActionHunters.Runtime
{
    public sealed class DemoMatchController : MonoBehaviour
    {
        [SerializeField] private DemoGameConfig config;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private DemoCameraRig cameraRig;
        [SerializeField] private DemoHud hud;
        [SerializeField] private DemoTutorialDirector tutorial;
        [SerializeField] private DemoEffectPool effectPool;
        [SerializeField] private List<DemoCombatant> blueHunters = new List<DemoCombatant>();
        [SerializeField] private List<DemoCombatant> redHunters = new List<DemoCombatant>();
        [SerializeField] private List<DemoCombatant> monsters = new List<DemoCombatant>();
        [SerializeField] private Vector3 blueBase = new Vector3(-18f, 0f, 0f);
        [SerializeField] private Vector3 redBase = new Vector3(18f, 0f, 0f);

        private readonly List<DemoCombatant> _allCombatants = new List<DemoCombatant>();
        private readonly List<DemoCombatant> _queryResults = new List<DemoCombatant>();
        private readonly Dictionary<DemoCombatant, DemoCombatant> _aiTargets = new Dictionary<DemoCombatant, DemoCombatant>();
        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _attackAction;
        private InputAction _interactAction;
        private InputAction _skillAction;
        private InputAction _nextAction;
        private InputAction _previousAction;
        private DemoCombatant _controlledHunter;
        private float _remainingTime;
        private float _aiThinkRemaining;
        private float _hudRefreshRemaining;
        private int _blueScore;
        private int _redScore;
        private int _blueGold;
        private bool _pendingTutorialMonsterDefeat;

        public DemoGameConfig Config => config;
        public DemoMatchState State { get; private set; }
        public bool AllowsCombat => State is DemoMatchState.Playing or DemoMatchState.SuddenDeath;
        public DemoEffectPool EffectPool => effectPool;
        public float RemainingTime => _remainingTime;
        public int BlueScore => _blueScore;
        public int RedScore => _redScore;
        public int BlueGold => _blueGold;
        public IReadOnlyList<DemoCombatant> BlueHunters => blueHunters;
        public IReadOnlyList<DemoCombatant> RedHunters => redHunters;
        public DemoCombatant ControlledHunter => _controlledHunter;
        public DemoTutorialStep CurrentTutorialStep => tutorial != null ? tutorial.CurrentStep : DemoTutorialStep.Complete;
        public string TutorialInstruction => DemoTutorialFlow.GetInstruction(CurrentTutorialStep);
        public bool RulesVisible => tutorial != null && tutorial.IsRulesVisible;
        public bool IsPlayerNearBase => _controlledHunter != null &&
                                        FlatDistanceSquared(_controlledHunter.transform.position, blueBase) <= 30.25f;
        public string ResultMessage { get; private set; } = string.Empty;

        public void Configure(
            DemoGameConfig configuredConfig,
            InputActionAsset configuredInputActions,
            DemoCameraRig configuredCameraRig,
            DemoHud configuredHud,
            DemoTutorialDirector configuredTutorial,
            DemoEffectPool configuredEffectPool,
            IReadOnlyList<DemoCombatant> configuredBlueHunters,
            IReadOnlyList<DemoCombatant> configuredRedHunters,
            IReadOnlyList<DemoCombatant> configuredMonsters)
        {
            config = configuredConfig;
            inputActions = configuredInputActions;
            cameraRig = configuredCameraRig;
            hud = configuredHud;
            tutorial = configuredTutorial;
            effectPool = configuredEffectPool;
            blueHunters = new List<DemoCombatant>(configuredBlueHunters);
            redHunters = new List<DemoCombatant>(configuredRedHunters);
            monsters = new List<DemoCombatant>(configuredMonsters);
        }

        private void Awake()
        {
            if (config == null || inputActions == null || cameraRig == null || hud == null || tutorial == null || effectPool == null)
            {
                enabled = false;
                Debug.LogError("[Action Hunters] DemoMatchController is missing required scene references.", this);
                return;
            }

            _playerMap = inputActions.FindActionMap("Player", true);
            _moveAction = _playerMap.FindAction("Move", true);
            _attackAction = _playerMap.FindAction("Attack", true);
            _interactAction = _playerMap.FindAction("Interact", true);
            _skillAction = _playerMap.FindAction("Jump", true);
            _nextAction = _playerMap.FindAction("Next", true);
            _previousAction = _playerMap.FindAction("Previous", true);

            _allCombatants.Clear();
            _allCombatants.AddRange(blueHunters);
            _allCombatants.AddRange(redHunters);
            _allCombatants.AddRange(monsters);
        }

        private void OnEnable()
        {
            _playerMap?.Enable();
        }

        private void Start()
        {
            StartFreshMatch();
        }

        private void OnDisable()
        {
            _playerMap?.Disable();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame ||
                State == DemoMatchState.Result && gamepad != null && gamepad.startButton.wasPressedThisFrame)
            {
                StartFreshMatch();
                return;
            }

            if (keyboard != null && (keyboard.f1Key.wasPressedThisFrame || keyboard.hKey.wasPressedThisFrame) ||
                gamepad != null && gamepad.selectButton.wasPressedThisFrame)
            {
                tutorial.ToggleRules();
                hud.Refresh(this);
            }

            if (tutorial.IsRulesVisible)
            {
                if (WasTutorialConfirmPressed())
                {
                    tutorial.DismissRules();
                    hud.Refresh(this);
                }

                return;
            }

            var deltaTime = Time.deltaTime;
            for (var index = 0; index < _allCombatants.Count; index++)
            {
                _allCombatants[index].Tick(deltaTime);
            }

            TickMatchClock(deltaTime);
            if (State == DemoMatchState.Countdown)
            {
                TickPlayerMovement(deltaTime);
            }

            if (AllowsCombat)
            {
                TickPlayer(deltaTime);
                _aiThinkRemaining -= deltaTime;
                if (_aiThinkRemaining <= 0f)
                {
                    _aiThinkRemaining += config.AiThinkInterval;
                    RefreshAiTargets();
                }

                TickAi(deltaTime);
            }

            _hudRefreshRemaining -= deltaTime;
            if (_hudRefreshRemaining <= 0f)
            {
                _hudRefreshRemaining = 0.1f;
                hud.Refresh(this);
            }
        }

        public DemoCombatant FindClosestHostile(DemoCombatant source, float range)
        {
            DemoCombatant closest = null;
            var closestDistance = range * range;
            for (var index = 0; index < _allCombatants.Count; index++)
            {
                var candidate = _allCombatants[index];
                if (candidate == source || !candidate.IsHired || !candidate.IsAlive ||
                    !DemoGameRules.AreHostile(source.Team, candidate.Team))
                {
                    continue;
                }

                var distance = FlatDistanceSquared(source.transform.position, candidate.transform.position);
                if (distance <= closestDistance)
                {
                    closest = candidate;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        public List<DemoCombatant> GetHostilesInRange(DemoCombatant source, float range)
        {
            return QueryRange(source, range, true);
        }

        public List<DemoCombatant> GetAlliesInRange(DemoCombatant source, float range)
        {
            return QueryRange(source, range, false);
        }

        public void OnCombatantDefeated(DemoCombatant defeated, DemoCombatant killer)
        {
            if (killer == null)
            {
                return;
            }

            if (defeated.Team == DemoTeam.Neutral)
            {
                var gold = defeated.Role == DemoRole.Boss ? config.EliteMonsterGold : config.MonsterGold;
                if (killer.Team == DemoTeam.Blue)
                {
                    _blueGold += gold;
                    hud.ShowToast(
                        defeated.Role == DemoRole.Boss ? $"+{gold} GOLD   +{config.BossKillScore} SCORE" : $"+{gold} GOLD",
                        new Color(1f, 0.82f, 0.24f));
                    _pendingTutorialMonsterDefeat = true;
                    TryReportPendingMonsterDefeat();
                }

                if (defeated.Role == DemoRole.Boss)
                {
                    AddScore(killer.Team, config.BossKillScore);
                }
            }
            else if (killer.Team is DemoTeam.Blue or DemoTeam.Red)
            {
                AddScore(killer.Team, config.HunterKillScore);
                hud.ShowToast($"{killer.Team.ToString().ToUpperInvariant()} +{config.HunterKillScore} SCORE", killer.Team == DemoTeam.Blue
                    ? new Color(0.2f, 0.72f, 1f)
                    : new Color(1f, 0.25f, 0.28f));
                if (State == DemoMatchState.SuddenDeath)
                {
                    Debug.Log($"[Action Hunters] {killer.Team} {killer.Role} defeated {defeated.Team} {defeated.Role}.");
                    FinishMatch();
                    return;
                }
            }

            Debug.Log($"[Action Hunters] {killer.Team} {killer.Role} defeated {defeated.Team} {defeated.Role}.");
        }

        private void StartFreshMatch()
        {
            _blueScore = 0;
            _redScore = 0;
            _blueGold = config.StartingGold;
            ResultMessage = string.Empty;
            State = DemoMatchState.Countdown;
            _remainingTime = config.CountdownDuration;
            _aiThinkRemaining = 0f;
            _aiTargets.Clear();
            _hudRefreshRemaining = 0f;
            _pendingTutorialMonsterDefeat = false;
            tutorial.BeginSession();

            for (var index = 0; index < blueHunters.Count; index++)
            {
                blueHunters[index].Initialize(this, index == 0);
            }

            for (var index = 0; index < redHunters.Count; index++)
            {
                redHunters[index].Initialize(this, index < 2);
            }

            for (var index = 0; index < monsters.Count; index++)
            {
                monsters[index].Initialize(this, true);
            }

            SelectHunter(0, true, false);
            hud.Refresh(this);
            Debug.Log("[Action Hunters] Offline vertical slice reset. Countdown started.");
        }

        private void TickMatchClock(float deltaTime)
        {
            if (State == DemoMatchState.Result)
            {
                return;
            }

            if (State == DemoMatchState.Playing && tutorial.CurrentStep != DemoTutorialStep.Complete)
            {
                return;
            }

            _remainingTime -= deltaTime;
            if (State == DemoMatchState.Countdown && _remainingTime <= 0f)
            {
                State = DemoMatchState.Playing;
                _remainingTime = config.MatchDuration;
                Debug.Log("[Action Hunters] Match state: Playing.");
                return;
            }

            if (State == DemoMatchState.Playing && _remainingTime <= 0f)
            {
                if (_blueScore == _redScore)
                {
                    State = DemoMatchState.SuddenDeath;
                    _remainingTime = config.SuddenDeathDuration;
                    Debug.Log("[Action Hunters] Match state: Sudden Death.");
                }
                else
                {
                    FinishMatch();
                }
            }
            else if (State == DemoMatchState.SuddenDeath && _remainingTime <= 0f)
            {
                FinishMatch();
            }
        }

        private void TickPlayer(float deltaTime)
        {
            if (!TickPlayerMovement(deltaTime))
            {
                return;
            }

            var gamepad = Gamepad.current;
            var gamepadAttack = gamepad != null && gamepad.rightTrigger.wasPressedThisFrame;
            var gamepadSkill = gamepad != null && gamepad.rightShoulder.wasPressedThisFrame;
            var gamepadHire = gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
            if (WasPressedOutsideGamepad(_attackAction) || gamepadAttack)
            {
                if (_controlledHunter.TryBasicAttack())
                {
                    tutorial.Report(DemoTutorialSignal.BasicAttackHit);
                }
            }

            TryReportPendingMonsterDefeat();

            if (!AllowsCombat)
            {
                return;
            }

            if (WasPressedOutsideGamepad(_skillAction) || gamepadSkill)
            {
                if (_controlledHunter.TrySkill())
                {
                    tutorial.Report(DemoTutorialSignal.SkillUsed);
                }
            }

            TryReportPendingMonsterDefeat();

            if (!AllowsCombat)
            {
                return;
            }

            if (WasPressedOutsideGamepad(_interactAction) || gamepadHire)
            {
                TryHireHunter();
            }

            if (WasPressedFromGamepad(_nextAction))
            {
                SelectRelative(1);
            }

            if (WasPressedFromGamepad(_previousAction))
            {
                SelectRelative(-1);
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectHunter(0);
                if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectHunter(1);
                if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectHunter(2);
                if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectHunter(3);
                if (Keyboard.current.tabKey.wasPressedThisFrame) SelectRelative(1);
            }
        }

        private bool TickPlayerMovement(float deltaTime)
        {
            RecoverControlledHunter();
            if (_controlledHunter == null || !_controlledHunter.IsAlive)
            {
                return false;
            }

            var movedDistance = _controlledHunter.Move(ReadMoveInput(), deltaTime);
            if (movedDistance > 0f)
            {
                tutorial.Report(DemoTutorialSignal.Moved, movedDistance);
            }

            return true;
        }

        private void TryReportPendingMonsterDefeat()
        {
            if (!_pendingTutorialMonsterDefeat || tutorial.CurrentStep != DemoTutorialStep.HuntMonster)
            {
                return;
            }

            _pendingTutorialMonsterDefeat = false;
            tutorial.Report(DemoTutorialSignal.MonsterDefeated);
        }

        private static bool WasPressedOutsideGamepad(InputAction action)
        {
            return action.WasPressedThisFrame() && action.activeControl?.device is not Gamepad;
        }

        private static bool WasPressedFromGamepad(InputAction action)
        {
            return action.WasPressedThisFrame() && action.activeControl?.device is Gamepad;
        }

        private Vector2 ReadMoveInput()
        {
            var input = _moveAction.ReadValue<Vector2>();
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var keyboardInput = Vector2.zero;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardInput.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardInput.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardInput.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardInput.y += 1f;
                if (keyboardInput.sqrMagnitude > input.sqrMagnitude)
                {
                    input = keyboardInput;
                }
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        private static bool WasTutorialConfirmPressed()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            var pointer = Pointer.current;
            return keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) ||
                   gamepad != null && gamepad.buttonSouth.wasPressedThisFrame ||
                   pointer != null && pointer.press.wasPressedThisFrame;
        }

        private void RefreshAiTargets()
        {
            for (var index = 0; index < _allCombatants.Count; index++)
            {
                var actor = _allCombatants[index];
                if (!actor.IsHired || !actor.IsAlive || actor == _controlledHunter)
                {
                    _aiTargets.Remove(actor);
                    continue;
                }

                _aiTargets[actor] = FindClosestHostile(actor, config.GetStats(actor.Role).detectionRange);
            }
        }

        private void TickAi(float deltaTime)
        {
            for (var index = 0; index < _allCombatants.Count; index++)
            {
                if (!AllowsCombat)
                {
                    break;
                }

                var actor = _allCombatants[index];
                if (!actor.IsHired || !actor.IsAlive || actor == _controlledHunter)
                {
                    continue;
                }

                var stats = config.GetStats(actor.Role);
                _aiTargets.TryGetValue(actor, out var target);
                if (actor.Role == DemoRole.Medic)
                {
                    actor.TrySkill();
                }

                if (target != null && target.IsAlive)
                {
                    if (actor.Role == DemoRole.Ranger)
                    {
                        actor.TrySkill();
                    }

                    if (!actor.TryBasicAttack(target))
                    {
                        actor.MoveToward(target.transform.position, deltaTime, stats.attackRange * 0.85f);
                    }
                    else if (actor.Role is DemoRole.Guardian or DemoRole.Striker)
                    {
                        actor.TrySkill();
                    }
                }
                else if (actor.Team == DemoTeam.Blue && _controlledHunter != null)
                {
                    actor.MoveToward(_controlledHunter.transform.position, deltaTime, 3f + actor.SquadSlot);
                }
                else if (actor.Team == DemoTeam.Red)
                {
                    actor.MoveToward(new Vector3(-5f, actor.transform.position.y, 0f), deltaTime, 2f);
                }
            }
        }

        private void TryHireHunter()
        {
            if (!IsPlayerNearBase)
            {
                Debug.Log("[Action Hunters] Hire rejected: return to the blue base.");
                hud.ShowToast("RETURN TO THE BLUE BASE TO HIRE", new Color(1f, 0.5f, 0.2f));
                return;
            }

            var activeCount = 0;
            DemoCombatant candidate = null;
            for (var index = 0; index < blueHunters.Count; index++)
            {
                if (blueHunters[index].IsHired)
                {
                    activeCount++;
                }
                else if (candidate == null)
                {
                    candidate = blueHunters[index];
                }
            }

            if (candidate == null || !DemoGameRules.CanHire(_blueGold, config.HireCost, activeCount, blueHunters.Count))
            {
                Debug.Log("[Action Hunters] Hire rejected: earn more gold or all four slots are occupied.");
                var message = candidate == null ? "ALL FOUR HUNTERS ARE ACTIVE" : $"{Mathf.Max(0, config.HireCost - _blueGold)}G MORE NEEDED";
                hud.ShowToast(message, new Color(1f, 0.5f, 0.2f));
                return;
            }

            _blueGold -= config.HireCost;
            candidate.SetHired(true, true);
            hud.ShowToast($"{candidate.Role.ToString().ToUpperInvariant()} JOINED — PRESS 2 / TAB TO CONTROL", new Color(0.28f, 1f, 0.55f));
            tutorial.Report(DemoTutorialSignal.HunterHired);
            Debug.Log($"[Action Hunters] Hired Blue {candidate.Role} for {config.HireCost} gold.");
        }

        private void SelectRelative(int direction)
        {
            var start = _controlledHunter == null ? 0 : blueHunters.IndexOf(_controlledHunter);
            for (var step = 1; step <= blueHunters.Count; step++)
            {
                var index = (start + step * direction + blueHunters.Count * 2) % blueHunters.Count;
                if (blueHunters[index].IsHired && blueHunters[index].IsAlive)
                {
                    SelectHunter(index);
                    return;
                }
            }
        }

        private void SelectHunter(int index, bool snapCamera = false, bool notifyTutorial = true)
        {
            if (index < 0 || index >= blueHunters.Count)
            {
                return;
            }

            var candidate = blueHunters[index];
            if (!candidate.IsHired || !candidate.IsAlive)
            {
                return;
            }

            var previous = _controlledHunter;
            var changed = previous != candidate;
            if (changed && previous != null)
            {
                previous.SetSelected(false);
            }

            _controlledHunter = candidate;
            candidate.SetSelected(true);
            cameraRig.SetTarget(candidate.transform, snapCamera);
            if (changed && notifyTutorial)
            {
                hud.ShowToast($"CONTROL: {candidate.Role.ToString().ToUpperInvariant()}", new Color(1f, 0.84f, 0.28f));
                tutorial.Report(DemoTutorialSignal.HunterSwitched);
            }

            Debug.Log($"[Action Hunters] Direct control switched to slot {index + 1}: {candidate.Role}.");
        }

        private void RecoverControlledHunter()
        {
            if (_controlledHunter != null && _controlledHunter.IsHired && _controlledHunter.IsAlive)
            {
                return;
            }

            for (var index = 0; index < blueHunters.Count; index++)
            {
                if (blueHunters[index].IsHired && blueHunters[index].IsAlive)
                {
                    SelectHunter(index, false, false);
                    return;
                }
            }

            if (_controlledHunter != null)
            {
                _controlledHunter.SetSelected(false);
            }

            _controlledHunter = null;
        }

        public void OnDamageFeedback(DemoCombatant victim, DemoCombatant attacker, float amount)
        {
            if (victim == _controlledHunter || attacker == _controlledHunter)
            {
                cameraRig.AddImpulse(Mathf.Clamp(amount / 160f, 0.035f, 0.18f));
            }
        }

        private List<DemoCombatant> QueryRange(DemoCombatant source, float range, bool hostile)
        {
            _queryResults.Clear();
            var rangeSquared = range * range;
            for (var index = 0; index < _allCombatants.Count; index++)
            {
                var candidate = _allCombatants[index];
                if (candidate == source || !candidate.IsHired || !candidate.IsAlive)
                {
                    continue;
                }

                var isHostile = DemoGameRules.AreHostile(source.Team, candidate.Team);
                if (isHostile != hostile || FlatDistanceSquared(source.transform.position, candidate.transform.position) > rangeSquared)
                {
                    continue;
                }

                _queryResults.Add(candidate);
            }

            _queryResults.Sort((left, right) =>
                FlatDistanceSquared(source.transform.position, left.transform.position)
                    .CompareTo(FlatDistanceSquared(source.transform.position, right.transform.position)));
            return new List<DemoCombatant>(_queryResults);
        }

        private void AddScore(DemoTeam team, int amount)
        {
            if (team == DemoTeam.Blue)
            {
                _blueScore += amount;
            }
            else if (team == DemoTeam.Red)
            {
                _redScore += amount;
            }
        }

        private void FinishMatch()
        {
            State = DemoMatchState.Result;
            _remainingTime = 0f;
            var winner = DemoGameRules.DetermineWinner(_blueScore, _redScore);
            ResultMessage = winner == DemoTeam.Neutral
                ? $"DRAW  {_blueScore} : {_redScore}   |   PRESS R TO PLAY AGAIN"
                : $"{winner.ToString().ToUpperInvariant()} WINS  {_blueScore} : {_redScore}   |   PRESS R TO PLAY AGAIN";
            hud.Refresh(this);
            Debug.Log($"[Action Hunters] Match result: {ResultMessage}");
        }

        private static float FlatDistanceSquared(Vector3 first, Vector3 second)
        {
            var delta = first - second;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }
    }
}
