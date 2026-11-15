using System.Collections.Generic;
using UnityEngine;

namespace MissionForge
{
    /// <summary>
    /// 3D view. Builds the spacecraft from primitives. BLENDER HOOK: put a prefab at
    /// Assets/_Game/Resources/Models/{partId} (e.g. "bus_6u", "pwr_deploy", "com_hga", "ins_tel",
    /// or "planet_LEO" / "planet_Mars") and it replaces the placeholder automatically.
    /// </summary>
    public class SpacecraftView : MonoBehaviour
    {
        public enum Mode { Title, Design, Sim }

        Camera cam;
        Transform craft, planet, stage;
        LineRenderer orbitLine;
        Mode mode;
        float orbitRadius = 4.5f, orbitAngle;
        float launchT = -1, shakeT, shakeAmp;
        Vector3 camBase;
        TrailRenderer trail;
        const float LaunchTime = 2.5f;
        readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();

        static readonly Color Foil = new Color(0.85f, 0.65f, 0.25f);
        static readonly Color Cell = new Color(0.10f, 0.15f, 0.45f);
        static readonly Color Metal = new Color(0.75f, 0.77f, 0.80f);
        static readonly Color Dark = new Color(0.15f, 0.15f, 0.17f);

        void Awake()
        {
            cam = Camera.main;
            if (cam == null) { cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera"; }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.015f, 0.03f);
            stage = new GameObject("Stage").transform;
            craft = new GameObject("Craft").transform; craft.SetParent(stage);
            planet = new GameObject("Planet").transform; planet.SetParent(stage);
            orbitLine = new GameObject("OrbitLine").AddComponent<LineRenderer>();
            orbitLine.transform.SetParent(stage);
            orbitLine.loop = true; orbitLine.widthMultiplier = 0.03f; orbitLine.useWorldSpace = false;
            orbitLine.material = Mat(new Color(0.4f, 0.7f, 1f));
            trail = craft.gameObject.AddComponent<TrailRenderer>();
            trail.time = 1.2f; trail.widthMultiplier = 0.12f; trail.emitting = false;
            trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            trail.material = Mat(new Color(1f, 0.55f, 0.15f));
            trail.colorGradient = new Gradient { colorKeys = new[] { new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0), new GradientColorKey(new Color(1f, 0.3f, 0.1f), 1) },
                alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) } };
            Stars();
            SetMode(Mode.Title, null);
        }

        public void SetMode(Mode m, MissionDesign d)
        {
            mode = m;
            launchT = -1; trail.emitting = false; trail.Clear();
            if (d != null) Build(d);
            craft.gameObject.SetActive(m != Mode.Title);
            planet.gameObject.SetActive(m != Mode.Design);
            orbitLine.gameObject.SetActive(m == Mode.Sim);
            craft.localScale = Vector3.one * (m == Mode.Sim ? 0.35f : 1f);

            switch (m)
            {
                case Mode.Title:
                    BuildPlanet(OrbitId.LEO);
                    cam.rect = new Rect(0, 0, 1, 1);
                    cam.transform.position = new Vector3(-3.5f, 1.2f, -7); cam.transform.LookAt(new Vector3(-3.5f, 0, 0));
                    break;
                case Mode.Design:
                    cam.rect = new Rect(0.34f, 0.10f, 0.38f, 0.83f);
                    craft.localPosition = Vector3.zero;
                    cam.transform.position = new Vector3(0, 1.5f, -6); cam.transform.LookAt(Vector3.zero);
                    break;
                case Mode.Sim:
                    BuildPlanet(d.orbit.id);
                    orbitRadius = d.orbit.id == OrbitId.GEO ? 6.5f : 4.2f;
                    DrawOrbit();
                    cam.rect = new Rect(0, 0.10f, 0.62f, 0.83f);
                    cam.transform.position = new Vector3(0, 6, -11); cam.transform.LookAt(Vector3.zero);
                    break;
            }
            camBase = cam.transform.position;
        }

        /// <summary>Rocket climbs from the planet surface to orbit, leaving a flame trail.</summary>
        public void PlayLaunch()
        {
            launchT = 0; orbitAngle = -Mathf.PI / 2;
            trail.Clear(); trail.emitting = true;
        }

        public void Shake(float amp = 0.25f, float time = 0.45f) { shakeAmp = amp; shakeT = time; }

        void LateUpdate()
        {
            if (shakeT > 0)
            {
                shakeT -= Time.deltaTime;
                cam.transform.position = camBase + Random.insideUnitSphere * shakeAmp * Mathf.Clamp01(shakeT * 3);
                if (shakeT <= 0) cam.transform.position = camBase;
            }
        }

        void Update()
        {
            if (mode == Mode.Design) craft.Rotate(0, 25 * Time.deltaTime, 0, Space.World);
            if (planet.gameObject.activeSelf) planet.Rotate(0, 6 * Time.deltaTime, 0, Space.Self);
            if (mode == Mode.Sim)
            {
                float r = orbitRadius;
                if (launchT >= 0 && launchT < LaunchTime)
                {
                    launchT += Time.deltaTime;
                    float k = Mathf.SmoothStep(0, 1, launchT / LaunchTime);
                    r = Mathf.Lerp(1.5f, orbitRadius, k);
                    orbitAngle += Time.deltaTime * 0.8f * k; // starts straight up, then bends into orbit
                    if (launchT >= LaunchTime) trail.emitting = false;
                }
                else orbitAngle += Time.deltaTime * 0.8f;
                craft.localPosition = new Vector3(Mathf.Cos(orbitAngle), 0, Mathf.Sin(orbitAngle)) * r;
                craft.LookAt(Vector3.zero);
            }
        }

        // ---------------- building ----------------

        public void Build(MissionDesign d)
        {
            ClearChildren(craft);
            var bus = d.Get(Category.Bus);
            float s = bus.visualScale;

            // Blender models are authored for a unit bus (1 x 1.2 x 1), so bus/power scale with s
            if (!TryModel(bus.id, Vector3.zero, Vector3.one * s))
                Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(s, s * 1.2f, s), Foil);

            var pwr = d.Get(Category.Power);
            if (!TryModel(pwr.id, Vector3.zero, Vector3.one * s))
            {
                switch (pwr.id)
                {
                    case "pwr_body":
                        Prim(PrimitiveType.Cube, new Vector3(0, 0, -s * 0.51f), new Vector3(s * 0.9f, s * 1.1f, 0.02f), Cell);
                        Prim(PrimitiveType.Cube, new Vector3(0, 0, s * 0.51f), new Vector3(s * 0.9f, s * 1.1f, 0.02f), Cell);
                        break;
                    case "pwr_rtg":
                        Prim(PrimitiveType.Cylinder, new Vector3(0, -s * 0.6f - 0.5f, 0), new Vector3(0.05f, 0.5f, 0.05f), Metal);
                        Prim(PrimitiveType.Cylinder, new Vector3(0, -s * 0.6f - 1.1f, 0), new Vector3(0.3f, 0.3f, 0.3f), Dark);
                        break;
                    default:
                        float w = pwr.id == "pwr_large" ? 2.6f : 1.4f;
                        for (int side = -1; side <= 1; side += 2)
                            Prim(PrimitiveType.Cube, new Vector3(side * (s * 0.5f + w * 0.5f + 0.05f), 0, 0), new Vector3(w, 0.03f, s * 0.8f), Cell);
                        break;
                }
            }

            var com = d.Get(Category.Comms);
            Vector3 top = new Vector3(0, s * 0.6f, 0);
            if (!TryModel(com.id, top, Vector3.one))
            {
                switch (com.id)
                {
                    case "com_uhf": Prim(PrimitiveType.Cylinder, top + Vector3.up * 0.4f, new Vector3(0.02f, 0.4f, 0.02f), Metal); break;
                    case "com_s": Prim(PrimitiveType.Cube, top + Vector3.up * 0.02f, new Vector3(0.25f, 0.04f, 0.25f), Metal); break;
                    default:
                        float dish = com.id == "com_hga" ? 1.2f : 0.45f;
                        Prim(PrimitiveType.Cylinder, top + Vector3.up * 0.15f, new Vector3(0.04f, 0.15f, 0.04f), Metal);
                        Prim(PrimitiveType.Sphere, top + Vector3.up * 0.32f, new Vector3(dish, dish * 0.18f, dish), Metal);
                        break;
                }
            }

            // instruments on the front face (+Z points at the target)
            for (int i = 0; i < d.instruments.Count; i++)
            {
                var ins = d.instruments[i];
                float x = (i - (d.instruments.Count - 1) * 0.5f) * s * 0.35f;
                var pos = new Vector3(x, -s * 0.2f, s * 0.5f);
                if (TryModel(ins.id, pos, Vector3.one)) continue;
                float size = ins.id == "ins_tel" ? 0.6f : ins.id == "ins_radar" ? 0.3f : 0.15f;
                var t = Prim(PrimitiveType.Cylinder, pos + Vector3.forward * size, new Vector3(size, size, size), Dark);
                t.localRotation = Quaternion.Euler(90, 0, 0);
            }
        }

        void BuildPlanet(OrbitId o)
        {
            ClearChildren(planet);
            if (TryModel("planet_" + o, Vector3.zero, Vector3.one * 3, planet)) return;
            Color c = o == OrbitId.Lunar ? new Color(0.6f, 0.6f, 0.62f)
                    : o == OrbitId.Mars ? new Color(0.78f, 0.36f, 0.18f)
                    : new Color(0.15f, 0.40f, 0.85f);
            var p = Prim(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 3, c, planet);
            p.localPosition = Vector3.zero;
        }

        void DrawOrbit()
        {
            const int n = 96;
            orbitLine.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2;
                orbitLine.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * orbitRadius);
            }
        }

        void Stars()
        {
            var rng = new System.Random(7);
            var root = new GameObject("Stars").transform; root.SetParent(stage);
            for (int i = 0; i < 250; i++)
            {
                var dir = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f).normalized;
                var st = Prim(PrimitiveType.Quad, dir * 60, Vector3.one * (0.1f + (float)rng.NextDouble() * 0.15f), Color.white, root);
                st.LookAt(Vector3.zero);
                st.Rotate(0, 180, 0);
            }
        }

        // Detach before Destroy so a rebuild in the same frame never shows old parts
        static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                c.transform.SetParent(null, false);
                Destroy(c);
            }
        }

        bool TryModel(string id, Vector3 pos, Vector3 scale, Transform parent = null)
        {
            var prefab = Resources.Load<GameObject>("Models/" + id);
            if (prefab == null) return false;
            var go = Instantiate(prefab, parent ? parent : craft);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.Scale(go.transform.localScale, scale);
            return true;
        }

        Transform Prim(PrimitiveType type, Vector3 pos, Vector3 scale, Color c, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent ? parent : craft, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
            return go.transform;
        }

        Material Mat(Color c)
        {
            if (mats.TryGetValue(c, out var m)) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader);
            m.SetColor("_BaseColor", c);
            m.color = c;
            if (c == Color.white) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white); }
            mats[c] = m;
            return m;
        }
    }
}
