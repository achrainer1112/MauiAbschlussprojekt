using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Models;

namespace MauiAbschlussprojekt.Services
{
    public class SleepApiService
    {
        private readonly ApiService _apiService;
        private readonly HttpClient _httpClient;

        private static string BaseUrl
        {
            get
            {
#if ANDROID
                return "http://10.0.2.2:5287/api";
#else
                return "http://localhost:5287/api";
#endif
            }
        }

        public SleepApiService(ApiService apiService)
        {
            _apiService = apiService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private void SetAuthHeader()
        {
            if (!string.IsNullOrEmpty(_apiService.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _apiService.Token);
            }
        }

        public async Task<List<SleepEntryDto>> GetRecentEntriesAsync(int days = 7)
        {
            try
            {
                SetAuthHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/sleep/recent?days={days}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<SleepEntryDto>>(content) ?? new List<SleepEntryDto>();
                }

                return new List<SleepEntryDto>();
            }
            catch
            {
                return new List<SleepEntryDto>();
            }
        }

        public async Task<SleepEntryDto?> AddSleepEntryAsync(AddSleepEntryRequest request)
        {
            try
            {
                SetAuthHeader();
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/sleep/add", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<SleepEntryDto>(responseContent);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<SleepEntryDto?> UpdateSleepEntryAsync(UpdateSleepEntryRequest request)
        {
            try
            {
                SetAuthHeader();
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{BaseUrl}/sleep/update", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<SleepEntryDto>(responseContent);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteSleepEntryAsync(int entryId)
        {
            try
            {
                SetAuthHeader();
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/sleep/delete/{entryId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<WeekSleepStatsDto?> GetWeekStatsAsync()
        {
            try
            {
                SetAuthHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/sleep/stats/week");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<WeekSleepStatsDto>(content);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<SleepStatsDto?> GetDetailedStatsAsync(int days = 30)
        {
            try
            {
                SetAuthHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/sleep/stats/detailed?days={days}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<SleepStatsDto>(content);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<UserDto?> UpdateSleepSettingsAsync(UpdateSleepSettingsRequest request)
        {
            try
            {
                SetAuthHeader();
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{BaseUrl}/user/sleep-settings", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<UserDto>(responseContent);
                    _apiService.CurrentUser = user;
                    return user;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
