using UnityEngine;

namespace BusUnjam
{
    [CreateAssetMenu(fileName = "Colors", menuName = "Bus Unjam/Colors")]
    public class ColorSettingsSO : ScriptableObject
    {
        [SerializeField]
        private KeyValue<eColorType, Color>[] _list;
        public Color GetColorByType(eColorType type)
        {
            foreach (var kv in _list) if (kv.Key == type) return kv.Value;
            return Color.clear;
        }
    }
}