using UnityEngine;

namespace Poligon.World
{
    public class ArenaBuilder : MonoBehaviour
    {
        public float size = 90f;

        void Start() => Build();

        Material Mat(Color c, float metal = 0f, float gloss = 0.3f)
        {
            var sh = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color");
            var m = new Material(sh);
            if (m.HasProperty("_Color")) m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", gloss);
            return m;
        }

        Texture2D Grid(int res, Color a, Color b, int cells)
        {
            var t = new Texture2D(res, res, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Repeat;
            int cs = res / cells;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                bool line = (x % cs < 2) || (y % cs < 2);
                t.SetPixel(x, y, line ? b : a);
            }
            t.Apply(true);
            return t;
        }

        void Build()
        {
            var floor = Mat(new Color(0.48f, 0.45f, 0.38f), 0f, 0.2f);
            floor.mainTexture = Grid(512, new Color(0.5f, 0.47f, 0.4f), new Color(0.35f, 0.33f, 0.28f), 16);
            floor.mainTextureScale = new Vector2(10, 10);

            var wall = Mat(new Color(0.58f, 0.55f, 0.48f), 0f, 0.25f);
            var dark = Mat(new Color(0.22f, 0.24f, 0.27f), 0.1f, 0.35f);
            var crate = Mat(new Color(0.5f, 0.35f, 0.18f), 0.05f, 0.3f);
            var metal = Mat(new Color(0.4f, 0.42f, 0.45f), 0.7f, 0.6f);
            var green = Mat(new Color(0.25f, 0.4f, 0.28f), 0f, 0.2f);

            Box("Floor", new Vector3(0, -0.25f, 0), new Vector3(size, 0.5f, size), floor);
            float h = 8f, t = 0.7f, half = size * 0.5f;
            Box("WallN", new Vector3(0, h * 0.5f, half), new Vector3(size, h, t), wall);
            Box("WallS", new Vector3(0, h * 0.5f, -half), new Vector3(size, h, t), wall);
            Box("WallE", new Vector3(half, h * 0.5f, 0), new Vector3(t, h, size), wall);
            Box("WallW", new Vector3(-half, h * 0.5f, 0), new Vector3(t, h, size), wall);

            // Structures
            Box("Bunker", new Vector3(-15, 1.5f, -10), new Vector3(8, 3, 6), dark);
            Box("Tower", new Vector3(18, 3f, 12), new Vector3(4, 6, 4), metal);
            Box("Platform", new Vector3(0, 0.5f, 20), new Vector3(12, 1, 6), green);

            for (int i = 0; i < 18; i++)
            {
                float x = Random.Range(-35f, 35f);
                float z = Random.Range(-35f, 35f);
                float s = Random.Range(1.2f, 2.6f);
                Box("Crate" + i, new Vector3(x, s * 0.5f, z), new Vector3(s, s, s * Random.Range(0.8f, 1.2f)), crate);
            }

            // Lights
            AddPoint(new Vector3(0, 6, 0), new Color(1f, 0.95f, 0.85f), 18f, 1.2f);
            AddPoint(new Vector3(-20, 5, -15), new Color(0.7f, 0.85f, 1f), 14f, 0.9f);
            AddPoint(new Vector3(20, 5, 15), new Color(1f, 0.8f, 0.6f), 14f, 0.9f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.58f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.4f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.16f, 0.14f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.55f, 0.6f, 0.68f);
            RenderSettings.fogDensity = 0.006f;
        }

        void Box(string n, Vector3 p, Vector3 s, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = n;
            go.transform.SetParent(transform);
            go.transform.position = p;
            go.transform.localScale = s;
            go.GetComponent<Renderer>().sharedMaterial = m;
            go.isStatic = true;
        }

        void AddPoint(Vector3 p, Color c, float range, float intensity)
        {
            var go = new GameObject("Lamp");
            go.transform.SetParent(transform);
            go.transform.position = p;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
        }
    }
}
