using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Arcade_mania_backend_webAPI.Models.Dtos.Users;

namespace Arcade_mania_backend_WPF.Services
{
    public class UserApiService
    {
        private readonly HttpClient _httpClient;

        private const string BaseUrl = "http://localhost:5118/api/Users";

        // FONTOS: egyezzen az appsettings.json Admin:ServiceKey értékével
        private const string AdminServiceKey = "ArcadeMania_WPF_ServiceKey_AtLeast_32_Chars!";

        private string? _adminToken;

        public UserApiService()
        {
            _httpClient = new HttpClient();
        }

        // --- AUTO TOKEN: WPF indul, és kész ---
        private async Task EnsureAdminTokenAsync()
        {
            if (!string.IsNullOrWhiteSpace(_adminToken) &&
                _httpClient.DefaultRequestHeaders.Authorization != null)
            {
                return;
            }

            var httpResponse = await _httpClient.PostAsJsonAsync(
                $"{BaseUrl}/admin-token",
                new { serviceKey = AdminServiceKey }
            );

            var raw = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Admin token hiba ({(int)httpResponse.StatusCode}): {raw}");
            }

            using var doc = JsonDocument.Parse(raw);

            if (!doc.RootElement.TryGetProperty("token", out var tokenProp))
                throw new Exception("Admin token válasz nem tartalmaz 'token' mezőt.");

            _adminToken = tokenProp.GetString();

            if (string.IsNullOrWhiteSpace(_adminToken))
                throw new Exception("Admin token üresen érkezett.");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _adminToken);
        }

        // Összes user admin nézettel
        public async Task<List<UserDataAdminDto>> GetAllUsersAdminAsync()
        {
            await EnsureAdminTokenAsync();

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<UserDataAdminDto>>>(
                $"{BaseUrl}/admin"
            );

            return response?.Result ?? new List<UserDataAdminDto>();
        }

        // Új user létrehozása (POST)
        public async Task<UserDataAdminDto?> CreateUserAdminAsync(UserCreateAdminDto dto)
        {
            await EnsureAdminTokenAsync();

            var httpResponse = await _httpClient.PostAsJsonAsync($"{BaseUrl}/admin", dto);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception($"API hiba ({(int)httpResponse.StatusCode}): {error}");
            }

            var apiResponse =
                await httpResponse.Content.ReadFromJsonAsync<ApiResponse<UserDataAdminDto>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

            return apiResponse?.Result;
        }

        // Egy user lekérése ID alapján (GET)
        public async Task<UserDataAdminDto?> GetUserAdminByIdAsync(Guid id)
        {
            await EnsureAdminTokenAsync();

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<UserDataAdminDto>>(
                $"{BaseUrl}/admin/{id}"
            );

            return response?.Result;
        }

        // User + score-ok módosítása (PUT)
        public async Task UpdateUserAdminAsync(Guid id, UserUpdateAdminDto dto)
        {
            await EnsureAdminTokenAsync();

            var httpResponse = await _httpClient.PutAsJsonAsync($"{BaseUrl}/admin/{id}", dto);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception($"API hiba ({(int)httpResponse.StatusCode}): {error}");
            }
        }

        // User törlése (DELETE)
        public async Task DeleteUserAdminAsync(Guid id)
        {
            await EnsureAdminTokenAsync();

            var httpResponse = await _httpClient.DeleteAsync($"{BaseUrl}/admin/{id}");

            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception($"API hiba ({(int)httpResponse.StatusCode}): {error}");
            }
        }
    }

    // WebAPI válasz wrapper: { message: "...", result: ... }
    public class ApiResponse<T>
    {
        public string? Message { get; set; }
        public T? Result { get; set; }
    }
}
