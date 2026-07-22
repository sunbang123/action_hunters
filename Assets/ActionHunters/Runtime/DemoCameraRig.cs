using UnityEngine;

namespace ActionHunters.Runtime
{
    public sealed class DemoCameraRig : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 17f, -13f);
        [SerializeField, Min(0.01f)] private float followSharpness = 6f;
        [SerializeField, Min(0.01f)] private float lookSharpness = 8f;

        private Transform _target;
        private float _impulseRemaining;
        private float _impulseDuration;
        private float _impulseAmplitude;

        public void AddImpulse(float amplitude, float duration = 0.16f)
        {
            _impulseAmplitude = Mathf.Max(_impulseAmplitude, amplitude);
            _impulseDuration = Mathf.Max(0.01f, duration);
            _impulseRemaining = _impulseDuration;
        }

        public void SetTarget(Transform target, bool snap = false)
        {
            _target = target;
            if (snap && _target != null)
            {
                transform.position = _target.position + offset;
                transform.LookAt(_target.position + Vector3.up * 1.2f);
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var positionBlend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            var rotationBlend = 1f - Mathf.Exp(-lookSharpness * Time.deltaTime);
            var desiredPosition = _target.position + offset;
            var desiredRotation = Quaternion.LookRotation(_target.position + Vector3.up * 1.2f - desiredPosition);
            var shakeOffset = Vector3.zero;
            if (_impulseRemaining > 0f)
            {
                _impulseRemaining = Mathf.Max(0f, _impulseRemaining - Time.deltaTime);
                var strength = _impulseAmplitude * (_impulseRemaining / _impulseDuration);
                shakeOffset = Random.insideUnitSphere * strength;
                shakeOffset.y *= 0.45f;
                if (_impulseRemaining <= 0f)
                {
                    _impulseAmplitude = 0f;
                }
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionBlend) + shakeOffset;
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);
        }
    }
}
