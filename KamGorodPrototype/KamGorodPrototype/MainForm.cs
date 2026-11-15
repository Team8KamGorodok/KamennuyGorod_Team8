using KamGorodPrototype.Models;
using KamGorodPrototype.Services;

namespace KamGorodPrototype{
    public partial class MainForm : Form{
        private static readonly string[] SupportedExtensions ={
            ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".opus", ".flac",
            ".wma", ".amr", ".webm", ".mp4", ".mkv"
        };

        private readonly List<AudioFileItem> _files = new();
        private readonly Dictionary<AudioFileItem, ListViewItem> _rows = new();
        private readonly PythonTranscriber? _transcriber;
        private CancellationTokenSource? _cts;
        private bool _closeAfterRun;

        private bool IsRunning => _cts != null;

        public MainForm(){
            InitializeComponent();

            btnAddFile.Enabled = true;
            btnAddFile.Click += BtnAddFile_Click;
            btnRemove.Click += BtnRemove_Click;
            btnRun.Click += BtnRun_Click;
            FormClosing += MainForm_FormClosing;

            var repoRoot = PythonTranscriber.FindRepoRoot();
            _transcriber = repoRoot != null ? new PythonTranscriber(repoRoot) : null;

            listViewFiles.AllowDrop = true;
            listViewFiles.ShowItemToolTips = true;
            listViewFiles.DragEnter += ListViewFiles_DragEnter;
            listViewFiles.DragDrop += ListViewFiles_DragDrop;
            listViewFiles.SelectedIndexChanged += (_, _) => {
                UpdateButtons();
                ShowSelectedTranscript();
            };
            listViewFiles.KeyDown += ListViewFiles_KeyDown;

            UpdateButtons();
        }
        private void BtnAddFile_Click(object? sender, EventArgs e){
            var mask = string.Join(";", SupportedExtensions.Select(x => "*" + x));
            using var dialog = new OpenFileDialog{
                Title = "Выберите записи звонков",
                Filter = $"Аудио и видео ({string.Join(", ", SupportedExtensions)})|{mask}|Все файлы (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
                AddFiles(dialog.FileNames, checkExtension: false);
        }

        private void ListViewFiles_DragEnter(object? sender, DragEventArgs e){
            e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void ListViewFiles_DragDrop(object? sender, DragEventArgs e){
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
                BeginInvoke(() => AddFiles(paths, checkExtension: true));
        }
        private void AddFiles(IEnumerable<string> paths, bool checkExtension){
            if (IsRunning)
                return;

            var skipped = new List<string>();

            listViewFiles.BeginUpdate();
            foreach (var path in paths){
                var name = Path.GetFileName(path);
                if (Directory.Exists(path)){
                    skipped.Add($"{name} — это папка");
                    continue;
                }
                if (!File.Exists(path)){
                    skipped.Add($"{name} — файл не найден");
                    continue;
                }
                if (checkExtension && !SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant())){
                    skipped.Add($"{name} — формат не поддерживается");
                    continue;
                }
                if (_files.Any(f => string.Equals(f.FilePath, path, StringComparison.OrdinalIgnoreCase))){
                    skipped.Add($"{name} — уже в списке");
                    continue;
                }

                var item = new AudioFileItem(path, new FileInfo(path).Length);
                if (item.SizeBytes == 0){
                    item.Status = FileStatus.Error;
                    item.Error = "пустой файл";
                }

                _files.Add(item);
                AddRow(item);
            }
            listViewFiles.EndUpdate();

            UpdateButtons();

            if (skipped.Count > 0){
                MessageBox.Show(this,
                    "Не добавлены:\n\n" + string.Join("\n", skipped),
                    "Добавление файлов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        private void BtnRemove_Click(object? sender, EventArgs e) => RemoveSelected();

        private void ListViewFiles_KeyDown(object? sender, KeyEventArgs e){
            if (e.KeyCode == Keys.Delete && !IsRunning)
                RemoveSelected();
        }

        private void RemoveSelected(){
            if (IsRunning || listViewFiles.SelectedItems.Count == 0)
                return;

            listViewFiles.BeginUpdate();
            foreach (var row in listViewFiles.SelectedItems.Cast<ListViewItem>().ToList()){
                var item = (AudioFileItem)row.Tag!;
                _files.Remove(item);
                _rows.Remove(item);
                listViewFiles.Items.Remove(row);
            }
            listViewFiles.EndUpdate();

            UpdateButtons();
            ShowSelectedTranscript();
        }

        private async void BtnRun_Click(object? sender, EventArgs e){
            if (IsRunning)
                return;

            var setupError = _transcriber is null
                ? "Не найдена папка transcriber с transcribe.py.\nПрограмма ищет её вверх от папки, где лежит exe."
                : _transcriber.CheckSetup();
            if (setupError != null){
                MessageBox.Show(this, setupError, "Транскрибация недоступна", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var batch = _files.Where(f => f.CanRun).ToList();
            if (batch.Count == 0)
                return;

            foreach (var item in batch){
                item.Status = FileStatus.Queued;
                item.Progress = 0;
                item.Error = null;
                RefreshRow(item);
            }

            var byPath = batch.ToDictionary(f => NormalizePath(f.FilePath), StringComparer.OrdinalIgnoreCase);

            _cts = new CancellationTokenSource();
            UpdateButtons();

            try{
                await _transcriber!.RunAsync(
                    batch.Select(f => f.FilePath).ToList(),
                    ev => ApplyEvent(ev, byPath, batch),
                    _cts.Token);
            }
            catch (OperationCanceledException){

            }
            catch (Exception ex){
                MessageBox.Show(this, ex.Message, "Ошибка транскрибации", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally{
                var reason = _cts.IsCancellationRequested ? "обработка прервана" : "обработка не завершена";
                foreach (var item in batch.Where(f => f.Status is FileStatus.Queued or FileStatus.Processing)){
                    item.Status = FileStatus.Error;
                    item.Error = reason;
                    RefreshRow(item);
                }

                _cts.Dispose();
                _cts = null;
                UpdateButtons();

                if (_closeAfterRun)
                    BeginInvoke(Close);
            }
        }

        private void ApplyEvent(TranscriptionEvent ev, Dictionary<string, AudioFileItem> byPath, List<AudioFileItem> batch){
            if (ev.Event == "model_error"){
                foreach (var queued in batch.Where(f => f.Status is FileStatus.Queued or FileStatus.Processing)){
                    queued.Status = FileStatus.Error;
                    queued.Error = "модель не загрузилась: " + ev.Error;
                    RefreshRow(queued);
                }
                return;
            }

            if (ev.File is null || !byPath.TryGetValue(NormalizePath(ev.File), out var item))
                return;

            switch (ev.Event){
                case "file_started":
                    item.Status = FileStatus.Processing;
                    item.Progress = 0;
                    break;

                case "progress":
                    item.Progress = Math.Clamp(ev.Percent, 0, 100);
                    break;

                case "file_done":
                    item.Status = FileStatus.Done;
                    item.Progress = 100;
                    item.Duration = TimeSpan.FromSeconds(ev.Duration);
                    item.ProcessingSeconds = ev.Processing;
                    item.TxtPath = ev.Txt;
                    item.JsonPath = ev.Json;
                    break;

                case "file_error":
                    item.Status = FileStatus.Error;
                    item.Error = ev.Error;
                    break;

                default:
                    return;
            }

            RefreshRow(item);
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e){
            if (!IsRunning)
                return;

            e.Cancel = true;
            var answer = MessageBox.Show(this,
                "Идёт транскрибация. Прервать и выйти?\nУже готовые транскрипты сохранятся.",
                "Транскрибация не завершена",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer == DialogResult.Yes){
                _closeAfterRun = true;
                _cts!.Cancel();
            }
        }

        private static string NormalizePath(string path){
            try{
                return Path.GetFullPath(path);
            }
            catch (Exception){
                return path;
            }
        }

        private void AddRow(AudioFileItem item){
            var row = new ListViewItem(new[] { "", "", "", "" }) { Tag = item };
            _rows[item] = row;
            listViewFiles.Items.Add(row);
            RefreshRow(item);
        }
        private void RefreshRow(AudioFileItem item){
            if (!_rows.TryGetValue(item, out var row))
                return;

            row.SubItems[0].Text = item.FileName;
            row.SubItems[1].Text = FormatSize(item.SizeBytes);
            row.SubItems[2].Text = item.Duration is { } d ? d.ToString(@"mm\:ss") : "—";
            row.SubItems[3].Text = item.StatusText;
            row.ToolTipText = item.FilePath;
            row.ForeColor = item.Status == FileStatus.Error ? Color.Firebrick : SystemColors.WindowText;

            if (ReferenceEquals(item, SelectedFile))
                ShowSelectedTranscript();
        }

        private AudioFileItem? SelectedFile =>
            listViewFiles.SelectedItems.Count == 1 ? (AudioFileItem)listViewFiles.SelectedItems[0].Tag! : null;

        private void ShowSelectedTranscript(){
            var item = SelectedFile;
            richTextTranscript.Clear();

            if (item is null){
                lblTranscriptTitle.Text = "Транскрипт: файл не выбран";
                return;
            }

            lblTranscriptTitle.Text = item.FileName;

            switch (item.Status){
                case FileStatus.Done:
                    var title = item.FileName;
                    if (item.Duration is { } duration)
                        title += " · " + FormatDuration(duration);
                    if (item.ProcessingSeconds is { } seconds)
                        title += $" · распознано за {seconds:F0} с";
                    lblTranscriptTitle.Text = title;
                    ShowTranscriptFile(item.TxtPath);
                    break;

                case FileStatus.Error:
                    AppendColored("Ошибка: " + item.Error, Color.Firebrick);
                    break;

                case FileStatus.Processing:
                    AppendColored($"Идёт распознавание… {item.Progress}%", SystemColors.GrayText);
                    break;

                default:
                    AppendColored("Файл ещё не распознан", SystemColors.GrayText);
                    break;
            }
        }

        private void ShowTranscriptFile(string? txtPath){
            if (string.IsNullOrEmpty(txtPath) || !File.Exists(txtPath)){
                AppendColored("Файл транскрипта не найден: " + txtPath, Color.Firebrick);
                return;
            }

            string[] lines;
            try{
                lines = File.ReadAllLines(txtPath, System.Text.Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException){
                AppendColored("Не удалось прочитать транскрипт: " + ex.Message, Color.Firebrick);
                return;
            }

            richTextTranscript.SuspendLayout();
            foreach (var line in lines){
                var close = line.StartsWith('[') ? line.IndexOf(']') : -1;
                if (close > 0){
                    AppendColored(line[..(close + 1)], SystemColors.GrayText);
                    AppendColored(line[(close + 1)..] + "\n", SystemColors.WindowText);
                }
                else{
                    AppendColored(line + "\n", SystemColors.WindowText);
                }
            }
            richTextTranscript.ResumeLayout();

            richTextTranscript.SelectionStart = 0;
            richTextTranscript.ScrollToCaret();
        }

        private void AppendColored(string text, Color color){
            richTextTranscript.SelectionStart = richTextTranscript.TextLength;
            richTextTranscript.SelectionLength = 0;
            richTextTranscript.SelectionColor = color;
            richTextTranscript.AppendText(text);
            richTextTranscript.SelectionColor = richTextTranscript.ForeColor;
        }

        private static string FormatDuration(TimeSpan d) =>
            d.TotalHours >= 1 ? d.ToString(@"h\:mm\:ss") : d.ToString(@"mm\:ss");

        private void UpdateButtons(){
            var running = IsRunning;
            btnAddFile.Enabled = !running;
            btnRemove.Enabled = !running && listViewFiles.SelectedItems.Count > 0;
            btnRun.Enabled = !running && _files.Any(f => f.CanRun);
            listViewFiles.AllowDrop = !running;
        }

        private static string FormatSize(long bytes) => bytes switch{
            < 1024 => $"{bytes} Б",
            < 1024 * 1024 => $"{bytes / 1024.0:F0} КБ",
            _ => $"{bytes / 1024.0 / 1024.0:F1} МБ"
        };

        private void MainForm_Load(object sender, EventArgs e){

        }
    }
}
