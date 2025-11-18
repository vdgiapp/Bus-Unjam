using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BusUnjam
{
    [Serializable]
    public enum ePassengerType
    {
        Normal = 0,
        Reversed
    }
    
    [Serializable]
    public class Passenger : MonoBehaviour
    {
        [HideInInspector] public eColorType colorType;
        [HideInInspector] public ePassengerType passengerType;
        
        [SerializeField] private int _specifiedColorMaterialIndex;
        [SerializeField] private int _specifiedOutlineMaterialIndex;
    
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            _mpb = new MaterialPropertyBlock();
        }

        public void SetColor(Color color)
        {
            _skinnedMeshRenderer.GetPropertyBlock(_mpb, _specifiedColorMaterialIndex);
            _mpb.SetColor(Constants.ShaderColorID, color);
            _skinnedMeshRenderer.SetPropertyBlock(_mpb, _specifiedColorMaterialIndex);
        }
        
        public void ToggleOutline(bool toggle)
        {
            _skinnedMeshRenderer.GetPropertyBlock(_mpb, _specifiedOutlineMaterialIndex);
            _mpb.SetColor(Constants.ShaderColorID, toggle ? Color.black : Color.clear);
            _skinnedMeshRenderer.SetPropertyBlock(_mpb, _specifiedOutlineMaterialIndex);
        }
    }
}