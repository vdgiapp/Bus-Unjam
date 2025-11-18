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
        private MaterialPropertyBlock _mpbColor;
        private MaterialPropertyBlock _mpbOutline;

        private void Awake()
        {
            _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            _mpbColor = new MaterialPropertyBlock();
            _mpbOutline = new MaterialPropertyBlock();
            _skinnedMeshRenderer.GetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
            _skinnedMeshRenderer.GetPropertyBlock(_mpbOutline, _specifiedOutlineMaterialIndex);
        }

        public void SetColor(Color color)
        {
            _mpbColor.SetColor(Constants.ShaderColorID, color);
            _skinnedMeshRenderer.SetPropertyBlock(_mpbColor, _specifiedColorMaterialIndex);
        }
        
        public void ToggleOutline(bool toggle)
        {
            _mpbOutline.SetColor(Constants.ShaderColorID, toggle ? Color.black : Color.clear);
            _skinnedMeshRenderer.SetPropertyBlock(_mpbOutline, _specifiedOutlineMaterialIndex);
        }
    }
}