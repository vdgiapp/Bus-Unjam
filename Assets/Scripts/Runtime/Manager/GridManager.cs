using UnityEngine;

namespace BusUnjam
{
    public class GridManager : MonoBehaviour
    {
        private const float STRAIGHT_COST = 10f;
        private const float DIAGONAL_COST = 14f;
        private const float SPEED_MOVE_DOPATH = 2f;

        [SerializeField] private LayerMask _gridPadLayerMask;
        [SerializeField] private Vector2 _gridSize = Vector2.zero;
        [SerializeField] private int _waitQueueSize = 0;

        private Camera _mainCamera;
    }
}