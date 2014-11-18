using System.Collections;
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public class AutoSave
{
	public static readonly string manualSaveKey = "autosave@manualSave";
	static readonly string lockFilePath = "Library/Backup/Lockfile";
	static readonly string editModeScenePath = "Temp/__EditModeScene";
	static readonly string backupEditModeScenePath = "Library/Backup/__EditModeScene.unity";
	static readonly string backupCurrentScenePath = "Library/Backup/__CurrentScene";
	
	static double nextTime = 0;
	static bool isChangedHierarchy = false;
	
	static AutoSave ()
	{
		IsManualSave = true;
		EditorApplication.playmodeStateChanged += () =>
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) {
				if (EditorApplication.isPlaying) {
					if (IsSavePrefab)
						EditorApplication.SaveAssets ();
					BackupEditSceneFile ();
					
					File.WriteAllText (lockFilePath, EditorApplication.currentScene);
				}
			}
			
			if (EditorApplication.isPlaying == false) {
				File.Delete (lockFilePath);  
			}
		};
		
		
		nextTime = EditorApplication.timeSinceStartup + Interval;
		EditorApplication.update += () =>
		{
			if (isChangedHierarchy && nextTime < EditorApplication.timeSinceStartup) {
				nextTime = EditorApplication.timeSinceStartup + Interval;
				
				IsManualSave = false;
				
				if (IsSaveSceneTimer && 
				    IsAutoSave && 
				    !EditorApplication.isPlaying &&
				    !string.IsNullOrEmpty (EditorApplication.currentScene)) {
					if (IsSavePrefab)
						EditorApplication.SaveAssets ();
					if (IsSaveScene) {
						Debug.Log ("save scene " + System.DateTime.Now);
						EditorApplication.SaveScene ();
					}
				}
			}
			isChangedHierarchy = false;
			IsManualSave = true;
		};
		
		EditorApplication.hierarchyWindowChanged += () =>
		{
			if (! EditorApplication.isPlaying)
				isChangedHierarchy = true;
		};
		
		EditorApplication.update += Init;
	}
	
	static void Init ()
	{
		EditorApplication.update -= Init;
		
		if (EditorApplication.timeSinceStartup > 5)
			return;
		
		if (File.Exists (lockFilePath)) {
			string sceneFile = File.ReadAllText (lockFilePath);
			File.Copy (sceneFile, backupCurrentScenePath, true);
			File.Copy (backupEditModeScenePath, sceneFile, true);
			File.Delete (lockFilePath);
			EditorApplication.OpenScene (sceneFile);
			File.Copy (backupCurrentScenePath, sceneFile, true);
		}
	}
	
	static void BackupEditSceneFile ()
	{
		Directory.CreateDirectory ("Library/Backup");
		File.Copy (editModeScenePath, backupEditModeScenePath, true);
	}
	
	public static bool IsManualSave {
		get {
			return EditorPrefs.GetBool (manualSaveKey);
		}
		private set {
			EditorPrefs.SetBool (manualSaveKey, value);
		}
	}
	
	private static readonly string autoSave = "auto save";
	
	static bool IsAutoSave {
		get {
			string value = EditorUserSettings.GetConfigValue (autoSave);
			return!string.IsNullOrEmpty (value) && value.Equals ("True");
		}
		set {
			EditorUserSettings.SetConfigValue (autoSave, value.ToString ());
		}
	}
	
	private static readonly string autoSavePrefab = "auto save prefab";
	
	static bool IsSavePrefab {
		get {
			string value = EditorUserSettings.GetConfigValue (autoSavePrefab);
			return!string.IsNullOrEmpty (value) && value.Equals ("True");
		}
		set {
			EditorUserSettings.SetConfigValue (autoSavePrefab, value.ToString ());
		}
	}
	
	private static readonly string autoSaveScene = "auto save scene";
	
	static bool IsSaveScene {
		get {
			string value = EditorUserSettings.GetConfigValue (autoSaveScene);
			return!string.IsNullOrEmpty (value) && value.Equals ("True");
		}
		set {
			EditorUserSettings.SetConfigValue (autoSaveScene, value.ToString ());
		}
	}
	
	private static readonly string autoSaveSceneTimer = "auto save scene timer";
	
	static bool IsSaveSceneTimer {
		get {
			string value = EditorUserSettings.GetConfigValue (autoSaveSceneTimer);
			return!string.IsNullOrEmpty (value) && value.Equals ("True");
		}
		set {
			EditorUserSettings.SetConfigValue (autoSaveSceneTimer, value.ToString ());
		}
	}
	
	private static readonly string autoSaveInterval = "save scene interval";
	
	static int Interval {
		get {
			
			string value = EditorUserSettings.GetConfigValue (autoSaveInterval);
			if (value == null) {
				value = "60";
			}
			return int.Parse (value);
		}
		set {
			if (value < 60)
				value = 60;
			EditorUserSettings.SetConfigValue (autoSaveInterval, value.ToString ());
		}
	}
	
	[PreferenceItem("Auto Save")] 
	static void ExampleOnGUI ()
	{
		bool isAutoSave = EditorGUILayout.BeginToggleGroup ("auto save", IsAutoSave);
		
		IsAutoSave = isAutoSave;
		EditorGUILayout.Space ();
		
		IsSavePrefab = EditorGUILayout.ToggleLeft ("save prefab", IsSavePrefab);
		IsSaveScene = EditorGUILayout.ToggleLeft ("save scene", IsSaveScene);
		IsSaveSceneTimer = EditorGUILayout.BeginToggleGroup ("save scene interval", IsSaveSceneTimer);
		Interval = EditorGUILayout.IntField ("interval(sec) min60sec", Interval);
		EditorGUILayout.EndToggleGroup ();
		EditorGUILayout.EndToggleGroup ();
	}
	
	public static void Backup ()
	{
		if( string.IsNullOrEmpty(EditorApplication.currentScene ))
			return;
		
		string expoertPath = "Library/Backup/" + EditorApplication.currentScene;
		
		Directory.CreateDirectory (Path.GetDirectoryName (expoertPath));
		
		if( string.IsNullOrEmpty(EditorApplication.currentScene))
			return;
		
		byte[] data = File.ReadAllBytes (EditorApplication.currentScene);
		File.WriteAllBytes (expoertPath, data);
	}
	
	[MenuItem("File/Backup/Rollback")]
	public static void RollBack ()
	{
		if (string.IsNullOrEmpty (EditorApplication.currentScene))
			return;
		
		string expoertPath = "Library/Backup/" + EditorApplication.currentScene;
		
		byte[] data = File.ReadAllBytes (expoertPath);
		File.WriteAllBytes (EditorApplication.currentScene, data);
		AssetDatabase.Refresh (ImportAssetOptions.Default);
	}
	
}


class SceneBackup : UnityEditor.AssetModificationProcessor
{
	static string[] OnWillSaveAssets (string[] paths)
	{
		bool manualSave = AutoSave.IsManualSave;
		if (manualSave) {
			AutoSave.Backup ();
		}
		
		return paths;
	}
}