#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GridState))]
public class GridStatePD : PropertyDrawer
{
    private const float CELL_SIZE = 20f;
    private const float PADDING = 2f;

    private float gridElementHeight;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get properties
        SerializedProperty blockStates = property.FindPropertyRelative("GridBlockStates");

        SerializedProperty widthProp = property.FindPropertyRelative("GridWidth");
        SerializedProperty heightProp = property.FindPropertyRelative("GridHeight");
        //SerializedProperty objectsProp = property.FindPropertyRelative("BlocksList");
        //if (objectsProp == null) Debug.Log("PD objList is null");

        int width = widthProp.intValue;
        int height = heightProp.intValue;
        //int objectCount = objectsProp.arraySize;

        position.y += EditorGUIUtility.singleLineHeight;

        // Calculate grid size
        float cellSize = position.width * .5f / width;
        float gridStartX = position.x + position.width * .25f;

        float gridWidth = width * cellSize;
        float gridHeight = height * cellSize;
        gridElementHeight = gridHeight;
        Rect gridRect = new Rect(gridStartX, position.y, gridWidth, gridHeight);

        // Draw background
        EditorGUI.DrawRect(gridRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));

        // Create style for cell text
        GUIStyle style = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            normal = { textColor = Color.white }
        };

        //draw grid cell contents
        float startY = position.y + cellSize * height - cellSize * .5f;
        float startX = gridStartX + cellSize * .5f;

        SerializedProperty coordsListProp = property.FindPropertyRelative("BlockCoordList");
        if (coordsListProp == null) Debug.Log("PD coordList is null");
        int coordCount = coordsListProp.arraySize;
        //Debug.Log($"coordlist count is {coordCount}");

        //broken for no reason
        for (int i = 0; i < coordCount; i++) {
                SerializedProperty coordProp = coordsListProp.GetArrayElementAtIndex(i);
                if (coordProp == null) Debug.Log("PD obj in coord is null");

                Vector2Int coord = coordProp?.vector2IntValue ?? Vector2Int.zero;

                Rect cellRect = new Rect(
                    startX + coord.x * cellSize - cellSize * .5f,
                    startY - coord.y * cellSize - cellSize * .5f,
                    cellSize,
                    cellSize
                );

                EditorGUI.DrawRect(cellRect, new Color(0, 0.3f, 0.5f));
                GUI.Label(cellRect, i.ToString(), style);
            }


        // Draw grid lines
        Handles.color = Color.gray;
        for (int x = 0; x <= width; x++) {
            float lineX = gridStartX + x * cellSize;
            Handles.DrawLine(
                new Vector3(lineX, position.y, 0),
                new Vector3(lineX, position.y + gridHeight, 0)
            );
        }
        for (int y = 0; y <= height; y++) {
            float lineY = position.y + y * cellSize;
            Handles.DrawLine(
                new Vector3(gridStartX, lineY, 0),
                new Vector3(gridStartX + gridWidth, lineY, 0)
            );
        }

        float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);
        float extraHeight = gridHeight;

        // Property rect (below my element)
        Rect propertyRect = new Rect(position.x, position.y + extraHeight + EditorGUIUtility.standardVerticalSpacing, position.width, baseHeight);

        EditorGUI.PropertyField(propertyRect, property, label, true);

        //Debug.Log($"PD OnGUI height {gridElementHeight}");
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty heightProp = property.FindPropertyRelative("GridHeight");
        int height = heightProp.intValue;

        //Debug.Log($"PD SetHeight height {gridElementHeight}");

        return EditorGUIUtility.singleLineHeight + // For the label
               gridElementHeight * 2 +   // For the grid
               10f + EditorGUI.GetPropertyHeight(property, label, true) + 
               EditorGUIUtility.standardVerticalSpacing;                               // Extra padding
    }
}
#endif