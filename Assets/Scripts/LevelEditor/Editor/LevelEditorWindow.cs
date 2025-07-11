using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
//using Sirenix.OdinInspector.Editor;
using static BlockStateSO;
using System;

using MoveDirection = BlockBehaviour.Direction;

#if UNITY_EDITOR
using UnityEditorInternal;
using System.Reflection;
#endif


public class LevelEditorWindow : EditorWindow {
    private static string PrefKey_DefaultLevelsFolder_Path => "DefaultLevelsFolder_Path";
    private string defaultLevelFolderPath;

    /// <summary>
    /// String name for a custom persistent Editor preferences key.
    /// Global unique identifier (GUID) for file in project.
    /// Stores a reference to the last level selected so it persists upon opening and closing the window, and Unity application itself
    /// </summary>
    private static string PrefKey_SelectedLevelDataSO_GUID => "SelectedLevelSO_GUID";
    private LevelDataSO levelData;
    //private Vector2Int levelData.GridSize; 

    private static string PrefKey_AvailableBlocksSO_GUID => "AvailableBlocksSOForEditor_GUID";
    private BlockTypesListSO availableBlocks;
    private GameObject selectedBlockTypeToPlace;

    private BlockData selectedBlockOfLevel;
    private BlockData SelectedBlockOfLevel {
        get { return selectedBlockOfLevel; }
        set {
            selectedBlockOfLevel = value;
            proxySelectedBlockMoveList.list = selectedBlockOfLevel?.movePath;
        }
    }

    // ur welcome -Kerry
    [MenuItem("Window/___Level Editor___")]
    [MenuItem("Tools/___Level Editor___")]
    public static void ShowWindow() {
        GetWindow<LevelEditorWindow>("Level Editor").Show();
    }

    private void OnEnable() {
        isDrawingPath = false;

        levelData = TryLoadAssetByEditorKeyGUID<LevelDataSO>(PrefKey_SelectedLevelDataSO_GUID, ref levelData);
        availableBlocks = TryLoadAssetByEditorKeyGUID<BlockTypesListSO>(PrefKey_AvailableBlocksSO_GUID, ref availableBlocks);
        defaultLevelFolderPath = EditorPrefs.GetString(PrefKey_DefaultLevelsFolder_Path, "");

        //wtf
        SetupReordableProxyMoveList();
    }

    private void SetupReordableProxyMoveList() {
        proxySelectedBlockMoveList = new ReorderableList(SelectedBlockOfLevel?.movePath, typeof(MoveDirection), true, true, true, true);

        proxySelectedBlockMoveList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            if (SelectedBlockOfLevel == null || SelectedBlockOfLevel.movePath == null || index >= SelectedBlockOfLevel.movePath.Count) return;

            var element = SelectedBlockOfLevel.movePath[index];
            rect.y += 2;
            element = (MoveDirection)EditorGUI.EnumPopup(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                "Step " + index, // More descriptive label
                element);
            if (SelectedBlockOfLevel.movePath[index] != element) {
                Undo.RecordObject(this, "Changed Move Path Element");
                SelectedBlockOfLevel.movePath[index] = element;
                EditorUtility.SetDirty(this);
            }
        };

        proxySelectedBlockMoveList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Move Path");
        };

        proxySelectedBlockMoveList.onAddCallback = (ReorderableList list) => {
            Undo.RecordObject(this, "Added Move Direction to Path");
            SelectedBlockOfLevel.movePath.Add(MoveDirection.wait); // Add a default value
            EditorUtility.SetDirty(this);
        };

        proxySelectedBlockMoveList.onRemoveCallback = (ReorderableList list) => {
            if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to remove this move step?", "Yes", "No")) {
                Undo.RecordObject(this, "Removed Move Direction from Path");
                SelectedBlockOfLevel.movePath.RemoveAt(list.index);
                EditorUtility.SetDirty(this);
            }
        };

        proxySelectedBlockMoveList.onReorderCallback = (ReorderableList list) => {
            Undo.RecordObject(this, "Reordered Move Path");
            EditorUtility.SetDirty(this);
        };
    }

    private T TryLoadAssetByEditorKeyGUID<T>(string EditorPrefKeyForGUID, ref T targetVar) where T : UnityEngine.Object {
        string guid = EditorPrefs.GetString(EditorPrefKeyForGUID, null);
        if (string.IsNullOrEmpty(guid)) return null;

        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;

        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private void OnDisable() {
        // Debug.Log("trying to set editor prefs for leveldata");
        string path = AssetDatabase.GetAssetPath(levelData);
        GUID gUID = AssetDatabase.GUIDFromAssetPath(path);
        EditorPrefs.SetString(PrefKey_SelectedLevelDataSO_GUID, gUID.ToString());

        // Debug.Log($"level data guid is {EditorPrefs.GetString(PrefKey_SelectedLevelDataSO_GUID)}");

        // Debug.Log("trying to set editor prefs for blocklist");

        path = AssetDatabase.GetAssetPath(availableBlocks);
        gUID = AssetDatabase.GUIDFromAssetPath(path);
        EditorPrefs.SetString(PrefKey_AvailableBlocksSO_GUID, gUID.ToString());

        // Debug.Log($"block list guid is {EditorPrefs.GetString(PrefKey_AvailableBlocksSO_GUID)}");

        EditorPrefs.SetString(PrefKey_DefaultLevelsFolder_Path, defaultLevelFolderPath);
    }


    private Vector2 scrollPosition;

    private void OnGUI() {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.BeginHorizontal();

        // ---------------------- LEFT SIDE ----------------------
        //! Removed GUILayout.ExpandWidth(true) and gave it a minimum width -Kerry
        EditorGUILayout.BeginVertical(GUILayout.MinWidth(400));

        PopulateMoveListOnMouseHover();
        GUILayout.Label("Level Editor", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        defaultLevelFolderPath = EditorGUILayout.TextField("Levels Folder Path", defaultLevelFolderPath);
        if (GUILayout.Button("Select", GUILayout.MaxWidth(60))) {
            string selected = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath)) {
                defaultLevelFolderPath = "Assets" + selected.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        levelData = (LevelDataSO)EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelDataSO), false);


        if (GUILayout.Button(" + ", GUILayout.Width(30))) {
            string path = EditorUtility.SaveFilePanelInProject("Create New Level File", "NewLevel", "asset", "Enter name", defaultLevelFolderPath);
            if (!string.IsNullOrEmpty(path)) {
                var newAsset = ScriptableObject.CreateInstance<LevelDataSO>();
                AssetDatabase.CreateAsset(newAsset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                levelData = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            }
        }

        GUI.enabled = levelData != null;
        if (GUILayout.Button("⎘", GUILayout.Width(30))) {
            string originalPath = AssetDatabase.GetAssetPath(levelData);
            string folder = System.IO.Path.GetDirectoryName(originalPath);
            string filename = System.IO.Path.GetFileNameWithoutExtension(originalPath);

            string path = EditorUtility.SaveFilePanelInProject("Duplicate Level File", $"{filename}_Copy", "asset", "Enter name", folder);
            if (!string.IsNullOrEmpty(path)) {
                var duplicated = UnityEngine.Object.Instantiate(levelData);
                AssetDatabase.CreateAsset(duplicated, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                levelData = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            }
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        availableBlocks = (BlockTypesListSO)EditorGUILayout.ObjectField("Available Blocks", availableBlocks, typeof(BlockTypesListSO), false);

        if (levelData == null) {
            EditorGUILayout.HelpBox("No LevelData SO selected", MessageType.Info);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            return;
        }

        GUILayout.BeginHorizontal();
        levelData.GridSize = EditorGUILayout.Vector2IntField("Grid Size", levelData.GridSize);
        GUILayout.Space(20);
        levelData.GoalCoord = EditorGUILayout.Vector2IntField("Goal Coord", levelData.GoalCoord);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Path Creator Area
        if (Event.current.type == EventType.MouseDown && IsMouseInsidePathCreator()) {
            isDrawingPath = true;
        }

        TrackMouseHover();
        DrawPathGrid();
        DrawPathLines();

        GUI.Button(new Rect(gridOffset.x - 5, gridOffset.y - 5, 10, 10), "");
        DrawClearButton();

        GUILayout.Space(10);

        GUILayout.Label("Level Info", EditorStyles.boldLabel);
        levelData.RetriggerSequenceOnReload = EditorGUILayout.Toggle("Retrigger On Reload", levelData.RetriggerSequenceOnReload);

        FieldInfo titleField = typeof(LevelDataSO).GetField("levelTitle", BindingFlags.NonPublic | BindingFlags.Instance);
        if (titleField != null) {
            string currentTitle = (string)titleField.GetValue(levelData);
            string newTitle = EditorGUILayout.TextField("Level Title", currentTitle);
            if (newTitle != currentTitle) {
                titleField.SetValue(levelData, newTitle);
                EditorUtility.SetDirty(levelData);
            }
        }

        InitializeTutorialMessageList();

        if (tutorialMessagesList != null) {
            GUILayout.Space(10);
            serializedLevelData.Update();
            tutorialMessagesList.DoLayoutList();
            serializedLevelData.ApplyModifiedProperties();
        }

        EditorGUILayout.EndVertical(); // End of Left Side

        // ---------------------- RIGHT SIDE ----------------------
        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        GUILayout.Space(10);
        DrawPresetButtons();
        GUILayout.Space(10);
        DrawGrid();
        GUILayout.Space(10);
        DrawSelectedBlockPathMoveList();
        EditorGUILayout.EndVertical(); // End of Right Side

        // Flexible space removes the horiontal 'nothing' space between left and right side, -Kerry
        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }



    private void InitializeTutorialMessageList() {
        if (levelData == null) {
            serializedLevelData = null;
            tutorialMessagesList = null;
            lastInspectedLevelData = null;
            return;
        }

        serializedLevelData = new SerializedObject(levelData);
        lastInspectedLevelData = levelData;
        var tutorialMessagesProperty = serializedLevelData.FindProperty("tutorialMessages");

        tutorialMessagesList = new ReorderableList(serializedLevelData, tutorialMessagesProperty,
            draggable: true, displayHeader: true, displayAddButton: true, displayRemoveButton: true);

        tutorialMessagesList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Tutorial Messages");
        };

        tutorialMessagesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = tutorialMessagesList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2; // vertical paddign

            Rect anchorRect = new Rect(rect.x, rect.y, 120, EditorGUIUtility.singleLineHeight);
            Rect messageRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 4, rect.width, rect.height - EditorGUIUtility.singleLineHeight - 8);

            SerializedProperty anchorProp = element.FindPropertyRelative("anchorPivot");
            SerializedProperty messageProp = element.FindPropertyRelative("message");

            EditorGUI.PropertyField(anchorRect, anchorProp, GUIContent.none);
            EditorGUI.PropertyField(messageRect, messageProp, GUIContent.none);
        };

        tutorialMessagesList.elementHeightCallback = (int index) => {
            SerializedProperty element = tutorialMessagesList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty messageProp = element.FindPropertyRelative("message");

            float propertyHeight = EditorGUI.GetPropertyHeight(messageProp);
            return propertyHeight + EditorGUIUtility.singleLineHeight + 10f; // 10f is for padding
        };
    }

    private SerializedObject serializedLevelData;
    private ReorderableList tutorialMessagesList;
    private LevelDataSO lastInspectedLevelData;




    ReorderableList proxySelectedBlockMoveList;

    /// <summary>
    /// i cant believe i need a whole proxy list of type ReorderableList to show a list of enums
    /// this part i used some AI - Ryan
    /// </summary>
    private void DrawSelectedBlockPathMoveList() {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Block Details", EditorStyles.boldLabel);

        // In a real scenario, you'd likely have a way to select the BlockData instance
        // For this example, we'll just assume selectedBlockOfLevel is initialized in OnEnable

        /// <summary>
        /// Every time you add a parameter to the level editor, you'll need to bulk set default values for all levels. 
        /// Go to BlockDataFixer.cs to fix this
        /// </summary>
        if (SelectedBlockOfLevel != null) {
            EditorGUILayout.LabelField($"Block at {SelectedBlockOfLevel.gridCoord}", EditorStyles.label);
            selectedBlockOfLevel.pathMode = (BlockBehaviour.BlockMoveState)EditorGUILayout.EnumPopup(selectedBlockOfLevel.pathMode);
            selectedBlockOfLevel.startFrozen = EditorGUILayout.Toggle("Start Frozen?", selectedBlockOfLevel.startFrozen);
            selectedBlockOfLevel.canBeFrozen = EditorGUILayout.Toggle("Can Be Frozen?", selectedBlockOfLevel.canBeFrozen);
            selectedBlockOfLevel.pushableWhenFrozen = EditorGUILayout.Toggle("Pushable When Frozen?", selectedBlockOfLevel.pushableWhenFrozen);
            selectedBlockOfLevel.phaseThrough = EditorGUILayout.Toggle("Phase Through?", selectedBlockOfLevel.phaseThrough);
            if (proxySelectedBlockMoveList != null) {
                proxySelectedBlockMoveList.DoLayoutList();
            }
            else {
                EditorGUILayout.HelpBox("Move path list not initialized.", MessageType.Warning);
            }
        }
        else {
            EditorGUILayout.HelpBox("No BlockData selected.", MessageType.Info);
        }
    }

    private MoveDirection GetMoveDirFromVector2Int(Vector2Int sourceVec) {
        if (sourceVec.y > 0) return MoveDirection.up;
        if (sourceVec.y < 0) return MoveDirection.down;
        if (sourceVec.x > 0) return MoveDirection.right;
        if (sourceVec.x < 0) return MoveDirection.left;

        return MoveDirection.wait;
    }

    private void PopulateMoveListOnMouseHover() {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0) // M1
        {
            isDrawing = true;
            pathCells.Clear();
        }

        if (e.type == EventType.MouseUp && e.button == 0) {
            isDrawing = false;
            List<MoveDirection> moveList = new List<MoveDirection>();

            for (int i = 1; i < pathCells.Count; i++) {
                Vector2Int prev = pathCells[i - 1];
                prev.y *= -1;
                Vector2Int current = pathCells[i];
                current.y *= -1;

                moveList.Add(GetMoveDirFromVector2Int(current - prev));
            }

            if (SelectedBlockOfLevel != null && moveList.Count > 0) {
                SelectedBlockOfLevel.movePath = moveList;
                proxySelectedBlockMoveList.list = selectedBlockOfLevel.movePath;
                EditorUtility.SetDirty(levelData); //! Marks asset as changed
            }

            // Debug.Log("Drawn Path: " + string.Join(", ", pathCells.ConvertAll(cell => $"({cell.x}, {cell.y})")));
            // Debug.Log("Move List: " + string.Join(", ", moveList));
        }
    }

    private void DrawClearButton() {
        if (GUILayout.Button("Clear", GUILayout.Width(80))) {
            levelData.Blocks.Clear();
            selectedBlockOfLevel = null;
            proxySelectedBlockMoveList.list = selectedBlockOfLevel?.movePath;

            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawPresetButtons() {
        GUILayout.Label("Select Block Preset", EditorStyles.boldLabel);

        if (availableBlocks == null) {
            GUILayout.Label("Reference to available blocks list is null, please assign", EditorStyles.label);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        GUIStyle selectedStyle = new GUIStyle(GUI.skin.button);
        selectedStyle.normal.background = MakeTex(2, 2, new Color(0f, 1, 1, .5f));
        selectedStyle.fontStyle = FontStyle.Bold;

        foreach (var blockType in availableBlocks.blockTypes) {
            if (selectedBlockTypeToPlace == blockType) {
                // Button is selected
                if (GUILayout.Button(blockType.name, selectedStyle)) {
                    // If clicked again -> deselefct
                    selectedBlockTypeToPlace = null;
                }
            }
            else {
                // Button is not selceted
                if (GUILayout.Button(blockType.name)) {
                    // If click -> select it
                    selectedBlockTypeToPlace = blockType;
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        if (selectedBlockTypeToPlace == null) {
            EditorGUILayout.LabelField("SELECT MODE: Left-Click to Select. Right-Click to erase.", EditorStyles.centeredGreyMiniLabel);
        }
        else {
            EditorGUILayout.LabelField($"PAINT MODE: '{selectedBlockTypeToPlace.name} (To go back to SELECT mode, click the selected preset button again)'", EditorStyles.centeredGreyMiniLabel);
        }
    }

    private List<Vector2Int> pathCells = new List<Vector2Int>();
    private bool isDrawing = false;
    private Vector2Int lastHoveredCell = new Vector2Int(-1, -1);

    private Vector2 gridOffset = new Vector2();


    private bool IsMouseInsidePathCreator() {
        Vector2 mousePosition = Event.current.mousePosition;
        Vector2 adjustedMousePosition = mousePosition - gridOffset;
        int cellWidth = 30;
        int cellHeight = 30;
        return adjustedMousePosition.x >= 0 && adjustedMousePosition.y >= 0 &&
               adjustedMousePosition.x < levelData.GridSize.x * cellWidth &&
               adjustedMousePosition.y < levelData.GridSize.y * cellHeight;
    }

    private bool isDrawingPath = false;

    private void TrackMouseHover() {
        if (!isDrawingPath) return;

        Vector2 mousePosition = Event.current.mousePosition;
        Vector2 adjustedMousePosition = mousePosition - gridOffset;

        int cellWidth = 30;
        int cellHeight = 30;
        if (adjustedMousePosition.x >= 0 && adjustedMousePosition.y >= 0) {

            int hoveredX = (int)(adjustedMousePosition.x / cellWidth);
            int hoveredY = Mathf.Max(0, (int)(adjustedMousePosition.y / cellHeight));
            //Debug.Log($"drawing grid relative mouse position = {adjustedMousePosition} in grid coords: ({hoveredX}, {hoveredY})");

            if (hoveredX >= 0 && hoveredX < levelData.GridSize.x && hoveredY >= 0 && hoveredY < levelData.GridSize.y) {
                Vector2Int hoveredCell = new Vector2Int(hoveredX, hoveredY);


                //! If you press w while drawing, it adds a wait
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.W) {
                    pathCells.Add(hoveredCell);
                    // Debug.Log("Added Wait to moveList");
                    Event.current.Use();
                }

                if (hoveredCell != lastHoveredCell || Mathf.Abs(hoveredCell.y - lastHoveredCell.y) > 1) {
                    pathCells.Add(hoveredCell);
                    lastHoveredCell = hoveredCell;
                }
            }



        }

        if (Event.current.type == EventType.MouseUp) {
            // Debug.Log("mouse up, exiting drawing");
            isDrawingPath = false;
            pathCells.Clear();
            lastHoveredCell = new Vector2Int(-1, -1);
        }
    }


    private Texture2D MakeTex(int width, int height, Color color) {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = color;
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }

    private void DrawGrid() {
        GUILayout.Label("Level Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Grid Coordinate Origin is the bottom left.", MessageType.Info);
        GUILayout.Space(10);
        for (int y = 0; y < levelData.GridSize.y; y++) {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < levelData.GridSize.x; x++) {

                Vector2Int actualGridCoord = new Vector2Int(x, levelData.GridSize.y - 1 - y);

                var defaultBgColor = GUI.backgroundColor;
                var goalColor = actualGridCoord == levelData.GoalCoord ? Color.red : defaultBgColor;
                GUI.backgroundColor = goalColor;

                var defaultContColor = GUI.contentColor;
                if (selectedBlockOfLevel != null)
                    GUI.contentColor = selectedBlockOfLevel?.gridCoord == actualGridCoord ? Color.yellow : defaultContColor;

                if (GUILayout.Button(GetBlockSymbol(actualGridCoord), GUILayout.Width(30), GUILayout.Height(30))) {
                    PlaceBlock(actualGridCoord);
                }

                GUI.backgroundColor = defaultBgColor;
                GUI.contentColor = defaultContColor;
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawPathGrid() {
        GUILayout.Label("Path Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        var lastRect = new Rect(GUILayoutUtility.GetLastRect());
        if (lastRect.position.y > 50) {
            gridOffset = lastRect.position + Vector2.up * lastRect.height;
            //Debug.Log(gridOffset);
        }

        for (int y = 0; y < levelData.GridSize.y; y++) {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < levelData.GridSize.x; x++) {
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.margin = new RectOffset(0, 0, 0, 0);

                string blockSymbol = " ";

                Vector2Int cellToGridCoord = new Vector2Int(x, levelData.GridSize.y - 1 - y);

                // light green
                if (SelectedBlockOfLevel != null && SelectedBlockOfLevel.gridCoord == cellToGridCoord) {
                    buttonStyle.normal.background = MakeTex(30, 30, new Color(0.353f, 1, 0.471f, 0.5f));
                    blockSymbol = "■";
                }

                // lighter light green
                if (isDrawing && pathCells.Contains(new Vector2Int(x, y))) {
                    buttonStyle.normal.background = MakeTex(30, 30, new Color(0.353f, 1, 0.471f, 0.15f));
                    blockSymbol = "■";
                }

                GUILayout.Button(blockSymbol, buttonStyle, GUILayout.Width(30), GUILayout.Height(30));
            }
            GUILayout.EndHorizontal();
        }


        GUILayout.Label($"started drawing is {isDrawingPath} - mouse is in drawing grid? {IsMouseInsidePathCreator()}");
        if (pathCells.Count > 1)
            GUILayout.Label($"current cell is {pathCells[pathCells.Count - 1]} - last cell is {pathCells[pathCells.Count - 2]}");
    }

    private void DrawPathLines() {
        if (!isDrawingPath || pathCells.Count < 2) return;

        Handles.color = Color.green;

        int cellSize = 30;

        for (int i = 1; i < pathCells.Count; i++) {
            Vector2Int prevCell = pathCells[i - 1];
            //prevCell.y = levelData.GridSize.y - prevCell.y - 1;
            Vector2Int currentCell = pathCells[i];
            //currentCell.y = levelData.GridSize.y - currentCell.y - 1;

            Vector2 startPos = new Vector2(prevCell.x * cellSize, prevCell.y * cellSize) + gridOffset + Vector2.one * cellSize / 2f;
            Vector2 endPos = new Vector2(currentCell.x * cellSize, currentCell.y * cellSize) + gridOffset + Vector2.one * cellSize / 2f;

            Handles.DrawLine(startPos, endPos);
        }
    }




    private string GetBlockSymbol(Vector2Int coord) {
        BlockData block = levelData.Blocks.Find(b => b.gridCoord == coord);

        if (block == null) {
            return "□";
        }


        // Safety check 
        if (block.blockTypeFab == null) {
            // levelData.Blocks.Remove(block);
            return "❓";
        }

        string prefabName = block.blockTypeFab.name;

        if (prefabName.Contains("Key")) return "🔑";
        if (prefabName.Contains("Void")) return "⚫";
        if (prefabName.Contains("Teleport")) return "🌀";
        if (prefabName.Contains("Wall")) return "⬛";

        // Now handle the Path block by reading its saved data
        if (prefabName.Contains("Path")) {
            if (block.movePath != null && block.movePath.Count > 0) {
                MoveDirection moveDir = block.movePath[0];
                switch (moveDir) {
                    case MoveDirection.wait: return "⏹️";
                    case MoveDirection.up: return "⬆️";
                    case MoveDirection.down: return "⬇️";
                    case MoveDirection.left: return "⬅️";
                    case MoveDirection.right: return "➡️";
                    default: return "⏹️";
                }
            }
            // Path Block with no path initialized (should be defaulted in the prefab)
            return "⏹️";
        }

        // Final fallback
        return "■";
    }


    private void PlaceBlock(Vector2Int position) {
        // Right click -> delete a block. (The only way to delete a block)
        if (Event.current.button == 1) {
            var blockToRemove = levelData.Blocks.Find(b => b.gridCoord == position);
            if (blockToRemove != null) {
                Undo.RecordObject(levelData, "Delete Block");
                if (blockToRemove == SelectedBlockOfLevel) {
                    SelectedBlockOfLevel = null;
                }
                levelData.Blocks.Remove(blockToRemove);
                EditorUtility.SetDirty(levelData);
            }
            Event.current.Use();
            return;
        }

        // From here, deal with Left-Click
        BlockData blockAtPosition = levelData.Blocks.Find(b => b.gridCoord == position);

        if (selectedBlockTypeToPlace != null) {
            if (selectedBlockTypeToPlace.name.Contains("oal")) {
                Undo.RecordObject(levelData, "Set Goal Position");
                levelData.GoalCoord = position;
                EditorUtility.SetDirty(levelData);
                return;
            }

            if (blockAtPosition == null) {


                Undo.RecordObject(levelData, "Place Block");

                var newBlock = new BlockData(selectedBlockTypeToPlace, position);

                if (selectedBlockTypeToPlace.name.Contains("Wall")) {
                    newBlock.startFrozen = true;
                }
                if (selectedBlockTypeToPlace.name.Contains("Tile")) {
                    newBlock.phaseThrough = true;
                }

                levelData.Blocks.Add(newBlock);

                SelectedBlockOfLevel = newBlock;
            }
            else {
                SelectedBlockOfLevel = blockAtPosition;
            }
        }
        else {
            if (blockAtPosition != null) {
                SelectedBlockOfLevel = blockAtPosition;
            }
            else {
                SelectedBlockOfLevel = null;
            }
        }

        EditorUtility.SetDirty(levelData);
        Repaint();
    }

}
