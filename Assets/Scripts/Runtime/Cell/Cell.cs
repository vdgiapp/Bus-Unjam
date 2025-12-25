using UnityEngine;

namespace VehicleUnjam
{
    public class Cell : MonoBehaviour
    {
        // Data
        public CellData data { get; private set; }
        
        // View
        [SerializeField] private Animator _animator;

        public void InitData(CellData initData)
        {
            data = initData;
        }
    }
}