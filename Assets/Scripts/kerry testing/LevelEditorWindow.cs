using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using static BlockStateSO;
using System;

public class LevelEditorWindow : EditorWindow {
    private LevelData levelData;
    private Vector2Int gridSize;

    private BlockStateSO selectedPreset;
    private List<BlockStateSO> availablePresets = new List<BlockStateSO>();

    private const string ASSET_PATH = "Assets/Scripts/kerry testing/Test Level/";
    private const string PRESETS_ASSET_PATH = "Assets/Scripts/kerry testing/BlockPresets/";

    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow() {
        GetWindow<LevelEditorWindow>("Level Editor");
    }

    private void OnEnable() {
        LoadBlockPresets();
    }

    private void LoadBlockPresets() {
        availablePresets.Clear();
        //
        // TODO: global string path variable
        string[] guids = AssetDatabase.FindAssets("t:BlockStateSO", new[] { PRESETS_ASSET_PATH });

        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BlockStateSO blockPreset = AssetDatabase.LoadAssetAtPath<BlockStateSO>(path);
            if (blockPreset != null)
                availablePresets.Add(blockPreset);
        }
    }


    private void OnGUI() {
        //! Init grid size
        gridSize = levelData.GridSize;

        PopulateMoveListOnMouseHover();

        DrawLevelDataLabel();

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
    }

    private void DrawLevelDataLabel() {
        GUILayout.Label("Level Editor", EditorStyles.boldLabel);
        levelData = (LevelData)EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelData), false);

        if (levelData == null) return;

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
            List<MovementDirection> moveList = new List<MovementDirection>();

            for (int i = 1; i < pathCells.Count; i++) {
                Vector2Int prev = pathCells[i - 1];
                Vector2Int current = pathCells[i];

                if (current.x > prev.x) moveList.Add(MovementDirection.Right);
                else if (current.x < prev.x) moveList.Add(MovementDirection.Left);
                else if (current.y > prev.y) moveList.Add(MovementDirection.Down);
                else if (current.y < prev.y) moveList.Add(MovementDirection.Up);
            }

            if (selectedBlockState != null && moveList.Count > 0) {
                selectedBlockState.MoveList = moveList;
                EditorUtility.SetDirty(selectedBlockState); //! Marks asset as changed
            }

            // Debug.Log("Drawn Path: " + string.Join(", ", pathCells.ConvertAll(cell => $"({cell.x}, {cell.y})")));
            // Debug.Log("Move List: " + string.Join(", ", moveList));
        }
    }

    private void DrawClearButton() {
        if (GUILayout.Button("Clear Level")) {
            foreach (var block in levelData.Blocks) {
                if (block.blockType != null) {
                    string path = ASSET_PATH + block.blockType.name + ".asset";
                    Debug.Log("Deleting: " + path);

                    AssetDatabase.DeleteAsset(path);
                } //"Assets/Scripts/kerry testing/Test Level/
            }
            levelData.Blocks.Clear();
            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawPresetButtons() {
        GUILayout.Label("Select Block Preset", EditorStyles.boldLabel);

        GUIStyle selectedStyle = new GUIStyle(GUI.skin.button);
        selectedStyle.normal.background = MakeTex(2, 2, new Color(.5f, 1, 1, .5f));

        foreach (var preset in availablePresets) {
            if (selectedPreset == preset) {
                if (GUILayout.Button(preset.name, selectedStyle)) {
                    selectedPreset = preset;
                }
            }
            else {
                if (GUILayout.Button(preset.name)) {
                    selectedPreset = preset;
                }
            }
        }

    }

    private List<Vector2Int> pathCells = new List<Vector2Int>();
    private bool isDrawing = false;
    private Vector2Int lastHoveredCell = new Vector2Int(-1, -1);

    private static readonly Vector2 GRID_OFFSET = new Vector2(4f, 75f);


    private bool IsMouseInsidePathCreator() {
        Vector2 mousePosition = Event.current.mousePosition;
        Vector2 adjustedMousePosition = mousePosition - GRID_OFFSET;
        int cellWidth = 30;
        int cellHeight = 30;
        return adjustedMousePosition.x >= 0 && adjustedMousePosition.y >= 0 &&
               adjustedMousePosition.x < gridSize.x * cellWidth &&
               adjustedMousePosition.y < gridSize.y * cellHeight;
    }


    private void TrackMouseHover() {
        Vector2 mousePosition = Event.current.mousePosition;
        Vector2 adjustedMousePosition = mousePosition - GRID_OFFSET;

        if (adjustedMousePosition.x >= 0 && adjustedMousePosition.y >= 0) {
            int cellWidth = 30;
            int cellHeight = 30;

            int hoveredX = (int)(adjustedMousePosition.x / cellWidth);
            int hoveredY = (int)(adjustedMousePosition.y / cellHeight);

            if (hoveredX >= 0 && hoveredX < gridSize.x && hoveredY >= 0 && hoveredY < gridSize.y) {
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
        for (int y = 0; y < gridSize.y; y++) {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < gridSize.x; x++) {
                if (GUILayout.Button(GetBlockSymbol(x, y), GUILayout.Width(30), GUILayout.Height(30))) {
                    PlaceBlock(new Vector2Int(x, y));
                }
            }
            GUILayout.EndHorizontal();
        }
    }


    // private Vector2Int highlightedPathCell = new Vector2Int(-1, -1);

    private void DrawPathGrid() {
        GUILayout.Label("Path Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        for (int y = 0; y < gridSize.y; y++) {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < gridSize.x; x++) {
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                string blockSymbol = " ";
                Vector2Int cellPosition = new Vector2Int(x, y);

                int overlapCount = pathCells.FindAll(cell => cell == cellPosition).Count;
                float alpha = Mathf.Clamp(0.15f + (overlapCount * 0.25f), 0.15f, 1f);

                if (selectedBlockState != null && selectedBlockStatePosition == cellPosition) {
                    buttonStyle.normal.background = MakeTex(30, 30, new Color(1, 0.475f, 0.914f, 0.5f));
                    blockSymbol = "■";
                }
                else if (isDrawing && overlapCount > 0) { // Pink to blue
                    buttonStyle.normal.background = MakeTex(30, 30, new Color(1 - alpha, 1, 1, alpha));
                    blockSymbol = "■";
                }

                GUILayout.Button(blockSymbol, buttonStyle, GUILayout.Width(30), GUILayout.Height(30));
            }
            GUILayout.EndHorizontal();
        }
    }
    //

    private void DrawPathLines() {
        if (pathCells.Count < 2) return;

        Handles.color = Color.green;

        int cellSize = 32;

        for (int i = 1; i < pathCells.Count; i++) {
            Vector2Int prevCell = pathCells[i - 1];
            Vector2Int currentCell = pathCells[i];

            Vector2 startPos = new Vector2(prevCell.x * cellSize, prevCell.y * cellSize) + GRID_OFFSET + new Vector2(cellSize / 2, cellSize / 2);
            Vector2 endPos = new Vector2(currentCell.x * cellSize, currentCell.y * cellSize) + GRID_OFFSET + new Vector2(cellSize / 2, cellSize / 2);

            Handles.DrawLine(startPos, endPos);
        }
    }




    private string GetBlockSymbol(int x, int y) {
        foreach (var block in levelData.Blocks) {
            if (block.position == new Vector2Int(x, y)) {
                if (block.blockType.name.Contains("Goal")) {
                    return "★";
                }
                else if (block.blockType.name.Contains("Key")) {
                    return "🔑";
                }
                else {
                    return "■";
                }
            }
        }
        return "□";
    }

    private BlockStateSO selectedBlockState;
    private Vector2Int selectedBlockStatePosition;

    private void PlaceBlock(Vector2Int position) {
        if (selectedPreset == null) return;

        var existingBlock = levelData.Blocks.Find(b => b.position == position);

        if (existingBlock != null) {
            string path = ASSET_PATH + existingBlock.blockType.name + ".asset";
            AssetDatabase.DeleteAsset(path);
            levelData.Blocks.Remove(existingBlock);
        }
        else {
            BlockStateSO clonedBlockState = ScriptableObject.Instantiate(selectedPreset);
            selectedBlockState = clonedBlockState;
            selectedBlockStatePosition = position;

            string path = ASSET_PATH + selectedPreset.name + position.x + "_" + position.y + ".asset";
            AssetDatabase.CreateAsset(clonedBlockState, path);
            AssetDatabase.SaveAssets();

            levelData.Blocks.Add(new BlockPlacement {
                position = position,
                blockType = clonedBlockState
            });
        }

        //


        EditorUtility.SetDirty(levelData);
    }


}
