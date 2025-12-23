#if UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VehicleUnjam.LevelEditor
{
    public class VehicleListItem : MonoBehaviour
    {
        [SerializeField] private Button _vehicleButton;
        [SerializeField] private Image _vehicleImage;
        [SerializeField] private TMP_Text _orderText;

        private void Awake()
        {
            _vehicleButton.onClick.AddListener(() =>
            {
                LevelEditorManager.OnVehicleSelected?.Invoke(this);
                Canvas.ForceUpdateCanvases();
            });
        }

        private void OnDestroy()
        {
            _vehicleButton.onClick.RemoveAllListeners();
        }

        public void InitInfo(int orderIndex, Color vehicleColor)
        {
            _vehicleImage.color = vehicleColor;
            _orderText.text = orderIndex.ToString();
            Canvas.ForceUpdateCanvases();
        }
    }
}
#endif