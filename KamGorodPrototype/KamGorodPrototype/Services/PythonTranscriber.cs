using System.Diagnostics;
using System.Text;

namespace KamGorodPrototype.Services
{
    /// <summary>
    /// Запускает распознавание и передаёт его события в форму.
    /// Готовый пакет для заказчика: transcriber\transcribe\transcribe.exe (Python внутри, собран build.bat).
    /// Разработка: transcriber\.venv\Scripts\python.exe + transcriber\transcribe.py.
    /// Один запуск = один пакет файлов: модель загружается один раз на весь пакет.
    /// </summary>
    public sealed class PythonTranscriber
    {
        /// <summary>Модель из папки models\. Выбор модели в интерфейсе — позже.</summary>
        public const string ModelName = "small";

        public PythonTranscriber(string repoRoot)
        {
            RepoRoot = repoRoot;
        }

        // ---------- Пути (все — от корня репозитория) ----------

        public string RepoRoot { get; }
        public string PythonExe => Path.Combine(RepoRoot, "transcriber", ".venv", "Scripts", "python.exe");
        public string ScriptPath => Path.Combine(RepoRoot, "transcriber", "transcribe.py");
        public string PackagedExe => Path.Combine(RepoRoot, "transcriber", "transcribe", "transcribe.exe");

        /// <summary>true — работаем из собранного пакета (transcribe.exe), false — из исходников.</summary>
        public bool IsPackaged => File.Exists(PackagedExe);
        public string ModelDir => Path.Combine(RepoRoot, "models", ModelName);
        public string OutputDir => Path.Combine(RepoRoot, "output");

        /// <summary>
        /// Ищет корень вверх от папки exe: первая папка, где есть transcriber\transcribe\transcribe.exe
        /// (пакет — exe лежит прямо в ней) или transcriber\transcribe.py (разработка — bin\Debug\...).
        /// </summary>
        public static string? FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "transcriber", "transcribe", "transcribe.exe")) ||
                    File.Exists(Path.Combine(dir.FullName, "transcriber", "transcribe.py")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }

        /// <summary>Проверка перед запуском. null — всё на месте, иначе текст для пользователя.</summary>
        public string? CheckSetup()
        {
            if (IsPackaged)
            {
                return Directory.Exists(ModelDir)
                    ? null
                    : $"Не найдена модель распознавания:\n{ModelDir}\n\nРаспакуйте архив целиком и запустите программу из распакованной папки.";
            }

            if (!File.Exists(PythonExe))
                return $"Не найдено окружение Python:\n{PythonExe}\n\nСоздайте его по инструкции transcriber\\README.md.";
            if (!File.Exists(ScriptPath))
                return $"Не найден скрипт:\n{ScriptPath}";
            if (!Directory.Exists(ModelDir))
                return $"Не найдена модель «{ModelName}»:\n{ModelDir}\n\nСкачайте её по инструкции transcriber\\README.md.";
            return null;
        }

        // ---------- Запуск ----------

        /// <summary>
        /// Распознаёт файлы. Каждое событие сразу передаётся в onEvent — в потоке формы,
        /// потому что вызывающий код ждёт этот метод из обработчика кнопки.
        /// Отмена через ct завершает процесс Python.
        /// Если Python завершился без batch_done — исключение с концом его stderr.
        /// </summary>
        public async Task RunAsync(IReadOnlyList<string> files, Action<TranscriptionEvent> onEvent, CancellationToken ct)
        {
            Directory.CreateDirectory(OutputDir);

            var psi = new ProcessStartInfo(IsPackaged ? PackagedExe : PythonExe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = Path.Combine(RepoRoot, "transcriber")
            };
            // ArgumentList сам расставляет кавычки — пробелы и кириллица в путях не ломаются.
            if (!IsPackaged)
                psi.ArgumentList.Add(ScriptPath);
            psi.ArgumentList.Add("--model");
            psi.ArgumentList.Add(ModelDir);
            psi.ArgumentList.Add("--output-dir");
            psi.ArgumentList.Add(OutputDir);
            foreach (var file in files)
                psi.ArgumentList.Add(file);

            psi.Environment["PYTHONIOENCODING"] = "utf-8";
            psi.Environment["PYTHONUNBUFFERED"] = "1";

            using var process = new Process { StartInfo = psi };

            // stderr читаем параллельно (иначе буфер заполнится и Python встанет), храним последние строки.
            var stderrTail = new Queue<string>();
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                lock (stderrTail)
                {
                    stderrTail.Enqueue(e.Data);
                    while (stderrTail.Count > 30) stderrTail.Dequeue();
                }
            };

            process.Start();
            process.BeginErrorReadLine();

            using var killOnCancel = ct.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // процесс уже завершился
                }
            });

            var batchDone = false;
            string? line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                var ev = TranscriptionEvent.TryParse(line);
                if (ev is null)
                    continue;
                if (ev.Event == "batch_done")
                    batchDone = true;
                onEvent(ev);
            }

            await process.WaitForExitAsync();
            ct.ThrowIfCancellationRequested();

            if (!batchDone)
            {
                string tail;
                lock (stderrTail) tail = string.Join("\n", stderrTail.TakeLast(10));
                throw new InvalidOperationException(
                    $"Python завершился без результата (код {process.ExitCode})." +
                    (tail.Length > 0 ? "\n\n" + tail : ""));
            }
        }
    }
}
