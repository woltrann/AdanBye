using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>
    /// GrassInteractor bileşeni gerçek Registry.Default ile. Açık sahnede başka interactor olabileceğinden
    /// mutlak sayı yerine başlangıca göre fark ve içerik denetlenir; teardown oluşturulanları siler.
    /// </summary>
    public class GrassInteractorTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        int _baseline;

        [SetUp]
        public void SetUp()
        {
            GrassInteractorRegistry.Default.Prune();
            _baseline = GrassInteractorRegistry.Default.Count;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
                if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
            GrassInteractorRegistry.Default.Prune();
        }

        GrassInteractor Create()
        {
            var go = new GameObject("TestInteractor");
            _objects.Add(go);
            return go.AddComponent<GrassInteractor>();
        }

        [Test]
        public void AddComponent_RegistersInDefault()
        {
            var i = Create();

            Assert.AreEqual(_baseline + 1, GrassInteractorRegistry.Default.Count);
            Assert.Contains(i, new List<IGrassInteractor>(GrassInteractorRegistry.Default.Items));
        }

        [Test]
        public void DestroyImmediate_Unregisters()
        {
            var i = Create();

            Object.DestroyImmediate(i.gameObject);

            Assert.AreEqual(_baseline, GrassInteractorRegistry.Default.Count);
        }

        [Test]
        public void Disable_UnregistersAndIsNotAlive_EnableRegistersAgain()
        {
            var i = Create();

            i.enabled = false;
            Assert.IsFalse(i.IsAlive);
            Assert.AreEqual(_baseline, GrassInteractorRegistry.Default.Count);

            i.enabled = true;
            Assert.IsTrue(i.IsAlive);
            Assert.AreEqual(_baseline + 1, GrassInteractorRegistry.Default.Count);
        }

        [Test]
        public void Position_IncludesYOffset()
        {
            var i = Create();
            i.transform.position = new Vector3(1, 2, 3);
            var so = new UnityEditor.SerializedObject(i);
            so.FindProperty("yOffset").floatValue = -0.5f;
            so.ApplyModifiedProperties();

            Assert.AreEqual(new Vector3(1, 1.5f, 3), i.Position);
        }
    }
}
