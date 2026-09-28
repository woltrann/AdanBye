using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>GrassLodMeshSet: varsayılan mesh, sahiplik (kullanıcı mesh'i yok edilmez), hata ve sözleşme uyarıları.</summary>
    public class GrassLodMeshSetTests
    {
        readonly List<Object> _created = new List<Object>();

        Mesh UserMesh(Vector3[] vertices)
        {
            var m = new Mesh { name = "TMP_UserMesh" };
            m.vertices = vertices;
            if (vertices.Length >= 3) m.triangles = new[] { 0, 1, 2 };
            _created.Add(m);
            return m;
        }

        // Sözleşmeye uyan birim blade: pivot kökte, 1 birim boy.
        Mesh ValidUserMesh() => UserMesh(new[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0f, 1f, 0f) });

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) if (o != null) Object.DestroyImmediate(o);
            _created.Clear();
        }

        [Test]
        public void NullEntries_AreFilledWithOwnedDefaults()
        {
            Assert.IsTrue(GrassLodMeshSet.TryCreate(new Mesh[3], out GrassLodMeshSet set, out ValidationReport report), report.ToString());
            try
            {
                Assert.AreEqual(3, set.Count);
                for (int i = 0; i < 3; i++)
                {
                    Assert.IsNotNull(set[i]);
                    Assert.Greater(set[i].vertexCount, 0);
                    Assert.IsTrue(set.IsOwned(i));
                }
                Assert.AreEqual(0, report.Warnings.Count, "varsayılan mesh'ler sözleşmeye uymalı");
            }
            finally { set.Dispose(); }
        }

        [Test]
        public void UserMesh_IsNotOwned_AndSurvivesDispose_WhileOwnedDefaultsAreDestroyed()
        {
            Mesh user = ValidUserMesh();
            Assert.IsTrue(GrassLodMeshSet.TryCreate(new[] { user, null }, out GrassLodMeshSet set, out ValidationReport report), report.ToString());
            Mesh owned = set[1];
            Assert.AreSame(user, set[0]);
            Assert.IsFalse(set.IsOwned(0));
            Assert.IsTrue(set.IsOwned(1));

            set.Dispose();

            Assert.IsTrue(user != null, "kullanıcının mesh'i yok edilmemeli");
            Assert.IsTrue(owned == null, "kendi ürettiğimiz mesh yok edilmeli");
            Assert.IsNull(set[0]);
            Assert.DoesNotThrow(set.Dispose, "Dispose idempotent olmalı");
        }

        [Test]
        public void MeshWithoutVertices_IsError()
        {
            Mesh empty = UserMesh(new Vector3[0]);

            Assert.IsFalse(GrassLodMeshSet.TryCreate(new[] { null, empty }, out GrassLodMeshSet set, out ValidationReport report));
            Assert.IsNull(set);
            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Errors.Any(e => e.Contains("LOD1") && e.Contains("vertex")), report.ToString());
        }

        [Test]
        public void ContractViolations_AreWarnings_NotErrors()
        {
            // Pivot yerin çok altında + 10 birim boy: iki sözleşme uyarısı, yine de çizilebilir.
            Mesh bad = UserMesh(new[] { new Vector3(-0.5f, -3f, 0f), new Vector3(0.5f, -3f, 0f), new Vector3(0f, 7f, 0f) });

            Assert.IsTrue(GrassLodMeshSet.TryCreate(new[] { bad }, out GrassLodMeshSet set, out ValidationReport report), report.ToString());
            try
            {
                Assert.IsTrue(report.IsValid);
                Assert.IsTrue(report.Warnings.Any(w => w.Contains("pivot")), report.ToString());
                Assert.IsTrue(report.Warnings.Any(w => w.Contains("ölçek")), report.ToString());
                Assert.AreSame(bad, set[0]);
            }
            finally { set.Dispose(); }
        }

        [Test]
        public void InvalidCounts_AreErrors()
        {
            Assert.IsFalse(GrassLodMeshSet.TryCreate(null, out _, out ValidationReport nullReport));
            Assert.IsFalse(nullReport.IsValid);
            Assert.IsFalse(GrassLodMeshSet.TryCreate(new Mesh[0], out _, out ValidationReport emptyReport));
            Assert.IsFalse(emptyReport.IsValid);
            Assert.IsFalse(GrassLodMeshSet.TryCreate(new Mesh[LodDistanceTable.MaxLodCount + 1], out _, out ValidationReport tooMany));
            Assert.IsFalse(tooMany.IsValid);
        }

        [Test]
        public void FailureAfterDefaultsCreated_DoesNotLeakDefaultMeshes()
        {
            Mesh empty = UserMesh(new Vector3[0]);
            int before = Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name.StartsWith("GRASS_"));

            GrassLodMeshSet.TryCreate(new[] { null, null, empty }, out _, out _);

            int after = Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name.StartsWith("GRASS_"));
            Assert.AreEqual(before, after, "hata yolunda üretilmiş varsayılan mesh'ler yok edilmeli");
        }
    }
}
