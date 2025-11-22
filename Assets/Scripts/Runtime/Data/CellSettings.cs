using UnityEngine;

namespace BusUnjam
{
    [CreateAssetMenu(fileName = "CellSettings", menuName = "Bus Unjam/Cell Settings")]
    public class CellSettingsSO : ScriptableObject
    {
        [SerializeField]
        private KeyValue<eCellType, GameObject>[] _list;
        public GameObject GetPrefabByType(eCellType type)
        {
            foreach (var kv in _list) if (kv.Key == type) return kv.Value;
            return null;
        }
    }
}