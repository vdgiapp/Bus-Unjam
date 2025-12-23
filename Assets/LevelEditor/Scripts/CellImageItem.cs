#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace VehicleUnjam.LevelEditor
{
    public class CellImageItem : MonoBehaviour
    {
        [SerializeField] private Sprite _noneSprite;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _tunnelSprite;
        
        [SerializeField] private Image _image;
        
        public void SetSpriteByType(eCellType cellType)
        {
            _image.sprite = cellType switch
            {
                eCellType.None => _noneSprite,
                eCellType.Normal => _normalSprite,
                eCellType.Tunnel => _tunnelSprite,
                _ => null
            };
            if (_image.sprite == null) _image.color = Color.clear;
            _image.preserveAspect = true;
            Canvas.ForceUpdateCanvases();
        }
        
        public void SetSpriteRotation(float rotationDegrees)
        {
            _image.rectTransform.rotation = Quaternion.Euler(0, 0, rotationDegrees);
            Canvas.ForceUpdateCanvases();
        }
    }
}
#endif