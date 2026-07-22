using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ActionHunters.Runtime
{
    [DisallowMultipleComponent]
    public sealed class DemoCombatantPresentation : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int AttackParameter = Animator.StringToHash("Attack");
        private static readonly int SkillParameter = Animator.StringToHash("Skill");
        private static readonly int JumpParameter = Animator.StringToHash("Jump");
        private static readonly int HitParameter = Animator.StringToHash("Hit");
        private static readonly int DeadParameter = Animator.StringToHash("Dead");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject attackEffectPrefab;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject skillEffectPrefab;
        [SerializeField] private GameObject healEffectPrefab;

        private readonly List<Renderer> _visualRenderers = new List<Renderer>();
        private MaterialPropertyBlock _propertyBlock;
        private DemoCombatant _owner;
        private DemoEffectPool _effectPool;
        private Transform _healthRoot;
        private Transform _healthFill;
        private GameObject _selectionMarker;
        private Material _healthBackgroundMaterial;
        private Material _healthFillMaterial;
        private Material _selectionMaterial;
        private Vector3 _visualBasePosition;
        private Quaternion _visualBaseRotation;
        private Vector3 _visualBaseScale;
        private Vector3 _lastWorldPosition;
        private float _hitFlashRemaining;
        private float _attackPulseRemaining;
        private float _jumpPulseRemaining;
        private bool _selected;
        private bool _initialized;
        private bool _hasJumpParameter;

        public void Configure(
            Transform configuredVisualRoot,
            Animator configuredAnimator,
            GameObject configuredAttackEffect,
            GameObject configuredHitEffect,
            GameObject configuredSkillEffect,
            GameObject configuredHealEffect)
        {
            visualRoot = configuredVisualRoot;
            animator = configuredAnimator;
            attackEffectPrefab = configuredAttackEffect;
            hitEffectPrefab = configuredHitEffect;
            skillEffectPrefab = configuredSkillEffect;
            healEffectPrefab = configuredHealEffect;
        }

        public void Initialize(DemoCombatant owner, DemoEffectPool effectPool)
        {
            _owner = owner;
            _effectPool = effectPool;
            _propertyBlock ??= new MaterialPropertyBlock();
            if (visualRoot == null)
            {
                var configuredAnimator = GetComponentInChildren<Animator>(true);
                visualRoot = configuredAnimator != null ? configuredAnimator.transform : FindVisualRoot();
                animator = configuredAnimator;
            }

            if (!_initialized)
            {
                _visualRenderers.Clear();
                if (visualRoot != null)
                {
                    visualRoot.GetComponentsInChildren(true, _visualRenderers);
                    _visualBasePosition = visualRoot.localPosition;
                    _visualBaseRotation = visualRoot.localRotation;
                    _visualBaseScale = visualRoot.localScale;
                }

                EnsureWorldStatus();
                _initialized = true;
            }

            _lastWorldPosition = transform.position;
            _hasJumpParameter = HasAnimatorParameter(animator, JumpParameter);
            PlaySpawn();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            RefreshWorldStatusVisibility();
        }

        public void PlayAttack(Vector3 targetPosition)
        {
            if (animator != null)
            {
                animator.SetTrigger(AttackParameter);
            }

            _attackPulseRemaining = 0.2f;
            var effectPosition = Vector3.Lerp(transform.position, targetPosition, 0.72f) + Vector3.up * 0.9f;
            _effectPool?.Play(attackEffectPrefab, effectPosition, Quaternion.identity);
        }

        public void PlaySkill()
        {
            if (animator != null)
            {
                animator.SetTrigger(SkillParameter);
            }

            _attackPulseRemaining = 0.35f;
            _effectPool?.Play(skillEffectPrefab, transform.position + Vector3.up * 0.65f, transform.rotation);
        }

        public void PlayHit()
        {
            if (animator != null)
            {
                animator.SetTrigger(HitParameter);
            }

            _hitFlashRemaining = 0.14f;
            ApplyHitFlash(true);
            _effectPool?.Play(hitEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
        }

        public void PlayHeal()
        {
            _effectPool?.Play(healEffectPrefab, transform.position + Vector3.up * 0.6f, Quaternion.identity);
        }

        public void PlayJump()
        {
            if (animator != null && _hasJumpParameter)
            {
                animator.SetTrigger(JumpParameter);
            }

            _jumpPulseRemaining = 0.28f;
        }

        public void PlayDeath()
        {
            if (animator != null)
            {
                animator.SetBool(DeadParameter, true);
            }

            RefreshWorldStatusVisibility();
        }

        public void PlaySpawn()
        {
            ApplyHitFlash(false);
            _hitFlashRemaining = 0f;
            _attackPulseRemaining = 0f;
            _jumpPulseRemaining = 0f;
            _lastWorldPosition = transform.position;
            if (visualRoot != null)
            {
                visualRoot.localPosition = _visualBasePosition;
                visualRoot.localRotation = _visualBaseRotation;
                visualRoot.localScale = _visualBaseScale;
            }

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.SetBool(DeadParameter, false);
                animator.SetFloat(SpeedParameter, 0f);
            }

            RefreshWorldStatusVisibility();
        }

        private void Update()
        {
            if (!_initialized || _owner == null)
            {
                return;
            }

            var delta = transform.position - _lastWorldPosition;
            delta.y = 0f;
            var speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
            _lastWorldPosition = transform.position;
            if (animator != null)
            {
                animator.SetFloat(SpeedParameter, Mathf.Clamp01(speed / 3.5f), 0.08f, Time.deltaTime);
            }
            else
            {
                UpdateProceduralMotion(speed);
            }

            if (_hitFlashRemaining > 0f)
            {
                _hitFlashRemaining -= Time.deltaTime;
                if (_hitFlashRemaining <= 0f)
                {
                    ApplyHitFlash(false);
                }
            }

            _attackPulseRemaining = Mathf.Max(0f, _attackPulseRemaining - Time.deltaTime);
            _jumpPulseRemaining = Mathf.Max(0f, _jumpPulseRemaining - Time.deltaTime);
            UpdateAirborneMotion();
            UpdateWorldStatus();
        }

        private void UpdateAirborneMotion()
        {
            if (visualRoot == null || animator == null || !_owner.IsAlive)
            {
                return;
            }

            var airborne = !_owner.IsGrounded && Mathf.Abs(_owner.VerticalVelocity) > 0.2f;
            var tilt = airborne ? Mathf.Clamp(-_owner.VerticalVelocity * 0.7f, -9f, 9f) : 0f;
            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation,
                _visualBaseRotation * Quaternion.Euler(tilt, 0f, 0f),
                1f - Mathf.Exp(-11f * Time.deltaTime));

            var pulse = _jumpPulseRemaining > 0f ? new Vector3(0.86f, 1.16f, 0.86f) : Vector3.one;
            visualRoot.localScale = Vector3.Lerp(
                visualRoot.localScale,
                Vector3.Scale(_visualBaseScale, pulse),
                14f * Time.deltaTime);
        }

        private void UpdateProceduralMotion(float speed)
        {
            if (visualRoot == null)
            {
                return;
            }

            if (!_owner.IsAlive)
            {
                visualRoot.localRotation = Quaternion.Slerp(
                    visualRoot.localRotation,
                    _visualBaseRotation * Quaternion.Euler(0f, 0f, 78f),
                    1f - Mathf.Exp(-10f * Time.deltaTime));
                visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, _visualBaseScale * 0.68f, 10f * Time.deltaTime);
                return;
            }

            var moving = speed > 0.15f;
            var bob = Mathf.Sin(Time.time * (moving ? 10f : 3.5f) + _owner.SquadSlot) * (moving ? 0.08f : 0.025f);
            visualRoot.localPosition = _visualBasePosition + Vector3.up * bob;
            var lean = moving ? Mathf.Clamp(speed * 1.8f, 0f, 10f) : 0f;
            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation,
                _visualBaseRotation * Quaternion.Euler(lean, 0f, 0f),
                1f - Mathf.Exp(-10f * Time.deltaTime));

            var pulse = _attackPulseRemaining > 0f ? 1f + Mathf.Sin(_attackPulseRemaining * 28f) * 0.12f : 1f;
            var squash = _hitFlashRemaining > 0f ? new Vector3(1.18f, 0.78f, 1.18f) : Vector3.one * pulse;
            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, Vector3.Scale(_visualBaseScale, squash), 16f * Time.deltaTime);
        }

        private void EnsureWorldStatus()
        {
            if (_healthRoot != null)
            {
                return;
            }

            var teamColor = _owner.Team switch
            {
                DemoTeam.Blue => new Color(0.12f, 0.65f, 1f),
                DemoTeam.Red => new Color(1f, 0.16f, 0.2f),
                _ => new Color(1f, 0.62f, 0.12f)
            };
            _healthBackgroundMaterial = CreateRuntimeMaterial(new Color(0.035f, 0.045f, 0.065f, 0.96f));
            _healthFillMaterial = CreateRuntimeMaterial(teamColor);
            _selectionMaterial = CreateRuntimeMaterial(new Color(1f, 0.84f, 0.18f));

            var healthObject = new GameObject("World_Health_Bar");
            healthObject.transform.SetParent(transform, false);
            healthObject.transform.localPosition = Vector3.up * (_owner.Role == DemoRole.Boss ? 2.8f : _owner.Role == DemoRole.Monster ? 2.1f : 2.25f);
            _healthRoot = healthObject.transform;

            var background = CreateStatusPrimitive("Background", PrimitiveType.Quad, _healthRoot, _healthBackgroundMaterial);
            background.transform.localScale = new Vector3(1.7f, 0.18f, 1f);
            var fill = CreateStatusPrimitive("Fill", PrimitiveType.Quad, _healthRoot, _healthFillMaterial);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.002f);
            fill.transform.localScale = new Vector3(1.58f, 0.1f, 1f);
            _healthFill = fill.transform;

            _selectionMarker = new GameObject("Controlled_Hunter_Marker");
            _selectionMarker.transform.SetParent(transform, false);
            _selectionMarker.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            CreateMarkerEdge("North", new Vector3(0f, 0f, 0.85f), new Vector3(1.3f, 0.035f, 0.09f));
            CreateMarkerEdge("South", new Vector3(0f, 0f, -0.85f), new Vector3(1.3f, 0.035f, 0.09f));
            CreateMarkerEdge("East", new Vector3(0.85f, 0f, 0f), new Vector3(0.09f, 0.035f, 1.3f));
            CreateMarkerEdge("West", new Vector3(-0.85f, 0f, 0f), new Vector3(0.09f, 0.035f, 1.3f));
        }

        private void CreateMarkerEdge(string name, Vector3 position, Vector3 scale)
        {
            var edge = CreateStatusPrimitive(name, PrimitiveType.Cube, _selectionMarker.transform, _selectionMaterial);
            edge.transform.localPosition = position;
            edge.transform.localScale = scale;
        }

        private void UpdateWorldStatus()
        {
            RefreshWorldStatusVisibility();
            if (_healthRoot == null || !_healthRoot.gameObject.activeSelf)
            {
                return;
            }

            var camera = Camera.main;
            if (camera != null)
            {
                _healthRoot.rotation = camera.transform.rotation;
            }

            var health = _owner.HealthNormalized;
            _healthFill.localScale = new Vector3(1.58f * health, 0.1f, 1f);
            _healthFill.localPosition = new Vector3(-0.79f * (1f - health), 0f, -0.002f);
        }

        private void RefreshWorldStatusVisibility()
        {
            var alive = _owner != null && _owner.IsHired && _owner.IsAlive;
            if (_healthRoot != null)
            {
                _healthRoot.gameObject.SetActive(alive);
            }

            if (_selectionMarker != null)
            {
                _selectionMarker.SetActive(alive && _selected);
            }
        }

        private void ApplyHitFlash(bool enabled)
        {
            for (var index = 0; index < _visualRenderers.Count; index++)
            {
                var renderer = _visualRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                if (!enabled)
                {
                    renderer.SetPropertyBlock(null);
                    continue;
                }

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorProperty, new Color(1f, 0.16f, 0.08f));
                _propertyBlock.SetColor(ColorProperty, new Color(1f, 0.16f, 0.08f));
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private Transform FindVisualRoot()
        {
            for (var index = 0; index < transform.childCount; index++)
            {
                var child = transform.GetChild(index);
                if (child.GetComponentInChildren<Renderer>(true) != null && !child.name.Contains("Pad"))
                {
                    return child;
                }
            }

            return null;
        }

        private static GameObject CreateStatusPrimitive(string name, PrimitiveType type, Transform parent, Material material)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            primitive.GetComponent<Renderer>().sharedMaterial = material;
            return primitive;
        }

        private static Material CreateRuntimeMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Sprites/Default");
            var material = new Material(shader)
            {
                color = color,
                hideFlags = HideFlags.DontSave
            };
            if (material.HasProperty(BaseColorProperty))
            {
                material.SetColor(BaseColorProperty, color);
            }

            if (material.HasProperty(ColorProperty))
            {
                material.SetColor(ColorProperty, color);
            }

            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static bool HasAnimatorParameter(Animator targetAnimator, int parameterHash)
        {
            if (targetAnimator == null)
            {
                return false;
            }

            var parameters = targetAnimator.parameters;
            for (var index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].nameHash == parameterHash)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDestroy()
        {
            if (_healthBackgroundMaterial != null) Destroy(_healthBackgroundMaterial);
            if (_healthFillMaterial != null) Destroy(_healthFillMaterial);
            if (_selectionMaterial != null) Destroy(_selectionMaterial);
        }
    }
}
