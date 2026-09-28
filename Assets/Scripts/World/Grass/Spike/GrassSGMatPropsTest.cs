using UnityEngine;
using UnityEngine.Rendering;

namespace AdanBye.Grass.Spike
{
    /// <summary>
    /// WP-3a SPIKE (kalıcı değil): "LOD başına ayrı buffer + RenderParams.matProps" tasarımını ölçer.
    /// İki ayrı GraphicsBuffer (A: kırmızı, B: mavi, yan yana iki bölge) ve iki ayrı RenderMeshIndirect çağrısı;
    /// her çağrı kendi MaterialPropertyBlock'unda `_GrassVisibleInstances`'ı bağlar (material.SetBuffer KULLANILMAZ).
    /// </summary>
    [ExecuteAlways]
    public sealed class GrassSGMatPropsTest : MonoBehaviour
    {
        public Material material;
        public int countPerGroup = 5000;
        public float groupSize = 14f;
        public float gap = 4f;

        sealed class Group
        {
            public GraphicsBuffer instances;
            public GraphicsBuffer args;
            public MaterialPropertyBlock props;
            public Bounds bounds;
        }

        readonly Group[] _groups = new Group[2];
        Mesh _mesh;
        IGrassCameraFilter _filter;

        /// <summary>Test için: materyal verilmeden önce çağrılır; sahneye bileşen eklendiğinde OnEnable Rebuild yapar.</summary>
        public void Rebuild()
        {
            Release();
            var terrain = Terrain.activeTerrain;
            if (terrain == null || material == null) { Debug.LogError("[GrassSGMatProps] terrain/material yok.", this); return; }

            _mesh = GrassSpikeBladeMesh.Create();
            Vector3 c = transform.position;
            // A solda (kırmızı), B sağda (mavi); x ekseni boyunca ayrık.
            _groups[0] = BuildGroup(terrain, c + Vector3.left * (gap * 0.5f + groupSize * 0.5f), new Color32(255, 0, 0, 255), 1u);
            _groups[1] = BuildGroup(terrain, c + Vector3.right * (gap * 0.5f + groupSize * 0.5f), new Color32(0, 0, 255, 255), 2u);
        }

        Group BuildGroup(Terrain terrain, Vector3 center, Color32 tint, uint seed)
        {
            var rng = new Unity.Mathematics.Random(seed * 7919u);
            var data = new GrassSpikeInstance[countPerGroup];
            float originY = terrain.GetPosition().y;
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < data.Length; i++)
            {
                float x = center.x + (rng.NextFloat() - 0.5f) * groupSize;
                float z = center.z + (rng.NextFloat() - 0.5f) * groupSize;
                float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + originY;
                var p = new Vector3(x, y, z);
                data[i] = GrassSpikeInstance.Create(p, rng.NextFloat(0f, 2f * Mathf.PI), rng.NextFloat(0.5f, 0.9f),
                                                    rng.NextFloat(0.06f, 0.10f), tint, rng.NextUInt());
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p + Vector3.up);
            }

            var g = new Group { props = new MaterialPropertyBlock() };
            g.instances = new GraphicsBuffer(GraphicsBuffer.Target.Structured, data.Length, GrassSpikeInstance.Stride);
            g.instances.SetData(data);
            g.args = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            g.args.SetData(new[]
            {
                new GraphicsBuffer.IndirectDrawIndexedArgs
                {
                    indexCountPerInstance = _mesh.GetIndexCount(0),
                    instanceCount = (uint)data.Length,
                    startIndex = _mesh.GetIndexStart(0),
                    baseVertexIndex = _mesh.GetBaseVertex(0),
                    startInstance = 0,
                }
            });
            g.props.SetBuffer("_GrassVisibleInstances", g.instances);
            g.bounds = new Bounds((min + max) * 0.5f, max - min + new Vector3(1f, 0f, 1f));
            return g;
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            Rebuild();
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Release();
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (_mesh == null) return;
            _filter ??= new DefaultGrassCameraFilter(gameObject.layer);
            if (!_filter.ShouldRender(cam)) return;

            foreach (var g in _groups)
            {
                if (g == null) continue;
                var rp = new RenderParams(material)
                {
                    camera = cam,
                    layer = gameObject.layer,
                    worldBounds = g.bounds,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = true,
                    matProps = g.props,
                };
                Graphics.RenderMeshIndirect(rp, _mesh, g.args);
            }
        }

        void Release()
        {
            for (int i = 0; i < _groups.Length; i++)
            {
                _groups[i]?.instances?.Release();
                _groups[i]?.args?.Release();
                _groups[i] = null;
            }
            if (_mesh != null)
            {
                if (Application.isPlaying) Destroy(_mesh); else DestroyImmediate(_mesh);
                _mesh = null;
            }
        }
    }
}
