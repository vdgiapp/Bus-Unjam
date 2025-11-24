using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace VehicleUnjam
{
    public class Vehicle : MonoBehaviour
    {
        [HideInInspector] public VehicleData data;
        
        [SerializeField] private Animator _animator;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private int _specifiedColorMaterialIndex;
        [SerializeField] private Transform _doorTransform;
        [SerializeField] private Transform[] _seatTransforms = new Transform[Constants.VEHICLE_SEAT_SLOTS];
        
        private MaterialPropertyBlock _mpbColor;
        
        private void Awake()
        {
            _mpbColor = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }
        
        public void SetColor(Color color)
        {
            _mpbColor.SetColor(Constants.SHADER_COLOR_ID, color);
            _meshRenderer.SetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }
        
        public async UniTask MoveTo(Vector3 worldPosition, float duration, Ease ease = Ease.Linear)
        {
            await transform.DOMove(worldPosition, duration).SetEase(ease).ToUniTask(); 
        }

        public async UniTask MoveLocalTo(Vector3 position, float duration, Ease ease = Ease.Linear)
        {
            await transform.DOLocalMove(position, duration).SetEase(ease).ToUniTask(); 
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

    [Serializable]
    public struct VehicleData
    {
        public eColorType colorType;
        public bool[] occupied;
    }
}