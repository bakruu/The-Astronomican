using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MissionForge
{
    public class Check
    {
        public string title, why;
        public bool ok;
        public bool critical; // critical = mission will very likely fail
        public Role owner;
    }

    /// <summary>Current choices of the team + all derived numbers (the "trade study").</summary>
    public class MissionDesign
    {
        public Scenario scenario;
        public readonly Dictionary<Category, Part> singles = new Dictionary<Category, Part>();
        public readonly List<Part> instruments = new List<Part>();
        public Orbit orbit;
        public Launcher launcher;

        // ---- derived ----
        public float mass, launchCapacity, busMaxMass;
        public float powerUse, powerGen, batteryWh, batteryNeed;
        public float dataGen, downlink;
        public float cost, opsCost;
        public int pointing, pointingNeed, slotsUsed, slots;
        public float reliability;
        public readonly List<Check> checks = new List<Check>();

        public MissionDesign(Scenario s)
        {
            scenario = s;
            foreach (var c in new[] { Category.Bus, Category.Power, Category.Battery, Category.Computer, Category.ADCS, Category.Comms, Category.Ground })
                singles[c] = GameDatabase.OfCategory(c).First();
            orbit = s.allowedOrbits != null ? GameDatabase.GetOrbit(s.allowedOrbits[0]) : GameDatabase.Orbits[0];
            launcher = GameDatabase.Launchers[0];
            Recalculate();
        }

        public Part Get(Category c) => singles.TryGetValue(c, out var p) ? p : null;
        public bool IsSelected(Part p) => p.cat == Category.Instrument ? instruments.Contains(p) : Get(p.cat) == p;

        public void Select(Part p)
        {
            if (p.cat == Category.Instrument)
            {
                if (!instruments.Remove(p)) instruments.Add(p);
            }
            else singles[p.cat] = p;
            Recalculate();
        }

        public void SelectOrbit(Orbit o) { orbit = o; Recalculate(); }
        public void SelectLauncher(Launcher l) { launcher = l; Recalculate(); }

        public IEnumerable<Part> AllParts() => singles.Values.Concat(instruments);
        public float Years => scenario.durationDays / 365f;

        public void Recalculate()
        {
            var bus = Get(Category.Bus);
            var comms = Get(Category.Comms);
            var ground = Get(Category.Ground);
            var adcs = Get(Category.ADCS);

            mass = AllParts().Sum(p => p.mass);
            busMaxMass = bus.maxMass;
            slots = bus.slots;
            slotsUsed = instruments.Count;
            launchCapacity = launcher.CapacityTo(orbit.id);

            powerUse = AllParts().Sum(p => p.power);
            powerGen = AllParts().Where(p => p.cat == Category.Power)
                .Sum(p => p.rtg ? p.powerGen : p.powerGen * orbit.solarFactor * orbit.sunFraction);
            float rtgGen = AllParts().Where(p => p.rtg).Sum(p => p.powerGen);
            batteryWh = AllParts().Sum(p => p.batteryWh);
            // energy needed in shadow, x1.4 so we never fully drain the battery (depth of discharge)
            batteryNeed = Mathf.Max(0, powerUse - rtgGen) * orbit.eclipseMin / 60f * 1.4f;

            dataGen = instruments.Sum(i => i.dataGBday);
            bool canTalk = !orbit.needsDeepSpace || (comms.deepSpace && ground.dsn);
            float contactMin = Mathf.Min(1440, orbit.contactMinPerDay * ground.contactMult);
            downlink = canTalk ? comms.rateMbps * orbit.rangeFactor * contactMin * 60f / 8000f : 0f; // GB/day

            pointing = adcs.pointing;
            if (orbit.id != OrbitId.LEO && orbit.id != OrbitId.SSO && adcs.id == "adcs_mag") pointing = 0; // no magnetic field to push on
            pointingNeed = instruments.Count == 0 ? 0 : instruments.Max(i => i.pointingReq);

            opsCost = ground.costPerYear * Years;
            cost = AllParts().Sum(p => p.cost) + launcher.cost + opsCost;

            reliability = launcher.reliability;
            foreach (var p in AllParts()) reliability *= p.reliability;

            BuildChecks(canTalk);
        }

        void BuildChecks(bool canTalk)
        {
            checks.Clear();
            void Add(Role r, bool ok, bool critical, string title, string why)
                => checks.Add(new Check { owner = r, ok = ok, critical = critical, title = title, why = why });

            var s = scenario;
            bool hasRequired = s.required.Length == 0 || instruments.Any(i => s.required.Contains(i.sci));
            Add(Role.Science, hasRequired, true, "Objective instrument",
                hasRequired ? "You carry an instrument that can answer the mission question."
                            : $"The objective needs a {string.Join(" or ", s.required)} instrument. Without it there is no science.");

            bool orbitOk = s.allowedOrbits == null || s.allowedOrbits.Contains(orbit.id);
            Add(Role.Launch, orbitOk, true, "Orbit fits objective",
                orbitOk ? "This orbit can see your target." : "Your target cannot be observed from this orbit.");

            Add(Role.Spacecraft, slotsUsed <= slots, true, $"Instrument slots {slotsUsed}/{slots}",
                slotsUsed <= slots ? "All instruments fit on the bus." : "Too many instruments for this bus. Pick a bigger bus or drop one.");

            Add(Role.Spacecraft, mass <= busMaxMass, true, $"Bus structure {mass:0.#}/{busMaxMass:0} kg",
                mass <= busMaxMass ? "The structure can carry this mass." : "The structure is overloaded. Choose a bigger bus or lighter parts.");

            bool fits = launchCapacity > 0 && mass <= launchCapacity;
            Add(Role.Launch, fits, true, $"Launch capacity {mass:0.#}/{launchCapacity:0} kg",
                launchCapacity <= 0 ? "This rocket cannot reach the selected orbit at all."
                : fits ? $"The rocket has {launchCapacity - mass:0} kg margin." : "Too heavy for this rocket to this orbit. Bigger rocket or lighter spacecraft.");

            bool powerOk = powerGen >= powerUse;
            Add(Role.Spacecraft, powerOk, powerGen < powerUse * 0.5f, $"Power {powerGen:0} W made / {powerUse:0} W used",
                powerOk ? "Power budget is positive." :
                orbit.solarFactor < 1 ? "Sunlight is weak here; your panels don't make enough. Bigger arrays, RTG, or fewer instruments."
                                      : "Systems use more power than you make. Instruments will be switched off part of the time.");

            bool batOk = batteryWh >= batteryNeed;
            Add(Role.Spacecraft, batOk, false, $"Battery {batteryWh:0}/{batteryNeed:0} Wh for eclipse",
                batOk ? "The battery survives the planet's shadow." : $"Each orbit has {orbit.eclipseMin:0} min of shadow. The battery runs flat, so systems brown out.");

            Add(Role.Spacecraft, pointing >= pointingNeed, false, $"Pointing level {pointing}/{pointingNeed}",
                pointing >= pointingNeed ? "Instruments can be pointed accurately." :
                pointing == 0 ? "Magnetorquers need Earth's magnetic field. They don't work here." : "Blurry data: instruments need more precise pointing (reaction wheels / star tracker).");

            Add(Role.Comms, canTalk, true, "Can reach Earth",
                canTalk ? "Radio link to Earth is possible." : "Beyond Earth orbit you need a deep-space antenna AND the Deep Space Network.");

            bool dataOk = dataGen <= downlink || dataGen == 0;
            Add(Role.Comms, dataOk, false, $"Data {dataGen:0.##} GB made / {downlink:0.##} GB sent per day",
                dataOk ? "Everything you measure gets home." : $"Only {(dataGen > 0 ? Mathf.Clamp01(downlink / dataGen) : 1):P0} of your data reaches Earth. Faster radio or more ground stations.");

            bool budgetOk = cost <= s.budget;
            Add(Role.Science, budgetOk, cost > s.budget * 1.3f, $"Budget ${cost:0.#}M / ${s.budget:0}M",
                budgetOk ? $"${s.budget - cost:0.#}M left in reserve." : "Over budget! Project may be cancelled. Cut cost somewhere.");
        }

        public bool AnyCriticalFail => checks.Any(c => !c.ok && c.critical);
    }
}
