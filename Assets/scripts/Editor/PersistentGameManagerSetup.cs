using UnityEngine;
using UnityEditor;
using System.IO;

public class PersistentGameManagerSetup : EditorWindow
{
    private Texture2D cursorTexture;
    private Vector2 hotSpot = new Vector2(16, 16);

    [MenuItem("Tools/Setup Persistent Game Manager")]
    public static void ShowWindow()
    {
        GetWindow<PersistentGameManagerSetup>("Persistent Game Manager Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Persistent Game Manager Setup", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        cursorTexture = (Texture2D)EditorGUILayout.ObjectField("Cursor Texture", cursorTexture, typeof(Texture2D), false);
        hotSpot = EditorGUILayout.Vector2Field("Cursor Hotspot", hotSpot);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create Persistent Game Manager"))
        {
            CreatePersistentGameManager();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Find Cursor Textures"))
        {
            FindCursorTextures();
        }
    }
    
    private void CreatePersistentGameManager()
    {
        // Check if a PersistentGameManager already exists in the scene
        PersistentGameManager existingManager = FindObjectOfType<PersistentGameManager>();
        if (existingManager != null)
        {
            if (EditorUtility.DisplayDialog("Manager Already Exists", 
                "A PersistentGameManager already exists in the scene. Do you want to update its settings?", 
                "Yes", "No"))
            {
                // Update existing manager
                existingManager.cursorTexture = cursorTexture;
                existingManager.hotSpot = hotSpot;
                EditorUtility.SetDirty(existingManager);
                Debug.Log("Updated existing PersistentGameManager");
            }
            return;
        }
        
        // Create a new GameObject for the manager
        GameObject managerObject = new GameObject("PersistentGameManager");
        PersistentGameManager manager = managerObject.AddComponent<PersistentGameManager>();
        
        // Set properties
        manager.cursorTexture = cursorTexture;
        manager.hotSpot = hotSpot;
        
        // Add CustomCursor component
        CustomCursor cursor = managerObject.AddComponent<CustomCursor>();
        cursor.cursorTexture = cursorTexture;
        cursor.hotSpot = hotSpot;
        manager.customCursor = cursor;
        
        // Create prefab
        if (!Directory.Exists("Assets/Prefabs"))
        {
            Directory.CreateDirectory("Assets/Prefabs");
        }
        
        // Save the prefab
        #if UNITY_2018_3_OR_NEWER
        string prefabPath = "Assets/Prefabs/PersistentGameManager.prefab";
        bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        
        if (prefabExists)
        {
            if (EditorUtility.DisplayDialog("Prefab Already Exists", 
                "The PersistentGameManager prefab already exists. Do you want to replace it?", 
                "Yes", "No"))
            {
                PrefabUtility.SaveAsPrefabAsset(managerObject, prefabPath);
                Debug.Log("Updated PersistentGameManager prefab at " + prefabPath);
            }
        }
        else
        {
            PrefabUtility.SaveAsPrefabAsset(managerObject, prefabPath);
            Debug.Log("Created PersistentGameManager prefab at " + prefabPath);
        }
        #else
        string prefabPath = "Assets/Prefabs/PersistentGameManager.prefab";
        bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        
        if (prefabExists)
        {
            if (EditorUtility.DisplayDialog("Prefab Already Exists", 
                "The PersistentGameManager prefab already exists. Do you want to replace it?", 
                "Yes", "No"))
            {
                PrefabUtility.CreatePrefab(prefabPath, managerObject);
                Debug.Log("Updated PersistentGameManager prefab at " + prefabPath);
            }
        }
        else
        {
            PrefabUtility.CreatePrefab(prefabPath, managerObject);
            Debug.Log("Created PersistentGameManager prefab at " + prefabPath);
        }
        #endif
        
        Selection.activeGameObject = managerObject;
    }
    
    private void FindCursorTextures()
    {
        // Find all cursor textures in the project
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            
            if (texture != null && (texture.name.Contains("Cursor") || path.Contains("Cursor")))
            {
                cursorTexture = texture;
                Debug.Log("Found cursor texture: " + path);
                Repaint();
                return;
            }
        }
        
        Debug.Log("No cursor textures found in the project");
    }
}
