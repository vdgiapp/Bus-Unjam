using UnityEngine;

namespace VehicleUnjam
{
    public class RopePassenger : Passenger
    {
        // View
        [SerializeField] protected GameObject[] _ropeObjects;
        [SerializeField] protected Transform _ropeRoot;

        public void SetRopeCount(int count)
        {
            for (int i = 0; i < Constants.PASSENGER_MAX_ROPE_COUNT; i++)
            {
                _ropeObjects[i].SetActive(i < count);
            }
        }
        
        public void RemoveRope()
        {
            SetRopeCount(0);
        }
    }
}