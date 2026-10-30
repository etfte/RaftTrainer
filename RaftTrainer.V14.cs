using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RaftTrainer
{
    // ==========================================
    // ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ И СТРУКТУРЫ ДАННЫХ
    // ==========================================
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

    public class TrailPoint
    {
        public Vector3 Pos;
        public float T;
    }

    public class FloorBlockCopy
    {
        public Vector3 LocalPos;
        public Item_Base Item;
    }

    public class ThermalTarget
    {
        public Transform T;
        public Color Col;
    }

    public class TeammateInfo
    {
        public Network_Player Player;
        public string Name = "Игрок";
        public float Dist = 0f;
        public string Status = "";
        public bool Fighting = false;
        public bool Dead = false;
        public float Hp = -1f;
        public float Hunger = -1f;
        public float Thirst = -1f;
        public bool StatsOk = false;
    }

    // ==========================================
    // ПОЛНЫЙ МОДУЛЬ РАСШИРЕНИЙ v14 (TITAN EDITION)
    // Включает: Автопилот, Горячие клавиши, Профили, Линейку, Чертёжник,
    // Тахометр, ESP-трассеры, Дашборд машин, ESP урожая, Авто-дозаправку,
    // Глобальную карту мира, Умные напоминания, Счётчик ресурсов,
    // Авто-гнёзда, Авто-посадку, Авто-сканер радара, Рыбалку 2.0,
    // Спасательный крюк, Тепловизор, Крысолов, Копир этажа, HUD игроков
    // ==========================================
    public partial class TrainerGUI : MonoBehaviour
    {
        private string activeCheatsHeaderStr = "<b>АКТИВНО: 0</b>";
        private string activeCheatsMoreStr = "";
        private string cachedSessionTimeStr = "Время: 00:00";
        private string cachedSessionItemsStr = "Лут: 0 шт";
        private string cachedSessionDistStr = "Путь: 0 м";
        private string perfFpsStr = "<b>60 FPS</b>";
        private string perfMsEntityStr = "16 мс • 0 сущ.";
        private string perfBlockCountStr = "Блоков плота: 0";
        private string machinesEnginesStr = "⚓ Двигатели: 0";
        private string machinesSmeltersStr = "🏭 Плавильни: 0";
        private string machinesCookersStr = "🍳 Грили/печи: 0";
        private string machinesChargersStr = "🔋 Зарядки: 0";
        private readonly List<ThermalTarget> thermalTargetPool = new List<ThermalTarget>();
        // ==========================================
        // СЕКЦИЯ 1: ПРОФИЛИ, БИНДЫ, HUD, АВТОПИЛОТ, МАКРО
        // ==========================================
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
                            for (int i = 0; i < cachedLandmarks.Count; i++)
                                if (cachedLandmarks[i] != null) autopilotLandmarks.Add(cachedLandmarks[i]);
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
                if (!Input.anyKeyDown && bindCaptureAction == null) return;
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

            Camera cam = GetCachedCamera();
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
            float bestSqr = float.MaxValue;
            int count = cachedSharksInfo.Count;
            for (int i = 0; i < count; i++)
            {
                SharkESPInfo info = cachedSharksInfo[i];
                if (info == null || info.transform == null) continue;
                float dSqr = (info.transform.position - pp).sqrMagnitude;
                if (dSqr < bestSqr) bestSqr = dSqr;
            }

            float rSqr = sharkAlarmRadius * sharkAlarmRadius;
            if (bestSqr <= rSqr)
            {
                float best = Mathf.Sqrt(bestSqr);
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
                        int sCount = player.Inventory.allSlots.Count;
                        for (int i = 0; i < sCount; i++)
                        {
                            Slot s = player.Inventory.allSlots[i];
                            if (s != null && s.HasValidItemInstance() && s.itemInstance != null) total += s.itemInstance.Amount;
                        }
                        if (lastInvTotal >= 0 && total > lastInvTotal) sessionItems += total - lastInvTotal;
                        lastInvTotal = total;
                    }
                    cachedSessionTimeStr = "Время: " + FormatSessionTime();
                    cachedSessionItemsStr = "Лут: " + sessionItems + " шт";
                    cachedSessionDistStr = "Путь: " + FormatDistance(sessionDistance);
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
            if (!hudActiveCheats)
            {
                if (activeCheatNames.Count > 0) activeCheatNames.Clear();
                return;
            }
            activeCheatNames.Clear();
            if (profileBoolFields == null) return;
            for (int i = 0; i < profileBoolFields.Length; i++)
            {
                FieldInfo f = profileBoolFields[i];
                if (hudHiddenFeatures.Contains(f.Name)) continue;
                try { if ((bool)f.GetValue(this)) activeCheatNames.Add(GetFeatureLabel(f.Name)); }
                catch {}
            }
            int c = activeCheatNames.Count;
            activeCheatsHeaderStr = "<b>АКТИВНО: " + c + "</b>";
            activeCheatsMoreStr = (c > 16) ? ("…и ещё " + (c - 16)) : "";
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

                private readonly List<MinimapBlip> minimapBlipPool = new List<MinimapBlip>();
        private int minimapBlipsCount = 0;

        private void AddMinimapBlip(Vector3 pos, Color col, float size)
        {
            MinimapBlip blip;
            if (minimapBlipsCount < minimapBlipPool.Count)
            {
                blip = minimapBlipPool[minimapBlipsCount];
            }
            else
            {
                blip = new MinimapBlip();
                minimapBlipPool.Add(blip);
            }
            minimapBlipsCount++;
            blip.Position = pos;
            blip.Col = col;
            blip.Size = size;
        }

        // ---------- ДАННЫЕ HUD ----------
        private void RefreshMinimapBlips()
        {
            if (!hudMinimap && !worldMapOn)
            {
                if (minimapBlips.Count > 0) minimapBlips.Clear();
                return;
            }
            try
            {
                minimapBlips.Clear();
                minimapBlipsCount = 0;
                if (cachedRaft != null)
                    AddMinimapBlip(cachedRaft.transform.position, new Color(0.25f, 0.85f, 1f), 7f);
                for (int i = 0; i < cachedSharksInfo.Count; i++)
                {
                    var info = cachedSharksInfo[i];
                    if (info == null || info.transform == null) continue;
                    AddMinimapBlip(info.transform.position, new Color(1f, 0.25f, 0.35f), 6f);
                }
                for (int i = 0; i < cachedPlayers.Count; i++)
                {
                    var p = cachedPlayers[i];
                    if (p == null) continue;
                    AddMinimapBlip(p.transform.position, new Color(0.2f, 1f, 0.4f), 5f);
                }
                for (int i = 0; i < cachedLandmarks.Count && i < 12; i++)
                {
                    var lm = cachedLandmarks[i];
                    if (lm == null) continue;
                    AddMinimapBlip(lm.transform.position, new Color(0.2f, 0.9f, 0.35f), 8f);
                }
                for (int i = 0; i < cachedTreasures.Count; i++)
                {
                    var tp = cachedTreasures[i];
                    if (tp == null) continue;
                    AddMinimapBlip(tp.transform.position, new Color(1f, 0.85f, 0.15f), 5f);
                }
                for (int i = 0; i < cachedPickups.Count && i < 20; i++)
                {
                    var p = cachedPickups[i];
                    if (p == null) continue;
                    AddMinimapBlip(p.transform.position, new Color(0.8f, 0.85f, 0.95f, 0.8f), 3f);
                }
                for (int i = 0; i < waypoints.Count; i++)
                {
                    AddMinimapBlip(waypoints[i].position, new Color(0.2f, 0.9f, 1f), 6f);
                }
                for (int i = 0; i < minimapBlipsCount; i++)
                {
                    minimapBlips.Add(minimapBlipPool[i]);
                }
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

        private static readonly string[] CardinalDirs = { "С", "СВ", "В", "ЮВ", "Ю", "ЮЗ", "З", "СЗ" };
        private static string CardinalName(float headingDeg)
        {
            int idx = ((int)Mathf.Repeat((headingDeg + 22.5f) / 45f, 8f)) % 8;
            return CardinalDirs[idx];
        }
        // ==========================================
        // СЕКЦИЯ 2: ТАХОМЕТР, ТРАССЕРЫ, УРОЖАЙ, ДАШБОРД, КРАФТ
        // ==========================================
        // ---------- V13 НАСТРОЙКИ ----------
        public bool perfHud = false;
        public bool espTracers = false;
        public bool sharkTrailOn = false;
        public bool espCrops = false;
        public bool hudMachines = false;
        public bool autoRefuelEngines = false;
        public bool antiStormRaft = false;
        public bool craftHelperOn = false;
        public bool fastNight = false;
        public float fastNightScale = 8f;
        public bool autoloadProfile = false;
        public string autoloadProfileName = "Мой профиль";
        public float menuOpacity = 1f;
        public bool menuCollapsed = false;
        public bool lootFilterFood = false;
        public bool lootFilterMetal = false;
        public bool lootFilterRare = false;
        public string lootBlacklist = "";

        // ---------- V13 СОСТОЯНИЕ ----------
        // Производительность
        private float perfFrames = 0f;
        private float perfWindowStart = 0f;
        private float fpsCurrent = 60f;
        private readonly float[] fpsSamples = new float[60];
        private int fpsSampleIdx = 0;
        private int fpsSampleCount = 0;
        private int entityCount = 0;
        private int blockCount = 0;
        private int lastBlockCount = -1;
        private float nextPerfScanTime = 0f;
        private float nextV13FastTime = 0f;
        private float nextV13SlowTime = 0f;

        // Машины плота
        private int enginesCount = 0;
        private int enginesFuel = 0;
        private int enginesFuelMax = 0;
        private int enginesBurning = 0;
        private int smeltersCount = 0;
        private int smeltersFuel = 0;
        private int smeltersFuelMax = 0;
        private int cookersCount = 0;
        private int cookersFuel = 0;
        private int cookersFuelMax = 0;
        private int chargersTotal = 0;
        private int chargersWithBattery = 0;
        private float nextMachinesScanTime = 0f;

        // Урожай ESP
        private readonly List<EspTag> cropTags = new List<EspTag>();
        private float nextCropScanTime = 0f;

        // След акул
        private readonly List<TrailPoint> sharkTrail = new List<TrailPoint>();

        // Шторм-автопилот
        private float nextAntiStormTime = 0f;
        private float nextAntiStormMsgTime = 0f;

        // Авто-дозаправка
        private float nextRefuelTime = 0f;

        // Статистика v13
        private readonly HashSet<int> deadSharkIds = new HashSet<int>();
        private readonly HashSet<int> visitedIslandIds = new HashSet<int>();
        private readonly List<Landmark> statsLandmarks = new List<Landmark>();
        private bool deathCounted = false;
        public int statSharkKills = 0;
        public int statDeaths = 0;
        public int statIslands = 0;
        public int statBlocksLost = 0;

        // Быстрая ночь
        private bool fastNightApplied = false;
        private UnityEngine.AzureSky.AzureSkyController cachedSky = null;
        private float nextSkyCacheTime = 0f;

        // Автозагрузка профиля
        private bool v13AutoLoaded = false;

        // Помощник крафта
        private readonly List<string> craftableList = new List<string>();
        private readonly List<string> missingList = new List<string>();
        private float nextCraftScanTime = 0f;

        // Слоты позиции x3
        private readonly Vector3[] posSlotPos = new Vector3[3];
        private readonly float[] posSlotYaw = new float[3];
        private readonly bool[] posSlotHas = new bool[3];

        // Черный список (кэш)
        private readonly List<string> lootBlacklistCache = new List<string>();
        private string lootBlacklistParsed = "§";

        // ==========================================
        // ГЛАВНЫЙ ЦИКЛ V13
        // ==========================================
        private void UpdateV13Features(Network_Player player)
        {
            try
            {
                if (!v13AutoLoaded && Time.time > 3f)
                {
                    v13AutoLoaded = true;
                    TryAutoLoadProfile();
                }

                UpdateFastNight();

                if (Time.time > nextV13FastTime)
                {
                    nextV13FastTime = Time.time + 0.25f;
                    UpdatePerfMeter();
                    if (sharkTrailOn) RecordSharkTrail();
                }

                if (Time.time > nextV13SlowTime)
                {
                    nextV13SlowTime = Time.time + 1f;
                    if (antiStormRaft) UpdateAntiStorm();
                    if (autoRefuelEngines) UpdateAutoRefuel();
                    UpdateV13Stats();
                    if (craftHelperOn) ScanCraftHelper();
                }
            }
            catch {}
        }

        // ---------- ТЕСТ ПРОИЗВОДИТЕЛЬНОСТИ ----------
        private void UpdatePerfMeter()
        {
            if (!perfHud) return;

            if (perfWindowStart <= 0f) perfWindowStart = Time.time;
            perfFrames += 1f;

            if (Time.time - perfWindowStart >= 0.5f)
            {
                fpsCurrent = perfFrames / (Time.time - perfWindowStart);
                perfFrames = 0f;
                perfWindowStart = Time.time;
                fpsSamples[fpsSampleIdx] = fpsCurrent;
                fpsSampleIdx = (fpsSampleIdx + 1) % fpsSamples.Length;
                if (fpsSampleCount < fpsSamples.Length) fpsSampleCount++;

                perfFpsStr = "<b>" + Mathf.RoundToInt(fpsCurrent) + " FPS</b>";
                perfMsEntityStr = Mathf.Round(1000f / Mathf.Max(1f, fpsCurrent)) + " мс • " + entityCount + " сущ.";
            }

            if (Time.time > nextPerfScanTime)
            {
                nextPerfScanTime = Time.time + 3f;
                entityCount = cachedEntities.Count + cachedPickups.Count + cachedPlayers.Count + cachedSharksInfo.Count;
                try
                {
                    int now = UnityEngine.Object.FindObjectsOfType<Block>().Length;
                    if (lastBlockCount >= 0 && now < lastBlockCount)
                        statBlocksLost += lastBlockCount - now;
                    lastBlockCount = now;
                    blockCount = now;
                }
                catch { blockCount = 0; }
                perfBlockCountStr = "Блоков плота: " + blockCount;
                perfMsEntityStr = Mathf.Round(1000f / Mathf.Max(1f, fpsCurrent)) + " мс • " + entityCount + " сущ.";
            }
        }

        // ---------- ТРАССЕРЫ ----------
        private void DrawTracerLines(Camera cam)
        {
            Vector3 from = new Vector3(Screen.width * 0.5f, Screen.height, 0f);
            int drawn = 0;

            for (int i = 0; i < cachedSharksInfo.Count && drawn < 26; i++)
            {
                var info = cachedSharksInfo[i];
                if (info == null || info.transform == null) continue;
                if (DrawTracer(cam, from, info.transform.position, new Color(1f, 0.25f, 0.35f, 0.75f))) drawn++;
            }
            for (int i = 0; i < cachedTreasures.Count && drawn < 34; i++)
            {
                var tp = cachedTreasures[i];
                if (tp == null) continue;
                if (DrawTracer(cam, from, tp.transform.position, new Color(1f, 0.85f, 0.15f, 0.7f))) drawn++;
            }
            for (int i = 0; i < cachedPlayers.Count && drawn < 40; i++)
            {
                var p = cachedPlayers[i];
                if (p == null) continue;
                if (DrawTracer(cam, from, p.transform.position, new Color(0.2f, 1f, 0.4f, 0.7f))) drawn++;
            }
            if (cachedRaft != null && drawn < 44)
            {
                DrawTracer(cam, from, cachedRaft.transform.position, new Color(0.25f, 0.85f, 1f, 0.7f));
            }
        }

        private bool DrawTracer(Camera cam, Vector3 fromScreen, Vector3 worldPos, Color col)
        {
            try
            {
                if (Vector3.Dot(cam.transform.forward, (worldPos - cam.transform.position)) <= 0.1f) return false;
                Vector3 sp = cam.WorldToScreenPoint(worldPos + Vector3.up * 0.5f);
                if (sp.z <= 0.5f) return false;
                Vector3 to = new Vector3(sp.x, Screen.height - sp.y, 0f);

                float dx = to.x - fromScreen.x;
                float dy = to.y - fromScreen.y;
                float len = Mathf.Sqrt(dx * dx + dy * dy);
                if (len < 24f) return false;

                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                Matrix4x4 old = GUI.matrix;
                GUIUtility.RotateAroundPivot(angle, new Vector2(fromScreen.x, fromScreen.y));
                GUI.color = col;
                GUI.DrawTexture(new Rect(fromScreen.x, fromScreen.y - 1f, len, 2f), texWhiteDot);
                GUI.matrix = old;
                GUI.color = Color.white;
                return true;
            }
            catch { return false; }
        }

        // ---------- СЛЕД АКУЛ ----------
        private float nextSharkTrailRecordTime = 0f;

        private void RecordSharkTrail()
        {
            if (Time.time < nextSharkTrailRecordTime) return;
            nextSharkTrailRecordTime = Time.time + 0.25f;

            for (int i = 0; i < cachedSharksInfo.Count && i < 3; i++)
            {
                var info = cachedSharksInfo[i];
                if (info == null || info.transform == null) continue;
                TrailPoint tp = new TrailPoint();
                tp.Pos = info.transform.position;
                tp.T = Time.time;
                sharkTrail.Add(tp);
            }
            float cutoff = Time.time - 30f;
            while (sharkTrail.Count > 0 && sharkTrail[0].T < cutoff) sharkTrail.RemoveAt(0);
            while (sharkTrail.Count > 200) sharkTrail.RemoveAt(0);
        }

        private void DrawSharkTrail(Camera cam)
        {
            if (sharkTrail.Count == 0) return;
            Color prev = GUI.color;
            for (int i = 0; i < sharkTrail.Count; i++)
            {
                TrailPoint tp = sharkTrail[i];
                float age = Time.time - tp.T;
                float alpha = 1f - Mathf.Clamp01(age / 30f);
                if (alpha <= 0.02f) continue;
                Vector3 sp = cam.WorldToScreenPoint(tp.Pos);
                if (sp.z <= 0.3f) continue;
                GUI.color = new Color(1f, 0.3f, 0.4f, alpha * 0.65f);
                GUI.DrawTexture(new Rect(sp.x - 1.5f, Screen.height - sp.y - 1.5f, 3f, 3f), texWhiteDot);
            }
            GUI.color = prev;
        }

        // ---------- ESP УРОЖАЯ ----------
        private void UpdateCropEsp()
        {
            if (!espCrops && !smartReminders) { if (cropTags.Count > 0) cropTags.Clear(); return; }
            if (Time.time < nextCropScanTime) return;
            nextCropScanTime = Time.time + 1.5f;

            cropTags.Clear();
            try
            {
                Vector3 pp = Vector3.zero;
                Network_Player player = ComponentManager<Network_Player>.Value;
                if (player != null) pp = player.transform.position;

                var plants = UnityEngine.Object.FindObjectsOfType<Plant>();
                for (int i = 0; i < plants.Length && cropTags.Count < 40; i++)
                {
                    Plant pl = plants[i];
                    if (pl == null || pl.transform == null) continue;
                    bool grown = false;
                    try { grown = pl.FullyGrown(); } catch {}
                    string label;
                    Color col;
                    if (grown) { label = "🌱 УРОЖАЙ ГОТОВ"; col = new Color(0.3f, 1f, 0.5f); }
                    else
                    {
                        float cur = 0f, total = 0f;
                        try { cur = pl.GetGrowTimer(); } catch {}
                        try { total = pl.growTime; } catch {}
                        float pct = total > 0.5f ? Mathf.Clamp01(cur / total) : 0f;
                        label = "🌱 Урожай " + Mathf.RoundToInt(pct * 100f) + "%";
                        col = Color.Lerp(new Color(1f, 0.6f, 0.2f), new Color(0.3f, 1f, 0.5f), pct);
                    }
                    EspTag tag = new EspTag();
                    tag.Position = pl.transform.position;
                    tag.Label = label;
                    tag.Col = col;
                    tag.MaxDist = 60f;
                    tag.BuildDist = (tag.Position - pp).sqrMagnitude;
                    cropTags.Add(tag);
                }
            }
            catch {}
        }

        private void AppendV13EspTags(Vector3 pp)
        {
            UpdateCropEsp();
            for (int i = 0; i < cropTags.Count; i++) espTags.Add(cropTags[i]);
        }

        // ---------- ПАНЕЛЬ МАШИН ПЛОТА ----------
        private void UpdateMachineScan()
        {
            if (Time.time < nextMachinesScanTime) return;
            nextMachinesScanTime = Time.time + 2f;

            enginesCount = 0; enginesFuel = 0; enginesFuelMax = 0; enginesBurning = 0;
            smeltersCount = 0; smeltersFuel = 0; smeltersFuelMax = 0;
            cookersCount = 0; cookersFuel = 0; cookersFuelMax = 0;
            chargersTotal = 0; chargersWithBattery = 0;

            try
            {
                var fuels = UnityEngine.Object.FindObjectsOfType<Fuel>();
                for (int i = 0; i < fuels.Length; i++)
                {
                    Fuel f = fuels[i];
                    if (f == null) continue;
                    string n = f.transform.root != null ? f.transform.root.name.ToLower() : "";
                    if (n.Length == 0) n = f.name.ToLower();
                    int fuel = 0, max = 0;
                    bool burning = false;
                    try { fuel = f.GetFuelCount(); } catch {}
                    try { max = f.MaxFuel; } catch {}
                    try { burning = f.IsBurning(); } catch {}

                    if (n.Contains("engine"))
                    {
                        enginesCount++; enginesFuel += fuel; enginesFuelMax += max;
                        if (burning) enginesBurning++;
                    }
                    else if (n.Contains("smelt"))
                    {
                        smeltersCount++; smeltersFuel += fuel; smeltersFuelMax += max;
                    }
                    else
                    {
                        cookersCount++; cookersFuel += fuel; cookersFuelMax += max;
                    }
                }

                var chargers = UnityEngine.Object.FindObjectsOfType<BatteryCharger>();
                for (int i = 0; i < chargers.Length; i++)
                {
                    BatteryCharger c = chargers[i];
                    if (c == null) continue;
                    chargersTotal++;
                    if (c.batteryL != null) chargersWithBattery++;
                    if (c.batteryR != null) chargersWithBattery++;
                }

                machinesEnginesStr = "⚓ Двигатели: " + enginesCount + " • топл. " + enginesFuel + "/" + enginesFuelMax + (enginesBurning > 0 ? " 🔥" : "");
                machinesSmeltersStr = "🏭 Плавильни: " + smeltersCount + " • топл. " + smeltersFuel + "/" + smeltersFuelMax;
                machinesCookersStr = "🍳 Грили/печи: " + cookersCount + " • топл. " + cookersFuel + "/" + cookersFuelMax;
                machinesChargersStr = "🔋 Зарядки: " + chargersTotal + " • батареек " + chargersWithBattery;
            }
            catch {}
        }

        // ---------- АНТИ-ШТОРМ АВТОПИЛОТ ----------
        private void UpdateAntiStorm()
        {
            if (Time.time < nextAntiStormTime) return;
            nextAntiStormTime = Time.time + 1f;

            try
            {
                bool storm = false;
                try
                {
                    if (cachedWeather != null)
                        storm = cachedWeather.GetCurrentWeatherType().ToString().ToLower().Contains("storm");
                }
                catch {}

                Raft raft = ComponentManager<Raft>.Value;
                if (!storm || raft == null) return;

                // Поднять якорь, если брошен
                try
                {
                    var anchors = UnityEngine.Object.FindObjectsOfType<Anchor_Stationary>();
                    for (int i = 0; i < anchors.Length; i++)
                    {
                        Anchor_Stationary a = anchors[i];
                        if (a == null) continue;
                        try
                        {
                            if (a.AtBottom)
                            {
                                MethodInfo mi = typeof(Anchor_Stationary).GetMethod("WeighAnchor",
                                    BindingFlags.NonPublic | BindingFlags.Instance);
                                if (mi != null) mi.Invoke(a, null);
                            }
                        }
                        catch {}
                    }
                    if (raft.IsAnchored) raft.RemoveAnchor(raft.AnchorCount);
                }
                catch {}

                // Отплыть от ближайшего острова/рифа
                Vector3 away = Vector3.zero;
                float best = float.MaxValue;
                Landmark nearest = null;
                var sources = cachedLandmarks.Count > 0 ? cachedLandmarks : statsLandmarks;
                for (int i = 0; i < sources.Count; i++)
                {
                    Landmark lm = sources[i];
                    if (lm == null) continue;
                    float d = Vector3.Distance(lm.transform.position, raft.transform.position);
                    if (d < best) { best = d; nearest = lm; }
                }
                if (nearest != null && best < 120f)
                {
                    away = raft.transform.position - nearest.transform.position;
                    away.y = 0f;
                    if (away.sqrMagnitude > 0.01f)
                    {
                        away.Normalize();
                        raft.AddForce(away, 5f, ForceMode.Acceleration, false);
                    }
                }

                if (Time.time > nextAntiStormMsgTime)
                {
                    nextAntiStormMsgTime = Time.time + 8f;
                    statusMessage = best < 120f
                        ? "🌩 Шторм: якорь поднят, отплываем от рифов (" + best.ToString("F0") + " м)"
                        : "🌩 Шторм: якорь поднят, плот в открытом море";
                    LogEvent("Анти-шторм активен");
                }
            }
            catch {}
        }

        // ---------- АВТО-ДОЗАПРАВКА ИЗ СУНДУКОВ ----------
        private void UpdateAutoRefuel()
        {
            if (Time.time < nextRefuelTime) return;
            nextRefuelTime = Time.time + 3f;

            try
            {
                var fuels = UnityEngine.Object.FindObjectsOfType<Fuel>();
                var chests = UnityEngine.Object.FindObjectsOfType<Storage_Small>();
                int refueled = 0;

                for (int i = 0; i < fuels.Length && refueled < 15; i++)
                {
                    Fuel f = fuels[i];
                    if (f == null) continue;
                    int cur = 0, max = 0;
                    try { cur = f.GetFuelCount(); } catch {}
                    try { max = f.MaxFuel; } catch {}
                    if (max <= 0 || cur >= max) continue;

                    for (int c = 0; c < chests.Length && cur < max && refueled < 15; c++)
                    {
                        Storage_Small st = chests[c];
                        if (st == null) continue;
                        Inventory inv = null;
                        try { inv = st.GetInventoryReference(); } catch {}
                        if (inv == null) continue;
                        int have = 0;
                        try { have = inv.GetItemCount("Plank"); } catch {}
                        if (have <= 0) continue;

                        try
                        {
                            inv.RemoveItem("Plank", 1);
                            f.AddFuel(1);
                            refueled++;
                            cur++;
                        }
                        catch {}
                    }
                }

                if (refueled > 0)
                {
                    statusMessage = "⛽ Дозаправлено машин: +" + refueled + " топлива";
                    LogEvent("Авто-дозаправка: +" + refueled);
                }
            }
            catch {}
        }

        // ---------- РАСШИРЕННАЯ СТАТИСТИКА ----------
        private void UpdateV13Stats()
        {
            if (!hudSessionStats) return;
            Network_Player player = ComponentManager<Network_Player>.Value;
            try
            {
                // Смерти
                if (player != null && player.Stats != null && player.Stats.stat_health != null)
                {
                    if (player.Stats.stat_health.IsDead())
                    {
                        if (!deathCounted) { deathCounted = true; statDeaths++; LogEvent("☠ Игрок погиб"); }
                    }
                    else deathCounted = false;
                }

                // Убитые акулы (используем cachedSharks без поиска по сцене)
                int sCount = cachedSharks.Count;
                for (int i = 0; i < sCount; i++)
                {
                    Shark s = cachedSharks[i];
                    if (s == null) continue;
                    int id = s.GetInstanceID();
                    if (deadSharkIds.Contains(id)) continue;
                    bool dead = false;
                    try { dead = s.entityComponent != null && s.entityComponent.stat_health != null && s.entityComponent.stat_health.IsDead(); }
                    catch {}
                    if (!dead)
                    {
                        try { dead = s.state == SharkState.Dead || s.state == SharkState.DeadSink; } catch {}
                    }
                    if (dead) { deadSharkIds.Add(id); statSharkKills++; LogEvent("🦈 Акула убита"); }
                }

                // Посещенные острова (переиспользуем cachedLandmarks)
                if (player != null)
                {
                    Vector3 pp = player.transform.position;
                    int lmCount = cachedLandmarks.Count;
                    for (int i = 0; i < lmCount; i++)
                    {
                        Landmark lm = cachedLandmarks[i];
                        if (lm == null) continue;
                        int id = lm.GetInstanceID();
                        if (visitedIslandIds.Contains(id)) continue;
                        if ((lm.transform.position - pp).sqrMagnitude < 22500f)
                        {
                            visitedIslandIds.Add(id);
                            statIslands++;
                            LogEvent("🏝 Новый остров посещен");
                        }
                    }
                }
            }
            catch {}
        }

        // ---------- БЫСТРАЯ НОЧЬ ----------
        private void UpdateFastNight()
        {
            if (!fastNight && !fastNightApplied) return;
            if (fastNight && Time.time > nextSkyCacheTime)
            {
                nextSkyCacheTime = Time.time + 5f;
                try { cachedSky = UnityEngine.Object.FindObjectOfType<UnityEngine.AzureSky.AzureSkyController>(); }
                catch { cachedSky = null; }
            }

            bool night = false;
            try { if (cachedSky != null) night = !cachedSky.IsDaytime; } catch {}

            if (fastNight && night && !fastNightApplied)
            {
                Time.timeScale = Mathf.Clamp(fastNightScale, 2f, 15f);
                fastNightApplied = true;
                statusMessage = "🌙 Ночь ускорена x" + fastNightScale.ToString("F0");
            }
            else if (fastNightApplied && (!fastNight || !night))
            {
                Time.timeScale = 1f;
                fastNightApplied = false;
                if (fastNight) statusMessage = "☀ Утро настало";
            }
        }

        // ---------- АВТОЗАГРУЗКА ПРОФИЛЯ ----------
        private void TryAutoLoadProfile()
        {
            if (!autoloadProfile || string.IsNullOrEmpty(autoloadProfileName)) return;
            try
            {
                if (File.Exists(GetProfilePath(autoloadProfileName)))
                {
                    LoadProfileFile(autoloadProfileName);
                    LogEvent("Автозагрузка профиля: " + autoloadProfileName);
                }
            }
            catch {}
        }

        // ---------- ПОМОЩНИК КРАФТА ----------
        private void ScanCraftHelper()
        {
            if (Time.time < nextCraftScanTime) return;
            nextCraftScanTime = Time.time + 3f;

            craftableList.Clear();
            missingList.Clear();

            try
            {
                Network_Player player = ComponentManager<Network_Player>.Value;
                if (player == null || player.Inventory == null) return;
                Inventory inv = player.Inventory;
                var items = ItemManager.GetAllItems();
                if (items == null) return;

                for (int i = 0; i < items.Count; i++)
                {
                    Item_Base it = items[i];
                    if (it == null) continue;
                    ItemInstance_Recipe rec = it.settings_recipe;
                    if (rec == null) continue;
                    bool learned = false;
                    try { learned = rec.Learned; } catch {}
                    if (!learned) continue;

                    CostMultiple[] costs = null;
                    try { costs = rec.NewCost; } catch {}
                    if (costs == null || costs.Length == 0) continue;

                    bool canCraft = true;
                    string missText = "";
                    for (int c = 0; c < costs.Length; c++)
                    {
                        CostMultiple cost = costs[c];
                        if (cost == null || cost.items == null || cost.items.Length == 0) continue;
                        Item_Base need = cost.items[0];
                        if (need == null) continue;
                        bool enough = false;
                        try { enough = cost.HasEnoughInInventory(inv); } catch {}
                        if (!enough)
                        {
                            canCraft = false;
                            int have = 0;
                            try { have = inv.GetItemCount(need); } catch {}
                            string needName = need.UniqueName;
                            try { if (need.settings_Inventory != null && !string.IsNullOrEmpty(need.settings_Inventory.DisplayName)) needName = need.settings_Inventory.DisplayName; } catch {}
                            missText += (missText.Length > 0 ? ", " : "") + needName + " x" + (cost.amount - have);
                        }
                    }

                    string title = it.UniqueName;
                    try { if (it.settings_Inventory != null && !string.IsNullOrEmpty(it.settings_Inventory.DisplayName)) title = it.settings_Inventory.DisplayName; } catch {}

                    if (canCraft)
                    {
                        if (craftableList.Count < 20) craftableList.Add(title);
                    }
                    else if (missingList.Count < 14) missingList.Add(title + "  →  нет: " + missText);
                }
            }
            catch {}
        }

        // ---------- КАЛЬКУЛЯТОР СТРОЙКИ ----------
        private string CalcExpandCost(int ring)
        {
            try
            {
                RaftBounds bounds = UnityEngine.Object.FindObjectOfType<RaftBounds>();
                if (bounds == null) return "";
                float bSize = BlockCreator.BlockSize > 0.5f ? BlockCreator.BlockSize : 1.5f;
                int cellsX = Mathf.Max(1, Mathf.RoundToInt(bounds.HalfWidth / bSize));
                int cellsZ = Mathf.Max(1, Mathf.RoundToInt(bounds.HalfLength / bSize));

                int cells = 0;
                int spanX = cellsX + ring;
                int spanZ = cellsZ + ring;
                for (int ix = -spanX; ix <= spanX; ix++)
                    for (int iz = -spanZ; iz <= spanZ; iz++)
                        if (Mathf.Abs(ix) > cellsX || Mathf.Abs(iz) > cellsZ) cells++;

                Item_Base foundItem = ItemManager.GetItemByName("Foundation");
                if (foundItem == null) return cells + " фунд. (рецепт не найден)";

                ItemInstance_Recipe rec = foundItem.settings_recipe;
                if (rec == null || rec.NewCost == null || rec.NewCost.Length == 0)
                    return cells + " фунд. (рецепт скрыт)";

                string parts = "";
                for (int c = 0; c < rec.NewCost.Length; c++)
                {
                    CostMultiple cost = rec.NewCost[c];
                    if (cost == null || cost.items == null || cost.items.Length == 0) continue;
                    string nm = cost.items[0].UniqueName;
                    try { if (cost.items[0].settings_Inventory != null && !string.IsNullOrEmpty(cost.items[0].settings_Inventory.DisplayName)) nm = cost.items[0].settings_Inventory.DisplayName; } catch {}
                    parts += (parts.Length > 0 ? ", " : "") + nm + " x" + (cost.amount * cells);
                }
                return cells + " фунд.: " + parts;
            }
            catch { return ""; }
        }

        // ---------- ФИЛЬТРЫ ЛУТА ----------
        private void ParseBlacklist()
        {
            if (lootBlacklist == lootBlacklistParsed) return;
            lootBlacklistParsed = lootBlacklist;
            lootBlacklistCache.Clear();
            if (string.IsNullOrEmpty(lootBlacklist)) return;
            string[] parts = lootBlacklist.ToLower().Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string k = parts[i].Trim();
                if (k.Length > 0) lootBlacklistCache.Add(k);
            }
        }

        private bool IsPickupAllowed(PickupItem p)
        {
            ParseBlacklist();
            if (lootBlacklistCache.Count > 0)
            {
                string low = p.name.ToLower();
                for (int i = 0; i < lootBlacklistCache.Count; i++)
                    if (low.Contains(lootBlacklistCache[i])) return false;
            }

            if (!lootFilterFood && !lootFilterMetal && !lootFilterRare) return true;

            string rus = GetRussianItemName(p).ToLower();
            string raw = p.name.ToLower();

            if (lootFilterFood &&
                (raw.Contains("potato") || raw.Contains("beet") || raw.Contains("meat") || raw.Contains("fish")
                 || raw.Contains("mango") || raw.Contains("coconut") || raw.Contains("watermelon")
                 || raw.Contains("pineapple") || raw.Contains("mushroom") || raw.Contains("egg")
                 || rus.Contains("картофел") || rus.Contains("свекл") || rus.Contains("мясо") || rus.Contains("рыб")
                 || rus.Contains("манго") || rus.Contains("кокос") || rus.Contains("арбуз") || rus.Contains("ананас")
                 || rus.Contains("гриб") || rus.Contains("яйц"))) return true;

            if (lootFilterMetal &&
                (raw.Contains("ore") || raw.Contains("ingot") || raw.Contains("scrap") || raw.Contains("metal")
                 || raw.Contains("nail") || raw.Contains("bolt") || raw.Contains("hinge") || raw.Contains("plank")
                 || raw.Contains("plastic") || raw.Contains("vine") || raw.Contains("stone") || raw.Contains("clay")
                 || raw.Contains("sand") || raw.Contains("dirt") || rus.Contains("руда") || rus.Contains("слиток")
                 || rus.Contains("хлам") || rus.Contains("металл") || rus.Contains("доск") || rus.Contains("пластик")
                 || rus.Contains("гвоздь") || rus.Contains("болт") || rus.Contains("петл") || rus.Contains("камень")
                 || rus.Contains("глин") || rus.Contains("песок") || rus.Contains("земл"))) return true;

            if (lootFilterRare &&
                (raw.Contains("titanium") || raw.Contains("explosive") || raw.Contains("vinegoo")
                 || raw.Contains("battery") || raw.Contains("circuit") || rus.Contains("титан")
                 || rus.Contains("взрывчатк") || rus.Contains("клей") || rus.Contains("батаре"))) return true;

            return false;
        }

        // ---------- СЛОТЫ ПОЗИЦИИ ----------
        private void SavePosSlot(int idx)
        {
            Network_Player player = ComponentManager<Network_Player>.Value;
            if (player == null) return;
            try
            {
                posSlotPos[idx] = player.transform.position;
                Camera cam = GetCachedCamera();
                posSlotYaw[idx] = cam != null ? cam.transform.eulerAngles.y : 0f;
                posSlotHas[idx] = true;
                statusMessage = "Слот " + (idx + 1) + ": позиция запомнена";
                LogEvent("Слот позиции " + (idx + 1) + " сохранен");
            }
            catch (Exception ex) { statusMessage = "Ошибка слота: " + ex.Message; }
        }

        private void GoPosSlot(int idx)
        {
            Network_Player player = ComponentManager<Network_Player>.Value;
            if (player == null) return;
            if (!posSlotHas[idx]) { statusMessage = "Слот " + (idx + 1) + " пуст — сначала запомните"; return; }
            try
            {
                player.transform.position = posSlotPos[idx] + Vector3.up * 1.5f;
                if (player.PersonController != null) player.PersonController.ResetFallDuration();
                statusMessage = "Возврат в слот " + (idx + 1);
                LogEvent("Возврат в слот позиции " + (idx + 1));
            }
            catch (Exception ex) { statusMessage = "Ошибка слота: " + ex.Message; }
        }

        // ---------- ТП К БЛИЖАЙШЕМУ КЛАДУ ----------
        private void TeleportToNearestTreasure()
        {
            try
            {
                Network_Player player = ComponentManager<Network_Player>.Value;
                if (player == null) return;
                Vector3 pp = player.transform.position;

                TreasurePoint best = null;
                float bd = float.MaxValue;
                for (int i = 0; i < cachedTreasures.Count; i++)
                {
                    TreasurePoint tp = cachedTreasures[i];
                    if (tp == null) continue;
                    float d = Vector3.Distance(tp.transform.position, pp);
                    if (d < bd) { bd = d; best = tp; }
                }
                if (best == null)
                {
                    var all = UnityEngine.Object.FindObjectsOfType<TreasurePoint>();
                    for (int i = 0; i < all.Length; i++)
                    {
                        TreasurePoint tp = all[i];
                        if (tp == null) continue;
                        float d = Vector3.Distance(tp.transform.position, pp);
                        if (d < bd) { bd = d; best = tp; }
                    }
                }
                if (best == null) { statusMessage = "Кладов поблизости не найдено"; return; }

                player.transform.position = best.transform.position + Vector3.up * 2f;
                if (player.PersonController != null) player.PersonController.ResetFallDuration();
                statusMessage = "Телепорт к кладу (" + bd.ToString("F0") + " м был)";
                LogEvent("ТП к кладу");
            }
            catch (Exception ex) { statusMessage = "Ошибка ТП к кладу: " + ex.Message; }
        }

        // ---------- СОРТИРОВКА ИНВЕНТАРЯ ----------
        private void SortInventoryNow()
        {
            try
            {
                Network_Player player = ComponentManager<Network_Player>.Value;
                if (player == null || player.Inventory == null) return;
                player.Inventory.Sort();
                statusMessage = "🎒 Инвентарь отсортирован";
                LogEvent("Инвентарь отсортирован");
            }
            catch (Exception ex) { statusMessage = "Ошибка сортировки: " + ex.Message; }
        }
        // ==========================================
        // СЕКЦИЯ 3: КАРТА МИРА, НАПОМИНАНИЯ, РЕСУРСЫ, СЕТИ, ЭТАЖИ
        // ==========================================
        // ---------- V14 НАСТРОЙКИ ----------
        public bool worldMapOn = false;
        public float worldMapZoom = 0.35f;
        public bool smartReminders = false;
        public bool autoCollectNests = false;
        public bool autoPlantSeeds = false;
        public string plantKeywords = "seed";
        public bool autoRadarScan = false;
        public bool magnetOnlyFood = false;
        public bool magnetOnlyMetal = false;
        public bool magnetOnlyRare = false;
        public bool rescueHook = false;
        public float rescueDelay = 3f;
        public bool thermalVision = false;
        public bool ratExterminator = false;
        public bool petsAlwaysFed = false;
        public bool autoFishPro = false;
        public string baitKeyword = "bait";
        public float biofuelRawPerUnit = 0.1f;
        public bool hudPlayersTable = false;
        public bool hudTeammateStatus = false;
        public float floorPasteOffset = 1.5f;

        // ---------- V14 СОСТОЯНИЕ ----------
        // Этажный копир
        private readonly List<FloorBlockCopy> floorBlocks = new List<FloorBlockCopy>();
        private bool floorHasCopy = false;

        // Тепловизор
        private readonly List<ThermalTarget> thermalTargets = new List<ThermalTarget>();
        private float nextThermalScanTime = 0f;

        // Таблица игроков
        private readonly List<Network_Player> v14Players = new List<Network_Player>();
        private float nextPlayersScanTime = 0f;

        // Статусы тиммейтов
        private readonly List<TeammateInfo> teammates = new List<TeammateInfo>();

        // Сумка друга (UI-состояние, в профили не пишется)
        private int bagViewerIndex = -1;
        private readonly List<Network_Player> bagTargets = new List<Network_Player>();
        private readonly List<string> bagLines = new List<string>();
        private string bagHeader = "—";
        private float nextBagTime = 0f;
        private Vector2 bagScroll = Vector2.zero;

        // Ресурсы сессии
        private readonly Dictionary<string, int> resourceGains = new Dictionary<string, int>();
        private readonly Dictionary<string, int> lastResCounts = new Dictionary<string, int>();
        private bool resTrackingInit = false;
        private float nextResTrackTime = 0f;

        // Напоминания
        private float cropReminderTime = 0f;
        private float fuelReminderTime = 0f;
        private float batteryReminderTime = 0f;
        private float weatherReminderTime = 0f;

        // Спасательный крюк
        private float rescueTimer = 0f;

        // Крысолов
        public int statRats = 0;
        private readonly HashSet<int> deadRatIds = new HashSet<int>();

        // Питомцы
        private static FieldInfo fHungerTimer = typeof(AI_NetworkBehaviour_Domestic).GetField("_hungerTimer",
            BindingFlags.NonPublic | BindingFlags.Instance);
        private float nextPetsTime = 0f;

        // Посадка
        private float nextPlantTime = 0f;
        private string[] plantKeywordsCache = new string[0];
        private string plantKeywordsParsed = "§";

        // Радар
        private int radarScanIndex = 0;
        private float nextRadarScanTime = 0f;

        // Рыбалка 2.0
        private float nextFishProTime = 0f;

        // Биотопливо
        private Tank_BiofuelExtractor cachedBioTank = null;
        private float nextBioTankTime = 0f;

        // Магнит (кэш категорий)
        private float nextV14FastTime = 0f;
        private float nextV14SlowTime = 0f;

        // ==========================================
        // ГЛАВНЫЙ ЦИКЛ V14
        // ==========================================
        private void UpdateV14Features(Network_Player player)
        {
            UpdateV12Features(player);
            UpdateV13Features(player);
            try
            {
                if (Time.time > nextV14FastTime)
                {
                    nextV14FastTime = Time.time + 0.3f;
                    if (rescueHook) UpdateRescueHook(player);
                    if (smartReminders) UpdateReminders(player);
                }

                if (Time.time > nextV14SlowTime)
                {
                    nextV14SlowTime = Time.time + 1f;
                    if (ratExterminator) KillRats();
                    if (petsAlwaysFed) UpdatePetsFed();
                    if (autoCollectNests) UpdateAutoNests(player);
                    if (autoPlantSeeds) UpdateAutoPlant(player);
                    if (autoFishPro) UpdateAutoFishPro(player);
                    if (thermalVision && Time.time > nextThermalScanTime) { nextThermalScanTime = Time.time + 1f; ScanThermalTargets(player); }
                    if (hudPlayersTable && Time.time > nextPlayersScanTime) { nextPlayersScanTime = Time.time + 2f; ScanPlayersTable(player); }
                    if (Time.time > nextResTrackTime) { nextResTrackTime = Time.time + 2f; TrackResources(player); }
                    if (autoRadarScan) UpdateAutoRadar();
                }
            }
            catch {}
        }

        // ---------- ГЛОБАЛЬНАЯ КАРТА ----------
        private void DrawWorldMapHud(Camera cam)
        {
            if (!worldMapOn) return;
            try
            {
                float size = 400f;
                float half = size / 2f;
                Vector2 center = new Vector2(Screen.width - size - 22f, Screen.height - 10f - half);
                Rect panel = new Rect(center.x - half, center.y - half, size, size);
                GUI.Box(panel, GUIContent.none, hudPanelStyle);
                GUI.Label(new Rect(panel.x + 6f, panel.y + 2f, 160f, 16f), "<b>🗺 КАРТА МИРА</b>", hudTitleStyle);
                GUI.Label(new Rect(panel.x + size - 130f, panel.y + 2f, 124f, 16f),
                    "1 px = " + (1f / Mathf.Max(0.05f, worldMapZoom)).ToString("F1") + " м", hudTextStyle);

                Vector3 pp = Vector3.zero;
                Network_Player player = ComponentManager<Network_Player>.Value;
                if (player != null) pp = player.transform.position;

                float z = Mathf.Clamp(worldMapZoom, 0.05f, 1.5f);

                // Сетка (каждые 200 м)
                Color prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.07f);
                float step = 200f * z;
                float startX = center.x - Mathf.Ceil(half / step) * step;
                for (float gx = startX; gx <= panel.xMax; gx += step)
                    if (gx > panel.x && gx < panel.xMax)
                        GUI.DrawTexture(new Rect(gx, panel.y, 1f, size), texWhiteDot);
                float startY = center.y - Mathf.Ceil(half / step) * step;
                for (float gy = startY; gy <= panel.yMax; gy += step)
                    if (gy > panel.y && gy < panel.yMax)
                        GUI.DrawTexture(new Rect(panel.x, gy, size, 1f), texWhiteDot);

                // Кольца дистанции 250 / 500 / 1000 м
                GUI.color = new Color(0.4f, 0.9f, 1f, 0.14f);
                for (int r = 0; r < MapRings.Length; r++)
                {
                    float rr = MapRings[r] * z;
                    if (rr < 6f || rr > half) continue;
                    DrawMapCircle(center, rr);
                }

                // Блипы
                DrawMapBlips(center, z, pp, panel);

                // Игрок в центре
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(center.x - 3f, center.y - 3f, 6f, 6f), texKnob);
                GUI.color = new Color(1f, 1f, 0.9f, 0.9f);
                GUI.Label(new Rect(center.x + 6f, center.y + 4f, 14f, 14f), "С", hudTextStyle);
                GUI.color = prev;
            }
            catch {}
        }

        private static readonly float[] MapRings = new float[] { 250f, 500f, 1000f };
        private static readonly Vector2[] CircleUnitPoints = new Vector2[16];

        public static void InitMapGeometry()
        {
            for (int i = 0; i < 16; i++)
            {
                float a = (float)i / 16f * 2f * Mathf.PI;
                CircleUnitPoints[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
        }

        private void DrawMapCircle(Vector2 center, float radius)
        {
            for (int i = 0; i < 16; i++)
            {
                int next = (i + 1) % 16;
                Vector2 p1 = center + CircleUnitPoints[i] * radius;
                Vector2 p2 = center + CircleUnitPoints[next] * radius;
                DrawMapSegment(p1, p2, 1.5f);
            }
        }

        private void DrawMapSegment(Vector2 from, Vector2 to, float th)
        {
            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.5f || len > 500f) return;
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, from);
            GUI.DrawTexture(new Rect(from.x, from.y - th * 0.5f, len, th), texWhiteDot);
            GUI.matrix = old;
        }

        private void DrawMapBlips(Vector2 center, float z, Vector3 pp, Rect panel)
        {
            Color prev = GUI.color;
            // Острова
            for (int i = 0; i < cachedLandmarks.Count; i++)
            {
                Landmark lm = cachedLandmarks[i];
                if (lm == null) continue;
                DrawMapBlip(center, z, pp, lm.transform.position, new Color(0.25f, 0.95f, 0.4f), 6f, panel, "Остров");
            }
            // Клады
            for (int i = 0; i < cachedTreasures.Count; i++)
            {
                TreasurePoint tp = cachedTreasures[i];
                if (tp == null) continue;
                DrawMapBlip(center, z, pp, tp.transform.position, new Color(1f, 0.85f, 0.15f), 4f, panel, null);
            }
            // Акулы
            for (int i = 0; i < cachedSharksInfo.Count; i++)
            {
                SharkESPInfo info = cachedSharksInfo[i];
                if (info == null || info.transform == null) continue;
                DrawMapBlip(center, z, pp, info.transform.position, new Color(1f, 0.3f, 0.4f), 5f, panel, null);
            }
            // Игроки
            for (int i = 0; i < v14Players.Count; i++)
            {
                Network_Player p = v14Players[i];
                if (p == null) continue;
                DrawMapBlip(center, z, pp, p.transform.position, new Color(0.2f, 1f, 0.5f), 5f, panel, null);
            }
            // Плот
            if (cachedRaft != null)
                DrawMapBlip(center, z, pp, cachedRaft.transform.position, new Color(0.25f, 0.85f, 1f), 8f, panel, "ПЛОТ");
            // Точки
            for (int i = 0; i < waypoints.Count; i++)
                DrawMapBlip(center, z, pp, waypoints[i].position, new Color(0.9f, 0.4f, 1f), 5f, panel, waypoints[i].name);
            GUI.color = prev;
        }

        private void DrawMapBlip(Vector2 center, float z, Vector3 pp, Vector3 wpos, Color col, float bs, Rect panel, string caption)
        {
            Vector3 rel = wpos - pp;
            float px = center.x + rel.x * z;
            float py = center.y - rel.z * z;
            if (px < panel.x + 2f || px > panel.xMax - 2f || py < panel.y + 2f || py > panel.yMax - 2f)
            {
                // Прижимаем к краю для далёких целей
                Vector2 d = new Vector2(px - center.x, py - center.y);
                float l = d.magnitude;
                if (l < 1f) return;
                Vector2 cl = center + d / l * (panel.width / 2f - 6f);
                px = cl.x; py = cl.y;
                col.a = 0.45f;
            }
            GUI.color = col;
            GUI.DrawTexture(new Rect(px - bs / 2f, py - bs / 2f, bs, bs), texWhiteDot);
            if (!string.IsNullOrEmpty(caption))
                GUI.Label(new Rect(px + 5f, py - 6f, 110f, 14f), caption, hudTextStyle);
        }

        // ---------- УМНЫЕ НАПОМИНАНИЯ ----------
        private void UpdateReminders(Network_Player player)
        {
            try
            {
                // Урожай созрел
                if (Time.time > cropReminderTime)
                {
                    int ready = 0;
                    for (int i = 0; i < cropTags.Count; i++)
                        if (cropTags[i] != null && cropTags[i].Label != null && cropTags[i].Label.Contains("ГОТОВ")) ready++;
                    if (ready > 0)
                    {
                        cropReminderTime = Time.time + 60f;
                        statusMessage = "🌱 Напоминание: готов урожай на " + ready + " растениях!";
                        LogEvent("🌱 Урожай созрел: " + ready);
                    }
                    else cropReminderTime = Time.time + 15f;
                }

                // Двигатель без топлива
                if (Time.time > fuelReminderTime)
                {
                    fuelReminderTime = Time.time + 90f;
                    if (enginesCount > 0 && enginesFuel == 0)
                    {
                        statusMessage = "⚓ Напоминание: двигатели без топлива!";
                        LogEvent("⚓ Двигатель без топлива");
                    }
                }

                // Нет батареек
                if (Time.time > batteryReminderTime)
                {
                    batteryReminderTime = Time.time + 120f;
                    if (chargersTotal > 0 && chargersWithBattery == 0)
                    {
                        statusMessage = "🔋 Напоминание: в зарядках нет батареек!";
                        LogEvent("🔋 Зарядки пустые");
                    }
                }

                // Скоро смена погоды
                if (Time.time > weatherReminderTime && cachedWeather != null)
                {
                    try
                    {
                        float t = cachedWeather.WeatherTimer;
                        if (t >= 0f && t < 45f)
                        {
                            weatherReminderTime = Time.time + 60f;
                            statusMessage = "☁ Напоминание: смена погоды через " + t.ToString("F0") + " с";
                            LogEvent("☁ Погода скоро сменится");
                        }
                        else weatherReminderTime = Time.time + 10f;
                    }
                    catch {}
                }
            }
            catch {}
        }

        private readonly Dictionary<string, int> resNowDict = new Dictionary<string, int>();
        private readonly Dictionary<string, string> resNamesDict = new Dictionary<string, string>();
        private readonly List<string> resGoneList = new List<string>();

        // ---------- ТАБЛИЦА ДОБЫЧИ РЕСУРСОВ ----------
        private void TrackResources(Network_Player player)
        {
            try
            {
                if (player == null || player.Inventory == null || player.Inventory.allSlots == null) return;

                resNowDict.Clear();
                resNamesDict.Clear();
                int slotCount = player.Inventory.allSlots.Count;
                for (int i = 0; i < slotCount; i++)
                {
                    Slot s = player.Inventory.allSlots[i];
                    if (s == null || !s.HasValidItemInstance() || s.itemInstance == null || s.itemInstance.baseItem == null) continue;
                    string un = s.itemInstance.baseItem.UniqueName;
                    int amt = s.itemInstance.Amount;
                    if (resNowDict.ContainsKey(un)) resNowDict[un] += amt;
                    else resNowDict[un] = amt;

                    if (!resNamesDict.ContainsKey(un))
                    {
                        string dn = un;
                        try { if (s.itemInstance.baseItem.settings_Inventory != null && !string.IsNullOrEmpty(s.itemInstance.baseItem.settings_Inventory.DisplayName)) dn = s.itemInstance.baseItem.settings_Inventory.DisplayName; } catch {}
                        resNamesDict[un] = dn;
                    }
                }

                if (!resTrackingInit) { resTrackingInit = true; lastResCounts.Clear(); }

                foreach (KeyValuePair<string, int> pair in resNowDict)
                {
                    int last;
                    lastResCounts.TryGetValue(pair.Key, out last);
                    int delta = pair.Value - last;
                    if (delta > 0)
                    {
                        string label = resNamesDict.ContainsKey(pair.Key) ? resNamesDict[pair.Key] : pair.Key;
                        if (resourceGains.ContainsKey(label)) resourceGains[label] += delta;
                        else resourceGains[label] = delta;
                    }
                    lastResCounts[pair.Key] = pair.Value;
                }

                resGoneList.Clear();
                foreach (KeyValuePair<string, int> pair in lastResCounts)
                    if (!resNowDict.ContainsKey(pair.Key)) resGoneList.Add(pair.Key);
                for (int i = 0; i < resGoneList.Count; i++) lastResCounts.Remove(resGoneList[i]);
            }
            catch {}
        }

        // ---------- АВТО-СБОР ЯИЦ И ГНЁЗД ----------
        private void UpdateAutoNests(Network_Player player)
        {
            try
            {
                if (BirdsNest.AllNests == null || player == null) return;
                int got = 0;
                for (int i = 0; i < BirdsNest.AllNests.Count; i++)
                {
                    BirdsNest nest = BirdsNest.AllNests[i];
                    if (nest == null) continue;
                    try
                    {
                        if (nest.HasEgg && nest.PickupEgg(player)) got++;
                        if (nest.HasFeather && nest.PickupFeather(player)) got++;
                    }
                    catch {}
                }
                if (got > 0)
                {
                    statusMessage = "🥚 Собрано из гнёзд: " + got + " шт";
                    LogEvent("🥚 Гнёзда: +" + got);
                }
            }
            catch {}
        }

        // ---------- АВТО-ПОСАДКА СЕМЯН ----------
        private void ParsePlantKeywords()
        {
            if (plantKeywords == plantKeywordsParsed) return;
            plantKeywordsParsed = plantKeywords;
            List<string> keys = new List<string>();
            if (!string.IsNullOrEmpty(plantKeywords))
            {
                string[] parts = plantKeywords.ToLower().Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    string k = parts[i].Trim();
                    if (k.Length > 0) keys.Add(k);
                }
            }
            plantKeywordsCache = keys.ToArray();
        }

        private void UpdateAutoPlant(Network_Player player)
        {
            if (Time.time < nextPlantTime || player == null || player.Inventory == null) return;
            nextPlantTime = Time.time + 3f;

            try
            {
                ParsePlantKeywords();
                if (plantKeywordsCache.Length == 0) return;

                // Ищем подходящий семенной предмет
                Item_Base seed = null;
                if (player.Inventory.allSlots != null)
                {
                    for (int i = 0; i < player.Inventory.allSlots.Count && seed == null; i++)
                    {
                        Slot s = player.Inventory.allSlots[i];
                        if (s == null || !s.HasValidItemInstance() || s.itemInstance == null || s.itemInstance.baseItem == null) continue;
                        string un = s.itemInstance.baseItem.UniqueName.ToLower();
                        for (int k = 0; k < plantKeywordsCache.Length; k++)
                        {
                            if (un.Contains(plantKeywordsCache[k])) { seed = s.itemInstance.baseItem; break; }
                        }
                    }
                }
                if (seed == null) return;

                PlantManager pm = UnityEngine.Object.FindObjectOfType<PlantManager>();
                if (pm == null) return;

                int planted = 0;
                var plots = PlantManager.allCropplots;
                if (plots == null) return;

                for (int i = 0; i < plots.Count && planted < 6; i++)
                {
                    Cropplot plot = plots[i];
                    if (plot == null) continue;
                    bool full = false;
                    try { full = plot.IsFull; } catch {}
                    if (full) continue;

                    List<PlantationSlot> slots = null;
                    int freeIdx = -1;
                    try { slots = plot.GetSlots(); } catch {}
                    if (slots == null) continue;
                    for (int s = 0; s < slots.Count; s++)
                    {
                        if (slots[s] == null) continue;
                        if (!slots[s].busy) { freeIdx = s; break; }
                    }
                    if (freeIdx < 0) continue;

                    Plant prefab = null;
                    try { prefab = pm.GetPlantByIndex(seed.UniqueIndex); } catch {}
                    if (prefab == null) continue;

                    bool ok = false;
                    try
                    {
                        ok = plot.PlantSeed(prefab, (uint)Mathf.Abs(seed.UniqueIndex), true, freeIdx);
                    }
                    catch {}

                    if (ok)
                    {
                        try { player.Inventory.RemoveItem(seed.UniqueName, 1); } catch {}
                        planted++;
                    }
                }

                if (planted > 0)
                {
                    statusMessage = "🌱 Посажено семян: " + planted;
                    LogEvent("🌱 Авто-посадка: " + planted);
                }
            }
            catch {}
        }

        // ---------- АВТОСКАЕР РАДАРА ----------
        private void UpdateAutoRadar()
        {
            if (Time.time < nextRadarScanTime) return;
            nextRadarScanTime = Time.time + 5f;

            radarScanIndex = (radarScanIndex + 1) % 8;
            SetRadarFrequency(radarScanIndex);
            LogEvent("📡 Радар-скан: " + GetFrequencyCode(radarScanIndex));
        }

        // ---------- АВТО-РЫБАЛКА 2.0 ----------
        private void UpdateAutoFishPro(Network_Player player)
        {
            if (Time.time < nextFishProTime || player == null) return;
            nextFishProTime = Time.time + 1.6f;

            try
            {
                // Авто-выбор наживки по ключевому слову
                if (!string.IsNullOrEmpty(baitKeyword))
                {
                    FishingBaitHandler bait = player.GetComponent<FishingBaitHandler>();
                    if (bait == null) bait = UnityEngine.Object.FindObjectOfType<FishingBaitHandler>();
                    if (bait != null)
                    {
                        Item_Base want = FindBaitInInventory(player, baitKeyword.ToLower());
                        if (want != null)
                        {
                            Item_Base cur = null;
                            try { cur = bait.CurrentBait; } catch {}
                            if (cur == null || cur.UniqueName != want.UniqueName)
                            {
                                bait.EquipBait(want);
                                LogEvent("🎣 Наживка: " + want.UniqueName);
                            }
                        }
                    }
                }

                // Авто-подсечение (как v11)
                var rod = UnityEngine.Object.FindObjectOfType<FishingRod>();
                if (rod == null) return;
                if (mPullItems == null)
                    mPullItems = typeof(FishingRod).GetMethod("PullItemsFromSea", BindingFlags.NonPublic | BindingFlags.Instance);
                if (mPullItems != null)
                {
                    mPullItems.Invoke(rod, null);
                    statusMessage = "🎣 Рыбалка 2.0: улов в сумку!";
                }
            }
            catch {}
        }

        private Item_Base FindBaitInInventory(Network_Player player, string keyword)
        {
            try
            {
                if (player.Inventory == null || player.Inventory.allSlots == null) return null;
                for (int i = 0; i < player.Inventory.allSlots.Count; i++)
                {
                    Slot s = player.Inventory.allSlots[i];
                    if (s == null || !s.HasValidItemInstance() || s.itemInstance == null || s.itemInstance.baseItem == null) continue;
                    if (s.itemInstance.baseItem.UniqueName.ToLower().Contains(keyword)) return s.itemInstance.baseItem;
                }
            }
            catch {}
            return null;
        }

        // ---------- КАЛЬКУЛЯТОР БИОТОПЛИВА ----------
        private string CalcBiofuel()
        {
            try
            {
                if (Time.time > nextBioTankTime)
                {
                    nextBioTankTime = Time.time + 3f;
                    cachedBioTank = UnityEngine.Object.FindObjectOfType<Tank_BiofuelExtractor>();
                }
                if (cachedBioTank == null) return "Биореактор не найден";

                float cur = 0f, max = 0f;
                try { cur = cachedBioTank.CurrentTankAmount; } catch {}
                try { max = cachedBioTank.maxCapacity; } catch {}
                float remain = Mathf.Max(0f, max - cur);
                float perUnit = Mathf.Clamp(biofuelRawPerUnit, 0.01f, 1f);
                int need = Mathf.CeilToInt(remain / perUnit);
                return "Бак: " + cur.ToString("F1") + "/" + max.ToString("F1") +
                    " • до полного ≈" + need + " сырья (1 сырье = +" + biofuelRawPerUnit.ToString("F2") + ")";
            }
            catch { return "Биореактор не найден"; }
        }

        // ---------- ФИЛЬТР МАГНИТА ----------
        private bool IsMagnetAllowed(PickupItem p)
        {
            if (!magnetOnlyFood && !magnetOnlyMetal && !magnetOnlyRare) return true;

            string rus = GetRussianItemName(p).ToLower();
            string raw = p.name.ToLower();

            if (magnetOnlyFood &&
                (raw.Contains("potato") || raw.Contains("beet") || raw.Contains("meat") || raw.Contains("fish")
                 || raw.Contains("mango") || raw.Contains("coconut") || raw.Contains("watermelon")
                 || raw.Contains("pineapple") || raw.Contains("mushroom") || raw.Contains("egg")
                 || rus.Contains("картофел") || rus.Contains("свекл") || rus.Contains("мясо") || rus.Contains("рыб")
                 || rus.Contains("манго") || rus.Contains("кокос") || rus.Contains("арбуз") || rus.Contains("ананас")
                 || rus.Contains("гриб") || rus.Contains("яйц"))) return true;

            if (magnetOnlyMetal &&
                (raw.Contains("ore") || raw.Contains("ingot") || raw.Contains("scrap") || raw.Contains("metal")
                 || raw.Contains("nail") || raw.Contains("bolt") || raw.Contains("hinge") || raw.Contains("plank")
                 || raw.Contains("plastic") || raw.Contains("vine") || raw.Contains("stone") || raw.Contains("clay")
                 || raw.Contains("sand") || raw.Contains("dirt") || rus.Contains("руда") || rus.Contains("слиток")
                 || rus.Contains("хлам") || rus.Contains("металл") || rus.Contains("доск") || rus.Contains("пластик"))) return true;

            if (magnetOnlyRare &&
                (raw.Contains("titanium") || raw.Contains("explosive") || raw.Contains("vinegoo")
                 || raw.Contains("battery") || raw.Contains("circuit") || rus.Contains("титан")
                 || rus.Contains("взрывчатк") || rus.Contains("клей") || rus.Contains("батаре"))) return true;

            return false;
        }

        // ---------- СПАСАТЕЛЬНЫЙ КРЮК ----------
        private void UpdateRescueHook(Network_Player player)
        {
            try
            {
                Raft raft = cachedRaft != null ? cachedRaft : ComponentManager<Raft>.Value;
                if (player == null || raft == null) { rescueTimer = 0f; return; }

                Vector3 d = player.transform.position - raft.transform.position;
                d.y = 0f;
                bool overboard = d.magnitude > 6f && player.transform.position.y < 0.6f;

                if (overboard)
                {
                    rescueTimer += 0.3f;
                    if (rescueTimer >= Mathf.Clamp(rescueDelay, 1f, 15f))
                    {
                        rescueTimer = 0f;
                        player.transform.position = raft.transform.position + Vector3.up * 2f;
                        if (player.PersonController != null) player.PersonController.ResetFallDuration();
                        statusMessage = "🪝 Спасательный крюк: возвращен на плот!";
                        LogEvent("🪝 Игрок возвращен на плот");
                    }
                    else if (rescueTimer > 0.6f)
                    {
                        statusMessage = "🪝 За бортом! Возврат через " + Mathf.Max(0f, rescueDelay - rescueTimer).ToString("F0") + " с";
                    }
                }
                else rescueTimer = 0f;
            }
            catch { rescueTimer = 0f; }
        }

        // ---------- ТЕПЛОВИЗОР ----------
        private static readonly string[] thermalKeywords = new string[]
        {
            "shark", "bear", "hyena", "boar", "rat", "puffer", "angler", "butler", "boss", "varuna", "utopia"
        };

        private void ScanThermalTargets(Network_Player player)
        {
            thermalTargets.Clear();
            if (player == null) return;
            try
            {
                Vector3 pp = player.transform.position;
                float maxDistSqr = 100f * 100f;
                for (int i = 0; i < cachedEntities.Count; i++)
                {
                    Network_Entity ent = cachedEntities[i];
                    if (ent == null || ent.transform == null) continue;
                    if (ent.gameObject == player.gameObject) continue;
                    if ((ent.transform.position - pp).sqrMagnitude > maxDistSqr) continue;
                    try { if (ent.stat_health != null && ent.stat_health.IsDead()) continue; } catch {}

                    string n = ent.name.ToLower();
                    bool predator = false;
                    for (int k = 0; k < thermalKeywords.Length; k++)
                        if (n.Contains(thermalKeywords[k])) { predator = true; break; }
                    if (!predator) continue;

                    ThermalTarget t;
                    if (thermalTargets.Count < thermalTargetPool.Count)
                    {
                        t = thermalTargetPool[thermalTargets.Count];
                    }
                    else
                    {
                        t = new ThermalTarget();
                        thermalTargetPool.Add(t);
                    }
                    t.T = ent.transform;
                    t.Col = (n.Contains("shark") || n.Contains("boss")) ? new Color(1f, 0.2f, 0.3f) : new Color(1f, 0.55f, 0.1f);
                    thermalTargets.Add(t);
                }
            }
            catch {}
        }

        private void DrawThermalOverlay(Camera cam)
        {
            if (thermalTargets.Count == 0) return;
            Color prev = GUI.color;
            Vector3 camPos = cam.transform.position;
            Vector3 camFwd = cam.transform.forward;
            for (int i = 0; i < thermalTargets.Count; i++)
            {
                ThermalTarget t = thermalTargets[i];
                if (t.T == null) continue;
                if (Vector3.Dot(camFwd, (t.T.position - camPos)) <= 0.1f) continue;
                Vector3 sp = cam.WorldToScreenPoint(t.T.position + Vector3.up * 0.6f);
                if (sp.z <= 0.5f || sp.x < -80f || sp.x > Screen.width + 80f || sp.y < -80f || sp.y > Screen.height + 80f) continue;

                float size = Mathf.Clamp(1600f / sp.z, 24f, 320f);
                Rect r = new Rect(sp.x - size * 0.5f, Screen.height - sp.y - size * 0.5f, size, size);
                GUI.color = new Color(t.Col.r, t.Col.g, t.Col.b, 0.22f);
                GUI.DrawTexture(r, texWhiteDot);
                GUI.color = new Color(t.Col.r, t.Col.g, t.Col.b, 0.85f);
                DrawOutlineRect(r, 2f);
            }
            GUI.color = prev;
        }

        // ---------- КРЫСОЛОВ ----------
        private void KillRats()
        {
            try
            {
                var host = ComponentManager<Network_Host>.Value;
                if (host == null) return;

                var rats = UnityEngine.Object.FindObjectsOfType<AI_NetworkBehaviour_Rat>();
                int killed = 0;
                for (int i = 0; i < rats.Length; i++)
                {
                    AI_NetworkBehaviour_Rat rat = rats[i];
                    if (rat == null || rat.networkEntity == null) continue;
                    int id = rat.GetInstanceID();
                    if (deadRatIds.Contains(id)) continue;
                    try
                    {
                        host.DamageEntity(rat.networkEntity, rat.transform, 999999f, rat.transform.position, Vector3.up, EntityType.Enemy, null);
                        deadRatIds.Add(id);
                        statRats++;
                        killed++;
                    }
                    catch {}
                }
                if (killed > 0)
                {
                    statusMessage = "🐀 Крысолов: уничтожено " + killed + " (всего " + statRats + ")";
                    LogEvent("🐀 Крысы: -" + killed);
                }
            }
            catch {}
        }

        // ---------- ПИТОМЦЫ ВСЕГДА СЫТЫ ----------
        private void UpdatePetsFed()
        {
            if (Time.time < nextPetsTime) return;
            nextPetsTime = Time.time + 2f;
            try
            {
                if (fHungerTimer == null)
                    fHungerTimer = typeof(AI_NetworkBehaviour_Domestic).GetField("_hungerTimer", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fHungerTimer == null) return;

                var animals = UnityEngine.Object.FindObjectsOfType<AI_NetworkBehaviour_Domestic>();
                for (int i = 0; i < animals.Length; i++)
                {
                    AI_NetworkBehaviour_Domestic a = animals[i];
                    if (a == null) continue;
                    try { fHungerTimer.SetValue(a, 0f); } catch {}
                }
            }
            catch {}
        }

        // ---------- ЭТАЖНЫЙ КОПИР ----------
        private void CopyFloor()
        {
            try
            {
                if (selectedBlocks.Count == 0) { statusMessage = "Сначала выделите блоки (ЛКМ)"; return; }
                Raft raft = ComponentManager<Raft>.Value;
                if (raft == null) { statusMessage = "Плот не найден"; return; }

                floorBlocks.Clear();
                for (int i = 0; i < selectedBlocks.Count; i++)
                {
                    Block b = selectedBlocks[i];
                    if (b == null) continue;
                    FloorBlockCopy fb = new FloorBlockCopy();
                    fb.LocalPos = raft.transform.InverseTransformPoint(b.transform.position);
                    fb.Item = b.buildableItem;
                    floorBlocks.Add(fb);
                }
                floorHasCopy = floorBlocks.Count > 0;
                statusMessage = floorHasCopy ? "Этаж скопирован: " + floorBlocks.Count + " блоков" : "Блоки без предмета";
                LogEvent("Этаж скопирован (" + floorBlocks.Count + ")");
            }
            catch (Exception ex) { statusMessage = "Ошибка копии этажа: " + ex.Message; }
        }

        private void PasteFloor()
        {
            try
            {
                if (!floorHasCopy) { statusMessage = "Сначала скопируйте этаж"; return; }
                Raft raft = ComponentManager<Raft>.Value;
                BlockCreator bc = UnityEngine.Object.FindObjectOfType<BlockCreator>();
                if (raft == null || bc == null) { statusMessage = "Плот/BlockCreator не найден"; return; }

                float bSize = BlockCreator.BlockSize > 0.5f ? BlockCreator.BlockSize : 1.5f;
                HashSet<string> used = new HashSet<string>();
                int placed = 0;

                for (int i = 0; i < floorBlocks.Count; i++)
                {
                    FloorBlockCopy fb = floorBlocks[i];
                    Item_Base item = fb.Item != null ? fb.Item : ItemManager.GetItemByName("Foundation");
                    if (item == null) continue;
                    try
                    {
                        Vector3 targetLocal = fb.LocalPos + new Vector3(0f, floorPasteOffset, 0f);
                        Vector3 world = raft.transform.TransformPoint(targetLocal);
                        BlockCreator.SnapBuildPosition(ref world);
                        Vector3 local = raft.transform.InverseTransformPoint(world);
                        local.y = fb.LocalPos.y + floorPasteOffset;
                        string key = Mathf.RoundToInt(local.x / bSize * 2f) + ":" +
                                     Mathf.RoundToInt(local.y / bSize * 2f) + ":" +
                                     Mathf.RoundToInt(local.z / bSize * 2f);
                        if (!used.Add(key)) continue;
                        bc.CreateBlockCheat(item, local, Vector3.zero, DPS.Default, 0);
                        placed++;
                    }
                    catch {}
                }
                statusMessage = "Этаж вставлен: " + placed + " блоков";
                LogEvent("Этаж вставлен (" + placed + ")");
            }
            catch (Exception ex) { statusMessage = "Ошибка вставки этажа: " + ex.Message; }
        }

        // ---------- HUD ТАБЛИЦА ИГРОКОВ ----------
        private void ScanPlayersTable(Network_Player localPlayer)
        {
            v14Players.Clear();
            teammates.Clear();
            try
            {
                for (int i = 0; i < cachedPlayers.Count; i++)
                {
                    Network_Player p = cachedPlayers[i];
                    if (p == null) continue;
                    if (localPlayer != null && p.gameObject == localPlayer.gameObject) continue;
                    v14Players.Add(p);

                    TeammateInfo ti = new TeammateInfo();
                    ti.Player = p;
                    try { if (p.playerNameTextMesh != null && !string.IsNullOrEmpty(p.playerNameTextMesh.text)) ti.Name = p.playerNameTextMesh.text; } catch {}
                    ti.Dist = localPlayer != null ? Vector3.Distance(localPlayer.transform.position, p.transform.position) : 0f;

                    // Статы (могут не синхронизироваться на наш клиент — тогда -1)
                    try
                    {
                        PlayerStats st = p.Stats;
                        if (st != null && st.stat_health != null)
                        {
                            try { ti.Dead = st.stat_health.IsDead(); } catch {}
                            try { ti.Hp = Mathf.Clamp01(st.stat_health.NormalValue); } catch {}
                            if (ti.Hp < 0f)
                            {
                                try { float m = st.stat_health.Max; if (m > 1f) ti.Hp = Mathf.Clamp01(st.stat_health.Value / m); } catch {}
                            }
                            ti.StatsOk = ti.Hp >= 0f || ti.Dead;
                        }
                    }
                    catch {}
                    try
                    {
                        PlayerStats st2 = p.Stats;
                        ti.Hunger = ConsumableNorm(st2 != null ? st2.stat_hunger : null);
                        ti.Thirst = ConsumableNorm(st2 != null ? st2.stat_thirst : null);
                    }
                    catch {}
                    if (ti.Hunger >= 0f || ti.Thirst >= 0f) ti.StatsOk = true;

                    // Локация
                    Vector3 pp = p.transform.position;
                    if (ti.Dead) ti.Status = "☠ мёртв";
                    else if (pp.y < 0.25f) ti.Status = "💧 в воде";
                    else if (cachedRaft != null && Vector3.Distance(pp, cachedRaft.transform.position) < 14f) ti.Status = "⚓ на плоту";
                    else
                    {
                        bool onIsland = false;
                        for (int k = 0; k < cachedLandmarks.Count; k++)
                        {
                            Landmark lm = cachedLandmarks[k];
                            if (lm == null) continue;
                            if (Vector3.Distance(pp, lm.transform.position) < 150f) { onIsland = true; break; }
                        }
                        ti.Status = onIsland ? "🏝 на острове" : "🌊 в открытом море";
                    }

                    // Акула рядом = бой
                    for (int k = 0; k < cachedSharksInfo.Count; k++)
                    {
                        SharkESPInfo info = cachedSharksInfo[k];
                        if (info == null || info.transform == null) continue;
                        if (Vector3.Distance(pp, info.transform.position) < 16f) { ti.Fighting = true; break; }
                    }

                    teammates.Add(ti);
                }
            }
            catch {}
        }

        private float ConsumableNorm(Stat_Bonus_Consumable c)
        {
            if (c == null) return -1f;
            try { if (c.normalConsumable != null) return Mathf.Clamp01(c.normalConsumable.NormalValue); } catch {}
            try
            {
                if (c.normalConsumable != null)
                {
                    float m = c.normalConsumable.Max;
                    if (m > 1f) return Mathf.Clamp01(c.normalConsumable.Value / m);
                }
            }
            catch {}
            return -1f;
        }

        private void DrawPlayersTableHud()
        {
            if (!hudPlayersTable) return;
            int rows = v14Players.Count;
            if (rows == 0) return;
            try
            {
                Network_Player me = ComponentManager<Network_Player>.Value;
                float w = 224f;
                float rowH = hudTeammateStatus ? 64f : 28f;
                float h = rows * rowH + 26f;
                Rect panel = new Rect(Screen.width - w - 10f, 192f, w, h);
                GUI.Box(panel, GUIContent.none, hudPanelStyle);
                GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, panel.width - 16f, 16f), "<b>👥 ИГРОКИ (" + rows + ")</b>", hudTitleStyle);

                for (int i = 0; i < rows; i++)
                {
                    float ry = panel.y + 22f + i * rowH;
                    Network_Player p = v14Players[i];
                    if (p == null) continue;
                    string nm = "Игрок";
                    try { if (p.playerNameTextMesh != null && !string.IsNullOrEmpty(p.playerNameTextMesh.text)) nm = p.playerNameTextMesh.text; } catch {}

                    TeammateInfo ti = null;
                    if (i < teammates.Count) ti = teammates[i];

                    Color prev = GUI.color;
                    if (ti != null && ti.Dead) GUI.color = new Color(1f, 0.35f, 0.35f);
                    GUI.Label(new Rect(panel.x + 8f, ry, panel.width - 16f, 13f), (ti != null && ti.Dead ? "☠ " : "") + nm, hudTextStyle);
                    GUI.color = prev;

                    string info = me != null ? Vector3.Distance(me.transform.position, p.transform.position).ToString("F0") + " м" : "? м";
                    if (ti != null && !string.IsNullOrEmpty(ti.Status)) info += " • " + ti.Status + (ti.Fighting ? " ⚠" : "");
                    GUI.Label(new Rect(panel.x + 8f, ry + 13f, panel.width - 16f, 12f), info, subLabelStyle);

                    if (hudTeammateStatus)
                    {
                        float bw = (panel.width - 16f - 12f) / 3f;
                        float hp = ti != null ? ti.Hp : -1f;
                        float hu = ti != null ? ti.Hunger : -1f;
                        float th = ti != null ? ti.Thirst : -1f;
                        DrawTeammateStatBar(panel.x + 8f, ry + 26f, bw, hp,
                            Color.Lerp(new Color(1f, 0.25f, 0.3f), new Color(0.3f, 1f, 0.5f), Mathf.Clamp01(hp)), "HP");
                        DrawTeammateStatBar(panel.x + 8f + bw + 6f, ry + 26f, bw, hu, new Color(1f, 0.75f, 0.2f), "🍗");
                        DrawTeammateStatBar(panel.x + 8f + (bw + 6f) * 2f, ry + 26f, bw, th, new Color(0.3f, 0.7f, 1f), "💧");
                    }
                }
            }
            catch {}
        }

        private void DrawTeammateStatBar(float x, float y, float w, float norm, Color col, string label)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(x, y, w, 7f), texWhiteDot);
            if (norm >= 0f)
            {
                GUI.color = col;
                GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(norm), 7f), texWhiteDot);
            }
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(x, y + 7f, w + 14f, 12f), norm >= 0f ? label + " " + Mathf.RoundToInt(norm * 100f) + "%" : label + " —", subLabelStyle);
        }

        // ---------- СУМКА ДРУГА (только просмотр) ----------
        private void UpdateBagViewer()
        {
            if (Time.time < nextBagTime) return;
            nextBagTime = Time.time + 0.5f;
            try
            {
                Network_Player me = ComponentManager<Network_Player>.Value;
                bagTargets.Clear();
                var all = UnityEngine.Object.FindObjectsOfType<Network_Player>();
                for (int i = 0; i < all.Length; i++)
                {
                    Network_Player p = all[i];
                    if (p == null) continue;
                    if (me != null && p.gameObject == me.gameObject) continue;
                    bagTargets.Add(p);
                }

                if (bagTargets.Count == 0)
                {
                    bagViewerIndex = -1;
                    bagHeader = "других игроков нет";
                    bagLines.Clear();
                    return;
                }
                if (bagViewerIndex < 0 || bagViewerIndex >= bagTargets.Count) bagViewerIndex = 0;

                Network_Player t = bagTargets[bagViewerIndex];
                string nm = "Игрок";
                try { if (t.playerNameTextMesh != null && !string.IsNullOrEmpty(t.playerNameTextMesh.text)) nm = t.playerNameTextMesh.text; } catch {}
                bagHeader = nm + "  (" + (bagViewerIndex + 1) + "/" + bagTargets.Count + ")";

                int total = 0;
                try
                {
                    if (t.Inventory != null && t.Inventory.allSlots != null)
                    {
                        for (int i = 0; i < t.Inventory.allSlots.Count; i++)
                        {
                            Slot s = t.Inventory.allSlots[i];
                            if (s == null || !s.HasValidItemInstance() || s.itemInstance == null || s.itemInstance.baseItem == null) continue;
                            string dn = s.itemInstance.baseItem.UniqueName;
                            try { if (s.itemInstance.baseItem.settings_Inventory != null && !string.IsNullOrEmpty(s.itemInstance.baseItem.settings_Inventory.DisplayName)) dn = s.itemInstance.baseItem.settings_Inventory.DisplayName; } catch {}
                            bagLines.Add(dn + " x" + s.itemInstance.Amount);
                            total += s.itemInstance.Amount;
                        }
                    }
                }
                catch {}

                if (total == 0)
                {
                    bagLines.Add("<i>Сумка пуста, либо инвентарь друга</i>");
                    bagLines.Add("<i>не синхронизируется на ваш клиент</i>");
                    bagLines.Add("<i>(в оригинальной игре сумки закрыты)</i>");
                }
                else
                {
                    // Что у него в руке (выбранный слот хотбара)
                    try
                    {
                        Slot hand = t.Inventory.GetSelectedHotbarSlot();
                        if (hand != null && hand.HasValidItemInstance() && hand.itemInstance != null && hand.itemInstance.baseItem != null)
                        {
                            string hn = hand.itemInstance.baseItem.UniqueName;
                            try { if (hand.itemInstance.baseItem.settings_Inventory != null && !string.IsNullOrEmpty(hand.itemInstance.baseItem.settings_Inventory.DisplayName)) hn = hand.itemInstance.baseItem.settings_Inventory.DisplayName; } catch {}
                            bagHeader += "  •  в руке: " + hn;
                        }
                    }
                    catch {}
                }
            }
            catch {}
        }
    }
}
