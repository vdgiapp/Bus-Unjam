using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BusUnjam
{
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance { get; protected set; }

        public static GridManager gridManager
            => instance._gridManager;

        public static PassengerManager passengerManager
            => instance._passengerManager;

        public static VehicleManager vehicleManager
            => instance._vehicleManager;

        public static LevelHandler levelHandler
            => instance._levelHandler;

        [SerializeField] private ThemeSettingsSO _themes;
        [SerializeField] private ColorSettingsSO _colors;

        private GridManager _gridManager;
        private PassengerManager _passengerManager;
        private VehicleManager _vehicleManager;
        private LevelHandler _levelHandler;

        private bool _isGameOver = false;
        private eThemeType _selectedThemeType = eThemeType.Default;

        private void Start()
        {
            instance = this;
            
            //LoadDataAndMainMenu().Forget();
            SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
        }

        public static Color GetColorByType(eColorType type) => instance._colors.GetColorByType(type);
        public static ThemeData GetCurrentTheme() => instance._themes.GetDataByType(instance._selectedThemeType);
        public static bool IsGameOver() => instance._isGameOver;

        public static async UniTask<bool> LoadLevel(int level)
        {
            await SceneManager.UnloadSceneAsync("Menu");
            await SceneManager.LoadSceneAsync("Level", LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Level"));
            await InstantiateAsync(GetCurrentTheme().GetEnvironmentPrefab(), null).ToUniTask();
            await WaitForManagersReady();
            
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
                new() { colorType = eColorType.Blue, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Blue, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Red, passengerType = ePassengerType.Normal},
                new() { colorType = eColorType.Green, passengerType = ePassengerType.Normal},
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
                    required = new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal}
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                    required = new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal}
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                    required = new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal}
                },
                new()
                {
                    colorType = eColorType.Green,
                    occupied = new[] {false, false, false},
                    required = new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal}
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                    required = new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal}
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                    required = new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal}
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                    required = new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal}
                },
                new()
                {
                    colorType = eColorType.Red,
                    occupied = new[] {false, false, false},
                    required = new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal}
                },
                new()
                {
                    colorType = eColorType.Blue,
                    occupied = new[] {false, false, false},
                    required = new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal}
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

            levelHandler.LoadLevelData(lev).Forget();
            return true;
        }

        private static async UniTask WaitForManagersReady()
        {
            while (true)
            {
                instance._gridManager ??= FindFirstObjectByType<GridManager>();
                instance._passengerManager ??= FindFirstObjectByType<PassengerManager>();
                instance._vehicleManager ??= FindFirstObjectByType<VehicleManager>();
                instance._levelHandler ??= FindFirstObjectByType<LevelHandler>();

                if (instance._gridManager != null &&
                    instance._passengerManager != null &&
                    instance._vehicleManager != null &&
                    instance._levelHandler != null)
                    break;

                await UniTask.Yield();
            }
        }
    }
}