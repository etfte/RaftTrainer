using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RaftTrainer
{
    public class TrailPoint
    {
        public Vector3 Pos;
        public float T;
    }

    // ==========================================
    // V13: ПРОИЗВОДИТЕЛЬНОСТЬ, ТРАССЕРЫ, УРОЖАЙ,
    // МАШИНЫ, ШТОРМ-АВТОПИЛОТ, КРАФТ-ХЕЛПЕР, ФИЛЬТРЫ
    // ==========================================
    public partial class TrainerGUI : MonoBehaviour
    {
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
        private float nextLandmarkStatsTime = 0f;

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
            }

            if (Time.time > nextPerfScanTime)
            {
                nextPerfScanTime = Time.time + 3f;
                try { entityCount = UnityEngine.Object.FindObjectsOfType<Transform>().Length; }
                catch { entityCount = 0; }
                try
                {
                    int now = UnityEngine.Object.FindObjectsOfType<Block>().Length;
                    if (lastBlockCount >= 0 && now < lastBlockCount)
                        statBlocksLost += lastBlockCount - now;
                    lastBlockCount = now;
                    blockCount = now;
                }
                catch { blockCount = 0; }
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
        private void RecordSharkTrail()
        {
            for (int i = 0; i < cachedSharksInfo.Count && i < 3; i++)
            {
                var info = cachedSharksInfo[i];
                if (info == null || info.transform == null) continue;
                TrailPoint tp = new TrailPoint();
                tp.Pos = info.transform.position;
                tp.T = Time.time;
                sharkTrail.Add(tp);
            }
            while (sharkTrail.Count > 0 && Time.time - sharkTrail[0].T > 30f) sharkTrail.RemoveAt(0);
            while (sharkTrail.Count > 400) sharkTrail.RemoveAt(0);
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
                    tag.BuildDist = Vector3.Distance(tag.Position, pp);
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

                // Убитые акулы
                var sharks = UnityEngine.Object.FindObjectsOfType<Shark>();
                for (int i = 0; i < sharks.Length; i++)
                {
                    Shark s = sharks[i];
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

                // Посещенные острова
                if (Time.time > nextLandmarkStatsTime)
                {
                    nextLandmarkStatsTime = Time.time + 3f;
                    statsLandmarks.Clear();
                    var lms = UnityEngine.Object.FindObjectsOfType<Landmark>();
                    for (int i = 0; i < lms.Length; i++) if (lms[i] != null) statsLandmarks.Add(lms[i]);
                }
                if (player != null)
                {
                    Vector3 pp = player.transform.position;
                    for (int i = 0; i < statsLandmarks.Count; i++)
                    {
                        Landmark lm = statsLandmarks[i];
                        if (lm == null) continue;
                        int id = lm.GetInstanceID();
                        if (visitedIslandIds.Contains(id)) continue;
                        if (Vector3.Distance(lm.transform.position, pp) < 150f)
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
                Camera cam = Camera.main;
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
    }
}
