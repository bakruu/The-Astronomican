using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static MissionForge.UIKit;

namespace MissionForge
{
    /// <summary>All screens. Each Show* clears the canvas and rebuilds it (simple and robust for a hackathon).</summary>
    public class GameUI : MonoBehaviour
    {
        GameManager gm => GameManager.I;
        RectTransform root;
        Role tab = Role.Science;
        Vector2 leftScroll = Vector2.up; // keep scroll position between rebuilds (up = top)
        ScrollRect leftScrollRect;

        void Awake()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)canvasGo.transform;
        }

        void ClearScreen()
        {
            StopAllCoroutines();
            Clear(root);
        }

        // ============================ TITLE ============================
        public void ShowTitle()
        {
            ClearScreen();
            gm.view.SetMode(SpacecraftView.Mode.Title, null);
            var box = Place(VBox(root, 18), 0.06f, 0.15f, 0.45f, 0.85f);
            box.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            Size(Label(box, GameManager.GameTitle, 72, TextAnchor.MiddleLeft, Accent, FontStyle.Bold), 100);
            Size(Label(box, "Design it. Launch it. Learn why it worked (or didn't).", 26, TextAnchor.MiddleLeft, Muted), 50);
            Size(Label(box, "Play alone or as a mission team of up to 4: Launch, Spacecraft, Ground & Comms, Science.", 20, TextAnchor.MiddleLeft, Muted), 60);
            Btn(box, "SOLO MISSION", () => { gm.players = 1; ShowScenarios(); }, Accent, 24, 64);
            var row = HBox(box, 12, 64);
            for (int n = 2; n <= 4; n++)
            {
                int p = n;
                Btn(row, $"TEAM OF {n}", () => { gm.players = p; ShowScenarios(); }, Card, 22, 64);
            }
        }

        // ============================ SCENARIOS (Office Hours / CML 1) ============================
        public void ShowScenarios()
        {
            ClearScreen();
            gm.view.SetMode(SpacecraftView.Mode.Title, null);
            Place(Panel(root, Bg).rectTransform, 0, 0, 1, 1);
            TopBar("PHASE 1 · OFFICE HOURS  -  Choose your mission objective", ShowTitle);
            var grid = Place(Node("Grid", root), 0.05f, 0.08f, 0.95f, 0.88f);
            var h = grid.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = true;

            foreach (var s in GameDatabase.Scenarios)
            {
                var card = Panel(grid, Card, "ScenarioCard");
                var v = Place(VBox(card.transform, 14, 24), 0, 0, 1, 1);
                Size(Label(v, s.title, 34, TextAnchor.UpperLeft, TextCol, FontStyle.Bold), 50);
                Size(Label(v, $"<color={Hex(Accent)}>{s.difficulty}</color>   Budget ${s.budget:0}M   ·   {s.durationDays} days", 20), 30);
                Size(Label(v, s.brief, 21, TextAnchor.UpperLeft, Muted), 150);
                string req = s.required.Length == 0 ? "anything" : string.Join(" or ", s.required);
                string orb = s.allowedOrbits == null ? "any" : string.Join(", ", s.allowedOrbits);
                Size(Label(v, $"Needs: <b>{req}</b> instrument\nOrbit: <b>{orb}</b>\nScience goal: <b>{s.targetScience:0}</b> pts", 19), 90);
                var spacer = Node("Spacer", v); spacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
                var sc = s;
                Btn(v, "SELECT MISSION", () => { tab = Role.Science; leftScroll = Vector2.up; leftScrollRect = null; gm.StartScenario(sc); }, Accent, 22, 60);
            }
        }

        // ============================ DESIGN (Concept -> Trade Studies -> Point Design) ============================
        public void ShowDesign()
        {
            if (leftScrollRect) leftScroll = leftScrollRect.normalizedPosition;
            ClearScreen();
            var d = gm.design;
            gm.view.SetMode(SpacecraftView.Mode.Design, d);

            TopBar($"PHASE 2 · TRADE STUDIES  -  {d.scenario.title}   (attempt {gm.attempts + 1})", ShowScenarios);

            // ---------- left: role tabs + options ----------
            var left = Place(Panel(root, Bg, "Left").rectTransform, 0, 0.10f, 0.34f, 0.93f);
            var tabs = Place(Node("Tabs", left), 0, 1, 1, 1);
            tabs.pivot = new Vector2(0.5f, 1); tabs.sizeDelta = new Vector2(0, 64);
            var th = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.spacing = 4; th.padding = new RectOffset(8, 8, 8, 4);
            th.childControlWidth = th.childControlHeight = true; th.childForceExpandWidth = th.childForceExpandHeight = true;
            foreach (Role r in System.Enum.GetValues(typeof(Role)))
            {
                var rr = r;
                int bad = d.checks.Count(c => c.owner == r && !c.ok);
                string who = gm.players > 1 ? $"\n<size=14>P{gm.PlayerFor(r)}</size>" : "";
                string flag = bad > 0 ? $" <color={Hex(Bad)}>({bad})</color>" : "";
                Btn(tabs, $"{GameDatabase.RoleName(r)}{flag}{who}", () => { tab = rr; leftScroll = Vector2.up; ShowDesign(); },
                    r == tab ? CardSel : Card, 16, -1);
            }
            var listArea = Place(Node("List", left), 0, 0, 1, 1);
            listArea.offsetMax = new Vector2(0, -64);
            var list = ScrollList(listArea, 8, 12);
            leftScrollRect = listArea.GetComponent<ScrollRect>();

            if (gm.players > 1)
                Size(Label(list, $"<color={Hex(Accent)}>Player {gm.PlayerFor(tab)}</color> owns this role. Discuss trade-offs with your team!", 18), 30);

            switch (tab)
            {
                case Role.Science:
                    Size(Label(list, $"<b>Objective:</b> {d.scenario.brief}", 19, TextAnchor.UpperLeft), 70);
                    CategoryList(list, d, MissionForge.Category.Instrument);
                    break;
                case Role.Launch:
                    Header(list, "ORBIT", "Where your spacecraft flies decides what it sees, how much sun it gets and how hard it is to talk to.");
                    foreach (var o in GameDatabase.Orbits)
                    {
                        var oo = o;
                        string allowed = d.scenario.allowedOrbits == null || d.scenario.allowedOrbits.Contains(o.id) ? "" : $" <color={Hex(Bad)}>(can't see target)</color>";
                        Option(list, $"<b>{o.name}</b>{allowed}\n<size=15><color={Hex(Muted)}>{o.desc}  Shadow {o.eclipseMin:0} min/orbit, sun x{o.solarFactor:0.##}</color></size>",
                            d.orbit == o, () => { d.SelectOrbit(oo); Changed(); });
                    }
                    Header(list, "LAUNCH VEHICLE", "Rockets differ in how much mass they can lift to each orbit, their price and reliability.");
                    foreach (var l in GameDatabase.Launchers)
                    {
                        var ll = l;
                        float cap = l.CapacityTo(d.orbit.id);
                        string capTxt = cap <= 0 ? $"<color={Hex(Bad)}>cannot reach this orbit</color>" : $"{cap:0} kg to this orbit";
                        Option(list, $"<b>{l.name}</b>   <color={Hex(Muted)}>${l.cost:0.#}M · {capTxt} · {l.reliability:P0} reliable</color>\n<size=15><color={Hex(Muted)}>{l.desc}</color></size>",
                            d.launcher == l, () => { d.SelectLauncher(ll); Changed(); });
                    }
                    break;
                default:
                    foreach (var c in GameDatabase.RoleCategories[tab]) CategoryList(list, d, c);
                    break;
            }
            Canvas.ForceUpdateCanvases();
            leftScrollRect.normalizedPosition = leftScroll;

            // ---------- right: dashboard ----------
            var right = Place(Panel(root, Bg, "Right").rectTransform, 0.72f, 0.10f, 1f, 0.93f);
            var dash = ScrollList(right, 10, 16);
            // Flight director hint: the most important open problem, one click to the role that owns it
            var next = d.checks.Where(c => !c.ok).OrderBy(c => c.critical ? 0 : 1).FirstOrDefault();
            if (next != null)
            {
                Color nc = next.critical ? Bad : Warn;
                Label(dash, $"<color={Hex(nc)}><b>FLIGHT DIRECTOR · NEXT STEP</b></color>\n<b>{next.title}</b>\n<size=16>{next.why}</size>", 17, TextAnchor.UpperLeft);
                var owner = next.owner;
                if (owner != tab)
                    Btn(dash, $"Open {GameDatabase.RoleName(owner)} >", () => { tab = owner; leftScroll = Vector2.up; ShowDesign(); }, Card, 17, 40);
            }
            else Label(dash, $"<color={Hex(Good)}><b>FLIGHT DIRECTOR</b></color>\n<size=16>All checks green. Launch, or try a cheaper design for a higher score.</size>", 17, TextAnchor.UpperLeft);
            Size(Label(dash, "MISSION DASHBOARD", 22, TextAnchor.MiddleLeft, Accent, FontStyle.Bold), 36);
            Bar(dash, "Mass vs rocket", d.mass, d.launchCapacity, $"{d.mass:0.#} / {d.launchCapacity:0} kg", d.launchCapacity > 0 && d.mass <= d.launchCapacity);
            Bar(dash, "Mass vs structure", d.mass, d.busMaxMass, $"{d.mass:0.#} / {d.busMaxMass:0} kg", d.mass <= d.busMaxMass);
            Bar(dash, "Power used / made", d.powerUse, d.powerGen, $"{d.powerUse:0} / {d.powerGen:0} W", d.powerGen >= d.powerUse);
            Bar(dash, "Eclipse energy need / battery", d.batteryNeed, d.batteryWh, $"{d.batteryNeed:0} / {d.batteryWh:0} Wh", d.batteryWh >= d.batteryNeed);
            Bar(dash, "Data made / downlink", d.dataGen, d.downlink, $"{d.dataGen:0.##} / {d.downlink:0.##} GB/day", d.dataGen <= d.downlink || d.dataGen == 0);
            Bar(dash, "Cost / budget", d.cost, d.scenario.budget, $"${d.cost:0.#}M / ${d.scenario.budget:0}M", d.cost <= d.scenario.budget);
            Size(Label(dash, $"Pointing {d.pointing}/{d.pointingNeed}   ·   Slots {d.slotsUsed}/{d.slots}   ·   Reliability {d.reliability:P0}", 18, TextAnchor.MiddleLeft, Muted), 28);

            Size(Label(dash, "DESIGN REVIEW", 22, TextAnchor.MiddleLeft, Accent, FontStyle.Bold), 40);
            foreach (var c in d.checks.OrderBy(c => c.ok))
            {
                Color col = c.ok ? Good : c.critical ? Bad : Warn;
                string mark = c.ok ? "OK" : c.critical ? "FAIL" : "WARN";
                Label(dash, $"<color={Hex(col)}><b>[{mark}]</b></color> <b>{c.title}</b> <color={Hex(Muted)}>({GameDatabase.RoleName(c.owner)})</color>\n<size=16><color={Hex(Muted)}>{c.why}</color></size>", 17, TextAnchor.UpperLeft);
            }

            // ---------- bottom: Go/No-Go + launch ----------
            var bottom = Place(Panel(root, Bg, "Bottom").rectTransform, 0, 0, 1, 0.10f);
            var bh = Place(Node("Row", bottom), 0.01f, 0.12f, 0.99f, 0.88f);
            var hl = bh.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandHeight = true; hl.childForceExpandWidth = true;

            if (gm.players > 1)
            {
                Size(Label(bh, "PHASE 3 · GO / NO-GO POLL\n<size=15>Every role must say GO. Any change resets the poll.</size>", 18), -1, 300);
                foreach (Role r in System.Enum.GetValues(typeof(Role)))
                {
                    var rr = r;
                    bool isGo = gm.go[r];
                    Btn(bh, $"{GameDatabase.RoleName(r)} (P{gm.PlayerFor(r)})\n<b>{(isGo ? "GO" : "NO-GO")}</b>", () => { gm.go[rr] = !gm.go[rr]; ShowDesign(); },
                        isGo ? new Color(0.15f, 0.45f, 0.25f) : new Color(0.40f, 0.15f, 0.15f), 17, -1);
                }
            }
            else
            {
                int crit = d.checks.Count(c => !c.ok && c.critical);
                string hint = crit > 0 ? $"<color={Hex(Bad)}>{crit} critical problem(s).</color> You can still launch and learn from the failure."
                                       : $"<color={Hex(Good)}>No critical problems.</color> Ready for launch!";
                Size(Label(bh, hint, 20), -1, -1, 3);
            }
            var launch = Btn(bh, "LAUNCH", gm.Launch, Accent, 28, -1);
            Size(launch, -1, 260);
            launch.interactable = gm.AllGo;

            if (tutorialStep >= 0) Tutorial(d);
        }

        // ============================ FIRST-PLAY TUTORIAL ============================
        static int tutorialStep; // -1 = finished/skipped (once per session)

        void Tutorial(MissionDesign d)
        {
            // (highlight xMin, yMin, xMax, yMax, card xMin, yMin, xMax, yMax, title, body)
            var steps = new (Rect hi, Rect card, string title, string body)[]
            {
                (new Rect(0, 0, 0, 0), new Rect(0.3f, 0.3f, 0.4f, 0.4f), "WELCOME, FLIGHT DIRECTOR",
                    $"Your mission: <b>{d.scenario.title}</b>.\n{d.scenario.brief}\n\nYou have <b>${d.scenario.budget:0}M</b>. Design a spacecraft, launch it, and find out why it worked (or didn't)."),
                (new Rect(0, 0.10f, 0.34f, 0.83f), new Rect(0.37f, 0.45f, 0.32f, 0.3f), "1 · CHOOSE PARTS",
                    "Each tab is a team role, just like a real NASA mission team. Click a part to select it. Every choice changes mass, power, data and cost."),
                (new Rect(0.34f, 0.10f, 0.38f, 0.83f), new Rect(0.04f, 0.35f, 0.28f, 0.3f), "2 · YOUR SPACECRAFT",
                    "Your spacecraft is assembled live as you pick parts: bus, solar panels, antenna and instruments."),
                (new Rect(0.72f, 0.10f, 0.28f, 0.83f), new Rect(0.37f, 0.45f, 0.32f, 0.3f), "3 · DASHBOARD",
                    "Green bars are fine, red bars are problems. The <b>Flight Director</b> at the top always tells you the most important thing to fix next."),
                (new Rect(0, 0, 1, 0.10f), new Rect(0.34f, 0.25f, 0.32f, 0.3f), "4 · LAUNCH",
                    "When the review is green, press <b>LAUNCH</b>. In team mode every role must vote GO first. Then watch your mission fly and read the report to learn what to improve."),
            };
            var s = steps[tutorialStep];
            var layer = Node("Tutorial", root); Place(layer, 0, 0, 1, 1);
            var dim = new Color(0, 0, 0, 0.85f);
            if (s.hi.width <= 0) Place(Panel(layer, dim).rectTransform, 0, 0, 1, 1);
            else
            {
                // four panels around the highlighted area = a "spotlight" cut-out
                Place(Panel(layer, dim).rectTransform, 0, 0, 1, s.hi.yMin);
                Place(Panel(layer, dim).rectTransform, 0, s.hi.yMax, 1, 1);
                Place(Panel(layer, dim).rectTransform, 0, s.hi.yMin, s.hi.xMin, s.hi.yMax);
                Place(Panel(layer, dim).rectTransform, s.hi.xMax, s.hi.yMin, 1, s.hi.yMax);
                // thin accent border around the spotlight
                var frame = Place(Node("Frame", layer), s.hi.xMin, s.hi.yMin, s.hi.xMax, s.hi.yMax);
                foreach (var (a, b) in new[] { (new Vector2(0, 0), new Vector2(1, 0)), (new Vector2(0, 1), new Vector2(1, 1)), (new Vector2(0, 0), new Vector2(0, 1)), (new Vector2(1, 0), new Vector2(1, 1)) })
                {
                    var line = Panel(frame, Accent, "Border"); line.raycastTarget = false;
                    var rt = line.rectTransform; rt.anchorMin = a; rt.anchorMax = b;
                    rt.sizeDelta = new Vector2(a.x == b.x ? 4 : 0, a.y == b.y ? 4 : 0); rt.anchoredPosition = Vector2.zero;
                }
            }
            var card = Panel(layer, Card, "TutorialCard");
            Place(card.rectTransform, s.card.xMin, s.card.yMin, s.card.xMax, s.card.yMax);
            var v = Place(VBox(card.transform, 12, 28), 0, 0, 1, 1);
            Size(Label(v, s.title, 28, TextAnchor.MiddleLeft, Accent, FontStyle.Bold), 40);
            Label(v, s.body, 20, TextAnchor.UpperLeft);
            var spacer = Node("Spacer", v); spacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var row = HBox(v, 12, 52);
            Btn(row, "SKIP", () => { tutorialStep = -1; ShowDesign(); }, Card, 20, 52);
            bool last = tutorialStep == steps.Length - 1;
            Btn(row, last ? "LET'S GO" : $"NEXT  ({tutorialStep + 1}/{steps.Length})", () => { tutorialStep = last ? -1 : tutorialStep + 1; ShowDesign(); }, Accent, 20, 52);
        }

        void Changed()
        {
            gm.OnDesignChanged();
            ShowDesign();
        }

        void Header(Transform list, string title, string explain)
        {
            Size(Label(list, title, 20, TextAnchor.LowerLeft, Accent, FontStyle.Bold), 36);
            Label(list, explain, 16, TextAnchor.UpperLeft, Muted);
        }

        void CategoryList(Transform list, MissionDesign d, Category c)
        {
            Header(list, c.ToString().ToUpper() + (c == MissionForge.Category.Instrument ? $"  ({d.slotsUsed}/{d.slots} slots)" : ""), GameDatabase.Explain(c));
            foreach (var p in GameDatabase.OfCategory(c))
            {
                var pp = p;
                Option(list, $"<b>{p.name}</b>   <color={Hex(Muted)}>{Stats(p)}</color>\n<size=15><color={Hex(Muted)}>{p.desc}</color></size>",
                    d.IsSelected(p), () => { d.Select(pp); Changed(); });
            }
        }

        static string Stats(Part p)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (p.mass > 0) parts.Add($"{p.mass:0.#} kg");
            if (p.power > 0) parts.Add($"{p.power:0} W");
            if (p.powerGen > 0) parts.Add($"+{p.powerGen:0} W");
            if (p.batteryWh > 0) parts.Add($"{p.batteryWh:0} Wh");
            if (p.rateMbps > 0) parts.Add($"{p.rateMbps:0.##} Mbps");
            if (p.slots > 0) parts.Add($"{p.slots} slots, max {p.maxMass:0} kg");
            if (p.pointing > 0) parts.Add($"pointing {p.pointing}");
            if (p.cat == MissionForge.Category.Instrument) parts.Add($"{p.dataGBday:0.##} GB/day · {p.science:0} sci · needs pointing {p.pointingReq}");
            if (p.costPerYear > 0) parts.Add($"${p.costPerYear:0.#}M/yr");
            parts.Add($"${p.cost:0.##}M");
            return string.Join(" · ", parts);
        }

        void Option(Transform list, string text, bool selected, System.Action onClick)
        {
            var b = Btn(list, text, onClick, selected ? CardSel : Card, 18, 76, TextAnchor.MiddleLeft);
            if (selected)
            {
                var edge = Panel(b.transform, Accent, "Edge");
                Place(edge.rectTransform, 0, 0, 0, 1).sizeDelta = new Vector2(6, 0);
                edge.rectTransform.pivot = new Vector2(0, 0.5f);
            }
        }

        void TopBar(string title, System.Action back)
        {
            var top = Place(Panel(root, Bg, "Top").rectTransform, 0, 0.93f, 1, 1);
            var lbl = Place(Label(top, title, 24, TextAnchor.MiddleLeft, TextCol, FontStyle.Bold).rectTransform, 0, 0, 1, 1);
            lbl.offsetMin = new Vector2(150, 0);
            if (back != null)
            {
                var b = Btn(top, "< BACK", back, Card, 18, -1);
                Place((RectTransform)b.transform, 0.005f, 0.15f, 0.07f, 0.85f);
            }
        }

        // ============================ SIMULATION (Launch & Operations) ============================
        public void ShowSimulation()
        {
            ClearScreen();
            var d = gm.design;
            var r = gm.result;
            gm.view.SetMode(SpacecraftView.Mode.Sim, d);
            TopBar($"PHASE 4 · OPERATIONS  -  {d.scenario.title} at {d.orbit.name}", null);

            var right = Place(Panel(root, Bg, "Log").rectTransform, 0.62f, 0.10f, 1, 0.93f);
            var log = ScrollList(right, 6, 16);
            Size(Label(log, "MISSION LOG", 22, TextAnchor.MiddleLeft, Accent, FontStyle.Bold), 32);

            var bottom = Place(Panel(root, Bg, "Bottom").rectTransform, 0, 0, 1, 0.10f);
            var status = Place(Label(bottom, "", 22).rectTransform, 0.02f, 0, 0.6f, 1).GetComponent<Text>();
            var barBg = Panel(bottom, new Color(1, 1, 1, 0.08f));
            Place(barBg.rectTransform, 0.02f, 0.12f, 0.6f, 0.28f);
            var fill = Panel(barBg.transform, Good);
            Place(fill.rectTransform, 0, 0, 0, 1);
            var next = Btn(bottom, "SKIP >", null, Card, 22, -1);
            Place((RectTransform)next.transform, 0.80f, 0.18f, 0.98f, 0.82f);

            // full-screen flash for events (red = bad, green = lucky)
            var flash = Place(Panel(root, new Color(0, 0, 0, 0), "Flash").rectTransform, 0, 0, 1, 1).GetComponent<Image>();
            flash.raycastTarget = false;

            StartCoroutine(Play());

            IEnumerator Flash(Color c)
            {
                for (float t = 0.5f; t > 0; t -= Time.deltaTime) { flash.color = new Color(c.r, c.g, c.b, t * 0.5f); yield return null; }
                flash.color = new Color(0, 0, 0, 0);
            }

            IEnumerator Play()
            {
                bool skip = false;
                next.onClick.AddListener(() => skip = true);
                if (r.launched)
                {
                    gm.view.PlayLaunch();
                    gm.view.Shake(0.15f, 1.2f);
                    status.text = "3... 2... 1... LIFTOFF!";
                    for (float t = 0; t < 2.6f && !skip; t += Time.deltaTime) yield return null;
                }
                foreach (var st in r.steps)
                {
                    if (st.log != null && !skip)
                    {
                        if (st.bad) { gm.view.Shake(); StartCoroutine(Flash(Bad)); }
                        else if (st.log.StartsWith("Lucky")) StartCoroutine(Flash(Good));
                    }
                    float ratio = r.target > 0 ? st.science / r.target : 0;
                    status.text = $"Day {st.day}/{d.scenario.durationDays}    Science {st.science:0.#} / {r.target:0}    Data home {st.dataSent:0} GB";
                    Place(fill.rectTransform, 0, 0, Mathf.Clamp01(ratio), 1);
                    fill.color = ratio >= 1 ? Good : Warn;
                    if (st.log != null)
                        Label(log, $"<color={Hex(Muted)}>Day {st.day}</color>  <color={Hex(st.bad ? Bad : Good)}>{st.log}</color>", 18, TextAnchor.UpperLeft);
                    if (!skip) yield return new WaitForSeconds(st.log != null ? 0.9f : 0.25f);
                }
                Label(log, r.launched && !r.lost ? $"<b>Mission complete.</b>" : $"<color={Hex(Bad)}><b>Mission failed.</b></color>", 20);
                next.onClick.RemoveAllListeners();
                next.GetComponentInChildren<Text>().text = "MISSION REPORT >";
                next.GetComponent<Image>().color = Accent;
                next.onClick.AddListener(ShowReport);
            }
        }

        // ============================ REPORT (post-mission analysis) ============================
        public void ShowReport()
        {
            ClearScreen();
            var d = gm.design;
            var r = gm.result;
            gm.view.SetMode(SpacecraftView.Mode.Design, d);
            TopBar($"PHASE 5 · MISSION REPORT  -  {d.scenario.title}", null);

            var left = Place(Panel(root, Bg).rectTransform, 0, 0.10f, 0.34f, 0.93f);
            var lv = Place(VBox(left, 10, 24), 0, 0, 1, 1);
            Color gc = r.grade == "S" || r.grade == "A" ? Good : r.grade == "B" || r.grade == "C" ? Warn : Bad;
            Size(Label(lv, r.grade, 140, TextAnchor.MiddleCenter, gc, FontStyle.Bold), 170);
            Size(Label(lv, $"SCORE {r.score}", 36, TextAnchor.MiddleCenter, TextCol, FontStyle.Bold), 50);
            Label(lv, $"Science: <b>{r.science:0.#}</b> / {r.target:0} ({r.SuccessRatio:P0})\n" +
                      $"Data returned: <b>{r.dataSent:0}</b> GB\n" +
                      $"Budget left: <b>${r.budgetLeft:0.#}M</b>\n" +
                      $"Mass {d.mass:0.#} kg  ·  Power {d.powerGen:0} W  ·  {d.launcher.name}", 20, TextAnchor.UpperLeft);
            if (gm.history.Count > 1)
                Label(lv, "<b>Attempts:</b>  " + string.Join("   ", gm.history.Select(h => $"#{h.attempt}: {h.grade} ({h.score})")), 18, TextAnchor.UpperLeft, Muted);

            var right = Place(Panel(root, Bg).rectTransform, 0.72f, 0.10f, 1f, 0.93f);
            var rv = ScrollList(right, 10, 16);
            if (r.ideal > 0 && r.launched && !r.lost)
            {
                Size(Label(rv, "WHERE YOUR SCIENCE WENT", 22, TextAnchor.MiddleLeft, Accent, FontStyle.Bold), 32);
                Label(rv, $"A perfect mission with this payload: <b>{r.ideal:0}</b> pts. You got <b>{r.science:0.#}</b>.", 17, TextAnchor.UpperLeft, Muted);
                if (r.factors.Count == 0) Label(rv, $"<color={Hex(Good)}>No losses. Every subsystem did its job.</color>", 17);
                foreach (var f in r.factors.OrderBy(f => f.kept))
                {
                    Bar(rv, f.name, f.kept, 1, $"{f.kept:P0} kept", f.kept >= 0.8f);
                    Label(rv, f.tip, 15, TextAnchor.UpperLeft, Muted);
                }
            }
            Size(Label(rv, "WHY IT WENT THIS WAY", 22, TextAnchor.MiddleLeft, Accent, FontStyle.Bold), 40);
            foreach (var l in r.lessons) Label(rv, "- " + l, 18, TextAnchor.UpperLeft);
            Size(Label(rv, "REAL NASA MISSION", 22, TextAnchor.MiddleLeft, Accent, FontStyle.Bold), 40);
            Label(rv, $"<b>{d.scenario.realMission}</b>\n<color={Hex(Muted)}>{d.scenario.realMissionNote}</color>", 18, TextAnchor.UpperLeft);

            var bottom = Place(Panel(root, Bg).rectTransform, 0, 0, 1, 0.10f);
            var row = Place(Node("Row", bottom), 0.25f, 0.15f, 0.75f, 0.85f);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 16; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandHeight = hl.childForceExpandWidth = true;
            Btn(row, "REDESIGN & RETRY", () => { gm.ResetGo(); ShowDesign(); }, Accent, 22, -1);
            Btn(row, "NEW MISSION", ShowScenarios, Card, 22, -1);
            Btn(row, "MAIN MENU", ShowTitle, Card, 22, -1);
        }
    }
}
