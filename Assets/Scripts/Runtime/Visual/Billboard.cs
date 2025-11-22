using UnityEngine;

namespace BusUnjam
{
    public class Billboard : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;

        private void Start()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            // Make the indicator face the camera
            transform.forward = _mainCamera.transform.forward;
        }
    }
}