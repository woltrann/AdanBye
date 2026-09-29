using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>GrassDrawSystem'in saf yardımcıları ve girdi doğrulaması (GPU gerektirmeyen kısım).</summary>
    public class GrassDrawSystemTests
    {
        [Test]
        public void ComputeMaxChunks_ScalesWithRing_ButNeverExceedsGridOrDispatchLimit()
        {
            int typical = GrassDrawSystem.ComputeMaxChunks(120f, 16f, 100000);
            Assert.That(typical, Is.InRange(200, 400), "120 m / 16 m halkası ~230 chunk + pay");

            Assert.AreEqual(50, GrassDrawSystem.ComputeMaxChunks(120f, 16f, 50), "grid'ten fazla chunk olamaz");
            Assert.AreEqual(GrassGpuResources.MaxChunkCapacity, GrassDrawSystem.ComputeMaxChunks(100000f, 1f, int.MaxValue));
            Assert.AreEqual(1, GrassDrawSystem.ComputeMaxChunks(1f, 1000f, 1));
        }

        [Test]
        public void GrassGpuChunk_StrideMatchesManagedSize()
        {
            // Compute'taki GrassChunk ile bayt düzeni uyumsuzsa buffer kayar; Stride elle yazıldığı için burada kilitlenir.
            Assert.AreEqual(System.Runtime.InteropServices.Marshal.SizeOf(typeof(GrassGpuChunk)), GrassGpuChunk.Stride);
        }

        [Test]
        public void TryCreate_MissingDependencies_ReportsEveryProblem()
        {
            bool ok = GrassDrawSystem.TryCreate(null, null, null, null, 0, null, out GrassDrawSystem system, out ValidationReport report);

            Assert.IsFalse(ok);
            Assert.IsNull(system);
            Assert.IsTrue(report.ToString().Contains("GrassSettings"), report.ToString());
            Assert.IsTrue(report.ToString().Contains("materyal"), report.ToString());
            Assert.IsTrue(report.ToString().Contains("compute"), report.ToString());
            Assert.IsTrue(report.ToString().Contains("Terrain"), report.ToString());
        }
    }
}
