import json
import pathlib
import subprocess
import sys

root = pathlib.Path(__file__).resolve().parent
run = root / (sys.argv[1] if len(sys.argv) > 1 else "attempt_01")
stats = json.loads((run / "capture_stats.json").read_text())
ffmpeg = (root / "ffmpeg_path.txt").read_text().strip()
duration = stats["frames"] / stats["fps"]
output = root / "VelvetVows_Full_Playthrough.mp4"
if stats["encoderExitCode"] != 0 or stats["audioSamples"] <= 0 or stats["audioPeak"] < .00001:
    raise RuntimeError("The recording must contain successfully encoded video and audible audio.")
audio_tempo = stats["audioDuration"] / max(.001, duration - max(0, stats["firstAudioDelay"]))

chapter_names = {
    "Phase2A/Opening/Dormant": "First meeting",
    "Phase3AIntro/Opening/Dormant": "Evelyn's photographs and diary",
    "Phase2A/MissingStudent/Dormant": "The missing student",
    "Phase2A/Overhearing/Dormant": "Celeste and Isabelle",
    "Phase2A/Watching/Dormant": "Watching Adrian in the Library",
    "Phase2A/Errands/Dormant": "Reputation errands",
    "Phase2A/RibbonSearch/Dormant": "Celeste's ribbon and the sketchbook",
    "Phase2A/FollowCeleste/Dormant": "Lead Celeste to the Art Studio",
    "Phase2A/Witness/Dormant": "The Art Studio witness",
    "Phase2A/Confront/Dormant": "Confrontation",
    "Phase2BCinematic/Complete/Dormant": "The murder",
    "Phase2BCleanup/Complete/Dormant": "Clean up the evidence",
    "Phase2BOutcome/Complete/Dormant": "After the cleanup",
    "Phase2CBall/Complete/Establishing": "The Black Rose Ball",
    "Phase2CBall/Complete/Dance": "The dance",
    "Phase2CBall/Complete/Revelation": "Adrian's confession and diary",
    "Phase2CEnding/Complete/Ending": "A Perfect Duet",
}
events = []
for line in (run / "timeline.tsv").read_text(encoding="utf-8-sig").splitlines()[1:]:
    seconds, kind, text = line.split("\t", 2)
    events.append((float(seconds), kind, text))
chapters = [(0.0, "Main menu")]
for seconds, kind, text in events:
    if kind == "state" and text in chapter_names:
        chapters.append((seconds, chapter_names[text]))
    elif kind == "choice":
        chapters.append((seconds, text))
meta = [";FFMETADATA1", "title=Velvet Vows — full recorded playthrough"]
for i, (seconds, title) in enumerate(chapters):
    end = chapters[i + 1][0] if i + 1 < len(chapters) else duration
    if end <= seconds:
        continue
    meta.extend(["[CHAPTER]", "TIMEBASE=1/1000", f"START={round(seconds * 1000)}",
                 f"END={round(end * 1000)}", "title=" + title.replace("=", "\\=")])
metadata = run / "chapters.ffmeta"
metadata.write_text("\n".join(meta) + "\n")

command = [ffmpeg, "-hide_banner", "-y", "-i", str(run / "video_silent.mp4"),
           "-itsoffset", str(max(0, stats["firstAudioDelay"])),
           "-f", "f32le", "-ar", str(stats["audioSampleRate"]), "-ac", str(stats["audioChannels"]),
           "-i", str(run / "audio.f32"), "-i", str(metadata),
           "-filter_complex", f"[1:a]atempo={audio_tempo:.10f},apad[a]", "-map", "0:v:0", "-map", "[a]",
           "-map_metadata", "2", "-map_chapters", "2", "-t", str(duration),
           "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", str(output)]
subprocess.run(command, check=True)
with (root / "chapters.txt").open("w") as f:
    for seconds, title in chapters:
        minutes, second = divmod(int(seconds), 60)
        f.write(f"{minutes:02d}:{second:02d}  {title}\n")
print(output)
print(json.dumps(stats, indent=2))

def event_time(needle, kind=None):
    return next(t for t, k, text in events if needle in text and (kind is None or k == kind))

def stamp(seconds):
    minutes, second = divmod(int(seconds), 60)
    return f"{minutes:02d}:{second:02d}"

review_frames = root / "review_frames"
review_frames.mkdir(exist_ok=True)
findings = [
    (event_time("Phase3AIntro/Opening", "state") + 36,
     "intro_caption", "Подпись с датой накладывается на такую же подпись внутри фотографии Адриана.",
     "Текст в правом нижнем углу фотографии двоится."),
    (event_time("Girls like her have always had everything.", "dialogue") + 1,
     "camera_column", "Во время мыслей Эвелин при подслушивании камера попадает в колонну.",
     "Каменная поверхность закрывает героиню и почти весь кадр."),
    (event_time("MARGOT|Right now?", "dialogue") + .6,
     "margot_table", "Марго стоит внутри рабочего стола в мастерской.",
     "Столешница пересекает тело NPC."),
    (event_time("MARGOT|Right now?", "dialogue") + .6,
     "studio_easel", "Изображение на оставшемся мольберте за Марго перевёрнуто.",
     "Материал загружен, но ориентация рисунка неправильная."),
    (event_time("Phase2A/MissingStudent", "state") + 2,
     "suspicion_bar", "При SUSPICION 0% красная шкала отображается заполненной.",
     "Визуальная шкала противоречит числу; у Image заполнения отсутствует sprite."),
]
rows = []
for seconds, slug, problem, impact in findings:
    frame = review_frames / (slug + ".jpg")
    subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-ss", str(seconds),
                    "-i", str(output), "-frames:v", "1", str(frame)], check=True)
    rows.append(f"| {stamp(seconds)} | {problem} {impact} | [Кадр]({frame}) |")

report = f"""# Velvet Vows — запись полного прохождения

[Открыть видео]({output}) · [Главы]({root / 'chapters.txt'}) · [Результат Play Mode]({root / 'full_run_results.xml'})

Длительность: {stamp(round(duration))}. Запись: 15 кадров/с, H.264, стереозвук AAC 48 кГц.

Прохождение выполнено в Unity Play Mode автоматическим управлением реального ThirdPersonController,
взаимодействиями E и кликами через EventSystem. Скорость игры — 1×. Пройдены начало, полное вступление
с дневником, оба подслушивания, три поручения до репутации 5/5, пять страниц скетчбука, лента,
сопровождение Селесты, уход Марго, дверь, конфронтация, убийство, все четыре задачи уборки,
выбор BURN THE EVIDENCE, бал, три страницы дневника Адриана и конечный экран A PERFECT DUET.
Запись продолжается семь секунд после появления итогового экрана. Это основная успешная ветка;
другие концовки и необязательные розы в этот прогон не входят.

В процессе зафиксировано ошибок/исключений Unity: {stats['unityErrors']}.
Аудиодорожка записана из AudioListener: пик {stats['audioPeak']:.3f},
продолжительность исходного звука {stats['audioDuration']:.2f} с.

## Обнаруженные дефекты

| Время | Наблюдение | Подтверждение |
| --- | --- | --- |
{chr(10).join(rows)}

Видеозапись сохраняет эти дефекты текущей сцены. Пропавших файлов материалов или неподдерживаемых
шейдеров предыдущая проверка 1620 рендереров не выявила. Крупное каменное пятно при подслушивании
связано с положением камеры относительно геометрии.

Первый технический прогон находится в attempt_01: запись остановилась до конечного экрана,
когда сработал флаг завершения признания. Полное видео выше собрано из одного непрерывного
повторного прогона {run.name}, остановленного по состоянию Phase2CEnding и видимой итоговой панели.
"""
(root / "Playthrough_Review.md").write_text(report)
