from pathlib import Path
import json
import xml.etree.ElementTree as ET
p=Path(__file__).resolve().parent
root=p.parent

def passed(name):
 r=ET.parse(p/name).getroot()
 assert r.get('result')=='Passed', name
 return int(r.get('passed'))

repair=passed('final_repairs.xml')
body=passed('body_final.xml')
regression=ET.parse(p/'regression_results.xml').getroot()
assert int(regression.get('passed'))==10 and int(regression.get('failed'))==1
for branch in ('burn','leave'):
 report=(p/(branch+'_standalone.txt')).read_text()
 assert 'Result=PASSED' in report and 'Errors=0' in report and 'Platform=OSXPlayer' in report
 assert not (p/(branch+'_decode_check.log')).read_text()
 assert (p/('VelvetVows_'+branch.upper()+'_Playthrough.mp4')).is_file()
assert 'Succeeded\nErrors: 0' in (p/'build_summary.txt').read_text()
assert 'Valid=True\nErrors=0' in (p/'standalone_smoke.txt').read_text()

def length(branch):
 x=json.loads((root/'Playthrough_Recording'/('phase3m_'+branch)/'capture_stats.json').read_text())
 assert x['unityErrors']==0 and x['encoderExitCode']==0
 seconds=round(x['frames']/x['fps'])
 return f'{seconds//60}:{seconds%60:02d}'

burn=length('burn'); leave=length('leave')
report=f'''# Velvet Vows — постановка, действия и проверка macOS

## Изменения

- Исправлена формула плавного завершения общих действий: раньше анимации рук едва достигали примерно 20% силы. Теперь руки полноценно тянутся к предметам и плавно возвращаются.
- При ударе рука плавно приближается к цели; звук и реакция Селесты начинаются одновременно, после чего рука плавно освобождается.
- Во время укрытия Эвелин сначала приближается сбоку, затем движется вместе с телом. Скорость перемещения тела ограничена реальным движением её капсулы; при препятствии действие прерывается с подсказкой. Камера ищет обзор обоих персонажей, а управление возвращается после завершения действия.
- При танце общая точка контакта сглаживается, а руки плавно входят в позу и выходят из неё. Используются прежние существующие циклы анимации.
- В студии визуальная скамья под тканью заменена на столешницу и ножки без изменения существующего коллайдера. Кранам возвращён металлический материал, добавлен мягкий тёплый заполняющий свет. [Кадр укрытия](body_hiding.jpg).
- Индикатор подозрения показывает причину активной зоны — близость префекта, запретную или охраняемую область. Перед выбором судьбы тела уточнён риск обнаружения.

## Проверка

В первой [регрессии](regression_results.xml) 10 из 11 проверок прошли; старая проверка HUD обнаружила несовместимость сигнатуры. Интерфейс исправлен и повторно проверен: [финальные проверки исправлений](final_repairs.xml) — {repair}/{repair}, включая HUD, все четыре действия уборки, провал и повтор, ограждения библиотеки и укрытие тела. [Повтор после смещения позы](body_final.xml) — {body}/{body}, включая проход уборки и замер рук.

На записи BURN около 4:24–4:27 при проходе возле двери камера на несколько секунд смотрит в стену; этот дефект не исправлен. [Контрольные кадры](camera_around_258.jpg).

В контрольном кадре расстояние от кистей до плеч лежащей Селесты составляло примерно 20–31 см. Это лучше прежних почти двух метров, но ещё не точный захват пальцами: тело сохраняет фиксированную позу, а перенос остаётся процедурным. Новой парной записи движений или полноценной физики тела нет.

Обе ветки пройдены в **отдельном macOS-приложении** автоматизированным маршрутом с реальным контроллером, нажатиями E и интерфейсом; сюжетные флаги напрямую не выставлялись. Видео записаны в обычном темпе с игровым звуком и полностью проверены декодированием:

- [BURN → A PERFECT DUET — {burn}](VelvetVows_BURN_Playthrough.mp4), [результат](burn_standalone.txt), [лог](burn_standalone.log).
- [LEAVE → HEARTBROKEN — {leave}](VelvetVows_LEAVE_Playthrough.mp4), [результат](leave_standalone.txt), [лог](leave_standalone.log).

В обоих полных проходах ошибок Unity не зафиксировано. Исходные записи и таймлайны находятся в `Playthrough_Recording/phase3m_burn` и `phase3m_leave`.

[Сборка macOS](../Builds/Phase3M/VelvetVows-macOS.zip) — Development, универсальная для arm64 и x86_64. Сборка завершена без ошибок; отдельная [начальная проверка приложения](standalone_smoke.txt) прошла. Исходная сцена до этой работы сохранена как `VelvetVows_Main_PrePhase3M.unity`.
'''
(p/'Report.md').write_text(report)
print('Report saved:',p/'Report.md')
