@echo off
chcp 65001 >nul
setlocal

rem Сборка пакета для заказчика: dist\Prototype.zip
rem Заказчик распаковывает архив и запускает KamGorodPrototype.exe. Ничего ставить не нужно.
rem Нужно на ПК разработчика: .NET SDK, transcriber\.venv с зависимостями, models\small, интернет (один раз для PyInstaller).

set "ROOT=%~dp0"
set "PY=%ROOT%transcriber\.venv\Scripts\python.exe"
set "DIST=%ROOT%dist"
set "PKG=%DIST%\Prototype"
set "WORK=%ROOT%build"

if not exist "%PY%" (
    echo [ОШИБКА] Нет окружения Python: %PY%
    goto fail
)
if not exist "%ROOT%models\small\model.bin" (
    echo [ОШИБКА] Нет модели: %ROOT%models\small
    goto fail
)

echo.
echo === 1/6 Очистка dist и build ===
if exist "%DIST%" rmdir /s /q "%DIST%"
if exist "%WORK%" rmdir /s /q "%WORK%"
mkdir "%PKG%" || goto fail

echo.
echo === 2/6 Публикация программы (.NET внутри exe) ===
dotnet publish "%ROOT%KamGorodPrototype\KamGorodPrototype\KamGorodPrototype.csproj" ^
    -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none ^
    -o "%PKG%" || goto fail

echo.
echo === 3/6 Сборка transcribe.exe (Python внутри) ===
"%PY%" -m pip install --quiet pyinstaller || goto fail
"%PY%" -m PyInstaller --noconfirm --clean --onedir --console --name transcribe ^
    --distpath "%PKG%\transcriber" --workpath "%WORK%\pyinstaller" --specpath "%WORK%" ^
    --collect-all faster_whisper --collect-all ctranslate2 --collect-all av ^
    --collect-all onnxruntime --collect-all tokenizers ^
    "%ROOT%transcriber\transcribe.py" || goto fail

echo.
echo === 4/6 Подсказка, ключевые слова и модель ===
copy /y "%ROOT%transcriber\prompt.txt" "%PKG%\transcriber\" >nul || goto fail
copy /y "%ROOT%transcriber\hotwords.txt" "%PKG%\transcriber\" >nul || goto fail
robocopy "%ROOT%models\small" "%PKG%\models\small" /E /NFL /NDL /NJH /NJS /NP >nul
if errorlevel 8 goto fail

echo.
echo === 5/6 Проверка transcribe.exe (загрузка модели) ===
"%PKG%\transcriber\transcribe\transcribe.exe" --model "%PKG%\models\small" --output-dir "%WORK%\smoke" "%WORK%\no_such_file.mp3"
if errorlevel 1 (
    echo [ОШИБКА] transcribe.exe не смог загрузить модель — смотрите вывод выше.
    goto fail
)
if exist "%PKG%\output" rmdir /s /q "%PKG%\output"

echo.
echo === 6/6 Упаковка в zip ===
powershell -NoProfile -Command "Compress-Archive -Path '%PKG%' -DestinationPath '%DIST%\Prototype.zip' -Force" || goto fail

echo.
echo Готово: %DIST%\Prototype.zip
endlocal
exit /b 0

:fail
echo.
echo Сборка прервана.
endlocal
exit /b 1
