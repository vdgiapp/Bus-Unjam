using System;
using DG.Tweening;
using UnityEngine;

namespace VehicleUnjam
{
    public class Passenger : MonoBehaviour
    {
        public static event Action<Passenger, ePassengerState, ePassengerState> PassengerStateChanged;
        
        // Data
        public PassengerData data { get; protected set; }
        public ePassengerState state { get; protected set; } = ePassengerState.None;

        // View
        [SerializeField] protected Animator _animator;
        [SerializeField] protected SkinnedMeshRenderer _skinnedMeshRenderer;
        [SerializeField] protected int _specifiedColorMaterialIndex;
    
        protected MaterialPropertyBlock _mpbColor;
        protected bool _isShaking = false;
        
        protected Sequence _moveSequence;

        protected virtual void Awake()
        {
            _mpbColor = new MaterialPropertyBlock();
            _skinnedMeshRenderer.GetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }

        public void InitData(PassengerData initData)
        {
            data = initData;
        }
        
        public void SetState(ePassengerState newState)
        {
            if (state == newState) return;
            ePassengerState oldState = state;
            state = newState;
            PassengerStateChanged?.Invoke(this, oldState, newState);
        }
        
        public void SetStateWithoutNotify(ePassengerState newState)
        {
            state = newState;
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
            _animator.SetTrigger(Constants.ANIMATOR_IS_SITTING_ID);
        }

        public Sequence MoveTo(Vector3 worldPosition, float speed, Ease ease = Ease.Linear)
        {
            _moveSequence.Kill();
            _moveSequence = DOTween.Sequence();
            float duration = Vector3.Distance(transform.position, worldPosition) / speed;
            _moveSequence.AppendCallback(() => FaceTo(worldPosition));
            _moveSequence.Join(transform.DOMove(worldPosition, duration).SetEase(ease));
            _moveSequence.onComplete += ResetRotation;
            _moveSequence.SetAutoKill(true);
            return _moveSequence;
        }
        
        public Sequence MovePath(Vector3[] path, float speed, Ease ease = Ease.Linear)
        {
            _moveSequence.Kill();
            _moveSequence = DOTween.Sequence();
            Vector3 previousPosition = transform.position;
            foreach (var pathPosition in path)
            {
                float duration = Vector3.Distance(previousPosition, pathPosition) / speed;
                _moveSequence.AppendCallback(() => FaceTo(pathPosition));
                _moveSequence.Append(transform.DOMove(pathPosition, duration).SetEase(ease));
                previousPosition = pathPosition;
            }
            _moveSequence.onComplete += ResetRotation;
            _moveSequence.SetAutoKill(true);
            return _moveSequence;
        }

        public void Shake()
        {
            if (_isShaking) return;
            _isShaking = true;
            float s = Constants.PASSENGER_SHAKE_STRENGTH;
            float d = Constants.PASSENGER_SHAKE_DURATION;
            int v = Constants.PASSENGER_SHAKE_VIBRATO;
            Sequence seq = DOTween.Sequence()
                .Append(transform.DOLocalRotate(new Vector3(0, -s, 0), d / (v * 2)).SetEase(Ease.InOutQuad))
                .Append(transform.DOLocalRotate(new Vector3(0, s, 0), d / v).SetEase(Ease.InOutQuad))
                .Append(transform.DOLocalRotate(new Vector3(0, -s, 0), d / v).SetEase(Ease.InOutQuad))
                .Append(transform.DOLocalRotate(Vector3.zero, d / (s * 2)).SetEase(Ease.InOutQuad));
            seq.onComplete += ResetShaking;
            seq.SetAutoKill(true);
        }

        protected void FaceTo(Vector3 target)
        {
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                transform.DORotateQuaternion(lookRot, Constants.PASSENGER_ROTATE_DURATION).SetEase(Ease.OutQuad);
            }
        }

        protected void ResetRotation()
        {
            transform.DOLocalRotateQuaternion(Quaternion.identity, Constants.PASSENGER_ROTATE_DURATION);
        }

        protected void ResetShaking()
        {
            _isShaking = false;
        }
    }
    
    public enum ePassengerState
    {
        None = -1, // Didn't spawn
        Idle,
        MovingToFirstRow,
        FirstRow,
        MovingToQueue,
        Waiting,
        MovingToVehicle,
        Sitting,
        Inactive, // Destroyed
    }

}