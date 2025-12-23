using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HospitalNet.Backend.Infrastructure
{
    public sealed class AiReportClient
    {
        private readonly HttpClient _http;
        private readonly string _model;

        public AiReportClient(HttpClient httpClient, string model)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _model = string.IsNullOrWhiteSpace(model) ? throw new ArgumentNullException(nameof(model)) : model;
        }

        public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentNullException(nameof(prompt));

            var payload = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync("chat/completions", content, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"AI API error ({(int)response.StatusCode}): {body}");
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var choices = root.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                    throw new Exception("AI response has no choices.");

                var message = choices[0].GetProperty("message");
                var result = message.GetProperty("content").GetString();
                return result ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse AI response: {ex.Message}\nRaw: {body}", ex);
            }
        }

        public static HttpClient CreateOpenAiCompatibleClient(string baseUrl, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                baseUrl += "/";
            }

            var http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl, UriKind.Absolute),
                Timeout = TimeSpan.FromSeconds(60)
            };

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            return http;
        }
    }
}
