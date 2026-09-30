using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>GrassInteractionPublisher: sahte sink ile count/dizi yazımı, sabit uzunluk, Clear, elenen sayısı.</summary>
    public class GrassInteractionPublisherTests
    {
        const int Max = GrassInteractionContract.MaxInteractors;

        sealed class FakeGlobals : IGrassShaderGlobals
        {
            public readonly Dictionary<string, float> Floats = new Dictionary<string, float>();
            public readonly Dictionary<string, Vector4[]> Arrays = new Dictionary<string, Vector4[]>();
            public int FloatWrites;
            public void SetFloat(string name, float value) { Floats[name] = value; FloatWrites++; }
            public void SetVectorArray(string name, Vector4[] values) => Arrays[name] = values;
        }

        GrassInteractorRegistry _registry;
        FakeGlobals _globals;
        GrassInteractionPublisher _publisher;

        [SetUp]
        public void SetUp()
        {
            _registry = new GrassInteractorRegistry();
            _globals = new FakeGlobals();
            _publisher = new GrassInteractionPublisher(_registry, _globals);
        }

        [Test]
        public void Publish_WritesCountAndArrays()
        {
            _registry.Register(new FakeInteractor { Position = new Vector3(1, 2, 3), Radius = 0.5f, Strength = 0.7f, VerticalRange = 2f });

            _publisher.Publish(null);

            Assert.AreEqual(1f, _globals.Floats[GrassInteractionContract.CountName]);
            var pr = _globals.Arrays[GrassInteractionContract.PosRadiusName];
            var pa = _globals.Arrays[GrassInteractionContract.ParamsName];
            Assert.AreEqual(new Vector4(1, 2, 3, 0.5f), pr[0]);
            Assert.AreEqual(new Vector4(0.7f, 2f, 0f, 0f), pa[0]);
            Assert.AreEqual(Vector4.zero, pr[1]);
            Assert.AreEqual(1, _publisher.ActiveCount);
            Assert.AreEqual(0, _publisher.DroppedCount);
        }

        [Test]
        public void Publish_ArraysAlwaysMaxLength_AndSameInstanceAcrossPublishes()
        {
            _publisher.Publish(null);
            var first = _globals.Arrays[GrassInteractionContract.PosRadiusName];
            Assert.AreEqual(Max, first.Length);
            Assert.AreEqual(Max, _globals.Arrays[GrassInteractionContract.ParamsName].Length);

            _registry.Register(new FakeInteractor());
            _publisher.Publish(null);
            var second = _globals.Arrays[GrassInteractionContract.PosRadiusName];

            Assert.AreSame(first, second);
            Assert.AreEqual(Max, second.Length);
        }

        [Test]
        public void Publish_PrunesDeadEntries()
        {
            var alive = new FakeInteractor();
            var dead = new FakeInteractor();
            _registry.Register(alive);
            _registry.Register(dead);
            dead.IsAlive = false;

            _publisher.Publish(null);

            Assert.AreEqual(1, _registry.Count);
            Assert.AreEqual(1f, _globals.Floats[GrassInteractionContract.CountName]);
        }

        [Test]
        public void Publish_OverMax_ReportsDropped()
        {
            for (int i = 0; i < Max + 3; i++)
                _registry.Register(new FakeInteractor { Position = new Vector3(i, 0, 0) });

            _publisher.Publish(Vector3.zero);

            Assert.AreEqual(Max, _publisher.ActiveCount);
            Assert.AreEqual(3, _publisher.DroppedCount);
            Assert.AreEqual((float)Max, _globals.Floats[GrassInteractionContract.CountName]);
        }

        [Test]
        public void Publish_InvalidEntry_CountedAsDropped()
        {
            _registry.Register(new FakeInteractor());
            _registry.Register(new FakeInteractor { Radius = 0f });

            _publisher.Publish(null);

            Assert.AreEqual(1, _publisher.ActiveCount);
            Assert.AreEqual(1, _publisher.DroppedCount);
        }

        [Test]
        public void Clear_WritesZeroCountAndZeroedArrays()
        {
            _registry.Register(new FakeInteractor { Position = Vector3.one });
            _publisher.Publish(null);

            _publisher.Clear();

            Assert.AreEqual(0f, _globals.Floats[GrassInteractionContract.CountName]);
            foreach (var v in _globals.Arrays[GrassInteractionContract.PosRadiusName]) Assert.AreEqual(Vector4.zero, v);
            foreach (var v in _globals.Arrays[GrassInteractionContract.ParamsName]) Assert.AreEqual(Vector4.zero, v);
            Assert.AreEqual(Max, _globals.Arrays[GrassInteractionContract.PosRadiusName].Length);
            Assert.AreEqual(0, _publisher.ActiveCount);
            Assert.AreEqual(0, _publisher.DroppedCount);
        }

        [Test]
        public void Constructor_NullDependencies_Throw()
        {
            Assert.Throws<System.ArgumentNullException>(() => new GrassInteractionPublisher(null, _globals));
            Assert.Throws<System.ArgumentNullException>(() => new GrassInteractionPublisher(_registry, null));
        }
    }
}
