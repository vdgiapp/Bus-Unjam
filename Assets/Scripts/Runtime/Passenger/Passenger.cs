using DG.Tweening;
using UnityEngine;

namespace VehicleUnjam
{
    public class Passenger : MonoBehaviour
    {
        // Data
        [HideInInspector] public PassengerData data;

        // View
        [SerializeField] private Animator _animator;
        [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
        [SerializeField] private int _specifiedColorMaterialIndex;
    
        private MaterialPropertyBlock _mpbColor;
        
        private bool _isShaking = false;

        private Tween _moveTween;
        private Tween _moveRotationTween;
        private Tween _moveResetRotationTween;
        private Tween _shakeTween;

        private void Awake()
        {
            _mpbColor = new MaterialPropertyBlock();
            _skinnedMeshRenderer.GetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _moveRotationTween?.Kill();
            _moveRotationTween = null;
            _moveResetRotationTween?.Kill();
            _moveResetRotationTween = null;
            _shakeTween?.Kill();
            _shakeTween = null;
        }

        public void SetColor(Color color)
        {
            _mpbColor.SetColor(Constants.SHADER_COLOR_ID, color);
            _skinnedMeshRenderer.SetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }

        public void SetRunningAnimation(bool running)
        {
            _animator.SetBool(Constants.ANIMATOR_IS_RUNNING_ID, running);
        }
        
        public void TriggerSittingAnimation(bool isRunning = true)
        {
            if (!isRunning) return;
            _animator.SetTrigger(Constants.ANIMATOR_IS_SITTING_ID);
        }

        public Tween MoveTo(Vector3 worldPosition, float duration, Ease ease = Ease.Linear)
        {
            Vector3 target = new(worldPosition.x, transform.position.y, worldPosition.z);
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                _moveRotationTween?.Kill();
                _moveRotationTween = transform.DORotateQuaternion(lookRot, Constants.PASSENGER_ROTATE_DURATION).SetEase(Ease.OutQuad);
                _moveRotationTween.SetAutoKill(true);
            }
            _moveTween?.Kill();
            _moveTween = transform.DOMove(target, duration).SetEase(ease);
            _moveTween.onComplete += ResetRotation;
            _moveTween.SetAutoKill(true);
            return _moveTween;
        }

        public void Shake()
        {
            if (_isShaking) return;
            _isShaking = true;
            float s = Constants.PASSENGER_SHAKE_STRENGTH;
            float d = Constants.PASSENGER_SHAKE_DURATION;
            int v = Constants.PASSENGER_SHAKE_VIBRATO;
            _shakeTween?.Kill();
            _shakeTween = DOTween.Sequence()
                .Append(transform.DOLocalRotate(new Vector3(0, -s, 0), d / (v * 2)).SetEase(Ease.InOutQuad))
                .Append(transform.DOLocalRotate(new Vector3(0, s, 0), d / v).SetEase(Ease.InOutQuad))
                .Append(transform.DOLocalRotate(new Vector3(0, -s, 0), d / v).SetEase(Ease.InOutQuad))
                .Append(transform.DOLocalRotate(Vector3.zero, d / (s * 2)).SetEase(Ease.InOutQuad));
            _shakeTween.onComplete += ResetShaking;
            _shakeTween.SetAutoKill(true);
        }

        private void ResetRotation()
        {
            _moveResetRotationTween?.Kill();
            _moveResetRotationTween = transform.DOLocalRotateQuaternion(Quaternion.identity, Constants.PASSENGER_ROTATE_DURATION);
            _moveResetRotationTween.SetAutoKill(true);
        }

        private void ResetShaking()
        {
            _isShaking = false;
        }
    }
}