using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>
    /// GrassSettings (ScriptableObject) -> GrassRuntimeConfig dönüşümü. SO yalnızca bellekte
    /// (CreateInstance) yaratılır, asset'e yazılmaz. Alanlar private+serileştirilmiş olduğundan değişiklik
    /// SerializedObject üzerinden yapılır — Inspector'ın kullandığı yolla aynı.
    /// </summary>
    public class GrassSettingsTests
    {
        readonly List<Object> _created = new List<Object>();

        GrassSettings NewSettings()
        {
            var s = ScriptableObject.CreateInstance<GrassSettings>();
            _created.Add(s);
            return s;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) if (o != null) Object.DestroyImmediate(o);
            _created.Clear();
        }

        static void Edit(GrassSettings s, System.Action<SerializedObject> edit)
        {
            var so = new SerializedObject(s);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SerializedProperty Lod(SerializedObject so, int i, string field)
            => so.FindProperty("lods").GetArrayElementAtIndex(i).FindPropertyRelative(field);

        static SerializedProperty Rule(SerializedObject so, int i, string field)
            => so.FindProperty("layerRules").GetArrayElementAtIndex(i).FindPropertyRelative(field);

        static ValidationReport Build(GrassSettings s, out GrassRuntimeConfig config, IReadOnlyList<string> terrainNames = null)
        {
            bool ok = s.TryBuildRuntime(terrainNames, out config, out ValidationReport report);
            Assert.AreEqual(ok, report.IsValid);
            Assert.AreEqual(ok, config != null, "Config yalnızca geçerli ayarda üretilmeli");
            return report;
        }

        static void AssertHasError(ValidationReport report, string fragment)
            => Assert.IsTrue(report.Errors.Any(e => e.Contains(fragment)),
                             $"'{fragment}' içeren hata bekleniyordu. Rapor:\n{report}");

        static readonly string[] DefaultTerrainNames = { "Grass_A_TerrainLayer", "Grass_Dry_TerrainLayer", "Snow_TerrainLayer", "Muddy_TerrainLayer" };

        [Test]
        public void RuntimeConfig_ExposesReadOnlyArrays_AndCopyIsIndependent()
        {
            ValidationReport report = Build(NewSettings(), out GrassRuntimeConfig c);
            Assert.IsTrue(report.IsValid, report.ToString());

            // Dış yüz dizi olarak geri çevrilememeli: aksi halde çağıran paylaşılan içeriği değiştirebilirdi.
            Assert.IsNull(c.LodInstanceBudgets as int[], "LodInstanceBudgets ham dizi olarak sızmamalı");
            Assert.IsNull(c.LodMeshes as Mesh[], "LodMeshes ham dizi olarak sızmamalı");

            int[] copy = c.CopyLodInstanceBudgets();
            copy[0] = -1;
            CollectionAssert.AreEqual(new[] { 400000, 600000, 800000 }, c.LodInstanceBudgets,
                                      "Kopyayı değiştirmek config'i etkilememeli");
            Assert.AreNotSame(copy, c.CopyLodInstanceBudgets(), "Her çağrı yeni kopya vermeli");
        }

        [Test]
        public void LayerError_DoesNotHideOtherErrors_AndAddsNoDensityError()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => so.FindProperty("chunkSize").floatValue = -1f);

            // Tek layer'lı terrain: 4 kanallık varsayılan kurallar ona uymaz => layer hatası, mapper kurulamaz ve
            // üretim doğrulaması sıfır yoğunluk fallback'iyle sürer (chunkSize hatası layer hatasının ardında kalmamalı).
            ValidationReport report = Build(s, out _, new[] { "Foo" });

            Assert.IsFalse(report.IsValid);
            AssertHasError(report, "chunkSize");
            Assert.IsTrue(report.Errors.Any(e => !e.Contains("chunkSize")), $"Layer hatası bekleniyordu. Rapor:\n{report}");
            Assert.IsFalse(report.Errors.Any(e => e.Contains("layerDensity")),
                           $"Sıfır yoğunluk fallback'i sahte layerDensity hatası üretmemeli. Rapor:\n{report}");
        }

        [Test]
        public void DefaultSettings_AreValid_AndMapToCoreTypes()
        {
            GrassSettings s = NewSettings();
            ValidationReport report = Build(s, out GrassRuntimeConfig c);

            Assert.IsTrue(report.IsValid, report.ToString());
            Assert.AreEqual(0, report.Warnings.Count, report.ToString());

            Assert.AreEqual(3, c.Lods.Count);
            CollectionAssert.AreEqual(new[] { 625f, 3600f, 14400f }, c.Lods.MaxDistanceSq);
            CollectionAssert.AreEqual(new[] { 1f, 0.35f, 0.1f }, c.Lods.KeepRatio);
            CollectionAssert.AreEqual(new[] { 400000, 600000, 800000 }, c.LodInstanceBudgets);
            Assert.AreEqual(120f, c.DrawDistance);
            Assert.AreEqual(120f, c.Lods.DrawDistance);
            Assert.AreEqual(0.3f, c.CullPadding);

            Assert.AreEqual(16f, c.Generate.ChunkSize);
            Assert.AreEqual(64, c.Generate.CellsPerChunkAxis);
            Assert.AreEqual(new Vector4(1f, 0.6f, 0f, 0.15f), c.Layers.DensityMultipliers);
            Assert.AreEqual(c.Layers.DensityMultipliers, c.Generate.LayerDensity);

            Assert.AreEqual(3, c.LodMeshes.Count);
            foreach (Mesh m in c.LodMeshes) Assert.IsNull(m, "Varsayılanda mesh boş = çizim katmanı varsayılanı kullanır");
        }

        [Test]
        public void DefaultSettings_MatchCoreLodDefaults()
        {
            // Tek kaynak: SO varsayılanı GrassLodDefaults'tan türer; sapma olursa burada yakalanır.
            Build(NewSettings(), out GrassRuntimeConfig c);
            Assert.IsTrue(LodDistanceTable.TryCreate(GrassLodDefaults.Create(), out LodDistanceTable expected, out _));
            CollectionAssert.AreEqual(expected.MaxDistanceSq, c.Lods.MaxDistanceSq);
            CollectionAssert.AreEqual(expected.WidthCompensation, c.Lods.WidthCompensation);
            CollectionAssert.AreEqual(expected.TransitionBand, c.Lods.TransitionBand);
        }

        [Test]
        public void AssignedMesh_IsCarriedPerLod()
        {
            GrassSettings s = NewSettings();
            var mesh = new Mesh();
            _created.Add(mesh);
            Edit(s, so => Lod(so, 1, "mesh").objectReferenceValue = mesh);

            Assert.IsTrue(Build(s, out GrassRuntimeConfig c).IsValid);
            Assert.IsNull(c.LodMeshes[0]);
            Assert.AreSame(mesh, c.LodMeshes[1]);
            Assert.IsNull(c.LodMeshes[2]);
        }

        [Test]
        public void EmptyLodList_IsError()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => so.FindProperty("lods").arraySize = 0);

            AssertHasError(Build(s, out GrassRuntimeConfig c), "LOD listesi boş");
            Assert.IsNull(c);
        }

        [Test]
        public void MoreThanThreeLods_IsError()
        {
            GrassSettings s = NewSettings();
            Edit(s, so =>
            {
                so.FindProperty("lods").arraySize = 4; // 4. eleman 3.'nün kopyası
                Lod(so, 3, "maxDistance").floatValue = 200f;
                Lod(so, 3, "keepRatio").floatValue = 0.05f;
            });
            AssertHasError(Build(s, out _), "LOD sayısı");
        }

        [Test]
        public void UnsortedDistances_IsError()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => Lod(so, 1, "maxDistance").floatValue = 20f); // LOD0 (25) sonrası geriye gitti
            AssertHasError(Build(s, out _), "artan sıra");
        }

        [Test]
        public void IncreasingKeepRatio_IsError()
        {
            // Artan keepRatio pop'suz alt küme garantisini bozar (bkz. LodDistanceTable) -> artık hata.
            GrassSettings s = NewSettings();
            Edit(s, so => Lod(so, 2, "keepRatio").floatValue = 0.5f);
            AssertHasError(Build(s, out _), "keepRatio");
        }

        [TestCase(0f)]
        [TestCase(-0.2f)]
        [TestCase(1.5f)]
        [TestCase(float.NaN)]
        public void InvalidKeepRatio_IsError(float keep)
        {
            GrassSettings s = NewSettings();
            Edit(s, so => Lod(so, 1, "keepRatio").floatValue = keep);
            AssertHasError(Build(s, out _), "keepRatio");
        }

        [TestCase("maxDistance", float.NaN, "maxDistance")]
        [TestCase("maxDistance", -5f, "maxDistance")]
        [TestCase("maxWidthCompensation", 0.5f, "maxWidthCompensation")]
        [TestCase("maxWidthCompensation", float.NaN, "maxWidthCompensation")]
        [TestCase("transitionBand", -1f, "transitionBand")]
        [TestCase("transitionBand", 500f, "transitionBand")] // LOD'un kendi aralığından büyük
        public void InvalidLodField_IsError(string field, float value, string expectedFragment)
        {
            GrassSettings s = NewSettings();
            Edit(s, so => Lod(so, 1, field).floatValue = value);
            AssertHasError(Build(s, out _), expectedFragment);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(GrassGpuResources.MaxInstanceCapacity + 1)]
        public void InstanceBudgetOutOfRange_IsError(int budget)
        {
            GrassSettings s = NewSettings();
            Edit(s, so => Lod(so, 0, "instanceBudget").intValue = budget);
            AssertHasError(Build(s, out _), "instanceBudget");
        }

        [Test]
        public void DrawDistanceBelowLastLod_IsError()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => so.FindProperty("drawDistance").floatValue = 100f); // son LOD 120'de bitiyor
            AssertHasError(Build(s, out _), "drawDistance");
        }

        [Test]
        public void DrawDistanceAboveLastLod_IsOnlyAWarning()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => so.FindProperty("drawDistance").floatValue = 200f);

            ValidationReport report = Build(s, out GrassRuntimeConfig c);
            Assert.IsTrue(report.IsValid, report.ToString());
            Assert.AreEqual(1, report.Warnings.Count);
            Assert.AreEqual(200f, c.DrawDistance);
        }

        [TestCase(float.NaN)]
        [TestCase(0f)]
        [TestCase(-3f)]
        public void InvalidDrawDistance_IsError(float value)
        {
            GrassSettings s = NewSettings();
            Edit(s, so => so.FindProperty("drawDistance").floatValue = value);
            AssertHasError(Build(s, out _), "drawDistance");
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        public void InvalidCullPadding_IsError(float value)
        {
            GrassSettings s = NewSettings();
            Edit(s, so => so.FindProperty("cullPadding").floatValue = value);
            AssertHasError(Build(s, out _), "cullPadding");
        }

        [Test]
        public void LayerIndexBeyondFourChannels_IsError()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => Rule(so, 3, "layerIndex").intValue = 4); // tek RGBA alphamap = 0..3
            AssertHasError(Build(s, out _), "layerIndex 4");
        }

        [Test]
        public void DuplicateLayerRule_IsError()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => Rule(so, 1, "layerIndex").intValue = 0);
            AssertHasError(Build(s, out _), "birden fazla kural");
        }

        [Test]
        public void InvalidRuleValues_AreErrors()
        {
            GrassSettings s = NewSettings();
            Edit(s, so =>
            {
                Rule(so, 0, "densityMultiplier").floatValue = -1f;
                Rule(so, 1, "heightMultiplier").floatValue = 0f;
                Rule(so, 2, "tipTint").colorValue = new Color(float.NaN, 0f, 0f, 1f);
            });
            ValidationReport report = Build(s, out _);
            AssertHasError(report, "densityMultiplier");
            AssertHasError(report, "heightMultiplier");
            AssertHasError(report, "tint");
        }

        [Test]
        public void EmptyRuleList_IsError()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => so.FindProperty("layerRules").arraySize = 0);
            AssertHasError(Build(s, out _), "listesi boş");
        }

        [Test]
        public void MissingRuleForTerrainLayer_IsOnlyAWarning()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => so.FindProperty("layerRules").arraySize = 3); // Muddy kuralı düştü

            ValidationReport report = Build(s, out _, DefaultTerrainNames);
            Assert.IsTrue(report.IsValid, report.ToString());
            Assert.AreEqual(1, report.Warnings.Count);
        }

        [Test]
        public void WithTerrainNames_DefaultRulesMatch_NoWarnings()
        {
            ValidationReport report = Build(NewSettings(), out _, DefaultTerrainNames);
            Assert.IsTrue(report.IsValid, report.ToString());
            Assert.AreEqual(0, report.Warnings.Count, report.ToString());
        }

        [Test]
        public void ReorderedTerrainLayers_WarnPerMismatchedExpectedName()
        {
            GrassSettings s = NewSettings();
            var reordered = new[] { "Grass_Dry_TerrainLayer", "Grass_A_TerrainLayer", "Snow_TerrainLayer", "Muddy_TerrainLayer" };

            ValidationReport report = Build(s, out _, reordered);
            Assert.IsTrue(report.IsValid, report.ToString());
            Assert.AreEqual(2, report.Warnings.Count, report.ToString());
        }

        [Test]
        public void WithoutTerrainNames_ExpectedNamesAreNotChecked()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => Rule(so, 0, "expectedLayerName").stringValue = "Baska");
            ValidationReport report = Build(s, out _);
            Assert.AreEqual(0, report.Warnings.Count, report.ToString());
        }

        [Test]
        public void EmptyExpectedName_MeansNoCheck()
        {
            GrassSettings s = NewSettings();
            Edit(s, so => Rule(so, 0, "expectedLayerName").stringValue = "");
            ValidationReport report = Build(s, out _, new[] { "Herhangi", "Grass_Dry_TerrainLayer", "Snow_TerrainLayer", "Muddy_TerrainLayer" });
            Assert.AreEqual(0, report.Warnings.Count, report.ToString());
        }

        [Test]
        public void RuleIndexBeyondRealTerrainLayerCount_IsError()
        {
            // Terrain'de 3 layer var ama kural 3. indekse bağlı: SO tek başına geçerli, terrain'le geçersiz.
            GrassSettings s = NewSettings();
            AssertHasError(Build(s, out _, new[] { "Grass_A_TerrainLayer", "Grass_Dry_TerrainLayer", "Snow_TerrainLayer" }), "layerIndex 3");
        }

        [Test]
        public void GenerateParameterError_IsReported()
        {
            GrassSettings s = NewSettings();
            Edit(s, so =>
            {
                so.FindProperty("slopeMinDeg").floatValue = 60f;
                so.FindProperty("slopeMaxDeg").floatValue = 40f;
            });
            AssertHasError(Build(s, out _), "Eğim");
        }

        [Test]
        public void AllProblems_AreReportedTogether()
        {
            GrassSettings s = NewSettings();
            Edit(s, so =>
            {
                so.FindProperty("chunkSize").floatValue = -1f;           // GrassGenerateSettings
                so.FindProperty("cullPadding").floatValue = -1f;         // SO'nun kendi kuralı
                so.FindProperty("drawDistance").floatValue = 50f;        // < son LOD (120)
                Lod(so, 1, "keepRatio").floatValue = 0f;                 // LodDistanceTable
                Lod(so, 2, "instanceBudget").intValue = 0;               // SO'nun bütçe kuralı
                Rule(so, 0, "layerIndex").intValue = 9;                  // LayerDensityMapper
            });

            ValidationReport report = Build(s, out GrassRuntimeConfig c);
            Assert.IsNull(c);
            AssertHasError(report, "chunkSize");
            AssertHasError(report, "cullPadding");
            AssertHasError(report, "drawDistance");
            AssertHasError(report, "keepRatio");
            AssertHasError(report, "instanceBudget");
            AssertHasError(report, "layerIndex 9");
            Assert.GreaterOrEqual(report.Errors.Count, 6);
        }

        [Test]
        public void GenerateError_IsNotHiddenBehindLayerError()
        {
            // Layer hatası olsa bile üretim parametreleri yine doğrulanır (bağımsız doğrulayıcılar).
            GrassSettings s = NewSettings();
            Edit(s, so =>
            {
                Rule(so, 0, "layerIndex").intValue = 9;
                so.FindProperty("maxDensityPerM2").floatValue = 0f;
            });
            ValidationReport report = Build(s, out _);
            AssertHasError(report, "layerIndex 9");
            AssertHasError(report, "maxDensityPerM2");
        }

        [Test]
        public void ResetToDefaults_RestoresDefaultValidConfig()
        {
            GrassSettings s = NewSettings();
            Edit(s, so =>
            {
                so.FindProperty("chunkSize").floatValue = -1f;
                so.FindProperty("lods").arraySize = 0;
                so.FindProperty("layerRules").arraySize = 0;
            });
            Assert.IsFalse(Build(s, out _).IsValid);

            s.ResetToDefaults();

            ValidationReport report = Build(s, out GrassRuntimeConfig reset);
            Assert.IsTrue(report.IsValid, report.ToString());
            Build(NewSettings(), out GrassRuntimeConfig fresh);
            CollectionAssert.AreEqual(fresh.Lods.MaxDistanceSq, reset.Lods.MaxDistanceSq);
            CollectionAssert.AreEqual(fresh.LodInstanceBudgets, reset.LodInstanceBudgets);
            Assert.AreEqual(fresh.Generate.CellsPerChunkAxis, reset.Generate.CellsPerChunkAxis);
            Assert.AreEqual(fresh.Layers.DensityMultipliers, reset.Layers.DensityMultipliers);
            Assert.AreEqual(fresh.DrawDistance, reset.DrawDistance);
        }

        [Test]
        public void Config_IsSnapshot_LaterSettingsChangesDoNotLeak()
        {
            GrassSettings s = NewSettings();
            Build(s, out GrassRuntimeConfig c);
            Edit(s, so => so.FindProperty("drawDistance").floatValue = 300f);
            Assert.AreEqual(120f, c.DrawDistance);
        }
    }
}
