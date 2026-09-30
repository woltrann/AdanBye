using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>Paylaşılan sahte etkileşimciler (registry ve packer testleri kullanır).</summary>
    sealed class FakeInteractor : IGrassInteractor
    {
        public Vector3 Position { get; set; }
        public float Radius { get; set; } = 1f;
        public float Strength { get; set; } = 1f;
        public float VerticalRange { get; set; } = 2f;
        public bool IsAlive { get; set; } = true;
    }

    /// <summary>Gerçek Unity-null davranışını sınamak için MonoBehaviour tabanlı etkileşimci.</summary>
    sealed class BehaviourInteractor : MonoBehaviour, IGrassInteractor
    {
        public Vector3 Position => transform.position;
        public float Radius => 1f;
        public float Strength => 1f;
        public float VerticalRange => 1f;
        public bool IsAlive => true;
    }

    /// <summary>GrassInteractorRegistry: idempotent kayıt, budama, kararlı sıra, Unity-null.</summary>
    public class GrassInteractionRegistryTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
                if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [Test]
        public void Register_IsIdempotent()
        {
            var reg = new GrassInteractorRegistry();
            var a = new FakeInteractor();
            Assert.IsTrue(reg.Register(a));
            Assert.IsFalse(reg.Register(a));
            Assert.AreEqual(1, reg.Count);
        }

        [Test]
        public void Register_RejectsNullAndDead()
        {
            var reg = new GrassInteractorRegistry();
            Assert.IsFalse(reg.Register(null));
            Assert.IsFalse(reg.Register(new FakeInteractor { IsAlive = false }));
            Assert.AreEqual(0, reg.Count);
        }

        [Test]
        public void Unregister_IsIdempotentAndSafeForUnknown()
        {
            var reg = new GrassInteractorRegistry();
            var a = new FakeInteractor();
            reg.Register(a);
            Assert.IsTrue(reg.Unregister(a));
            Assert.IsFalse(reg.Unregister(a));
            Assert.IsFalse(reg.Unregister(null));
            Assert.AreEqual(0, reg.Count);
        }

        [Test]
        public void Order_IsRegistrationOrder_AndSurvivesUnregister()
        {
            var reg = new GrassInteractorRegistry();
            var a = new FakeInteractor();
            var b = new FakeInteractor();
            var c = new FakeInteractor();
            reg.Register(a); reg.Register(b); reg.Register(c);
            reg.Unregister(b);
            reg.Register(b);   // sona eklenir
            reg.Register(a);   // tekrar: sırayı değiştirmez
            CollectionAssert.AreEqual(new IGrassInteractor[] { a, c, b }, reg.Items);
        }

        [Test]
        public void Prune_RemovesDeadAndKeepsOrder()
        {
            var reg = new GrassInteractorRegistry();
            var a = new FakeInteractor();
            var b = new FakeInteractor();
            var c = new FakeInteractor();
            reg.Register(a); reg.Register(b); reg.Register(c);
            b.IsAlive = false;

            Assert.AreEqual(1, reg.Prune());
            CollectionAssert.AreEqual(new IGrassInteractor[] { a, c }, reg.Items);
            Assert.AreEqual(0, reg.Prune());
        }

        [Test]
        public void Prune_RemovesDestroyedUnityObjects()
        {
            var reg = new GrassInteractorRegistry();
            var go = new GameObject("TMP_Interactor");
            _objects.Add(go);
            var mb = go.AddComponent<BehaviourInteractor>();
            var plain = new FakeInteractor();
            reg.Register(mb);
            reg.Register(plain);

            Object.DestroyImmediate(go);
            // Gerçek Unity davranışı: C# referansı null değil ama Unity-null.
            Assert.IsFalse(ReferenceEquals(mb, null));
            Assert.IsTrue(mb == null);

            Assert.AreEqual(1, reg.Prune());
            CollectionAssert.AreEqual(new IGrassInteractor[] { plain }, reg.Items);
        }

        [Test]
        public void Unregister_WorksForDestroyedUnityObject()
        {
            var reg = new GrassInteractorRegistry();
            var go = new GameObject("TMP_Interactor");
            _objects.Add(go);
            var mb = go.AddComponent<BehaviourInteractor>();
            reg.Register(mb);
            Object.DestroyImmediate(go);

            Assert.IsTrue(reg.Unregister(mb));
            Assert.AreEqual(0, reg.Count);
        }

        [Test]
        public void IsUsable_HandlesDestroyedObject()
        {
            var go = new GameObject("TMP_Interactor");
            _objects.Add(go);
            var mb = go.AddComponent<BehaviourInteractor>();
            Assert.IsTrue(GrassInteractorRegistry.IsUsable(mb));
            Object.DestroyImmediate(go);
            Assert.IsFalse(GrassInteractorRegistry.IsUsable(mb));
        }

        [Test]
        public void Default_IsNotNull_AndIndependentOfNewInstances()
        {
            Assert.IsNotNull(GrassInteractorRegistry.Default);
            var other = new GrassInteractorRegistry();
            other.Register(new FakeInteractor());
            Assert.AreNotSame(GrassInteractorRegistry.Default, other);
            CollectionAssert.DoesNotContain((System.Collections.IEnumerable)GrassInteractorRegistry.Default.Items, other.Items[0]);
        }
    }
}
