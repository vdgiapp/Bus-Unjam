#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OdinSerializer = Sirenix.OdinSerializer;

namespace VehicleUnjam.LevelEditor
{
    public class LevelEditorManager : MonoBehaviour
    {
        enum eEditorMode
        {
            Select = 0,
            AddClone,
            Delete
        }

        enum eGridMode
        {
            Cell = 0,
            Passenger
        }
        
        private readonly Color INACTIVE_BUTTON_COLOR = new(36f/255f, 36f/255f, 36f/255f, 236f/255f);
        private readonly Color ACTIVE_BUTTON_COLOR = new(103f/255f, 103f/255f, 103f/255f, 236f/255f);
        private const float CELL_SIZE = 110f;
        private const float ZOOM_STEP = 0.25f;
        private const float MIN_ZOOM = 0.1f;
        
        public static Action<LevelListItem> OnLevelSelected;
        public static Action<VehicleListItem> OnVehicleSelected;
        public static Action<GridButtonItem> OnGridCellClicked;
        public static Action<int, eColorType> OnVehicleColorChanged;
        public static Action<int, eColorType> OnPassengerColorChanged;
        public static Action<int, ePassengerType> OnPassengerTypeChanged;
        public static Action<int, int> OnRopeCountChanged;
        public static Action<int, int> OnBombTimeChanged;
        public static Action<int, bool> OnCloakRevealChanged;
        public static Action<int, eCellType> OnCellTypeChanged;
        public static Action<int, int> OnTunnelDirectionChanged;
        public static Action<int, int> OnTunnelPassengerRemoved;
        public static Action<int, int> OnTunnelPassengerAdded;
        public static Action<int, int, eColorType> OnTunnelPassengerColorChanged;
        
        public string levelFolderPath = Constants.LEVEL_FOLDER_PATH;
        public ColorSettingsSO colors;
        
        public GameObject levelEditorCanvas;
        public Button saveAllButton;
        public Button addNewLevelButton;
        public Button removeLevelButton;
        public RectTransform levelListContent;
        public GameObject levelListItemPrefab;
        public TMP_InputField levelFindInput;
        public Button levelFindButton;

        public TMP_Text levelEditText;
        public Button levelSaveButton;
        public TMP_InputField levelRowsInput;
        public TMP_InputField levelColumnsInput;
        public Button levelGenerateGridButton;
        public TMP_InputField levelWaitingSizeInput;
        
        public Button addNewVehicleButton;
        public Button removeVehicleButton;
        public RectTransform vehicleListContent;
        public GameObject vehicleListItemPrefab;
        public GameObject selectVehicleImage;

        public ScrollRect levelEditorScrollRect;
        public Button selectModeEditorButton;
        public Button addCloneModeEditorButton;
        public Button deleteModeEditorButton;
        public Button cellModeEditorButton;
        public Button passengerModeEditorButton;
        public Button zoomInEditorButton;
        public Button zoomOutEditorButton;
        public Button zoomResetEditorButton;
        public RectTransform cellEditorContent;
        public GameObject cellImageItemPrefab;
        public RectTransform passengerEditorContent;
        public GameObject passengerImageItemPrefab;
        public RectTransform gridButtonEditorContent;
        public GameObject gridButtonItemPrefab;
        public GameObject selectGridImage;
        
        public TMP_Text inspectorModeText;
        public RectTransform inspectorModeContent;
        public GameObject inspectorModeVehiclePrefab;
        public GameObject inspectorModePassengerPrefab;
        public GameObject inspectorModeCellPrefab;
        
        public ScrollRect statisticsScrollRect;
        public TMP_Text statisticsText;
        
        public RectTransform levelPanel;
        public RectTransform rightTopPanel;
        public RectTransform rightBottomPanel;
        
        private List<LevelDataEntry> _allLevels = new();
        private List<VehicleData> _currentVehicles = new();
        private List<CellData> _currentCells = new();
        private List<PassengerData> _currentPassengers = new();

        private PassengerData _cloneTemplatePassenger;
        private CellData _cloneTemplateCell;
        
        private int _selectedLevelIndex = -1;
        private int _selectedVehicleIndex = -1;
        
        private eEditorMode _currentEditorMode = eEditorMode.Select;
        private eGridMode _currentGridMode = eGridMode.Cell;
        
        private bool _needCanvasUpdate = false;
        
        private class LevelDataEntry
        {
            public LevelData Data;
            public string FilePath;

            public LevelDataEntry(LevelData data, string path)
            {
                Data = data;
                FilePath = path;
            }
        }

        private void Awake()
        {
            OnLevelSelected += HandleLevelSelected;
            OnVehicleSelected += HandleVehicleSelected;
            OnGridCellClicked += HandleGridCellClicked;
            OnVehicleColorChanged += HandleVehicleColorChanged;
            OnPassengerColorChanged += HandlePassengerColorChanged;
            OnPassengerTypeChanged += HandlePassengerTypeChanged;
            OnRopeCountChanged += HandleRopeCountChanged;
            OnBombTimeChanged += HandleBombTimeChanged;
            OnCloakRevealChanged += HandleCloakRevealChanged;
            OnCellTypeChanged += HandleCellTypeChanged;
            OnTunnelDirectionChanged += HandleTunnelDirectionChanged;
            OnTunnelPassengerRemoved += HandleTunnelPassengerRemoved;
            OnTunnelPassengerAdded += HandleTunnelPassengerAdded;
            OnTunnelPassengerColorChanged += HandleTunnelPassengerColorChanged;
            saveAllButton.onClick.AddListener(SaveAllLevelsToDisk);
            addNewLevelButton.onClick.AddListener(CreateNewLevel);
            removeLevelButton.onClick.AddListener(DeleteLastLevel);
            levelFindButton.onClick.AddListener(SearchLevels);
            levelSaveButton.onClick.AddListener(SaveCurrentLevelToDisk);
            levelGenerateGridButton.onClick.AddListener(GenerateNewGrid);
            selectModeEditorButton.onClick.AddListener(SwitchToSelectMode);
            addCloneModeEditorButton.onClick.AddListener(SwitchToAddCloneMode);
            deleteModeEditorButton.onClick.AddListener(SwitchToDeleteMode);
            cellModeEditorButton.onClick.AddListener(SwitchToCellMode);
            passengerModeEditorButton.onClick.AddListener(SwitchToPassengerMode);
            zoomInEditorButton.onClick.AddListener(ZoomInGrid);
            zoomOutEditorButton.onClick.AddListener(ZoomOutGrid);
            zoomResetEditorButton.onClick.AddListener(ResetGridZoom);
            addNewVehicleButton.onClick.AddListener(CreateNewVehicle);
            removeVehicleButton.onClick.AddListener(DeleteLastVehicle);
        }

        private void Start()
        {
            SetMainPanelsActive(false);
            LoadAllLevelsFromDisk();
        }

        private void Update()
        {
            statisticsText.text = BuildStatistics();
        }

        private void LateUpdate()
        {
            if (_needCanvasUpdate)
            {
                Canvas.ForceUpdateCanvases();
                _needCanvasUpdate = false;
            }
        }
        
        private void OnDestroy()
        {
            OnLevelSelected -= HandleLevelSelected;
            OnVehicleSelected -= HandleVehicleSelected;
            OnGridCellClicked -= HandleGridCellClicked;
            OnVehicleColorChanged -= HandleVehicleColorChanged;
            OnPassengerColorChanged -= HandlePassengerColorChanged;
            OnPassengerTypeChanged -= HandlePassengerTypeChanged;
            OnRopeCountChanged -= HandleRopeCountChanged;
            OnBombTimeChanged -= HandleBombTimeChanged;
            OnCloakRevealChanged -= HandleCloakRevealChanged;
            OnCellTypeChanged -= HandleCellTypeChanged;
            OnTunnelDirectionChanged -= HandleTunnelDirectionChanged;
            OnTunnelPassengerRemoved -= HandleTunnelPassengerRemoved;
            OnTunnelPassengerAdded -= HandleTunnelPassengerAdded;
            OnTunnelPassengerColorChanged -= HandleTunnelPassengerColorChanged;
            saveAllButton.onClick.RemoveAllListeners();
            removeLevelButton.onClick.RemoveAllListeners();
            addNewLevelButton.onClick.RemoveAllListeners();
            levelFindButton.onClick.RemoveAllListeners();
            levelSaveButton.onClick.RemoveAllListeners();
            levelGenerateGridButton.onClick.RemoveAllListeners();
            selectModeEditorButton.onClick.RemoveAllListeners();
            addCloneModeEditorButton.onClick.RemoveAllListeners();
            deleteModeEditorButton.onClick.RemoveAllListeners();
            cellModeEditorButton.onClick.RemoveAllListeners();
            passengerModeEditorButton.onClick.RemoveAllListeners();
            zoomInEditorButton.onClick.RemoveAllListeners();
            zoomOutEditorButton.onClick.RemoveAllListeners();
            zoomResetEditorButton.onClick.RemoveAllListeners();
            addNewVehicleButton.onClick.RemoveAllListeners();
            removeVehicleButton.onClick.RemoveAllListeners();
        }

        private void LoadAllLevelsFromDisk()
        {
            try
            {
                _allLevels.Clear();
                ClearChildren(levelListContent);

                string[] jsonFiles = Directory.GetFiles(levelFolderPath, "*.json");
                foreach (string filePath in jsonFiles)
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    LevelData levelData = OdinSerializer.SerializationUtility.DeserializeValue<LevelData>(
                        fileBytes, 
                        OdinSerializer.DataFormat.JSON
                    );
                    CreateLevelListItem(Path.GetFileNameWithoutExtension(filePath));
                    _allLevels.Add(new LevelDataEntry(levelData, filePath));
                }
                Debug.Log($"✓ Loaded {_allLevels.Count} levels successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Failed to load levels: {ex.Message}");
            }
        }

        private void SaveAllLevelsToDisk()
        {
            try
            {
                int savedCount = 0;
                foreach (var levelEntry in _allLevels)
                {
                    byte[] levelBytes = OdinSerializer.SerializationUtility.SerializeValue(
                        levelEntry.Data, 
                        OdinSerializer.DataFormat.JSON
                    );
                    File.WriteAllBytes(levelEntry.FilePath, levelBytes);
                    savedCount++;
                }
                
                Debug.Log($"✓ Saved {savedCount} levels successfully");
                LoadAllLevelsFromDisk();
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Failed to save levels: {ex.Message}");
            }
        }

        private void CreateNewLevel()
        {
            try
            {
                LevelData newLevel = new LevelData();
                int levelNumber = _allLevels.Count + 1;
                string filePath = Path.Combine(levelFolderPath, $"level_{levelNumber}.json");
                
                byte[] levelBytes = OdinSerializer.SerializationUtility.SerializeValue(
                    newLevel, 
                    OdinSerializer.DataFormat.JSON
                );
                File.WriteAllBytes(filePath, levelBytes);
                
                Debug.Log($"✓ Created new level ({levelNumber}) at {filePath}");
                LoadAllLevelsFromDisk();
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Failed to create new level: {ex.Message}");
            }
        }

        private void DeleteLastLevel()
        {
            if (_allLevels.Count <= 0) return;
            try
            {
                var lastLevel = _allLevels[^1];
                File.Delete(lastLevel.FilePath);
                File.Delete(lastLevel.FilePath + ".meta");
                
                Debug.Log("✓ Deleted last level successfully");
                LoadAllLevelsFromDisk();
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Failed to delete level: {ex.Message}");
            }
            if (_allLevels.Count <= 0 || _selectedLevelIndex >= _allLevels.Count) SetMainPanelsActive(false);
        }

        private void SearchLevels()
        {
            string searchTerm = levelFindInput.text.ToLower();
            for (int i = 0; i < _allLevels.Count; i++)
            {
                string levelName = Path.GetFileNameWithoutExtension(_allLevels[i].FilePath).ToLower();
                bool shouldShow = string.IsNullOrWhiteSpace(searchTerm) || levelName.Contains(searchTerm);
                levelListContent.GetChild(i).gameObject.SetActive(shouldShow);
            }
        }

        private void SaveCurrentLevelToDisk()
        {
            if (!IsLevelSelected()) return;

            try
            {
                if (!ValidateGridInputs(out int rows, out int cols, out int waitSize))
                {
                    Debug.LogError("✗ Invalid grid parameters");
                    return;
                }

                var currentEntry = _allLevels[_selectedLevelIndex];
                currentEntry.Data.rows = rows;
                currentEntry.Data.columns = cols;
                currentEntry.Data.waitAreaSize = waitSize;
                currentEntry.Data.cells = (List<CellData>)OdinSerializer.SerializationUtility.CreateCopy(_currentCells);
                currentEntry.Data.passengers = (List<PassengerData>)OdinSerializer.SerializationUtility.CreateCopy(_currentPassengers);
                currentEntry.Data.vehicles = (List<VehicleData>)OdinSerializer.SerializationUtility.CreateCopy(_currentVehicles);
                
                byte[] levelBytes = OdinSerializer.SerializationUtility.SerializeValue(currentEntry.Data, OdinSerializer.DataFormat.JSON);
                File.WriteAllBytes(currentEntry.FilePath, levelBytes);
                
                Debug.Log("✓ Saved current level successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Failed to save current level: {ex.Message}");
            }
        }
        
        private void CreateLevelListItem(string levelName)
        {
            GameObject itemObject = Instantiate(levelListItemPrefab, levelListContent);
            LevelListItem itemComponent = itemObject.GetComponent<LevelListItem>();
            itemComponent.InitInfo(levelName);
        }

        private void GenerateNewGrid()
        {
            if (!IsLevelSelected()) return;
            
            if (!ValidateGridInputs(out int rows, out int cols, out _))
            {
                Debug.LogError("✗ Invalid grid parameters");
                return;
            }
            
            ClearGrid();
            _currentCells.Clear();
            _currentPassengers.Clear();
            
            int totalCells = rows * cols;
            for (int i = 0; i < totalCells; i++)
            {
                CreateGridCellAt(i, rows, cols);
            }
            Debug.Log($"✓ Generated {totalCells} cells grid ({rows}x{cols})");
        }

        private void LoadLevelGrid(LevelData levelData)
        {
            if (!IsLevelSelected() || levelData.cells == null) return;
            
            ClearGrid();
            _currentCells.Clear();
            _currentPassengers.Clear();
            
            int rows = levelData.rows;
            int cols = levelData.columns;
            
            for (int i = 0; i < levelData.cells.Count; i++)
            {
                CellData cellClone = (CellData)OdinSerializer.SerializationUtility.CreateCopy(levelData.cells[i]);
                PassengerData passengerClone = (PassengerData)OdinSerializer.SerializationUtility.CreateCopy(levelData.passengers[i]);

                CreateGridCellVisuals(i, rows, cols, cellClone, passengerClone);

                _currentCells.Add(cellClone);
                _currentPassengers.Add(passengerClone);
            }
            
            Debug.Log($"✓ Loaded grid with {levelData.cells.Count} cells ({levelData.rows}x{levelData.columns})");
        }
        
        private void CreateGridCellAt(int index, int rows, int cols)
        {
            CellData newCell = new();
            PassengerData newPassenger = new();
            
            CreateGridCellVisuals(index, rows, cols, newCell, newPassenger);
            
            _currentCells.Add(newCell);
            _currentPassengers.Add(newPassenger);
        }
        
        private void CreateGridCellVisuals(int index, int rows, int cols, CellData cellData, PassengerData passengerData)
        {
            int row = index / cols;
            int col = index % cols;
            Vector2 position = new(col * CELL_SIZE, row * -CELL_SIZE);

            // Create cell visual
            CellImageItem cellVisual = Instantiate(cellImageItemPrefab, cellEditorContent).GetComponent<CellImageItem>();
            cellVisual.GetComponent<RectTransform>().anchoredPosition = position;
            cellVisual.SetSpriteByType(cellData.cellType);
            cellVisual.SetSpriteRotation(GetCellRotation(cellData));
            
            // Create passenger visual
            PassengerImageItem passengerVisual = Instantiate(passengerImageItemPrefab, passengerEditorContent).GetComponent<PassengerImageItem>();
            passengerVisual.GetComponent<RectTransform>().anchoredPosition = position;
            passengerVisual.SetSpriteByType(passengerData.passengerType);
            
            bool shouldShowPassenger = cellData.isOccupied && !Utilities.IsCellTypeIgnoreOccupied(cellData.cellType);
            passengerVisual.SetSpriteColor(shouldShowPassenger ? colors.GetColorByType(passengerData.colorType) : Color.clear);

            // Create grid button
            GridButtonItem gridButton = Instantiate(gridButtonItemPrefab, gridButtonEditorContent).GetComponent<GridButtonItem>();
            gridButton.index = index;
            gridButton.GetComponent<RectTransform>().anchoredPosition = position;
        }
        
        private float GetCellRotation(CellData cellData)
        {
            if (cellData is { cellType: eCellType.Tunnel, extraData: TunnelCellData tunnelData })
            {
                return tunnelData.direction switch
                {
                    0 => 0f,
                    1 => 270f,
                    2 => 180f,
                    3 => 90f,
                    _ => 0f
                };
            }
            return 0f;
        }
        
        private void ClearGrid()
        {
            ClearChildren(cellEditorContent);
            ClearChildren(passengerEditorContent);
            ClearChildren(gridButtonEditorContent);
        }
        
        private bool ValidateGridInputs(out int rows, out int cols, out int waitSize)
        {
            bool validRows = int.TryParse(levelRowsInput.text, out rows) && rows > 0;
            bool validCols = int.TryParse(levelColumnsInput.text, out cols) && cols > 0;
            bool validWait = int.TryParse(levelWaitingSizeInput.text, out waitSize) && waitSize > 0;
            
            return validRows && validCols && validWait;
        }

        private void CreateNewVehicle()
        {
            if (!IsLevelSelected()) return;
            
            VehicleData newVehicle = new VehicleData
            {
                colorType = eColorType.Red,
            };
            
            _currentVehicles.Add(newVehicle);
            CreateVehicleListItem(newVehicle, _currentVehicles.Count - 1);
            _selectedVehicleIndex = -1;
            
            ShowInspectorForNothing();
            Debug.Log("✓ Added new vehicle");
        }

        private void DeleteLastVehicle()
        {
            if (!IsLevelSelected() || _currentVehicles.Count <= 0) return;
            
            Destroy(vehicleListContent.GetChild(0).gameObject);
            _currentVehicles.RemoveAt(_currentVehicles.Count - 1);
            _selectedVehicleIndex = -1;
            
            ShowInspectorForNothing();
            Debug.Log("✓ Removed last vehicle");
        }

        private void LoadVehicleList(LevelData levelData)
        {
            _currentVehicles.Clear();
            ClearChildren(vehicleListContent);
            
            if (levelData.vehicles == null) return;
            
            for (int i = 0; i < levelData.vehicles.Count; i++)
            {
                VehicleData clone = (VehicleData)OdinSerializer.SerializationUtility.CreateCopy(levelData.vehicles[i]);
                _currentVehicles.Add(clone);
                CreateVehicleListItem(clone, i);
            }
            
            Debug.Log($"✓ Loaded {_currentVehicles.Count} vehicles");
        }
        
        private void CreateVehicleListItem(VehicleData vehicleData, int index)
        {
            GameObject itemObject = Instantiate(vehicleListItemPrefab, vehicleListContent);
            VehicleListItem itemComponent = itemObject.GetComponent<VehicleListItem>();
            itemComponent.transform.SetAsFirstSibling();
            itemComponent.InitInfo(index, colors.GetColorByType(vehicleData.colorType));
        }

        private void HandleLevelSelected(LevelListItem levelItem)
        {
            _selectedLevelIndex = levelItem.transform.GetSiblingIndex();
            LevelDataEntry selectedEntry = _allLevels[_selectedLevelIndex];
            
            levelEditText.text = $"ĐANG CHỈNH SỬA: {Path.GetFileNameWithoutExtension(selectedEntry.FilePath)} ({_selectedLevelIndex})";
            
            SetMainPanelsActive(true);
            
            levelRowsInput.text = selectedEntry.Data.rows.ToString();
            levelColumnsInput.text = selectedEntry.Data.columns.ToString();
            levelWaitingSizeInput.text = selectedEntry.Data.waitAreaSize.ToString();

            LoadVehicleList(selectedEntry.Data);
            LoadLevelGrid(selectedEntry.Data);
            
            SwitchToCellMode();
            SwitchToSelectMode();
            ResetGridZoom();
            
            ShowInspectorForNothing();
            ResetStatisticsScroll();
        }
        
        private void HandleVehicleSelected(VehicleListItem vehicleItem)
        {
            if (!IsLevelSelected()) return;
            _selectedVehicleIndex = (_currentVehicles.Count - 1) - vehicleItem.transform.GetSiblingIndex();
            ShowVehicleSelectionAt(_selectedVehicleIndex);
            HideGridSelection();
            ShowInspectorForVehicle();
        }

        private void SwitchToSelectMode()
        {
            _currentEditorMode = eEditorMode.Select;
            UpdateModeButtonColors(selectModeEditorButton);
            HideVehicleSelection();
            HideGridSelection();
            ShowInspectorForNothing();
        }
        
        private void SwitchToAddCloneMode()
        {
            _currentEditorMode = eEditorMode.AddClone;
            UpdateModeButtonColors(addCloneModeEditorButton);
            HideVehicleSelection();
            HideGridSelection();
            
            SetupCloneTemplate();
        }
        
        private void SwitchToDeleteMode()
        {
            _currentEditorMode = eEditorMode.Delete;
            UpdateModeButtonColors(deleteModeEditorButton);
            HideVehicleSelection();
            HideGridSelection();
            ShowInspectorForNothing();
        }
        
        private void SwitchToCellMode() 
        {
            _currentGridMode = eGridMode.Cell;
            UpdateGridModeButtonColors(cellModeEditorButton);
            HideVehicleSelection();
            HideGridSelection();
            if (_currentEditorMode == eEditorMode.AddClone)
                SetupCloneTemplate();
            else
                ShowInspectorForNothing();
        }
        
        private void SwitchToPassengerMode() 
        {
            _currentGridMode = eGridMode.Passenger;
            UpdateGridModeButtonColors(passengerModeEditorButton);
            HideVehicleSelection();
            HideGridSelection();
            if (_currentEditorMode == eEditorMode.AddClone)
                SetupCloneTemplate();
            else
                ShowInspectorForNothing();
        }
        
        private void UpdateModeButtonColors(Button activeButton)
        {
            selectModeEditorButton.image.color = INACTIVE_BUTTON_COLOR;
            addCloneModeEditorButton.image.color = INACTIVE_BUTTON_COLOR;
            deleteModeEditorButton.image.color = INACTIVE_BUTTON_COLOR;
            activeButton.image.color = ACTIVE_BUTTON_COLOR;
        }

        private void UpdateGridModeButtonColors(Button activeButton)
        {
            cellModeEditorButton.image.color = INACTIVE_BUTTON_COLOR;
            passengerModeEditorButton.image.color = INACTIVE_BUTTON_COLOR;
            activeButton.image.color = ACTIVE_BUTTON_COLOR;
        }

        private void SetupCloneTemplate()
        {
            switch (_currentGridMode)
            {
                case eGridMode.Passenger:
                {
                    inspectorModeText.text = "THÊM HÀNH KHÁCH";
                    ClearChildren(inspectorModeContent);

                    _cloneTemplatePassenger = new();

                    PassengerInspector passengerInspector = Instantiate(inspectorModePassengerPrefab, inspectorModeContent)
                        .GetComponent<PassengerInspector>();
                    passengerInspector.InitInfo(-1, _cloneTemplatePassenger.passengerType,
                        _cloneTemplatePassenger.colorType, -1, -1);
                    break;
                }
                    
                case eGridMode.Cell:
                {
                    inspectorModeText.text = "THÊM Ô";
                    ClearChildren(inspectorModeContent);

                    _cloneTemplateCell = new();

                    CellInspector cellInspector = Instantiate(inspectorModeCellPrefab, inspectorModeContent)
                        .GetComponent<CellInspector>();
                    cellInspector.InitInfo(-1, _cloneTemplateCell.cellType, -1, -1);
                    break;
                }
            }
        }
        
        private void ShowInspectorForVehicle()
        {
            if (_selectedVehicleIndex < 0)
            {
                ShowInspectorForNothing();
                return;
            }
            
            VehicleData vehicleData = _currentVehicles[_selectedVehicleIndex];
            
            inspectorModeText.text = "CHỈNH SỬA XE";
            ClearChildren(inspectorModeContent);
            
            VehicleInspector inspector = Instantiate(inspectorModeVehiclePrefab, inspectorModeContent)
                .GetComponent<VehicleInspector>();
            inspector.InitInfo(_selectedVehicleIndex, vehicleData.colorType);
            
            HideGridSelection();
        }

        private void ShowInspectorForNothing()
        {
            inspectorModeText.text = "CHỌN ĐỐI TƯỢNG";
            ClearChildren(inspectorModeContent);
            HideVehicleSelection();
            HideGridSelection();
        }

        private void ShowInspectorForCell(int cellIndex)
        {
            if (!IsValidCellIndex(cellIndex)) return;
            
            CellData cellData = _currentCells[cellIndex];
            int rows = int.Parse(levelRowsInput.text);
            int cols = int.Parse(levelColumnsInput.text);
            int row = cellIndex / cols;
            int col = cellIndex % cols;
            
            inspectorModeText.text = "CHỈNH Ô";
            ClearChildren(inspectorModeContent);

            CellInspector inspector = Instantiate(inspectorModeCellPrefab, inspectorModeContent)
                .GetComponent<CellInspector>();
            inspector.InitInfo(cellIndex, cellData.cellType, row, col);

            if (cellData is { cellType: eCellType.Tunnel, extraData: TunnelCellData tunnelData })
            {
                inspector.InitInfoTunnel(tunnelData.direction, tunnelData.passengers);
            }

            HideVehicleSelection();
            ShowGridSelectionAt(row, col);
        }

        private void ShowInspectorForPassenger(int passengerIndex)
        {
            if (!IsValidPassengerIndex(passengerIndex)) return;
            if (Utilities.IsCellTypeIgnoreOccupied(_currentCells[passengerIndex].cellType)) return;
            if (!_currentCells[passengerIndex].isOccupied) return;
            
            PassengerData passengerData = _currentPassengers[passengerIndex];
            int rows = int.Parse(levelRowsInput.text);
            int cols = int.Parse(levelColumnsInput.text);
            int row = passengerIndex / cols;
            int col = passengerIndex % cols;
            
            inspectorModeText.text = "CHỈNH HÀNH KHÁCH";
            ClearChildren(inspectorModeContent);

            PassengerInspector inspector = Instantiate(inspectorModePassengerPrefab, inspectorModeContent)
                .GetComponent<PassengerInspector>();
            inspector.InitInfo(passengerIndex, passengerData.passengerType, passengerData.colorType, row, col);

            if (passengerData is { passengerType: ePassengerType.Rope, extraData: RopePassengerData ropeData })
            {
                inspector.InitInfoRope(ropeData.ropeCount);
            }
            else if (passengerData is { passengerType: ePassengerType.Bomb, extraData: BombPassengerData bombData })
            {
                inspector.InitInfoBomb(bombData.bombTime);
            }
            else if (passengerData is { passengerType: ePassengerType.Cloak, extraData: CloakPassengerData cloakData })
            {
                inspector.InitInfoCloak(cloakData.isRevealed);
            }

            HideVehicleSelection();
            ShowGridSelectionAt(row, col);
        }
        
        private void ShowGridSelectionAt(int row, int col)
        {
            if (row < 0 || col < 0 || !IsLevelSelected())
            {
                HideGridSelection();
                return;
            }
            
            selectGridImage.GetComponent<RectTransform>().anchoredPosition = 
                new Vector2(col * CELL_SIZE, row * -CELL_SIZE);
            selectGridImage.GetComponent<CanvasGroup>().alpha = 1;
        }

        private void HideGridSelection()
        {
            selectGridImage.GetComponent<CanvasGroup>().alpha = 0;
        }
        
        private void ShowVehicleSelectionAt(int vehicleIndex)
        {
            if (vehicleIndex < 0 || !IsLevelSelected())
            {
                HideVehicleSelection();
                return;
            }
            
            int displayIndex = (_currentVehicles.Count - 1) - vehicleIndex;
            selectVehicleImage.GetComponent<RectTransform>().anchoredPosition = 
                vehicleListContent.GetChild(displayIndex).GetComponent<RectTransform>().anchoredPosition;
            selectVehicleImage.GetComponent<CanvasGroup>().alpha = 1;
        }

        private void HideVehicleSelection()
        {
            selectVehicleImage.GetComponent<CanvasGroup>().alpha = 0;
        }
        
        private void ZoomInGrid()
        {
            levelEditorScrollRect.transform.localScale += new Vector3(ZOOM_STEP, ZOOM_STEP, ZOOM_STEP);
        }
        
        private void ZoomOutGrid()
        {
            float currentZoom = levelEditorScrollRect.transform.localScale.x;
            float newZoom = Mathf.Max(MIN_ZOOM, currentZoom - ZOOM_STEP);
            levelEditorScrollRect.transform.localScale = new(newZoom, newZoom);
        }
        
        private void ResetGridZoom()
        {
            levelEditorScrollRect.verticalNormalizedPosition = 1;
            levelEditorScrollRect.horizontalNormalizedPosition = 0;
            levelEditorScrollRect.transform.localScale = Vector3.one;
        }

        private void HandleGridCellInSelectMode(int cellIndex)
        {
            ShowInspectorForCell(cellIndex);
        }

        private void HandleGridCellInAddCloneMode(int cellIndex)
        {
            CellData cloned = (CellData)OdinSerializer.SerializationUtility.CreateCopy(_cloneTemplateCell);
            _currentCells[cellIndex] = cloned;
            
            CellImageItem cellVisual = cellEditorContent.GetChild(cellIndex).GetComponent<CellImageItem>();
            cellVisual.SetSpriteByType(cloned.cellType);
            cellVisual.SetSpriteRotation(GetCellRotation(cloned));

            int cols = int.Parse(levelColumnsInput.text);
            int row = cellIndex / cols;
            int col = cellIndex % cols;
            
            HideVehicleSelection();
            ShowGridSelectionAt(row, col);
        }

        private void HandleGridCellInDeleteMode(int cellIndex)
        {
            CellData clearedCell = _currentCells[cellIndex];
            clearedCell.cellType = eCellType.None;
            clearedCell.isOccupied = false;
            _currentCells[cellIndex] = clearedCell;
            
            CellImageItem cellVisual = cellEditorContent.GetChild(cellIndex).GetComponent<CellImageItem>();
            cellVisual.SetSpriteByType(eCellType.None);
            
            PassengerImageItem passengerVisual = passengerEditorContent.GetChild(cellIndex).GetComponent<PassengerImageItem>();
            passengerVisual.SetSpriteColor(Color.clear);
            
            int cols = int.Parse(levelColumnsInput.text);
            int row = cellIndex / cols;
            int col = cellIndex % cols;
            
            HideVehicleSelection();
            ShowGridSelectionAt(row, col);
        }

        private void HandleGridPassengerInSelectMode(int passengerIndex)
        {
            if (Utilities.IsCellTypeIgnoreOccupied(_currentCells[passengerIndex].cellType)) return;
            if (!_currentCells[passengerIndex].isOccupied) return;
            
            ShowInspectorForPassenger(passengerIndex);
        }

        private void HandleGridPassengerInAddCloneMode(int passengerIndex)
        {
            if (Utilities.IsCellTypeIgnoreOccupied(_currentCells[passengerIndex].cellType)) return;
            
            CellData cell = _currentCells[passengerIndex];
            cell.isOccupied = true;
            _currentCells[passengerIndex] = cell;
            
            PassengerData cloned = (PassengerData)OdinSerializer.SerializationUtility.CreateCopy(_cloneTemplatePassenger);
            _currentPassengers[passengerIndex] = cloned;
            
            PassengerImageItem passengerVisual = passengerEditorContent.GetChild(passengerIndex).GetComponent<PassengerImageItem>();
            passengerVisual.SetSpriteByType(cloned.passengerType);
            passengerVisual.SetSpriteColor(colors.GetColorByType(cloned.colorType));
            
            int cols = int.Parse(levelColumnsInput.text);
            int row = passengerIndex / cols;
            int col = passengerIndex % cols;
            
            HideVehicleSelection();
            ShowGridSelectionAt(row, col);
        }

        private void HandleGridPassengerInDeleteMode(int passengerIndex)
        {
            if (Utilities.IsCellTypeIgnoreOccupied(_currentCells[passengerIndex].cellType)) return;
            
            CellData cell = _currentCells[passengerIndex];
            cell.isOccupied = false;
            _currentCells[passengerIndex] = cell;
            
            PassengerImageItem passengerVisual = passengerEditorContent.GetChild(passengerIndex).GetComponent<PassengerImageItem>();
            passengerVisual.SetSpriteColor(Color.clear);
            
            int cols = int.Parse(levelColumnsInput.text);
            int row = passengerIndex / cols;
            int col = passengerIndex % cols;
            
            HideVehicleSelection();
            ShowGridSelectionAt(row, col);
        }
        
        private void HandleGridCellClicked(GridButtonItem gridButton)
        {
            if (!IsLevelSelected()) return;
            
            int cellIndex = gridButton.index;
            
            switch (_currentGridMode)
            {
                case eGridMode.Cell:
                {
                    switch (_currentEditorMode)
                    {
                        case eEditorMode.Select:
                        {
                            HandleGridCellInSelectMode(cellIndex);
                            break;
                        }
                        case eEditorMode.AddClone:
                        {
                            HandleGridCellInAddCloneMode(cellIndex);
                            break;
                        }
                        case eEditorMode.Delete:
                        {
                            HandleGridCellInDeleteMode(cellIndex);
                            break;
                        }
                    }
                    break;
                }
                case eGridMode.Passenger:
                {
                    switch (_currentEditorMode)
                    {
                        case eEditorMode.Select:
                        {
                            HandleGridPassengerInSelectMode(cellIndex);
                            break;
                        }
                        case eEditorMode.AddClone:
                        {
                            HandleGridPassengerInAddCloneMode(cellIndex);
                            break;
                        }
                        case eEditorMode.Delete:
                        {
                            HandleGridPassengerInDeleteMode(cellIndex);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void HandleVehicleColorChanged(int index, eColorType newColor)
        {
            VehicleData vehicle = _currentVehicles[index];
            vehicle.colorType = newColor;
            _currentVehicles[index] = vehicle;

            int displayIndex = (_currentVehicles.Count - 1) - index;
            VehicleListItem item = vehicleListContent.GetChild(displayIndex).GetComponent<VehicleListItem>();
            item.InitInfo(index, colors.GetColorByType(newColor));
        }
        
        private void HandlePassengerColorChanged(int index, eColorType newColor)
        {
            switch(_currentEditorMode)
            {
                case eEditorMode.Select:
                {
                    PassengerData passenger = _currentPassengers[index];
                    passenger.colorType = newColor;
                    _currentPassengers[index] = passenger;
                    PassengerImageItem item = passengerEditorContent.GetChild(index).GetComponent<PassengerImageItem>();
                    item.SetSpriteColor(colors.GetColorByType(newColor));
                    break;
                }
                case eEditorMode.AddClone:
                {
                    _cloneTemplatePassenger.colorType = newColor;
                    break;
                }
            }
        }
        
        private void HandlePassengerTypeChanged(int index, ePassengerType newType)
        {
            switch(_currentEditorMode)
            {
                case eEditorMode.Select:
                {
                    PassengerData passenger = _currentPassengers[index];
                    passenger.passengerType = newType;
                    switch (passenger.passengerType)
                    {
                        case ePassengerType.Cloak:
                        {
                            HandleCloakRevealChanged(index, true);
                            break;
                        }
                        case ePassengerType.Bomb:
                        {
                            HandleBombTimeChanged(index, 10);
                            break;
                        }
                        case ePassengerType.Rope:
                        {
                            HandleRopeCountChanged(index, 3);
                            break;
                        }
                    }
                    _currentPassengers[index] = passenger;
                    PassengerImageItem item = passengerEditorContent.GetChild(index).GetComponent<PassengerImageItem>();
                    item.SetSpriteByType(newType);
                    break;
                }
                case eEditorMode.AddClone:
                {
                    _cloneTemplatePassenger.passengerType = newType;
                    break;
                }
            }
        }
        
        private void HandleRopeCountChanged(int index, int ropeCount)
        {
            switch (_currentEditorMode)
            {
                case eEditorMode.Select:
                {
                    PassengerData passenger = _currentPassengers[index];
                    passenger.extraData = new RopePassengerData { ropeCount = ropeCount };
                    _currentPassengers[index] = passenger;
                    break;
                }
                case eEditorMode.AddClone:
                {
                    _cloneTemplatePassenger.extraData = new RopePassengerData { ropeCount = ropeCount };
                    break;
                }
            }
        }
        
        private void HandleBombTimeChanged(int index, int bombTime)
        {
            switch (_currentEditorMode)
            {
                case eEditorMode.Select:
                {
                    PassengerData passenger = _currentPassengers[index];
                    passenger.extraData = new BombPassengerData { bombTime = bombTime };;
                    _currentPassengers[index] = passenger;
                    break;
                }
                case eEditorMode.AddClone:
                {
                    _cloneTemplatePassenger.extraData = new BombPassengerData { bombTime = bombTime };
                    break;
                }
            }
        }

        private void HandleCloakRevealChanged(int index, bool isRevealed)
        {
            switch (_currentEditorMode)
            {
                case eEditorMode.Select:
                {
                    PassengerData passenger = _currentPassengers[index];
                    passenger.extraData = new CloakPassengerData { isRevealed = isRevealed };
                    _currentPassengers[index] = passenger;
                    break;
                }
                case eEditorMode.AddClone:
                {
                    _cloneTemplatePassenger.extraData = new CloakPassengerData { isRevealed = isRevealed };
                    break;
                }
            }
        }

        private void HandleCellTypeChanged(int index, eCellType newType)
        {
            switch(_currentEditorMode)
            {
                case eEditorMode.Select:
                {
                    CellData cell = _currentCells[index];
                    cell.cellType = newType;
                    switch (newType)
                    {
                        case eCellType.Tunnel:
                        {
                            cell.extraData = new TunnelCellData();
                            break;
                        }
                    }
                    _currentCells[index] = cell;
                    CellImageItem item = cellEditorContent.GetChild(index).GetComponent<CellImageItem>();
                    item.SetSpriteByType(newType);
                    break;
                }
                case eEditorMode.AddClone:
                {
                    _cloneTemplateCell.cellType = newType;
                    switch (newType)
                    {
                        case eCellType.Tunnel:
                        {
                            _cloneTemplateCell.extraData ??= new TunnelCellData();
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void HandleTunnelDirectionChanged(int index, int direction)
        {
            switch (_currentEditorMode)
            {
                case eEditorMode.Select:
                {
                    CellData data = _currentCells[index];
                    if (data.extraData is TunnelCellData tunnelData)
                    {
                        tunnelData.direction = direction;
                        data.extraData = tunnelData;
                        _currentCells[index] = data;

                        CellImageItem item = cellEditorContent.GetChild(index).GetComponent<CellImageItem>();
                        item.SetSpriteRotation(360f - (tunnelData.direction * 90f));
                    }
                    break;
                }
                case eEditorMode.AddClone:
                {
                    if (_cloneTemplateCell.extraData is TunnelCellData cloneTunnelData)
                    {
                        cloneTunnelData.direction = direction;
                        _cloneTemplateCell.extraData = cloneTunnelData;
                    }
                    break;
                }
            }
        }
        
        private void HandleTunnelPassengerRemoved(int index, int passengerIndex)
        {
            switch (_currentEditorMode)
            {
                case eEditorMode.Select:
                {
                    CellData data = _currentCells[index];
                    if (data.extraData is TunnelCellData tunnelData)
                    {
                        List<PassengerData> list = tunnelData.passengers;
                        list.RemoveAt(passengerIndex);
                        tunnelData.passengers = list;
                        data.extraData = tunnelData;
                        _currentCells[index] = data;
                        //RefreshTunnelInspector(tunnelData);
                        CellInspector item = inspectorModeContent.GetChild(0).GetComponent<CellInspector>();
                        item.RebuildPassengerList(tunnelData.passengers);
                    }
                    break;
                }
                case eEditorMode.AddClone:
                {
                    if (_cloneTemplateCell.extraData is TunnelCellData cloneTunnelData)
                    {
                        List<PassengerData> list = cloneTunnelData.passengers;
                        list.RemoveAt(passengerIndex);
                        _cloneTemplateCell.extraData = new TunnelCellData
                        {
                            direction = cloneTunnelData.direction,
                            passengers = list
                        };
                        CellInspector item = inspectorModeContent.GetChild(0).GetComponent<CellInspector>();
                        item.RebuildPassengerList(cloneTunnelData.passengers);
                    }
                    break;
                }
            }
        }

        private void HandleTunnelPassengerAdded(int index, int passengerIndex)
        {
            PassengerData newPassenger = new();
            switch (_currentEditorMode)
            {
                case eEditorMode.Select:
                {
                    CellData data = _currentCells[index];
                    if (data.extraData is TunnelCellData tunnelData)
                    {
                        List<PassengerData> list = tunnelData.passengers;
                        list.Insert(passengerIndex, newPassenger);
                        tunnelData.passengers = list;
                        data.extraData = tunnelData;
                        _currentCells[index] = data;
                        CellInspector item = inspectorModeContent.GetChild(0).GetComponent<CellInspector>();
                        item.RebuildPassengerList(tunnelData.passengers);
                    }
                    break;
                }
                case eEditorMode.AddClone:
                {
                    if (_cloneTemplateCell.extraData is TunnelCellData cloneTunnelData)
                    {
                        List<PassengerData> list = cloneTunnelData.passengers;
                        list.Insert(passengerIndex, newPassenger);
                        _cloneTemplateCell.extraData = new TunnelCellData
                        {
                            direction = cloneTunnelData.direction,
                            passengers = list
                        };
                        CellInspector item = inspectorModeContent.GetChild(0).GetComponent<CellInspector>();
                        item.RebuildPassengerList(cloneTunnelData.passengers);
                    }
                    break;
                }
            }
        }

        private void HandleTunnelPassengerColorChanged(int index, int passengerIndex, eColorType colorType)
        {
            switch (_currentEditorMode)
            {
                case eEditorMode.Select:
                {
                    CellData data = _currentCells[index];
                    if (data.extraData is TunnelCellData tunnelData)
                    {
                        PassengerData passengerData = tunnelData.passengers[passengerIndex];
                        passengerData.colorType = colorType;
                        tunnelData.passengers[passengerIndex] = passengerData;
                        data.extraData = tunnelData;
                        _currentCells[index] = data;
                    }
                    break;
                }
                case eEditorMode.AddClone:
                {
                    if (_cloneTemplateCell.extraData is TunnelCellData cloneTunnelData)
                    {
                        List<PassengerData> list = cloneTunnelData.passengers;
                        PassengerData passenger = list[passengerIndex];
                        passenger.colorType = colorType;
                        list[passengerIndex] = passenger;
                        _cloneTemplateCell.extraData = new TunnelCellData
                        {
                            direction = cloneTunnelData.direction,
                            passengers = list
                        };
                    }
                    break;
                }
            }
        }
        
        private void SetMainPanelsActive(bool isActive)
        {
            levelPanel.gameObject.SetActive(isActive);
            rightTopPanel.gameObject.SetActive(isActive);
            rightBottomPanel.gameObject.SetActive(isActive);
        }

        private void ClearChildren(Transform parent)
        {
            foreach (Transform child in parent)
            {
                Destroy(child.gameObject);
            }
        }

        private bool IsLevelSelected() => _selectedLevelIndex >= 0;
        
        private bool IsValidCellIndex(int index) => 
            _currentCells.Count > 0 && index >= 0 && index < _currentCells.Count;
        
        private bool IsValidPassengerIndex(int index) => 
            _currentPassengers.Count > 0 && index >= 0 && index < _currentPassengers.Count;

        private void ResetStatisticsScroll()
        {
            statisticsScrollRect.verticalNormalizedPosition = 1;
            statisticsScrollRect.horizontalNormalizedPosition = 0;
            Canvas.ForceUpdateCanvases();
        }
        
        private string BuildStatistics()
        {
            if (_currentCells == null || _currentCells.Count == 0) return "Chưa có dữ liệu level";
            
            int rows = int.Parse(levelRowsInput.text);
            int cols = int.Parse(levelColumnsInput.text);

            StringBuilder sb = new();
            sb.AppendLine("====== GRID ======");
            sb.AppendLine($"Hàng: {rows}");
            sb.AppendLine($"Cột: {cols}");
            sb.AppendLine($"Tổng ô: {_currentCells.Count}");
            
            Dictionary<eCellType, int> cellTypeCount = new();
            foreach (var cell in _currentCells)
            {
                if (!cellTypeCount.ContainsKey(cell.cellType)) cellTypeCount[cell.cellType] = 0;
                cellTypeCount[cell.cellType]++;
            }
            foreach (var kv in cellTypeCount) sb.AppendLine($"Ô {kv.Key}: {kv.Value}");
            
            sb.AppendLine();
            sb.AppendLine("=== PASSENGER ===");
            int totalPassengers = 0;
            Dictionary<ePassengerType, int> passengerByType = new();
            Dictionary<eColorType, int> passengerByColor = new();
            for (int i = 0;i < _currentPassengers.Count; i++)
            {
                CellData cell = _currentCells[i];
                PassengerData passenger = _currentPassengers[i];
                if (cell is { cellType: eCellType.Tunnel, extraData: TunnelCellData tunnel })
                {
                    foreach (var p in tunnel.passengers)
                    {
                        if (p == null) continue;
                        if (!passengerByType.ContainsKey(p.passengerType)) passengerByType[p.passengerType] = 0;
                        passengerByType[p.passengerType]++;
                        if (!passengerByColor.ContainsKey(p.colorType)) passengerByColor[p.colorType] = 0;
                        passengerByColor[p.colorType]++;
                        totalPassengers++;
                    }
                }
                else
                {
                    if (!cell.isOccupied || Utilities.IsCellTypeIgnoreOccupied(cell.cellType)) continue;
                    if (!passengerByType.ContainsKey(passenger.passengerType)) passengerByType[passenger.passengerType] = 0;
                    passengerByType[passenger.passengerType]++;
                    if (!passengerByColor.ContainsKey(passenger.colorType)) passengerByColor[passenger.colorType] = 0;
                    passengerByColor[passenger.colorType]++;
                    totalPassengers++;
                }
            }
            
            sb.AppendLine($"Tổng hành khách: {totalPassengers}");
            sb.AppendLine("- Theo type:");
            foreach (var kv in passengerByType) sb.AppendLine($"  {kv.Key}: {kv.Value}");

            sb.AppendLine("- Theo màu:");
            foreach (var kv in passengerByColor) sb.AppendLine($"  {kv.Key}: {kv.Value}");
            
            sb.AppendLine();
            sb.AppendLine("=== VEHICLE ===");
            int totalVehicles = _currentVehicles.Count;
            Dictionary<eColorType, int> vehicleByColor = new();
            foreach (var v in _currentVehicles)
            {
                if (!vehicleByColor.ContainsKey(v.colorType)) vehicleByColor[v.colorType] = 0;
                vehicleByColor[v.colorType]++;
            }

            sb.AppendLine($"Tổng xe: {totalVehicles}");
            foreach (var kv in vehicleByColor) sb.AppendLine($"Xe màu {kv.Key}: {kv.Value}");
            
            sb.AppendLine();
            sb.AppendLine("=== VALIDATION ===");

            foreach (var kv in passengerByColor)
            {
                if (!vehicleByColor.ContainsKey(kv.Key)) sb.AppendLine($"[!] Có passenger màu {kv.Key} nhưng không có xe");
            }

            if (totalVehicles != 0)
            {
                int soDu = totalPassengers % totalVehicles;
                if (soDu != 0)
                    sb.AppendLine(
                        $"[!] Số lượng hành khách không chia hết cho số xe {totalPassengers}/{totalVehicles} (dư {soDu} hành khách)");
            }
            return sb.ToString();
        }
    }
}
#endif