using System.Collections.Generic;
using UnityEngine;

namespace VehicleUnjam
{
    [CreateAssetMenu(fileName = "Themes", menuName = "Bus Unjam/Themes")]
    public class ThemeSettingsSO : ScriptableObject
    {
        [SerializeField]
        private List<KeyValue<eThemeType, ThemeData>> _themeList;
        public ThemeData GetDataByType(eThemeType type)
        {
            foreach (var kv in _themeList) if (kv.Key == type) return kv.Value;
            return null;
        }
    }
}