#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VehicleUnjam.LevelEditor
{
    public class CellInspector : MonoBehaviour
    {
        public static Action<int, int> OnTunnelPassengerRemoveRequested;
        
        public int cellIndex { get; private set; } = -1;
        
        [SerializeField] private TMP_Text _positionText;
        [SerializeField] private TMP_Dropdown _typeDropdown;
        
        [SerializeField] private TMP_Dropdown _tunnelDirectionDropdown;
        [SerializeField] private TMP_Text _tunnelListCountText;
        [SerializeField] private TMP_InputField _tunnelIndexInputField;
        [SerializeField] private Button _addPassengerTunnelButton;
        [SerializeField] private RectTransform _tunnelListContent;
        
        [SerializeField] private RectTransform _tunnelInspectorContent;
        
        [SerializeField] private GameObject _tunnelDataListItemPrefab;
        
        private void Awake()
        {
            OnTunnelPassengerRemoveRequested += HandlePassengerRemoveRequested;
            // Setup direction dropdown
            List<TMP_Dropdown.OptionData> directionOptions = new()
            {
                new("Up"),
                new("Right"),
                new("Down"),
                new("Left")
            };
            _tunnelDirectionDropdown.options = directionOptions;
            _tunnelDirectionDropdown.onValueChanged.AddListener((i) =>
            {
                LevelEditorManager.OnTunnelDirectionChanged?.Invoke(cellIndex, i);
                Canvas.ForceUpdateCanvases();
            });
            
            // Setup cell type dropdown
            List<TMP_Dropdown.OptionData> typeOptions = new();
            foreach (eCellType cellType in Enum.GetValues(typeof(eCellType)))
            {
                typeOptions.Add(new(cellType.ToString()));
            }
            _typeDropdown.options = typeOptions;
            _typeDropdown.onValueChanged.AddListener((i) =>
            {
                eCellType selectedType = (eCellType)i;
                UpdateInspectorByType(selectedType);
                LevelEditorManager.OnCellTypeChanged?.Invoke(cellIndex, selectedType);
                Canvas.ForceUpdateCanvases();
            });
            
            // Setup add passenger button
            _addPassengerTunnelButton.onClick.AddListener(() =>
            {
                int insertIndex = CalculateInsertIndex();
                LevelEditorManager.OnTunnelPassengerAdded?.Invoke(cellIndex, insertIndex);
                Canvas.ForceUpdateCanvases();
            });
        }
        
        private void OnDestroy()
        {
            OnTunnelPassengerRemoveRequested -= HandlePassengerRemoveRequested;
            _typeDropdown.onValueChanged.RemoveAllListeners();
            _tunnelDirectionDropdown.onValueChanged.RemoveAllListeners();
            _addPassengerTunnelButton.onClick.RemoveAllListeners();
        }

        private int CalculateInsertIndex()
        {
            int childCount = _tunnelListContent.childCount;
            
            if (string.IsNullOrWhiteSpace(_tunnelIndexInputField.text))
                return childCount;
            
            if (int.TryParse(_tunnelIndexInputField.text, out int index))
                return Mathf.Clamp(index, 0, childCount);
            
            return childCount;
        }

        private void HandlePassengerRemoveRequested(int cellIdx, int passengerIndex)
        {
            LevelEditorManager.OnTunnelPassengerRemoved?.Invoke(cellIdx, passengerIndex);
            Canvas.ForceUpdateCanvases();
        }

        public void InitInfo(int cellIdx, eCellType cellType, int row, int col)
        {
            cellIndex = cellIdx;
            _typeDropdown.SetValueWithoutNotify((int)cellType);
            
            _positionText.text = cellIndex == -1 
                ? "Chọn vị trí" 
                : $"Vị trí: ({row}, {col})";
            
            UpdateInspectorByType(cellType);
        }

        public void InitInfoTunnel(int direction, List<PassengerData> passengers)
        {
            _tunnelDirectionDropdown.SetValueWithoutNotify(direction);
            RebuildPassengerList(passengers);
        }

        public void RebuildPassengerList(List<PassengerData> passengers)
        {
            _tunnelListCountText.text = $"Danh sách: {passengers.Count}";
            ClearPassengerList();
            for (int i = 0; i < passengers.Count; i++)
            {
                TunnelPassengerDataListItem itemComponent = Instantiate(_tunnelDataListItemPrefab, _tunnelListContent).GetComponent<TunnelPassengerDataListItem>();
                itemComponent.InitInfo(cellIndex, i, passengers[i].colorType);
            }
            Canvas.ForceUpdateCanvases();
        }

        private void ClearPassengerList()
        {
            foreach (Transform child in _tunnelListContent)
            {
                Destroy(child.gameObject);
            }
        }
        
        private void UpdateInspectorByType(eCellType type)
        {
            _tunnelInspectorContent.gameObject.SetActive(type == eCellType.Tunnel);
            Canvas.ForceUpdateCanvases();
        }
    }
}
#endif