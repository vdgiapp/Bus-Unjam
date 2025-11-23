using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace VehicleUnjam
{
    public class Passenger : MonoBehaviour
    {
        [HideInInspector] public PassengerData data;

        [SerializeField] private Animator _animator;
        [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
        [SerializeField] private int _specifiedColorMaterialIndex;
    
        private MaterialPropertyBlock _mpbColor;
        
        private bool _isShaking = false;

        private void Awake()
        {
            _mpbColor = new MaterialPropertyBlock();
            _skinnedMeshRenderer.GetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
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

        public async UniTask MoveTo(Vector3 worldPosition, float duration, Ease ease = Ease.Linear)
        {
            Vector3 target = new(worldPosition.x, transform.position.y, worldPosition.z);
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                transform.DORotateQuaternion(lookRot, Constants.PASSENGER_ROTATE_DURATION).SetEase(Ease.OutQuad).ToUniTask().Forget();
            }
            await transform.DOMove(target, duration).SetEase(ease).ToUniTask();
            transform.DOLocalRotateQuaternion(Quaternion.identity, Constants.PASSENGER_ROTATE_DURATION).ToUniTask().Forget();
        }

        public async UniTask Shake()
        {
            if (_isShaking) return;
            _isShaking = true;
            float s = Constants.PASSENGER_SHAKE_STRENGTH;
            float d = Constants.PASSENGER_SHAKE_DURATION;
            int v = Constants.PASSENGER_SHAKE_VIBRATO;
            await DOTween.Sequence()
                .Append(transform.DOLocalRotate(new Vector3(0, -s, 0), d / (v * 2)).SetEase(Ease.InOutQuad))
                .Append(transform.DOLocalRotate(new Vector3(0, s, 0), d / v).SetEase(Ease.InOutQuad))
                .Append(transform.DOLocalRotate(new Vector3(0, -s, 0), d / v).SetEase(Ease.InOutQuad))
                .Append(transform.DOLocalRotate(Vector3.zero, d / (s * 2)).SetEase(Ease.InOutQuad))
            .ToUniTask();
            _isShaking = false;
        }
    }

    [Serializable]
    public class PassengerData
    {
        public eColorType colorType;
        public ePassengerType passengerType;
    }
    
    [Serializable]
    public enum ePassengerType
    {
        Normal = 0,
        Hidden,
        Iced
    }
}