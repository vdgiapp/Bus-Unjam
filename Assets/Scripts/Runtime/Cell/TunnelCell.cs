using TMPro;
using UnityEngine;

namespace VehicleUnjam
{
    public class TunnelCell : Cell
    {
        // View
        [SerializeField] private GameObject _tunnelObject;
        [SerializeField] private TMP_Text _textTmp;
        
        public void SetTunnelCount(int count)
        {
            if (count <= 0) _textTmp.text = " ";
            else _textTmp.text = count.ToString();
        }
        
        public void SetTunnelDirection(int direction)
        {
            _tunnelObject.transform.rotation = Quaternion.Euler(0f, 360f - (direction * 90f), 0f);
        }
    }
}