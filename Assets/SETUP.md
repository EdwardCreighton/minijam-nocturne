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

## P3. Враг Chaser + очки + респаун (выполнено)

### Префаб `Chaser` (`Assets/Prefabs/Enemies/Chaser.prefab`)
- Корень: слой `Enemy (7)`, `Rigidbody2D` (Kinematic, Continuous), `CircleCollider2D` (`r 0.4`).
- Скрипты: `Enemy` (HP/урон/очки, `Initialize` с множителями, смерть → `RunState.AddKill`), `EnemyMover` (chase в агро `6`, separation), `EnemyAttack` (дистанционная проверка ≤ `1.0`, кулдаун `1с` — коллизий kinematic-kinematic нет, поэтому без колбэков).
- Визуал ВРЕМЕННЫЙ: `idle_down.png` с красным тинтом. Заменить артом врага, скрипты не трогать. Стартовые цифры (`50/10/10`) — поля `BalanceConfig` (`chaserHP/Damage/Score` + `enemySpeed/Aggro/AttackRadius/AttackCooldown`).

### Спавнер (`EnemySpawner` в `SampleScene`)
- 7 точек: 3 центр + 2 восток + 2 запад (см. `entries` в Inspector). `respawnDelay = 0` (пополнение только через смерть игрока — цикл фарма по концепту).
- Смерть игрока → `GameManager.DeathRoutine` → `RespawnAll()` (все пересозданы с полным HP, гейты не тронуты).
- Новый тип врага (P5): новый префаб + добавить `entries`, смерть сама маппится на свою точку (`origin`).

### Движение и стены (фикс ревью P3)
- Тела кинематические: голый `MovePosition` проходил бы сквозь стены. Все движение идет через `Nocturne.Core.MovementUtil.TryMove` (sweep `Rigidbody2D.Cast` по слоям `World|Gate` + скольжение по осям). Новые движущиеся сущности — только через него.

### Уроки сетапа (повторяющиеся грабли)
- `SaveAsPrefabAsset` требует существующую папку — создавать через `AssetDatabase.CreateFolder` заранее.
- Headless-batchmode упирается в лицензию (`com.unity.editor.headless was not found`, виснет). Если повторяется — сетап-скрипты запускать из Editor через `[MenuItem("Nocturne/Setup/...")]`, batchmode только для проверки компиляции.

## P4. Гейты полностью (выполнено)

### Логика холда (`PlayerInteractor`, единственный путь траты)
- Условия роста прогресса: `Playing` + кнопка `Interact` зажата + ближайший закрытый гейт в радиусе `1.5` + `Unspent >= cost` + вне дебаунса. Завершение → `Gate.TryOpen` (атомарно).
- Сброс без траты: отпустил кнопку / вышел из радиуса / получил урон (отслеживается падение HP) / смерть / пауза. Холд без денег не стартует.
- Успешное открытие ставит дебаунс `0.5с`. При двух гейтах в радиусе работает только ближайший.

### Гейт (`Gate`)
- `TryOpen(RunState)` + событие `Opened`. Визуал-заглушка: закрыт — синий столб (`SpriteRenderer` поверх коллайдера `0.5×2`, scale `0.55×2.5`), открыт — бледно-зелёный + коллизия выкл.
- Новый гейт: дублировать существующий, задать уникальные `id`/`cost`. `SpriteRenderer` обязателен (иначе невидим).

### Промпт (`GatePrompt` под `HudCanvas`)
- Текст (`Держи E — открыть (N)` / `..., не хватает M`) + полоса `HoldBar` (`fillAmount = HoldProgress`). Виден только рядом с закрытым гейтом в `Playing`.
- Проверка в Editor: встал рядом без денег — текст нехватки, полоса 0; держишь E — полоса растёт; отпустил/получил урон — сброс.

## P5. Сложность + Dasher (выполнено)

### Формула (`Core/DifficultyScaler.cs`, чистые статические функции)
- `level = clamp(1 + floor(Spent / N), 1, maxLevel)`; HP `1 + K·(lvl-1)`, урон `1 + M·(lvl-1)`. Дефолты `N=100, K=0.25, M=0.15, maxLevel=5`.
- Множители инжектятся только в момент спавна/респауна; живые враги не баффаются. HUD: `Spent: N (lvl L)`.

### Типы врагов (базовые статы — поля префаба на `Enemy`)
- `Chaser` (`50/10/10`, без рывка), `Dasher` (`100/20/25`, `dashSpeedMult 2.5 / interval 3с / duration 0.4с`, оранжевый плейсхолдер).
- Состав через `EnemySpawner` → `entries[]`: `tierPrefab + tierMinLevel` (грейбокс: восток → Dasher с lvl 2, запад → с lvl 3, центр всегда Chaser). Переключение применяется на ближайшем респауне (смерть игрока), не на живых.
- Внимание: суммарные траты грейбокса (30+50=80) не дотягивают до `N=100` — тир 2 в грейбоксе недостижим без временного занижения `N`. Полноценная лестница цен — в P6-карте. Временные правки баланса для тестов НЕ коммитить (проверять `difficultyN` перед коммитом).

## P6. Карта: хаб-петля + карман (выполнено)

### Топология (вид сверху, X → восток)
- Центр `(0,0)` 12×8 (`StartPoint`); восток `Finish_East (17,0)`; запад `Finish_West (-17,0)`.
- Северный хаб `x -18..18, y 9..14`: вход из центра коридором `x 3..5` через `Gate_North` (`id gate_north`, `cost 60`, горизонтальный бар `2×0.5`); коннекторы хаб→восток (`x 14..16`) и хаб→запад (`x -16..-14`).
- Маршруты: восток `30 / 60`, запад `50 / 60`. Суммарный максимум трат `140` → достижим только `lvl 2`.
- Южный фарм-карман `x -3..3, y -10..-4.5` (тупик, 2 точки Chaser).
- Спавнер: 12 точек (3 центр + 2 восток + 2 запад + 2 карман + 3 хаб). Тиры: восток/запад/хаб → Dasher@2, центр/карман — всегда Chaser.
- Новый гейт/стена: именовать `Gate_*/Wall_*`, слой `Gate`/`World`, гейту — `SpriteRenderer`-бар. После ручных правок стен проверять обход пешком: дыры наружу и затыки ловятся только плей-тестом.

## P7. Главное меню (выполнено)

### Структура `MainMenu.unity`
- `Canvas`: строго `Screen Space Overlay` + `Scale With Screen Size 1280×720` (сетап форсит Overlay явно).
- `MenuRoot` (Title + `NewGameButton` + `CreditsButton`), `CreditsPanel` (текст + `BackButton`, по умолчанию скрыта), `EventSystem` (`InputSystemUIInputModule` → actions asset проекта, карта `Player` в меню не включается).
- `MainMenu` (пустой GO): `MainMenuController` (ссылки `newGameButton/creditsButton/backButton/creditsPanel/menuRoot`).

### Поведение
- `Новая игра` → `SceneLoader.LoadGameLevel` (свежий `RunState` создаётся сценой, сброса не нужно). `Титры` → `MenuRoot` скрывается, только текст + `Назад`. `Назад` → наоборот.
- Новых кнопок не добавлять без нужды (настройки/выход вне скоупа WebGL). Правки текста титров — прямо в `CreditsText` в Inspector.

## P8. Экраны, звук, тесты (выполнено)

### Экраны уровня (`Screens` на GO `Screens` под `HudCanvas`)
- 4 панели (дим 0.65 + текст + кнопки): `BriefingPanel` (управление/правила + `Начать`), `DeathPanel` (только текст, висит на `Dying`), `WinPanel` (статистика `FinishId/Spent/Deaths/время` + `В меню` / `Новая игра`), `PausePanel` (`Продолжить` / `В меню`). Все стартуют скрытыми, переключаются по `GameManager.StateChanged`.
- Ран начинается в `Briefing` (мир заморожен) до `DismissBriefing`. Победа/пауза тоже замораживают время + включают `UI`-карту.
- `EventSystem` (`InputSystemUIInputModule` → actions asset) обязателен в обеих сценах — без него кнопки мертвы для клавиатуры/геймпада.

### Финиши
- `CircleCollider2D`-триггер (`r 0.7`) на каждом `FinishPoint`; касание игроком → `OnFinishReached`. Новый финиш: компонент + триггер + уникальный `finishId`.

### Звук (заглушки)
- `AudioManager` на `GameSystems` (один `AudioSource`, методов `Play*` пишут `[Audio] id` в консоль). Вызовы вшиты: удар/попадание/гейт/смерть/победа. Настоящие клипы: Vorbis, только после первого жеста (WebGL).

### Тесты (`Assets/Tests/`, Test Runner)
- EditMode: `RunStateTests`, `DifficultyScalerTests`, `BalanceAndGateTests` (дефолты конфига + уникальность `Gate.id` в сцене).
- PlayMode: `DeathPersistenceTests`, `HoldToOpenTests` (синтетическая клавиатура через queued state; враги чистятся для детерминизма), `FlowTests` (пауза, финиш; ожидания только unscaled — победа/смерть морозят `timeScale`).

### Сборки (важно)
- Код разделён на сборки: `Nocturne.Game` (весь рантайм), `Nocturne.Tests.EditMode/PlayMode` (только с `TestAssemblies`, в плеер не утекают), `Nocturne.Editor` (только Editor). Корневой рантайм-asmdef затягивает `Assets/Editor/` в сборку — поэтому Editor-скрипты живут только под `Nocturne.Editor.asmdef`, иначе WebGL-билд падает с `CS0234/CS0246`.
- Одноразовые сетапы удалять сразу после прогона (они тоже ломают билд через `UnityEditor`-API).
- Весь UI — TextMeshPro (`com.unity.textmeshpro`, шрифт по умолчанию из `TMP Settings`). Legacy `Text` не использовать.

### WebGL-приёмка (TZ §2.1)
- `Build Profiles → WebGL → Build And Run`: меню → уровень → бой/гейты/смерть/финиш без ошибок консоли в Chrome. `preloadedAssets` с actions-ассетом прописывается сам — коммитить.
