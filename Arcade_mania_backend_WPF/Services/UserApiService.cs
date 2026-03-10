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

        // ha más porton fut, át kell írni
        private const string BaseUrl = "http://localhost:5118/api/Users";

        private string? _adminToken;

        public bool IsAdminLoggedIn =>
            !string.IsNullOrWhiteSpace(_adminToken) &&
            _httpClient.DefaultRequestHeaders.Authorization != null;

        public UserApiService()
        {

            _httpClient = new HttpClient();

        }

        // ADMIN LOGIN (JWT)
        public async Task<bool> AdminLoginAsync(string name, string password)
        {

            var httpResponse = await _httpClient.PostAsJsonAsync(
                $"{BaseUrl}/admin/login",
                new { name, password }
            );

            var raw = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {

                return false;

            }

            using var doc = JsonDocument.Parse(raw);

            if (!doc.RootElement.TryGetProperty("token", out var tokenProp))
            {

                return false;

            }

            _adminToken = tokenProp.GetString();

            if (string.IsNullOrWhiteSpace(_adminToken))
            {

                return false;

            }
                
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _adminToken);

            return true;
        }

        public void Logout()
        {

            _adminToken = null;

            _httpClient.DefaultRequestHeaders.Authorization = null;

        }

        private void EnsureAdminLoggedIn()
        {

            if (!IsAdminLoggedIn)
            {

                throw new Exception("Admin nincs bejelentkezve. Előbb: AdminLoginAsync(name, password)");

            }
        }

        public async Task<List<UserDataAdminDto>> GetAllUsersAdminAsync()
        {

            EnsureAdminLoggedIn();

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<UserDataAdminDto>>>(
                $"{BaseUrl}/admin"
            );

            return response?.Result ?? new List<UserDataAdminDto>();
        }

        public async Task<UserDataAdminDto?> GetUserAdminByIdAsync(Guid id)
        {

            EnsureAdminLoggedIn();

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<UserDataAdminDto>>(
                $"{BaseUrl}/admin/{id}"
            );

            return response?.Result;
        }

        public async Task<UserDataAdminDto?> CreateUserAdminAsync(UserCreateAdminDto dto)
        {

            EnsureAdminLoggedIn();

            var httpResponse = await _httpClient.PostAsJsonAsync($"{BaseUrl}/admin", dto);
            var raw = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {

                throw new Exception(raw);

            }
                
            var response = JsonSerializer.Deserialize<ApiResponse<UserDataAdminDto>>(
                raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return response?.Result;
        }

        public async Task<bool> UpdateUserAdminAsync(Guid id, UserUpdateAdminDto dto)
        {

            EnsureAdminLoggedIn();

            var httpResponse = await _httpClient.PutAsJsonAsync($"{BaseUrl}/admin/{id}", dto);
            var raw = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {

                throw new Exception(raw);

            }
                
            return true;
        }

        public async Task<bool> DeleteUserAdminAsync(Guid id)
        {

            EnsureAdminLoggedIn();

            var httpResponse = await _httpClient.DeleteAsync($"{BaseUrl}/admin/{id}");
            var raw = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {

                throw new Exception(raw);

            }

            return true;
        }
    }

    public class ApiResponse<T>
    {
        public string? Message { get; set; }
        public T? Result { get; set; }
    }
}
