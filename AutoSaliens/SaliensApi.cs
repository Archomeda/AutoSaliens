using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoSaliens.Api.Models;
using Flurl;
using Flurl.Http;
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

        public static int NumberOfRetries { get; set; } = 5;


        public static async Task<List<Planet>> GetPlanets(bool activeOnly = false)
        {
            var url = Url.Combine(GetPlanetsUrl, activeOnly ? "&active_only=1" : "");
            var response = await url.GetJsonAsync<ApiResponse<PlanetsResponse>>();
            if(response?.Response?.Planets == null)
                throw new SaliensApiException();

            return response.Response.Planets;
        }

        public static async Task<Planet> GetPlanet(string id)
        {
            var response =  await GetPlanetUrl.SetQueryParam("id", id)
                .GetJsonAsync<ApiResponse<PlanetsResponse>>();
            if (response?.Response?.Planets == null || response.Response.Planets.Count < 1 || response.Response.Planets[0] == null)
                throw new SaliensApiException();

            return response.Response.Planets[0];
        }

        public static async Task<PlayerInfoResponse> GetPlayerInfo(string accessToken)
        {
            var response = await GetPlayerInfoUrl.SetQueryParam("access_token", accessToken)
                .PostAndRecieveAsync<ApiResponse<PlayerInfoResponse>>();

            if (response?.Response == null)
                throw new SaliensApiException();

            return response.Response;
        }

        public static async Task JoinPlanet(string accessToken, string planetId)
        {
            await JoinPlanetUrl.SetQueryParam("access_token", accessToken)
                .SetQueryParam("id", planetId)
                .PostStringAsync(string.Empty);
        }

        public static async Task<Zone> JoinZone(string accessToken, int zonePosition)
        {
            var response = await JoinZoneUrl.SetQueryParam("access_token", accessToken)
                .SetQueryParam("zone_position", zonePosition)
                .PostAndRecieveAsync<ApiResponse<JoinZoneResponse>>();
            if (response?.Response?.ZoneInfo == null)
                throw new SaliensApiException();
            return response.Response.ZoneInfo;
           
        }

        public static async Task<ReportScoreResponse> ReportScore(string accessToken, int score)
        {
            var response = await ReportScoreUrl.SetQueryParam("access_token", accessToken)
                .SetQueryParam("score", score)
                .PostAndRecieveAsync<ApiResponse<ReportScoreResponse>>();

            if(response?.Response == null)
                throw new SaliensApiException();
            return response.Response;
        }
        public static async Task LeaveGame(string accessToken, string gameId)
        {
            await LeaveGameUrl.SetQueryParam("access_token", accessToken).SetQueryParam("gameid", gameId).PostStringAsync(string.Empty);
            // Always an empty response? Well then...
        }

        private static async Task<T> PostAndRecieveAsync<T>(this Url url)
        {
            var res = await url.PostStringAsync(string.Empty);
            var eResult = res.Headers.GetValues("x-eresult").FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(eResult) && eResult != "1")
            {
                var message = res.Headers.GetValues("x-error_message").FirstOrDefault();
                throw SaliensApiException.FromString(eResult, message);
            }
            return JsonConvert.DeserializeObject<T>(await res.Content.ReadAsStringAsync());
        }
    }
}
