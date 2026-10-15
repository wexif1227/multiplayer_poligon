using UnityEngine;

namespace Poligon.Weapons
{
    public static class ViewModelBuilder
    {
        static Material M(Color c, float metal = 0.5f, float gloss = 0.55f)
        {
            var sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            var m = new Material(sh);
            if (m.HasProperty("_Color")) m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", gloss);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", gloss);
            return m;
        }

        static Transform P(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(t);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        public static GameObject Build(WeaponId id, Transform parent)
        {
            var root = new GameObject("VM_" + id);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            var dark = M(new Color(0.1f, 0.11f, 0.13f), 0.6f, 0.4f);
            var steel = M(new Color(0.35f, 0.38f, 0.42f), 0.85f, 0.7f);
            var wood = M(new Color(0.4f, 0.25f, 0.12f), 0.05f, 0.3f);
            var pearl = M(new Color(0.9f, 0.92f, 0.95f), 0.75f, 0.8f);
            var black = M(new Color(0.05f, 0.05f, 0.06f), 0.4f, 0.35f);
            var emerald = M(new Color(0.05f, 0.9f, 0.4f), 0.9f, 0.85f);
            var gold = M(new Color(0.85f, 0.7f, 0.25f), 0.9f, 0.75f);

            switch (id)
            {
                case WeaponId.ak47:
                    // Viewmodel convention: +Z points forward from the camera,
                    // +Y is up. Keep the stock behind the receiver and the
                    // barrel/handguard in front so the rifle cannot appear reversed.
                    P(root.transform, PrimitiveType.Cube,
                        new Vector3(0.02f, -0.015f, 0.12f),
                        new Vector3(0.065f, 0.085f, 0.34f), dark);

                    // Rear wooden stock (towards the player: -Z)
                    P(root.transform, PrimitiveType.Cube,
                        new Vector3(0.02f, -0.02f, -0.20f),
                        new Vector3(0.055f, 0.075f, 0.24f), wood);

                    // Wooden handguard and front gas tube
                    P(root.transform, PrimitiveType.Cube,
                        new Vector3(0.02f, 0.005f, 0.34f),
                        new Vector3(0.06f, 0.065f, 0.22f), wood);

                    // Barrel points along +Z
                    P(root.transform, PrimitiveType.Cylinder,
                        new Vector3(0.02f, 0.015f, 0.57f),
                        new Vector3(0.018f, 0.16f, 0.018f), steel,
                        new Vector3(90f, 0f, 0f));

                    // Magazine hangs down (-Y)
                    P(root.transform, PrimitiveType.Cube,
                        new Vector3(0.02f, -0.105f, 0.10f),
                        new Vector3(0.045f, 0.14f, 0.09f), dark,
                        new Vector3(-8f, 0f, 0f));

                    // Front sight and rear sight
                    P(root.transform, PrimitiveType.Cube,
                        new Vector3(0.02f, 0.065f, 0.43f),
                        new Vector3(0.025f, 0.045f, 0.035f), steel);
                    P(root.transform, PrimitiveType.Cube,
                        new Vector3(0.02f, 0.055f, -0.005f),
                        new Vector3(0.025f, 0.035f, 0.04f), steel);
                    break;

                case WeaponId.m4a1s:
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.02f, -0.01f, 0.22f), new Vector3(0.055f, 0.075f, 0.45f), pearl);
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.02f, 0.045f, 0.15f), new Vector3(0.04f, 0.025f, 0.32f), black);
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.02f, -0.01f, -0.08f), new Vector3(0.05f, 0.07f, 0.18f), black);
                    P(root.transform, PrimitiveType.Cylinder, new Vector3(0.02f, 0.0f, 0.52f), new Vector3(0.024f, 0.18f, 0.024f), steel, new Vector3(90, 0, 0));
                    P(root.transform, PrimitiveType.Cylinder, new Vector3(0.02f, 0.0f, 0.68f), new Vector3(0.038f, 0.05f, 0.038f), black, new Vector3(90, 0, 0));
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.02f, -0.07f, 0.05f), new Vector3(0.035f, 0.1f, 0.05f), black);
                    // sticker accent
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.05f, 0.02f, 0.2f), new Vector3(0.005f, 0.03f, 0.04f), gold);
                    break;

                case WeaponId.awp:
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.02f, -0.02f, 0.2f), new Vector3(0.07f, 0.09f, 0.6f), pearl);
                    P(root.transform, PrimitiveType.Cylinder, new Vector3(0.02f, 0.0f, 0.6f), new Vector3(0.022f, 0.28f, 0.022f), steel, new Vector3(90, 0, 0));
                    P(root.transform, PrimitiveType.Cylinder, new Vector3(0.02f, 0.1f, 0.08f), new Vector3(0.055f, 0.1f, 0.055f), black, new Vector3(90, 0, 0));
                    P(root.transform, PrimitiveType.Cylinder, new Vector3(0.02f, 0.1f, 0.08f), new Vector3(0.03f, 0.12f, 0.03f), steel, new Vector3(90, 0, 0));
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.02f, -0.08f, -0.15f), new Vector3(0.065f, 0.12f, 0.2f), black);
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.02f, -0.06f, 0.15f), new Vector3(0.04f, 0.08f, 0.06f), black);
                    break;

                case WeaponId.glock:
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.05f, -0.02f, 0.1f), new Vector3(0.045f, 0.075f, 0.2f), steel);
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.05f, -0.08f, 0.04f), new Vector3(0.04f, 0.11f, 0.07f), black);
                    P(root.transform, PrimitiveType.Cylinder, new Vector3(0.05f, 0.01f, 0.22f), new Vector3(0.018f, 0.07f, 0.018f), steel, new Vector3(90, 0, 0));
                    break;

                case WeaponId.knife:
                    P(root.transform, PrimitiveType.Cylinder, new Vector3(0.06f, -0.04f, 0.02f), new Vector3(0.04f, 0.06f, 0.04f), black, new Vector3(0, 0, 90));
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.06f, -0.04f, 0.1f), new Vector3(0.025f, 0.05f, 0.1f), dark);
                    // curved blade approximation
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.08f, -0.02f, 0.22f), new Vector3(0.015f, 0.008f, 0.2f), emerald);
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.1f, 0.0f, 0.3f), new Vector3(0.012f, 0.006f, 0.12f), emerald, new Vector3(0, 25, 15));
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.05f, -0.04f, 0.08f), new Vector3(0.05f, 0.008f, 0.05f), gold);
                    break;

                default:
                    P(root.transform, PrimitiveType.Cube, new Vector3(0.02f, 0, 0.2f), new Vector3(0.06f, 0.07f, 0.4f), steel);
                    break;
            }
            return root;
        }
    }
}
