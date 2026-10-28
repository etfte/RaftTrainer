using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RaftTrainer
{
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
    // V14: КАРТА, НАПОМИНАНИЯ, РЕСУРСЫ, ГНЁЗДА,
    // ПОСАДКА, РАДАР, РЫБАЛКА 2.0, ЭТАЖИ, ТЕПЛОВИЗОР
    // ==========================================
    public partial class TrainerGUI : MonoBehaviour
    {
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
                float[] rings = new float[] { 250f, 500f, 1000f };
                for (int r = 0; r < rings.Length; r++)
                {
                    float rr = rings[r] * z;
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

        private void DrawMapCircle(Vector2 center, float radius)
        {
            int seg = 32;
            for (int i = 0; i < seg; i++)
            {
                float a1 = (float)i / seg * 2f * Mathf.PI;
                float a2 = (float)(i + 1) / seg * 2f * Mathf.PI;
                Vector2 p1 = center + new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
                Vector2 p2 = center + new Vector2(Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius);
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

        // ---------- ТАБЛИЦА ДОБЫЧИ РЕСУРСОВ ----------
        private void TrackResources(Network_Player player)
        {
            try
            {
                if (player == null || player.Inventory == null || player.Inventory.allSlots == null) return;

                Dictionary<string, int> now = new Dictionary<string, int>();
                Dictionary<string, string> names = new Dictionary<string, string>();
                for (int i = 0; i < player.Inventory.allSlots.Count; i++)
                {
                    Slot s = player.Inventory.allSlots[i];
                    if (s == null || !s.HasValidItemInstance() || s.itemInstance == null || s.itemInstance.baseItem == null) continue;
                    string un = s.itemInstance.baseItem.UniqueName;
                    int amt = s.itemInstance.Amount;
                    if (now.ContainsKey(un)) now[un] += amt;
                    else now[un] = amt;

                    if (!names.ContainsKey(un))
                    {
                        string dn = un;
                        try { if (s.itemInstance.baseItem.settings_Inventory != null && !string.IsNullOrEmpty(s.itemInstance.baseItem.settings_Inventory.DisplayName)) dn = s.itemInstance.baseItem.settings_Inventory.DisplayName; } catch {}
                        names[un] = dn;
                    }
                }

                if (!resTrackingInit) { resTrackingInit = true; lastResCounts.Clear(); }

                foreach (KeyValuePair<string, int> pair in now)
                {
                    int last;
                    lastResCounts.TryGetValue(pair.Key, out last);
                    int delta = pair.Value - last;
                    if (delta > 0)
                    {
                        string label = names.ContainsKey(pair.Key) ? names[pair.Key] : pair.Key;
                        if (resourceGains.ContainsKey(label)) resourceGains[label] += delta;
                        else resourceGains[label] = delta;
                    }
                    lastResCounts[pair.Key] = pair.Value;
                }
                // Убравшиеся из инвентаря предметы просто сбрасываются
                List<string> gone = new List<string>();
                foreach (KeyValuePair<string, int> pair in lastResCounts)
                    if (!now.ContainsKey(pair.Key)) gone.Add(pair.Key);
                for (int i = 0; i < gone.Count; i++) lastResCounts.Remove(gone[i]);
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
                var entities = UnityEngine.Object.FindObjectsOfType<Network_Entity>();
                for (int i = 0; i < entities.Length; i++)
                {
                    Network_Entity ent = entities[i];
                    if (ent == null || ent.transform == null) continue;
                    if (ent.gameObject == player.gameObject) continue;
                    if (Vector3.Distance(ent.transform.position, pp) > 100f) continue;
                    try { if (ent.stat_health != null && ent.stat_health.IsDead()) continue; } catch {}

                    string n = ent.name.ToLower();
                    bool predator = false;
                    for (int k = 0; k < thermalKeywords.Length; k++)
                        if (n.Contains(thermalKeywords[k])) { predator = true; break; }
                    if (!predator) continue;

                    ThermalTarget t = new ThermalTarget();
                    t.T = ent.transform;
                    t.Col = n.Contains("shark") || n.Contains("boss") ? new Color(1f, 0.2f, 0.3f) : new Color(1f, 0.55f, 0.1f);
                    thermalTargets.Add(t);
                }
            }
            catch {}
        }

        private void DrawThermalOverlay(Camera cam)
        {
            if (thermalTargets.Count == 0) return;
            Color prev = GUI.color;
            for (int i = 0; i < thermalTargets.Count; i++)
            {
                ThermalTarget t = thermalTargets[i];
                if (t.T == null) continue;
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
                var all = UnityEngine.Object.FindObjectsOfType<Network_Player>();
                for (int i = 0; i < all.Length; i++)
                {
                    Network_Player p = all[i];
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
            try
            {
                Network_Player me = ComponentManager<Network_Player>.Value;
                float w = 224f;
                int rows = v14Players.Count;
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
