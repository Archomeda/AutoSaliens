using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api;
using AutoSaliens.Api.Models;
using AutoSaliens.Presence;
using AutoSaliens.Utils;

namespace AutoSaliens.Bot
{
    internal class SaliensBot : ISaliensBot, IDisposable
    {
        private const int roundTime = 120;
        private readonly Dictionary<Difficulty, int> pointsPerRound = new Dictionary<Difficulty, int>
        {
            { Difficulty.Low, 5 * roundTime },
            { Difficulty.Medium, 10 * roundTime },
            { Difficulty.High, 20 * roundTime }
        };

        private Thread botThread;
        private CancellationTokenSource cancelSource;
        private bool botStarted = false;
        private TimeSpan reportScoreNetworkDelay;
        private readonly double reportScoreNetworkDelayTolerance = 0.4;


        public event EventHandler BotActivated;
        public event EventHandler BotDeactivated;


        // Settings
        public bool EnableNetworkTolerance { get; set; }

        public int GameTime { get; set; }

        public ILogger Logger { get; set; }

        public string OverridePlanetId { get; set; }

        public BotStrategy Strategy { get; set; }

        public string Token { get; set; }


        // States
        public Planet ActivePlanet { get; private set; }

        public Zone ActiveZone { get; private set; }

        public DateTime ActiveZoneStartDate { get; private set; }

        public bool HasActivePlanet => this.ActivePlanet != null;

        public bool HasActiveZone => this.ActiveZone != null;

        public bool IsBotActive { get; private set; }

        public PlayerInfoResponse PlayerInfo { get; private set; }

        public BotUpdateTrigger PresenceUpdateTrigger { get; private set; } = new BotUpdateTrigger();

        public BotState State { get; private set; }


        public void Start()
        {
            if (this.botStarted)
                return;
            this.botStarted = true;

            this.State = BotState.Resume;

            this.cancelSource = new CancellationTokenSource();
            this.botThread = new Thread(async () =>
            {
                this.IsBotActive = true;
                await this.Loop();
            });
            this.botThread.Start();
        }

        public void Stop()
        {
            if (!this.botStarted)
                return;
            this.botStarted = false;

            this.IsBotActive = false;
            this.cancelSource?.Cancel();
        }

        private async Task Loop()
        {
            this.BotActivated?.Invoke(this, null);
            while (!this.cancelSource.Token.IsCancellationRequested)
            {
                try
                {
                    this.Logger?.LogMessage($"{{verb}}State: {this.State}");
                    switch (this.State)
                    {
                        case BotState.Idle:
                            await this.DoIdle();
                            break;

                        case BotState.Resume:
                            await this.DoResume();
                            break;

                        case BotState.OnPlanet:
                            await this.DoOnPlanet();
                            break;

                        case BotState.InZone:
                            await this.DoInZone();
                            break;

                        case BotState.InZoneEnd:
                            await this.DoInZoneEnd();
                            break;

                        case BotState.Invalid:
                        default:
                            await this.DoInvalid();
                            break;
                    }
                }
                catch (OperationCanceledException) { }
                catch (SaliensApiException ex)
                {
                    switch (ex.EResult)
                    {
                        case EResult.Expired:
                        case EResult.NoMatch:
                            // Update states
                            this.Logger?.LogMessage($"{{action}}Updating states...");
                            await this.GetPlayerInfo(true);
                            await SaliensApi.GetPlanetsWithZonesAsync(true, true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    this.Logger?.LogException(ex);
                    this.State = BotState.Invalid;
                }
            }
            this.BotDeactivated?.Invoke(this, null);
        }


        private async Task DoIdle()
        {
            // We are not on a planet nor in a zone

            // Get our most wanted planets
            var planets = await FindMostWantedPlanets();

            // Fly to the first planet
            await this.JoinPlanet(planets[0].Id);
        }

        private async Task DoResume()
        {
            // We are resuming

            // Update player state
            await this.GetPlayerInfo();
        }

        private async Task DoOnPlanet()
        {
            // We are on a planet

            // Check if we are on our most wanted planet
            var planets = await FindMostWantedPlanets();
            if (this.ActivePlanet.Id != planets[0].Id)
            {
                await this.LeaveGame(this.ActivePlanet.Id);
                return;
            }

            // Get our most wanted zones
            var zones = await FindMostWantedZones();

            // Join the first zone
            await this.JoinZone(zones[0].ZonePosition);
        }

        private async Task DoInZone()
        {
            // We are in a zone

            // We have to wait until time runs out
            await this.WaitForActiveZoneToFinish();
        }

        private async Task DoInZoneEnd()
        {
            // We are in a zone, at ending

            // Report score
            var score = this.CalculatePoints(this.ActiveZone?.Difficulty ?? Difficulty.Low, roundTime);
            await this.ReportScore(score);
        }

        private async Task DoInvalid()
        {
            // Invalid state, reset

            this.Logger?.LogMessage("{action}Attempting to restart in 10 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(10), this.cancelSource.Token);
            await this.GetPlayerInfo();
            if (this.HasActiveZone)
                await this.LeaveGame(this.ActiveZone.GameId);
            if (this.HasActivePlanet)
                await this.LeaveGame(this.ActivePlanet.Id);
        }


        private int CalculatePoints(Difficulty difficulty, int seconds)
        {
            //! This has some stupid fallback to at least try to get some points, might fail
            return this.pointsPerRound.ContainsKey(difficulty) ?
                this.pointsPerRound[difficulty] :
                this.pointsPerRound[Difficulty.High];
        }

        private async Task<List<Planet>> FindMostWantedPlanets()
        {
            var mostWantedPlanets = new List<Planet>();
            var activePlanets = (await SaliensApi.GetPlanetsWithZonesAsync(true)).Values;

            // Overridden planet
            if (!string.IsNullOrWhiteSpace(this.OverridePlanetId))
            {
                try
                {
                    var planet = await SaliensApi.GetPlanetAsync(this.OverridePlanetId);
                    if (planet.State.Running)
                        mostWantedPlanets.Add(planet);
                }
                catch (SaliensApiException ex)
                {
                    switch (ex.EResult)
                    {
                        case EResult.InvalidParam:
                        case EResult.Expired:
                        case EResult.NoMatch:
                        case EResult.ValueOutOfRange:
                            Program.Settings.OverridePlanetId.Value = null;
                            break;

                        default:
                            throw;
                    }
                }
            }

            // Force current joined planet if FocusCurrentPlanet is selected
            if (this.Strategy.HasFlag(BotStrategy.FocusCurrentPlanet) && this.HasActivePlanet)
            {
                var planet = await SaliensApi.GetPlanetAsync(this.ActivePlanet.Id);
                if (planet.State.Running)
                    mostWantedPlanets.Add(planet);
            }

            if (this.Strategy.HasFlag(BotStrategy.FocusRandomPlanet))
            {
                mostWantedPlanets.AddRange(activePlanets
                    .Where(p => !mostWantedPlanets.Any(mp => mp.Id == p.Id))
                    .ToList()
                    .Shuffle());
            }
            else
            {
                // As of 26th June, the planet difficulty is always low, so let's skip it for now
                var planets = activePlanets.OrderBy(p => 0);
                if (this.Strategy.HasFlag(BotStrategy.MostDifficultPlanetsFirst))
                {
                    planets = planets
                        //.ThenByDescending(p => (int)p.State.Difficulty)
                        .ThenByDescending(p => p.MaxFreeZonesDifficulty)
                        .ThenByDescending(p => p.WeightedAverageFreeZonesDifficulty);
                }
                else if (this.Strategy.HasFlag(BotStrategy.LeastDifficultPlanetsFirst))
                {
                    planets = planets
                        //.ThenBy(p => (int)p.State.Difficulty)
                        .ThenBy(p => p.MaxFreeZonesDifficulty)
                        .ThenBy(p => p.WeightedAverageFreeZonesDifficulty);
                }
                if (this.Strategy.HasFlag(BotStrategy.MostCompletedPlanetsFirst))
                    planets = planets.ThenByDescending(p => p.State.CaptureProgress);
                else if (this.Strategy.HasFlag(BotStrategy.LeastCompletedPlanetsFirst))
                    planets = planets.ThenBy(p => p.State.CaptureProgress);

                if (this.Strategy.HasFlag(BotStrategy.TopDown))
                    planets = planets.ThenBy(p => p.Id);
                else if (this.Strategy.HasFlag(BotStrategy.BottomUp))
                    planets = planets.ThenByDescending(p => p.Id);

                mostWantedPlanets.AddRange(planets);
            }

            return mostWantedPlanets;
        }

        private async Task<List<Zone>> FindMostWantedZones()
        {
            var activeZones = (await SaliensApi.GetPlanetAsync(this.ActivePlanet.Id)).Zones.Where(z => !z.Captured);

            var zones = activeZones.OrderBy(p => 0);
            if (this.Strategy.HasFlag(BotStrategy.MostDifficultZonesFirst))
                zones = zones.ThenByDescending(z => z.Difficulty);
            else if (this.Strategy.HasFlag(BotStrategy.LeastDifficultZonesFirst))
                zones = zones.ThenBy(z => z.Difficulty);
            if (this.Strategy.HasFlag(BotStrategy.MostCompletedZonesFirst))
                zones = zones.ThenByDescending(z => z.CaptureProgress);
            else if (this.Strategy.HasFlag(BotStrategy.LeastCompletedZonesFirst))
                zones = zones.ThenBy(z => z.CaptureProgress);

            if (this.Strategy.HasFlag(BotStrategy.TopDown))
                zones = zones.ThenBy(z => z.ZonePosition);
            else if (this.Strategy.HasFlag(BotStrategy.BottomUp))
                zones = zones.ThenByDescending(z => z.ZonePosition);

            return zones.ToList();
        }

        private async Task WaitForActiveZoneToFinish()
        {
            var targetTime = this.ActiveZoneStartDate + TimeSpan.FromSeconds(this.GameTime);
            if (this.EnableNetworkTolerance)
                targetTime -= TimeSpan.FromMilliseconds(this.reportScoreNetworkDelay.TotalMilliseconds * (1 - this.reportScoreNetworkDelayTolerance));

            var timeLeft = targetTime - DateTime.Now;
            if (timeLeft.TotalSeconds > 0)
            {
                this.Logger?.LogMessage($"{{action}}Waiting for zone to finish in {{value}}{timeLeft.TotalSeconds.ToString("#.###")} seconds{{action}} (at {{value}}{targetTime.ToString("HH:mm:ss.fff")}{{action}})...");
                var tasks = new List<Task>();
                if (timeLeft.TotalSeconds > 10)
                {
                    // Schedule to print planets 5 seconds before the zone is finished
                    tasks.Add(Task.Delay(timeLeft - TimeSpan.FromSeconds(5), this.cancelSource.Token)
                        .ContinueWith(t => this.PrintActivePlanets()));
                }
                tasks.Add(Task.Delay(timeLeft, this.cancelSource.Token));
                await Task.WhenAll(tasks);
            }

            // States
            this.State = BotState.InZoneEnd;
        }


        public async Task GetPlayerInfo(bool forceLive = false)
        {
            if (string.IsNullOrEmpty(this.Token))
                return;

            this.PlayerInfo = await SaliensApi.GetPlayerInfoAsync(this.Token, forceLive);
            this.State = BotState.Idle;

            if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActivePlanet))
            {
                this.ActivePlanet = await SaliensApi.GetPlanetAsync(this.PlayerInfo.ActivePlanet, forceLive);
                this.State = BotState.OnPlanet;
            }

            if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActiveZonePosition) &&
                int.TryParse(this.PlayerInfo.ActiveZonePosition, out int zonePosition))
            {
                this.ActiveZone = (await SaliensApi.GetPlanetAsync(this.PlayerInfo.ActivePlanet, forceLive)).Zones[zonePosition];
                this.ActiveZoneStartDate = DateTime.Now - this.PlayerInfo.TimeInZone;
                this.State = BotState.InZone;
            }

            this.PresenceUpdateTrigger.SetSaliensPlayerState(this.PlayerInfo);
        }

        private async Task JoinPlanet(string planetId)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var planet = await SaliensApi.GetPlanetAsync(planetId);
                    this.Logger?.LogMessage($"{{action}}Joining planet {{planet}}{planetId} ({planet.State.Name}){{action}}...");
                    await SaliensApi.JoinPlanetAsync(this.Token, planetId);

                    // States
                    this.ActivePlanet = planet;
                    this.PlayerInfo.ActivePlanet = planetId;
                    this.State = BotState.OnPlanet;

                    this.PresenceUpdateTrigger.SetSaliensPlayerState(this.PlayerInfo);

                    return;
                }
                catch (SaliensApiException ex)
                {
                    switch (ex.EResult)
                    {
                        case EResult.Fail:
                        case EResult.Busy:
                        case EResult.RateLimitExceeded:
                            this.Logger?.LogMessage($"{{warn}}Failed to join planet: {ex.Message} - Giving it a few seconds ({i + 1}/5)...");
                            await Task.Delay(2000);
                            continue;

                        case EResult.Expired:
                        case EResult.NoMatch:
                            this.Logger?.LogMessage($"{{warn}}Failed to join planet: {ex.Message}");
                            ResetState();
                            throw;

                        default:
                            ResetState();
                            throw;
                    }
                }
            }

            // States, only set when failed
            ResetState();
            void ResetState()
            {
                this.State = BotState.Idle;
            }
        }

        private Task JoinZone(string zonePosition)
        {
            if (!int.TryParse(zonePosition, out int zonePositionInt))
                throw new ArgumentException("Not an integer", nameof(zonePosition));
            return this.JoinZone(zonePositionInt);
        }

        private async Task JoinZone(int zonePosition)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    this.Logger?.LogMessage($"{{action}}Joining {{zone}}zone {zonePosition}{{action}}...");
                    await SaliensApi.JoinZoneAsync(this.Token, zonePosition);

                    // States
                    this.ActiveZone = this.ActivePlanet.Zones[zonePosition];
                    this.ActiveZoneStartDate = DateTime.Now;
                    this.PlayerInfo.ActiveZoneGame = this.ActiveZone.GameId;
                    this.PlayerInfo.ActiveZonePosition = zonePosition.ToString();
                    this.State = BotState.InZone;

                    this.PresenceUpdateTrigger.SetSaliensPlayerState(this.PlayerInfo);

                    return;
                }
                catch (SaliensApiException ex)
                {
                    switch (ex.EResult)
                    {
                        case EResult.Fail:
                        case EResult.Busy:
                        case EResult.RateLimitExceeded:
                            this.Logger?.LogMessage($"{{warn}}Failed to join zone: {ex.Message} - Giving it a few seconds ({i + 1}/5)...");
                            await Task.Delay(2000);
                            continue;

                        case EResult.Expired:
                        case EResult.NoMatch:
                            this.Logger?.LogMessage($"{{warn}}Failed to join zone: {ex.Message}");
                            ResetState();
                            throw;

                        default:
                            ResetState();
                            throw;
                    }
                }
            }

            // States, only set when failed
            ResetState();
            void ResetState()
            {
                this.State = BotState.OnPlanet;
            }
        }

        public async Task ReportScore(int score)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    this.Logger?.LogMessage($"{{action}}Reporting score {{xp}}{score.ToString("#,##0")}{{action}}...");
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var response = await SaliensApi.ReportScoreAsync(this.Token, score);
                    stopwatch.Stop();
                    // Only change the network delay if the last delay was lower
                    // Don't want to be too eager for an occasional spike
                    if (this.reportScoreNetworkDelay.TotalMilliseconds == 0 || stopwatch.Elapsed < this.reportScoreNetworkDelay)
                        this.reportScoreNetworkDelay = stopwatch.Elapsed;

                    if (!string.IsNullOrWhiteSpace(response.NewScore))
                        this.Logger?.LogMessage($"XP: {{oldxp}}{long.Parse(response.OldScore).ToString("#,##0")}{{reset}} -> {{xp}}{long.Parse(response.NewScore).ToString("#,##0")}{{reset}} (next level at {{reqxp}}{long.Parse(response.NextLevelScore).ToString("#,##0")}{{reset}})");
                    if (response.NewLevel != response.OldLevel)
                        this.Logger?.LogMessage($"New level: {{oldlevel}}{response.OldLevel}{{reset}} -> {{level}}{response.NewLevel}{{reset}}");

                    // States
                    this.PlayerInfo.Score = response.NewScore;
                    this.PlayerInfo.Level = response.NewLevel;
                    this.PlayerInfo.NextLevelScore = response.NextLevelScore;
                    this.ActiveZone = null;
                    this.PlayerInfo.ActiveZoneGame = null;
                    this.PlayerInfo.ActiveZonePosition = null;
                    this.State = BotState.OnPlanet;

                    return;
                }
                catch (SaliensApiException ex)
                {
                    switch (ex.EResult)
                    {
                        case EResult.Fail:
                        case EResult.Busy:
                        case EResult.RateLimitExceeded:
                        case EResult.TimeIsOutOfSync:
                            if (ex.EResult == EResult.TimeIsOutOfSync)
                            {
                                // Decrease the delay with a small amount
                                this.reportScoreNetworkDelay -= TimeSpan.FromMilliseconds(25);
                            }
                            this.Logger?.LogMessage($"{{warn}}Failed to submit score: {ex.Message} - Giving it a few seconds ({i + 1}/5)...");
                            await Task.Delay(2000);
                            continue;

                        case EResult.Expired:
                        case EResult.NoMatch:
                            this.Logger?.LogMessage($"{{warn}}Failed to submit score: {ex.Message}");
                            ResetState();
                            throw;

                        default:
                            ResetState();
                            throw;
                    }
                }
            }

            // States, only set when failed
            ResetState();
            void ResetState()
            {
                this.ActiveZone = null;
                this.PlayerInfo.ActiveZoneGame = null;
                this.PlayerInfo.ActiveZonePosition = null;
                this.State = BotState.OnPlanet;
            }
        }

        private async Task LeaveGame(string gameId)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await SaliensApi.LeaveGameAsync(this.Token, gameId);
                    if (this.HasActivePlanet && this.ActivePlanet.Id == gameId)
                    {
                        this.Logger?.LogMessage($"{{action}}Leaving planet {{planet}}{gameId} ({this.ActivePlanet.State.Name}){{action}}...");

                        // States
                        this.ActivePlanet = null;
                        this.PlayerInfo.ActivePlanet = null;
                        this.State = BotState.Idle;
                    }
                    else if (this.HasActiveZone && this.ActiveZone.GameId == gameId)
                    {
                        this.Logger?.LogMessage($"{{action}}Leaving zone {{zone}}{this.ActiveZone.ZonePosition} ({gameId}){{action}}...");

                        // States
                        this.ActiveZone = null;
                        this.PlayerInfo.ActiveZoneGame = null;
                        this.PlayerInfo.ActiveZonePosition = null;
                        this.State = BotState.OnPlanet;
                    }

                    this.PresenceUpdateTrigger.SetSaliensPlayerState(this.PlayerInfo);

                    return;
                }
                catch (SaliensApiException ex)
                {
                    switch (ex.EResult)
                    {
                        case EResult.Fail:
                        case EResult.Busy:
                        case EResult.RateLimitExceeded:
                            this.Logger?.LogMessage($"{{warn}}Failed to leave game: {ex.Message} - Giving it a few seconds ({i + 1}/5)...");
                            await Task.Delay(2000);
                            continue;

                        case EResult.Expired:
                        case EResult.NoMatch:
                            this.Logger?.LogMessage($"{{warn}}Failed to join planet: {ex.Message}");
                            ResetState();
                            throw;

                        default:
                            ResetState();
                            throw;
                    }
                }
            }

            // States, only set when failed
            ResetState();
            void ResetState()
            {
                this.State = BotState.Invalid; // Just reset
            }
        }


        private async Task PrintActivePlanets()
        {
            var planets = (await SaliensApi.GetPlanetsAsync()).Values
                .OrderBy(p => p.State.Priority);

            var activePlanets = await Task.WhenAll(planets
                .Where(p => p.State.Running)
                .Select(p => SaliensApi.GetPlanetAsync(p.Id)));

            this.Logger?.LogMessage("Active planets:");
            foreach (var planet in activePlanets)
            {
                this.Logger?.LogMessage(planet.ToConsoleLine());
                if (this.ActivePlanet?.Id == planet.Id && this.HasActiveZone)
                    this.Logger?.LogMessage(this.ActiveZone.ToConsoleLine());
            }

            // Get the next 2 future planets, if available
            var lastPlanetIndex = planets.ToList().FindIndex(p => p.Id == activePlanets.Last().Id);
            var lastPlanets = (await Task.WhenAll(planets.Skip(lastPlanetIndex + 1)
                .Take(2)
                .Select(p => SaliensApi.GetPlanetAsync(p.Id))));
            if (lastPlanets.Length > 0)
            {
                this.Logger?.LogMessage("Upcoming planets:");
                foreach (var planet in lastPlanets)
                    this.Logger?.LogMessage(planet.ToConsoleLine());
            }
        }


        public void Dispose()
        {
            this.cancelSource.Dispose();
        }
    }
}
