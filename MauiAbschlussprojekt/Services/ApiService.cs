using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Models;

namespace MauiAbschlussprojekt.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:5287/api";

        public string? Token { get; set; }
        public int? CurrentUserId { get; set; }
        public UserDto? CurrentUser { get; set; }

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private void SetAuthHeader()
        {
            if (!string.IsNullOrEmpty(Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Token);
            }
        }

        // === AUTH ===

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/auth/register", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);
                    if (authResponse != null && authResponse.Success)
                    {
                        Token = authResponse.Token;
                        CurrentUserId = authResponse.User?.Id;
                        CurrentUser = authResponse.User;
                        return authResponse;
                    }
                }

                var errorResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);
                return errorResponse ?? new AuthResponse
                {
                    Success = false,
                    Message = $"Fehler: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Verbindungsfehler: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);
                    if (authResponse != null && authResponse.Success)
                    {
                        Token = authResponse.Token;
                        CurrentUserId = authResponse.User?.Id;
                        CurrentUser = authResponse.User;
                        return authResponse;
                    }
                }

                var errorResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);
                return errorResponse ?? new AuthResponse
                {
                    Success = false,
                    Message = $"Fehler: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Verbindungsfehler: {ex.Message}"
                };
            }
        }

        public void Logout()
        {
            Token = null;
            CurrentUserId = null;
            CurrentUser = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        // === USER ===

        public async Task<UserDto?> GetUserAsync()
        {
            if (!CurrentUserId.HasValue)
                return null;

            try
            {
                SetAuthHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/user/{CurrentUserId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<UserDto>(content);
                    CurrentUser = user;
                    return user;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<UserDto?> UpdateUserAsync(UpdateUserRequest request)
        {
            if (!CurrentUserId.HasValue)
                return null;

            try
            {
                SetAuthHeader();
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{BaseUrl}/user/update?userId={CurrentUserId}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<UserDto>(responseContent);
                    CurrentUser = user;
                    return user;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<UserDto?> UpdateReminderSettingsAsync(UpdateReminderRequest request)
        {
            if (!CurrentUserId.HasValue)
                return null;

            try
            {
                SetAuthHeader();
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{BaseUrl}/user/reminder-settings?userId={CurrentUserId}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<UserDto>(responseContent);
                    CurrentUser = user;
                    return user;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // === WATER ENTRIES ===

        public async Task<List<WaterEntryDto>> GetTodayEntriesAsync()
        {
            if (!CurrentUserId.HasValue)
                return new List<WaterEntryDto>();

            try
            {
                SetAuthHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/water/today/{CurrentUserId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<WaterEntryDto>>(content) ?? new List<WaterEntryDto>();
                }

                return new List<WaterEntryDto>();
            }
            catch
            {
                return new List<WaterEntryDto>();
            }
        }

        public async Task<WaterEntryDto?> AddWaterAsync(AddWaterRequest request)
        {
            if (!CurrentUserId.HasValue)
                return null;

            try
            {
                SetAuthHeader();
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/water/add?userId={CurrentUserId}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<WaterEntryDto>(responseContent);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<WaterEntryDto?> UpdateWaterAsync(UpdateWaterEntryRequest request)
        {
            if (!CurrentUserId.HasValue)
                return null;

            try
            {
                SetAuthHeader();
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{BaseUrl}/water/update?userId={CurrentUserId}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<WaterEntryDto>(responseContent);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteWaterAsync(int entryId)
        {
            if (!CurrentUserId.HasValue)
                return false;

            try
            {
                SetAuthHeader();
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/water/delete/{entryId}?userId={CurrentUserId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // === STATISTICS ===

        public async Task<WeekStatsDto?> GetWeekStatsAsync()
        {
            if (!CurrentUserId.HasValue)
                return null;

            try
            {
                SetAuthHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/water/stats/week/{CurrentUserId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<WeekStatsDto>(content);
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