using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace BusUnjam
{
    public class Passenger : MonoBehaviour
    {
        private const float ROTATE_DURATION = 0.2f;
        
        [HideInInspector] public PassengerData data;

        [SerializeField] private Animator _animator;
        [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
        [SerializeField] private int _specifiedColorMaterialIndex;
    
        private MaterialPropertyBlock _mpbColor;

        private void Awake()
        {
            _mpbColor = new MaterialPropertyBlock();
            _skinnedMeshRenderer.GetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }

        public void SetColor(Color color)
        {
            _mpbColor.SetColor(Constants.ShaderColorID, color);
            _skinnedMeshRenderer.SetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }

        public void SetRunning(bool running)
        {
            _animator.SetBool(Constants.AnimatorIsRunningID, running);
        }
        
        public void TriggerSitting(bool isRunning = true)
        {
            if (!isRunning) return;
            _animator.SetTrigger(Constants.AnimatorIsSittingID);
        }

        public async UniTask MoveTo(Vector3 worldPosition, float duration, Ease ease = Ease.Linear)
        {
            Vector3 target = new(worldPosition.x, transform.position.y, worldPosition.z);
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                transform
                    .DORotateQuaternion(lookRot, ROTATE_DURATION)
                    .SetEase(Ease.OutQuad);
            }
            await transform
                .DOMove(target, duration)
                .SetEase(ease)
                .ToUniTask();
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
        Reversed
    }
}