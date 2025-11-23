using UnityEngine;

namespace VehicleUnjam
{
    [DisallowMultipleComponent]
    public class Bounce : MonoBehaviour
    {
        public float frequency = 4f;
        public float amplitudeY = 0.005f;
        public float amplitudeXZ = 0.0025f;
        public bool inverseXZ = true;
        public float phaseOffset = 0f;

        private Vector3 _originalScale;
        private float _phase;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _phase = phaseOffset;
        }

        private void Update()
        {
            _phase += Time.deltaTime * frequency * Mathf.PI * 2f; // convert freq (Hz) -> rad/s
            float s = Mathf.Sin(_phase); // in [-1,1]

            // multiplicative factors centered at 1:
            float factorY = 1f + s * amplitudeY;
            float factorXZ = 1f + (inverseXZ ? -s : s) * amplitudeXZ;

            transform.localScale = new Vector3(
                _originalScale.x * factorXZ,
                _originalScale.y * factorY,
                _originalScale.z * factorXZ
            );
        }
    }
}