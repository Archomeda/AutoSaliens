using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
        private const double reportScoreNetworkDelayTolerance = 0.4;

        private readonly TimeSpan blacklistGamesDuration = TimeSpan.FromMinutes(6);

        private const int reportBossDamageTaken = 0;
        private const int reportBossDamageDelay = 5000;
        private DateTime reportBossDamageHealUsed;
        private readonly TimeSpan reportBossDamageHealCooldown = TimeSpan.FromSeconds(120);

        public event EventHandler BotActivated;
        public event EventHandler BotDeactivated;


        // Settings
        public int BossDamageDealtMin { get; set; }

        public int BossDamageDealtMax { get; set; }

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

        public Dictionary<string, DateTime> BlacklistedGames { get; private set; } = new Dictionary<string, DateTime>();

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

                        case BotState.InBossZone:
                            await this.DoInBossZone();
                            break;

                        case BotState.ForcedZoneLeave:
                            await this.DoForcedZoneLeave();
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
                    // Update states
                    this.Logger?.LogMessage($"{{action}}Updating states...");
                    await this.UpdatePlayerInfo(TimeSpan.FromSeconds(5));
                    await SaliensApi.GetPlanetsWithZonesAsync(true, TimeSpan.FromSeconds(5));

                    switch (ex.EResult)
                    {
                        case EResult.Expired:
                        case EResult.NoMatch:
                            if (this.State == BotState.InZone || this.State == BotState.InZoneEnd)
                                this.State = BotState.ForcedZoneLeave;
                            break;

                        case EResult.InvalidState:
                        default:
                            this.State = BotState.Invalid;
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
            await this.UpdatePlayerInfo();
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

            // Join the first joinable zone
            var joined = false;
            for (int i = 0; i < zones.Count; i++)
            {
                try
                {
                    if (zones[i].RealDifficulty == RealDifficulty.Boss)
                        await this.JoinBossZone(zones[i].ZonePosition);
                    else
                        await this.JoinZone(zones[i].ZonePosition);
                    joined = true;
                    break;
                }
                catch (SaliensApiException ex)
                {
                    if (ex.EResult == EResult.Banned || zones[i].CaptureProgress == 0)
                    {
                        // Assume the zone is unjoinable, blacklist and go to the next one
                        var end = DateTime.Now + this.blacklistGamesDuration;
                        this.BlacklistedGames[zones[i].GameId] = end;
                        this.Logger?.LogMessage($"{{zone}}Zone {zones[i].ZonePosition}{{action}} on {{planet}}planet {planets[0].Id}{{action}} has been blacklisted until {end.ToString("HH:mm:ss")}");
                        continue; 
                    }
                    throw;
                }
            }

            if (!joined)
            {
                // We haven't joined a zone, blacklist and force another planet
                var end = DateTime.Now + this.blacklistGamesDuration;
                this.BlacklistedGames[planets[0].Id] = end;
                this.Logger?.LogMessage($"{{planet}}Planet {planets[0].Id}{{action}} has been blacklisted until {end.ToString("HH:mm:ss")}");
                await this.LeaveGame(planets[0].Id);
            }
            else
                await this.PrintActivePlanets();
        }

        private async Task DoInZone()
        {
            // We are in a zone

            // We have to wait until time runs out
            await this.WaitForActiveZoneToFinish();
        }

        private async Task DoInBossZone()
        {
            // We are in a boss zone

            // We have to do stuff until time runs out
            await this.PlayBossZoneUntilFinish();
        }

        private async Task DoInZoneEnd()
        {
            // We are in a zone, at ending

            // Report score
            var score = this.CalculatePoints(this.ActiveZone?.Difficulty ?? Difficulty.Low, roundTime);
            await this.ReportScore(score);
        }

        private async Task DoForcedZoneLeave()
        {
            // We are forced to leave zone

            // Make sure it happens
            if (!string.IsNullOrWhiteSpace(this.ActiveZone?.GameId))
                await this.LeaveGame(this.ActiveZone.GameId);
            else
                this.State = BotState.Invalid;
        }

        private async Task DoInvalid()
        {
            // Invalid state, reset

            this.Logger?.LogMessage("{action}Attempting to restart in 10 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(10), this.cancelSource.Token);
            await this.UpdatePlayerInfo();
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

        private int GetRandomBossDamage() =>
            new Random().Next(this.BossDamageDealtMin, this.BossDamageDealtMax);

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

                if (this.Strategy.HasFlag(BotStrategy.FocusBosses))
                    planets = planets.ThenByDescending(p => p.Zones.Any(z => z.BossActive) ? 1 : 0);
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

            // Filter out blacklisted games
            return mostWantedPlanets
                .Where(p => !(this.BlacklistedGames.ContainsKey(p.Id) && this.BlacklistedGames[p.Id] > DateTime.Now))
                .ToList();
        }

        private async Task<List<Zone>> FindMostWantedZones()
        {
            var activeZones = (await SaliensApi.GetPlanetAsync(this.ActivePlanet.Id)).Zones.Where(z => !z.Captured);

            // Filter out blacklisted games
            var zones = activeZones.OrderBy(p => 0);

            if (this.Strategy.HasFlag(BotStrategy.FocusBosses))
                zones = zones.ThenByDescending(z => z.BossActive ? 1 : 0);
            if (this.Strategy.HasFlag(BotStrategy.MostDifficultZonesFirst))
                zones = zones.ThenByDescending(z => z.RealDifficulty);
            else if (this.Strategy.HasFlag(BotStrategy.LeastDifficultZonesFirst))
                zones = zones.ThenBy(z => z.RealDifficulty);
            if (this.Strategy.HasFlag(BotStrategy.MostCompletedZonesFirst))
                zones = zones.ThenByDescending(z => z.CaptureProgress);
            else if (this.Strategy.HasFlag(BotStrategy.LeastCompletedZonesFirst))
                zones = zones.ThenBy(z => z.CaptureProgress);

            if (this.Strategy.HasFlag(BotStrategy.TopDown))
                zones = zones.ThenBy(z => z.ZonePosition);
            else if (this.Strategy.HasFlag(BotStrategy.BottomUp))
                zones = zones.ThenByDescending(z => z.ZonePosition);

            // Filter out blacklisted games
            return zones
                .Where(z => !string.IsNullOrWhiteSpace(z.GameId) && !(this.BlacklistedGames.ContainsKey(z.GameId) && this.BlacklistedGames[z.GameId] > DateTime.Now))
                .ToList();
        }

        private async Task WaitForActiveZoneToFinish()
        {
            var targetTime = this.ActiveZoneStartDate + TimeSpan.FromSeconds(this.GameTime);
            if (this.EnableNetworkTolerance)
                targetTime -= TimeSpan.FromMilliseconds(this.reportScoreNetworkDelay.TotalMilliseconds * (1 - reportScoreNetworkDelayTolerance));

            var timeLeft = targetTime - DateTime.Now;
            if (timeLeft.TotalSeconds > 0)
            {
                this.Logger?.LogMessage($"{{action}}Waiting for zone to finish in {{value}}{timeLeft.TotalSeconds.ToString("#.###")} seconds{{action}} (at {{value}}{targetTime.ToString("HH:mm:ss.fff")}{{action}})...");
                var tasks = new List<Task>();
                if (timeLeft.TotalSeconds > 10)
                {
                    // Schedule to print planets 5 seconds before the zone is finished
                    tasks.Add(Task.Delay(timeLeft - TimeSpan.FromSeconds(5), this.cancelSource.Token)
                        .ContinueWith(t => this.GetActivePlanets()));
                }
                tasks.Add(Task.Delay(timeLeft, this.cancelSource.Token));
                await Task.WhenAll(tasks);
            }

            // States
            this.State = BotState.InZoneEnd;
        }

        private async Task PlayBossZoneUntilFinish()
        {
            var startLevel = this.PlayerInfo.Level;
            long.TryParse(this.PlayerInfo.Score, out long startXp);

            // Loop until the boss is dead
            BossLevelState bossState = BossLevelState.WaitingForPlayers;
            this.reportBossDamageHealUsed = DateTime.Now + TimeSpan.FromSeconds(new Random().Next((int)this.reportBossDamageHealCooldown.TotalSeconds));
            while (bossState != BossLevelState.GameOver && bossState != BossLevelState.Error)
            {
                await Task.Delay(reportBossDamageDelay);
                var useHeal = this.reportBossDamageHealUsed < DateTime.Now;
                if (useHeal)
                    this.reportBossDamageHealUsed = DateTime.Now + this.reportBossDamageHealCooldown;

                bossState = await this.ReportBossDamage(startLevel, startXp, useHeal, this.GetRandomBossDamage(), reportBossDamageTaken);
            }

            if (long.TryParse(this.PlayerInfo.Score, out long score))
                this.Logger?.LogMessage($"{{xp}}{(score - startXp).ToString("#,##0")} XP{{reset}} gained: {{oldxp}}{startXp.ToString("#,##0")}{{reset}} -> {{xp}}{score.ToString("#,##0")}");
            if (this.PlayerInfo.Level > startLevel)
                this.Logger?.LogMessage($"New level: {{oldlevel}}{startLevel}{{reset}} -> {{level}}{this.PlayerInfo.Level}{{reset}}");

            // States
            this.ActiveZone = null;
            this.PlayerInfo.ActiveBossGame = null;
            this.PlayerInfo.ActiveZonePosition = null;
            this.State = bossState == BossLevelState.GameOver ? BotState.OnPlanet : BotState.Invalid;
        }


        private async Task UpdatePlayerInfo(TimeSpan? forceCacheExpiryTime = null)
        {
            if (string.IsNullOrEmpty(this.Token))
                return;

            this.PlayerInfo = await SaliensApi.GetPlayerInfoAsync(this.Token, forceCacheExpiryTime);
            this.State = BotState.Idle;

            if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActivePlanet))
            {
                this.ActivePlanet = await SaliensApi.GetPlanetAsync(this.PlayerInfo.ActivePlanet, forceCacheExpiryTime);
                this.State = BotState.OnPlanet;

                if (int.TryParse(this.PlayerInfo.ActiveZonePosition, out int zonePosition))
                {
                    this.ActiveZone = this.ActivePlanet.Zones[zonePosition];
                    this.ActiveZoneStartDate = DateTime.Now - this.PlayerInfo.TimeInZone;
                    this.State = BotState.InZone;
                }

                if (!string.IsNullOrWhiteSpace(this.PlayerInfo.ActiveBossGame))
                {
                    this.ActiveZone = this.ActivePlanet.Zones.FirstOrDefault(z => z.GameId == this.PlayerInfo.ActiveBossGame);
                    this.ActiveZoneStartDate = DateTime.Now - this.PlayerInfo.TimeInZone;
                    this.State = BotState.InBossZone;
                }
            }

            this.PresenceUpdateTrigger.SetSaliensPlayerState(this.PlayerInfo);
        }

        private async Task<List<Planet>> GetActivePlanets()
        {
            var planets = (await SaliensApi.GetPlanetsAsync()).Values
                .OrderBy(p => p.State.Priority);

            var activePlanets = await Task.WhenAll(planets
                .Where(p => p.State.Running)
                .Select(p => SaliensApi.GetPlanetAsync(p.Id)));

            // Get the next 2 future planets, if available
            var lastPlanetIndex = planets.ToList().FindIndex(p => p.Id == activePlanets.Last().Id);
            var lastPlanets = (await Task.WhenAll(planets.Skip(lastPlanetIndex + 1)
                .Take(2)
                .Select(p => SaliensApi.GetPlanetAsync(p.Id))));

            return activePlanets.Concat(lastPlanets).ToList();
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
                        default:
                            this.Logger?.LogMessage($"{{warn}}Failed to join planet: {ex.Message}");
                            ResetState();
                            throw;
                    }
                }
                catch (WebException ex)
                {
                    this.Logger?.LogMessage($"{{warn}}Failed to join planet: {ex.Message} - Giving it a few seconds ({i + 1}/5)...");
                    await Task.Delay(2000);
                    continue;
                }
            }

            // States, only set when failed
            ResetState();
            void ResetState()
            {
                this.State = BotState.Idle;
            }
        }

        private async Task JoinZone(int zonePosition)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    this.Logger?.LogMessage($"{{action}}Joining {{zone}}zone {zonePosition}{{action}}...");
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    await SaliensApi.JoinZoneAsync(this.Token, zonePosition);
                    stopwatch.Stop();

                    var startDate = DateTime.Now;

                    // If the request took too long, resynchronize the start date
                    if (stopwatch.Elapsed > TimeSpan.FromSeconds(1))
                    {
                        var playerInfo = await SaliensApi.GetPlayerInfoAsync(this.Token, TimeSpan.FromSeconds(0));
                        var diff = (startDate - (DateTime.Now - playerInfo.TimeInZone));
                        if (diff > TimeSpan.FromSeconds(0))
                        {
                            this.Logger?.LogMessage($"{{action}}Recalibrated zone join time with {{value}}{diff.Negate().TotalSeconds.ToString("0.###")} seconds");
                            startDate = DateTime.Now - playerInfo.TimeInZone;
                        }
                    }

                    // States
                    this.ActiveZone = this.ActivePlanet.Zones[zonePosition];
                    this.ActiveZoneStartDate = startDate;
                    this.PlayerInfo.ActiveZoneGame = this.ActiveZone.GameId;
                    this.PlayerInfo.ActiveZonePosition = zonePosition.ToString();
                    this.PlayerInfo.TimeInZone = TimeSpan.FromSeconds(0);
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
                        default:
                            this.Logger?.LogMessage($"{{warn}}Failed to join zone: {ex.Message}");
                            ResetState();
                            throw;
                    }
                }
                catch (WebException ex)
                {
                    this.Logger?.LogMessage($"{{warn}}Failed to join zone: {ex.Message} - Giving it a few seconds ({i + 1}/5)...");
                    await Task.Delay(2000);
                    continue;
                }
            }

            // States, only set when failed
            ResetState();
            void ResetState()
            {
                this.State = BotState.OnPlanet;
            }
        }

        private async Task JoinBossZone(int zonePosition)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    this.Logger?.LogMessage($"{{action}}Joining {{zone}}BOSS zone {zonePosition}{{action}}...");
                    await SaliensApi.JoinBossZoneAsync(this.Token, zonePosition);

                    // States
                    this.ActiveZone = this.ActivePlanet.Zones[zonePosition];
                    this.ActiveZoneStartDate = DateTime.Now;
                    this.PlayerInfo.ActiveZoneGame = null;
                    this.PlayerInfo.ActiveBossGame = this.ActiveZone.GameId;
                    this.PlayerInfo.ActiveZonePosition = zonePosition.ToString();
                    this.PlayerInfo.TimeInZone = TimeSpan.FromSeconds(0);
                    this.State = BotState.InBossZone;

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
                            this.Logger?.LogMessage($"{{warn}}Failed to join boss zone: {ex.Message} - Giving it a few seconds ({i + 1}/5)...");
                            await Task.Delay(2000);
                            continue;

                        case EResult.Expired:
                        case EResult.NoMatch:
                        default:
                            this.Logger?.LogMessage($"{{warn}}Failed to join boss zone: {ex.Message}");
                            ResetState();
                            throw;
                    }
                }
                catch (WebException ex)
                {
                    this.Logger?.LogMessage($"{{warn}}Failed to join boss zone: {ex.Message} - Giving it a few seconds ({i + 1}/5)...");
                    await Task.Delay(2000);
                    continue;
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
                    {
                        if (!string.IsNullOrWhiteSpace(response.NextLevelScore))
                            this.Logger?.LogMessage($"XP: {{oldxp}}{long.Parse(response.OldScore).ToString("#,##0")}{{reset}} -> {{xp}}{long.Parse(response.NewScore).ToString("#,##0")}{{reset}} (next level at {{reqxp}}{long.Parse(response.NextLevelScore).ToString("#,##0")}{{reset}})");
                        else
                            this.Logger?.LogMessage($"XP: {{oldxp}}{long.Parse(response.OldScore).ToString("#,##0")}{{reset}} -> {{xp}}{long.Parse(response.NewScore).ToString("#,##0")}");
                    }
                    if (response.NewLevel != response.OldLevel)
                        this.Logger?.LogMessage($"New level: {{oldlevel}}{response.OldLevel}{{reset}} -> {{level}}{response.NewLevel}{{reset}}");

                    // States
                    this.PlayerInfo.Score = response.NewScore;
                    this.PlayerInfo.Level = response.NewLevel;
                    this.PlayerInfo.NextLevelScore = response.NextLevelScore;
                    this.ActiveZone = null;
                    this.PlayerInfo.ActiveZoneGame = null;
                    this.PlayerInfo.ActiveZonePosition = null;
                    this.PlayerInfo.TimeInZone = TimeSpan.FromSeconds(0);
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
                        default:
                            this.Logger?.LogMessage($"{{warn}}Failed to submit score: {ex.Message}");
                            ResetState();
                            throw;
                    }
                }
                catch (WebException ex)
                {
                    this.Logger?.LogMessage($"{{warn}}Failed to submit score: {ex.Message} - Giving it a few seconds ({i + 1}/5)...");
                    await Task.Delay(2000);
                    continue;
                }
            }

            // States, only set when failed
            ResetState();
            void ResetState()
            {
                this.ActiveZone = null;
                this.PlayerInfo.ActiveZoneGame = null;
                this.PlayerInfo.ActiveZonePosition = null;
                this.State = BotState.ForcedZoneLeave;
            }
        }

        public async Task<BossLevelState> ReportBossDamage(int startLevel, long startXp, bool useHealAbility, int damageToBoss, int damageTaken)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var message = $"{{action}}Reporting boss damage {{value}}+{damageToBoss.ToString("#,##0")}";
                    if (damageTaken > 0)
                        message += $"{{action}}, {{negvalue}}-{damageTaken.ToString("#,##0")}";
                    if (useHealAbility)
                        message += $"{{action}}, {{value}}+Heal";
                    message += $"{{action}}...";
                    this.Logger?.LogMessage(message);

                    var response = await SaliensApi.ReportBossDamageAsync(this.Token, useHealAbility, damageToBoss, damageTaken);

                    if (response.BossStatus == null)
                        return BossLevelState.WaitingForPlayers;

                    BossPlayer currentPlayer = null;
                    var bossHpColor = MathUtils.ScaleColor(response.BossStatus.BossMaxHp - response.BossStatus.BossHp, response.BossStatus.BossMaxHp, new[] { "{svlow}", "{slow}", "{smed}", "{shigh}", "{svhigh}" });
                    this.Logger?.LogMessage($"{bossHpColor}Boss HP: {response.BossStatus.BossHp.ToString("#,##0")}/{response.BossStatus.BossMaxHp.ToString("#,##0")}{{reset}} - {{lasers}}{response.NumLaserUses} lasers{{reset}} - {{heals}}{response.NumTeamHeals} heals");
                    foreach (var player in response.BossStatus.BossPlayers.OrderBy(p => p.Name))
                    {
                        var playerStartLevel = player.LevelOnJoin;
                        long.TryParse(player.ScoreOnJoin, out long playerStartXp);

                        var isCurrentPlayer = playerStartLevel == startLevel && playerStartXp == startXp;
                        if (isCurrentPlayer)
                            currentPlayer = player;
                        var playerColor = isCurrentPlayer ? "{player}" : "{reset}";
                        var hpColor = MathUtils.ScaleColor(player.MaxHp - player.Hp, player.MaxHp, new[] { "{svlow}", "{slow}", "{smed}", "{shigh}", "{svhigh}" });
                        this.Logger?.LogMessage($"{playerColor}{(player.Name.Length > 16 ? player.Name.Substring(0, 16) : player.Name).PadLeft(16)}: " +
                            $"{hpColor}HP {player.Hp.ToString("#,##0").PadLeft(7)}/{player.MaxHp.ToString("#,##0").PadLeft(7)}{playerColor} - " +
                            $"XP {player.XpEarned.ToString("#,##0").PadLeft(9)}/{(playerStartXp + player.XpEarned).ToString("#,##0").PadLeft(12)}");
                    }

                    if (response.GameOver && currentPlayer != null && long.TryParse(this.PlayerInfo.Score, out long oldScore))
                    {
                        // States
                        this.PlayerInfo.Score = (oldScore + currentPlayer.XpEarned).ToString();
                        this.PlayerInfo.Level = currentPlayer.NewLevel;
                        this.PlayerInfo.NextLevelScore = currentPlayer.NextLevelScore;
                    }
                    return response.GameOver ? BossLevelState.GameOver : BossLevelState.Active;
                }
                catch (SaliensApiException ex)
                {
                    switch (ex.EResult)
                    {
                        case EResult.Fail:
                        case EResult.Busy:
                        case EResult.RateLimitExceeded:
                            this.Logger?.LogMessage($"{{warn}}Failed to report boss damage: {ex.Message} - Giving it a second ({i + 1}/5)...");
                            await Task.Delay(1000);
                            continue;

                        case EResult.Expired:
                        case EResult.NoMatch:
                        default:
                            this.Logger?.LogMessage($"{{warn}}Failed to report boss damage: {ex.Message}");
                            ResetState();
                            throw;
                    }
                }
                catch (WebException ex)
                {
                    this.Logger?.LogMessage($"{{warn}}Failed to report boss damage: {ex.Message} - Giving it a second ({i + 1}/5)...");
                    await Task.Delay(1000);
                    continue;
                }
            }

            // States, only set when failed
            ResetState();
            void ResetState()
            {
                this.ActiveZone = null;
                this.PlayerInfo.ActiveBossGame = null;
                this.PlayerInfo.ActiveZonePosition = null;
                this.State = BotState.ForcedZoneLeave;
            }

            return BossLevelState.Error;
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
                        this.PlayerInfo.TimeOnPlanet = TimeSpan.FromSeconds(0);
                        this.State = BotState.Idle;
                    }
                    else if (this.HasActiveZone && this.ActiveZone.GameId == gameId)
                    {
                        this.Logger?.LogMessage($"{{action}}Leaving zone {{zone}}{this.ActiveZone.ZonePosition} ({gameId}){{action}}...");

                        // States
                        this.ActiveZone = null;
                        this.PlayerInfo.ActiveBossGame = null;
                        this.PlayerInfo.ActiveZoneGame = null;
                        this.PlayerInfo.ActiveZonePosition = null;
                        this.PlayerInfo.TimeInZone = TimeSpan.FromSeconds(0);
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
                        default:
                            this.Logger?.LogMessage($"{{warn}}Failed to leave game: {ex.Message}");
                            ResetState();
                            throw;
                    }
                }
                catch (WebException ex)
                {
                    this.Logger?.LogMessage($"{{warn}}Failed to leave game: {ex.Message} - Giving it a few seconds ({i + 1}/5)...");
                    await Task.Delay(2000);
                    continue;
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
            var activePlanets = await this.GetActivePlanets();

            this.Logger?.LogMessage("Active planets:");
            foreach (var planet in activePlanets.Where(p => p.State.Running))
            {
                this.Logger?.LogMessage(planet.ToConsoleLine());
                if (this.ActivePlanet?.Id == planet.Id && this.HasActiveZone)
                    this.Logger?.LogMessage(this.ActiveZone.ToConsoleLine());
            }

            var futurePlanets = activePlanets.Where(p => !p.State.Active && !p.State.Captured).ToList();
            if (futurePlanets.Count > 0)
            {
                this.Logger?.LogMessage("Upcoming planets:");
                foreach (var planet in futurePlanets)
                    this.Logger?.LogMessage(planet.ToConsoleLine());
            }
        }


        public void Dispose()
        {
            this.cancelSource.Dispose();
        }
    }
}
