using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using static BlockStateSO;

public class LevelEditorWindow : EditorWindow
{
    private LevelData levelData;
    private Vector2Int gridSize;

    private BlockStateSO selectedPreset;
    private List<BlockStateSO> availablePresets = new List<BlockStateSO>();

    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }

    private void OnEnable()
    {
        LoadBlockPresets();
    }

    private void LoadBlockPresets()
    {
        availablePresets.Clear();
        string[] guids = AssetDatabase.FindAssets("t:BlockStateSO");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BlockStateSO blockPreset = AssetDatabase.LoadAssetAtPath<BlockStateSO>(path);
            if (blockPreset != null)
                availablePresets.Add(blockPreset);
        }
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0) // M1
        {
            isDrawing = true;
            pathCells.Clear();
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            isDrawing = false;
            List<MovementDirection> moveList = new List<MovementDirection>();

            for (int i = 1; i < pathCells.Count; i++)
            {
                Vector2Int prev = pathCells[i - 1];
                Vector2Int current = pathCells[i];

                if (current.x > prev.x) moveList.Add(MovementDirection.Right);
                else if (current.x < prev.x) moveList.Add(MovementDirection.Left);
                else if (current.y > prev.y) moveList.Add(MovementDirection.Down);
                else if (current.y < prev.y) moveList.Add(MovementDirection.Up);
            }

            if (selectedBlockState != null)
            {
                selectedBlockState.MoveList2 = moveList;
                EditorUtility.SetDirty(selectedBlockState); //! Marks asset as changed
            }

            Debug.Log("Drawn Path: " + string.Join(", ", pathCells.ConvertAll(cell => $"({cell.x}, {cell.y})")));
            Debug.Log("Move List: " + string.Join(", ", moveList));
        }

        GUILayout.Label("Level Editor", EditorStyles.boldLabel);
        levelData = (LevelData)EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelData), false);

        if (levelData == null) return;

        GUILayout.Space(10);


        DrawPathGrid();
        DrawPathLines();

        GUILayout.Space(10);

        gridSize = levelData.GridSize;

        GUILayout.Label("Select Block Preset", EditorStyles.boldLabel);

        GUIStyle selectedStyle = new GUIStyle(GUI.skin.button);
        selectedStyle.normal.background = MakeTex(2, 2, new Color(.5f, 1, 1, .5f));

        foreach (var preset in availablePresets)
        {
            if (selectedPreset == preset)
            {
                if (GUILayout.Button(preset.name, selectedStyle))
                {
                    selectedPreset = preset;
                }
            }
            else
            {
                if (GUILayout.Button(preset.name))
                {
                    selectedPreset = preset;
                }
            }
        }

        GUILayout.Space(10);

        DrawGrid();

        GUILayout.Space(10);


        GUILayout.Space(10);

        TrackMouseHover();

        if (GUILayout.Button("Clear Level"))
        {
            levelData.Blocks.Clear();
            EditorUtility.SetDirty(levelData);
        }
    }

    private List<Vector2Int> pathCells = new List<Vector2Int>();
    private bool isDrawing = false;
    private Vector2Int lastHoveredCell = new Vector2Int(-1, -1);

    private static readonly Vector2 GRID_OFFSET = new Vector2(4f, 75f);


    private void TrackMouseHover()
    {
        Vector2 mousePosition = Event.current.mousePosition;
        Vector2 adjustedMousePosition = mousePosition - GRID_OFFSET;

        if (adjustedMousePosition.x >= 0 && adjustedMousePosition.y >= 0)
        {
            int cellWidth = 30;
            int cellHeight = 30;

            int hoveredX = (int)(adjustedMousePosition.x / cellWidth);
            int hoveredY = (int)(adjustedMousePosition.y / cellHeight);

            if (hoveredX >= 0 && hoveredX < gridSize.x && hoveredY >= 0 && hoveredY < gridSize.y)
            {
                Vector2Int hoveredCell = new Vector2Int(hoveredX, hoveredY);

                if (isDrawing && hoveredCell != lastHoveredCell)
                {
                    pathCells.Add(hoveredCell);
                    lastHoveredCell = hoveredCell;
                }
            }
        }

        if (Event.current.type == EventType.MouseUp)
        {
            pathCells.Clear();
            lastHoveredCell = new Vector2Int(-1, -1);
        }
    }


    private Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = color;
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }

    private void DrawGrid()
    {
        GUILayout.Label("Level Creator", EditorStyles.boldLabel);

        GUILayout.Space(10);
        for (int y = 0; y < gridSize.y; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < gridSize.x; x++)
            {
                if (GUILayout.Button(GetBlockSymbol(x, y), GUILayout.Width(30), GUILayout.Height(30)))
                {
                    PlaceBlock(new Vector2Int(x, y));
                }
            }
            GUILayout.EndHorizontal();
        }
    }


    // private Vector2Int highlightedPathCell = new Vector2Int(-1, -1);

    private void DrawPathGrid()
    {
        GUILayout.Label("Path Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        for (int y = 0; y < gridSize.y; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < gridSize.x; x++)
            {
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                string blockSymbol = " ";

                Vector2Int cellPosition = new Vector2Int(x, y);

                // light green
                if (selectedBlockState != null && selectedBlockStatePosition == cellPosition)
                {
                    buttonStyle.normal.background = MakeTex(30, 30, new Color(0.353f, 1, 0.471f, 0.5f));
                    blockSymbol = "■";
                }

                // lighter light green
                if (isDrawing && pathCells.Contains(cellPosition))
                {
                    buttonStyle.normal.background = MakeTex(30, 30, new Color(0.353f, 1, 0.471f, 0.15f));
                    blockSymbol = "■";
                }

                GUILayout.Button(blockSymbol, buttonStyle, GUILayout.Width(30), GUILayout.Height(30));
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawPathLines()
    {
        if (pathCells.Count < 2) return;

        Handles.color = Color.green;

        int cellSize = 30;

        for (int i = 1; i < pathCells.Count; i++)
        {
            Vector2Int prevCell = pathCells[i - 1];
            Vector2Int currentCell = pathCells[i];

            Vector2 startPos = new Vector2(prevCell.x * cellSize, prevCell.y * cellSize) + GRID_OFFSET + new Vector2(cellSize / 2, cellSize / 2);
            Vector2 endPos = new Vector2(currentCell.x * cellSize, currentCell.y * cellSize) + GRID_OFFSET + new Vector2(cellSize / 2, cellSize / 2);

            Handles.DrawLine(startPos, endPos);
        }
    }




    private string GetBlockSymbol(int x, int y)
    {
        foreach (var block in levelData.Blocks)
        {
            if (block.position == new Vector2Int(x, y))
            {
                if (block.blockType.name == "Goal")
                {
                    return "★";
                }
                else if (block.blockType.name == "Key")
                {
                    return "🔑";
                }
                else
                {
                    return "■";
                }
            }
        }
        return "□";
    }


    private BlockStateSO selectedBlockState;
    private Vector2Int selectedBlockStatePosition;

    private void PlaceBlock(Vector2Int position)
    {
        if (selectedPreset == null) return;

        selectedBlockState = selectedPreset;
        selectedBlockStatePosition = position;

        var existingBlock = levelData.Blocks.Find(b => b.position == position);
        if (existingBlock != null)
        {
            levelData.Blocks.Remove(existingBlock);
        }
        else
        {
            levelData.Blocks.Add(new BlockPlacement
            {
                position = position,
                blockType = selectedPreset
            });
        }


        EditorUtility.SetDirty(levelData);
    }
}
