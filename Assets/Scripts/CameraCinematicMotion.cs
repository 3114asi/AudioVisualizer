using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class CameraCinematicMotion : MonoBehaviour
    {
        public float loopDuration = 10f;
        public float pushInDistance = 0.35f;
        public float verticalDrift = 0.08f;
        public float rollDegrees = 0.25f;

        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        private void Update()
        {
            float t = Mathf.Repeat(Time.time / Mathf.Max(0.1f, loopDuration), 1f);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.position = startPosition + transform.forward * pushInDistance * eased + Vector3.up * Mathf.Sin(t * Mathf.PI * 2f) * verticalDrift;
            transform.rotation = startRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * rollDegrees);
        }
    }
}
