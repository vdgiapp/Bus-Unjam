using DG.Tweening;
using UnityEngine;

namespace VehicleUnjam
{
    public class Vehicle : MonoBehaviour
    {
        // Data
        public VehicleData data { get; private set; }
        
        // View
        [SerializeField] private Animator _animator;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private int _specifiedColorMaterialIndex;
        [SerializeField] private Transform _doorTransform;
        [SerializeField] private Transform[] _seatTransforms = new Transform[Constants.VEHICLE_SEAT_SLOTS];
        
        private MaterialPropertyBlock _mpbColor;

        private Tween _moveLocalTween;
        private Tween _moveTween;
        
        private void Awake()
        {
            _mpbColor = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _moveLocalTween?.Kill();
            _moveLocalTween = null;
        }

        public void InitData(VehicleData initData)
        {
            data = initData;
        }

        public void SetColor(Color color)
        {
            _mpbColor.SetColor(Constants.SHADER_COLOR_ID, color);
            _meshRenderer.SetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }
        
        public Tween MoveTo(Vector3 worldPosition, float duration, Ease ease = Ease.Linear)
        {
            _moveTween?.Kill();
            _moveTween = transform.DOMove(worldPosition, duration).SetEase(ease);
            _moveTween.SetAutoKill(true);
            return _moveTween; 
        }

        public Tween MoveLocalTo(Vector3 position, float duration, Ease ease = Ease.Linear)
        {
            _moveLocalTween?.Kill();
            _moveLocalTween = transform.DOLocalMove(position, duration).SetEase(ease);
            _moveTween.SetAutoKill(true);
            return _moveLocalTween;
        }

        public Transform GetSeatTransformAtIndex(int index)
        {
            if (index < 0 || index >= _seatTransforms.Length) return null;
            return _seatTransforms[index];
        }

        public Transform GetDoorTransform()
        {
            return _doorTransform;
        }
    }
}