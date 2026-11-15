import argparse
import json
import os
import sys
import time
import traceback
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
PROMPT_FILE = SCRIPT_DIR / "prompt.txt"
HOTWORDS_FILE = SCRIPT_DIR / "hotwords.txt"

def setup_output() -> None:
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")


def emit(event: str, **data) -> None:
    sys.stdout.write(json.dumps({"event": event, **data}, ensure_ascii=False) + "\n")
    sys.stdout.flush()


def log(message: str) -> None:
    print(message, file=sys.stderr, flush=True)

def setup_cuda_dlls() -> None:
    if os.name != "nt":
        return
    try:
        import nvidia 
    except ImportError:
        log("Пакеты nvidia-*-cu12 не установлены — для --device cuda нужны библиотеки CUDA.")
        return

    for root in nvidia.__path__:
        for sub in ("cublas", "cudnn", "cuda_runtime"):
            bin_dir = Path(root) / sub / "bin"
            if bin_dir.is_dir():
                os.add_dll_directory(str(bin_dir))
                os.environ["PATH"] = str(bin_dir) + os.pathsep + os.environ.get("PATH", "")

def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Транскрибация записей локальной моделью Whisper")
    p.add_argument("files", nargs="+", help="файлы записей (аудио или видео)")
    p.add_argument("--model", required=True, help="путь к папке модели, например ..\\models\\small")
    p.add_argument("--output-dir", required=True, help="куда сохранять .json и .txt")
    p.add_argument("--device", choices=["cpu", "cuda"], default="cpu")
    p.add_argument("--compute-type", default=None,
                   help="по умолчанию int8 для cpu и float16 для cuda")
    p.add_argument("--language", default="ru", help="код языка или auto")
    p.add_argument("--initial-prompt", default=None, help="подсказка: термины, названия курсов")
    p.add_argument("--hotwords", default=None, help="слова, которые модель должна узнавать")
    p.add_argument("--no-prompt", action="store_true",
                   help="не использовать подсказку и hotwords (для сравнения качества)")
    p.add_argument("--no-vad", action="store_true", help="не вырезать тишину (фильтр VAD включён по умолчанию)")
    p.add_argument("--beam-size", type=int, default=5)
    return p.parse_args()

def read_text_file(path: Path) -> str | None:
    if not path.is_file():
        return None
    text = " ".join(path.read_text(encoding="utf-8-sig").split())
    return text or None


def resolve_prompt(args) -> None:
    if args.no_prompt:
        args.initial_prompt = None
        args.hotwords = None
        return
    if args.initial_prompt is None:
        args.initial_prompt = read_text_file(PROMPT_FILE)
    if args.hotwords is None:
        args.hotwords = read_text_file(HOTWORDS_FILE)


def fmt_time(seconds: float) -> str:
    total = int(seconds)
    h, rest = divmod(total, 3600)
    m, s = divmod(rest, 60)
    return f"{h}:{m:02d}:{s:02d}" if h else f"{m:02d}:{s:02d}"


def unique_stem(stem: str, used: set) -> str:
    """Два файла с одинаковым именем из разных папок не перезапишут друг друга."""
    name, n = stem, 2
    while name.lower() in used:
        name = f"{stem}_{n}"
        n += 1
    used.add(name.lower())
    return name

def transcribe_file(model, path: Path, args, out_dir: Path, used_names: set, model_name: str) -> None:
    if not path.is_file():
        raise FileNotFoundError("файл не найден")
    if path.stat().st_size == 0:
        raise ValueError("пустой файл")

    started = time.perf_counter()
    language = None if args.language == "auto" else args.language

    segments_iter, info = model.transcribe(
        str(path),
        language=language,
        initial_prompt=args.initial_prompt or None,
        hotwords=args.hotwords or None,
        vad_filter=not args.no_vad,
        beam_size=args.beam_size,
    )
    duration = float(info.duration or 0)

    segments = []
    last_percent = -1
    for s in segments_iter:
        segments.append({
            "start": round(s.start, 2),
            "end": round(s.end, 2),
            "text": s.text.strip(),
            "avg_logprob": round(s.avg_logprob, 3),
            "no_speech_prob": round(s.no_speech_prob, 3),
        })
        if duration > 0:
            percent = min(99, int(s.end / duration * 100))
            if percent != last_percent:
                emit("progress", file=str(path), percent=percent)
                last_percent = percent

    processing = time.perf_counter() - started

    stem = unique_stem(path.stem, used_names)
    json_path = out_dir / f"{stem}.json"
    txt_path = out_dir / f"{stem}.txt"

    json_path.write_text(json.dumps({
        "source": str(path),
        "model": model_name,
        "device": args.device,
        "language": info.language,
        "duration": round(duration, 2),
        "processing_seconds": round(processing, 2),
        "initial_prompt": args.initial_prompt,
        "hotwords": args.hotwords,
        "vad": not args.no_vad,
        "segments": segments,
    }, ensure_ascii=False, indent=2), encoding="utf-8")

    lines = [f"[{fmt_time(s['start'])} – {fmt_time(s['end'])}] {s['text']}" for s in segments]
    txt_path.write_text("\n".join(lines) + "\n", encoding="utf-8")

    emit("file_done",
         file=str(path),
         json=str(json_path),
         txt=str(txt_path),
         duration=round(duration, 2),
         processing=round(processing, 2),
         segments=len(segments),
         language=info.language)

def main() -> int:
    setup_output()
    args = parse_args()
    resolve_prompt(args)

    if args.compute_type is None:
        args.compute_type = "float16" if args.device == "cuda" else "int8"

    model_dir = Path(args.model)
    if not model_dir.is_dir():
        emit("model_error", error=f"папка модели не найдена: {model_dir}")
        return 1

    if args.device == "cuda":
        setup_cuda_dlls()

    out_dir = Path(args.output_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    from faster_whisper import WhisperModel 

    try:
        t0 = time.perf_counter()
        model = WhisperModel(str(model_dir), device=args.device, compute_type=args.compute_type)
        emit("model_loaded",
             model=model_dir.name,
             device=args.device,
             compute_type=args.compute_type,
             seconds=round(time.perf_counter() - t0, 2),
             initial_prompt=args.initial_prompt,
             hotwords=args.hotwords,
             vad=not args.no_vad)
    except Exception as e: 
        log(traceback.format_exc())
        hint = " (проверьте библиотеки CUDA или запустите с --device cpu)" if args.device == "cuda" else ""
        emit("model_error", error=f"{e}{hint}")
        return 1

    batch_start = time.perf_counter()
    used_names: set = set()
    ok = failed = 0

    for file_arg in args.files:
        path = Path(file_arg)
        emit("file_started", file=str(path))
        try:
            transcribe_file(model, path, args, out_dir, used_names, model_dir.name)
            ok += 1
        except Exception as e:
            failed += 1
            log(f"Ошибка в файле {path}:\n{traceback.format_exc()}")
            emit("file_error", file=str(path), error=str(e) or e.__class__.__name__)

    emit("batch_done",
         total=len(args.files),
         ok=ok,
         failed=failed,
         seconds=round(time.perf_counter() - batch_start, 2))
    return 0

if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        sys.exit(130)
