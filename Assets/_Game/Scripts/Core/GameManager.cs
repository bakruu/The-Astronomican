using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MissionForge
{
    /// <summary>
    /// Entry point. Spawns itself on play in any scene, so no scene setup is needed.
    /// Flow: Title -> Scenario (Office Hours) -> Design (Trade Studies) -> Go/No-Go -> Simulation -> Report
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const string GameTitle = "THE ASTRONOMICAN";

        public static GameManager I { get; private set; }

        public int players = 1;                       // 1 = solo, 2-4 = team (hot-seat)
        [System.NonSerialized] public MissionDesign design;
        [System.NonSerialized] public SimResult result;
        public readonly Dictionary<Role, bool> go = new Dictionary<Role, bool>();
        public int attempts;
        public readonly List<(int attempt, int score, string grade)> history = new List<(int, int, string)>();

        public GameUI ui;
        public SpacecraftView view;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<GameManager>() == null)
                new GameObject("GameManager").AddComponent<GameManager>();
        }

        void Awake()
        {
            I = this;
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            view = new GameObject("SpacecraftView").AddComponent<SpacecraftView>();
            ui = gameObject.AddComponent<GameUI>();
        }

        void Start() => ui.ShowTitle();

        /// <summary>Which player controls a role (round-robin over 4 roles).</summary>
        public int PlayerFor(Role r) => players <= 1 ? 1 : ((int)r % players) + 1;

        public void StartScenario(Scenario s)
        {
            design = new MissionDesign(s);
            attempts = 0;
            history.Clear();
            ResetGo();
            ui.ShowDesign();
        }

        public void ResetGo()
        {
            foreach (Role r in System.Enum.GetValues(typeof(Role))) go[r] = false;
        }

        public bool AllGo => players <= 1 || go.Values.All(v => v);

        /// <summary>Any design change invalidates the team's Go/No-Go poll.</summary>
        public void OnDesignChanged()
        {
            ResetGo();
            view.Build(design);
        }

        public void Launch()
        {
            attempts++;
            result = MissionSimulator.Run(design, players > 1 && AllGo, Random.Range(0, int.MaxValue));
            history.Add((attempts, result.score, result.grade));
            ui.ShowSimulation();
        }
    }
}
