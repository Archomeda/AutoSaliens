using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
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

        public static int NumberOfRetries { get; set; } = 5;


        public static async Task<List<Planet>> GetPlanets(bool activeOnly = false)
        {
            var uri = new Uri(GetPlanetsUrl + (activeOnly ? "&active_only=1" : ""));
            return (await GetJson<ApiResponse<PlanetsResponse>>(uri)).Response?.Planets;
        }

        public static Task<List<Planet>> GetPlanets(CancellationToken cancellationToken) =>
            CallN(() => GetPlanets(), NumberOfRetries, cancellationToken);

        public static Task<List<Planet>> GetPlanets(bool activeOnly, CancellationToken cancellationToken) =>
            CallN(() => GetPlanets(activeOnly), NumberOfRetries, cancellationToken);

        public static async Task<Planet> GetPlanet(string id)
        {
            var uri = new Uri(GetPlanetUrl + $"&id={id}");
            return (await GetJson<ApiResponse<PlanetsResponse>>(uri)).Response?.Planets[0];
        }

        public static Task<Planet> GetPlanet(string id, CancellationToken cancellationToken) =>
            CallN(() => GetPlanet(id), NumberOfRetries, cancellationToken);

        public static async Task<PlayerInfoResponse> GetPlayerInfo(string accessToken)
        {
            var uri = new Uri(GetPlayerInfoUrl + $"?access_token={accessToken}");
            return (await PostJson<ApiResponse<PlayerInfoResponse>>(uri)).Response;
        }

        public static Task<PlayerInfoResponse> GetPlayerInfo(string accessToken, CancellationToken cancellationToken) =>
            CallN(() => GetPlayerInfo(accessToken), NumberOfRetries, cancellationToken);

        public static async Task JoinPlanet(string accessToken, string planetId)
        {
            var uri = new Uri(JoinPlanetUrl + $"?access_token={accessToken}&id={planetId}");
            await PostJson<ApiResponse<object>>(uri);
            // Always an empty response? Well then...
        }

        public static Task JoinPlanet(string accessToken, string planetId, CancellationToken cancellationToken) =>
            CallN(() => JoinPlanet(accessToken, planetId), NumberOfRetries, cancellationToken);

        public static async Task<Zone> JoinZone(string accessToken, int zonePosition)
        {
            var uri = new Uri(JoinZoneUrl + $"?access_token={accessToken}&zone_position={zonePosition}");
            var response = await PostJson<ApiResponse<JoinZoneResponse>>(uri);
            if (response.Response == null)
                throw new SaliensApiException();

            return response.Response.ZoneInfo;
        }

        public static Task<Zone> JoinZone(string accessToken, int zonePosition, CancellationToken cancellationToken) =>
            CallN(() => JoinZone(accessToken, zonePosition), NumberOfRetries, cancellationToken);

        public static async Task<ReportScoreResponse> ReportScore(string accessToken, int score)
        {
            var uri = new Uri(ReportScoreUrl + $"?access_token={accessToken}&score={score}");
            var response = await PostJson<ApiResponse<ReportScoreResponse>>(uri);
            if (response.Response == null)
                throw new SaliensApiException();

            return response.Response;
        }

        public static Task<ReportScoreResponse> ReportScore(string accessToken, int score, CancellationToken cancellationToken) =>
            CallN(() => ReportScore(accessToken, score), NumberOfRetries, cancellationToken);

        public static async Task LeaveGame(string accessToken, string planetId)
        {
            var uri = new Uri(LeaveGameUrl + $"?access_token={accessToken}&gameid={planetId}");
            await PostJson<ApiResponse<object>>(uri);
            // Always an empty response? Well then...
        }

        public static Task LeaveGame(string accessToken, string planetId, CancellationToken cancellationToken) =>
          CallN(() => LeaveGame(accessToken, planetId), NumberOfRetries, cancellationToken);


        private static Task CallN(Func<Task> func, int times, CancellationToken cancellationToken) =>
            CallN(() => { func(); return Task.FromResult(true); }, times, cancellationToken);

        private static async Task<T> CallN<T>(Func<Task<T>> func, int times, CancellationToken cancellationToken)
        {
            var exceptions = new List<Exception>();
            for (int i = 0; i < times; i++)
            {
                try
                {
                    return await func();
                }
                catch (SaliensApiException)
                {
                    // Game error, no use repeating this...
                    throw;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    try
                    {
                        await Task.Delay(2000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                }
            }
            throw new AggregateException(exceptions);
        }


        private static async Task<T> GetJson<T>(Uri uri)
        {
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", "AutoSaliens/1.0 (https://github.com/Archomeda/AutoAliens)");
                var json = await webClient.DownloadStringTaskAsync(uri);
                var eResult = webClient.ResponseHeaders["x-eresult"].ToString();
                if (!string.IsNullOrWhiteSpace(eResult) && eResult != "1")
                    throw SaliensApiException.FromString(eResult);
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        private static async Task<T> PostJson<T>(Uri uri)
        {
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", "AutoSaliens/1.0 (https://github.com/Archomeda/AutoAliens)");
                var json = await webClient.UploadStringTaskAsync(uri, "");
                var eResult = webClient.ResponseHeaders["x-eresult"].ToString();
                if (!string.IsNullOrWhiteSpace(eResult) && eResult != "1")
                    throw SaliensApiException.FromString(eResult);
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
}
