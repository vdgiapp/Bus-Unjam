using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace BusUnjam
{
    public class Vehicle : MonoBehaviour
    {
        private const int CAPACITY = 3;

        [HideInInspector] public VehicleData data;
        
        [SerializeField] private Animator _animator;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private int _specifiedColorMaterialIndex;
        [SerializeField] private Transform[] _seatTransforms = new Transform[CAPACITY];
        
        // passenger, index
        private readonly Dictionary<Passenger, int> _lastAddedPassengers = new();
        private MaterialPropertyBlock _mpbColor;
        
        private void Awake()
        {
            _mpbColor = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }

        public void Reset()
        {
            _lastAddedPassengers.Clear();
        }
        
        public void SetColor(Color color)
        {
            _mpbColor.SetColor(Constants.ShaderColorID, color);
            _meshRenderer.SetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }
        
        public bool IsFull() => GetNextAvailableIndex() == -1;

        public bool CanAddToVehicle(Passenger p) => GetEmptyIndex(p) != -1;

        public async UniTask MoveTo(Vector3 worldPosition, float duration, Ease ease = Ease.Linear)
        {
            await transform.DOMove(worldPosition, duration).SetEase(ease).ToUniTask(); 
        }

        public async UniTask MoveLocalTo(Vector3 position, float duration, Ease ease = Ease.Linear)
        {
            await transform.DOLocalMove(position, duration).SetEase(ease).ToUniTask(); 
        }

        public Passenger GetLastAddedPassenger()
        {
            Passenger p = null;
            foreach (Passenger i in _lastAddedPassengers.Keys) p = i;
            return p;
        }
        
        public Passenger UndoLastAddedPassenger()
        {
            Passenger p = GetLastAddedPassenger();
            int index = _lastAddedPassengers[p];
            data.occupied[index] = false;
            _lastAddedPassengers.Remove(p);
            return p;
        }

        public bool TryAddPassenger(Passenger p)
        {
            int index = GetEmptyIndex(p);
            if (index == -1) return false;
            p.transform.SetParent(_seatTransforms[index]);
            p.transform.localPosition = Vector3.zero;
            data.occupied[index] = true;
            _lastAddedPassengers.Add(p, index);
            return true;
        }

        private int GetEmptyIndex(Passenger p)
        {
            if (IsFull() || p == null || p.data == null) return -1;
            for (int i = 0; i < CAPACITY; i++)
            {
                if (data.occupied[i]) continue;
                if (data.required[i] == p.data.passengerType && data.colorType == p.data.colorType) return i;
            }
            return -1;
        }
        
        private int GetNextAvailableIndex()
        {
            for (int i = 0; i < CAPACITY; i++) if (!data.occupied[i]) return i;
            return -1;
        }
    }

    [Serializable]
    public class VehicleData
    {
        public eColorType colorType;
        public ePassengerType[] required;
        public bool[] occupied;
    }
}