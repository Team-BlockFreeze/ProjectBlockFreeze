using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

[CustomEditor(typeof(Transform))]
[CanEditMultipleObjects]
public class CustomTransform : Editor
{
    private Transform _transform;
    private GUILayoutOption layoutMaxWidth = null;
    private bool showChildren = false;


    //! Menu Item

    private static bool isCustomEditorEnabled = false;

    [MenuItem("Tools/Fucking around with editor tools")]
    private static void ToggleCustomEditor()
    {
        isCustomEditorEnabled = !isCustomEditorEnabled;
        Debug.Log($"Custom Transform Inspector is now {(isCustomEditorEnabled ? "Enabled" : "Disabled")}");
    }



    public override void OnInspectorGUI()
    {
        if (layoutMaxWidth == null)
            layoutMaxWidth = GUILayout.MaxWidth(600);

        _transform = (Transform)target;

        StandardTransformInspector();

        if (isCustomEditorEnabled == false) return;

        QuaternionInspector();

        Transform targetTransform = (Transform)target;
        int childCount = targetTransform.childCount;

        EditorGUILayout.Space();
        showChildren = EditorGUILayout.Foldout(showChildren, "Children (" + childCount + ")");

        if (showChildren && childCount > 0)
        {
            EditorGUI.indentLevel++;
            ShowChildrenRecursive(targetTransform);
            EditorGUI.indentLevel--;
        }
    }

    private void ShowChildrenRecursive(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            EditorGUILayout.ObjectField(child.gameObject, typeof(GameObject), true);

            if (child.childCount > 0)
            {
                ShowChildrenRecursive(child);
            }
        }
    }


    private void StandardTransformInspector()
    {
        bool didPositionChange = false;
        bool didRotationChange = false;
        bool didScaleChange = false;

        Vector3 initialLocalPosition = _transform.localPosition;
        Vector3 initialLocalEuler = _transform.localEulerAngles;
        Vector3 initialLocalScale = _transform.localScale;

        EditorGUI.BeginChangeCheck();
        Vector3 localPosition = EditorGUILayout.Vector3Field("Position", _transform.localPosition, layoutMaxWidth);
        if (EditorGUI.EndChangeCheck())
            didPositionChange = true;

        EditorGUI.BeginChangeCheck();
        Vector3 localEulerAngles = EditorGUILayout.Vector3Field(
            "Euler Rotation",
            _transform.localEulerAngles,
            layoutMaxWidth);

        if (EditorGUI.EndChangeCheck())
            didRotationChange = true;

        EditorGUI.BeginChangeCheck();
        Vector3 localScale = EditorGUILayout.Vector3Field("Scale", _transform.localScale, layoutMaxWidth);
        if (EditorGUI.EndChangeCheck())
            didScaleChange = true;

        if (didPositionChange || didRotationChange || didScaleChange)
        {
            Undo.RecordObject(_transform, _transform.name);

            if (didPositionChange)
                _transform.localPosition = localPosition;

            if (didRotationChange)
                _transform.localEulerAngles = localEulerAngles;

            if (didScaleChange)
                _transform.localScale = localScale;

        }

        Transform[] selectedTransforms = Selection.transforms;
        if (selectedTransforms.Length > 1)
        {
            foreach (var item in selectedTransforms)
            {
                if (didPositionChange || didRotationChange || didScaleChange)
                    Undo.RecordObject(item, item.name);

                if (didPositionChange)
                {
                    item.localPosition = ApplyChangesOnly(
                        item.localPosition, initialLocalPosition, _transform.localPosition);
                }

                if (didRotationChange)
                {
                    item.localEulerAngles = ApplyChangesOnly(
                        item.localEulerAngles, initialLocalEuler, _transform.localEulerAngles);
                }

                if (didScaleChange)
                {
                    item.localScale = ApplyChangesOnly(
                        item.localScale, initialLocalScale, _transform.localScale);
                }

            }
        }
    }

    private Vector3 ApplyChangesOnly(Vector3 toApply, Vector3 initial, Vector3 changed)
    {
        if (!Mathf.Approximately(initial.x, changed.x))
            toApply.x = _transform.localPosition.x;

        if (!Mathf.Approximately(initial.y, changed.y))
            toApply.y = _transform.localPosition.y;

        if (!Mathf.Approximately(initial.z, changed.z))
            toApply.z = _transform.localPosition.z;

        return toApply;
    }



    private static bool quaternionFoldout = false;
    private void QuaternionInspector()
    {
        quaternionFoldout = EditorGUILayout.Foldout(quaternionFoldout, "Quaternion Rotation:    " + _transform.localRotation.ToString("F3"));
        if (quaternionFoldout)
        {
            Vector4 q = QuaternionToVector4(_transform.localRotation);
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Note: Only z value matters because 2d");
            EditorGUILayout.BeginHorizontal(layoutMaxWidth);
            GUILayout.Label("X");
            q.x = EditorGUILayout.FloatField(q.x);
            GUILayout.Label("Y");
            q.y = EditorGUILayout.FloatField(q.y);
            GUILayout.Label("Z");
            q.z = EditorGUILayout.FloatField(q.z);
            GUILayout.Label("W");
            q.w = EditorGUILayout.FloatField(q.w);
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_transform, "modify quaternion rotation on " + _transform.name);
                _transform.localRotation = ConvertToQuaternion(q);
            }
        }
    }


    private Quaternion ConvertToQuaternion(Vector4 v4)
    {
        return new Quaternion(v4.x, v4.y, v4.z, v4.w);
    }


    private Vector4 QuaternionToVector4(Quaternion q)
    {
        return new Vector4(q.x, q.y, q.z, q.w);
    }




    private AnimBool m_showExtraFields;
    private static bool _showExtraFields;

    void OnEnable()
    {
        m_showExtraFields = new AnimBool(_showExtraFields);
        m_showExtraFields.valueChanged.AddListener(Repaint);
    }



    public enum AlignToType { lastSelected, firstSelected }
    public enum AxisFlag { X = 1, Y = 2, Z = 4 }

    public AlignToType alignTo = AlignToType.lastSelected;
    public AxisFlag alignmentAxis = AxisFlag.X;

}