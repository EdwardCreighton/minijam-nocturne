# SETUP — настройка префабов, сцен и ассетов (Nocturne)

Пошаговые инструкции для ручной настройки в Unity Editor. Источник правды по цифрам — `Docs/TZ.md`, дефолты — `Assets/Settings/BalanceConfig.asset`.

## P0. Ввод, конфиг, сцены (выполнено)

### InputSystem_Actions (пересоздан, GUID `2bcd2660ca9b64942af0de543d8d7100`)
- Файл: `Assets/Settings/InputSystem_Actions.inputactions` (JSON, НЕ YAML — YAML не парсится импортером).
- Рядом лежит сгенерированный `InputSystem_Actions.cs` (чекбокс `Generate C# Class` в импортере ВКЛ). Единственный подход к вводу — этот класс (`TZ §5.3`); `SendMessages`/`UnityEvents` не использовать.
- Карта `Player`: `Move` (Vector2: WASD + стрелки + левый стик), `Attack` (LMB / Enter / West), `Interact` (E / North, **без** `Hold`-интеракции), `Pause` (Esc / Start).
- ВАЖНО: `Hold`-интеракцию в ассет НЕ добавлять — холд `0.6с` меряет код по `BalanceConfig.holdTime` (`TZ §2`).
- Карта `UI`: `Navigate/Submit/Cancel/Point/Click/ScrollWheel` (меню + экраны паузы/победы).
- Если ассет правится в Editor: после изменений `Save Asset`, проверить что `.cs` перегенерировался, закоммитить оба файла + `.meta`.

### BalanceConfig
- Скрипт: `Assets/Scripts/Config/BalanceConfig.cs` (`Nocturne.Config`), инстанс: `Assets/Settings/BalanceConfig.asset`.
- Новый баланс — только полями инстанса в Inspector, НЕ правкой дефолтов в коде. `OnValidate` режет невалидные значения.
- `Gate.cost` — поле префаба/инстанса гейта (левел-дизайн), не глобальный баланс.

### Сцены и билд
- Список билда (`File → Build Profiles`, 2 сцены): `MainMenu` (index 0), `SampleScene` (index 1, позже переименовать в `GameLevel` через Editor с сохранением GUID + обновить `SceneLoader.GameLevel`).
- `MainMenu.unity`: `Main Camera` + `Canvas` (`CanvasScaler → Scale With Screen Size`, `1280×720`) + `EventSystem` (`InputSystemUIInputModule`) + пустой `MainMenu` (логика в P7).
- Переходы только через `Nocturne.Core.SceneLoader` (`Меню ↔ Уровень`), внутри `GameLevel` — никаких `LoadScene`.

### Слои физики (задел под P2–P3)
- Использовать слои `Player`, `Enemy`, `World`, `Gate` (создать в `Tags and Layers`, если отсутствуют).
- Матрица (`Project Settings → Physics 2D`): `Player ↔ World/Gate/Enemy`, `Enemy ↔ World`; атака игрока бьёт только `Enemy`, контактный урон — только `Player`.
