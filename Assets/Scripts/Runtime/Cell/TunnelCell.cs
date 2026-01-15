using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VehicleUnjam
{
    public class TunnelCell : Cell
    {
        // Data
        public List<Passenger> passengers { get; private set; } = new();
        
        // View
        [SerializeField] protected GameObject _tunnelObject;
        [SerializeField] protected TMP_Text _textTmp;
        
        public void SetTunnelCount(int count)
        {
            _textTmp.text = (count <= 0) ? " " : count.ToString();
        }
        
        public void SetTunnelDirection(int direction)
        {
            _tunnelObject.transform.rotation = Quaternion.Euler(0f, 360f - (direction * 90f), 0f);
        }
    }
}