using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusUnjam
{
    [Serializable]
    public enum eColorType
    {
        Red = 0,
        Blue,
        Yellow,
        Green,
        Purple,
        Orange
    }
    
    [Serializable]
    public class ColorData
    {
        public eColorType colorType;
        public Color color;
    }
    
    [CreateAssetMenu(fileName = "ColorData", menuName = "Bus Unjam/Color Data")]
    public class ColorDataSO : ScriptableObject
    {
        public Material outlineMaterial;
        public List<ColorData> colorDataList = new();
    }
}