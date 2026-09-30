using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>GrassInteractionPacker: eleme, kırpma, en yakın Max seçimi, kararlı sıra, dizi sıfırlama.</summary>
    public class GrassInteractionPackerTests
    {
        const int Max = GrassInteractionContract.MaxInteractors;

        Vector4[] _pos;
        Vector4[] _prm;

        [SetUp]
        public void SetUp()
        {
            _pos = new Vector4[Max];
            _prm = new Vector4[Max];
        }

        static FakeInteractor At(float x, float strength = 1f, float radius = 1f, float vr = 2f) =>
            new FakeInteractor { Position = new Vector3(x, 0f, 0f), Strength = strength, Radius = radius, VerticalRange = vr };

        int Pack(List<IGrassInteractor> list, Vector3? focus, out int dropped) =>
            GrassInteractionPacker.Pack(list, focus, _pos, _prm, out dropped);

        [Test]
        public void Contract_Names_AreStable()
        {
            Assert.AreEqual(16, GrassInteractionContract.MaxInteractors);
            Assert.AreEqual("_GrassInteractorCount", GrassInteractionContract.CountName);
            Assert.AreEqual("_GrassInteractorPosRadius", GrassInteractionContract.PosRadiusName);
            Assert.AreEqual("_GrassInteractorParams", GrassInteractionContract.ParamsName);
        }

        [Test]
        public void Pack_WritesLayout()
        {
            var a = new FakeInteractor { Position = new Vector3(1, 2, 3), Radius = 4f, Strength = 0.5f, VerticalRange = 6f };
            int n = Pack(new List<IGrassInteractor> { a }, null, out int dropped);

            Assert.AreEqual(1, n);
            Assert.AreEqual(0, dropped);
            Assert.AreEqual(new Vector4(1, 2, 3, 4), _pos[0]);
            Assert.AreEqual(new Vector4(0.5f, 6f, 0f, 0f), _prm[0]);
        }

        [Test]
        public void Pack_Empty_And_NullSource_ZeroEverything()
        {
            for (int i = 0; i < Max; i++) { _pos[i] = Vector4.one; _prm[i] = Vector4.one; }

            Assert.AreEqual(0, Pack(new List<IGrassInteractor>(), null, out int d1));
            Assert.AreEqual(0, d1);
            AssertAllZero(0);

            for (int i = 0; i < Max; i++) { _pos[i] = Vector4.one; _prm[i] = Vector4.one; }
            Assert.AreEqual(0, GrassInteractionPacker.Pack(null, null, _pos, _prm, out int d2));
            Assert.AreEqual(0, d2);
            AssertAllZero(0);
        }

        [Test]
        public void Pack_ClearsUnusedSlots_FromPreviousFrame()
        {
            var list = new List<IGrassInteractor> { At(1), At(2), At(3) };
            Assert.AreEqual(3, Pack(list, null, out _));
            list.RemoveRange(1, 2);
            Assert.AreEqual(1, Pack(list, null, out _));
            Assert.AreEqual(1f, _pos[0].x);
            AssertAllZero(1);
        }

        [Test]
        public void Pack_DropsInvalid_AndCountsDropped()
        {
            var good = At(1);
            var list = new List<IGrassInteractor>
            {
                new FakeInteractor { Position = new Vector3(float.NaN, 0, 0) },
                new FakeInteractor { Position = new Vector3(0, float.PositiveInfinity, 0) },
                new FakeInteractor { Radius = float.NaN },
                new FakeInteractor { Radius = float.PositiveInfinity },
                new FakeInteractor { Radius = 0f },
                new FakeInteractor { Radius = -1f },
                new FakeInteractor { Strength = 0f },
                new FakeInteractor { Strength = -0.5f },
                new FakeInteractor { Strength = float.NaN },
                new FakeInteractor { VerticalRange = float.NaN },
                new FakeInteractor { IsAlive = false },
                null,
                good,
            };

            int n = Pack(list, null, out int dropped);

            Assert.AreEqual(1, n);
            Assert.AreEqual(list.Count - 1, dropped);
            Assert.AreEqual(1f, _pos[0].x);
            AssertAllZero(1);
        }

        [Test]
        public void Pack_ClampsStrengthTo01_AndNegativeVerticalRangeToZero()
        {
            var list = new List<IGrassInteractor> { At(0, strength: 5f), At(1, strength: 0.25f, vr: -3f) };
            Assert.AreEqual(2, Pack(list, null, out _));
            Assert.AreEqual(1f, _prm[0].x);
            Assert.AreEqual(0.25f, _prm[1].x);
            Assert.AreEqual(0f, _prm[1].y);
        }

        [Test]
        public void Pack_DropsDestroyedUnityObject()
        {
            var go = new GameObject("TMP_Interactor");
            try
            {
                var mb = go.AddComponent<BehaviourInteractor>();
                Object.DestroyImmediate(go);
                Assert.AreEqual(0, Pack(new List<IGrassInteractor> { mb }, null, out int dropped));
                Assert.AreEqual(1, dropped);
            }
            finally { if (go != null) Object.DestroyImmediate(go); }
        }

        [Test]
        public void Pack_ExactlyMax_KeepsAllInOrder()
        {
            var list = MakeLine(Max);
            Assert.AreEqual(Max, Pack(list, new Vector3(1000, 0, 0), out int dropped));
            Assert.AreEqual(0, dropped);
            for (int i = 0; i < Max; i++) Assert.AreEqual(i, _pos[i].x);
        }

        [Test]
        public void Pack_OverMax_PicksNearestToFocus_InRegistrationOrder()
        {
            // x = 0..19; focus x=10 => en yakın 16: 2..17 aralığı değil; mesafeye göre {2..17}? hesapla:
            // |x-10| sıralaması: 10,9,11,8,12,7,13,6,14,5,15,4,16,3,17,2 (16 adet) => x = 2..17.
            var list = MakeLine(20);
            int n = Pack(list, new Vector3(10, 0, 0), out int dropped);

            Assert.AreEqual(Max, n);
            Assert.AreEqual(4, dropped);
            // Çıktı kayıt sırasında: 2,3,...,17
            for (int i = 0; i < Max; i++) Assert.AreEqual(i + 2, _pos[i].x);
        }

        [Test]
        public void Pack_OverMax_TieBreaksByRegistrationOrder()
        {
            // 18 kayıt, hepsi focus'a aynı uzaklıkta: ilk 16'sı kalmalı.
            var list = new List<IGrassInteractor>();
            for (int i = 0; i < 18; i++)
                list.Add(new FakeInteractor { Position = new Vector3(5, 0, 0), Radius = 1f + i });

            Assert.AreEqual(Max, Pack(list, Vector3.zero, out int dropped));
            Assert.AreEqual(2, dropped);
            for (int i = 0; i < Max; i++) Assert.AreEqual(1f + i, _pos[i].w);
        }

        [Test]
        public void Pack_OverMax_SelectionIsStableAcrossCalls()
        {
            var list = MakeLine(30);
            Pack(list, new Vector3(3, 0, 0), out _);
            var first = (Vector4[])_pos.Clone();
            Pack(list, new Vector3(3, 0, 0), out _);
            CollectionAssert.AreEqual(first, _pos);
        }

        [Test]
        public void Pack_OverMax_InvalidDoNotCountAgainstBudget()
        {
            // 17 kayıt ama 2'si geçersiz => 15 geçerli, hepsi sığmalı (seçim devreye girmez).
            var list = MakeLine(17);
            ((FakeInteractor)list[0]).Radius = 0f;
            ((FakeInteractor)list[1]).Strength = float.NaN;

            Assert.AreEqual(15, Pack(list, new Vector3(100, 0, 0), out int dropped));
            Assert.AreEqual(2, dropped);
            Assert.AreEqual(2f, _pos[0].x);
        }

        [Test]
        public void Pack_FocusNull_TakesFirstMaxValid_InOrder()
        {
            var list = MakeLine(20);
            ((FakeInteractor)list[0]).Radius = 0f;   // geçersiz: atlanır, yerine 16. geçerli gelir

            Assert.AreEqual(Max, Pack(list, null, out int dropped));
            Assert.AreEqual(4, dropped);
            for (int i = 0; i < Max; i++) Assert.AreEqual(i + 1, _pos[i].x);
        }

        [Test]
        public void Pack_NonFiniteFocus_BehavesLikeNull()
        {
            var list = MakeLine(20);
            Assert.AreEqual(Max, Pack(list, new Vector3(float.NaN, 0, 0), out _));
            for (int i = 0; i < Max; i++) Assert.AreEqual(i, _pos[i].x);
        }

        [Test]
        public void Pack_LongerArrays_OnlyFirstMaxTouched_ShortArraysThrow()
        {
            var longPos = new Vector4[Max + 4];
            var longPrm = new Vector4[Max + 4];
            longPos[Max] = Vector4.one;
            GrassInteractionPacker.Pack(new List<IGrassInteractor> { At(1) }, null, longPos, longPrm, out _);
            Assert.AreEqual(Vector4.one, longPos[Max]);

            Assert.Throws<System.ArgumentException>(() =>
                GrassInteractionPacker.Pack(new List<IGrassInteractor>(), null, new Vector4[Max - 1], _prm, out _));
            Assert.Throws<System.ArgumentException>(() =>
                GrassInteractionPacker.Pack(new List<IGrassInteractor>(), null, _pos, null, out _));
        }

        static List<IGrassInteractor> MakeLine(int count)
        {
            var list = new List<IGrassInteractor>(count);
            for (int i = 0; i < count; i++) list.Add(At(i));
            return list;
        }

        void AssertAllZero(int fromSlot)
        {
            for (int i = fromSlot; i < Max; i++)
            {
                Assert.AreEqual(Vector4.zero, _pos[i], $"posRadius[{i}]");
                Assert.AreEqual(Vector4.zero, _prm[i], $"params[{i}]");
            }
        }
    }
}
