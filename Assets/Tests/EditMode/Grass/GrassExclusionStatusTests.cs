using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    public class GrassExclusionStatusTests
    {
        static readonly Vector3 TOrigin = new Vector3(-500f, 0f, -500f);
        static readonly Vector3 TSize = new Vector3(1000f, 100f, 1000f);

        static ExclusionMaskState C(bool has, bool tex, Vector2 o, Vector2 s) =>
            GrassExclusionStatus.Classify(has, tex, o, s, TOrigin, TSize);

        [Test]
        public void NoMask_IsNone() =>
            Assert.AreEqual(ExclusionMaskState.None, GrassExclusionStatus.Classify(null, TOrigin, TSize));

        [Test]
        public void MaskWithoutTexture_IsNotBaked() =>
            Assert.AreEqual(ExclusionMaskState.NotBaked, C(true, false, new Vector2(-500f, -500f), new Vector2(1000f, 1000f)));

        [Test]
        public void MatchingGeometry_IsOk() =>
            Assert.AreEqual(ExclusionMaskState.Ok, C(true, true, new Vector2(-500f, -500f), new Vector2(1000f, 1000f)));

        [Test]
        public void WithinTolerance_IsOk() =>
            Assert.AreEqual(ExclusionMaskState.Ok, C(true, true, new Vector2(-500.005f, -500f), new Vector2(1000f, 1000.005f)));

        [Test]
        public void DifferentOrigin_IsMismatch() =>
            Assert.AreEqual(ExclusionMaskState.GeometryMismatch, C(true, true, new Vector2(0f, 0f), new Vector2(1000f, 1000f)));

        [Test]
        public void DifferentSize_IsMismatch() =>
            Assert.AreEqual(ExclusionMaskState.GeometryMismatch, C(true, true, new Vector2(-500f, -500f), new Vector2(500f, 1000f)));
    }
}
