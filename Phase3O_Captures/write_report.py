from pathlib import Path
import json
import xml.etree.ElementTree as ET
p=Path(__file__).resolve().parent
root=p.parent
r=ET.parse(root/'Phase3N_Captures/regression.xml').getroot()
assert r.attrib['result']=='Passed' and r.attrib['passed']=='12'
checks=ET.parse(root/'Phase3N_Captures/camera_grip_final.xml').getroot()
camera=[x for x in checks.iter('test-case') if x.attrib.get('fullname','').endswith('CameraRecoversFromPersistentCloseWallAndRespectsCinematics')]
assert camera and camera[0].attrib['result']=='Passed'
assert ET.parse(root/'Phase3N_Captures/body_current.xml').getroot().attrib['result']=='Passed'
assert 'Succeeded\nErrors: 0' in (p/'build_summary.txt').read_text()
assert 'Result=PASSED' in (p/'burn_standalone.txt').read_text() and 'Errors=0' in (p/'burn_standalone.txt').read_text()
assert not (p/'burn_decode_check.log').read_text()
stats=json.loads((root/'Playthrough_Recording/phase3o_burn/capture_stats.json').read_text())
assert stats['unityErrors']==0 and stats['encoderExitCode']==0
length=round(stats['frames']/stats['fps'])
report=f'''# Velvet Vows — камера и перенос тела

- При длительном обзоре стены камера ищет свободную позицию, если находится вплотную к препятствию или движущийся персонаж вышел из кадра. Постановочные камеры и диалоги не переориентируются этим механизмом.
- При укрытии обе руки направляются к ближайшей руке Селесты. Цели пересчитываются после движения тела; добавлена глубокая поза и ограниченное сгибание пальцев. На проверенных кадрах переноса максимальный зазор от кистей до руки Селесты составил около 4,5 см. [Кадр](body_hiding.jpg).
- Сохранены проверка физического пути к шкафу, отмена при препятствии и возврат управления.

Основная регрессия: [12/12 тестов](../Phase3N_Captures/regression.xml), включая 72 угла камеры, переходы через дверь, уборку, танец и отмену действий. После уточнения условия камеры повторно прошли проверка камеры и 72 угла; отдельный [повтор захвата](../Phase3N_Captures/body_current.xml) прошёл. Один промежуточный прогон имел недостаточно кадров для замера; тест запущен повторно с более ранней точкой съёмки.

Полный BURN пройден в собранном macOS-приложении автоматизированным маршрутом с контроллером и взаимодействиями: [видео со звуком — {length//60}:{length%60:02d}](VelvetVows_BURN_Playthrough.mp4), [результат](burn_standalone.txt), 0 ошибок Unity. Видео проверено декодированием. На реальном проблемном переходе восстановление камеры сработало: [кадры до](../Phase3N_Captures/doorway_route.jpg), [после](doorway_after.jpg). Стена и дверь по-прежнему могут кратко перекрыть экран, но в этом проходе обзор возвращался; восстановление может сопровождаться заметным изменением ракурса.

[Сборка macOS](../Builds/Phase3O/VelvetVows-macOS.zip) — Development, для arm64 и x86_64, архив проверен. Предыдущий полный проход LEAVE доступен [здесь](../Phase3M_Captures/VelvetVows_LEAVE_Playthrough.mp4); в новой версии LEAVE полностью не перепроходился.

Захват и вторичная анимация процедурные; полноценной физической симуляции тела и новой парной анимации нет. Запись может содержать короткие паузы звука, добавленные при захвате для сохранения синхронизации.
'''
(p/'Report.md').write_text(report)
print('Saved',p/'Report.md')
