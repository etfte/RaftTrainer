using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RaftTrainer
{
    public class MinimapBlip
    {
        public Vector3 Position;
        public Color Col;
        public float Size;
    }

    public class BlueprintBlock
    {
        public Vector3 LocalPos;
        public bool Reinforced;
    }

    // ==========================================
    // V12: ПРОФИЛИ, БИНДЫ, HUD, АВТОПИЛОТ, МАКРО
    // ==========================================
    public partial class TrainerGUI : MonoBehaviour
    {
        // ---------- V12 НАСТРОЙКИ (сериализуются в профили) ----------
        public bool keybindsEnabled = true;
        public string profileName = "Мой профиль";
        public bool hudActiveCheats = true;
        public bool hudSessionStats = true;
        public bool hudWeatherTimer = true;
        public bool hudCompass = true;
        public bool hudMinimap = false;
        public bool eventLogEnabled = true;
        public bool sharkAlarm = false;
        public float sharkAlarmRadius = 40f;
        public bool stormMode = false;
        public bool autoBatteryCharge = false;
        public bool autoTrash = false;
        public string trashKeywords = "leaf,feather";
        public bool autopilotEnabled = false;
        public float autopilotSpeed = 6f;
        public bool photoMode = false;
        public float blueprintPasteOffset = 6f;
        public float minimapZoom = 0.9f;

        // ---------- V12 СОСТОЯНИЕ ----------
        public bool selectMode = false;
        public bool rulerOn = false;
        public bool hasRulerA = false;
        public bool hasRulerB = false;
        public Vector3 rulerA = Vector3.zero;
        public Vector3 rulerB = Vector3.zero;
        public readonly List<Block> selectedBlocks = new List<Block>();
        public bool blueprintHasCopy = false;

        private float nextV12ThrottleTime = 0f;
        private float nextV12FastTime = 0f;
        private bool v12CachesReady = false;

        // Профили
        private const string ProfileDirName = "RaftTrainer_Profiles";
        private static FieldInfo[] profileBoolFields;
        private static FieldInfo[] profileFloatFields;
        private static FieldInfo[] profileIntFields;
        private static readonly HashSet<string> profileSkipFields = new HashSet<string>
        {
            "showMenu", "selectMode", "rulerOn", "autopilotEnabled", "photoMode", "uiScale", "menuCollapsed"
        };
        private string[] profileListCache = null;

        // Подписи фич (для HUD, поиска и профилей)
        private static readonly Dictionary<string, string> featureLabels = new Dictionary<string, string>
        {
            { "oneHitKill", "Ван-Хит" },
            { "killHostilesAura", "Аура Хищников" },
            { "pacifyShark", "Мирная Акула" },
            { "triggerbot", "Триггербот" },
            { "multishotBow", "Мультишот Лук" },
            { "noArrowDrop", "Лазерные Стрелы" },
            { "infiniteAmmo", "Вечные Стрелы" },
            { "ghostMode", "Режим Призрака" },
            { "infiniteBait", "Вечная Приманка" },
            { "sharkRiding", "Оседлать Акулу" },
            { "perfectBowDraw", "Лук без задержки" },
            { "infiniteHarpoonNet", "Вечные Сети Гарпуна" },
            { "fastHook", "Турбо-Крюк" },
            { "fastChop", "Быстрый Топор" },
            { "flyMode", "Свободный Полёт" },
            { "noFallDamage", "Без урона падения" },
            { "freeZiplineAir", "Зиплайн в воздухе" },
            { "superDiving", "Супер-Дайвинг" },
            { "flipperGlider", "Подводный Скутер" },
            { "infiniteOxygen", "Бесконечный Кислород" },
            { "zoomEnabled", "Орлиный Взор" },
            { "superSpeed", "Супер-Скорость" },
            { "superJump", "Супер-Прыжок" },
            { "blueprintCamera", "Дрон-Камера" },
            { "espMaster", "ESP Включен" },
            { "espShark", "ESP Акула" },
            { "espPlayers", "ESP Игроки" },
            { "espUnderwaterOres", "ESP Руда" },
            { "espTreasures", "ESP Клады" },
            { "espAllItems", "ESP Все Вещи" },
            { "espBarrels", "ESP Бочки" },
            { "waypointEsp", "Метки Точек" },
            { "clearWater", "Прозрачная Вода" },
            { "nightVision", "Ночное Зрение" },
            { "subseaFloodlight", "Подводный Прожектор" },
            { "alwaysActiveDetector", "Вечный Детектор" },
            { "wireframeBuilding", "Подсветка Каркаса" },
            { "soundBooster", "Усилитель Звука" },
            { "godMode", "Бессмертие" },
            { "infiniteNeeds", "Без голода и жажды" },
            { "infiniteDurability", "Вечная Прочность" },
            { "freeCraft", "Бесплатный Крафт" },
            { "freeBuild", "Бесплатная Стройка" },
            { "noBuildRestrictions", "Стройка без Опор" },
            { "instantDemolish", "Мгновенный Снос" },
            { "indestructibleRaft", "Неразрушимый Плот" },
            { "infinitePipeWater", "Вода в Трубах" },
            { "freezeRaft", "Заморозка Плот" },
            { "alwaysTailwind", "Всегда по Ветру" },
            { "turboRaft", "Турбо-Плот" },
            { "infinitePaint", "Бесконечная Краска" },
            { "raftStabilizer", "Антикрен Плот" },
            { "calmSea", "Штиль" },
            { "instantDesalinator", "Мгн. Опреснитель" },
            { "autoCropHarvest", "Авто-Сбор Урожая" },
            { "perpetualGrass", "Вечная Трава" },
            { "autoWaterCrops", "Авто-Полив" },
            { "instantBiofuel", "Мгн. Биотопливо" },
            { "hyperBeehives", "Гипер-Ульи" },
            { "instantSmeltCook", "Мгн. Плавка / Еда" },
            { "instantFishing", "Мгновенный Клёв" },
            { "autoFishing", "Авто-Рыбалка" },
            { "instantDomesticProduction", "Ресурсы Зверей x10" },
            { "autoShearer", "Авто-Стрижка" },
            { "infiniteBattery", "Вечные Батарейки" },
            { "infiniteFuel", "Бесконечное Топливо" },
            { "noFuelLanterns", "Вечные Фонари" },
            { "instantHarvest", "Моментальная Добыча" },
            { "antiSeagull", "Защита от Чаек" },
            { "autoLootPickups", "Авто-Сбор Мусора" },
            { "itemMagnet", "Магнит Лута" },
            { "autoAnchorOnArrival", "Якорь у Острова" },
            { "autoCooker", "Авто-Гриль" },
            { "autoSmelter", "Авто-Плавильня" },
            { "autoZipline", "Зиплайн + Сбор" },
            { "animalBrush", "Кисть Животных" },
            { "aimbotAnimals", "Aimbot Звери" },
            { "aimbotItems", "Aimbot Вещи" },
            { "aimbotAlwaysOn", "Аим Всегда" },
            { "instantTame", "Мгновенное Приручение" },
            { "instantTreasureDig", "Мгн. Раскопки" },
            { "keybindsEnabled", "Горячие Клавиши" },
            { "hudActiveCheats", "HUD Активных Читов" },
            { "hudSessionStats", "HUD Статистики" },
            { "hudWeatherTimer", "HUD Погоды" },
            { "hudCompass", "HUD Компаса" },
            { "hudMinimap", "Миникарта" },
            { "eventLogEnabled", "Журнал Событий" },
            { "sharkAlarm", "Сигнализация Акул" },
            { "stormMode", "Режим Шторма" },
            { "autoBatteryCharge", "Авто-Заряд Батареек" },
            { "autoTrash", "Авто-Выброс Мусора" },
            { "photoMode", "Фото-Режим" },
            { "autopilotEnabled", "Автопилот Плот" },
            { "selectMode", "Выделение Блоков" },
            { "rulerOn", "Строительная Линейка" },
            { "perfHud", "Тахометр Производительности" },
            { "espTracers", "ESP-Линии (Трассеры)" },
            { "sharkTrailOn", "След Акул" },
            { "espCrops", "ESP Урожая" },
            { "hudMachines", "Дашборд Машин" },
            { "autoRefuelEngines", "Авто-Дозаправка" },
            { "antiStormRaft", "Анти-Шторм Автопилот" },
            { "craftHelperOn", "Помощник Крафта" },
            { "fastNight", "Быстрая Ночь" },
            { "autoloadProfile", "Автозагрузка Профиля" },
            { "lootFilterFood", "Фильтр Лута: Еда" },
            { "lootFilterMetal", "Фильтр Лута: Ресурсы" },
            { "lootFilterRare", "Фильтр Лута: Редкое" },
            { "worldMapOn", "Карта Мира" },
            { "worldMapZoom", "Зум Карты" },
            { "smartReminders", "Умные Напоминания" },
            { "autoCollectNests", "Авто-Сбор Гнёзд" },
            { "autoPlantSeeds", "Авто-Посадка Семян" },
            { "plantKeywords", "Ключи Семян" },
            { "autoRadarScan", "Авто-Сканер Радара" },
            { "magnetOnlyFood", "Магнит: только Еда" },
            { "magnetOnlyMetal", "Магнит: только Металл" },
            { "magnetOnlyRare", "Магнит: только Редкое" },
            { "rescueHook", "Спасательный Крюк" },
            { "rescueDelay", "Задержка Спасения" },
            { "thermalVision", "Тепловизор" },
            { "ratExterminator", "Крысолов" },
            { "petsAlwaysFed", "Питомцы Всегда Сыты" },
            { "autoFishPro", "Рыбалка 2.0" },
            { "baitKeyword", "Ключ Наживки" },
            { "biofuelRawPerUnit", "Сырья на 1 Топливо" },
            { "hudPlayersTable", "HUD Таблица Игроков" },
            { "hudTeammateStatus", "HUD Статусы Тиммейтов" },
            { "floorPasteOffset", "Смещение Этажа" }
        };

        // Не показывать в HUD активных читов (это настройки интерфейса)
        private static readonly HashSet<string> hudHiddenFeatures = new HashSet<string>
        {
            "keybindsEnabled", "hudActiveCheats", "hudSessionStats", "hudWeatherTimer",
            "hudCompass", "hudMinimap", "eventLogEnabled", "photoMode", "waypointEsp",
            "espMaster", "selectMode", "rulerOn", "perfHud", "hudMachines", "autoloadProfile",
            "worldMapOn", "hudPlayersTable", "smartReminders", "hudTeammateStatus"
        };

        // Горячие клавиши
        private readonly Dictionary<string, KeyCode> keybinds = new Dictionary<string, KeyCode>();
        private readonly string[] keybindActionNames = new string[]
        {
            "menu", "teleportraft", "photo", "macro", "autopilot", "ruler", "esp", "fly", "god",
            "sortinv", "recall1", "recall2", "recall3"
        };
        private readonly string[] keybindActionTitles = new string[]
        {
            "Меню", "ТП на плот", "Скриншот", "Макро-рутина", "Автопилот", "Линейка", "ESP", "Полёт", "Год-мод",
            "Сортировка сумки", "Слот позиции 1", "Слот позиции 2", "Слот позиции 3"
        };
        private string bindCaptureAction = null;
        private static KeyCode[] allKeyCodes = null;

        // Фото-режим
        private bool uiHiddenForShot = false;

        // Журнал событий
        private readonly List<string> eventLog = new List<string>();
        private const int EventLogMax = 200;
        private string lastLoggedStatus = "";

        // Сигнализация акул
        private float sharkAlarmUntil = 0f;
        private float nextAlarmCheckTime = 0f;
        private float nextAlarmBeepTime = 0f;
        private AudioClip alarmClip = null;
        private AudioSource alarmSource = null;

        // Миникарта
        private readonly List<MinimapBlip> minimapBlips = new List<MinimapBlip>();
        private readonly List<string> activeCheatNames = new List<string>();

        // Погода
        private WeatherManager cachedWeather = null;
        private float nextWeatherCacheTime = 0f;

        // Автопилот
        public bool autopilotToIsland = true;
        public int autopilotWaypointIndex = -1;
        private readonly List<Landmark> autopilotLandmarks = new List<Landmark>();
        private float nextAutopilotLandmarkTime = 0f;
        private float nextAutopilotLogTime = 0f;

        // Статистика сессии
        private float sessionStartTime = -1f;
        private int sessionItems = 0;
        private float sessionDistance = 0f;
        private Vector3 lastStatsPos = Vector3.zero;
        private int lastInvTotal = -1;
        private bool sessionInitialized = false;
        private float nextStatsTime = 0f;

        // Снимок инвентаря
        private RGD_Slot[] inventorySnapshot = null;
        private bool hasInventorySnapshot = false;

        // Таймеры V12
        private float nextBatteryTime = 0f;
        private float nextTrashTime = 0f;
        private float nextStormTime = 0f;
        private static readonly MethodInfo mRechargeBattery = typeof(BatteryCharger).GetMethod("RechargeBattery", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        // План плота (копия фундаментов)
        private readonly List<BlueprintBlock> blueprintBlocks = new List<BlueprintBlock>();

        // Наборы (пресеты) спавна
        private readonly Item_Base[] presetItems = new Item_Base[3];
        private readonly int[] presetAmounts = new int[] { 20, 20, 20 };

        // Макро-рутина
        private bool macroRunning = false;
        private int macroStep = 0;
        private float macroNextStepTime = 0f;

        // ---------- ГЛАВНЫЙ ЦИКЛ V12 ----------
        private void UpdateV12Features(Network_Player player)
        {
            EnsureToggleCaches();
            if (sessionStartTime < 0f) sessionStartTime = Time.time;

            bool fastTick = Time.time > nextV12FastTime;
            if (fastTick) nextV12FastTime = Time.time + 0.15f;

            if (stormMode && Time.time > nextStormTime) { nextStormTime = Time.time + 6f; ApplyStormMode(); }
            if (autoBatteryCharge && Time.time > nextBatteryTime) { nextBatteryTime = Time.time + 1.5f; ApplyAutoBattery(); }
            if (autoTrash && Time.time > nextTrashTime) { nextTrashTime = Time.time + 2f; ApplyAutoTrash(player); }

            TickMacro();
            UpdateSessionStats(player);
            HandleMouseTools(player);
            UpdateSharkAlarm(player);

            if (Time.time > nextV12ThrottleTime)
            {
                nextV12ThrottleTime = Time.time + 0.5f;
                RefreshMinimapBlips();
                RefreshActiveCheatNames();
                UpdateWeatherCache();
                WatchStatusForLog();

                if (autopilotEnabled)
                {
                    if (autopilotToIsland && Time.time > nextAutopilotLandmarkTime)
                    {
                        nextAutopilotLandmarkTime = Time.time + 2f;
                        autopilotLandmarks.Clear();
                        try
                        {
                            var lms = UnityEngine.Object.FindObjectsOfType<Landmark>();
                            for (int i = 0; i < lms.Length; i++) if (lms[i] != null) autopilotLandmarks.Add(lms[i]);
                        }
                        catch {}
                    }
                    if (Time.time > nextAutopilotLogTime)
                    {
                        nextAutopilotLogTime = Time.time + 5f;
                        Raft raft = ComponentManager<Raft>.Value;
                        if (raft != null)
                        {
                            Vector3 target = GetAutopilotTarget(raft.transform.position);
                            if (target != Vector3.zero)
                                statusMessage = "Автопилот: до цели " + Vector3.Distance(raft.transform.position, target).ToString("F0") + " м";
                        }
                    }
                }
            }
        }

        // ---------- ЖУРНАЛ ----------
        private void LogEvent(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return;
                eventLog.Add("[" + DateTime.Now.ToString("HH:mm") + "] " + text);
                if (eventLog.Count > EventLogMax) eventLog.RemoveRange(0, eventLog.Count - EventLogMax);
            }
            catch {}
        }

        private void WatchStatusForLog()
        {
            if (statusMessage != lastLoggedStatus)
            {
                lastLoggedStatus = statusMessage;
                LogEvent(statusMessage);
            }
        }

        // ---------- ГОРЯЧИЕ КЛАВИШИ ----------
        private void EnsureKeybinds()
        {
            if (keybinds.Count > 0) return;
            keybinds["menu"] = KeyCode.F4;
            keybinds["teleportraft"] = KeyCode.F5;
            keybinds["photo"] = KeyCode.F6;
            keybinds["macro"] = KeyCode.F7;
            keybinds["autopilot"] = KeyCode.F9;
            keybinds["ruler"] = KeyCode.F10;
            keybinds["esp"] = KeyCode.F12;
            keybinds["fly"] = KeyCode.F2;
            keybinds["god"] = KeyCode.F1;
            keybinds["sortinv"] = KeyCode.O;
            keybinds["recall1"] = KeyCode.F8;
            keybinds["recall2"] = KeyCode.None;
            keybinds["recall3"] = KeyCode.None;
        }

        private string KeybindTitle(string action)
        {
            for (int i = 0; i < keybindActionNames.Length; i++)
                if (keybindActionNames[i] == action) return keybindActionTitles[i];
            return action;
        }

        private void ProcessKeybinds()
        {
            try
            {
                EnsureKeybinds();

                if (bindCaptureAction != null)
                {
                    if (allKeyCodes == null) allKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
                    for (int i = 0; i < allKeyCodes.Length; i++)
                    {
                        KeyCode code = allKeyCodes[i];
                        if (code == KeyCode.None) continue;
                        if (Input.GetKeyDown(code))
                        {
                            keybinds[bindCaptureAction] = code;
                            statusMessage = "Клавиша \"" + KeybindTitle(bindCaptureAction) + "\" → " + code;
                            LogEvent("Бинд: " + KeybindTitle(bindCaptureAction) + " = " + code);
                            bindCaptureAction = null;
                            break;
                        }
                    }
                    return;
                }

                if (!keybindsEnabled) return;

                foreach (KeyValuePair<string, KeyCode> pair in keybinds)
                {
                    if (pair.Value == KeyCode.None) continue;
                    if (Input.GetKeyDown(pair.Value)) DispatchKeybindAction(pair.Key);
                }
            }
            catch {}
        }

        private void DispatchKeybindAction(string action)
        {
            switch (action)
            {
                case "menu": showMenu = !showMenu; break;
                case "teleportraft": TeleportToRaft(); break;
                case "photo": TakeScreenshot(); break;
                case "macro": StartMacro(); break;
                case "autopilot": ToggleAutopilot(); break;
                case "ruler":
                    rulerOn = !rulerOn;
                    statusMessage = rulerOn ? "Линейка ВКЛ (ЛКМ в мир)" : "Линейка ВЫКЛ";
                    LogEvent("Линейка: " + (rulerOn ? "ВКЛ" : "ВЫКЛ"));
                    break;
                case "esp":
                    espMaster = !espMaster;
                    LogEvent("ESP: " + (espMaster ? "ВКЛ" : "ВЫКЛ"));
                    break;
                case "fly":
                    flyMode = !flyMode;
                    LogEvent("Полёт: " + (flyMode ? "ВКЛ" : "ВЫКЛ"));
                    break;
                case "god":
                    godMode = !godMode;
                    LogEvent("Бессмертие: " + (godMode ? "ВКЛ" : "ВЫКЛ"));
                    break;
                case "sortinv":
                    SortInventoryNow();
                    break;
                case "recall1":
                    GoPosSlot(0);
                    break;
                case "recall2":
                    GoPosSlot(1);
                    break;
                case "recall3":
                    GoPosSlot(2);
                    break;
            }
        }

        // ---------- АВТОПИЛОТ ----------
        public void ToggleAutopilot()
        {
            autopilotEnabled = !autopilotEnabled;
            if (autopilotEnabled)
            {
                autopilotToIsland = true;
                statusMessage = "Автопилот: курс на ближайший остров";
            }
            else statusMessage = "Автопилот выключен";
            LogEvent("Автопилот: " + (autopilotEnabled ? "ВКЛ" : "ВЫКЛ"));
        }

        public void SetAutopilotToIsland()
        {
            autopilotToIsland = true;
            autopilotEnabled = true;
            statusMessage = "Автопилот: курс на ближайший остров";
            LogEvent("Автопилот: остров");
        }

        public void SetAutopilotToWaypoint()
        {
            if (waypoints.Count == 0) { statusMessage = "Нет точек для автопилота"; return; }
            autopilotWaypointIndex = waypoints.Count - 1;
            autopilotToIsland = false;
            autopilotEnabled = true;
            statusMessage = "Автопилот: курс на точку \"" + waypoints[autopilotWaypointIndex].name + "\"";
            LogEvent("Автопилот: точка " + waypoints[autopilotWaypointIndex].name);
        }

        private Vector3 GetAutopilotTarget(Vector3 fromPos)
        {
            if (autopilotToIsland)
            {
                Landmark best = null;
                float bd = float.MaxValue;
                for (int i = 0; i < autopilotLandmarks.Count; i++)
                {
                    Landmark lm = autopilotLandmarks[i];
                    if (lm == null) continue;
                    float d = Vector3.Distance(lm.transform.position, fromPos);
                    if (d < bd) { bd = d; best = lm; }
                }
                return best != null ? best.transform.position : Vector3.zero;
            }
            if (autopilotWaypointIndex >= 0 && autopilotWaypointIndex < waypoints.Count)
                return waypoints[autopilotWaypointIndex].position;
            return Vector3.zero;
        }

        // Вызывается из FixedUpdate
        private void UpdateAutopilotPhysics()
        {
            if (!autopilotEnabled) return;
            try
            {
                Raft raft = ComponentManager<Raft>.Value;
                if (raft == null) { autopilotEnabled = false; return; }

                Vector3 target = GetAutopilotTarget(raft.transform.position);
                if (target == Vector3.zero)
                {
                    autopilotEnabled = false;
                    statusMessage = "Автопилот: цель не найдена";
                    return;
                }

                Vector3 delta = target - raft.transform.position;
                delta.y = 0f;
                float dist = delta.magnitude;
                float stopDist = autopilotToIsland ? 35f : 10f;

                if (dist <= stopDist)
                {
                    autopilotEnabled = false;
                    try { raft.AddAnchor(false, null); } catch {}
                    statusMessage = "Автопилот: цель достигнута (" + dist.ToString("F0") + " м), якорь брошен";
                    LogEvent("Автопилот завершен");
                    return;
                }

                Vector3 dir = delta / dist;
                raft.AddForce(dir, autopilotSpeed * 2.2f, ForceMode.Acceleration, false);
            }
            catch (Exception ex)
            {
                autopilotEnabled = false;
                statusMessage = "Ошибка автопилота: " + ex.Message;
            }
        }

        // ---------- ЛКМ-ИНСТРУМЕНТЫ (линейка / выделение) ----------
        private void HandleMouseTools(Network_Player player)
        {
            if (!Input.GetMouseButtonDown(0) || showMenu) return;
            if (animalBrush) return; // ЛКМ занята кистью животных

            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 300f)) return;

            if (selectMode)
            {
                Block block = hit.collider.GetComponentInParent<Block>();
                if (block == null) { statusMessage = "Это не блок плота"; return; }
                if (selectedBlocks.Contains(block)) selectedBlocks.Remove(block);
                else if (selectedBlocks.Count < 60) selectedBlocks.Add(block);
                statusMessage = "Выделено блоков: " + selectedBlocks.Count;
            }
            else if (rulerOn)
            {
                if (!hasRulerA || (hasRulerA && hasRulerB))
                {
                    rulerA = hit.point;
                    hasRulerA = true;
                    hasRulerB = false;
                    statusMessage = "Линейка: точка A поставлена";
                }
                else
                {
                    rulerB = hit.point;
                    hasRulerB = true;
                    float d = Vector3.Distance(rulerA, rulerB);
                    statusMessage = "Линейка: расстояние " + d.ToString("F2") + " м";
                    LogEvent("Линейка: " + d.ToString("F2") + " м");
                }
            }
        }

        // ---------- СИГНАЛИЗАЦИЯ АКУЛ ----------
        private void UpdateSharkAlarm(Network_Player player)
        {
            if (!sharkAlarm || player == null) return;
            if (Time.time < nextAlarmCheckTime) return;
            nextAlarmCheckTime = Time.time + 0.5f;

            Vector3 pp = player.transform.position;
            float best = float.MaxValue;
            for (int i = 0; i < cachedSharksInfo.Count; i++)
            {
                SharkESPInfo info = cachedSharksInfo[i];
                if (info == null || info.transform == null) continue;
                float d = Vector3.Distance(info.transform.position, pp);
                if (d < best) best = d;
            }

            if (best <= sharkAlarmRadius)
            {
                bool wasSilent = Time.time > sharkAlarmUntil;
                sharkAlarmUntil = Time.time + 2.5f;
                if (wasSilent)
                {
                    statusMessage = "⚠ ВНИМАНИЕ: акула в " + best.ToString("F0") + " м!";
                    LogEvent("⚠ Акула в радиусе " + best.ToString("F0") + " м");
                }
                if (Time.time > nextAlarmBeepTime)
                {
                    nextAlarmBeepTime = Time.time + 2f;
                    PlayAlarmBeep();
                }
            }
        }

        private void PlayAlarmBeep()
        {
            try
            {
                if (alarmClip == null)
                {
                    int samples = 4800; // 0.6 сек при 8 кГц
                    alarmClip = AudioClip.Create("__RT_Alarm__", samples, 1, 8000, false);
                    float[] data = new float[samples];
                    for (int i = 0; i < samples; i++)
                    {
                        float t = (float)i / 8000f;
                        float env = 1f;
                        if (i < samples * 0.1f) env = (float)i / (samples * 0.1f);
                        else if (i > samples * 0.8f) env = (float)(samples - i) / (samples * 0.2f);
                        data[i] = Mathf.Sin(t * 2f * Mathf.PI * 880f) * 0.5f * env;
                    }
                    alarmClip.SetData(data, 0);
                }
                if (alarmSource == null)
                {
                    GameObject go = new GameObject("__RT_AlarmSrc__");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    alarmSource = go.AddComponent<AudioSource>();
                    alarmSource.playOnAwake = false;
                    alarmSource.volume = 0.6f;
                }
                if (!alarmSource.isPlaying) alarmSource.PlayOneShot(alarmClip);
            }
            catch {}
        }

        // ---------- ФОТО-РЕЖИМ ----------
        private void TakeScreenshot()
        {
            try
            {
                uiHiddenForShot = true;
                string name = "RaftTrainer_Photo_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
                ScreenCapture.CaptureScreenshot(name);
                LogEvent("Скриншот: " + name);
                statusMessage = "Скриншот сохранен: " + name;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка скриншота: " + ex.Message;
            }
        }

        // ---------- ПОГОДА / ШТОРМ ----------
        private void UpdateWeatherCache()
        {
            if (Time.time < nextWeatherCacheTime) return;
            nextWeatherCacheTime = Time.time + 3f;
            try { cachedWeather = UnityEngine.Object.FindObjectOfType<WeatherManager>(); }
            catch { cachedWeather = null; }
        }

        private void ApplyStormMode()
        {
            try
            {
                WeatherManager wm = UnityEngine.Object.FindObjectOfType<WeatherManager>();
                if (wm == null) return;
                string cur = "";
                try { cur = wm.GetCurrentWeatherType().ToString(); } catch {}
                if (!cur.ToLower().Contains("storm")) wm.SetWeather("Storm", false);
            }
            catch {}
        }

        // ---------- АВТО-ЗАРЯД БАТАРЕЕК ----------
        private void ApplyAutoBattery()
        {
            try
            {
                var chargers = UnityEngine.Object.FindObjectsOfType<BatteryCharger>();
                for (int i = 0; i < chargers.Length; i++)
                {
                    BatteryCharger c = chargers[i];
                    if (c == null) continue;
                    if (mRechargeBattery != null)
                    {
                        if (c.batteryL != null) mRechargeBattery.Invoke(c, new object[] { c.batteryL });
                        if (c.batteryR != null) mRechargeBattery.Invoke(c, new object[] { c.batteryR });
                    }
                    if (c.fuelTank != null && fFuelCount != null && fMaxFuel != null)
                    {
                        int max = (int)fMaxFuel.GetValue(c.fuelTank);
                        fFuelCount.SetValue(c.fuelTank, max);
                    }
                }
            }
            catch {}
        }

        // ---------- АВТО-ВЫБРОС МУСОРА ----------
        private void ApplyAutoTrash(Network_Player player)
        {
            if (player == null || player.Inventory == null) return;
            try
            {
                string raw = trashKeywords.ToLower();
                string[] parts = raw.Split(',');
                List<string> keys = new List<string>();
                for (int i = 0; i < parts.Length; i++)
                {
                    string k = parts[i].Trim();
                    if (k.Length > 0) keys.Add(k);
                }
                if (keys.Count == 0) return;

                Inventory inv = player.Inventory;
                if (inv.allSlots == null) return;

                List<string> names = new List<string>();
                List<int> amounts = new List<int>();
                for (int i = 0; i < inv.allSlots.Count; i++)
                {
                    Slot s = inv.allSlots[i];
                    if (s == null || !s.HasValidItemInstance() || s.itemInstance.baseItem == null) continue;
                    string un = s.itemInstance.baseItem.UniqueName.ToLower();
                    for (int k = 0; k < keys.Count; k++)
                    {
                        if (un.Contains(keys[k]))
                        {
                            names.Add(s.itemInstance.baseItem.UniqueName);
                            amounts.Add(s.itemInstance.Amount);
                            break;
                        }
                    }
                }

                int removed = 0;
                for (int i = 0; i < names.Count; i++)
                {
                    inv.RemoveItem(names[i], amounts[i]);
                    removed += amounts[i];
                }
                if (removed > 0)
                {
                    statusMessage = "Авто-мусорка: уничтожено " + removed + " шт.";
                    LogEvent("Авто-мусорка: " + removed + " шт.");
                }
            }
            catch {}
        }

        // ---------- СНИМОК / ВОССТАНОВЛЕНИЕ ИНВЕНТАРЯ ----------
        private void SaveInventorySnapshot()
        {
            try
            {
                Network_Player player = ComponentManager<Network_Player>.Value;
                if (player == null || player.Inventory == null) { statusMessage = "Инвентарь не найден"; return; }
                inventorySnapshot = player.Inventory.GetRGDSlots();
                hasInventorySnapshot = inventorySnapshot != null && inventorySnapshot.Length > 0;
                statusMessage = hasInventorySnapshot ? "Снимок инвентаря сохранен" : "Снимок пуст";
                LogEvent("Снимок инвентаря сохранен");
            }
            catch (Exception ex) { statusMessage = "Ошибка снимка: " + ex.Message; }
        }

        private void RestoreInventorySnapshot()
        {
            try
            {
                if (!hasInventorySnapshot || inventorySnapshot == null) { statusMessage = "Снимка нет — сначала запомните"; return; }
                Network_Player player = ComponentManager<Network_Player>.Value;
                if (player == null || player.Inventory == null) return;
                player.Inventory.SetSlotsFromRGD(inventorySnapshot);
                statusMessage = "Инвентарь восстановлен из снимка";
                LogEvent("Инвентарь восстановлен");
            }
            catch (Exception ex) { statusMessage = "Ошибка восстановления: " + ex.Message; }
        }

        // ---------- СТАТИСТИКА СЕССИИ ----------
        private void UpdateSessionStats(Network_Player player)
        {
            if (Time.time < nextStatsTime) return;
            nextStatsTime = Time.time + 1f;
            try
            {
                if (player != null)
                {
                    if (sessionInitialized) sessionDistance += Vector3.Distance(player.transform.position, lastStatsPos);
                    lastStatsPos = player.transform.position;
                    sessionInitialized = true;

                    if (player.Inventory != null && player.Inventory.allSlots != null)
                    {
                        int total = 0;
                        for (int i = 0; i < player.Inventory.allSlots.Count; i++)
                        {
                            Slot s = player.Inventory.allSlots[i];
                            if (s != null && s.HasValidItemInstance() && s.itemInstance != null) total += s.itemInstance.Amount;
                        }
                        if (lastInvTotal >= 0 && total > lastInvTotal) sessionItems += total - lastInvTotal;
                        lastInvTotal = total;
                    }
                }
            }
            catch {}
        }

        private void ResetSessionStats()
        {
            sessionItems = 0;
            sessionDistance = 0f;
            sessionStartTime = Time.time;
            lastInvTotal = -1;
            sessionInitialized = false;
            statusMessage = "Статистика сессии сброшена";
        }

        private string FormatSessionTime()
        {
            float t = Mathf.Max(0f, Time.time - sessionStartTime);
            return FormatTimeSpan(t);
        }

        private static string FormatTimeSpan(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int h = total / 3600;
            int m = (total % 3600) / 60;
            int s = total % 60;
            if (h > 0) return h.ToString("00") + ":" + m.ToString("00") + ":" + s.ToString("00");
            return m.ToString("00") + ":" + s.ToString("00");
        }

        private static string FormatDistance(float meters)
        {
            if (meters < 1000f) return meters.ToString("F0") + " м";
            return (meters / 1000f).ToString("F2") + " км";
        }

        // ---------- ПЛАН ПЛОТА (копия фундаментов) ----------
        private void CopyRaftBlueprint()
        {
            try
            {
                Raft raft = ComponentManager<Raft>.Value;
                if (raft == null) { statusMessage = "Плот не найден"; return; }
                blueprintBlocks.Clear();
                var blocks = UnityEngine.Object.FindObjectsOfType<Block>();
                for (int i = 0; i < blocks.Length; i++)
                {
                    Block b = blocks[i];
                    if (b == null || !(b is Block_Foundation)) continue;
                    BlueprintBlock bp = new BlueprintBlock();
                    bp.LocalPos = raft.transform.InverseTransformPoint(b.transform.position);
                    bp.Reinforced = b.Reinforced;
                    blueprintBlocks.Add(bp);
                }
                blueprintHasCopy = blueprintBlocks.Count > 0;
                statusMessage = blueprintHasCopy
                    ? "План скопирован: " + blueprintBlocks.Count + " фундаментов"
                    : "Фундаменты не найдены";
                LogEvent("План плота скопирован (" + blueprintBlocks.Count + ")");
            }
            catch (Exception ex) { statusMessage = "Ошибка копии плана: " + ex.Message; }
        }

        private void PasteRaftBlueprint()
        {
            try
            {
                if (!blueprintHasCopy) { statusMessage = "Сначала скопируйте план"; return; }
                BlockCreator bc = UnityEngine.Object.FindObjectOfType<BlockCreator>();
                if (bc == null) { statusMessage = "BlockCreator не найден"; return; }
                Item_Base item = ItemManager.GetItemByName("Foundation");
                if (item == null) { statusMessage = "Предмет Foundation не найден"; return; }

                int placed = 0;
                Vector3 offset = new Vector3(blueprintPasteOffset, 0f, blueprintPasteOffset);
                Raft raft = ComponentManager<Raft>.Value;
                float bSize = BlockCreator.BlockSize > 0.5f ? BlockCreator.BlockSize : 1.5f;
                HashSet<string> used = new HashSet<string>();
                for (int i = 0; i < blueprintBlocks.Count; i++)
                {
                    try
                    {
                        if (raft != null)
                        {
                            Vector3 world = raft.transform.TransformPoint(blueprintBlocks[i].LocalPos + offset);
                            BlockCreator.SnapBuildPosition(ref world);
                            Vector3 local = raft.transform.InverseTransformPoint(world);
                            local.y = blueprintBlocks[i].LocalPos.y;
                            string key = Mathf.RoundToInt(local.x / bSize * 2f) + ":" + Mathf.RoundToInt(local.z / bSize * 2f);
                            if (!used.Add(key)) continue;
                            bc.CreateBlockCheat(item, local, Vector3.zero, DPS.Default, 0);
                        }
                        else
                        {
                            bc.CreateBlockCheat(item, blueprintBlocks[i].LocalPos + offset, Vector3.zero, DPS.Default, 0);
                        }
                        placed++;
                    }
                    catch {}
                }
                statusMessage = "План вставлен: " + placed + " фундаментов";
                LogEvent("План плота вставлен (" + placed + ")");
            }
            catch (Exception ex) { statusMessage = "Ошибка вставки плана: " + ex.Message; }
        }

        // ---------- ДЕЙСТВИЯ С ВЫДЕЛЕННЫМИ БЛОКАМИ ----------
        private void RepairSelectedBlocks()
        {
            try
            {
                int count = 0;
                for (int i = 0; i < selectedBlocks.Count; i++)
                {
                    Block b = selectedBlocks[i];
                    if (b != null && b.Health < b.MaxHealth) { b.SetHealth(b.MaxHealth); count++; }
                }
                statusMessage = "Починено выделенных блоков: " + count;
            }
            catch (Exception ex) { statusMessage = "Ошибка ремонта: " + ex.Message; }
        }

        private void PaintSelectedBlocks()
        {
            try
            {
                if (selectedBlocks.Count == 0) { statusMessage = "Сначала выделите блоки (ЛКМ)"; return; }
                SO_ColorValue asset = CreatePaintAsset();
                int count = 0;
                for (int i = 0; i < selectedBlocks.Count; i++)
                {
                    Block b = selectedBlocks[i];
                    if (b != null) { b.SetInstanceColorAndPattern(asset, asset, 0, 0u); count++; }
                }
                statusMessage = "Покрашено выделенных блоков: " + count;
            }
            catch (Exception ex) { statusMessage = "Ошибка покраски: " + ex.Message; }
        }

        private void DeleteSelectedBlocks()
        {
            try
            {
                int count = 0;
                for (int i = 0; i < selectedBlocks.Count; i++)
                {
                    Block b = selectedBlocks[i];
                    if (b != null) { UnityEngine.Object.Destroy(b.gameObject); count++; }
                }
                selectedBlocks.Clear();
                statusMessage = "Снесено выделенных блоков: " + count;
                LogEvent("Снесено блоков: " + count);
            }
            catch (Exception ex) { statusMessage = "Ошибка сноса: " + ex.Message; }
        }

        // ---------- НАБОРЫ (ПРЕСЕТЫ) СПАВНА ----------
        private void SaveSpawnPreset(int idx)
        {
            if (selectedItem == null) { statusMessage = "Выберите предмет в списке спавнера"; return; }
            int amount;
            if (!int.TryParse(customAmountStr, out amount) || amount <= 0) amount = 20;
            presetItems[idx] = selectedItem;
            presetAmounts[idx] = amount;
            statusMessage = "Набор " + (idx + 1) + ": " + GetItemTitle(selectedItem) + " x" + amount;
            LogEvent("Набор " + (idx + 1) + " сохранен");
        }

        private void GiveSpawnPreset(int idx)
        {
            Item_Base it = presetItems[idx];
            if (it == null) { statusMessage = "Набор " + (idx + 1) + " пуст"; return; }
            SpawnItem(it.UniqueName, presetAmounts[idx]);
        }

        // ---------- МАКРО-РУТИНА ----------
        private void StartMacro()
        {
            if (macroRunning) return;
            macroRunning = true;
            macroStep = 0;
            macroNextStepTime = Time.time + 0.3f;
            LogEvent("Макро-рутина запущена");
        }

        private void TickMacro()
        {
            if (!macroRunning) return;
            if (Time.time < macroNextStepTime) return;
            macroNextStepTime = Time.time + 0.45f;

            switch (macroStep)
            {
                case 0: TeleportToRaft(); macroStep++; break;
                case 1: RepairAllRaftBlocks(); macroStep++; break;
                case 2: SmartChestAutoSort(ComponentManager<Network_Player>.Value); macroStep++; break;
                case 3: EmptyAllCollectionNets(); macroStep++; break;
                case 4: CollectAllDomesticYield(ComponentManager<Network_Player>.Value); macroStep++; break;
                case 5: WaterAllCrops(); macroStep++; break;
                case 6: InstantGrowCrops(); macroStep++; break;
                default:
                    macroRunning = false;
                    statusMessage = "Макро-рутина завершена ✓";
                    LogEvent("Макро-рутина завершена");
                    break;
            }
        }

        // ---------- ПРОФИЛИ ----------
        private string GetProfilePath(string name)
        {
            try { Directory.CreateDirectory(ProfileDirName); } catch {}
            string safe = name == null ? "" : name.Trim();
            foreach (char c in Path.GetInvalidFileNameChars()) safe = safe.Replace(c, '_');
            if (safe.Length == 0) safe = "profile";
            return Path.Combine(ProfileDirName, safe + ".txt");
        }

        private void EnsureToggleCaches()
        {
            if (v12CachesReady) return;
            try
            {
                FieldInfo[] all = typeof(TrainerGUI).GetFields(BindingFlags.Public | BindingFlags.Instance);
                List<FieldInfo> bools = new List<FieldInfo>();
                List<FieldInfo> floats = new List<FieldInfo>();
                List<FieldInfo> ints = new List<FieldInfo>();
                for (int i = 0; i < all.Length; i++)
                {
                    FieldInfo f = all[i];
                    Type ft = f.FieldType;
                    if (ft == typeof(bool)) bools.Add(f);
                    else if (ft == typeof(float)) floats.Add(f);
                    else if (ft == typeof(int)) ints.Add(f);
                }
                bools.Sort(delegate(FieldInfo a, FieldInfo b)
                {
                    return string.CompareOrdinal(GetFeatureLabel(a.Name), GetFeatureLabel(b.Name));
                });
                profileBoolFields = bools.ToArray();
                profileFloatFields = floats.ToArray();
                profileIntFields = ints.ToArray();
                v12CachesReady = true;
            }
            catch {}
        }

        private static string GetFeatureLabel(string fieldName)
        {
            string label;
            if (featureLabels.TryGetValue(fieldName, out label)) return label;
            return fieldName;
        }

        private void RefreshActiveCheatNames()
        {
            activeCheatNames.Clear();
            if (profileBoolFields == null) return;
            for (int i = 0; i < profileBoolFields.Length; i++)
            {
                FieldInfo f = profileBoolFields[i];
                if (hudHiddenFeatures.Contains(f.Name)) continue;
                try { if ((bool)f.GetValue(this)) activeCheatNames.Add(GetFeatureLabel(f.Name)); }
                catch {}
            }
        }

        private void SaveProfileFile(string name)
        {
            try
            {
                EnsureToggleCaches();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# RaftTrainer v12 profile: " + name);
                if (profileBoolFields != null)
                    for (int i = 0; i < profileBoolFields.Length; i++)
                    {
                        FieldInfo f = profileBoolFields[i];
                        if (profileSkipFields.Contains(f.Name)) continue;
                        sb.Append(f.Name).Append('=').Append((bool)f.GetValue(this) ? "1" : "0").AppendLine();
                    }
                if (profileFloatFields != null)
                    for (int i = 0; i < profileFloatFields.Length; i++)
                    {
                        FieldInfo f = profileFloatFields[i];
                        sb.Append(f.Name).Append('=').Append(((float)f.GetValue(this)).ToString(System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
                    }
                if (profileIntFields != null)
                    for (int i = 0; i < profileIntFields.Length; i++)
                    {
                        FieldInfo f = profileIntFields[i];
                        sb.Append(f.Name).Append('=').Append(((int)f.GetValue(this)).ToString(System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
                    }
                EnsureKeybinds();
                foreach (KeyValuePair<string, KeyCode> pair in keybinds)
                    sb.Append("bind_").Append(pair.Key).Append('=').Append(pair.Value.ToString()).AppendLine();

                File.WriteAllText(GetProfilePath(name), sb.ToString());
                profileListCache = null;
                statusMessage = "Профиль сохранен: " + name;
                LogEvent("Профиль сохранен: " + name);
            }
            catch (Exception ex) { statusMessage = "Ошибка профиля: " + ex.Message; }
        }

        private void LoadProfileFile(string name)
        {
            try
            {
                string path = GetProfilePath(name);
                if (!File.Exists(path)) { statusMessage = "Профиль не найден: " + name; return; }
                EnsureToggleCaches();
                EnsureKeybinds();

                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    if (key.StartsWith("bind_"))
                    {
                        string action = key.Substring(5);
                        KeyCode code;
                        if (Enum.TryParse<KeyCode>(value, true, out code)) keybinds[action] = code;
                        continue;
                    }

                    if (profileBoolFields != null)
                        for (int b = 0; b < profileBoolFields.Length; b++)
                        {
                            FieldInfo f = profileBoolFields[b];
                            if (f.Name != key) continue;
                            if (profileSkipFields.Contains(f.Name)) break;
                            f.SetValue(this, value == "1");
                            break;
                        }
                    if (profileFloatFields != null)
                        for (int b = 0; b < profileFloatFields.Length; b++)
                        {
                            FieldInfo f = profileFloatFields[b];
                            if (f.Name != key) continue;
                            float fv;
                            if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv))
                                f.SetValue(this, fv);
                            break;
                        }
                    if (profileIntFields != null)
                        for (int b = 0; b < profileIntFields.Length; b++)
                        {
                            FieldInfo f = profileIntFields[b];
                            if (f.Name != key) continue;
                            int iv;
                            if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out iv))
                                f.SetValue(this, iv);
                            break;
                        }
                }
                statusMessage = "Профиль загружен: " + name;
                LogEvent("Профиль загружен: " + name);
            }
            catch (Exception ex) { statusMessage = "Ошибка загрузки профиля: " + ex.Message; }
        }

        private string[] ListProfileFiles()
        {
            try
            {
                Directory.CreateDirectory(ProfileDirName);
                string[] files = Directory.GetFiles(ProfileDirName, "*.txt");
                string[] names = new string[files.Length];
                for (int i = 0; i < files.Length; i++) names[i] = Path.GetFileNameWithoutExtension(files[i]);
                return names;
            }
            catch {}
            return new string[0];
        }

        // ---------- ЭКСПОРТ / ИМПОРТ ТОЧЕК ----------
        private const string WaypointFileName = "RaftTrainer_Waypoints.txt";

        private void ExportWaypoints()
        {
            try
            {
                if (waypoints.Count == 0) { statusMessage = "Нет точек для сохранения"; return; }
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < waypoints.Count; i++)
                {
                    TeleportWaypoint wp = waypoints[i];
                    sb.Append(wp.name.Replace(';', ' ')).Append(';')
                      .Append(wp.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(';')
                      .Append(wp.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(';')
                      .Append(wp.position.z.ToString(System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
                }
                File.WriteAllText(WaypointFileName, sb.ToString());
                statusMessage = "Точки экспортированы: " + waypoints.Count;
                LogEvent("Точки экспортированы (" + waypoints.Count + ")");
            }
            catch (Exception ex) { statusMessage = "Ошибка экспорта: " + ex.Message; }
        }

        private void ImportWaypoints()
        {
            try
            {
                if (!File.Exists(WaypointFileName)) { statusMessage = "Файл точек не найден"; return; }
                string[] lines = File.ReadAllLines(WaypointFileName);
                int loaded = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (waypoints.Count >= MaxWaypoints) break;
                    string line = lines[i].Trim();
                    if (line.Length == 0) continue;
                    string[] parts = line.Split(';');
                    if (parts.Length < 4) continue;
                    float x, y, z;
                    if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)) continue;
                    if (!float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y)) continue;
                    if (!float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z)) continue;
                    waypoints.Add(new TeleportWaypoint { name = parts[0], position = new Vector3(x, y, z) });
                    loaded++;
                }
                statusMessage = "Импортировано точек: " + loaded;
                LogEvent("Точки импортированы (" + loaded + ")");
            }
            catch (Exception ex) { statusMessage = "Ошибка импорта: " + ex.Message; }
        }

        // ---------- ДАННЫЕ HUD ----------
        private void RefreshMinimapBlips()
        {
            try
            {
                minimapBlips.Clear();
                if (cachedRaft != null)
                    minimapBlips.Add(new MinimapBlip { Position = cachedRaft.transform.position, Col = new Color(0.25f, 0.85f, 1f), Size = 7f });
                for (int i = 0; i < cachedSharksInfo.Count; i++)
                {
                    var info = cachedSharksInfo[i];
                    if (info == null || info.transform == null) continue;
                    minimapBlips.Add(new MinimapBlip { Position = info.transform.position, Col = new Color(1f, 0.25f, 0.35f), Size = 6f });
                }
                for (int i = 0; i < cachedPlayers.Count; i++)
                {
                    var p = cachedPlayers[i];
                    if (p == null) continue;
                    minimapBlips.Add(new MinimapBlip { Position = p.transform.position, Col = new Color(0.2f, 1f, 0.4f), Size = 5f });
                }
                for (int i = 0; i < cachedLandmarks.Count && i < 12; i++)
                {
                    var lm = cachedLandmarks[i];
                    if (lm == null) continue;
                    minimapBlips.Add(new MinimapBlip { Position = lm.transform.position, Col = new Color(0.2f, 0.9f, 0.35f), Size = 8f });
                }
                for (int i = 0; i < cachedTreasures.Count; i++)
                {
                    var tp = cachedTreasures[i];
                    if (tp == null) continue;
                    minimapBlips.Add(new MinimapBlip { Position = tp.transform.position, Col = new Color(1f, 0.85f, 0.15f), Size = 5f });
                }
                for (int i = 0; i < cachedPickups.Count && i < 20; i++)
                {
                    var p = cachedPickups[i];
                    if (p == null) continue;
                    minimapBlips.Add(new MinimapBlip { Position = p.transform.position, Col = new Color(0.8f, 0.85f, 0.95f, 0.8f), Size = 3f });
                }
                for (int i = 0; i < waypoints.Count; i++)
                    minimapBlips.Add(new MinimapBlip { Position = waypoints[i].position, Col = new Color(0.9f, 0.4f, 1f), Size = 6f });
            }
            catch {}
        }

        private bool GetCompassTarget(out Vector3 targetPos, out string label)
        {
            targetPos = Vector3.zero;
            label = "";
            try
            {
                Network_Player player = ComponentManager<Network_Player>.Value;
                if (player == null) return false;
                Vector3 pp = player.transform.position;

                if (waypoints.Count > 0)
                {
                    int bestIdx = -1;
                    float best = float.MaxValue;
                    for (int i = 0; i < waypoints.Count; i++)
                    {
                        float d = Vector3.Distance(waypoints[i].position, pp);
                        if (d < best) { best = d; bestIdx = i; }
                    }
                    targetPos = waypoints[bestIdx].position;
                    label = "📍 " + waypoints[bestIdx].name;
                    return true;
                }

                if (cachedRaft != null)
                {
                    float d = Vector3.Distance(cachedRaft.transform.position, pp);
                    if (d > 12f)
                    {
                        targetPos = cachedRaft.transform.position;
                        label = "⚓ ПЛОТ";
                        return true;
                    }
                }
            }
            catch {}
            return false;
        }

        private static string TranslateWeather(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "—";
            string low = raw.ToLower();
            if (low.Contains("calm") || low.Contains("clear") || low.Contains("sun")) return "Ясно";
            if (low.Contains("rain")) return "Дождь";
            if (low.Contains("storm")) return "Шторм";
            return raw;
        }

        private static string CardinalName(float headingDeg)
        {
            string[] dirs = { "С", "СВ", "В", "ЮВ", "Ю", "ЮЗ", "З", "СЗ" };
            int idx = ((int)Mathf.Repeat((headingDeg + 22.5f) / 45f, 8f)) % 8;
            return dirs[idx];
        }
    }
}
