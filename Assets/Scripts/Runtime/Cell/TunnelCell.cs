using TMPro;
using UnityEngine;

namespace VehicleUnjam
{
    public class TunnelCell : Cell
    {
        // Data
        [HideInInspector] public TunnelCellData tunnelData;
        
        // View
        [SerializeField] private GameObject _tunnelObject;
        [SerializeField] private TMP_Text _textTmp;
        
        public void SetTunnelCount(int count)
        {
            if (count <= 0) _textTmp.text = " ";
            else _textTmp.text = count.ToString();
        }
    }
}