using System.Collections.Generic;
using UnityEngine;

namespace ActionHunters.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DemoCombatant : MonoBehaviour
    {
        [SerializeField] private DemoTeam team;
        [SerializeField] private DemoRole role;
        [SerializeField, Min(0)] private int squadSlot;
        [SerializeField] private Vector3 spawnPosition;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly RaycastHit[] _lineOfSightHits = new RaycastHit[16];
        private DemoMatchController _match;
        private DemoRoleStats _stats;
        private float _health;
        private float _attackRemaining;
        private float _skillRemaining;
        private float _respawnRemaining;
        private float _damageReductionRemaining;
        private bool _isAlive;
        private bool _isHired;
        private bool _rewardGranted;
        private Vector3 _lastMoveDirection = Vector3.right;
        private CharacterController _characterController;
        private DemoCombatantPresentation _presentation;
        private float _verticalVelocity;
        private float _deathVisualRemaining;
        private float _aiDetourRemaining;
        private int _aiDetourSign = 1;
        private CollisionFlags _lastMoveCollisions;

        public DemoTeam Team => team;
        public DemoRole Role => role;
        public int SquadSlot => squadSlot;
        public bool IsAlive => _isAlive;
        public bool IsHired => _isHired;
        public float Health => _health;
        public float MaxHealth => _stats.maxHealth;
        public float HealthNormalized => _stats.maxHealth <= 0f ? 0f : Mathf.Clamp01(_health / _stats.maxHealth);
        public float SkillNormalized => _stats.skillCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(_skillRemaining / _stats.skillCooldown);

        public void Configure(DemoTeam configuredTeam, DemoRole configuredRole, int configuredSlot, Vector3 configuredSpawn)
        {
            team = configuredTeam;
            role = configuredRole;
            squadSlot = configuredSlot;
            spawnPosition = configuredSpawn;
        }

        public void Initialize(DemoMatchController match, bool hired)
        {
            _match = match;
            _stats = match.Config.GetStats(role);
            _renderers.Clear();
            GetComponentsInChildren(true, _renderers);
            _characterController = GetComponent<CharacterController>();
            if (_characterController == null)
            {
                _characterController = gameObject.AddComponent<CharacterController>();
            }

            ConfigureCharacterController();
            DisableChildColliders();
            _presentation = GetComponent<DemoCombatantPresentation>();
            _presentation?.Initialize(this, match.EffectPool);
            _aiDetourSign = ((squadSlot + (int)team) & 1) == 0 ? 1 : -1;
            SetHired(hired, true);
        }

        public void SetHired(bool hired, bool immediateReset = false)
        {
            _isHired = hired;
            if (!hired)
            {
                _isAlive = false;
                if (_characterController != null)
                {
                    _characterController.enabled = false;
                }

                SetVisuals(false);
                return;
            }

            if (immediateReset || !_isAlive)
            {
                Respawn();
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_isHired)
            {
                return;
            }

            _attackRemaining = Mathf.Max(0f, _attackRemaining - deltaTime);
            _skillRemaining = Mathf.Max(0f, _skillRemaining - deltaTime);
            _damageReductionRemaining = Mathf.Max(0f, _damageReductionRemaining - deltaTime);

            if (_isAlive)
            {
                ApplyGravity(deltaTime);
                return;
            }

            if (_deathVisualRemaining > 0f)
            {
                _deathVisualRemaining -= deltaTime;
                if (_deathVisualRemaining <= 0f)
                {
                    SetVisuals(false);
                }
            }

            _respawnRemaining -= deltaTime;
            if (_respawnRemaining <= 0f && _match.State != DemoMatchState.Result)
            {
                Respawn();
            }
        }

        public float Move(Vector2 input, float deltaTime)
        {
            if (!CanMove || input.sqrMagnitude < 0.001f)
            {
                return 0f;
            }

            var direction = new Vector3(input.x, 0f, input.y).normalized;
            return MoveDirection(direction, deltaTime);
        }

        public void MoveToward(Vector3 target, float deltaTime, float stoppingDistance)
        {
            if (!CanAct)
            {
                return;
            }

            var delta = target - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                return;
            }

            var direction = delta.normalized;
            if (_aiDetourRemaining > 0f)
            {
                _aiDetourRemaining = Mathf.Max(0f, _aiDetourRemaining - deltaTime);
                var side = new Vector3(-direction.z, 0f, direction.x) * _aiDetourSign;
                MoveDirection((direction * 0.35f + side).normalized, deltaTime);
                return;
            }

            var expectedDistance = _stats.moveSpeed * deltaTime;
            var movedDistance = MoveDirection(direction, deltaTime);
            if ((_lastMoveCollisions & CollisionFlags.Sides) != 0 && movedDistance < expectedDistance * 0.8f)
            {
                _aiDetourSign *= -1;
                _aiDetourRemaining = 0.7f;
            }
        }

        public bool TryBasicAttack(DemoCombatant preferredTarget = null)
        {
            if (!CanAct || _attackRemaining > 0f)
            {
                return false;
            }

            var target = IsValidTarget(preferredTarget, _stats.attackRange + 0.5f)
                ? preferredTarget
                : _match.FindClosestHostile(this, _stats.attackRange + 0.5f);
            if (target == null || !HasLineOfSightTo(target))
            {
                return false;
            }

            Face(target.transform.position - transform.position);
            _attackRemaining = _stats.attackCooldown;
            _presentation?.PlayAttack(target.transform.position);
            target.ReceiveDamage(_stats.attackDamage, this);
            return true;
        }

        public bool TrySkill()
        {
            if (!CanAct || _skillRemaining > 0f)
            {
                return false;
            }

            var used = role switch
            {
                DemoRole.Guardian => UseGuardianSkill(),
                DemoRole.Ranger => UseRangerSkill(),
                DemoRole.Medic => UseMedicSkill(),
                DemoRole.Striker => UseStrikerSkill(),
                _ => false
            };

            if (used)
            {
                _skillRemaining = _stats.skillCooldown;
                _presentation?.PlaySkill();
            }

            return used;
        }

        public void ReceiveDamage(float amount, DemoCombatant attacker)
        {
            if (!_isAlive || amount <= 0f || _match.State == DemoMatchState.Result)
            {
                return;
            }

            if (_damageReductionRemaining > 0f)
            {
                amount *= 0.45f;
            }

            _health = Mathf.Max(0f, _health - amount);
            if (_health <= 0f)
            {
                Die(attacker);
            }
            else
            {
                _presentation?.PlayHit();
            }

            _match.OnDamageFeedback(this, attacker, amount);
        }

        public void Heal(float amount)
        {
            if (_isAlive && amount > 0f)
            {
                var previousHealth = _health;
                _health = Mathf.Min(_stats.maxHealth, _health + amount);
                if (_health > previousHealth)
                {
                    _presentation?.PlayHeal();
                }
            }
        }

        public void SetSelected(bool selected)
        {
            _presentation?.SetSelected(selected);
        }

        private bool CanAct => _isHired && _isAlive && _match != null && _match.AllowsCombat;
        private bool CanMove => _isHired && _isAlive && _match != null &&
                                _match.State is DemoMatchState.Countdown or DemoMatchState.Playing or DemoMatchState.SuddenDeath;

        private float MoveDirection(Vector3 direction, float deltaTime)
        {
            _lastMoveDirection = direction;
            var previous = transform.position;
            var next = previous + direction * (_stats.moveSpeed * deltaTime);
            var extents = _match.Config.ArenaExtents;
            next.x = Mathf.Clamp(next.x, -extents.x, extents.x);
            next.z = Mathf.Clamp(next.z, -extents.y, extents.y);
            next.y = previous.y;
            if (_characterController != null && _characterController.enabled)
            {
                _lastMoveCollisions = _characterController.Move(next - previous);
            }
            else
            {
                _lastMoveCollisions = CollisionFlags.None;
                transform.position = next;
            }

            Face(direction);
            var moved = transform.position - previous;
            moved.y = 0f;
            return moved.magnitude;
        }

        private void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.35f);
            }
        }

        private bool UseGuardianSkill()
        {
            _damageReductionRemaining = 3f;
            var enemies = _match.GetHostilesInRange(this, 3.5f);
            for (var index = 0; index < enemies.Count; index++)
            {
                enemies[index].ReceiveDamage(_stats.attackDamage * 0.65f, this);
            }

            return true;
        }

        private bool UseRangerSkill()
        {
            var targets = _match.GetHostilesInRange(this, _stats.attackRange + 3f);
            if (targets.Count == 0)
            {
                return false;
            }

            var direction = targets[0].transform.position - transform.position;
            direction.y = 0f;
            direction.Normalize();
            Face(direction);
            var hitCount = 0;
            for (var index = 0; index < targets.Count; index++)
            {
                var toTarget = targets[index].transform.position - transform.position;
                toTarget.y = 0f;
                if (Vector3.Dot(direction, toTarget.normalized) > 0.78f)
                {
                    targets[index].ReceiveDamage(_stats.attackDamage * 1.8f, this);
                    hitCount++;
                }
            }

            return hitCount > 0;
        }

        private bool UseMedicSkill()
        {
            var allies = _match.GetAlliesInRange(this, 7f);
            var healed = false;
            for (var index = 0; index < allies.Count; index++)
            {
                if (allies[index].HealthNormalized < 0.99f)
                {
                    allies[index].Heal(55f);
                    healed = true;
                }
            }

            if (HealthNormalized < 0.99f)
            {
                Heal(55f);
                healed = true;
            }

            return healed;
        }

        private bool UseStrikerSkill()
        {
            var target = _match.FindClosestHostile(this, 8f);
            if (target == null)
            {
                return false;
            }

            var direction = target.transform.position - transform.position;
            direction.y = 0f;
            var distance = Mathf.Max(0f, direction.magnitude - 1.4f);
            var dash = direction.normalized * Mathf.Min(distance, 4.5f);
            var targetPosition = transform.position + dash;
            var extents = _match.Config.ArenaExtents;
            targetPosition.x = Mathf.Clamp(targetPosition.x, -extents.x, extents.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, -extents.y, extents.y);
            if (_characterController != null && _characterController.enabled)
            {
                _lastMoveCollisions = _characterController.Move(targetPosition - transform.position);
            }
            else
            {
                _lastMoveCollisions = CollisionFlags.None;
                transform.position = targetPosition;
            }

            Face(direction);
            if (IsValidTarget(target, 2.2f) && HasLineOfSightTo(target))
            {
                target.ReceiveDamage(_stats.attackDamage * 2f, this);
            }

            return true;
        }

        private bool HasLineOfSightTo(DemoCombatant candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var origin = transform.position + Vector3.up * 0.9f;
            var destination = candidate.transform.position + Vector3.up * 0.9f;
            var delta = destination - origin;
            var distance = delta.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            var hitCount = Physics.RaycastNonAlloc(
                origin,
                delta / distance,
                _lineOfSightHits,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var index = 0; index < hitCount; index++)
            {
                var hitCollider = _lineOfSightHits[index].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                var hitCombatant = hitCollider.GetComponentInParent<DemoCombatant>();
                if (hitCombatant == this || hitCombatant == candidate)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private bool IsValidTarget(DemoCombatant candidate, float range)
        {
            return candidate != null && candidate.IsHired && candidate.IsAlive &&
                   DemoGameRules.AreHostile(team, candidate.Team) &&
                   FlatDistanceSquared(candidate.transform.position) <= range * range;
        }

        private float FlatDistanceSquared(Vector3 position)
        {
            var delta = position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }

        private void Die(DemoCombatant killer)
        {
            if (!_isAlive)
            {
                return;
            }

            _isAlive = false;
            _respawnRemaining = role is DemoRole.Monster or DemoRole.Boss
                ? _match.Config.MonsterRespawnDelay
                : _match.Config.HunterRespawnDelay;
            _deathVisualRemaining = 0.85f;
            _verticalVelocity = 0f;
            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            _presentation?.PlayDeath();

            if (!_rewardGranted)
            {
                _rewardGranted = true;
                _match.OnCombatantDefeated(this, killer);
            }
        }

        private void Respawn()
        {
            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            transform.position = spawnPosition;
            _health = _stats.maxHealth;
            _attackRemaining = 0f;
            _skillRemaining = 0f;
            _damageReductionRemaining = 0f;
            _verticalVelocity = -2f;
            _deathVisualRemaining = 0f;
            _aiDetourRemaining = 0f;
            _lastMoveCollisions = CollisionFlags.None;
            _rewardGranted = false;
            _isAlive = true;
            SetVisuals(true);
            if (_characterController != null)
            {
                _characterController.enabled = true;
            }

            _presentation?.PlaySpawn();
        }

        private void ApplyGravity(float deltaTime)
        {
            if (_characterController == null || !_characterController.enabled)
            {
                return;
            }

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity = Mathf.Max(_verticalVelocity + Physics.gravity.y * 2.4f * deltaTime, -35f);
            }

            _characterController.Move(Vector3.up * (_verticalVelocity * deltaTime));
        }

        private void ConfigureCharacterController()
        {
            var height = role == DemoRole.Boss ? 2.2f : role == DemoRole.Monster ? 1.7f : 1.65f;
            var radius = role == DemoRole.Boss ? 0.8f : role == DemoRole.Monster ? 0.55f : 0.42f;
            _characterController.height = height;
            _characterController.radius = radius;
            _characterController.center = Vector3.up * (height * 0.5f);
            _characterController.slopeLimit = 50f;
            _characterController.stepOffset = 0.3f;
            _characterController.skinWidth = 0.05f;
            _characterController.minMoveDistance = 0f;
        }

        private void DisableChildColliders()
        {
            var colliders = GetComponentsInChildren<Collider>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (colliders[index].transform != transform)
                {
                    colliders[index].enabled = false;
                }
            }
        }

        private void SetVisuals(bool visible)
        {
            for (var index = 0; index < _renderers.Count; index++)
            {
                if (_renderers[index] != null)
                {
                    _renderers[index].enabled = visible;
                }
            }
        }
    }
}
