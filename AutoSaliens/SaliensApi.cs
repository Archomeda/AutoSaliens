using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoSaliens.Api.Models;
using Newtonsoft.Json;

namespace AutoSaliens
{
    internal static class SaliensApi
    {
        public const string BaseUrl = "https://community.steam-api.com/";
        public const string GetPlanetsUrl = BaseUrl + "ITerritoryControlMinigameService/GetPlanets/v0001?language=english";
        public const string GetPlanetUrl = BaseUrl + "ITerritoryControlMinigameService/GetPlanet/v0001?language=english";
        public const string GetPlayerInfoUrl = BaseUrl + "ITerritoryControlMinigameService/GetPlayerInfo/v0001";
        public const string JoinPlanetUrl = BaseUrl + "ITerritoryControlMinigameService/JoinPlanet/v0001";
        public const string JoinZoneUrl = BaseUrl + "ITerritoryControlMinigameService/JoinZone/v0001";
        public const string RepresentClanUrl = BaseUrl + "ITerritoryCOntrolMinigameService/RepresentClan/v0001";
        public const string ReportScoreUrl = BaseUrl + "ITerritoryCOntrolMinigameService/ReportScore/v0001";
        public const string LeaveGameUrl = BaseUrl + "IMiniGameService/LeaveGame/v0001";

        #region API functions

        public static List<Planet> GetPlanets(bool activeOnly = false)
        {
            var uri = new Uri(GetPlanetsUrl + (activeOnly ? "&active_only=1" : ""));
            return ParsePlanets(GetJson<ApiResponse<PlanetsResponse>>(uri));
        }

        public static async Task<List<Planet>> GetPlanetsAsync(bool activeOnly = false)
        {
            var uri = new Uri(GetPlanetsUrl + (activeOnly ? "&active_only=1" : ""));
            return ParsePlanets(await GetJsonAsync<ApiResponse<PlanetsResponse>>(uri));
        }

        private static List<Planet> ParsePlanets(ApiResponse<PlanetsResponse> response)
        {
            return response?.Response?.Planets ?? throw new SaliensApiException();
        }


        public static Planet GetPlanet(string id)
        {
            var uri = new Uri(GetPlanetUrl + $"&id={id}");
            return ParsePlanet(GetJson<ApiResponse<PlanetsResponse>>(uri));
        }

        public static async Task<Planet> GetPlanetAsync(string id)
        {
            var uri = new Uri(GetPlanetUrl + $"&id={id}");
            return ParsePlanet(await GetJsonAsync<ApiResponse<PlanetsResponse>>(uri));
        }

        private static Planet ParsePlanet(ApiResponse<PlanetsResponse> response)
        {
            if (response?.Response?.Planets == null || response.Response.Planets.Count < 1 || response.Response.Planets[0] == null)
                throw new SaliensApiException();
            return response.Response.Planets[0];
        }


        public static PlayerInfoResponse GetPlayerInfo(string accessToken)
        {
            var uri = new Uri(GetPlayerInfoUrl + $"?access_token={accessToken}");
            return ParsePlayerInfo(PostJson<ApiResponse<PlayerInfoResponse>>(uri));
        }

        public static async Task<PlayerInfoResponse> GetPlayerInfoAsync(string accessToken)
        {
            var uri = new Uri(GetPlayerInfoUrl + $"?access_token={accessToken}");
            return ParsePlayerInfo(await PostJsonAsync<ApiResponse<PlayerInfoResponse>>(uri));
        }

        private static PlayerInfoResponse ParsePlayerInfo(ApiResponse<PlayerInfoResponse> response)
        {
            return response?.Response ?? throw new SaliensApiException();
        }


        public static void JoinPlanet(string accessToken, string planetId)
        {
            var uri = new Uri(JoinPlanetUrl + $"?access_token={accessToken}&id={planetId}");
            PostJson<ApiResponse<object>>(uri);
        }

        public static async Task JoinPlanetAsync(string accessToken, string planetId)
        {
            var uri = new Uri(JoinPlanetUrl + $"?access_token={accessToken}&id={planetId}");
            await PostJsonAsync<ApiResponse<object>>(uri);
        }


        public static Zone JoinZone(string accessToken, int zonePosition)
        {
            var uri = new Uri(JoinZoneUrl + $"?access_token={accessToken}&zone_position={zonePosition}");
            return ParseJoinZone(PostJson<ApiResponse<JoinZoneResponse>>(uri));
        }

        public static async Task<Zone> JoinZoneAsync(string accessToken, int zonePosition)
        {
            var uri = new Uri(JoinZoneUrl + $"?access_token={accessToken}&zone_position={zonePosition}");
            return ParseJoinZone(await PostJsonAsync<ApiResponse<JoinZoneResponse>>(uri));
        }

        private static Zone ParseJoinZone(ApiResponse<JoinZoneResponse> response)
        {
            return response?.Response?.ZoneInfo ?? throw new SaliensApiException();
        }


        public static ReportScoreResponse ReportScore(string accessToken, int score)
        {
            var uri = new Uri(ReportScoreUrl + $"?access_token={accessToken}&score={score}");
            return ParseReportScore(PostJson<ApiResponse<ReportScoreResponse>>(uri));
        }

        public static async Task<ReportScoreResponse> ReportScoreAsync(string accessToken, int score)
        {
            var uri = new Uri(ReportScoreUrl + $"?access_token={accessToken}&score={score}");
            return ParseReportScore(await PostJsonAsync<ApiResponse<ReportScoreResponse>>(uri));
        }

        private static ReportScoreResponse ParseReportScore(ApiResponse<ReportScoreResponse> response)
        {
            return response?.Response ?? throw new SaliensApiException();
        }


        public static void LeaveGame(string accessToken, string gameId)
        {
            var uri = new Uri(LeaveGameUrl + $"?access_token={accessToken}&gameid={gameId}");
            PostJson<ApiResponse<object>>(uri);
        }

        public static async Task LeaveGameAsync(string accessToken, string gameId)
        {
            var uri = new Uri(LeaveGameUrl + $"?access_token={accessToken}&gameid={gameId}");
            await PostJsonAsync<ApiResponse<object>>(uri);
        }

        #endregion

        #region Request helpers

        private static T GetJson<T>(Uri uri) => DoRequest<T>(uri);

        private static Task<T> GetJsonAsync<T>(Uri uri) => DoRequestAsync<T>(uri);

        private static T PostJson<T>(Uri uri) => DoRequest<T>(uri, true);

        private static Task<T> PostJsonAsync<T>(Uri uri) => DoRequestAsync<T>(uri, true);

        private static T DoRequest<T>(Uri uri, bool isPost = false)
        {
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", "AutoSaliens/1.0 (https://github.com/Archomeda/AutoAliens)");
                var json = isPost ? webClient.UploadString(uri, "") : webClient.DownloadString(uri);
                var eResult = webClient.ResponseHeaders["x-eresult"].ToString();
                if (!string.IsNullOrWhiteSpace(eResult) && eResult != "1")
                {
                    var message = webClient.ResponseHeaders["x-error_message"]?.ToString();
                    throw SaliensApiException.FromString(eResult, message);
                }
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        private static async Task<T> DoRequestAsync<T>(Uri uri, bool isPost = false)
        {
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", "AutoSaliens/1.0 (https://github.com/Archomeda/AutoAliens)");
                var json = isPost ? await webClient.UploadStringTaskAsync(uri, "") : await webClient.DownloadStringTaskAsync(uri);
                var eResult = webClient.ResponseHeaders["x-eresult"].ToString();
                if (!string.IsNullOrWhiteSpace(eResult) && eResult != "1")
                {
                    var message = webClient.ResponseHeaders["x-error_message"]?.ToString();
                    throw SaliensApiException.FromString(eResult, message);
                }
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        #endregion
    }
}
