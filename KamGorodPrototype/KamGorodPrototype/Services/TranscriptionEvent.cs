using System.Text.Json;
using System.Text.Json.Serialization;

namespace KamGorodPrototype.Services
{
    /// <summary>
    /// Одно событие от transcribe.py — одна JSON-строка из его stdout.
    /// Описание событий: transcriber\README.md.
    /// </summary>
    public sealed class TranscriptionEvent
    {
        private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

        /// <summary>model_loaded, model_error, file_started, progress, file_done, file_error, batch_done.</summary>
        [JsonPropertyName("event")] public string Event { get; set; } = "";

        [JsonPropertyName("file")] public string? File { get; set; }
        [JsonPropertyName("percent")] public int Percent { get; set; }
        [JsonPropertyName("json")] public string? Json { get; set; }
        [JsonPropertyName("txt")] public string? Txt { get; set; }
        [JsonPropertyName("duration")] public double Duration { get; set; }
        [JsonPropertyName("processing")] public double Processing { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }

        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("ok")] public int Ok { get; set; }
        [JsonPropertyName("failed")] public int Failed { get; set; }

        /// <summary>Разбирает строку stdout. Не JSON или без поля event — null (строка пропускается).</summary>
        public static TranscriptionEvent? TryParse(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line[0] != '{')
                return null;
            try
            {
                var ev = JsonSerializer.Deserialize<TranscriptionEvent>(line, Options);
                return string.IsNullOrEmpty(ev?.Event) ? null : ev;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
