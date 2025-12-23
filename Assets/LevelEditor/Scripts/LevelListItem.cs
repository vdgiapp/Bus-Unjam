#if UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VehicleUnjam.LevelEditor
{
    public class LevelListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Button _levelButton;

        private void Awake()
        {
            _levelButton.onClick.AddListener(OnSelected);
        }

        private void OnDestroy()
        {
            _levelButton.onClick.RemoveAllListeners();
        }

        private void OnSelected()
        {
            LevelEditorManager.OnLevelSelected?.Invoke(this);
            Canvas.ForceUpdateCanvases();
        }
        
        public void InitInfo(string levelName)
        {
            _levelText.text = levelName;
        }
    }
}
#endif