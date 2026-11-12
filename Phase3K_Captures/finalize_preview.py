from pathlib import Path
import json
import subprocess
import xml.etree.ElementTree as ET

folder = Path(__file__).resolve().parent
project = folder.parent
source = project / 'Playthrough_Recording/phase3k_cast'
results = ET.parse(folder / 'final_results.xml').getroot()
assert results.attrib['result'] == 'Passed', 'Resolve the regression failures before delivering the preview'
stats = json.loads((source / 'capture_stats.json').read_text())
assert stats['unityErrors'] == 0 and stats['encoderExitCode'] == 0 and stats['audioPeak'] > .001
ffmpeg = (project / 'Playthrough_Recording/ffmpeg_path.txt').read_text().strip()
duration = stats['frames'] / stats['fps']
delay = max(0, stats['firstAudioDelay'])
tempo = stats['audioDuration'] / (duration - delay)
video = folder / 'VelvetVows_Character_Preview.mp4'
subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'warning', '-y', '-i', str(source / 'video_silent.mp4'),
    '-itsoffset', str(delay), '-f', 'f32le', '-ar', str(stats['audioSampleRate']), '-ac', str(stats['audioChannels']),
    '-i', str(source / 'audio.f32'), '-filter_complex', f'[1:a]atempo={tempo:.10f},apad[a]',
    '-map', '0:v:0', '-map', '[a]', '-t', str(duration), '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k',
    '-movflags', '+faststart', str(video)], check=True)
check = subprocess.run([ffmpeg, '-hide_banner', '-v', 'error', '-i', str(video), '-f', 'null', '-'], capture_output=True, text=True)
(folder / 'preview_decode_check.log').write_text(check.stderr)
assert check.returncode == 0 and not check.stderr
print(json.dumps({'video': str(video), 'seconds': duration, 'passed': results.attrib['passed'], 'unityErrors': stats['unityErrors']}))
