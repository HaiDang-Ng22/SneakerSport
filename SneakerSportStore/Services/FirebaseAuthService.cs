using Newtonsoft.Json;
using SneakerSportStore.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SneakerSportStore.Services
{
    public class FirebaseAuthService
    {
        private readonly string _apiKey;
        private readonly string _dbUrl;
        private readonly HttpClient _httpClient;

        public FirebaseAuthService(string apiKey, string dbUrl)
        {
            _apiKey = apiKey;
            _dbUrl = dbUrl;
            _httpClient = new HttpClient();
        }

        public async Task<dynamic> SignInWithPassword(string email, string password)
        {
            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}",
                content
            );

            return await HandleResponse(response);
        }

        public async Task<dynamic> SignUp(string email, string password)
        {
            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}",
                content
            );

            return await HandleResponse(response);
        }

        public async Task SendVerificationEmail(string idToken)
        {
            var payload = new
            {
                requestType = "VERIFY_EMAIL",
                idToken
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={_apiKey}",
                content
            );
        }

        public async Task SendPasswordResetEmail(string email)
        {
            var payload = new
            {
                requestType = "PASSWORD_RESET",
                email
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={_apiKey}",
                content
            );
        }

        public async Task<KhachHang> GetUserData(string userId)
        {
            var response = await _httpClient.GetAsync($"{_dbUrl}/users/{userId}.json");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<KhachHang>(json);
        }

        public async Task SaveUserData(string userId, KhachHang userData)
        {
            var content = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"{_dbUrl}/users/{userId}.json", content);
        }

        public async Task<dynamic> ChangePassword(string idToken, string newPassword)
        {
            var payload = new
            {
                idToken,
                password = newPassword,
                returnSecureToken = true
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_apiKey}",
                content
            );

            return await HandleResponse(response);
        }

        public async Task<dynamic> SignInWithGoogle(string idToken)
        {
            var payload = new
            {
                postBody = $"id_token={idToken}",
                requestUri = "http://localhost", // Thay bằng domain của bạn
                returnIdpCredential = true,
                returnSecureToken = true
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={_apiKey}",
                content
            );

            return await HandleResponse(response);
        }

        public async Task DeleteUserAccount(string userId, string idToken)
        {
            // Xóa dữ liệu người dùng
            await _httpClient.DeleteAsync($"{_dbUrl}/users/{userId}.json");

            // Xóa tài khoản authentication
            var payload = new { idToken };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:delete?key={_apiKey}",
                content
            );
        }

        private async Task<dynamic> HandleResponse(HttpResponseMessage response)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Firebase error: {responseBody}");
            }
            return JsonConvert.DeserializeObject<dynamic>(responseBody);
        }
    }
}