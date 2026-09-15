using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sahnedeki "MapProxy" layer'ýný üstten çeker ve PNG olarak diske kaydeder.
/// Kullaným:
/// 1. Bu script'i boþ bir GameObject'e ekle.
/// 2. "Map Camera" alanýna, sadece MapProxy layer'ýný gören,
///    orthographic ve tam tepeden bakan (rotation.x = 90) bir kamera ata.
/// 3. Inspector'da sað týk > "Bake Map To PNG" (ya da Play modunda deðilken
///    component'in üstündeki üç noktaya týkla).
/// </summary>
public class MapBaker : MonoBehaviour
{
    [Header("Bake ayarlarý")]
    public Camera mapCamera;

    [Tooltip("Çýktý çözünürlüðü (piksel, kare varsayýyoruz)")]
    public int resolution = 2048;

    [Tooltip("Kaydedilecek dosya adý (uzantýsýz)")]
    public string fileName = "chapter_map";

    [Tooltip("Proje içinde kaydedilecek klasör (Assets altýnda)")]
    public string outputFolder = "Assets/MapBakes";

    [ContextMenu("Bake Map To PNG")]
    public void BakeMap()
    {
        if (mapCamera == null)
        {
            Debug.LogError("MapBaker: mapCamera atanmamýþ.");
            return;
        }

        // Geçici bir RenderTexture oluþtur
        RenderTexture rt = new RenderTexture(resolution, resolution, 24);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = mapCamera.targetTexture;

        mapCamera.targetTexture = rt;
        mapCamera.Render();

        RenderTexture.active = rt;

        Texture2D output = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        output.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        output.Apply();

        // Kamerayý ve RenderTexture'ý eski haline getir
        mapCamera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        rt.Release();

        byte[] pngData = output.EncodeToPNG();

#if UNITY_EDITOR
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string path = Path.Combine(outputFolder, fileName + ".png");
        File.WriteAllBytes(path, pngData);
        AssetDatabase.Refresh();

        Debug.Log("MapBaker: Harita kaydedildi -> " + path);

        // Import ayarlarýný UI için uygun hale getir (Sprite, Point/Bilinear vs.)
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
#else
        string path = Path.Combine(Application.persistentDataPath, fileName + ".png");
        File.WriteAllBytes(path, pngData);
        Debug.Log("MapBaker: Harita kaydedildi -> " + path);
#endif

        DestroyImmediate(output);
    }
}