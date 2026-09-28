using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>GrassSettings.Changed: Inspector değişikliği (OnValidate) çalışan renderer'a bildirilir.</summary>
    public class GrassSettingsChangedTests
    {
        GrassSettings _settings;

        [SetUp] public void SetUp() => _settings = ScriptableObject.CreateInstance<GrassSettings>();
        [TearDown] public void TearDown() { if (_settings != null) Object.DestroyImmediate(_settings); }

        [Test]
        public void SerializedEdit_RaisesChanged()
        {
            int calls = 0;
            _settings.Changed += () => calls++;

            var so = new SerializedObject(_settings);
            so.FindProperty("seed").intValue = 777;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.GreaterOrEqual(calls, 1);
        }

        [Test]
        public void Unsubscribed_HandlerIsNotCalled()
        {
            int calls = 0;
            System.Action handler = () => calls++;
            _settings.Changed += handler;
            _settings.Changed -= handler;

            var so = new SerializedObject(_settings);
            so.FindProperty("seed").intValue = 778;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(0, calls);
        }
    }
}
