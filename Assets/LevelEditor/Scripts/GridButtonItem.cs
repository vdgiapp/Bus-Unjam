#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace VehicleUnjam.LevelEditor
{
    public class GridButtonItem : MonoBehaviour
    {
        public int index = -1;
        
        [SerializeField] private Button _button;
        
        private void Awake()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
        }

        private void OnClicked()
        {
            LevelEditorManager.OnGridCellClicked?.Invoke(this);
            Canvas.ForceUpdateCanvases();
        }
    }
}
#endif