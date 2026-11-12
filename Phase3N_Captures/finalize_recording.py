from pathlib import Path
import json
import subprocess
import sys

branch = sys.argv[1].lower()
assert branch in ('burn', 'leave')
folder = Path(__file__).resolve().parent
project = folder.parent
source = project / ('Playthrough_Recording/phase3n_' + branch)
results = (folder / (branch + '_standalone.txt')).read_text()
assert 'Result=PASSED' in results and 'Errors=0' in results and 'Platform=OSXPlayer' in results
stats = json.loads((source / 'capture_stats.json').read_text())
assert stats['unityErrors'] == 0 and stats['encoderExitCode'] == 0 and stats['audioPeak'] > .001
ffmpeg = (project / 'Playthrough_Recording/ffmpeg_path.txt').read_text().strip()
duration = stats['frames'] / stats['fps']
delay = max(0, stats['firstAudioDelay'])
tempo = stats['audioDuration'] / (duration - delay)
video = folder / ('VelvetVows_' + branch.upper() + '_Playthrough.mp4')
subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'warning', '-y', '-i', str(source / 'video_silent.mp4'),
    '-itsoffset', str(delay), '-f', 'f32le', '-ar', str(stats['audioSampleRate']), '-ac', str(stats['audioChannels']),
    '-i', str(source / 'audio.f32'), '-filter_complex', f'[1:a]atempo={tempo:.10f},apad[a]',
    '-map', '0:v:0', '-map', '[a]', '-t', str(duration), '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k',
    '-movflags', '+faststart', str(video)], check=True)
check = subprocess.run([ffmpeg, '-hide_banner', '-v', 'error', '-i', str(video), '-f', 'null', '-'], capture_output=True, text=True)
(folder / (branch + '_decode_check.log')).write_text(check.stderr)
assert check.returncode == 0 and not check.stderr
events = []
for line in (source / 'timeline.tsv').read_text(encoding='utf-8-sig').splitlines()[1:]:
    seconds, kind, text = line.split('\t', 2)
    events.append((float(seconds), kind, text))
proof = folder / ('proof_' + branch)
proof.mkdir(exist_ok=True)
frames = [(max(0, duration - 3), 'ending')]
for needle, offset, name in [('BooksPickup', .4, 'pickup'), ('Phase2BCinematic', 2, 'murder'), ('Phase2BCleanup', 1, 'cleanup'), ('/Dance', 4, 'dance')]:
    found = next((t for t, kind, text in events if needle in text and kind == 'state'), None)
    if found is not None:
        frames.append((min(duration - 1, found + offset), name))
for seconds, name in frames:
    subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-y', '-ss', str(seconds), '-i', str(video), '-frames:v', '1', str(proof / (name + '.jpg'))], check=True)
print(json.dumps({'video': str(video), 'seconds': duration, 'unityErrors': stats['unityErrors'], 'decoded': True}))
