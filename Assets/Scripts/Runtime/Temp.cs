using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            await InstantiateAsync(GameManager.GetCurrentTheme().GetEnvironmentPrefab(), null).ToUniTask();

            CellData[] cellList =
            {
                new() { cellType = eCellType.None, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.None, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.None, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
                new() { cellType = eCellType.Normal, isOccupied = true },
            };
            PassengerData[] passList =
            {
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Blue, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Blue, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Green, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Green, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Blue, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Green, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
            };
            VehicleData[] vehList =
            {
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                },
                new()
                {
                    colorType = eColorType.Green,
                    occupied = new[] {false, false, false},
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                },
                new()
                {
                    colorType = eColorType.Blue,
                    occupied = new[] {false, false, false},
                },
            };
            
            LevelData lev = (new LevelData()
            {
                columns = 6,
                rows = 5,
                waitAreaSize = 5,
                cells = cellList,
                passengers = passList,
                vehicles = vehList
            });
            await LevelHandler.instance.InitLevel(lev);
        }
    }
}