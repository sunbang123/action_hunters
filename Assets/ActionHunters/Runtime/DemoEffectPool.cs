using System.Collections.Generic;
using UnityEngine;

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
        private ParticleSystem[] _particles;

        public bool IsPlaying => gameObject.activeSelf;

        public void Prepare()
        {
            CacheParticles();
            gameObject.SetActive(false);
        }

        public void Play(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);
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
        }

        private void Update()
        {
            if (_particles == null)
            {
                gameObject.SetActive(false);
                return;
            }

            for (var index = 0; index < _particles.Length; index++)
            {
                if (_particles[index] != null && _particles[index].IsAlive(true))
                {
                    return;
                }
            }

            gameObject.SetActive(false);
        }

        private void CacheParticles()
        {
            _particles = GetComponentsInChildren<ParticleSystem>(true);
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
