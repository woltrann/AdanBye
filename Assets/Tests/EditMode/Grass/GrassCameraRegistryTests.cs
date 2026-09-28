using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>GrassCameraRegistry: ekleme, budama, Dispose sahipliği ve idempotent DisposeAll (Unity render gerektirmez).</summary>
    public class GrassCameraRegistryTests
    {
        sealed class FakeContext : IDisposable
        {
            public int DisposeCount;
            public void Dispose() => DisposeCount++;
        }

        sealed class PlainContext { }

        readonly List<GameObject> _objects = new List<GameObject>();

        Camera NewCamera(string name)
        {
            var go = new GameObject("TMP_" + name);
            _objects.Add(go);
            return go.AddComponent<Camera>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [Test]
        public void GetOrCreate_CreatesOncePerCamera()
        {
            var registry = new GrassCameraRegistry<FakeContext>();
            Camera cam = NewCamera("A");
            int factoryCalls = 0;
            Func<Camera, FakeContext> factory = c => { factoryCalls++; return new FakeContext(); };

            FakeContext first = registry.GetOrCreate(cam, factory);
            FakeContext second = registry.GetOrCreate(cam, factory);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, factoryCalls);
            Assert.AreEqual(1, registry.Count);
            Assert.IsTrue(registry.TryGet(cam, out FakeContext found));
            Assert.AreSame(first, found);
        }

        [Test]
        public void GetOrCreate_FactoryReturnsNull_NothingRegistered()
        {
            var registry = new GrassCameraRegistry<FakeContext>();
            Camera cam = NewCamera("A");

            Assert.IsNull(registry.GetOrCreate(cam, _ => null));
            Assert.AreEqual(0, registry.Count);
            Assert.IsFalse(registry.TryGet(cam, out _));
        }

        [Test]
        public void NullCamera_IsRejectedWithoutCallingFactory()
        {
            var registry = new GrassCameraRegistry<FakeContext>();
            bool called = false;

            Assert.IsNull(registry.GetOrCreate(null, _ => { called = true; return new FakeContext(); }));
            Assert.IsFalse(called);
            Assert.IsFalse(registry.TryGet(null, out _));
        }

        [Test]
        public void PruneDestroyed_DisposesAndRemovesOnlyDeadCameras()
        {
            var registry = new GrassCameraRegistry<FakeContext>();
            Camera alive = NewCamera("Alive");
            Camera dying = NewCamera("Dying");
            FakeContext aliveCtx = registry.GetOrCreate(alive, _ => new FakeContext());
            FakeContext dyingCtx = registry.GetOrCreate(dying, _ => new FakeContext());

            UnityEngine.Object.DestroyImmediate(dying.gameObject);
            int removed = registry.PruneDestroyed();

            Assert.AreEqual(1, removed);
            Assert.AreEqual(1, registry.Count);
            Assert.AreEqual(1, dyingCtx.DisposeCount);
            Assert.AreEqual(0, aliveCtx.DisposeCount);
            Assert.AreEqual(0, registry.PruneDestroyed(), "ikinci budama yapacak iş bulmamalı");
            Assert.AreEqual(1, dyingCtx.DisposeCount, "aynı context iki kez Dispose edilmemeli");
        }

        [Test]
        public void DisposeAll_DisposesEveryContext_AndIsIdempotent()
        {
            var registry = new GrassCameraRegistry<FakeContext>();
            FakeContext a = registry.GetOrCreate(NewCamera("A"), _ => new FakeContext());
            FakeContext b = registry.GetOrCreate(NewCamera("B"), _ => new FakeContext());

            registry.DisposeAll();
            registry.DisposeAll();

            Assert.AreEqual(0, registry.Count);
            Assert.AreEqual(1, a.DisposeCount);
            Assert.AreEqual(1, b.DisposeCount);
        }

        [Test]
        public void NonDisposableContext_IsSupported()
        {
            var registry = new GrassCameraRegistry<PlainContext>();
            registry.GetOrCreate(NewCamera("A"), _ => new PlainContext());

            Assert.DoesNotThrow(registry.DisposeAll);
            Assert.AreEqual(0, registry.Count);
        }
    }
}
