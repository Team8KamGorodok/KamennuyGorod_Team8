namespace KamGorodPrototype.Models
{
    /// <summary>
    /// Состояние файла в списке.
    /// Добавлен → В очереди → В работе (N%) → Готово / Ошибка.
    /// Повторный запуск берёт файлы в состояниях «Добавлен» и «Ошибка».
    /// </summary>
    public enum FileStatus
    {
        Added,
        Queued,
        Processing,
        Done,
        Error
    }

    /// <summary>
    /// Один файл записи в списке. Список таких объектов — единственный источник данных,
    /// ListView на форме только показывает его.
    /// </summary>
    public class AudioFileItem
    {
        public AudioFileItem(string filePath, long sizeBytes)
        {
            FilePath = filePath;
            SizeBytes = sizeBytes;
        }

        public string FilePath { get; }
        public long SizeBytes { get; }
        public string FileName => Path.GetFileName(FilePath);

        public FileStatus Status { get; set; } = FileStatus.Added;

        /// <summary>Процент распознавания (0–100), пока Status = Processing.</summary>
        public int Progress { get; set; }

        /// <summary>Текст ошибки, если Status = Error.</summary>
        public string? Error { get; set; }

        /// <summary>Длительность записи. Сообщает Python по готовности файла.</summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>Сколько секунд шло распознавание. Сообщает Python по готовности файла.</summary>
        public double? ProcessingSeconds { get; set; }

        /// <summary>Готовый транскрипт: читаемый текст и структурированный вариант.</summary>
        public string? TxtPath { get; set; }
        public string? JsonPath { get; set; }

        /// <summary>Какой моделью распознан (small, medium…).</summary>
        public string? Model { get; set; }

        /// <summary>
        /// Файл попадёт в запуск выбранной моделью: новый, с ошибкой
        /// или уже распознанный, но другой моделью (для сравнения small / medium).
        /// </summary>
        public bool CanRunWith(string model) =>
            Status is FileStatus.Added or FileStatus.Error ||
            (Status == FileStatus.Done && !string.Equals(Model, model, StringComparison.OrdinalIgnoreCase));

        public string StatusText => Status switch
        {
            FileStatus.Added => "Добавлен",
            FileStatus.Queued => "В очереди",
            FileStatus.Processing => $"В работе {Progress}%",
            FileStatus.Done => "Готово",
            FileStatus.Error => "Ошибка: " + Error,
            _ => Status.ToString()
        };
    }
}
