    using System.Net.Http;
    using System.Threading.Tasks;

    namespace SneakerSportStore.Extensions
    {
        /// <summary>
        /// Cung cấp phương thức PatchAsync cho HttpClient (REST Firebase cần).
        /// </summary>
        public static class HttpClientPatchExtension
        {
            /// <summary>
            /// Gửi HTTP PATCH request.
            /// </summary>
            /// <param name="client">HttpClient đã được cấu hình BaseAddress.</param>
            /// <param name="requestUri">Đường dẫn (có thể tương đối hoặc tuyệt đối).</param>
            /// <param name="content">Nội dung JSON (StringContent...)</param>
            /// <returns>HttpResponseMessage</returns>
            public static Task<HttpResponseMessage> PatchAsync(
                this HttpClient client,
                string requestUri,
                HttpContent content)
            {
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri)
                {
                    Content = content
                };
                return client.SendAsync(request);
            }
        }
    }
