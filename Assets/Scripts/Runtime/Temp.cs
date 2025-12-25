using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using OdinSerializer = Sirenix.OdinSerializer;

namespace VehicleUnjam
{
    public class Temp : MonoBehaviour
    {
        public void Play()
        {
            _ = PlayAsync();
        }
        
        public async UniTask PlayAsync()
        {
            await SceneManager.UnloadSceneAsync("Menu");
            await SceneManager.LoadSceneAsync("Level", LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Level"));
            await InstantiateAsync(GameManager.GetCurrentTheme()?.environmentPrefab, null);

            // CellData[] cellList =
            // {
            //     new() { cellType = eCellType.None, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.None, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.None, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            //     new() { cellType = eCellType.Normal, isOccupied = true },
            // };
            // PassengerData[] passList =
            // {
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Blue, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Blue, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Green, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Green, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Blue, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Green, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            //     new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            // };
            // VehicleData[] vehList =
            // {
            //     new()
            //     {
            //         colorType = eColorType.Red,
            //         occupied = new[] {false, false, false},
            //     },
            //     new()
            //     {
            //         colorType = eColorType.Red,
            //         occupied = new[] {false, false, false},
            //     },
            //     new()
            //     {
            //         colorType = eColorType.Red,
            //         occupied = new[] {false, false, false},
            //     },
            //     new()
            //     {
            //         colorType = eColorType.Green,
            //         occupied = new[] {false, false, false},
            //     },
            //     new()
            //     {
            //         colorType = eColorType.Red,
            //         occupied = new[] {false, false, false},
            //     },
            //     new()
            //     {
            //         colorType = eColorType.Red,
            //         occupied = new[] {false, false, false},
            //     },
            //     new()
            //     {
            //         colorType = eColorType.Red,
            //         occupied = new[] {false, false, false},
            //     },
            //     new()
            //     {
            //         colorType = eColorType.Red,
            //         occupied = new[] {false, false, false},
            //     },
            //     new()
            //     {
            //         colorType = eColorType.Blue,
            //         occupied = new[] {false, false, false},
            //     },
            // };
            //
            // LevelData lev = (new LevelData()
            // {
            //     rows = 5,
            //     columns = 6,
            //     waitAreaSize = 5,
            //     cells = new(cellList),
            //     passengers = new(passList),
            //     vehicles = new(vehList)
            // });

            int level = 2;
            LevelData lev = new();
            string[] jsonFiles = Directory.GetFiles(Constants.LEVEL_FOLDER_PATH, "*.json");
            for (int i = 0; i < jsonFiles.Length; i++)
            {
                if (i == level - 1)
                {
                    string filePath = jsonFiles[i];
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                    lev = OdinSerializer.SerializationUtility.DeserializeValue<LevelData>(fileBytes, OdinSerializer.DataFormat.JSON);
                    break;
                }
            }
            
            await LevelHandler.instance.InitLevel(lev);
        }
    }
}