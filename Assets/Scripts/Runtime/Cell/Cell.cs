using UnityEngine;

namespace VehicleUnjam
{
    public class Cell : MonoBehaviour
    {
        // Data
        [HideInInspector] public CellData data;
        
        // View
        [SerializeField] private Animator _animator;
    }
}