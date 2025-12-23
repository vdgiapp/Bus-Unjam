#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace VehicleUnjam.LevelEditor
{
    public class PassengerImageItem : MonoBehaviour
    {
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _hiddenSprite;
        [SerializeField] private Sprite _ropeSprite;
        [SerializeField] private Sprite _cloakSprite;
        [SerializeField] private Sprite _bombSprite;
        
        [SerializeField] private Image _image;

        public void SetSpriteByType(ePassengerType passengerType)
        {
            _image.sprite = passengerType switch
            {
                ePassengerType.Normal => _normalSprite,
                ePassengerType.Hidden => _hiddenSprite,
                ePassengerType.Rope => _ropeSprite,
                ePassengerType.Cloak => _cloakSprite,
                ePassengerType.Bomb => _bombSprite,
                _ => null
            };
            
            if (_image.sprite == null)
            {
                _image.color = Color.clear;
            }
            
            _image.preserveAspect = true;
        }

        public void SetSpriteColor(Color color)
        {
            _image.color = color;
        }
    }
}
#endif