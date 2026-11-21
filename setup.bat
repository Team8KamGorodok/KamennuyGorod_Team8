@echo off
chcp 65001 >nul
setlocal

rem Подготовка проекта после клонирования: окружение Python, зависимости, модели Whisper.
rem Запуск: setup.bat            — модели small и medium
rem         setup.bat small      — только small (быстрее, ~0.5 ГБ)
rem Повторный запуск безопасен: то, что уже есть, пропускается.
rem Нужно: Python 3.10–3.12 и интернет.

set "ROOT=%~dp0"
set "VENV=%ROOT%transcriber\.venv"
set "PY=%VENV%\Scripts\python.exe"
set "MODELS=%*"
if "%MODELS%"=="" set "MODELS=small medium"

echo.
echo === 1/3 Окружение Python (transcriber\.venv) ===
if exist "%PY%" goto venv_ok
call :find_python || goto fail
echo   создаю окружение через: %BASEPY%
%BASEPY% -m venv "%VENV%" || goto fail
goto venv_done
:venv_ok
echo   уже есть
:venv_done

echo.
echo === 2/3 Зависимости (transcriber\requirements.txt) ===
"%PY%" -m pip install --disable-pip-version-check --quiet -r "%ROOT%transcriber\requirements.txt" || goto fail
echo   установлены

echo.
echo === 3/3 Модели Whisper (models\) ===
for %%M in (%MODELS%) do (
    call :get_model %%M || goto fail
)

echo.
echo Готово. Можно открывать KamGorodPrototype\KamGorodPrototype.slnx и запускать,
echo или собрать пакет для заказчика: build.bat
endlocal
exit /b 0

:find_python
py -3.12 -c "import sys" >nul 2>&1 && (set "BASEPY=py -3.12" & exit /b 0)
python -c "import sys; sys.exit(0 if sys.version_info[:2] >= (3, 10) else 1)" >nul 2>&1 && (set "BASEPY=python" & exit /b 0)
echo [ОШИБКА] Не найден Python 3.10–3.12. Установите: winget install Python.Python.3.12
exit /b 1

:get_model
if exist "%ROOT%models\%1\model.bin" (
    echo   %1 — уже есть
    exit /b 0
)
echo   %1 — скачивание с Hugging Face...
"%PY%" -c "import sys; from huggingface_hub import snapshot_download; snapshot_download(sys.argv[1], local_dir=sys.argv[2])" Systran/faster-whisper-%1 "%ROOT%models\%1" || exit /b 1
exit /b 0

:fail
echo.
echo Подготовка прервана.
endlocal
exit /b 1
