using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MissionForge
{
    public class SimStep
    {
        public int day;
        public float science;     // cumulative
        public float dataSent;    // cumulative GB
        public string log;        // event text (null = quiet)
        public bool bad;
    }

    public class SimResult
    {
        public readonly List<SimStep> steps = new List<SimStep>();
        public bool launched = true, lost;
        public float science, target, dataSent, budgetLeft;
        public int score;
        public string grade;
        public readonly List<string> lessons = new List<string>();
        public float SuccessRatio => target > 0 ? science / target : 0;
    }

    /// <summary>Runs the whole mission up front; the UI then replays it step by step.</summary>
    public static class MissionSimulator
    {
        const int Steps = 24;

        public static SimResult Run(MissionDesign d, bool teamAllGo, int seed)
        {
            var rng = new System.Random(seed);
            var r = new SimResult { target = d.scenario.targetScience, budgetLeft = d.scenario.budget - d.cost };
            float days = d.scenario.durationDays / (float)Steps;

            foreach (var c in d.checks.Where(c => !c.ok)) r.lessons.Add($"{c.title}: {c.why}");

            // --- Launch ---
            if (d.launchCapacity <= 0 || d.mass > d.launchCapacity || d.mass > d.busMaxMass)
            {
                r.launched = false;
                r.steps.Add(new SimStep { log = "LAUNCH SCRUBBED: the spacecraft cannot be launched as designed.", bad = true });
                Finish(r, d, teamAllGo);
                return r;
            }
            bool launchAnomaly = rng.NextDouble() > d.launcher.reliability;
            r.steps.Add(new SimStep { log = launchAnomaly ? "Launch anomaly! Wrong orbit insertion, fuel spent fixing it (-25% lifetime)." : "Liftoff! Spacecraft separated and deployed.", bad = launchAnomaly });
            int lastStep = launchAnomaly ? Mathf.RoundToInt(Steps * 0.75f) : Steps;

            // --- Static performance factors ---
            float powerRatio = d.powerUse > 0 ? Mathf.Clamp01(d.powerGen / d.powerUse) : 1;
            float batteryFactor = d.batteryWh >= d.batteryNeed ? 1f : Mathf.Lerp(0.5f, 1f, d.batteryNeed > 0 ? d.batteryWh / d.batteryNeed : 1);
            float dlFraction = d.dataGen > 0 ? Mathf.Clamp01(d.downlink / d.dataGen) : 1;
            bool objectiveOk = d.scenario.required.Length == 0 || d.instruments.Any(i => d.scenario.required.Contains(i.sci));
            bool orbitOk = d.scenario.allowedOrbits == null || d.scenario.allowedOrbits.Contains(d.orbit.id);
            bool overBudget = d.cost > d.scenario.budget * 1.3f;

            if (overBudget) r.lessons.Add("Cost overrun above 30%: in real life the project would face cancellation review.");
            if (!objectiveOk || !orbitOk) r.lessons.Add("Science only counts when it answers the mission's question.");

            float pointing = d.pointing;
            float sci = 0, sent = 0;
            bool hasRadHard = d.Get(Category.Computer).radHard;
            bool singleStation = d.Get(Category.Ground).contactMult <= 1 && !d.Get(Category.Ground).dsn;
            float eventChance = 0.06f + d.orbit.radiation * 0.08f + (1 - d.reliability) * 0.3f;

            if (powerRatio < 0.5f)
            {
                r.steps.Add(new SimStep { day = 1, log = "Power collapse: batteries drained in the first days. Spacecraft lost.", bad = true });
                r.lost = true;
                Finish(r, d, teamAllGo);
                return r;
            }

            for (int i = 1; i <= lastStep; i++)
            {
                string log = null; bool bad = false;
                float stepFactor = 1f;

                if (rng.NextDouble() < eventChance)
                {
                    switch (rng.Next(6))
                    {
                        case 0:
                            if (hasRadHard) log = "Solar storm! Rad-hard computer kept running.";
                            else { log = "Solar storm! Computer reset, safe mode for weeks."; stepFactor = 0; bad = true; }
                            break;
                        case 1:
                            powerRatio *= 0.9f; log = "Micrometeoroid hit a solar panel (-10% power)."; bad = true; break;
                        case 2:
                            if (d.Get(Category.ADCS).pointing >= 2) { pointing -= 1; log = "Reaction wheel failure: pointing degraded."; bad = true; }
                            else log = "Attitude glitch, recovered.";
                            break;
                        case 3:
                            if (singleStation) { stepFactor = 0.3f; log = "Ground station outage: little data downloaded."; bad = true; }
                            else log = "One ground station down; the network covered it.";
                            break;
                        case 4:
                            batteryFactor *= 0.92f; log = "Battery aging faster than expected."; bad = true; break;
                        default:
                            stepFactor = 1.3f; log = "Lucky observation window: extra science!"; break;
                    }
                }

                float stepSci = 0;
                foreach (var ins in d.instruments)
                {
                    float pointFactor = pointing >= ins.pointingReq ? 1f : 0.35f;
                    bool relevant = d.scenario.required.Length == 0 || d.scenario.required.Contains(ins.sci);
                    float value = ins.science * (relevant ? 1f : 0.3f); // off-topic science is worth less
                    stepSci += value * d.orbit.Bonus(ins.sci) * pointFactor;
                }
                stepSci *= powerRatio * batteryFactor * dlFraction * stepFactor / Steps;
                if (!objectiveOk || !orbitOk) stepSci *= 0.2f;
                sci += stepSci;
                sent += Mathf.Min(d.dataGen, d.downlink) * days * stepFactor;

                r.steps.Add(new SimStep { day = Mathf.RoundToInt(i * days), science = sci, dataSent = sent, log = log, bad = bad });
            }

            if (launchAnomaly) r.lessons.Add("Rockets are not 100% reliable. Cheaper rockets have higher risk.");
            if (dlFraction < 1) r.lessons.Add($"Only {dlFraction:P0} of measured data got home. Data that isn't downlinked is not science.");
            if (powerRatio < 1) r.lessons.Add("Not enough power meant instruments were often switched off.");

            r.science = sci;
            r.dataSent = sent;
            Finish(r, d, teamAllGo);
            return r;
        }

        static void Finish(SimResult r, MissionDesign d, bool teamAllGo)
        {
            float ratio = Mathf.Min(r.SuccessRatio, 1.5f);
            float budgetFrac = Mathf.Clamp(r.budgetLeft / d.scenario.budget, -1f, 0.5f);
            r.score = Mathf.Max(0, Mathf.RoundToInt(ratio * 700 + budgetFrac * 400 + (teamAllGo ? 100 : 0)));
            r.grade = !r.launched || r.lost ? "F"
                : r.SuccessRatio >= 1.2f ? "S"
                : r.SuccessRatio >= 1f ? "A"
                : r.SuccessRatio >= 0.7f ? "B"
                : r.SuccessRatio >= 0.4f ? "C" : "D";
            if (r.lessons.Count == 0) r.lessons.Add("Clean design! Try a cheaper or more ambitious version and compare.");
        }
    }
}
