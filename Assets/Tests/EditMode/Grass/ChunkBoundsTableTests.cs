using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    public class ChunkBoundsTableTests
    {
        // Test terrain'i: 64x64 m, 65x65 örnek (aralık 1 m), chunk 16 m -> 4x4 chunk; her chunk 17x17 örnek kapsar
        // (sınır örnekleri komşularla paylaşılır). Yükseklik: originY=10, ölçek=100 -> düz h=0.5 => Y=60.
        const float OriginY = 10f;
        const float Scale = 100f;
        const float FlatY = 60f;

        static float[,] FlatHeights(int res = 65)
        {
            var h = new float[res, res];
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                    h[z, x] = 0.5f;
            return h;
        }

        static ChunkBoundsTable Build(float[,] heights, float sizeXZ = 64f)
        {
            Assert.IsTrue(ChunkGrid.TryCreate(-32f, -32f, sizeXZ, sizeXZ, 16f, out ChunkGrid grid, out string gridError), gridError);
            Assert.IsTrue(ChunkBoundsTable.TryCreate(grid, heights, OriginY, Scale, out ChunkBoundsTable table, out string error), error);
            return table;
        }

        [Test]
        public void FlatTerrain_AllChunksHaveSameMinMax_AndWorldBounds()
        {
            ChunkBoundsTable table = Build(FlatHeights());

            for (int cz = 0; cz < 4; cz++)
            {
                for (int cx = 0; cx < 4; cx++)
                {
                    Assert.AreEqual(FlatY, table.GetMinY(cx, cz), 1e-4f);
                    Assert.AreEqual(FlatY, table.GetMaxY(cx, cz), 1e-4f);
                }
            }

            Bounds b = table.GetBounds(0, 0); // XZ: -32..-16
            Assert.AreEqual(new Vector3(-24f, FlatY, -24f), b.center);
            Assert.AreEqual(new Vector3(16f, 0f, 16f), b.size);
        }

        [Test]
        public void SinglePeak_RaisesOnlyItsChunk()
        {
            float[,] h = FlatHeights();
            h[20, 40] = 1f; // z=20 -> cz=1, x=40 -> cx=2 (sınırda değil)
            ChunkBoundsTable table = Build(h);

            Assert.AreEqual(110f, table.GetMaxY(2, 1), 1e-4f); // 10 + 1.0*100
            Assert.AreEqual(FlatY, table.GetMinY(2, 1), 1e-4f);
            Assert.AreEqual(FlatY, table.GetMaxY(1, 1), 1e-4f);
            Assert.AreEqual(FlatY, table.GetMaxY(3, 1), 1e-4f);
            Assert.AreEqual(FlatY, table.GetMaxY(2, 0), 1e-4f);

            Bounds b = table.GetBounds(2, 1);
            Assert.AreEqual(85f, b.center.y, 1e-4f);
            Assert.AreEqual(50f, b.size.y, 1e-4f);
            Assert.AreEqual(8f, b.center.x, 1e-4f); // -32 + 32 + 8
        }

        [Test]
        public void PeakOnChunkBoundary_IsIncludedByBothNeighbours()
        {
            float[,] h = FlatHeights();
            h[20, 32] = 1f; // x=32: cx=1 ile cx=2 arasındaki sınır örneği; iki chunk'ın yüzeyini de etkiler
            ChunkBoundsTable table = Build(h);

            Assert.AreEqual(110f, table.GetMaxY(1, 1), 1e-4f);
            Assert.AreEqual(110f, table.GetMaxY(2, 1), 1e-4f);
            Assert.AreEqual(FlatY, table.GetMaxY(0, 1), 1e-4f);
            Assert.AreEqual(FlatY, table.GetMaxY(3, 1), 1e-4f);
        }

        [Test]
        public void PartialEdgeChunk_IsClippedToTerrainAndSeesLastSample()
        {
            // 50 m terrain: 51 örnek, 4 chunk; son chunk yalnızca 2 m genişliğinde.
            float[,] h = FlatHeights(51);
            h[10, 50] = 1f;
            ChunkBoundsTable table = Build(h, 50f);

            Bounds last = table.GetBounds(3, 0);
            Assert.AreEqual(2f, last.size.x, 1e-4f);
            Assert.AreEqual(110f, table.GetMaxY(3, 0), 1e-4f);
        }

        [Test]
        public void UpdateRegion_RecomputesOnlyAffectedChunks()
        {
            float[,] h = FlatHeights();
            ChunkBoundsTable table = Build(h);

            h[20, 40] = 1f;          // bildirilen bölge
            h[50, 10] = 1f;          // bildirilMEYEN düzenleme: seçici güncellemenin kanıtı, tablo bunu görmemeli
            Assert.IsTrue(table.TryUpdateRegion(h, new RectInt(40, 20, 1, 1), out int updated));

            Assert.AreEqual(1, updated);
            Assert.AreEqual(110f, table.GetMaxY(2, 1), 1e-4f);
            Assert.AreEqual(FlatY, table.GetMaxY(0, 3), 1e-4f);
        }

        [Test]
        public void UpdateRegion_OnChunkBoundary_UpdatesBothNeighbours()
        {
            float[,] h = FlatHeights();
            ChunkBoundsTable table = Build(h);

            h[20, 32] = 1f;
            Assert.IsTrue(table.TryUpdateRegion(h, new RectInt(32, 20, 1, 1), out int updated));
            Assert.AreEqual(2, updated);
            Assert.AreEqual(110f, table.GetMaxY(1, 1), 1e-4f);
            Assert.AreEqual(110f, table.GetMaxY(2, 1), 1e-4f);
        }

        [Test]
        public void UpdateRegion_ClipsOverhang_AndIgnoresFullyOutside()
        {
            float[,] h = FlatHeights();
            ChunkBoundsTable table = Build(h);

            Assert.IsTrue(table.TryUpdateRegion(h, new RectInt(500, 500, 10, 10), out int updated));
            Assert.AreEqual(0, updated);

            // Sağ-üst köşeden taşan bölge: yalnızca son chunk(lar) güncellenir, istisna yok.
            Assert.IsTrue(table.TryUpdateRegion(h, new RectInt(60, 60, 100, 100), out updated));
            Assert.Greater(updated, 0);
            Assert.LessOrEqual(updated, 4);
        }

        [Test]
        public void UpdateRegion_WrongSizedArray_ReturnsFalse()
        {
            ChunkBoundsTable table = Build(FlatHeights());
            Assert.IsFalse(table.TryUpdateRegion(FlatHeights(33), new RectInt(0, 0, 1, 1), out int updated));
            Assert.AreEqual(0, updated);
            Assert.IsFalse(table.TryUpdateRegion(null, new RectInt(0, 0, 1, 1), out _));
        }

        [Test]
        public void NaNSample_IsIgnored()
        {
            float[,] h = FlatHeights();
            h[5, 5] = float.NaN;
            ChunkBoundsTable table = Build(h);
            Assert.AreEqual(FlatY, table.GetMinY(0, 0), 1e-4f);
            Assert.AreEqual(FlatY, table.GetMaxY(0, 0), 1e-4f);
        }

        [Test]
        public void AllNaNChunk_FallsBackToOriginY()
        {
            float[,] h = FlatHeights();
            for (int z = 0; z <= 16; z++)
                for (int x = 0; x <= 16; x++)
                    h[z, x] = float.NaN;
            ChunkBoundsTable table = Build(h);
            Assert.AreEqual(OriginY, table.GetMinY(0, 0), 1e-4f);
            Assert.AreEqual(OriginY, table.GetMaxY(0, 0), 1e-4f);
        }

        [Test]
        public void Create_InvalidInput_ReturnsFalseWithError()
        {
            Assert.IsTrue(ChunkGrid.TryCreate(0f, 0f, 64f, 64f, 16f, out ChunkGrid grid, out _));

            Assert.IsFalse(ChunkBoundsTable.TryCreate(grid, null, 0f, 100f, out _, out string error));
            Assert.IsNotEmpty(error);
            Assert.IsFalse(ChunkBoundsTable.TryCreate(grid, new float[1, 1], 0f, 100f, out _, out error));
            Assert.IsNotEmpty(error);
            Assert.IsFalse(ChunkBoundsTable.TryCreate(grid, FlatHeights(), 0f, 0f, out _, out error));
            Assert.IsFalse(ChunkBoundsTable.TryCreate(grid, FlatHeights(), float.NaN, 100f, out _, out error));
            Assert.IsFalse(ChunkBoundsTable.TryCreate(default, FlatHeights(), 0f, 100f, out _, out error));
        }
    }
}
