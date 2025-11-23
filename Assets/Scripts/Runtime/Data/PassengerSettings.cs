using UnityEngine;

namespace VehicleUnjam
{
    [CreateAssetMenu(fileName = "PassengerSettings", menuName = "Bus Unjam/Passenger Settings")]
    public class PassengerSettingsSO : ScriptableObject
    {
        [SerializeField]
        private KeyValue<ePassengerType, GameObject>[] _list;
        public GameObject GetPrefabByType(ePassengerType type)
        {
            foreach (var kv in _list) if (kv.Key == type) return kv.Value;
            return null;
        }
    }
}