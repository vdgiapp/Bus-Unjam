#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VehicleUnjam.LevelEditor
{
    public class VehicleInspector : MonoBehaviour
    {
        public int vehicleIndex { get; private set; } = -1;
        
        [SerializeField] private TMP_Text _orderText;
        [SerializeField] private TMP_Dropdown _dropDown;
        
        private void Awake()
        {
            List<TMP_Dropdown.OptionData> colorOptions = new();
            foreach (eColorType color in Enum.GetValues(typeof(eColorType)))
            {
                colorOptions.Add(new(color.ToString()));
            }
            _dropDown.options = colorOptions;
            _dropDown.onValueChanged.AddListener(OnColorChanged);
        }
        
        private void OnDestroy()
        {
            _dropDown.onValueChanged.RemoveAllListeners();
        }

        private void OnColorChanged(int colorIndex)
        {
            LevelEditorManager.OnVehicleColorChanged?.Invoke(vehicleIndex, (eColorType)colorIndex);
            Canvas.ForceUpdateCanvases();
        }

        public void InitInfo(int vehicleIdx, eColorType color)
        {
            vehicleIndex = vehicleIdx;
            _dropDown.SetValueWithoutNotify((int)color);
            _orderText.text = $"Vị trí: {vehicleIndex}";
            Canvas.ForceUpdateCanvases();
        }
    }
}
#endif