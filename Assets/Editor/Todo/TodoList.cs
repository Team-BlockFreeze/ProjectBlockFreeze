using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class TodoList : EditorWindow
{
	private static TodoList _window;
	private ListData _listData;
	private string _listDataDirectory = "/Resources/Todo/";
	private string _listDataAssetPath = "Assets/Resources/Todo/TodoList.asset";
	private int _currentOwnerIndex = 0;
	private int _newTaskOwnerIndex = 0;
	private string _newTask;
	private bool showCompletedTasks = true;
	private Vector2 _scrollPosition = Vector2.zero;

	[MenuItem("Window/Todo List %l")]
	public static void Init()
	{
		_window = (TodoList)EditorWindow.GetWindow(typeof(TodoList));
		_window.titleContent = new GUIContent("Todo List");
		_window.autoRepaintOnSceneChange = false;
	}

	public void OnGUI()
	{
		if (_listData == null)
		{
			_listData = AssetDatabase.LoadAssetAtPath(_listDataAssetPath, typeof(ListData)) as ListData;
			if (_listData == null)
			{
				_listData = ScriptableObject.CreateInstance<ListData>();
				Directory.CreateDirectory(Application.dataPath + _listDataDirectory);
				AssetDatabase.CreateAsset(_listData, _listDataAssetPath);
			}
		}

		string[] owners = new string[_listData.owners.Count + 1];
		string[] ownersToSelect = new string[_listData.owners.Count];

		owners[0] = "All Tasks";
		for (int i = 0; i < _listData.owners.Count; i++)
		{
			owners[i + 1] = _listData.owners[i].name;
			ownersToSelect[i] = _listData.owners[i].name;
		}

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Show tasks:", EditorStyles.boldLabel);
		_currentOwnerIndex = EditorGUILayout.Popup(_currentOwnerIndex, owners);
		EditorGUILayout.EndHorizontal();

		GUIStyle itemStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
		itemStyle.alignment = TextAnchor.UpperLeft;
		_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
		int displayCount = 0;

		for (int i = 0; i < _listData.items.Count; i++)
		{
			ListItem item = _listData.items[i];
			ListItemOwner owner = item.owner;
			if (_currentOwnerIndex == 0 || owner.name == _listData.owners[_currentOwnerIndex - 1].name)
			{
				itemStyle.normal.textColor = owner.color;
				if (!item.isComplete)
				{
					displayCount++;
					EditorGUILayout.BeginHorizontal();

					bool newCompleteState = EditorGUILayout.Toggle(item.isComplete, GUILayout.Width(20));
					if (newCompleteState != item.isComplete)
					{
						_listData.items[i].isComplete = newCompleteState;
						MarkDirty();
					}

					EditorGUI.BeginChangeCheck();
					string updatedTask = EditorGUILayout.TextField(item.task, itemStyle);
					if (EditorGUI.EndChangeCheck())
					{
						_listData.items[i].task = updatedTask;
						MarkDirty();
					}

					int newOwnerIndex = EditorGUILayout.Popup(owner.index, ownersToSelect, GUILayout.Width(60));
					if (newOwnerIndex != owner.index)
					{
						_listData.items[i].owner = _listData.owners[newOwnerIndex];
						MarkDirty();
					}

					EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space();
				}
			}
		}

		if (displayCount == 0)
		{
			EditorGUILayout.LabelField("No tasks currently", EditorStyles.largeLabel);
		}

		if (showCompletedTasks && _currentOwnerIndex == 0)
		{
			itemStyle.normal.textColor = Color.grey;
			for (int i = _listData.items.Count - 1; i >= 0; i--)
			{
				if (_listData.items[i].isComplete)
				{
					ListItem item = _listData.items[i];
					EditorGUILayout.BeginHorizontal();

					bool newCompleteState = EditorGUILayout.Toggle(item.isComplete, GUILayout.Width(20));
					if (!newCompleteState)
					{
						_listData.items[i].isComplete = false;
						MarkDirty();
					}

					EditorGUILayout.LabelField(item.task, itemStyle);
					if (GUILayout.Button("x", GUILayout.Width(23)))
					{
						_listData.items.RemoveAt(i);
						MarkDirty();
					}

					EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space();
				}
			}
		}

		EditorGUILayout.EndScrollView();

		// Task creation UI
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Create Task:", EditorStyles.boldLabel);
		_newTaskOwnerIndex = EditorGUILayout.Popup(_newTaskOwnerIndex, ownersToSelect, GUILayout.Width(60));
		EditorGUILayout.EndHorizontal();

		// Capture input but do NOT update database on every keystroke
		_newTask = EditorGUILayout.TextField(_newTask, GUILayout.Height(40));

		if (GUILayout.Button("Create Task") && !string.IsNullOrWhiteSpace(_newTask))
		{
			ListItemOwner newOwner = _listData.owners[_newTaskOwnerIndex];
			_listData.AddTask(newOwner, _newTask);
			_newTask = "";
			GUI.FocusControl(null);
			MarkDirty();
		}
	}

	private void MarkDirty()
	{
		EditorUtility.SetDirty(_listData);
		AssetDatabase.SaveAssets();
	}

	void OnDestroy()
	{
		MarkDirty();
	}
}
