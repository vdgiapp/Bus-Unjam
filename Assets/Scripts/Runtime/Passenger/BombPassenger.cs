using TMPro;
using UnityEngine;

namespace VehicleUnjam
{
    public class BombPassenger : Passenger
    {
        // View
        [SerializeField] protected GameObject _bombObject;
        [SerializeField] protected TMP_Text _textTmp;

        public void SetBombTime(int time)
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
            _textTmp.gameObject.SetActive(toggle);
        }
    }
}