using System.Collections.Generic;
using NUnit.Framework;

namespace AdanBye.Grass.Tests
{
    public class LodDistanceTableTests
    {
        // Plan varsayılanı: LOD0 0-25 keep 1, LOD1 25-60 keep 0.35 (x1.7 cap), LOD2 60-120 keep 0.1 (x3 cap).
        static List<LodLevelSpec> DefaultSpecs() => new List<LodLevelSpec>
        {
            new LodLevelSpec(25f, 1f, 1f, 5f),
            new LodLevelSpec(60f, 0.35f, 1.7f, 10f),
            new LodLevelSpec(120f, 0.1f, 3f, 20f),
        };

        static ValidationReport Validate(List<LodLevelSpec> specs)
        {
            bool ok = LodDistanceTable.TryCreate(specs, out LodDistanceTable table, out ValidationReport report);
            Assert.AreEqual(ok, report.IsValid);
            Assert.AreEqual(ok, table != null, "Tablo yalnızca geçerli veride üretilmeli");
            return report;
        }

        [Test]
        public void DefaultLods_ProduceExpectedShaderArrays()
        {
            Assert.IsTrue(LodDistanceTable.TryCreate(DefaultSpecs(), out LodDistanceTable t, out ValidationReport report), report.ToString());

            Assert.AreEqual(3, t.Count);
            CollectionAssert.AreEqual(new[] { 625f, 3600f, 14400f }, t.MaxDistanceSq);
            CollectionAssert.AreEqual(new[] { 1f, 0.35f, 0.1f }, t.KeepRatio);
            Assert.AreEqual(1f, t.WidthCompensation[0], 1e-5f);
            Assert.AreEqual(1.7f, t.WidthCompensation[1], 1e-5f);  // 1/0.35 = 2.86 -> cap 1.7
            Assert.AreEqual(3f, t.WidthCompensation[2], 1e-5f);    // 1/0.1 = 10 -> cap 3
            CollectionAssert.AreEqual(new[] { 5f, 10f, 20f }, t.TransitionBand);
            Assert.AreEqual(120f, t.DrawDistance);
            Assert.AreEqual(0, report.Warnings.Count);
        }

        [Test]
        public void WidthCompensation_UsesInverseKeepRatio_WhenBelowCap()
        {
            var specs = new List<LodLevelSpec> { new LodLevelSpec(50f, 0.5f, 4f, 0f) };
            Assert.IsTrue(LodDistanceTable.TryCreate(specs, out LodDistanceTable t, out _));
            Assert.AreEqual(2f, t.WidthCompensation[0], 1e-5f);
        }

        [Test]
        public void FindLod_MirrorsDistanceBoundaries()
        {
            Assert.IsTrue(LodDistanceTable.TryCreate(DefaultSpecs(), out LodDistanceTable t, out _));

            Assert.AreEqual(0, t.FindLod(0f));
            Assert.AreEqual(0, t.FindLod(24.9f));
            Assert.AreEqual(1, t.FindLod(25f));   // maxDistance dışlayıcı: sınırdaki nokta bir sonraki LOD'da
            Assert.AreEqual(1, t.FindLod(59.9f));
            Assert.AreEqual(2, t.FindLod(119.9f));
            Assert.AreEqual(-1, t.FindLod(120f)); // çizim mesafesi ötesi: çim yok
            Assert.AreEqual(-1, t.FindLod(-1f));
            Assert.AreEqual(-1, t.FindLod(float.NaN));
        }

        [Test]
        public void EmptyOrNullList_IsError()
        {
            Assert.IsFalse(Validate(new List<LodLevelSpec>()).IsValid);
            Assert.IsFalse(LodDistanceTable.TryCreate(null, out LodDistanceTable t, out ValidationReport report));
            Assert.IsNull(t);
            Assert.AreEqual(1, report.Errors.Count);
        }

        [Test]
        public void TooManyLods_IsError()
        {
            var specs = DefaultSpecs();
            specs.Add(new LodLevelSpec(200f, 0.05f, 4f, 0f));
            Assert.IsFalse(Validate(specs).IsValid);
        }

        [Test]
        public void NonIncreasingDistances_AreErrors()
        {
            var equal = DefaultSpecs();
            equal[1] = new LodLevelSpec(25f, 0.35f, 1.7f, 0f);
            Assert.IsFalse(Validate(equal).IsValid);

            var descending = DefaultSpecs();
            descending[2] = new LodLevelSpec(30f, 0.1f, 3f, 0f);
            Assert.IsFalse(Validate(descending).IsValid);
        }

        [TestCase(0f)]
        [TestCase(-0.5f)]
        [TestCase(1.5f)]
        [TestCase(float.NaN)]
        public void InvalidKeepRatio_IsError(float keep)
        {
            var specs = DefaultSpecs();
            specs[1] = new LodLevelSpec(60f, keep, 1.7f, 0f);
            Assert.IsFalse(Validate(specs).IsValid);
        }

        [TestCase(0f)]
        [TestCase(-10f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidMaxDistance_IsError(float distance)
        {
            var specs = new List<LodLevelSpec> { new LodLevelSpec(distance, 1f, 1f, 0f) };
            Assert.IsFalse(Validate(specs).IsValid);
        }

        [Test]
        public void WidthCompensationBelowOne_IsError()
        {
            var specs = new List<LodLevelSpec> { new LodLevelSpec(50f, 1f, 0.5f, 0f) };
            Assert.IsFalse(Validate(specs).IsValid);
        }

        [Test]
        public void TransitionBand_NegativeOrLargerThanOwnRange_IsError()
        {
            Assert.IsFalse(Validate(new List<LodLevelSpec> { new LodLevelSpec(25f, 1f, 1f, -1f) }).IsValid);
            var tooWide = DefaultSpecs();
            tooWide[0] = new LodLevelSpec(25f, 1f, 1f, 30f); // LOD0 aralığı 25 m
            Assert.IsFalse(Validate(tooWide).IsValid);
        }

        [Test]
        public void MultipleProblems_AreReportedTogether()
        {
            var specs = new List<LodLevelSpec>
            {
                new LodLevelSpec(-1f, 2f, 0.5f, -3f),
                new LodLevelSpec(50f, 0f, 1f, 0f),
            };
            ValidationReport report = Validate(specs);
            Assert.GreaterOrEqual(report.Errors.Count, 4);
        }

        [Test]
        public void IncreasingKeepRatio_IsError()
        {
            // Artan keepRatio, "uzak LOD'daki blade yakın LOD'da da vardır" (pop'suz alt küme) garantisini bozar;
            // bu yüzden uyarı değil hata (önceki sürümde yalnızca uyarıydı).
            var specs = DefaultSpecs();
            specs[2] = new LodLevelSpec(120f, 0.5f, 3f, 20f); // LOD2 keep > LOD1 keep
            Assert.IsFalse(LodDistanceTable.TryCreate(specs, out LodDistanceTable t, out ValidationReport report));
            Assert.IsNull(t);
            Assert.AreEqual(1, report.Errors.Count);
            Assert.AreEqual(0, report.Warnings.Count);
        }

        [Test]
        public void EqualKeepRatio_IsAllowed()
        {
            // Eşit oran hâlâ alt küme sağlar (aynı eşik); yalnızca ARTIŞ pop üretir.
            var specs = DefaultSpecs();
            specs[2] = new LodLevelSpec(120f, 0.35f, 3f, 20f);
            Assert.IsTrue(LodDistanceTable.TryCreate(specs, out _, out ValidationReport report), report.ToString());
        }
    }
}
