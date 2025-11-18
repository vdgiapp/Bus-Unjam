using System;
using UnityEngine;

namespace BusUnjam
{
    [Serializable]
    public class Bus : MonoBehaviour
    {
        private const int CAPACITY = 3;
        
        [HideInInspector] public eColorType colorType;
        [HideInInspector] public ePassengerType[] required = new ePassengerType[CAPACITY];
        [HideInInspector] public bool[] occupied = new bool[CAPACITY];

        [SerializeField] private Transform[] _seatTransforms = new Transform[CAPACITY];
        [SerializeField] private int _specifiedColorMaterialIndex;
        
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
        }

        public bool IsFull() => GetNextAvailableSeatIndex() == -1;

        public Transform TryAddToBus(ePassengerType passengerType)
        {
            for (int i = 0; i < CAPACITY; i++)
            {
                if (required[i] == passengerType && !occupied[i])
                {
                    occupied[i] = true;
                    return _seatTransforms[i];
                }
            }
            return null;
        }

        public void SetColor(Color color)
        {
            _meshRenderer.GetPropertyBlock(_mpb, _specifiedColorMaterialIndex);
            _mpb.SetColor(Constants.ShaderColorID, color);
            _meshRenderer.SetPropertyBlock(_mpb, _specifiedColorMaterialIndex);
        }
        
        private int GetNextAvailableSeatIndex()
        {
            for (int i = 0; i < CAPACITY; i++) if (!occupied[i]) return i;
            return -1;
        }
    }
}