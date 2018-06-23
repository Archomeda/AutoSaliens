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
            this.updatePlanetsTimer.Elapsed += async (s, a) => await this.UpdatePlanets(this.cancellationTokenSource.Token);
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
            get => this.PlayerInfo != null ? (int?)int.Parse(this.PlayerInfo.ActiveZonePosition) : null;
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
                    await this.UpdatePlanets(this.cancellationTokenSource.Token);
                    this.AutomationActive = true;
                    await this.Loop();
                }).Start();
            });
        }

        public void Stop()
        {
            this.AutomationActive = false;
            this.cancellationTokenSource.Cancel();
            this.updatePlanetsTimer.Stop();
        }


        private async Task Loop()
        {
            while (this.AutomationActive)
            {
                try
                {
                    int score = 0;

                    // Refresh our player info
                    await this.UpdatePlayerInfo(this.cancellationTokenSource.Token);

                    // If the player is already joined in a zone, wait for it to end and submit the score afterwards
                    if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActiveZonePosition))
                    {
                        var timeLeft = this.GameTime - (int)this.PlayerInfo.TimeInZone.TotalSeconds;
                        if (timeLeft < -120)
                        {
                            // Assume we're stuck somehow after 2 minutes, leave game and restart
                            Shell.WriteLine($"We are stuck, leaving and restarting...");
                            await this.LeaveGame(this.JoinedPlanetId, this.cancellationTokenSource.Token);
                            continue;
                        }
                        if (timeLeft > 0)
                            await Task.Delay(timeLeft * 1000, this.cancellationTokenSource.Token);
                        if (this.cancellationTokenSource.Token.IsCancellationRequested)
                            return;

                        score = this.CalculatePoints(this.JoinedZone?.Difficulty ?? Difficulty.Low, maxGameTime);
                        await this.ReportScore(score, this.cancellationTokenSource.Token);
                    }

                    // Refresh our joined planet
                    await this.UpdateJoinedPlanet(this.cancellationTokenSource.Token);

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
                        Shell.WriteLine($"Choosing {planet.State.Name} as new planet");
                        Shell.WriteLines(planet.ToString().Split('\n'));
                        await this.JoinPlanet(planet.Id, this.cancellationTokenSource.Token);
                        // Force wait for a second
                        await Task.Delay(1000);
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
                    Shell.WriteLine($"Choosing {zone.ZonePosition} as new zone");
                    Shell.WriteLines(zone.ToString().Split('\n'));
                    await this.JoinZone(zone.ZonePosition, this.cancellationTokenSource.Token);

                    // "Play"
                    Shell.WriteLine($"Waiting for {this.GameTime} seconds...");
                    await Task.Delay(this.GameTime * 1000, this.cancellationTokenSource.Token);
                    if (this.cancellationTokenSource.Token.IsCancellationRequested)
                        return;

                    score = this.CalculatePoints(this.JoinedZone.Difficulty, maxGameTime);
                    await this.ReportScore(score, this.cancellationTokenSource.Token);
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
                        await Task.Delay(5000, this.cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Fine...
                    }
                }
            }
        }


        public int CalculatePoints(Difficulty difficulty, int seconds)
        {
            //! This has some stupid fallback to at least try to get some points, might fail
            return this.pointsPerSecond.ContainsKey(difficulty) ?
                this.pointsPerSecond[difficulty] * seconds :
                this.pointsPerSecond[Difficulty.High] * seconds;
        }


        public async Task UpdatePlanets(CancellationToken cancellationToken)
        {
            try
            {
                Shell.WriteLine("Updating global planet list...");
                this.PlanetDetails = await SaliensApi.GetPlanets(cancellationToken);
                Shell.WriteLine("Global planets list updated");

                var details = await Task.WhenAll(this.PlanetDetails.Where(p => p.State.Running).Select(p => SaliensApi.GetPlanet(p.Id, cancellationToken)));
                Shell.WriteLine("Current active planets:");
                foreach (var planet in details)
                {
                    // Replace the planet because this instance has more information (e.g. zones)
                    var i = this.PlanetDetails.FindIndex(p => p.Id == planet.Id);
                    this.PlanetDetails[i] = planet;
                }
                Shell.WriteLines(this.PlanetsToString(this.PlanetDetails.Where(p => p.State.Running), true).Split('\n'));
            }
            catch (Exception ex)
            {
                Shell.WriteLine(Shell.FormatExceptionOutput(ex));
            }
        }

        public async Task UpdateJoinedPlanet(CancellationToken cancellationToken)
        {
            if (this.JoinedPlanet == null)
                return;

            Shell.WriteLine("Updating joined planet...");
            var index = this.PlanetDetails.FindIndex(p => p.Id == this.JoinedPlanetId);
            this.PlanetDetails[index] = await SaliensApi.GetPlanet(this.JoinedPlanetId, cancellationToken);
            Shell.WriteLine("Joined planet updated");
        }

        public async Task UpdatePlayerInfo(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(this.Token))
                return;

            try
            {
                Shell.WriteLine("Updating player info...");
                this.PlayerInfo = await SaliensApi.GetPlayerInfo(this.Token, cancellationToken);
                Shell.WriteLine("Player info updated");
                if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActivePlanet)) {
                    if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActiveZonePosition))
                        Shell.WriteLine($"Active on: planet {this.PlayerInfo.ActivePlanet}, zone {this.PlayerInfo.ActiveZonePosition} for {this.PlayerInfo.TimeOnPlanet.ToString()}, {this.PlayerInfo.TimeInZone.TotalSeconds}s");
                    else
                        Shell.WriteLine($"Active on: planet {this.PlayerInfo.ActivePlanet} for {this.PlayerInfo.TimeOnPlanet.ToString()}");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActiveZonePosition))
                        Shell.WriteLine($"Active on: unknown planet, zone {this.PlayerInfo.ActiveZonePosition} for {this.PlayerInfo.TimeOnPlanet.ToString()}, {this.PlayerInfo.TimeInZone.TotalSeconds}s");
                    else
                        Shell.WriteLine($"Not active on any planet and/or zone");
                }

                Shell.WriteLine($"Level: {this.PlayerInfo.Level} ({long.Parse(this.PlayerInfo.Score).ToString("#,##0")}/{long.Parse(this.PlayerInfo.NextLevelScore).ToString("#,##0")})");
            }
            catch (Exception ex)
            {
                Shell.WriteLine(Shell.FormatExceptionOutput(ex));
            }
        }

        public async Task JoinPlanet(string planetId, CancellationToken cancellationToken)
        {
            Shell.WriteLine($"Joining planet...");
            await SaliensApi.JoinPlanet(this.Token, planetId, this.cancellationTokenSource.Token);
            Shell.WriteLine($"Joined {this.PlanetDetails.FirstOrDefault(p => p.Id == planetId)?.State.Name ?? $"planet {planetId}"}");
            this.JoinedPlanetId = planetId;
        }

        public async Task JoinZone(int zonePosition, CancellationToken cancellationToken)
        {
            Shell.WriteLine($"Joining zone...");
            await SaliensApi.JoinZone(this.Token, zonePosition, this.cancellationTokenSource.Token);
            Shell.WriteLine($"Joined zone {zonePosition}");
            this.JoinedZonePosition = zonePosition;
        }

        public async Task ReportScore(int score, CancellationToken cancellationToken)
        {
            Shell.WriteLine($"Submitting score: {score.ToString("#,##0")}...");
            var response = await SaliensApi.ReportScore(this.Token, score, this.cancellationTokenSource.Token);
            Shell.WriteLine("Score submitted");
            if (!string.IsNullOrWhiteSpace(response.NewScore))
                Shell.WriteLine($"XP progression: {long.Parse(response.OldScore).ToString("#,##0")} -> {long.Parse(response.NewScore).ToString("#,##0")} (next level at {long.Parse(response.NextLevelScore).ToString("#,##0")})");
            if (response.NewLevel != response.OldLevel)
                Shell.WriteLine($"New level: {response.OldLevel} -> {response.NewLevel}");
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
