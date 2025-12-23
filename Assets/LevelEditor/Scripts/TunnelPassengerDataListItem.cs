#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VehicleUnjam.LevelEditor
{
    public class TunnelPassengerDataListItem : MonoBehaviour
    {
        public int cellIndex { get; private set; } = -1;
        public int passengerIndex { get; private set; } = -1;
        
        [SerializeField] private TMP_Text _indexText;
        [SerializeField] private TMP_Dropdown _colorDropdown;
        [SerializeField] private Button _removeButton;
        
        private void Awake() 
        {
            _removeButton.onClick.AddListener(() =>
            {
                CellInspector.OnTunnelPassengerRemoveRequested?.Invoke(cellIndex, passengerIndex);
                Canvas.ForceUpdateCanvases();
            });
            
            List<TMP_Dropdown.OptionData> colorOptions = new();
            foreach (eColorType color in Enum.GetValues(typeof(eColorType)))
            {
                colorOptions.Add(new(color.ToString()));
            }
            _colorDropdown.options = colorOptions;
            _colorDropdown.onValueChanged.AddListener((i) =>
            {
                LevelEditorManager.OnTunnelPassengerColorChanged?.Invoke(cellIndex, passengerIndex, (eColorType)i);
                Canvas.ForceUpdateCanvases();
            });
        }

        private void OnDestroy()
        {
            _colorDropdown.onValueChanged.RemoveAllListeners();
            _removeButton.onClick.RemoveAllListeners();
        }
        
        public void InitInfo(int cellIdx, int passengerIdx, eColorType color)
        {
            cellIndex = cellIdx;
            passengerIndex = passengerIdx;
            _indexText.text = passengerIndex.ToString();
            _colorDropdown.SetValueWithoutNotify((int)color);
        }
    }
}
#endif