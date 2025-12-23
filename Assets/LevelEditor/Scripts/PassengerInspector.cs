#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VehicleUnjam.LevelEditor
{
    public class PassengerInspector : MonoBehaviour
    {
        public int index = -1;
        
        [SerializeField] private TMP_Text _positionText;
        [SerializeField] private TMP_Dropdown _typeDropdown;
        [SerializeField] private TMP_Dropdown _colorDropdown;
        
        [SerializeField] private TMP_InputField _ropeInputField;
        [SerializeField] private TMP_InputField _bombInputField;
        [SerializeField] private Toggle _cloakIsToggle;
        
        [SerializeField] private RectTransform _ropeInspectorContent;
        [SerializeField] private RectTransform _bombInspectorContent;
        [SerializeField] private RectTransform _cloakInspectorContent;
        
        private void Awake()
        {
            List<TMP_Dropdown.OptionData> options = new();
            foreach (eColorType color in Enum.GetValues(typeof(eColorType))) { options.Add(new(color.ToString())); }
            _colorDropdown.options = options;
            _colorDropdown.onValueChanged.AddListener((i) =>
            {
                LevelEditorManager.OnPassengerColorChanged?.Invoke(index, (eColorType)i);
                Canvas.ForceUpdateCanvases();
            });
            
            options = new();
            foreach (ePassengerType type in Enum.GetValues(typeof(ePassengerType))) { options.Add(new(type.ToString())); }
            _typeDropdown.options = options;
            _typeDropdown.onValueChanged.AddListener((i) => 
            {
                ePassengerType type = (ePassengerType)i;
                UpdateInspectorByType(type);
                LevelEditorManager.OnPassengerTypeChanged?.Invoke(index, type);
                Canvas.ForceUpdateCanvases();
            });
            
            _ropeInputField.onValueChanged.AddListener((str) =>
            {
                if (!int.TryParse(str, out int value)) return;
                LevelEditorManager.OnRopeCountChanged?.Invoke(index, value);
                Canvas.ForceUpdateCanvases();
            });
            _bombInputField.onValueChanged.AddListener((str) =>
            {
                if (!int.TryParse(str, out int value)) return;
                LevelEditorManager.OnBombTimeChanged?.Invoke(index, value);
                Canvas.ForceUpdateCanvases();
            });
            _cloakIsToggle.onValueChanged.AddListener((value) =>
            {
                LevelEditorManager.OnCloakRevealChanged?.Invoke(index, value);
                Canvas.ForceUpdateCanvases();
            });
        }
        
        private void OnDestroy()
        {
            _typeDropdown.onValueChanged.RemoveAllListeners();
            _colorDropdown.onValueChanged.RemoveAllListeners();
            _ropeInputField.onValueChanged.RemoveAllListeners();
            _bombInputField.onValueChanged.RemoveAllListeners();
            _cloakIsToggle.onValueChanged.RemoveAllListeners();
        }
        
        public void InitInfo(int idx, ePassengerType type, eColorType color, int row, int col)
        {
            index = idx;

            _colorDropdown.SetValueWithoutNotify((int)color);
            _typeDropdown.SetValueWithoutNotify((int)type);

            if (idx == -1) _positionText.text = "Chọn vị trí";
            else _positionText.text = $"Vị trí: ({row}, {col})";

            UpdateInspectorByType(type);
        }

        public void InitInfoRope(int rope)
        {
            _ropeInputField.SetTextWithoutNotify(rope.ToString());
        }

        public void InitInfoBomb(int bomb)
        {
            _bombInputField.SetTextWithoutNotify(bomb.ToString());
        }

        public void InitInfoCloak(bool isRevealed)
        {
            _cloakIsToggle.SetIsOnWithoutNotify(isRevealed);
        }
        
        private void UpdateInspectorByType(ePassengerType type)
        {
            _ropeInspectorContent.gameObject.SetActive(type == ePassengerType.Rope);
            _bombInspectorContent.gameObject.SetActive(type == ePassengerType.Bomb);
            _cloakInspectorContent.gameObject.SetActive(type == ePassengerType.Cloak);
        }
    }
}
#endif