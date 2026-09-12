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

## P1. Каркас рана + грейбокс (выполнено)

### Повторная генерация ввода (фикс P0)
- Композиты `WASD`/`Arrows` из P0 — невалидны (таких composite-типов нет, резолвинг падал). Переписано на `2DVector(mode=1)` с частями `Up/Down/Left/Right` + отдельно `<Gamepad>/leftStick`. Шрифт HUD — `LegacyRuntime.ttf` (`Arial.ttf` больше не доступен как builtin).
- Проверка после любых правок `.inputactions`: в импорте не должно быть `InvalidOperationException while resolving binding`.

### Слои (назначены в `TagManager`, матрица — кодом)
- Индексы: `6 = Player`, `7 = Enemy`, `8 = World`, `9 = Gate` (константы `Nocturne.Core.Layers`).
- Матрица применяется в `GameManager.Awake` через `Layers.ApplyCollisionMatrix()` (единственное исключение: `Enemy ↔ Enemy` не сталкиваются; закрытые гейты блокируют всех). Ручную матрицу в Project Settings не править.

### Грейбокс `SampleScene` (схема, вид сверху, X → восток)
- Центр `(0,0)`: `StartPoint`, стартовая комната 12×8.
- Восток (дешёвый маршрут): коридор `x 6→10`, `Gate_East` (`id gate_east`, `cost 30`), комната, `Finish_East` (`id finish_east`) в `(17,0)`.
- Запад (дорогой маршрут): зеркально, `Gate_West` (`id gate_west`, `cost 50`), `Finish_West` в `(-17,0)`.
- Стены — `BoxCollider2D` на слое `World` (префикс `Wall_`); гейты — `BoxCollider2D 0.5×2` на слое `Gate` + компонент `Gate` (id/cost/isOpen).
- `GameSystems`: `GameManager` (синглтон сцены, владеет `RunState` + `InputSystem_Actions`, `GameState`, пауза) + `AttemptResetter`.
- `Main Camera` + `FollowCam` (target назначится в P2 на игрока; `Snap()` — для респауна).
- `HudCanvas` (Screen Space Overlay, 1280×720): `Hud` + 3 `Text` (Points/Spent/Deaths). `EventSystem` пока НЕТ (кнопок нет до P8).
- Новый гейт: дублировать `Gate_East/West`, задать уникальные `id` + `cost`, слой `Gate`. Дубли `id` ловятся EditMode-тестом (P8).

## P2. Игрок (выполнено)

### Префаб `MainCharacter` (правился скриптом, руками не трогать структуру)
- Корень: тег `Player`, слой `Player (6)`, `Rigidbody2D` (Kinematic, Continuous, `gravityScale 0`, `FreezeRotation`), `CapsuleCollider2D` (`0.6×0.8`, vertical).
- Скрипты на корне: `PlayerController` (движение `MovePosition`, скорость из конфига, `LastDirection`), `PlayerCombat` (арка по последнему `Move`, кулдаун, бьёт слой `Enemy` через `IDamageable`), `PlayerHealth` (`HP` + `damageCooldown`, смерть → `GameManager.OnPlayerDied`), `PlayerInteractor` (пока только поиск ближайшего гейта + `CancelHold`; холд в P4), `PlayerVisual` (см. ниже).
- Спрайт/аниматор остаются на ребенке `Animations` — не переносить.

### Аниматор (подход: code-driven)
- Состояние `MainCharacter_Attack1Upanim` переименовано в `MainCharacter_Attack1Up` (была опечатка в ассете). Переходов/параметров НЕ добавлять — `PlayerVisual` переключает 16 состояний через `Animator.Play()` по имени (`MainCharacter_{Idle,Run,Attack1,Attack2}{Down,Left,Right,Up}`), атаки чередуются 1/2.
- Проверка в Editor: Play → WASD двигает, `Run`-спрайт по направлению; Enter/LMB — `Attack`-спрайт; камера следует.

### Смерть/респаун
- `GameManager.OnPlayerDied` → `Dying` → задержка `deathDelay` (unscaled, паузой не прерывается) → `Unspent=0`, `Deaths++` → позиция `StartPoint`, `rb.position` + нулевая скорость, `ResetHP`, `CancelHold`, `FollowCam.Snap()` → `Playing`. Пересоздание врагов — в P3.
- Инстанс игрока в `SampleScene` — на `StartPoint (0,0,0)`, `FollowCam.target` привязан. Второго игрока не спавнить (синглтон сцены).
