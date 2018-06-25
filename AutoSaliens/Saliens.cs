using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api.Models;
using AutoSaliens.Console;
using Timer = System.Timers.Timer;

namespace AutoSaliens
{
    internal class Saliens : IDisposable
    {
        private const int maxGameTime = 120;
        private readonly Dictionary<Difficulty, int> pointsPerSecond = new Dictionary<Difficulty, int>
        {
            { Difficulty.Low, 5 },
            { Difficulty.Medium, 10 },
            { Difficulty.High, 20 }
        };

        private CancellationTokenSource cancellationTokenSource;
        private readonly Timer updatePlanetsTimer = new Timer(10 * 60 * 1000);
        private readonly Timer updatePlayerInfoTimer = new Timer(5 * 60 * 1000);


        public Saliens()
        {
            this.updatePlanetsTimer.Elapsed += this.UpdatePlanetsTimer_Elapsed;
            this.updatePlayerInfoTimer.Elapsed += this.UpdatePlayerInfoTimer_Elapsed;
        }

        public bool AutomationActive { get; private set; }

        public bool EnableNetworkTolerance => Program.Settings.EnableNetworkTolerance;

        public int GameTime => Program.Settings.GameTime;

        public string OverridePlanetId => Program.Settings.OverridePlanetId;

        public AutomationStrategy Strategy => Program.Settings.Strategy;

        public string Token => Program.Settings.Token;


        public List<Planet> PlanetDetails { get; set; }

        public PlayerInfoResponse PlayerInfo { get; set; }

        public Planet JoinedPlanet => this.PlanetDetails?.FirstOrDefault(p => p.Id == this.JoinedPlanetId);

        public string JoinedPlanetId
        {
            get => this.PlayerInfo?.ActivePlanet;
            set => this.PlayerInfo.ActivePlanet = value;
        }

        public Zone JoinedZone => this.JoinedPlanet?.Zones?.FirstOrDefault(z => z.ZonePosition == this.JoinedZonePosition);

        public int? JoinedZonePosition
        {
            get => this.PlayerInfo?.ActiveZonePosition != null ? (int?)int.Parse(this.PlayerInfo.ActiveZonePosition) : null;
            set => this.PlayerInfo.ActiveZonePosition = value?.ToString();
        }
        
        public DateTime JoinedZoneStart { get; set; }

        public TimeSpan ReportScoreNetworkDelay { get; set; }

        public float ReportScoreNetworkDelayTolerance { get; set; } = 0.2f;


        public Task Start()
        {
            return Task.Run(() =>
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                this.updatePlanetsTimer.Start();
                this.updatePlayerInfoTimer.Start();
                new Thread(async () =>
                {
                    // Start loop
                    this.AutomationActive = true;
                    await this.Loop();
                }).Start();
            });
        }

        public void Stop()
        {
            this.AutomationActive = false;
            this.cancellationTokenSource?.Cancel();
            this.updatePlanetsTimer.Stop();
            this.updatePlayerInfoTimer.Stop();
        }


        private async Task Loop()
        {
            // Initialization
            bool isInitialized = false;
            while (!isInitialized)
            {
                try
                {
                    await Task.WhenAll(
                        this.UpdatePlanets(false).ContinueWith(t => this.PrintActivePlanets()),
                        this.UpdatePlayerInfo().ContinueWith(t => this.PrintPlayerInfo())
                    );
                    isInitialized = true;
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Shell.WriteLine(Shell.FormatExceptionOutput(ex));
                    Shell.WriteLine($"{{verb}}Attempting to restart in 10 seconds...");
                    try { await Task.Delay(TimeSpan.FromSeconds(10), this.cancellationTokenSource.Token); }
                    catch (Exception) { }
                }
            }

            while (this.AutomationActive)
            {
                try
                {
                    // If the player is already joined in a zone, wait for it to end and submit the score afterwards
                    if (this.JoinedZonePosition != null)
                    {
                        await this.WaitForJoinedZoneToFinish();
                        var score = this.CalculatePoints(this.JoinedZone?.Difficulty ?? Difficulty.Low, maxGameTime);
                        await this.ReportScore(score);

                        // Manually leave game to hopefully prevent invalid states
                        await this.LeaveGame(this.JoinedZone.GameId);
                    }

                    // Get our most wanted planet
                    Planet planet = null;
                    // Force planet override
                    if (this.OverridePlanetId != null)
                        planet = this.PlanetDetails.FirstOrDefault(p => p.Id == this.OverridePlanetId);

                    // Force current joined planet if FocusCurrentPlanet is selected
                    if (this.Strategy.HasFlag(AutomationStrategy.FocusCurrentPlanet) && this.JoinedPlanet != null)
                        planet = this.JoinedPlanet;

                    if (planet == null)
                    {
                        // We also capture the FocusCurrentPlanet strategy here, because it's possible that
                        // the planet has finished and we still want to move on to some other planet
                        var planets = this.PlanetDetails.Where(p => p.State.Running).OrderBy(p => 0);
                        if (this.Strategy.HasFlag(AutomationStrategy.FocusRandomPlanet))
                        {
                            var planetList = planets.ToList();
                            planet = planetList[new Random().Next(0, planetList.Count - 1)];
                        }
                        else
                        {
                            if (this.Strategy.HasFlag(AutomationStrategy.MostDifficultPlanetsFirst))
                                planets = planets.ThenByDescending(p => (int)p.State.Difficulty).ThenByDescending(p => p.Zones.Where(z => !z.Captured).Max(z => (int)z.Difficulty));
                            else if (this.Strategy.HasFlag(AutomationStrategy.LeastDifficultPlanetsFirst))
                                planets = planets.ThenBy(p => (int)p.State.Difficulty).ThenBy(p => p.Zones.Where(z => !z.Captured).Max(z => (int)z.Difficulty));
                            if (this.Strategy.HasFlag(AutomationStrategy.MostCompletedPlanetsFirst))
                                planets = planets.ThenByDescending(p => p.State.CaptureProgress);
                            else if (this.Strategy.HasFlag(AutomationStrategy.LeastCompletedPlanetsFirst))
                                planets = planets.ThenBy(p => p.State.CaptureProgress);

                            if (this.Strategy.HasFlag(AutomationStrategy.TopDown))
                                planets = planets.ThenBy(p => p.Id);
                            else if (this.Strategy.HasFlag(AutomationStrategy.BottomUp))
                                planets = planets.ThenByDescending(p => p.Id);
                            planet = planets.First();
                        }
                    }

                    // Check if the joined planet is fully captured or if we go for a new one
                    if (this.JoinedPlanet != null && (this.JoinedPlanet.State.Captured || planet.Id != this.JoinedPlanetId))
                    {
                        var planetName = this.JoinedPlanet.State.Name;
                        var planetId = this.JoinedPlanetId;
                        if (this.JoinedPlanet.State.Captured)
                            Shell.WriteLine($"{{planet}}{planet.Id} ({planetName}){{action}} has been fully captured, leaving planet...");
                        await this.LeaveGame(this.JoinedPlanetId);
                    }

                    // Fly to the new planet when it's different
                    if (planet.Id != this.JoinedPlanetId)
                    {
                        Shell.WriteLine($"{{action}}Joining planet {{planet}}{planet.Id} ({planet.State.Name}){{action}}...");
                        await this.JoinPlanet(planet.Id);
                    }

                    // Get our most wanted zone
                    Zone zone = null;
                    var zones = this.JoinedPlanet.Zones.Where(z => !z.Captured).OrderBy(z => 0);
                    if (this.Strategy.HasFlag(AutomationStrategy.MostDifficultZonesFirst))
                        zones = zones.ThenByDescending(z => (int)z.Difficulty);
                    else if (this.Strategy.HasFlag(AutomationStrategy.LeastDifficultZonesFirst))
                        zones = zones.ThenBy(z => (int)z.Difficulty);
                    if (this.Strategy.HasFlag(AutomationStrategy.MostCompletedZonesFirst))
                        zones = zones.ThenByDescending(z => z.CaptureProgress);
                    else if (this.Strategy.HasFlag(AutomationStrategy.LeastCompletedZonesFirst))
                        zones = zones.ThenBy(z => z.CaptureProgress);

                    if (this.Strategy.HasFlag(AutomationStrategy.TopDown))
                        zones = zones.ThenBy(z => z.ZonePosition);
                    else if (this.Strategy.HasFlag(AutomationStrategy.BottomUp))
                        zones = zones.ThenByDescending(z => z.ZonePosition);
                    zone = zones.First();

                    // Join the zone
                    Shell.WriteLine($"{{action}}Joining {{zone}}zone {zone.ZonePosition}{{action}}...");
                    Shell.WriteLine(zone.ToConsoleLine());
                    await this.JoinZone(zone.ZonePosition);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Shell.WriteLine(Shell.FormatExceptionOutput(ex));

                    try
                    {
                        // Assume we're stuck leave game and restart
                        Shell.WriteLine($"{{verb}}Attempting to restart in 10 seconds...");
                        if (this.JoinedZonePosition != null)
                            await this.LeaveGame(this.JoinedZone.GameId);

                        var tasks = new List<Task>();
                        if (!string.IsNullOrWhiteSpace(this.JoinedPlanetId) && this.JoinedPlanetId != "0")
                            tasks.Add(this.LeaveGame(this.JoinedPlanetId));
                        tasks.Add(this.UpdatePlayerInfo().ContinueWith(t => this.PrintPlayerInfo()));
                        tasks.Add(this.UpdatePlanets().ContinueWith(t => this.PrintActivePlanets()));
                        await Task.WhenAll(tasks);

                        // Reset local states properly
                        this.JoinedZonePosition = null;
                        this.JoinedPlanetId = null;
                    }
                    catch (Exception ex2)
                    {
                        Shell.WriteLine(Shell.FormatExceptionOutput(ex2));
                    }

                    try { await Task.Delay(TimeSpan.FromSeconds(10), this.cancellationTokenSource.Token); }
                    catch (Exception) { }
                }
            }
        }


        private async void UpdatePlanetsTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                await this.UpdatePlanets(false);
            }
            catch (Exception ex)
            {
                Shell.WriteLine(Shell.FormatExceptionOutput(ex));
            }
        }

        private async void UpdatePlayerInfoTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                await this.UpdatePlayerInfo();
            }
            catch (Exception ex)
            {
                Shell.WriteLine(Shell.FormatExceptionOutput(ex));
            }
        }


        private async Task WaitForJoinedZoneToFinish()
        {
            var targetTime = this.JoinedZoneStart + TimeSpan.FromSeconds(this.GameTime);
            if (this.EnableNetworkTolerance)
                targetTime -= TimeSpan.FromMilliseconds(this.ReportScoreNetworkDelay.TotalMilliseconds * (1 - this.ReportScoreNetworkDelayTolerance));

            var timeLeft = targetTime - DateTime.Now;
            if (timeLeft.TotalSeconds > 0)
            {
                Shell.WriteLine($"{{action}}Waiting for zone to finish in {{value}}{timeLeft.TotalSeconds.ToString("#.###")} seconds{{action}} (at {{value}}{targetTime.ToString("HH:mm:ss.fff")}{{action}})...");
                var tasks = new List<Task>();
                if (timeLeft.TotalSeconds > 10)
                {
                    // Schedule to update some data 5 seconds before the zone is finished
                    tasks.Add(Task.Delay(timeLeft - TimeSpan.FromSeconds(5)).ContinueWith(async t => {
                        await this.UpdatePlanets();
                        this.PrintActivePlanets();
                    }));
                }
                tasks.Add(Task.Delay(timeLeft));
                await Task.WhenAll(tasks);
            }
        }


        public int CalculatePoints(Difficulty difficulty, int seconds)
        {
            //! This has some stupid fallback to at least try to get some points, might fail
            return this.pointsPerSecond.ContainsKey(difficulty) ?
                this.pointsPerSecond[difficulty] * seconds :
                this.pointsPerSecond[Difficulty.High] * seconds;
        }


        public async Task UpdatePlanets(bool activeOnly = true)
        {
            var newPlanets = await SaliensApi.GetPlanets(activeOnly);
            if (!activeOnly)
            {
                // Only bother updating when it's the full list,
                // we override the active ones with a more detailed instance further ahead
                this.PlanetDetails = newPlanets;
            }
            else
            {
                // Override all non-captured planet active properties to make sure we don't leave a few dangling
                foreach (var planet in this.PlanetDetails)
                    if (!planet.State.Captured)
                        planet.State.Active = false;
            }

            // Get zones of all active planets + 1 upcoming
            var planetsToGetZonesOf = newPlanets.OrderBy(p => p.State.Priority).Where(p => p.State.Running);
            var lastPlanetIndex = this.PlanetDetails.ToList().FindIndex(p => p.Id == planetsToGetZonesOf.Last().Id);
            planetsToGetZonesOf = planetsToGetZonesOf.Concat(this.PlanetDetails.Skip(lastPlanetIndex + 1).Take(1));

            var newDetails = await Task.WhenAll(planetsToGetZonesOf.Select(p => SaliensApi.GetPlanet(p.Id)));
            foreach (var planet in newDetails)
            {
                // Replace the planet because this instance has more information (e.g. zones)
                var i = this.PlanetDetails.FindIndex(p => p.Id == planet.Id);
                this.PlanetDetails[i] = planet;
            }
        }

        public async Task UpdatePlayerInfo()
        {
            if (string.IsNullOrEmpty(this.Token))
                return;

            this.PlayerInfo = await SaliensApi.GetPlayerInfo(this.Token);

            if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActiveZonePosition))
                this.JoinedZoneStart = DateTime.Now - this.PlayerInfo.TimeInZone;
        }

        public async Task JoinPlanet(string planetId)
        {
            await SaliensApi.JoinPlanet(this.Token, planetId);
            this.JoinedPlanetId = planetId;
        }

        public async Task JoinZone(int zonePosition)
        {
            try
            {
                await SaliensApi.JoinZone(this.Token, zonePosition);
                this.JoinedZonePosition = zonePosition;
                this.JoinedZoneStart = DateTime.Now;
            }
            catch (SaliensApiException ex)
            {
                if (ex.EResult == EResult.Expired || ex.EResult == EResult.NoMatch)
                {
                    Shell.WriteLine("{warn}Failed to join zone: Zone already captured");
                    this.JoinedZonePosition = null;
                }
                throw;
            }
        }

        public async Task ReportScore(int score)
        {
            for (int i = 0; ; i++)
            {
                try
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var response = await SaliensApi.ReportScore(this.Token, score);
                    stopwatch.Stop();
                    // Only change the network delay if the last delay was lower
                    // Don't want to be too eager for an occasional spike
                    if (this.ReportScoreNetworkDelay.TotalMilliseconds == 0 || stopwatch.Elapsed < this.ReportScoreNetworkDelay)
                        this.ReportScoreNetworkDelay = stopwatch.Elapsed;

                    if (!string.IsNullOrWhiteSpace(response.NewScore))
                        Shell.WriteLine($"XP: {{oldxp}}{long.Parse(response.OldScore).ToString("#,##0")}{{reset}} -> {{xp}}{long.Parse(response.NewScore).ToString("#,##0")}{{reset}} (next level at {{reqxp}}{long.Parse(response.NextLevelScore).ToString("#,##0")}{{reset}})");
                    if (response.NewLevel != response.OldLevel)
                        Shell.WriteLine($"New level: {{oldlevel}}{response.OldLevel}{{reset}} -> {{level}}{response.NewLevel}{{reset}}");
                    break;
                }
                catch (SaliensApiException ex)
                {
                    if (ex.EResult == EResult.TimeIsOutOfSync && i < 5)
                    {
                        // Gradually decrease the delay until we no longer get issues on future requests
                        this.ReportScoreNetworkDelay -= TimeSpan.FromMilliseconds(10);
                        Shell.WriteLine($"{{warn}}Failed to submit score of {score.ToString("#,##0")}: Submitting too fast, giving it a second ({i + 1}/5)...");
                        await Task.Delay(1000);
                        continue;
                    }
                    else if (ex.EResult == EResult.Expired || ex.EResult == EResult.NoMatch)
                        Shell.WriteLine($"{{warn}}Failed to submit score of {score.ToString("#,##0")}: Zone already captured");
                    throw;
                }
            }
        }

        public async Task LeaveGame(string gameId)
        {
            await SaliensApi.LeaveGame(this.Token, gameId);
            if (this.JoinedPlanetId == gameId)
                this.JoinedPlanetId = null;
            else if (this.JoinedZone.GameId == gameId)
                this.JoinedZonePosition = null;
        }

        public void PrintActivePlanets()
        {
            var planets = this.PlanetDetails.OrderBy(p => p.State.Priority).Where(p => p.State.Running);

            Shell.WriteLine("Active planets:");
            foreach (var planet in planets)
            {
                Shell.WriteLine(planet.ToConsoleLine());
                if (this.JoinedPlanetId == planet.Id && this.JoinedZonePosition != null)
                {
                    var zone = planet.Zones.FirstOrDefault(z => z.ZonePosition == this.JoinedZonePosition);
                    if (zone != null)
                        Shell.WriteLine(zone.ToConsoleLine());
                }
            }

            // Get the next future planet, if available
            var lastPlanetIndex = this.PlanetDetails.ToList().FindIndex(p => p.Id == planets.Last().Id);
            var lastPlanet = this.PlanetDetails.Skip(lastPlanetIndex + 1).FirstOrDefault();
            if (lastPlanet != null)
            {
                Shell.WriteLine("Upcoming planets:");
                Shell.WriteLine(lastPlanet.ToConsoleLine());
            }
        }

        public void PrintPlayerInfo()
        {
            if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActivePlanet))
                Shell.WriteLine($"Active planet: {{planet}}{this.PlayerInfo.ActivePlanet}{{reset}} for {{value}}{this.PlayerInfo.TimeOnPlanet.ToString()}{{reset}}");
            if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActiveZonePosition))
                Shell.WriteLine($"Active zone: {{zone}}{this.PlayerInfo.ActiveZonePosition}{{reset}} for {{value}}{this.PlayerInfo.TimeInZone.TotalSeconds}s{{reset}}");
            Shell.WriteLine($"Level {{level}}{this.PlayerInfo.Level}{{reset}}: {{xp}}{long.Parse(this.PlayerInfo.Score).ToString("#,##0")}{{reset}}/{{reqxp}}{long.Parse(this.PlayerInfo.NextLevelScore).ToString("#,##0")}{{reset}}");
        }


        public void Dispose()
        {
            this.updatePlanetsTimer.Dispose();
            this.updatePlayerInfoTimer.Dispose();
        }
    }
}
