using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace ActionHunters.Runtime
{
    public sealed class DemoEffectPool : MonoBehaviour
    {
        [SerializeField] private List<GameObject> effectPrefabs = new List<GameObject>();
        [SerializeField, Range(1, 12)] private int instancesPerEffect = 8;

        private readonly Dictionary<GameObject, List<DemoPooledEffect>> _pools =
            new Dictionary<GameObject, List<DemoPooledEffect>>();
        private readonly Dictionary<GameObject, int> _nextIndices = new Dictionary<GameObject, int>();

        public void Configure(IReadOnlyList<GameObject> configuredPrefabs)
        {
            effectPrefabs = new List<GameObject>(configuredPrefabs);
        }

        private void Awake()
        {
            for (var index = 0; index < effectPrefabs.Count; index++)
            {
                if (effectPrefabs[index] != null)
                {
                    EnsurePool(effectPrefabs[index]);
                }
            }
        }

        public void Play(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return;
            }

            var pool = EnsurePool(prefab);
            var nextIndex = _nextIndices[prefab];
            for (var offset = 0; offset < pool.Count; offset++)
            {
                var candidateIndex = (nextIndex + offset) % pool.Count;
                if (pool[candidateIndex].IsPlaying)
                {
                    continue;
                }

                _nextIndices[prefab] = (candidateIndex + 1) % pool.Count;
                pool[candidateIndex].Play(position, rotation);
                return;
            }

            pool[nextIndex].Play(position, rotation);
            _nextIndices[prefab] = (nextIndex + 1) % pool.Count;
        }

        private List<DemoPooledEffect> EnsurePool(GameObject prefab)
        {
            if (prefab == null)
            {
                return new List<DemoPooledEffect>();
            }

            if (_pools.TryGetValue(prefab, out var existingPool))
            {
                return existingPool;
            }

            var pool = new List<DemoPooledEffect>();
            _pools[prefab] = pool;
            _nextIndices[prefab] = 0;
            var poolRoot = new GameObject(prefab.name + "_Pool");
            poolRoot.transform.SetParent(transform, false);
            for (var index = 0; index < instancesPerEffect; index++)
            {
                var instance = Instantiate(prefab, poolRoot.transform);
                instance.name = $"{prefab.name}_{index:00}";
                var pooledEffect = instance.GetComponent<DemoPooledEffect>();
                if (pooledEffect == null)
                {
                    pooledEffect = instance.AddComponent<DemoPooledEffect>();
                }

                pooledEffect.Prepare();
                pool.Add(pooledEffect);
            }

            return pool;
        }
    }

    public sealed class DemoPooledEffect : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float maxLifetime = 6f;

        private ParticleSystem[] _particles;
        private PlayableDirector[] _directors;
        private Animation[] _animations;
        private float _remainingLifetime;
        private Transform _followTarget;
        private Vector3 _followOffset;
        private bool _isMoving;
        private Vector3 _moveStart;
        private Vector3 _moveEnd;
        private Transform _moveTarget;
        private Vector3 _moveTargetOffset;
        private float _moveDuration;
        private float _moveElapsed;

        public bool IsPlaying => gameObject.activeSelf;

        public void Prepare()
        {
            CacheParticles();
            _remainingLifetime = 0f;
            ClearMotion();
            gameObject.SetActive(false);
        }

        public void Play(Vector3 position, Quaternion rotation)
        {
            ClearMotion();
            BeginPlay(position, rotation);
        }

        public void PlayFollowing(Transform target, Vector3 offset, Quaternion rotation)
        {
            ClearMotion();
            _followTarget = target;
            _followOffset = offset;
            BeginPlay(target != null ? target.position + offset : transform.position, rotation);
        }

        public void PlayMoving(
            Vector3 start,
            Vector3 end,
            Transform target,
            Vector3 targetOffset,
            float duration,
            Quaternion rotation)
        {
            ClearMotion();
            _isMoving = duration > 0f;
            _moveStart = start;
            _moveEnd = end;
            _moveTarget = target;
            _moveTargetOffset = targetOffset;
            _moveDuration = Mathf.Max(0.01f, duration);
            BeginPlay(start, rotation);
        }

        private void BeginPlay(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);
            _remainingLifetime = maxLifetime;
            if (_particles == null || _particles.Length == 0)
            {
                CacheParticles();
            }

            for (var index = 0; index < _particles.Length; index++)
            {
                if (_particles[index] == null)
                {
                    continue;
                }

                _particles[index].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _particles[index].Play(true);
            }

            for (var index = 0; index < _directors.Length; index++)
            {
                if (_directors[index] == null)
                {
                    continue;
                }

                _directors[index].time = 0d;
                _directors[index].Evaluate();
                _directors[index].Play();
            }

            for (var index = 0; index < _animations.Length; index++)
            {
                if (_animations[index] != null)
                {
                    _animations[index].Rewind();
                    _animations[index].Play();
                }
            }
        }

        private void Update()
        {
            if (_followTarget != null)
            {
                transform.position = _followTarget.position + _followOffset;
            }
            else if (_isMoving)
            {
                _moveElapsed += Time.deltaTime;
                var end = _moveTarget != null ? _moveTarget.position + _moveTargetOffset : _moveEnd;
                transform.position = Vector3.Lerp(_moveStart, end, Mathf.Clamp01(_moveElapsed / _moveDuration));
                if (_moveElapsed >= _moveDuration)
                {
                    _isMoving = false;
                }
            }

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                StopAndDeactivate();
                return;
            }

            if (_particles == null)
            {
                StopAndDeactivate();
                return;
            }

            for (var index = 0; index < _particles.Length; index++)
            {
                if (_particles[index] != null && _particles[index].IsAlive(true))
                {
                    return;
                }
            }

            StopAndDeactivate();
        }

        private void StopAndDeactivate()
        {
            ClearMotion();
            if (_particles != null)
            {
                for (var index = 0; index < _particles.Length; index++)
                {
                    if (_particles[index] != null)
                    {
                        _particles[index].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }

            if (_directors != null)
            {
                for (var index = 0; index < _directors.Length; index++)
                {
                    if (_directors[index] != null)
                    {
                        _directors[index].Stop();
                    }
                }
            }

            gameObject.SetActive(false);
        }

        private void ClearMotion()
        {
            _followTarget = null;
            _followOffset = Vector3.zero;
            _isMoving = false;
            _moveStart = Vector3.zero;
            _moveEnd = Vector3.zero;
            _moveTarget = null;
            _moveTargetOffset = Vector3.zero;
            _moveDuration = 0f;
            _moveElapsed = 0f;
        }

        private void CacheParticles()
        {
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            _directors = GetComponentsInChildren<PlayableDirector>(true);
            _animations = GetComponentsInChildren<Animation>(true);
            for (var index = 0; index < _particles.Length; index++)
            {
                if (_particles[index] == null)
                {
                    continue;
                }

                var main = _particles[index].main;
                main.stopAction = ParticleSystemStopAction.None;
            }
        }
    }
}
