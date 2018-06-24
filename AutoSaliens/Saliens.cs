using System;
using System.Collections.Generic;
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


        public Saliens()
        {
            this.updatePlanetsTimer.Elapsed += async (s, a) => await this.UpdatePlanets(false, this.cancellationTokenSource.Token);
        }


        public bool AutomationActive { get; private set; }

        public int GameTime { get; set; } = 120;

        public AutomationStrategy Strategy { get; set; } =
            AutomationStrategy.TopDown |
            AutomationStrategy.MostCompletedPlanetsFirst |
            AutomationStrategy.MostCompletedZonesFirst |
            AutomationStrategy.MostDifficultPlanetsFirst |
            AutomationStrategy.MostDifficultZonesFirst;

        public string OverridePlanetId { get; set; }

        public string Token { get; set; }


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
        

        public Task Start()
        {
            return Task.Run(() =>
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                this.updatePlanetsTimer.Start();
                new Thread(async () =>
                {
                    // Initialization
                    await Task.WhenAll(
                        this.UpdatePlanets(false, this.cancellationTokenSource.Token),
                        this.UpdatePlayerInfo(this.cancellationTokenSource.Token)
                    );

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
        }


        private async Task Loop()
        {
            while (this.AutomationActive)
            {
                try
                {
                    // If the player is already joined in a zone, wait for it to end and submit the score afterwards
                    if (this.JoinedZonePosition != null)
                    {
                        await this.WaitForJoinedZoneToFinish();
                        var score = this.CalculatePoints(this.JoinedZone?.Difficulty ?? Difficulty.Low, maxGameTime);
                        await this.ReportScore(score, this.cancellationTokenSource.Token);
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
                            Shell.WriteLine($"{planetName} has been fully captured, leaving...");
                        await this.LeaveGame(this.JoinedPlanetId, this.cancellationTokenSource.Token);
                    }

                    // Fly to the new planet when it's different
                    if (planet.Id != this.JoinedPlanetId)
                    {
                        Shell.WriteLine($"Choosing new planet: {planet.State.Name}");
                        Shell.WriteLines(planet.ToShortString().Split('\n'));
                        await this.JoinPlanet(planet.Id, this.cancellationTokenSource.Token);
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
                    Shell.WriteLine($"Choosing zone: {zone.ZonePosition}");
                    Shell.WriteLines(zone.ToShortString().Split('\n'));
                    await this.JoinZone(zone.ZonePosition, this.cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Fine...
                }
                catch (Exception ex)
                {
                    Shell.WriteLine(Shell.FormatExceptionOutput(ex));

                    try
                    {
                        // Assume we're stuck leave game and restart
                        Shell.WriteLine($"Leaving and restarting...");
                        await Task.WhenAll(
                            this.LeaveGame(this.JoinedPlanetId, this.cancellationTokenSource.Token),
                            this.UpdatePlayerInfo(this.cancellationTokenSource.Token)
                        );
                    }
                    catch (Exception) { }

                    try { await Task.Delay(5000, this.cancellationTokenSource.Token); }
                    catch (OperationCanceledException) { }
                }
            }
        }


        private async Task WaitForJoinedZoneToFinish()
        {
            var timeLeft = this.JoinedZoneStart - DateTime.Now + TimeSpan.FromSeconds(this.GameTime);
            if (timeLeft.TotalSeconds > 0)
            {
                Shell.WriteLine($"Wait for zone to finish in {(int)timeLeft.TotalSeconds} seconds...");
                var tasks = new List<Task>();
                if (timeLeft.TotalSeconds > 5)
                {
                    // Schedule to update some data 5 seconds before the zone is finished
                    tasks.Add(Task.Delay(timeLeft - TimeSpan.FromSeconds(5)).ContinueWith(t =>
                        this.UpdatePlanets(true, this.cancellationTokenSource.Token)));
                }
                tasks.Add(Task.Delay(timeLeft, this.cancellationTokenSource.Token));
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


        public async Task UpdatePlanets(bool activeOnly, CancellationToken cancellationToken)
        {
            try
            {
                var newPlanets = await SaliensApi.GetPlanets(activeOnly, cancellationToken);
                if (!activeOnly)
                {
                    // Only bother updating when it's the full list,
                    // we override the active ones with a more detailed instance further ahead
                    this.PlanetDetails = newPlanets;
                    Shell.WriteLine("List of all planets updated");
                }
                else
                {
                    // Override all non-captured planet active properties to make sure we don't leave a few dangling
                    foreach (var planet in this.PlanetDetails)
                        if (!planet.State.Captured)
                            planet.State.Active = false;
                }

                var newDetails = await Task.WhenAll(newPlanets.Where(p => p.State.Running).Select(p => SaliensApi.GetPlanet(p.Id, cancellationToken)));
                foreach (var planet in newDetails)
                {
                    // Replace the planet because this instance has more information (e.g. zones)
                    var i = this.PlanetDetails.FindIndex(p => p.Id == planet.Id);
                    this.PlanetDetails[i] = planet;
                }
                Shell.WriteLine("Details of active planets updated");
                Shell.WriteLines(this.PlanetsToString(this.PlanetDetails.Where(p => p.State.Running), true).Split('\n'));
            }
            catch (Exception ex)
            {
                Shell.WriteLine(Shell.FormatExceptionOutput(ex));
            }
        }

        public async Task UpdatePlayerInfo(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(this.Token))
                return;

            try
            {
                this.PlayerInfo = await SaliensApi.GetPlayerInfo(this.Token, cancellationToken);
                Shell.WriteLine("Player info updated");

                if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActiveZonePosition))
                    this.JoinedZoneStart = DateTime.Now - this.PlayerInfo.TimeInZone;

                if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActivePlanet))
                    Shell.WriteLine($"  Active planet: {this.PlayerInfo.ActivePlanet} for {this.PlayerInfo.TimeOnPlanet.ToString()}");
                if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActiveZonePosition))
                    Shell.WriteLine($"  Active zone: {this.PlayerInfo.ActiveZonePosition} for {this.PlayerInfo.TimeInZone.TotalSeconds}s");
                Shell.WriteLine($"  Level: {this.PlayerInfo.Level} ({long.Parse(this.PlayerInfo.Score).ToString("#,##0")}/{long.Parse(this.PlayerInfo.NextLevelScore).ToString("#,##0")})");
            }
            catch (Exception ex)
            {
                Shell.WriteLine(Shell.FormatExceptionOutput(ex));
            }
        }

        public async Task JoinPlanet(string planetId, CancellationToken cancellationToken)
        {
            await SaliensApi.JoinPlanet(this.Token, planetId, this.cancellationTokenSource.Token);
            Shell.WriteLine($"Joined {this.PlanetDetails.FirstOrDefault(p => p.Id == planetId)?.State.Name ?? $"planet {planetId}"}");
            this.JoinedPlanetId = planetId;
        }

        public async Task JoinZone(int zonePosition, CancellationToken cancellationToken)
        {
            try
            {
                await SaliensApi.JoinZone(this.Token, zonePosition, this.cancellationTokenSource.Token);
                Shell.WriteLine($"Joined zone {zonePosition}");
                this.JoinedZonePosition = zonePosition;
                this.JoinedZoneStart = DateTime.Now;
            }
            catch (SaliensApiException ex)
            {
                if (ex.EResult == EResult.Expired || ex.EResult == EResult.NoMatch)
                {
                    Shell.WriteLine("Failed to join zone: Zone already captured");
                    this.JoinedZonePosition = null;
                }
                else
                    throw ex;
            }
        }

        public async Task ReportScore(int score, CancellationToken cancellationToken)
        {
            try
            {
                var response = await SaliensApi.ReportScore(this.Token, score, this.cancellationTokenSource.Token);
                Shell.WriteLine($"Score submitted: {score.ToString("#,##0")}");
                if (!string.IsNullOrWhiteSpace(response.NewScore))
                    Shell.WriteLine($"  XP progression: {long.Parse(response.OldScore).ToString("#,##0")} -> {long.Parse(response.NewScore).ToString("#,##0")} (next level at {long.Parse(response.NextLevelScore).ToString("#,##0")})");
                if (response.NewLevel != response.OldLevel)
                    Shell.WriteLine($"  New level: {response.OldLevel} -> {response.NewLevel}");
            }
            catch (SaliensApiException ex)
            {
                if (ex.EResult == EResult.Expired || ex.EResult == EResult.NoMatch)
                    Shell.WriteLine($"Failed to submit score of {score.ToString("#,##0")}: Zone already captured");
                else
                    throw ex;
            }

            this.JoinedZonePosition = null;
        }

        public async Task LeaveGame(string planetId, CancellationToken cancellationToken)
        {
            Shell.WriteLine($"Leaving planet...");
            await SaliensApi.LeaveGame(this.Token, planetId, this.cancellationTokenSource.Token);
            Shell.WriteLine($"Left {this.JoinedPlanet.State.Name}");
            this.JoinedZonePosition = null;
            this.JoinedPlanetId = null;
        }


        public string PlanetsToString(IEnumerable<Planet> planets, bool @short = false) =>
            string.Join("\n", planets.Select(p => @short ? p.ToShortString() : p.ToString()));

        public void Dispose()
        {
            this.updatePlanetsTimer.Dispose();
        }
    }
}
