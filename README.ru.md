> 🇬🇧 [English version](README.md)

<p align="center"> <img alt="Zona-14" width="1320" height="540" src="https://github.com/stalker14-project/stalker14/blob/master/Resources/Textures/Logo/logo-stalker.png" /></p>

# Zona-14

Zona-14 — это EN-ориентированный форк Space Station 14 в линии `space-wizards → space-syndicate → stalker14-project → Zona-14`, работающий на движке [Robust Toolbox](https://github.com/space-wizards/RobustToolbox), написанном на C#.

Весь новый код Zona-14 изолирован в папках `_Zone14/`. Подробнее о правилах контрибьюта — в [CONTRIBUTING.ru.md](CONTRIBUTING.ru.md) (или в полной [англоязычной версии](CONTRIBUTING.md)).

Основной форк, с которого регулярно мёрджится апстрим: [stalker14-project/stalker14](https://github.com/stalker14-project/stalker14). За основу `stalker14-project` в свою очередь взят [space-syndicate/space-station-14](https://github.com/space-syndicate/space-station-14).

## Ссылки

[Zona-14 Discord](https://discord.gg/CFVWFfVpJg) | [Stalker14 Discord](https://discord.gg/pu6DEPGjsN) | [SS14 Вики](https://wiki.station14.ru) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Клиент без Steam](https://spacestation14.io/about/nightlies/) | [Репозиторий Zona-14](https://github.com/Zona-14/Zona-14) | [Апстрим stalker14-project](https://github.com/stalker14-project/stalker14)

## Контрибьют

Полные правила — в [CONTRIBUTING.ru.md](CONTRIBUTING.ru.md) (краткая сводка) или [CONTRIBUTING.md](CONTRIBUTING.md) (полная англоязычная версия).

Коротко: весь новый код Zona-14 кладётся в папки `_Zone14/`, правки в файлах вне `_Zone14/` помечаются комментарием `// Zone14:`, у каждого `meta.json` должны быть заполнены поля `license` и `copyright`. Локальная проверка перед пушем: `bash Tools/_Zone14/check-conventions.sh origin/master HEAD`.

Обсуждение разработки, плейтесты, загрузка больших видео для PR — [Zona-14 Discord](https://discord.gg/CFVWFfVpJg). Баги и задачи — GitHub Issues. Вопросы по апстриму `stalker14-project` — их [Discord](https://discord.gg/pu6DEPGjsN).

## Лицензия

Репозиторий имеет слоёную лицензию — см. полный текст в [LICENSE.TXT](LICENSE.TXT).

- **Апстрим-код** (Space Wizards Federation, Corvax) — под [MIT](LICENSE.TXT).
- **Код команды Stalker14** (авторы, перечисленные в заголовке `LICENSE.TXT`) — **All rights reserved**. Для переиспользования обращайтесь к [команде Stalker14](https://discord.gg/GXzurVkWYX).
- **Код команды Zona-14** (всё под `_Zone14/`) — под [MIT](LICENSE.TXT) © 2024-2026 Zona-14 Team. Открывая PR с файлами в `_Zone14/`, вы соглашаетесь на лицензирование вашего вклада по условиям Zona-14 MIT.
- **Ассеты** (спрайты, звуки, карты) по умолчанию под [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/). Конкретная лицензия и авторство указываются в файле `meta.json` каждого ассета ([пример](https://github.com/space-syndicate/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json)). Ассеты Stalker14 могут использоваться в любых не-коммерческих опен-сурс проектах.

Некоторые ассеты лицензированы на некоммерческой основе ([CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) или аналогичной); их необходимо удалить при коммерческом использовании проекта.
