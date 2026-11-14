using System.Collections.Generic;
using System.Linq;

namespace MissionForge
{
    /// <summary>
    /// All game content. Values are simplified but in realistic ranges
    /// (CubeSat 101, NASA MDL subsystem lists). Edit freely to balance.
    /// </summary>
    public static class GameDatabase
    {
        public static readonly List<Part> Parts = new List<Part>
        {
            // ---------- BUS (structure) ----------
            new Part { id="bus_6u", cat=Category.Bus, name="6U CubeSat", mass=2.5f, cost=0.5f, slots=1, maxMass=12, visualScale=0.6f,
                desc="Shoebox-sized. Cheap, light, only room for one small instrument." },
            new Part { id="bus_12u", cat=Category.Bus, name="12U CubeSat", mass=5, cost=1.2f, slots=2, maxMass=24, visualScale=0.8f,
                desc="Double CubeSat. Two small instruments fit." },
            new Part { id="bus_small", cat=Category.Bus, name="SmallSat (150 kg class)", mass=60, cost=15, slots=3, maxMass=300, reliability=0.99f, visualScale=1.1f,
                desc="Washing-machine size. Room for bigger instruments and big solar arrays." },
            new Part { id="bus_large", cat=Category.Bus, name="Flagship Bus", mass=600, cost=120, slots=4, maxMass=5000, reliability=0.995f, visualScale=1.6f,
                desc="Bus-sized observatory platform. Carries anything, costs a lot." },

            // ---------- POWER ----------
            new Part { id="pwr_body", cat=Category.Power, name="Body-mounted cells", mass=0.3f, cost=0.1f, powerGen=20,
                desc="Solar cells glued to the sides. Light, but little power." },
            new Part { id="pwr_deploy", cat=Category.Power, name="Deployable panels", mass=1.5f, cost=0.5f, powerGen=70,
                desc="Fold-out wings. 3x more power, a bit more mass." },
            new Part { id="pwr_large", cat=Category.Power, name="Large solar array", mass=30, cost=6, powerGen=900,
                desc="Big rotating arrays for SmallSats and larger." },
            new Part { id="pwr_rtg", cat=Category.Power, name="RTG (nuclear)", mass=45, cost=70, powerGen=110, rtg=true, reliability=0.995f,
                desc="Works in the dark and far from the Sun. Very expensive." },

            // ---------- BATTERY ----------
            new Part { id="bat_s", cat=Category.Battery, name="Small battery (40 Wh)", mass=0.3f, cost=0.05f, batteryWh=40,
                desc="Enough for a tiny CubeSat in shadow." },
            new Part { id="bat_m", cat=Category.Battery, name="Medium battery (200 Wh)", mass=1.5f, cost=0.3f, batteryWh=200,
                desc="Good for larger CubeSats." },
            new Part { id="bat_l", cat=Category.Battery, name="Large battery (2000 Wh)", mass=15, cost=2, batteryWh=2000,
                desc="SmallSat and flagship class." },

            // ---------- COMPUTER ----------
            new Part { id="obc_basic", cat=Category.Computer, name="Commercial computer", mass=0.1f, power=1, cost=0.05f, reliability=0.93f,
                desc="Cheap. Radiation can make it reset (safe mode)." },
            new Part { id="obc_rad", cat=Category.Computer, name="Rad-hard computer", mass=1, power=5, cost=1.5f, radHard=true, reliability=0.99f,
                desc="Survives radiation storms. Needed far from Earth." },

            // ---------- ADCS (pointing) ----------
            new Part { id="adcs_mag", cat=Category.ADCS, name="Magnetorquers", mass=0.3f, power=1, cost=0.1f, pointing=1,
                desc="Uses Earth's magnetic field. Coarse pointing, useless far from Earth." },
            new Part { id="adcs_rw", cat=Category.ADCS, name="Reaction wheels", mass=1.5f, power=6, cost=0.6f, pointing=2, reliability=0.96f,
                desc="Spinning wheels turn the spacecraft. Fine pointing for cameras." },
            new Part { id="adcs_st", cat=Category.ADCS, name="Star tracker + wheels", mass=8, power=20, cost=4, pointing=3, reliability=0.97f,
                desc="Navigates by the stars. Precise pointing for telescopes." },

            // ---------- COMMS ----------
            new Part { id="com_uhf", cat=Category.Comms, name="UHF radio", mass=0.2f, power=2, cost=0.05f, rateMbps=0.01f,
                desc="Simple radio, very slow (10 kbps)." },
            new Part { id="com_s", cat=Category.Comms, name="S-band radio", mass=0.5f, power=8, cost=0.3f, rateMbps=2,
                desc="Standard radio, 2 Mbps." },
            new Part { id="com_x", cat=Category.Comms, name="X-band radio", mass=2, power=25, cost=2, rateMbps=50,
                desc="High-speed downlink for image-heavy missions." },
            new Part { id="com_hga", cat=Category.Comms, name="High-gain antenna (deep space)", mass=15, power=60, cost=10, rateMbps=40, deepSpace=true, reliability=0.99f,
                desc="Large dish that can talk across the Solar System." },

            // ---------- GROUND SEGMENT ----------
            new Part { id="gnd_one", cat=Category.Ground, name="Single university station", cost=0.2f, costPerYear=0.3f, contactMult=1,
                desc="One antenna. Spacecraft passes over it a few times a day." },
            new Part { id="gnd_net", cat=Category.Ground, name="Commercial station network", cost=0.5f, costPerYear=2, contactMult=4,
                desc="Stations around the world: about 4x more contact time." },
            new Part { id="gnd_dsn", cat=Category.Ground, name="NASA Deep Space Network", cost=1, costPerYear=8, contactMult=1, dsn=true, reliability=0.995f,
                desc="Giant 34-70 m dishes. Required beyond Earth orbit." },

            // ---------- INSTRUMENTS (payload) ----------
            new Part { id="ins_rgb", cat=Category.Instrument, name="Color camera", sci=SciType.EarthObs, mass=0.5f, power=3, cost=0.3f, dataGBday=2, science=12, pointingReq=1,
                desc="Simple color images of the surface." },
            new Part { id="ins_ms", cat=Category.Instrument, name="Multispectral imager", sci=SciType.EarthObs, mass=3, power=12, cost=2.5f, dataGBday=10, science=30, pointingReq=2,
                desc="Sees many colors incl. infrared: plant health, water, ice." },
            new Part { id="ins_tir", cat=Category.Instrument, name="Thermal infrared camera", sci=SciType.EarthObs, mass=6, power=20, cost=5, dataGBday=4, science=28, pointingReq=2,
                desc="Measures surface temperature: fires, ice, heat." },
            new Part { id="ins_mag", cat=Category.Instrument, name="Magnetometer", sci=SciType.Fields, mass=0.3f, power=1, cost=0.2f, dataGBday=0.05f, science=8, pointingReq=1,
                desc="Measures magnetic fields. Tiny data, tiny cost." },
            new Part { id="ins_rad", cat=Category.Instrument, name="Radiation detector", sci=SciType.SpaceWeather, mass=0.5f, power=2, cost=0.3f, dataGBday=0.1f, science=10, pointingReq=1,
                desc="Counts energetic particles: space weather for astronauts." },
            new Part { id="ins_spec", cat=Category.Instrument, name="IR spectrometer", sci=SciType.Composition, mass=8, power=18, cost=8, dataGBday=1.5f, science=40, pointingReq=2,
                desc="Splits light to find what surfaces are made of (e.g. water ice)." },
            new Part { id="ins_radar", cat=Category.Instrument, name="Subsurface radar", sci=SciType.Subsurface, mass=20, power=90, cost=18, dataGBday=6, science=55, pointingReq=1,
                desc="Sees ice and rock layers below the surface. Power hungry." },
            new Part { id="ins_tel", cat=Category.Instrument, name="UV/Optical telescope", sci=SciType.Astronomy, mass=120, power=150, cost=90, dataGBday=15, science=120, pointingReq=3,
                desc="A real space telescope. Needs precise pointing." },
        };

        public static readonly List<Orbit> Orbits = new List<Orbit>
        {
            new Orbit { id=OrbitId.LEO, name="Low Earth Orbit (500 km)", radiation=0.1f, eclipseMin=36, sunFraction=0.62f, contactMinPerDay=40,
                desc="Close and cheap to reach. Great images, short ground passes.",
                bonus = { [SciType.EarthObs]=1.1f, [SciType.Fields]=1.1f } },
            new Orbit { id=OrbitId.SSO, name="Sun-synchronous (700 km)", radiation=0.15f, eclipseMin=30, sunFraction=0.68f, contactMinPerDay=50,
                desc="Passes over every place at the same local time: ideal for Earth observation.",
                bonus = { [SciType.EarthObs]=1.4f } },
            new Orbit { id=OrbitId.GEO, name="Geostationary (36,000 km)", radiation=0.4f, eclipseMin=70, sunFraction=0.99f, contactMinPerDay=1440, rangeFactor=0.3f,
                desc="Hovers over one spot. Constant contact, but far: images are less detailed.",
                bonus = { [SciType.EarthObs]=0.8f, [SciType.SpaceWeather]=1.4f } },
            new Orbit { id=OrbitId.Lunar, name="Lunar orbit", radiation=0.5f, eclipseMin=47, sunFraction=0.7f, contactMinPerDay=480, rangeFactor=0.08f, needsDeepSpace=true,
                desc="Around the Moon. Needs deep-space radio and the DSN.",
                bonus = { [SciType.Composition]=1.5f, [SciType.Subsurface]=1.3f, [SciType.EarthObs]=0.2f } },
            new Orbit { id=OrbitId.Mars, name="Mars orbit", radiation=0.6f, solarFactor=0.43f, eclipseMin=40, sunFraction=0.65f, contactMinPerDay=480, rangeFactor=0.03f, needsDeepSpace=true,
                desc="Sunlight is only 43% of Earth's. Data trickles home slowly.",
                bonus = { [SciType.Composition]=1.6f, [SciType.Subsurface]=1.8f, [SciType.Fields]=1.3f, [SciType.EarthObs]=0f } },
        };

        public static readonly List<Launcher> Launchers = new List<Launcher>
        {
            new Launcher { id="lv_ride", name="CubeSat rideshare", cost=0.6f, reliability=0.97f,
                desc="Ride along with a bigger satellite. LEO/SSO only, 30 kg max.",
                capacity = { [OrbitId.LEO]=30, [OrbitId.SSO]=30 } },
            new Launcher { id="lv_small", name="Small launcher", cost=8, reliability=0.94f,
                desc="Dedicated small rocket. Goes exactly where you want.",
                capacity = { [OrbitId.LEO]=300, [OrbitId.SSO]=200, [OrbitId.Lunar]=40 } },
            new Launcher { id="lv_medium", name="Medium launcher", cost=65, reliability=0.98f,
                desc="Workhorse rocket. Can reach the Moon and Mars.",
                capacity = { [OrbitId.LEO]=15000, [OrbitId.SSO]=10000, [OrbitId.GEO]=5000, [OrbitId.Lunar]=2500, [OrbitId.Mars]=1200 } },
            new Launcher { id="lv_heavy", name="Heavy launcher", cost=150, reliability=0.97f,
                desc="Huge rocket for flagship missions.",
                capacity = { [OrbitId.LEO]=60000, [OrbitId.SSO]=45000, [OrbitId.GEO]=20000, [OrbitId.Lunar]=15000, [OrbitId.Mars]=12000 } },
        };

        public static readonly List<Scenario> Scenarios = new List<Scenario>
        {
            new Scenario { id="forest", title="Forest Watch", difficulty="Easy",
                brief="Monitor deforestation and crop health around the world for one year. A university team with a small budget.",
                required = new[]{ SciType.EarthObs }, allowedOrbits = new[]{ OrbitId.LEO, OrbitId.SSO, OrbitId.GEO },
                budget=12, durationDays=365, targetScience=35,
                realMission="Landsat 9 / PlanetScope", realMissionNote="Landsat flies a multispectral imager in sun-synchronous orbit; Planet uses fleets of CubeSats." },
            new Scenario { id="moonice", title="Lunar Ice Hunter", difficulty="Medium",
                brief="Find water ice in the Moon's shadowed craters, a key resource for future astronauts.",
                required = new[]{ SciType.Composition, SciType.Subsurface }, allowedOrbits = new[]{ OrbitId.Lunar },
                budget=130, durationDays=365, targetScience=60,
                realMission="Lunar Trailblazer / LRO", realMissionNote="Lunar Trailblazer is a NASA SmallSat with an IR spectrometer to map lunar water." },
            new Scenario { id="marsice", title="Buried Ice of Mars", difficulty="Hard",
                brief="Map ice layers beneath the Martian surface for one Mars year.",
                required = new[]{ SciType.Subsurface, SciType.Composition }, allowedOrbits = new[]{ OrbitId.Mars },
                budget=450, durationDays=687, targetScience=90,
                realMission="Mars Reconnaissance Orbiter (SHARAD)", realMissionNote="MRO carries the SHARAD subsurface radar and a high-gain antenna to send its huge data volume home." },
            new Scenario { id="sandbox", title="Sandbox", difficulty="Free",
                brief="No objective, huge budget. Experiment with any combination and see what happens.",
                required = new SciType[0], allowedOrbits = null,
                budget=2000, durationDays=730, targetScience=100,
                realMission="Hubble / JWST", realMissionNote="Flagship observatories combine precise pointing, big power and fast downlink." },
        };

        public static IEnumerable<Part> OfCategory(Category c) => Parts.Where(p => p.cat == c);
        public static Orbit GetOrbit(OrbitId id) => Orbits.First(o => o.id == id);

        public static readonly Dictionary<Role, Category[]> RoleCategories = new Dictionary<Role, Category[]>
        {
            [Role.Launch] = new Category[0], // orbit + launcher are special lists
            [Role.Spacecraft] = new[] { Category.Bus, Category.Power, Category.Battery, Category.Computer, Category.ADCS },
            [Role.Comms] = new[] { Category.Comms, Category.Ground },
            [Role.Science] = new[] { Category.Instrument },
        };

        public static string RoleName(Role r) => r switch
        {
            Role.Launch => "Launch & Orbit",
            Role.Spacecraft => "Spacecraft",
            Role.Comms => "Ground & Comms",
            _ => "Science",
        };

        // Short "why it matters" explanations (educational layer)
        public static string Explain(Category c) => c switch
        {
            Category.Bus => "The body of the spacecraft. Decides how much mass and how many instruments it can carry.",
            Category.Power => "Makes electricity. Every system uses power; solar gets weaker far from the Sun and in shadow.",
            Category.Battery => "Keeps everything alive while the spacecraft is in a planet's shadow (eclipse).",
            Category.Computer => "The brain. Radiation can crash cheap computers and cost you science days.",
            Category.ADCS => "Attitude control: how precisely the spacecraft can point its instruments.",
            Category.Comms => "The radio. Faster radios send more data home, but use more power.",
            Category.Ground => "Antennas on Earth. More stations = more contact time = more data home.",
            Category.Instrument => "Your science. More/better instruments = more science, but also more mass, power, data and cost.",
            _ => ""
        };
    }
}
