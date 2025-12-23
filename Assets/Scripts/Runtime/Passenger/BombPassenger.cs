using TMPro;
using UnityEngine;

namespace VehicleUnjam
{
    public class BombPassenger : Passenger
    {
        // Data
        [HideInInspector] public BombPassengerData bombData;
        
        // View
        [SerializeField] private GameObject _bombObject;
        [SerializeField] private TMP_Text _textTmp;

        public void SetBombText(int time)
        {
            if (time <= 0)
            {
                _textTmp.text = " ";
                ToggleBomb(false);
            }
            else
            {
                _textTmp.text = time.ToString();
                ToggleBomb(true);
            }
        }

        public void ToggleBomb(bool toggle)
        {
            _bombObject.SetActive(toggle);
        }
    }
}