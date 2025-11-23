using System;
using UnityEngine;

namespace VehicleUnjam
{
    public class DestroyOnLoad : MonoBehaviour
    {
        private void Awake()
        {
            Destroy(gameObject);
        }
    }
}