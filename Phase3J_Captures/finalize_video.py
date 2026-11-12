from pathlib import Path
import json
import subprocess
import xml.etree.ElementTree as ET

folder = Path(__file__).resolve().parent
project = folder.parent
source = project / 'Playthrough_Recording/phase3j_final'
ffmpeg = (project / 'Playthrough_Recording/ffmpeg_path.txt').read_text().strip()
results = ET.parse(folder / 'full_run_results.xml').getroot()
assert results.attrib['result'] == 'Passed', 'Full playthrough did not pass'
stats = json.loads((source / 'capture_stats.json').read_text())
assert stats['encoderExitCode'] == 0 and stats['audioSamples'] > 0 and stats['audioPeak'] > .001
duration = stats['frames'] / stats['fps']
video = folder / 'VelvetVows_Phase3J_Playthrough.mp4'
events = []
for line in (source / 'timeline.tsv').read_text(encoding='utf-8-sig').splitlines()[1:]:
    seconds, kind, text = line.split('\t', 2)
    events.append((float(seconds), kind, text))
labels = {
    'Phase2A/Opening/Dormant': 'First meeting',
    'Phase3AIntro/Opening/Dormant': 'Evelyn diary and photographs',
    'Phase2A/MissingStudent/Dormant': 'The missing student',
    'Phase2A/Overhearing/Dormant': 'Celeste and Isabelle',
    'Phase2A/Watching/Dormant': 'Watching Adrian',
    'Phase2A/Errands/Dormant': 'Reputation errands',
    'Phase2A/RibbonSearch/Dormant': 'Ribbon and sketchbook',
    'Phase2A/FollowCeleste/Dormant': 'Lead Celeste to the Studio',
    'Phase2A/Witness/Dormant': 'Margot leaves',
    'Phase2A/Confront/Dormant': 'Confrontation',
    'Phase2BCinematic/Complete/Dormant': 'Murder',
    'Phase2BCleanup/Complete/Dormant': 'Physical cleanup',
    'Phase2BOutcome/Complete/Dormant': 'After the cleanup',
    'Phase2CBall/Complete/Establishing': 'Black Rose Ball',
    'Phase2CBall/Complete/Revelation': 'Adrian confession and diary',
    'Phase2CEnding/Complete/Ending': 'A Perfect Duet',
}
chapters = [(0.0, 'Main menu')]
chapters += [(t, labels[text]) for t, kind, text in events if kind == 'state' and text in labels]
meta = [';FFMETADATA1', 'title=Velvet Vows - repaired full playthrough']
for index, (t, title) in enumerate(chapters):
    end = chapters[index + 1][0] if index + 1 < len(chapters) else duration
    meta += ['[CHAPTER]', 'TIMEBASE=1/1000', f'START={round(t*1000)}', f'END={round(end*1000)}', f'title={title}']
metadata = folder / 'chapters.ffmeta'
metadata.write_text('\n'.join(meta)+'\n')
tempo = stats['audioDuration'] / (duration - max(0, stats['firstAudioDelay']))
subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'warning', '-y', '-i', str(source / 'video_silent.mp4'),
    '-itsoffset', str(max(0, stats['firstAudioDelay'])), '-f', 'f32le', '-ar', str(stats['audioSampleRate']),
    '-ac', str(stats['audioChannels']), '-i', str(source / 'audio.f32'), '-i', str(metadata),
    '-filter_complex', f'[1:a]atempo={tempo:.10f},apad[a]', '-map', '0:v:0', '-map', '[a]',
    '-map_metadata', '2', '-map_chapters', '2', '-t', str(duration), '-c:v', 'copy', '-c:a', 'aac',
    '-b:a', '192k', '-movflags', '+faststart', str(video)], check=True)
checked = subprocess.run([ffmpeg, '-hide_banner', '-v', 'error', '-i', str(video), '-map', '0:v:0', '-map', '0:a:0',
    '-f', 'null', '-'], capture_output=True, text=True)
(folder / 'video_decode_check.log').write_text(checked.stderr)
assert checked.returncode == 0 and not checked.stderr
proof = folder / 'proof'
proof.mkdir(exist_ok=True)
def first(needle, kind=None):
    return next(t for t, k, text in events if needle in text and (kind is None or k == kind))
frames = [
    (first('ADRIAN|Are you alright?', 'dialogue') + .5, 'characters'),
    (first('Phase3AIntro/Opening', 'state') + 36, 'intro_caption'),
    (first('Girls like her have always had everything.', 'dialogue') + 1, 'reaction'),
    (first('MARGOT|Right now?', 'dialogue') + .5, 'studio_witness'),
    (first('library books', 'interaction') - .4, 'interaction_focus'),
    (duration - 3, 'ending'),
]
for t, name in frames:
    subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-y', '-ss', str(t), '-i', str(video),
        '-frames:v', '1', str(proof / f'{name}.jpg')], check=True)

print(json.dumps({"video":str(video),"duration":duration,"bytes":video.stat().st_size,"unity_errors":stats["unityErrors"],"decode_check":"passed"}))
