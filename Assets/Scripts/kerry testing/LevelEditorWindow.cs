using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
//using Sirenix.OdinInspector.Editor;
using static BlockStateSO;
using System;

using MoveDirection = BlockBehaviour.Direction;
using UnityEditorInternal;

public class LevelEditorWindow : EditorWindow {
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

    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow() {
        GetWindow<LevelEditorWindow>("Level Editor").Show();
    }

    private void OnEnable() {
        TryLoadAssetByEditorKeyGUID<LevelDataSO>(PrefKey_AvailableBlocksSO_GUID, ref levelData);
        TryLoadAssetByEditorKeyGUID<BlockTypesListSO>(PrefKey_AvailableBlocksSO_GUID, ref availableBlocks);

        //wtf
        setupReordableProxyMoveList();
    }

    private void setupReordableProxyMoveList() {
        proxySelectedBlockMoveList = new ReorderableList(SelectedBlockOfLevel.movePath, typeof(MoveDirection), true, true, true, true);

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

    private void TryLoadAssetByEditorKeyGUID<T>(string EidtorPrefKeyForGUID, ref T targetVar) where T : UnityEngine.Object {
        string guid = EditorPrefs.GetString(EidtorPrefKeyForGUID, null);
        if (string.IsNullOrEmpty(guid)) return;

        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return;

        targetVar = AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private void OnGUI() {
        PopulateMoveListOnMouseHover();

        GUILayout.Label("Level Editor", EditorStyles.boldLabel);
        levelData = (LevelDataSO)EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelDataSO), false);
        if (levelData == null) {
            EditorGUILayout.HelpBox("No LevelData SO selected", MessageType.Info);
            return;
        }
        levelData.GridSize = EditorGUILayout.Vector2IntField("Grid Size", levelData.GridSize);

        availableBlocks = (BlockTypesListSO)EditorGUILayout.ObjectField("Available Blocks", availableBlocks, typeof(BlockTypesListSO), false);


        GUILayout.Space(10);


        DrawPathGrid();
        DrawPathLines();

        GUILayout.Space(10);

        DrawClearButton();

        GUILayout.Space(10);

        DrawPresetButtons();

        GUILayout.Space(10);

        DrawGrid();

        TrackMouseHover();

        DrawSelectedBlockPathMoveList();
    }

    ReorderableList proxySelectedBlockMoveList;

    /// <summary>
    /// i cant believe i need a whole proxy list of type ReorderableList to show a list of enums
    /// this part i used some AI - Ryan
    /// </summary>
    private void DrawSelectedBlockPathMoveList() {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Block MovePath", EditorStyles.boldLabel);

        // In a real scenario, you'd likely have a way to select the BlockData instance
        // For this example, we'll just assume selectedBlockOfLevel is initialized in OnEnable
        if (SelectedBlockOfLevel != null) {
            EditorGUILayout.LabelField($"Block at {SelectedBlockOfLevel.gridCoord}", EditorStyles.label);
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
                Vector2Int current = pathCells[i];

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
        if (GUILayout.Button("Clear Level")) {
            levelData.Blocks.Clear();
            selectedBlockOfLevel = null;
            proxySelectedBlockMoveList.list = selectedBlockOfLevel?.movePath;

            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawPresetButtons() {
        GUILayout.Label("Select Block Preset", EditorStyles.boldLabel);

        if(availableBlocks==null) {
            GUILayout.Label("Reference to available blocks list is null, please assign", EditorStyles.label);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        GUIStyle selectedStyle = new GUIStyle(GUI.skin.button);
        selectedStyle.normal.background = MakeTex(2, 2, new Color(0f, 1, 1, .5f));

        foreach (var blockType in availableBlocks.blockTypes) {
            if (selectedBlockTypeToPlace == blockType) {
                if (GUILayout.Button(blockType.name, selectedStyle)) {
                    selectedBlockTypeToPlace = blockType;
                }
            }
            else {
                if (GUILayout.Button(blockType.name)) {
                    selectedBlockTypeToPlace = blockType;
                }
            }
        }

        EditorGUILayout.EndHorizontal();

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


    private void TrackMouseHover() {
        Vector2 mousePosition = Event.current.mousePosition;
        Vector2 adjustedMousePosition = mousePosition - gridOffset;

        if (adjustedMousePosition.x >= 0 && adjustedMousePosition.y >= 0) {
            int cellWidth = 30;
            int cellHeight = 30;

            int hoveredX = (int)(adjustedMousePosition.x / cellWidth);
            int hoveredY = (int)(adjustedMousePosition.y / cellHeight);

            if (hoveredX >= 0 && hoveredX < levelData.GridSize.x && hoveredY >= 0 && hoveredY < levelData.GridSize.y) {
                Vector2Int hoveredCell = new Vector2Int(hoveredX, hoveredY);

                if (isDrawing && hoveredCell != lastHoveredCell) {
                    pathCells.Add(hoveredCell);
                    lastHoveredCell = hoveredCell;
                }
            }
        }

        if (Event.current.type == EventType.MouseUp) {
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

        GUILayout.Space(10);
        for (int y = 0; y < levelData.GridSize.y; y++) {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < levelData.GridSize.x; x++) {

                Vector2Int actualGridCoord = new Vector2Int(x, levelData.GridSize.y - 1 -y);

                var defaultColor = GUI.backgroundColor;
                var color = actualGridCoord == levelData.GoalCoord ? Color.red : defaultColor;
                GUI.backgroundColor = color;

                if (GUILayout.Button(GetBlockSymbol(actualGridCoord), GUILayout.Width(30), GUILayout.Height(30))) {
                    PlaceBlock(actualGridCoord);
                }

                GUI.backgroundColor = defaultColor;
            }
            GUILayout.EndHorizontal();
        }
    }


    // private Vector2Int highlightedPathCell = new Vector2Int(-1, -1);

    private void DrawPathGrid() {
        GUILayout.Label("Path Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        var lastRect = GUILayoutUtility.GetLastRect();
        gridOffset = lastRect.position + Vector2.up * lastRect.height;

        for (int y = 0; y < levelData.GridSize.y; y++) {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < levelData.GridSize.x; x++) {
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
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
    }

    private void DrawPathLines() {
        if (pathCells.Count < 2) return;

        Handles.color = Color.green;

        int cellSize = 32;

        for (int i = 1; i < pathCells.Count; i++) {
            Vector2Int prevCell = pathCells[i - 1];
            Vector2Int currentCell = pathCells[i];

            Vector2 startPos = new Vector2(prevCell.x * cellSize, prevCell.y * cellSize) + gridOffset + new Vector2(cellSize / 2, cellSize / 2);
            Vector2 endPos = new Vector2(currentCell.x * cellSize, currentCell.y * cellSize) + gridOffset + new Vector2(cellSize / 2, cellSize / 2);

            Handles.DrawLine(startPos, endPos);
        }
    }




    private string GetBlockSymbol(Vector2Int coord) {
        foreach (var block in levelData.Blocks) {
            if (block.gridCoord == coord) {
                //safety is something fucked up creating a block
                if(block.blockTypeFab==null) {
                    levelData.Blocks.Remove(block);
                    continue;
                }

                if (block.blockTypeFab.name.Contains("Key")) {
                    return "🔑";
                }
                else {
                    return "■";
                }
            }
        }
        return "□";
    }


    private void PlaceBlock(Vector2Int position) {
        if (selectedBlockTypeToPlace == null) return;

        var existingBlock = levelData.Blocks.Find(b => b.gridCoord == position);

        if(selectedBlockTypeToPlace.name.Contains("oal")) {
            levelData.GoalCoord = position;
            EditorUtility.SetDirty(levelData);
            return;
        }

        if (existingBlock != null) {
            levelData.Blocks.Remove(existingBlock);
        }
        else {
            var newBlock = new BlockData(selectedBlockTypeToPlace, position);
            levelData.Blocks.Add(newBlock);
            SelectedBlockOfLevel = levelData.Blocks[levelData.Blocks.Count-1];
        }

        EditorUtility.SetDirty(levelData);
    }


}
