using System;
using System.Collections.Generic;

namespace MissionForge
{
    // 4 roles = NASA's 4 mission segments (Launch / Space / Ground / User)
    public enum Role { Launch, Spacecraft, Comms, Science }

    public enum Category { Bus, Power, Battery, Computer, ADCS, Comms, Ground, Instrument }

    public enum SciType { EarthObs, Composition, Fields, SpaceWeather, Astronomy, Subsurface }

    public enum OrbitId { LEO, SSO, GEO, Lunar, Mars }

    /// <summary>One selectable spacecraft/ground item. Flat class on purpose: easy to list in UI.</summary>
    [Serializable]
    public class Part
    {
        public string id, name, desc;
        public Category cat;
        public float mass;          // kg
        public float power;         // W consumed (average)
        public float cost;          // $M
        public float reliability = 0.98f;

        // Power
        public float powerGen;      // W at 1 AU in sunlight (or constant for RTG)
        public bool rtg;
        public float batteryWh;

        // Comms
        public float rateMbps;
        public bool deepSpace;

        // Ground
        public float contactMult = 1f;
        public float costPerYear;   // $M / year of operations
        public bool dsn;

        // Instruments
        public SciType sci;
        public float dataGBday;
        public float science;
        public int pointingReq;     // 1 coarse, 2 fine, 3 precise

        // ADCS
        public int pointing;

        // Computer
        public bool radHard;

        // Bus
        public int slots;
        public float maxMass;
        public float visualScale = 1f;
    }

    [Serializable]
    public class Orbit
    {
        public OrbitId id;
        public string name, desc;
        public float solarFactor = 1f;      // sunlight strength vs Earth (1/r^2)
        public float sunFraction = 0.62f;   // fraction of orbit in sunlight
        public float eclipseMin = 35f;      // longest shadow per orbit
        public float contactMinPerDay = 40f;// ground contact with 1 station
        public float rangeFactor = 1f;      // data-rate drop with distance
        public bool needsDeepSpace;
        public float radiation;             // 0..1
        [NonSerialized] public Dictionary<SciType, float> bonus = new Dictionary<SciType, float>();

        public float Bonus(SciType t) => bonus.TryGetValue(t, out var b) ? b : 1f;
    }

    [Serializable]
    public class Launcher
    {
        public string id, name, desc;
        public float cost, reliability;
        [NonSerialized] public Dictionary<OrbitId, float> capacity = new Dictionary<OrbitId, float>();

        public float CapacityTo(OrbitId o) => capacity.TryGetValue(o, out var c) ? c : 0f;
    }

    [Serializable]
    public class Scenario
    {
        public string id, title, brief, difficulty;
        public SciType[] required;      // need at least one instrument of these types
        public OrbitId[] allowedOrbits; // null = any
        public float budget;            // $M
        public int durationDays;
        public float targetScience;
        public string realMission, realMissionNote;
    }
}
