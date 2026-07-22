using UnityEngine;

namespace ActionHunters.Runtime
{
    public enum DemoWorldGimmickKind
    {
        WaterShield,
        FireProjectile,
        WindBeam,
        EarthAoe,
        JumpPad,
        PipeLauncher,
        TeamFlag
    }

    [DisallowMultipleComponent]
    public sealed class DemoWorldGimmick : MonoBehaviour
    {
        [SerializeField] private DemoWorldGimmickKind kind;
        [SerializeField] private DemoTeam owningTeam;
        [SerializeField] private BoxCollider trigger;
        [SerializeField] private Transform readyVisual;
        [SerializeField] private Light readyLight;
        [SerializeField] private GameObject activationEffectPrefab;
        [SerializeField, Min(0.1f)] private float cooldown = 8f;
        [SerializeField, Min(0f)] private float power = 30f;
        [SerializeField, Min(0f)] private float radius = 5f;
        [SerializeField, Min(0f)] private float launchHeight = 3.5f;
        [SerializeField, Min(0f)] private float launchSpeed = 7f;
        [SerializeField] private Vector3 launchDirection = Vector3.forward;
        [SerializeField, Min(0f)] private float activationDelay;

        private DemoMatchController _match;
        private DemoPooledEffect _activationEffect;
        private GameObject _transientActivationEffect;
        private DemoCombatant _pendingActor;
        private float _pendingRemaining;
        private float _cooldownRemaining;
        private Vector3 _readyScale = Vector3.one;
        private Vector3 _readyPosition;
        private bool _visualStateInitialized;

        public DemoWorldGimmickKind Kind => kind;
        public DemoTeam OwningTeam => owningTeam;
        public BoxCollider Trigger => trigger;
        public bool IsReady => _cooldownRemaining <= 0f && _pendingActor == null;
        public float CooldownRemaining => _cooldownRemaining;
        public bool HasActivationEffect => activationEffectPrefab != null;

        public void Configure(
            DemoWorldGimmickKind configuredKind,
            DemoTeam configuredTeam,
            BoxCollider configuredTrigger,
            Transform configuredReadyVisual,
            Light configuredReadyLight,
            GameObject configuredActivationEffect,
            float configuredCooldown,
            float configuredPower,
            float configuredRadius,
            float configuredLaunchHeight,
            float configuredLaunchSpeed,
            Vector3 configuredLaunchDirection,
            float configuredActivationDelay = 0f)
        {
            kind = configuredKind;
            owningTeam = configuredTeam;
            trigger = configuredTrigger;
            readyVisual = configuredReadyVisual;
            readyLight = configuredReadyLight;
            activationEffectPrefab = configuredActivationEffect;
            cooldown = Mathf.Max(0.1f, configuredCooldown);
            power = Mathf.Max(0f, configuredPower);
            radius = Mathf.Max(0f, configuredRadius);
            launchHeight = Mathf.Max(0f, configuredLaunchHeight);
            launchSpeed = Mathf.Max(0f, configuredLaunchSpeed);
            launchDirection = configuredLaunchDirection;
            activationDelay = Mathf.Max(0f, configuredActivationDelay);
        }

        public void Initialize(DemoMatchController match)
        {
            _match = match;
            CacheVisualState();
            RefreshReadyVisual();
        }

        public void ResetGimmick()
        {
            _pendingActor = null;
            _pendingRemaining = 0f;
            _cooldownRemaining = 0f;
            if (_activationEffect != null && _activationEffect.gameObject.activeSelf)
            {
                _activationEffect.gameObject.SetActive(false);
            }

            if (_transientActivationEffect != null)
            {
                Destroy(_transientActivationEffect);
                _transientActivationEffect = null;
            }

            RefreshReadyVisual();
        }

        public bool TryActivate(DemoCombatant actor)
        {
            if (!IsReady || actor == null || actor.Team == DemoTeam.Neutral || !actor.CanUseWorldGimmicks || _match == null)
            {
                return false;
            }

            if (kind is DemoWorldGimmickKind.FireProjectile or DemoWorldGimmickKind.EarthAoe or DemoWorldGimmickKind.TeamFlag &&
                !_match.AllowsCombat)
            {
                return false;
            }

            var capturedEnemyFlag = false;
            DemoCombatant fireTarget = null;
            switch (kind)
            {
                case DemoWorldGimmickKind.WaterShield:
                    actor.Heal(power);
                    actor.GrantShield(radius);
                    break;
                case DemoWorldGimmickKind.FireProjectile:
                    fireTarget = _match.FindClosestHostile(actor, radius);
                    if (!actor.TryElementalStrike(power, radius))
                    {
                        return false;
                    }

                    break;
                case DemoWorldGimmickKind.WindBeam:
                case DemoWorldGimmickKind.JumpPad:
                case DemoWorldGimmickKind.PipeLauncher:
                    actor.LaunchToHeight(launchHeight, ResolveLaunchDirection(actor) * launchSpeed);
                    break;
                case DemoWorldGimmickKind.EarthAoe:
                    ActivateEarthPulse(actor);
                    break;
                case DemoWorldGimmickKind.TeamFlag:
                    if (actor.Team == DemoTeam.Neutral || owningTeam == DemoTeam.Neutral)
                    {
                        return false;
                    }

                    capturedEnemyFlag = actor.Team != owningTeam;
                    if (!capturedEnemyFlag)
                    {
                        actor.Heal(power);
                        actor.GrantShield(radius);
                    }
                    break;
            }

            _cooldownRemaining = cooldown;
            PlayActivationEffect(actor, fireTarget);
            RefreshReadyVisual();
            _match.OnWorldGimmickActivated(this, actor, capturedEnemyFlag);
            return true;
        }

        private void Awake()
        {
            if (trigger == null)
            {
                var colliders = GetComponents<BoxCollider>();
                for (var index = 0; index < colliders.Length; index++)
                {
                    if (colliders[index].isTrigger)
                    {
                        trigger = colliders[index];
                        break;
                    }
                }
            }

            CacheVisualState();
        }

        private void Start()
        {
            if (_match == null)
            {
                Initialize(FindFirstObjectByType<DemoMatchController>());
            }
        }

        private void Update()
        {
            if (_pendingActor != null)
            {
                _pendingRemaining -= Time.deltaTime;
                if (_pendingRemaining <= 0f)
                {
                    var actor = _pendingActor;
                    _pendingActor = null;
                    if (!TryActivate(actor))
                    {
                        RefreshReadyVisual();
                    }
                }
            }

            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);
                if (_cooldownRemaining <= 0f)
                {
                    RefreshReadyVisual();
                }
            }

            if (readyVisual != null && IsReady)
            {
                readyVisual.Rotate(Vector3.up, 55f * Time.deltaTime, Space.Self);
                readyVisual.localPosition = _readyPosition + Vector3.up * (Mathf.Sin(Time.time * 3.2f + transform.position.x) * 0.1f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var actor = other.GetComponentInParent<DemoCombatant>();
            if (actor == null || !IsReady)
            {
                return;
            }

            if (activationDelay > 0f)
            {
                _pendingActor = actor;
                _pendingRemaining = activationDelay;
                RefreshReadyVisual();
                return;
            }

            TryActivate(actor);
        }

        private void ActivateEarthPulse(DemoCombatant actor)
        {
            var hostiles = _match.GetHostilesInRange(actor, radius);
            for (var index = 0; index < hostiles.Count; index++)
            {
                var hostile = hostiles[index];
                if (Mathf.Abs(hostile.transform.position.y - actor.transform.position.y) > 2.5f)
                {
                    continue;
                }

                var direction = hostile.transform.position - transform.position;
                direction.y = 0f;
                hostile.ReceiveDamage(power, actor);
                hostile.LaunchToHeight(1.3f, direction.sqrMagnitude > 0.01f ? direction.normalized * 3.5f : Vector3.zero);
            }
        }

        private Vector3 ResolveLaunchDirection(DemoCombatant actor)
        {
            var direction = launchDirection;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = actor.transform.forward;
            }

            direction.y = 0f;
            return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.zero;
        }

        private void PlayActivationEffect(DemoCombatant actor, DemoCombatant fireTarget)
        {
            if (activationEffectPrefab == null)
            {
                return;
            }

            var rotation = ResolveEffectRotation(actor, fireTarget);
            if (kind == DemoWorldGimmickKind.FireProjectile && fireTarget != null)
            {
                var source = actor.transform.position + Vector3.up * 0.9f + rotation * Vector3.forward * 0.6f;
                var destination = fireTarget.transform.position + Vector3.up * 0.75f;
                if (TryPlayNativeProjectile(source, destination))
                {
                    return;
                }
            }

            if (_activationEffect == null)
            {
                var instance = Instantiate(activationEffectPrefab, transform);
                instance.name = activationEffectPrefab.name + "_GimmickEffect";
                var colliders = instance.GetComponentsInChildren<Collider>(true);
                for (var index = 0; index < colliders.Length; index++)
                {
                    colliders[index].enabled = false;
                }

                _activationEffect = instance.GetComponent<DemoPooledEffect>();
                if (_activationEffect == null)
                {
                    _activationEffect = instance.AddComponent<DemoPooledEffect>();
                }

                _activationEffect.Prepare();
            }

            if (kind == DemoWorldGimmickKind.WaterShield)
            {
                _activationEffect.PlayFollowing(actor.transform, Vector3.up * 0.8f, rotation);
                return;
            }

            if (kind == DemoWorldGimmickKind.FireProjectile && fireTarget != null)
            {
                var source = actor.transform.position + Vector3.up * 0.9f + rotation * Vector3.forward * 0.6f;
                var destination = fireTarget.transform.position + Vector3.up * 0.75f;
                var travelTime = Mathf.Clamp(Vector3.Distance(source, destination) / 16f, 0.25f, 0.8f);
                _activationEffect.PlayMoving(source, destination, fireTarget.transform, Vector3.up * 0.75f, travelTime, rotation);
                return;
            }

            _activationEffect.Play(transform.position + Vector3.up * 0.55f, rotation);
        }

        private bool TryPlayNativeProjectile(Vector3 source, Vector3 destination)
        {
            GameObject instance = null;
            try
            {
                instance = Instantiate(activationEffectPrefab);
                instance.name = activationEffectPrefab.name + "_GimmickProjectile";
                var colliders = instance.GetComponentsInChildren<Collider>(true);
                for (var index = 0; index < colliders.Length; index++)
                {
                    colliders[index].enabled = false;
                }

                var behaviours = instance.GetComponents<MonoBehaviour>();
                for (var index = 0; index < behaviours.Length; index++)
                {
                    var behaviour = behaviours[index];
                    if (behaviour == null || behaviour.GetType().FullName != "PixPlays.ElementalVFX.ProjectileVfx")
                    {
                        continue;
                    }

                    var playMethod = behaviour.GetType().GetMethod("Play", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var parameters = playMethod?.GetParameters();
                    if (parameters == null || parameters.Length != 1)
                    {
                        break;
                    }

                    var data = System.Activator.CreateInstance(
                        parameters[0].ParameterType,
                        new object[] { source, destination, 0.65f, 1f });
                    _transientActivationEffect = instance;
                    playMethod.Invoke(behaviour, new[] { data });
                    return true;
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[Action Hunters] Native projectile VFX fallback: {exception.GetBaseException().Message}", this);
            }

            if (instance != null)
            {
                Destroy(instance);
            }

            return false;
        }

        private Quaternion ResolveEffectRotation(DemoCombatant actor, DemoCombatant fireTarget)
        {
            var direction = ResolveLaunchDirection(actor);
            if (kind == DemoWorldGimmickKind.FireProjectile && fireTarget != null)
            {
                direction = fireTarget.transform.position - actor.transform.position;
                direction.y = 0f;
            }

            return direction.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direction.normalized) : transform.rotation;
        }

        private void CacheVisualState()
        {
            if (_visualStateInitialized || readyVisual == null)
            {
                return;
            }

            _readyScale = readyVisual.localScale;
            _readyPosition = readyVisual.localPosition;
            _visualStateInitialized = true;
        }

        private void RefreshReadyVisual()
        {
            CacheVisualState();
            if (readyVisual != null)
            {
                readyVisual.gameObject.SetActive(IsReady);
                if (IsReady)
                {
                    readyVisual.localScale = _readyScale;
                    readyVisual.localPosition = _readyPosition;
                }
            }

            if (readyLight != null)
            {
                readyLight.enabled = IsReady;
            }
        }
    }
}
