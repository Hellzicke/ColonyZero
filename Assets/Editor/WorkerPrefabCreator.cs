using UnityEngine;
using UnityEditor;
using CT.Workers;

public class WorkerPrefabCreator : MonoBehaviour
{
    [MenuItem("CT/Create Worker Prefab")]
    public static void CreateWorkerPrefab()
    {
        // Create the worker GameObject
        GameObject workerGO = new GameObject("Worker");
        
        // Add required components
        SpriteRenderer sr = workerGO.AddComponent<SpriteRenderer>();
        Rigidbody2D rb = workerGO.AddComponent<Rigidbody2D>();
        CircleCollider2D col = workerGO.AddComponent<CircleCollider2D>();
        Worker worker = workerGO.AddComponent<Worker>();
        
        // Configure SpriteRenderer
        sr.sprite = CreateWorkerSprite();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 10;
        
        // Configure Rigidbody2D
        rb.gravityScale = 0f;
        rb.drag = 8f;
        rb.angularDrag = 10f;
        rb.freezeRotation = true;
        
        // Configure Collider
        col.radius = 0.3f;
        col.isTrigger = false;
        
        // Configure Worker component with good defaults
        worker.moveSpeed = 2f;
        worker.rotationSpeed = 10f;
        worker.idleWanderRadius = 5f;
        worker.minIdleTime = 2f;
        worker.maxIdleTime = 8f;
        worker.showDebugInfo = true;
        
        // Save as prefab
        string prefabPath = "Assets/Prefabs/Worker.prefab";
        PrefabUtility.SaveAsPrefabAsset(workerGO, prefabPath);
        
        // Clean up the GameObject in scene
        DestroyImmediate(workerGO);
        
        Debug.Log($"Worker prefab created at: {prefabPath}");
        
        // Select the created prefab
        Object prefabAsset = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
        Selection.activeObject = prefabAsset;
        EditorGUIUtility.PingObject(prefabAsset);
    }
    
    private static Sprite CreateWorkerSprite()
    {
        // Create a simple colored square texture for the worker
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color workerColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green color
        Color borderColor = new Color(0.1f, 0.6f, 0.1f, 1f); // Darker green for border
        
        // Fill the texture
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                // Create a border effect
                if (x == 0 || x == size - 1 || y == 0 || y == size - 1 ||
                    x == 1 || x == size - 2 || y == 1 || y == size - 2)
                {
                    texture.SetPixel(x, y, borderColor);
                }
                else
                {
                    texture.SetPixel(x, y, workerColor);
                }
            }
        }
        
        texture.Apply();
        
        // Create sprite from texture
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), // Pivot at center
            size // Pixels per unit
        );
        
        sprite.name = "WorkerSprite";
        
        // Save the texture as an asset
        string texturePath = "Assets/Art/Generated/WorkerTexture.png";
        System.IO.Directory.CreateDirectory("Assets/Art/Generated");
        byte[] pngData = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(texturePath, pngData);
        
        AssetDatabase.Refresh();
        
        return sprite;
    }
}
