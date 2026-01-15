using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VehicleUnjam
{
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance { get; private set; }

        [SerializeField] private ThemeSettingsSO _themes;
        [SerializeField] private ColorSettingsSO _colors;

        private eThemeType _selectedThemeType = eThemeType.Default;

        private void Start()
        {
            instance = this;
            _ = LoadMainMenu();
        }
        
        public static Color GetColorByType(eColorType type)
        {
            return instance._colors.GetColorByType(type);
        }
        
        public static ThemeData GetCurrentTheme()
        {
            return instance._themes.GetDataByType(instance._selectedThemeType);
        }

        private async UniTask LoadMainMenu()
        {
            await SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Menu"));
        }
    }
}