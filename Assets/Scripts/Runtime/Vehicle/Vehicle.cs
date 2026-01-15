using DG.Tweening;
using UnityEngine;

namespace VehicleUnjam
{
    public class Vehicle : MonoBehaviour
    {
        // Data
        public VehicleData data { get; private set; }
        public bool[] seatOccupied { get; private set; } = { false, false, false };
        public Passenger[] reservedPassengers { get; private set; } = { null, null, null };
        
        // View
        [SerializeField] protected Animator _animator;
        [SerializeField] protected MeshRenderer _meshRenderer;
        [SerializeField] protected int _specifiedColorMaterialIndex;
        [SerializeField] protected Transform _doorTransform;
        [SerializeField] protected Transform[] _seatTransforms = new Transform[Constants.VEHICLE_SEAT_SLOTS];
        
        protected MaterialPropertyBlock _mpbColor;

        protected Tween _moveLocalTween;
        protected Tween _moveTween;
        
        protected void Awake()
        {
            _mpbColor = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }

        protected void OnDestroy()
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

        public bool SetSeatOccupied(int seatIndex, bool isOccupied)
        {
            if (seatIndex < 0 || seatIndex >= seatOccupied.Length) return false;
            seatOccupied[seatIndex] = isOccupied;
            return true;
        }
        
        public bool SetReservedPassenger(int seatIndex, Passenger passenger)
        {
            if (seatIndex < 0 || seatIndex >= reservedPassengers.Length) return false;
            reservedPassengers[seatIndex] = passenger;
            return true;
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