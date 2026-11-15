using Whisper.net;
using Whisper.net.Ggml;
string modelFileName = "ggml-base.bin";
//В начале 1 раз надо запустить только 3 строчки ниже для установки модели. После этого нужно закомментировать и использовать всё, что ниже!!!
//using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Small); //Указывается тип модели!
//using var fileWriter = File.OpenWrite(modelFileName);
//await modelStream.CopyToAsync(fileWriter);


using var whisperFactory = WhisperFactory.FromPath(modelFileName);
using var processor = whisperFactory.CreateBuilder()
    .WithLanguage("ru")  //Язык
    .WithPrompt("Звонят из образовательного центра Каменный город. Ранее они уже общались. Говорящие здороваются и прощаются друг с другом. Курсовая подготовка, планирование обучения, прошлое сотрудничество, инновационная площадка Сколково - основные темы диалога.")  // Важно! Не задачи! Примеры, уточнения и тд
    .Build();

using var fileStream = File.OpenRead("C:/Users/saras/Downloads/моп_2_звонок_1.wav"); //Полный путь до файла

await foreach (var result in processor.ProcessAsync(fileStream))
{
    Console.WriteLine($"{result.Start} -> {result.End}: {result.Text}");
}