using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// FilledChanger -> StatBarView geçişi. Yalnızca FilledChanger'a dokunur; başka bileşeni değiştirmez.
// Sahne/prefab DOSYASINI KAYDETMEZ: sahneyi/prefab stage'ini "değişti" (dirty) işaretler, kaydetmek kullanıcıya kalır.
// Prefab için LoadPrefabContents+SaveAsPrefabAsset yerine açık Prefab Mode kullanılır; böylece kayıt kullanıcının
// elinde kalır ve yanlışsa Ctrl+Z / kaydetmeden kapatma ile geri alınabilir.
public static class FilledChangerMigrationTool
{
    private const string MenuRoot = "Tools/AdanBye/HUD/";
    private const string ApplyMenu = MenuRoot + "FilledChanger -> StatBarView (açık sahne + açık prefab)";
    private const string PreviewMenu = MenuRoot + "FilledChanger -> StatBarView Önizleme (değiştirmez)";

    private enum Outcome { Convert, RemoveLeftover, SkipPrefabInstance }

    // StatBarView'daki [SerializeField] adlarıyla birebir eşleşmeli; Migrate bunları string ile yazar.
    private static readonly (string name, SerializedPropertyType type)[] RequiredFields =
    {
        ("kind", SerializedPropertyType.Enum),
        ("fill", SerializedPropertyType.ObjectReference),
        ("lowColor", SerializedPropertyType.Color),
        ("midColor", SerializedPropertyType.Color),
        ("highColor", SerializedPropertyType.Color),
    };

    // Alan şeması tipe ait (nesneye değil); gizli, kaydedilmeyen probe üzerinde doğrularız. Böylece
    // gerçek nesneye bileşen eklemeden (yarım durum riski olmadan) ve önizlemede yan etkisiz kontrol edilir.
    private static string FindSchemaProblem()
    {
        var probe = new GameObject("~StatBarViewProbe") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var so = new SerializedObject(probe.AddComponent<StatBarView>());
            foreach (var (name, type) in RequiredFields)
            {
                SerializedProperty p = so.FindProperty(name);
                if (p == null) return $"'{name}' alanı bulunamadı";
                if (p.propertyType != type) return $"'{name}' alanı beklenen tipte değil (beklenen {type}, bulunan {p.propertyType})";
            }
            return null;
        }
        finally { Object.DestroyImmediate(probe); }
    }

    [MenuItem(ApplyMenu)]
    private static void Apply() => Run(dryRun: false);

    [MenuItem(PreviewMenu)]
    private static void Preview() => Run(dryRun: true);

    private static void Run(bool dryRun)
    {
        var log = new StringBuilder();
        int changed = 0, skipped = 0, failed = 0;
        var dirtyScenes = new HashSet<Scene>();
        // Yalnızca dönüştürme gereken nesne varsa probe oluştur; boş çalıştırmada gereksiz iş yapma.
        string schemaProblem = null;
        bool schemaChecked = false;

        foreach (FilledChanger fc in Collect())
        {
            Outcome outcome = Decide(fc);
            string path = HierarchyPath(fc.transform);

            if (outcome == Outcome.Convert)
            {
                if (!schemaChecked) { schemaProblem = FindSchemaProblem(); schemaChecked = true; }
                if (schemaProblem != null)
                {
                    // Yıkıcı adımdan önce vazgeç: FilledChanger yerinde kalır, nesne hiç değişmez.
                    failed++;
                    string msg = $"StatBarView: {schemaProblem}; bu nesne dönüştürülemez: {path}";
                    Debug.LogError($"[FilledChangerMigration] {(dryRun ? "ÖNİZLEME: " : "ATLANDI: ")}{msg}", fc);
                    log.AppendLine($"  ATLANDI (hatalı) {path}: {schemaProblem}");
                    continue;
                }
            }

            if (outcome == Outcome.SkipPrefabInstance)
            {
                skipped++;
                log.AppendLine($"  ATLANDI (prefab instance'ı; kaynak prefab'tan dönüştür): {path}");
                continue;
            }

            log.AppendLine($"  {(dryRun ? "DEĞİŞECEK" : "DEĞİŞTİ")} [{outcome}] {path}"
                + (outcome == Outcome.Convert ? $" -> {(fc.isHunger ? StatKind.Hunger : StatKind.Thirst)}" : "")
                + (fc.fillImage == null ? "  (UYARI: fillImage boş)" : ""));
            changed++;
            if (dryRun) continue;

            Migrate(fc, outcome);
            dirtyScenes.Add(fc.gameObject.scene);
        }

        foreach (Scene scene in dirtyScenes)
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"[FilledChangerMigration] {(dryRun ? "ÖNİZLEME" : "TAMAM")}: {changed} değişiklik, {skipped} atlandı, {failed} atlanan (hatalı)."
            + (changed + skipped + failed > 0 ? "\n" + log : "\n  Dönüştürülecek FilledChanger yok.")
            + (!dryRun && changed > 0 ? "\n  Sahneyi/prefab'ı KAYDETMEYİ unutma (Ctrl+S)." : ""));
    }

    // Açık sahnelerdeki + açık prefab stage'indeki FilledChanger'lar. Asset (persistent) nesneler hariç:
    // Project'teki prefab'a doğrudan dokunmayız, yalnızca Prefab Mode'da açıkken.
    private static IEnumerable<FilledChanger> Collect()
    {
        var found = new List<FilledChanger>();
        foreach (FilledChanger fc in Object.FindObjectsByType<FilledChanger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (fc != null && fc.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(fc)) found.Add(fc);
        }
        return found;
    }

    private static Outcome Decide(FilledChanger fc)
    {
        // Sahnedeki prefab instance'ında bileşen silmek override olur; doğru yer kaynak prefab (Prefab Mode).
        // Prefab Mode içindeki nesne kaynağın kendisi olduğu için IsPartOfPrefabInstance false döner.
        if (PrefabUtility.IsPartOfPrefabInstance(fc)) return Outcome.SkipPrefabInstance;
        // Çift çalıştırma: StatBarView zaten varsa yalnızca artık FilledChanger'ı temizle.
        return fc.TryGetComponent<StatBarView>(out _) ? Outcome.RemoveLeftover : Outcome.Convert;
    }

    private static void Migrate(FilledChanger fc, Outcome outcome)
    {
        GameObject go = fc.gameObject;
        if (outcome == Outcome.Convert)
        {
            StatBarView view = Undo.AddComponent<StatBarView>(go);
            var so = new SerializedObject(view);
            so.FindProperty("kind").enumValueIndex = (int)(fc.isHunger ? StatKind.Hunger : StatKind.Thirst);
            so.FindProperty("fill").objectReferenceValue = fc.fillImage;
            so.FindProperty("lowColor").colorValue = fc.lowColor;
            so.FindProperty("midColor").colorValue = fc.midColor;
            so.FindProperty("highColor").colorValue = fc.highColor;
            so.ApplyModifiedProperties();
        }
        Undo.DestroyObjectImmediate(fc);
    }

    private static string HierarchyPath(Transform t)
    {
        var sb = new StringBuilder(t.name);
        for (Transform p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
        return $"{t.gameObject.scene.name}:{sb}";
    }
}
