using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api.Models;
using Newtonsoft.Json;

namespace AutoSaliens.Api
{
    internal static class SaliensApi
    {
        private const string BaseUrl = "https://community.steam-api.com/";
        private const string GetPlanetsUrl = BaseUrl + "ITerritoryControlMinigameService/GetPlanets/v0001?language=english";
        private const string GetPlanetUrl = BaseUrl + "ITerritoryControlMinigameService/GetPlanet/v0001?language=english";
        private const string GetPlayerInfoUrl = BaseUrl + "ITerritoryControlMinigameService/GetPlayerInfo/v0001";
        private const string JoinPlanetUrl = BaseUrl + "ITerritoryControlMinigameService/JoinPlanet/v0001";
        private const string JoinZoneUrl = BaseUrl + "ITerritoryControlMinigameService/JoinZone/v0001";
        private const string JoinBossZoneUrl = BaseUrl + "ITerritoryControlMinigameService/JoinBossZone/v0001";
        private const string RepresentClanUrl = BaseUrl + "ITerritoryControlMinigameService/RepresentClan/v0001";
        private const string ReportScoreUrl = BaseUrl + "ITerritoryControlMinigameService/ReportScore/v0001";
        private const string ReportBossDamageUrl = BaseUrl + "ITerritoryControlMinigameService/ReportBossDamage/v0001";
        private const string LeaveGameUrl = BaseUrl + "IMiniGameService/LeaveGame/v0001";

        private static readonly TimeSpan planetCacheDuration = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan playerInfoDuration = TimeSpan.FromSeconds(10);

        private static Dictionary<string, CacheItem<Planet>> cachedPlanets = new Dictionary<string, CacheItem<Planet>>();
        private static Dictionary<string, CacheItem<Planet>> cachedPlanetsWithZones = new Dictionary<string, CacheItem<Planet>>();
        private static Dictionary<string, CacheItem<PlayerInfoResponse>> cachedPlayerInfo = new Dictionary<string, CacheItem<PlayerInfoResponse>>();


        #region API functions

        public static Dictionary<string, Planet> GetPlanets(bool activeOnly = false, bool forceLive = false)
        {
            if (forceLive || cachedPlanets.Count == 0 || cachedPlanets.Any(kvp => kvp.Value.Expires < DateTime.Now))
            {
                var requestActiveOnly = cachedPlanets.Count > 0 && activeOnly;
                var uri = new Uri(GetPlanetsUrl + (requestActiveOnly ? "&active_only=1" : ""));
                UpdatePlanets(requestActiveOnly, GetJson<ApiResponse<PlanetsResponse>>(uri));
            }
            IEnumerable<KeyValuePair<string, CacheItem<Planet>>> e = cachedPlanets;
            if (activeOnly)
                e = e.Where(p => p.Value.Item.State.Running);
            return e.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Item);
        }

        public static async Task<Dictionary<string, Planet>> GetPlanetsAsync(bool activeOnly = false, bool forceLive = false)
        {
            if (forceLive || cachedPlanets.Count == 0 || cachedPlanets.Any(kvp => kvp.Value.Expires < DateTime.Now))
            {
                var requestActiveOnly = cachedPlanets.Count > 0 && activeOnly;
                var uri = new Uri(GetPlanetsUrl + (requestActiveOnly ? "&active_only=1" : ""));
                UpdatePlanets(requestActiveOnly, await GetJsonAsync<ApiResponse<PlanetsResponse>>(uri));
            }
            IEnumerable<KeyValuePair<string, CacheItem<Planet>>> e = cachedPlanets;
            if (activeOnly)
                e = e.Where(p => p.Value.Item.State.Running);
            return e.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Item);
        }

        private static void UpdatePlanets(bool activeOnly, ApiResponse<PlanetsResponse> response)
        {
            var planets = response?.Response?.Planets ?? throw new SaliensApiException();
            if (activeOnly)
                foreach (var planet in cachedPlanets)
                    planet.Value.Item.State.Active = false;
            foreach (var planet in planets)
                cachedPlanets[planet.Id] = new CacheItem<Planet>(planet, planetCacheDuration);
        }


        public static Dictionary<string, Planet> GetPlanetsWithZones(bool activeOnly = false, bool forceLive = false)
        {
            if (forceLive || cachedPlanetsWithZones.Count == 0 || cachedPlanetsWithZones.Any(kvp => kvp.Value.Expires < DateTime.Now))
            {
                var planets = GetPlanets(activeOnly);
                var responses = planets.Values.Select(p => GetPlanet(p.Id));
                UpdatePlanetsWithZones(activeOnly, responses);
            }
            IEnumerable<KeyValuePair<string, CacheItem<Planet>>> e = cachedPlanetsWithZones;
            if (activeOnly)
                e = e.Where(p => p.Value.Item.State.Running);
            return e.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Item);
        }

        public static async Task<Dictionary<string, Planet>> GetPlanetsWithZonesAsync(bool activeOnly = false, bool forceLive = false)
        {
            if (forceLive || cachedPlanetsWithZones.Count == 0 || cachedPlanetsWithZones.Any(kvp => kvp.Value.Expires < DateTime.Now))
            {
                var planets = await GetPlanetsAsync(activeOnly);
                var responses = await Task.WhenAll(planets.Values.Select(p => GetPlanetAsync(p.Id)));
                UpdatePlanetsWithZones(activeOnly, responses);
            }
            IEnumerable<KeyValuePair<string, CacheItem<Planet>>> e = cachedPlanetsWithZones;
            if (activeOnly)
                e = e.Where(p => p.Value.Item.State.Running);
            return e.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Item);
        }

        private static void UpdatePlanetsWithZones(bool activeOnly, IEnumerable<Planet> planets)
        {
            if (activeOnly)
                foreach (var planet in cachedPlanetsWithZones.Where(kvp => !planets.Any(p => p.Id == kvp.Value.Item.Id)))
                    planet.Value.Item.State.Active = false;

            foreach (var planet in planets)
                cachedPlanetsWithZones[planet.Id] = new CacheItem<Planet>(planet, planetCacheDuration);
        }


        public static Planet GetPlanet(string id, bool forceLive = false)
        {
            if (forceLive || !cachedPlanetsWithZones.ContainsKey(id) || cachedPlanetsWithZones[id].Expires < DateTime.Now)
            {
                var uri = new Uri(GetPlanetUrl + $"&id={id}");
                UpdatePlanet(GetJson<ApiResponse<PlanetsResponse>>(uri));
            }
            return cachedPlanetsWithZones[id].Item;
        }

        public static async Task<Planet> GetPlanetAsync(string id, bool forceLive = false)
        {
            if (forceLive || !cachedPlanetsWithZones.ContainsKey(id) || cachedPlanetsWithZones[id].Expires < DateTime.Now)
            {
                var uri = new Uri(GetPlanetUrl + $"&id={id}");
                UpdatePlanet(await GetJsonAsync<ApiResponse<PlanetsResponse>>(uri));
            }
            return cachedPlanetsWithZones[id].Item;
        }

        private static void UpdatePlanet(ApiResponse<PlanetsResponse> response)
        {
            if (response?.Response?.Planets == null || response.Response.Planets.Count < 1 || response.Response.Planets[0] == null)
                throw new SaliensApiException();
            var planet = response.Response.Planets[0];
            cachedPlanetsWithZones[planet.Id] = new CacheItem<Planet>(planet, planetCacheDuration);
        }


        public static PlayerInfoResponse GetPlayerInfo(string accessToken, bool forceLive = false)
        {
            if (forceLive || !cachedPlayerInfo.ContainsKey(accessToken) || cachedPlayerInfo[accessToken].Expires < DateTime.Now)
            {
                var uri = new Uri(GetPlayerInfoUrl);
                UpdatePlayerInfo(accessToken, PostJson<ApiResponse<PlayerInfoResponse>>(uri, $"access_token={accessToken}"));
            }
            return cachedPlayerInfo[accessToken].Item;
        }

        public static async Task<PlayerInfoResponse> GetPlayerInfoAsync(string accessToken, bool forceLive = false)
        {
            if (forceLive || !cachedPlayerInfo.ContainsKey(accessToken) || cachedPlayerInfo[accessToken].Expires < DateTime.Now)
            {
                var uri = new Uri(GetPlayerInfoUrl);
                UpdatePlayerInfo(accessToken, await PostJsonAsync<ApiResponse<PlayerInfoResponse>>(uri, $"access_token={accessToken}"));
            }
            return cachedPlayerInfo[accessToken].Item;
        }

        private static void UpdatePlayerInfo(string accessToken, ApiResponse<PlayerInfoResponse> response)
        {
            var playerInfo = response?.Response ?? throw new SaliensApiException();
            cachedPlayerInfo[accessToken] = new CacheItem<PlayerInfoResponse>(playerInfo, playerInfoDuration);
        }


        public static void JoinPlanet(string accessToken, string planetId)
        {
            var uri = new Uri(JoinPlanetUrl);
            PostJson<ApiResponse<object>>(uri, $"access_token={accessToken}&id={planetId}");
        }

        public static async Task JoinPlanetAsync(string accessToken, string planetId)
        {
            var uri = new Uri(JoinPlanetUrl);
            await PostJsonAsync<ApiResponse<object>>(uri, $"access_token={accessToken}&id={planetId}");
        }


        public static Zone JoinZone(string accessToken, int zonePosition)
        {
            var uri = new Uri(JoinZoneUrl);
            return ParseJoinZone(PostJson<ApiResponse<JoinZoneResponse>>(uri, $"access_token={accessToken}&zone_position={zonePosition}"));
        }

        public static async Task<Zone> JoinZoneAsync(string accessToken, int zonePosition)
        {
            var uri = new Uri(JoinZoneUrl);
            return ParseJoinZone(await PostJsonAsync<ApiResponse<JoinZoneResponse>>(uri, $"access_token={accessToken}&zone_position={zonePosition}"));
        }

        private static Zone ParseJoinZone(ApiResponse<JoinZoneResponse> response)
        {
            return response?.Response?.ZoneInfo ?? throw new SaliensApiException();
        }


        public static JoinBossZoneResponse JoinBossZone(string accessToken, int zonePosition)
        {
            var uri = new Uri(JoinBossZoneUrl);
            return ParseJoinBossZone(PostJson<ApiResponse<JoinBossZoneResponse>>(uri, $"access_token={accessToken}&zone_position={zonePosition}"));
        }

        public static async Task<JoinBossZoneResponse> JoinBossZoneAsync(string accessToken, int zonePosition)
        {
            var uri = new Uri(JoinBossZoneUrl);
            return ParseJoinBossZone(await PostJsonAsync<ApiResponse<JoinBossZoneResponse>>(uri, $"access_token={accessToken}&zone_position={zonePosition}"));
        }

        private static JoinBossZoneResponse ParseJoinBossZone(ApiResponse<JoinBossZoneResponse> response)
        {
            return response?.Response ?? throw new SaliensApiException();
        }


        public static ReportScoreResponse ReportScore(string accessToken, int score)
        {
            var uri = new Uri(ReportScoreUrl);
            return ParseReportScore(PostJson<ApiResponse<ReportScoreResponse>>(uri, $"access_token={accessToken}&score={score}"));
        }

        public static async Task<ReportScoreResponse> ReportScoreAsync(string accessToken, int score)
        {
            var uri = new Uri(ReportScoreUrl);
            return ParseReportScore(await PostJsonAsync<ApiResponse<ReportScoreResponse>>(uri, $"access_token={accessToken}&score={score}"));
        }

        private static ReportScoreResponse ParseReportScore(ApiResponse<ReportScoreResponse> response)
        {
            return response?.Response ?? throw new SaliensApiException();
        }


        public static ReportBossDamageResponse ReportBossDamage(string accessToken, bool useHealAbility, int damageToBoss, int damageTaken)
        {
            var uri = new Uri(ReportBossDamageUrl);
            return ParseReportBossDamage(PostJson<ApiResponse<ReportBossDamageResponse>>(uri,
                $"access_token={accessToken}&use_heal_ability={(useHealAbility ? "1" : "0")}&damage_to_boss={damageToBoss}&damage_taken={damageTaken}"));
        }

        public static async Task<ReportBossDamageResponse> ReportBossDamageAsync(string accessToken, bool useHealAbility, int damageToBoss, int damageTaken)
        {
            var uri = new Uri(ReportBossDamageUrl);
            return ParseReportBossDamage(await PostJsonAsync<ApiResponse<ReportBossDamageResponse>>(uri,
                $"access_token={accessToken}&use_heal_ability={(useHealAbility ? "1" : "0")}&damage_to_boss={damageToBoss}&damage_taken={damageTaken}"));
        }

        private static ReportBossDamageResponse ParseReportBossDamage(ApiResponse<ReportBossDamageResponse> response)
        {
            return response?.Response ?? throw new SaliensApiException();
        }


        public static void LeaveGame(string accessToken, string gameId)
        {
            var uri = new Uri(LeaveGameUrl);
            PostJson<ApiResponse<object>>(uri, $"access_token={accessToken}&gameid={gameId}");
        }

        public static async Task LeaveGameAsync(string accessToken, string gameId)
        {
            var uri = new Uri(LeaveGameUrl);
            await PostJsonAsync<ApiResponse<object>>(uri, $"access_token={accessToken}&gameid={gameId}");
        }

        #endregion

        #region Request helpers

        private static T GetJson<T>(Uri uri) => DoRequest<T>(uri);

        private static Task<T> GetJsonAsync<T>(Uri uri) => DoRequestAsync<T>(uri);

        private static T PostJson<T>(Uri uri, string data) => DoRequest<T>(uri, data);

        private static Task<T> PostJsonAsync<T>(Uri uri, string data) => DoRequestAsync<T>(uri, data);

        private static T DoRequest<T>(Uri uri, string data = null)
        {
            var isPost = data != null;
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", "AutoSaliens/1.0 (https://github.com/Archomeda/AutoAliens)");
                if (isPost)
                    webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
#if DEBUG
                Program.Logger.LogMessage($"{{verb}}{(isPost ? "[POST]" : "[GET]")} {uri}");
#endif
                var json = isPost ? webClient.UploadString(uri, data) : webClient.DownloadString(uri);
                var eResult = webClient.ResponseHeaders["x-eresult"].ToString();
#if DEBUG
                Program.Logger.LogMessage($"{{verb}}EResult: {eResult}");
#endif
                if (!string.IsNullOrWhiteSpace(eResult) && eResult != "1")
                {
                    var message = webClient.ResponseHeaders["x-error_message"]?.ToString();
#if DEBUG
                    Program.Logger.LogMessage($"{{verb}}Error message: {message}");
#endif
                    throw SaliensApiException.FromString(eResult, message);
                }
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        private static async Task<T> DoRequestAsync<T>(Uri uri, string data = null)
        {
            var isPost = data != null;
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", "AutoSaliens/1.1 (https://github.com/Archomeda/AutoAliens)");
                if (isPost)
                    webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
#if DEBUG
                Program.Logger.LogMessage($"{{verb}}{(isPost ? "[POST]" : "[GET]")} {uri}");
#endif
                var json = isPost ? await webClient.UploadStringTaskAsync(uri, data) : await webClient.DownloadStringTaskAsync(uri);
                var eResult = webClient.ResponseHeaders["x-eresult"].ToString();
#if DEBUG
                Program.Logger.LogMessage($"{{verb}}EResult: {eResult}");
#endif
                if (!string.IsNullOrWhiteSpace(eResult) && eResult != "1")
                {
                    var message = webClient.ResponseHeaders["x-error_message"]?.ToString();
#if DEBUG
                    Program.Logger.LogMessage($"{{verb}}Error message: {message}");
#endif
                    throw SaliensApiException.FromString(eResult, message);
                }
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        #endregion
    }
}
