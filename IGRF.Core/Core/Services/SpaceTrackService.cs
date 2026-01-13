#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IGRF_Interface.Core.Interfaces;

namespace IGRF_Interface.Core.Services
{
    public class SpaceTrackService : ISpaceTrackService
    {
        private HttpClient _httpClient;
        private System.Net.CookieContainer _cookieContainer;
        public bool IsLoggedIn { get; private set; }
        public string LastStatus { get; private set; } = "";

        public SpaceTrackService()
        {
            _cookieContainer = new System.Net.CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var loginUrl = "https://www.space-track.org/ajaxauth/login";
                var loginContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("identity", username),
                    new KeyValuePair<string, string>("password", password)
                });

                var response = await _httpClient.PostAsync(loginUrl, loginContent);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && !result.Contains("Failed"))
                {
                    IsLoggedIn = true;
                    LastStatus = "Logged in successfully";
                    return true;
                }

                LastStatus = "Login failed. Check credentials.";
                IsLoggedIn = false;
                return false;
            }
            catch (Exception ex)
            {
                LastStatus = $"Login Error: {ex.Message}";
                IsLoggedIn = false;
                return false;
            }
        }

        public async Task<SatelliteInfo?> FetchTleAsync(string noradId)
        {
            if (!IsLoggedIn)
            {
                LastStatus = "Not logged in";
                return null;
            }

            try
            {
                LastStatus = $"Fetching TLE for {noradId}...";

                // Space-Track query requires numeric ID (6 digits) used in query
                // If Alpha-5 (e.g. A0001), convert to 100001
                string queryId = ConvertAlpha5ToNumber(noradId);

                // Use format/json to get full metadata including OBJECT_TYPE
                string queryUrl = $"https://www.space-track.org/basicspacedata/query/class/gp/norad_cat_id/{queryId}/format/json";

                var response = await _httpClient.GetStringAsync(queryUrl);

                // Parse JSON manually or use System.Text.Json
                // Using dynamic/JObject approach or simple string parsing if dependency is an issue
                // Given .NET 6+, System.Text.Json is standard.

                using (var doc = System.Text.Json.JsonDocument.Parse(response))
                {
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                    {
                        var el = doc.RootElement[0];
                        string name = el.GetProperty("OBJECT_NAME").GetString() ?? "Unknown";
                        string type = el.GetProperty("OBJECT_TYPE").GetString() ?? "UNKNOWN";
                        string l1 = el.GetProperty("TLE_LINE1").GetString() ?? "";
                        string l2 = el.GetProperty("TLE_LINE2").GetString() ?? "";

                        LastStatus = $"Found {name} ({type})";
                        return new SatelliteInfo
                        {
                            Name = name,
                            ID = noradId,
                            Line1 = l1,
                            Line2 = l2,
                            ObjectType = type
                        };
                    }
                }

                LastStatus = "No data found";
                return null;
            }
            catch (Exception ex)
            {
                LastStatus = $"Fetch Error: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Converts Alpha-5 ID (e.g. "A0001") to 6-digit number (100001).
        /// Checks if input matches Alpha-5 pattern: [A-Z][0-9]{4}
        /// </summary>
        private string ConvertAlpha5ToNumber(string input)
        {
            input = input.Trim().ToUpper();
            if (string.IsNullOrEmpty(input)) return input;

            // Check length and pattern for Alpha-5
            if (input.Length == 5 && char.IsLetter(input[0]) &&
                char.IsDigit(input[1]) && char.IsDigit(input[2]) &&
                char.IsDigit(input[3]) && char.IsDigit(input[4]))
            {
                char first = input[0];
                // Alpha-5 Logic: A=10, B=11 ... Z=33 (skipping I and O)
                int prefix = 0;
                if (first >= 'A' && first <= 'H') prefix = first - 'A' + 10;
                else if (first >= 'J' && first <= 'N') prefix = first - 'A' + 10 - 1;
                else if (first >= 'P' && first <= 'Z') prefix = first - 'A' + 10 - 2;
                else return input; // Invalid letter?

                return $"{prefix}{input.Substring(1)}";
            }

            return input;
        }
    }
}
