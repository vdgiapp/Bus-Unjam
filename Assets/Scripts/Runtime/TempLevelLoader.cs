using System.IO;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using OdinSerializer = Sirenix.OdinSerializer;

namespace VehicleUnjam
{
    public class TempLevelLoader : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private TMP_Text _textLog;
        
        public void Play()
        {
            _ = PlayAsync();
        }
        
        public async UniTask PlayAsync()
        {
            if (!int.TryParse(_inputField.text, out int level) || level <= 0)
            {
                _textLog.text = "Invalid level number";
                return;
            }
            
            LevelData levelData = new();
            string[] jsonFiles = Directory.GetFiles(Constants.LEVEL_FOLDER_PATH, "*.json");
            
            if (level > jsonFiles.Length)
            {
                _textLog.text = "Level not found";
                return;
            }
            
            for (int i = 0; i < jsonFiles.Length; i++)
            {
                if (i == level - 1)
                {
                    string filePath = jsonFiles[i];
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                    levelData = OdinSerializer.SerializationUtility.DeserializeValue<LevelData>(fileBytes, OdinSerializer.DataFormat.JSON);
                    break;
                }
            }
            
            await SceneManager.UnloadSceneAsync("Menu");
            await SceneManager.LoadSceneAsync("Level", LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Level"));
            await InstantiateAsync(GameManager.GetCurrentTheme().environmentPrefab, null);

            await LevelHandler.instance.InitLevel(levelData);
        }
    }
}