using NUnit.Framework;
using Unity.Mathematics;

namespace AdanBye.Grass.Tests
{
    public class ChunkGridTests
    {
        // Gerçek terrain: 1000x1000, origin (-500, -500), chunk 16 m -> 62.5 => 63 chunk (sonuncusu kısmi).
        static ChunkGrid CreateTerrainGrid()
        {
            Assert.IsTrue(ChunkGrid.TryCreate(-500f, -500f, 1000f, 1000f, 16f, out ChunkGrid grid, out string error), error);
            return grid;
        }

        [Test]
        public void Create_RealTerrain_HasPartialLastChunk()
        {
            ChunkGrid grid = CreateTerrainGrid();
            Assert.AreEqual(63, grid.CountX);
            Assert.AreEqual(63, grid.CountZ);
            Assert.AreEqual(63 * 63, grid.Count);
        }

        [Test]
        public void Create_ExactMultiple_DoesNotAddEmptyChunk()
        {
            Assert.IsTrue(ChunkGrid.TryCreate(0f, 0f, 64f, 64f, 16f, out ChunkGrid grid, out _));
            Assert.AreEqual(4, grid.CountX);
            Assert.AreEqual(4, grid.CountZ);
        }

        [Test]
        public void WorldToChunk_NegativeOrigin_MapsCorners()
        {
            ChunkGrid grid = CreateTerrainGrid();

            Assert.IsTrue(grid.TryWorldToChunk(-500f, -500f, out int cx, out int cz));
            Assert.AreEqual((0, 0), (cx, cz));

            Assert.IsTrue(grid.TryWorldToChunk(0f, 0f, out cx, out cz)); // (0 - -500) / 16 = 31.25
            Assert.AreEqual((31, 31), (cx, cz));

            Assert.IsTrue(grid.TryWorldToChunk(-484f, -485f, out cx, out cz)); // tam chunk sınırı: -484 -> cx=1
            Assert.AreEqual((1, 0), (cx, cz));
        }

        [Test]
        public void WorldToChunk_FarEdge_BelongsToLastChunk()
        {
            ChunkGrid grid = CreateTerrainGrid();
            Assert.IsTrue(grid.TryWorldToChunk(500f, 500f, out int cx, out int cz));
            Assert.AreEqual((62, 62), (cx, cz));
        }

        [Test]
        public void WorldToChunk_OutsideOrNaN_ReturnsFalse()
        {
            ChunkGrid grid = CreateTerrainGrid();
            Assert.IsFalse(grid.TryWorldToChunk(-500.01f, 0f, out _, out _));
            Assert.IsFalse(grid.TryWorldToChunk(0f, 500.01f, out _, out _));
            Assert.IsFalse(grid.TryWorldToChunk(float.NaN, 0f, out _, out _));
            Assert.IsFalse(grid.TryWorldToChunk(0f, float.PositiveInfinity, out _, out _));
        }

        [Test]
        public void WorldToChunkClamped_OutsideSnapsToEdgeChunk()
        {
            ChunkGrid grid = CreateTerrainGrid();
            Assert.AreEqual(new int2(0, 62), grid.WorldToChunkClamped(-9999f, 9999f));
        }

        [Test]
        public void ChunkRect_LastChunkIsClippedToTerrainEdge()
        {
            ChunkGrid grid = CreateTerrainGrid();

            grid.GetChunkRect(62, 0, out float minX, out float minZ, out float maxX, out float maxZ);
            Assert.AreEqual(492f, minX, 1e-4f);   // -500 + 62*16
            Assert.AreEqual(500f, maxX, 1e-4f);   // 508 değil: terrain 500'de biter (8 m'lik kısmi chunk)
            Assert.AreEqual(-500f, minZ, 1e-4f);
            Assert.AreEqual(-484f, maxZ, 1e-4f);
        }

        [Test]
        public void IndexRoundTrip()
        {
            ChunkGrid grid = CreateTerrainGrid();
            int index = grid.ToIndex(5, 7);
            Assert.AreEqual(7 * 63 + 5, index);
            Assert.AreEqual(new int2(5, 7), grid.FromIndex(index));
            Assert.IsTrue(grid.Contains(62, 62));
            Assert.IsFalse(grid.Contains(63, 0));
            Assert.IsFalse(grid.Contains(-1, 0));
        }

        [Test]
        public void ChunkRange_PartiallyOutside_IsClamped_AndFullyOutside_ReturnsFalse()
        {
            ChunkGrid grid = CreateTerrainGrid();

            Assert.IsTrue(grid.TryGetChunkRange(-600f, -600f, -480f, -480f, out int2 lo, out int2 hi));
            Assert.AreEqual(new int2(0, 0), lo);
            Assert.AreEqual(new int2(1, 1), hi);

            Assert.IsFalse(grid.TryGetChunkRange(600f, 0f, 700f, 10f, out _, out _));
            Assert.IsFalse(grid.TryGetChunkRange(float.NaN, 0f, 10f, 10f, out _, out _));
        }

        [TestCase(0f, 100f, 16f)]
        [TestCase(-5f, 100f, 16f)]
        [TestCase(1000f, 0f, 16f)]
        [TestCase(1000f, 1000f, 0f)]
        [TestCase(1000f, 1000f, -1f)]
        [TestCase(float.NaN, 100f, 16f)]
        [TestCase(1000f, 1000f, float.NaN)]
        [TestCase(1000f, 1000f, 0.01f)] // 100000^2 chunk: üst sınırı aşar
        public void Create_InvalidInput_ReturnsFalseWithError(float sizeX, float sizeZ, float chunkSize)
        {
            Assert.IsFalse(ChunkGrid.TryCreate(0f, 0f, sizeX, sizeZ, chunkSize, out _, out string error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void Create_NonFiniteOrigin_ReturnsFalse()
        {
            Assert.IsFalse(ChunkGrid.TryCreate(float.NaN, 0f, 10f, 10f, 1f, out _, out string error));
            Assert.IsNotEmpty(error);
        }
    }
}
