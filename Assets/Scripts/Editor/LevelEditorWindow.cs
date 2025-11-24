#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using OdinSerializer = Sirenix.OdinSerializer;

namespace VehicleUnjam
{
    public class LevelEditorWindow : EditorWindow
    {
        private const string LEVEL_NAME_PREFIX = "Level ";
        
        private readonly List<string> _levelPaths = new();
        private readonly List<string> _levelNames = new();
        private readonly List<LevelData> _levelDatas = new();
        
        private int _currentIndex = -1;
        private LevelData _currentLevelData = null;

        private eCellType _currentCellBrushMode = eCellType.Normal;
        private ePassengerType _currentPassengerBrushMode = ePassengerType.Normal;
        
        private ListView _listView;
        private VisualElement _levelPanel;
        
        [MenuItem(Constants.LEVEL_EDITOR_WINDOW_MENU)]
        public static void Open()
        {
            LevelEditorWindow window = GetWindow<LevelEditorWindow>(" ⊞ " + Constants.LEVEL_EDITOR_NAME);
            window.minSize = new Vector2(640, 480);
        }

        private void OnEnable()
        {
            CreateUI();
        }

        void CreateUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Row;

            // ========= LEFT PANEL =========
            var left = new VisualElement
            {
                style =
                {
                    width = new Length(200, LengthUnit.Pixel),
                    flexDirection = FlexDirection.Column,
                    borderRightWidth = 2,
                    borderRightColor = EditorGUIUtility.isProSkin
                        ? new Color(0.15f, 0.15f, 0.15f)
                        : new Color(0.7f, 0.7f, 0.7f),
                }
            };
            
            // Levels label
            left.Add(new Label()
            {
                text = "DANH SÁCH LEVEL",
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    height = 25,
                    alignContent = Align.Center,
                    alignSelf = Align.Center,
                    alignItems = Align.Center
                }
            });
            
            // List
            _listView = new ListView
            {
                style =
                {
                    flexGrow = 1,
                    backgroundColor = new Color(0f, 0f, 0f, 0.15f),
                    borderBottomWidth = 2,
                    borderBottomColor = EditorGUIUtility.isProSkin
                        ? new Color(0.15f, 0.15f, 0.15f)
                        : new Color(0.7f, 0.7f, 0.7f),
                    borderTopWidth = 2,
                    borderTopColor = EditorGUIUtility.isProSkin
                        ? new Color(0.15f, 0.15f, 0.15f)
                        : new Color(0.7f, 0.7f, 0.7f)
                },
                makeItem = () => new Label()
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        paddingLeft = 10,
                        paddingRight = 10,
                    }
                },
                bindItem = (elem, i) => ((Label)elem).text = _levelNames[i],
                selectionType = SelectionType.Single
            };
            _listView.selectionChanged += objs =>
            {
                _currentIndex = _listView.selectedIndex;
                if (_currentIndex >= 0)
                    LoadSelectedLevel();
            };
            left.Add(_listView);
         
            // Toolbar
            var toolbar = new Toolbar()
            {
                style =
                {
                    height = 25
                }
            };
            toolbar.Add(new ToolbarButton(SaveAllLevels)
            {
                text = "Lưu tất cả", 
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    width = new Length(45, LengthUnit.Percent),
                    paddingLeft = 10,
                    paddingRight = 10
                }
            });
            toolbar.Add(new ToolbarButton(AddNewLevel)
            { 
                text = "Thêm", 
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    width = new Length(27.5f, LengthUnit.Percent),
                    paddingLeft = 10,
                    paddingRight = 10,
                }
            });
            toolbar.Add(new ToolbarButton(RemoveSelectedLevel)
            {
                text = "Xoá", 
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    width = new Length(27.5f, LengthUnit.Percent),
                    paddingLeft = 10,
                    paddingRight = 10
                }
                
            });
            left.Add(toolbar);

            root.Add(left);

            // ========= RIGHT PANEL =========
            _levelPanel = new ScrollView();
            _levelPanel.style.flexGrow = 1;
            root.Add(_levelPanel);
            
            LoadAllLevels();
            DrawRightPanel();
        }
        
        void LoadAllLevels()
        {
            int i = 1;
            foreach (var file in Directory.GetFiles(Constants.LEVEL_FOLDER_PATH, "*.json"))
            {
                byte[] bytes = File.ReadAllBytes(file);
                var lvl = OdinSerializer.SerializationUtility.DeserializeValue<LevelData>(bytes, OdinSerializer.DataFormat.JSON);
                _levelDatas.Add(lvl);
                _levelPaths.Add(file);
                _levelNames.Add(LEVEL_NAME_PREFIX + i);
                i++;
            }
            _listView.itemsSource = _levelPaths;
            _listView.Rebuild();
        }
        
        void SaveAllLevels()
        {
            for (int i = 0; i < _levelDatas.Count; i++)
            {
                var bytes = OdinSerializer.SerializationUtility.SerializeValue(_levelDatas[i], OdinSerializer.DataFormat.JSON);
                File.WriteAllBytes(_levelPaths[i], bytes);
            }
            EditorUtility.DisplayDialog("ĐÃ LƯU", "Tất cả level đã được lưu!", "Đóng");
        }
        
        void AddNewLevel()
        {
            int newIndex = _levelDatas.Count;
            string path = Path.Combine(Constants.LEVEL_FOLDER_PATH, $"level_{(newIndex + 1)}.json");
            LevelData newLevel = new LevelData();
            _levelDatas.Add(newLevel);
            _levelPaths.Add(path);
            _levelNames.Add(LEVEL_NAME_PREFIX + (newIndex + 1));
            var bytes = OdinSerializer.SerializationUtility.SerializeValue(newLevel, OdinSerializer.DataFormat.JSON);
            File.WriteAllBytes(path, bytes);
            _currentIndex = newIndex;
            _listView.Rebuild();
            _listView.selectedIndex = _currentIndex;
            LoadSelectedLevel();
        }
        
        void RemoveSelectedLevel()
        {
            if (_currentIndex < 0)
            {
                EditorUtility.DisplayDialog("LỖI", "Chưa chọn level để xoá!", "Đóng");
                return;
            }
            if (!EditorUtility.DisplayDialog("XÁC NHẬN", "Xác nhận xoá level này?", "Có", "Không"))
                return;
            
            // delete meta file
            File.Delete(_levelPaths[_currentIndex]+".meta");
            File.Delete(_levelPaths[_currentIndex]);
            
            _levelPaths.RemoveAt(_currentIndex);
            _levelDatas.RemoveAt(_currentIndex);
            
            // Re-save file names to keep sequence clean
            for (int i = 0; i < _levelPaths.Count; i++)
            {
                string newPath = Path.Combine(Constants.LEVEL_FOLDER_PATH, $"level_{(i + 1)}.json");
                if (_levelPaths[i] != newPath)
                {
                    File.Move(_levelPaths[i], newPath);
                    _levelPaths[i] = newPath;
                }
            }
            _listView.Rebuild();
            _currentIndex = _levelDatas.Count-1;
            _listView.selectedIndex = _currentIndex;
            LoadSelectedLevel();
        }
        
        void LoadSelectedLevel()
        {
            if (_currentIndex < 0)
            {
                DrawRightPanel();
                return;
            }
            _currentLevelData = _levelDatas[_currentIndex];
            DrawRightPanel();
        }

        void DrawRightPanel()
        {
            _levelPanel.Clear();
            if (_currentLevelData == null || _currentIndex == -1)
            {
                var label = new Label("CHỌN MỘT LEVEL ĐỂ CHỈNH SỬA.")
                {
                    style =
                    {
                        flexGrow = 1,
                        paddingTop = new Length(25, LengthUnit.Percent),
                        paddingBottom = new Length(75, LengthUnit.Percent),
                        unityFontStyleAndWeight = FontStyle.Bold,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                };
                _levelPanel.Add(label);
                return;
            }
            _levelPanel.Add(new Label($"Editing Level {_currentIndex + 1}") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            
            // Rows / Cols
            IntegerField rows = new IntegerField("Rows") { value = _currentLevelData.rows };
            rows.RegisterValueChangedCallback(ev => _currentLevelData.rows = ev.newValue);
            _levelPanel.Add(rows);
            
            IntegerField cols = new IntegerField("Cols") { value = _currentLevelData.columns };
            cols.RegisterValueChangedCallback(ev => _currentLevelData.columns = ev.newValue);
            _levelPanel.Add(cols);
            
            IntegerField wait = new IntegerField("Wait Area Size") { value = _currentLevelData.waitAreaSize };
            wait.RegisterValueChangedCallback(ev => _currentLevelData.waitAreaSize = ev.newValue);
            _levelPanel.Add(wait);
            
            // Generate grid
            
            // Brush Modes
            _levelPanel.Add(new Label("Cell Brush Mode"));
            EnumField cellBrush = new EnumField(_currentCellBrushMode);
            cellBrush.RegisterValueChangedCallback(ev => _currentCellBrushMode = (eCellType)ev.newValue);
            _levelPanel.Add(cellBrush);

            _levelPanel.Add(new Label("Passenger Brush Mode"));
            EnumField passengerBrush = new EnumField(_currentPassengerBrushMode);
            passengerBrush.RegisterValueChangedCallback(ev => _currentPassengerBrushMode = (ePassengerType)ev.newValue);
            _levelPanel.Add(passengerBrush);
        }
    }
}
#endif