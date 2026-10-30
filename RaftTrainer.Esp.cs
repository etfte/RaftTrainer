using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaftTrainer
{
    public class EspTag
    {
        public Vector3 Position;
        public string Label;
        public Color Col;
        public float MaxDist;
        public float Offset;
        public float BuildDist;
    }

    public partial class TrainerGUI
    {
        private readonly List<Landmark> cachedLandmarks = new List<Landmark>();
        private readonly List<EspTag> espTags = new List<EspTag>();
        private readonly List<EspTag> espTagPool = new List<EspTag>();
        private int espTagsCount = 0;
        private readonly List<SharkESPInfo> espNameSharks = new List<SharkESPInfo>();
        private readonly Dictionary<int, string> espNameCache = new Dictionary<int, string>();
        private readonly HashSet<Transform> sharkSeenTransforms = new HashSet<Transform>();
        private static Vector3 espSortOrigin = Vector3.zero;
        private float nextEspPlayersTime = 0f;
        private float nextEspEntitiesTime = 0f;
        private float nextEspSharkTime = 0f;
        private float nextEspNameSharkTime = 0f;
        private float nextEspPickupTime = 0f;
        private float nextEspTreasureTime = 0f;
        private float nextEspLandmarkTime = 0f;
        private const int EspMaxLabels = 70;

        private static readonly string[] DistStrings = new string[3001];
        private static readonly GUIContent sharedContent = new GUIContent();
        private struct PickupDistEntry
        {
            public PickupItem item;
            public float sqrDist;
        }

        private static readonly List<PickupDistEntry> espPickupScratch = new List<PickupDistEntry>();
        private static readonly Comparison<PickupDistEntry> ItemDistComparison = (a, b) => a.sqrDist.CompareTo(b.sqrDist);
        private static readonly Comparison<EspTag> TagDistComparison = CompareTagsByDistance;
        private static readonly Color BlockDamagedColor = new Color(1f, 0.25f, 0.30f);
        private static readonly Color BlockHealthyColor = new Color(0.20f, 1f, 0.60f);
        private static readonly Color BlockSelectedColor = new Color(1f, 0.85f, 0.20f);
        private readonly List<SharkESPInfo> sharkInfoPool = new List<SharkESPInfo>();
        private readonly List<SharkESPInfo> espNameSharkPool = new List<SharkESPInfo>();

        private static readonly Color TagSharkCol = new Color(1f, 0.25f, 0.35f);
        private static readonly Color TagBarrelCol = new Color(1f, 0.85f, 0.2f);
        private static readonly Color TagTreasureCol = new Color(1f, 0.85f, 0.1f);
        private static readonly Color TagWaypointCol = new Color(0.2f, 0.9f, 1f);
        private static readonly Color TagRaftCol = new Color(0.3f, 0.8f, 1f);
        private static readonly Color TagPlayerCol = new Color(0.2f, 1f, 0.4f);

        private static readonly Color ColItemWood = new Color(0.9f, 0.7f, 0.4f);
        private static readonly Color ColItemPlastic = new Color(0.4f, 0.85f, 1f);
        private static readonly Color ColItemScrap = new Color(0.75f, 0.8f, 0.9f);
        private static readonly Color ColItemTitanium = new Color(0.3f, 1f, 0.7f);
        private static readonly Color ColItemDefault = new Color(0.85f, 0.85f, 0.95f);

        private static readonly Color ColOreTitanium = new Color(0.3f, 1f, 0.6f);
        private static readonly Color ColOreMetal = new Color(0.4f, 0.8f, 1f);
        private static readonly Color ColOreCopper = new Color(1f, 0.6f, 0.2f);
        private static readonly Color ColOreSeaweed = new Color(0.4f, 1f, 0.3f);
        private static readonly Color ColOreSand = new Color(0.9f, 0.9f, 0.4f);
        private static readonly Color ColOreClam = new Color(0.9f, 0.4f, 1f);
        private static readonly Color ColOreDefault = new Color(0.2f, 0.9f, 1f);

        static TrainerGUI()
        {
            for (int i = 0; i <= 3000; i++)
            {
                DistStrings[i] = "  " + i + "м";
            }
            InitMapGeometry();
        }

        private static int CompareTagsByDistance(EspTag a, EspTag b)
        {
            return a.BuildDist.CompareTo(b.BuildDist);
        }

        private void UpdateEspCache(Network_Player player)
        {
            if (player == null) return;
            try
            {
                float now = Time.time;
                Vector3 pp = player.transform.position;
                cachedRaft = ComponentManager<Raft>.Value;

                if (now >= nextEspPlayersTime)
                {
                    nextEspPlayersTime = now + 1f;
                    cachedPlayers.Clear();
                    if (espPlayers)
                    {
                        var players = UnityEngine.Object.FindObjectsOfType<Network_Player>();
                        for (int i = 0; i < players.Length; i++)
                        {
                            var p = players[i];
                            if (p != null && p.gameObject != player.gameObject) cachedPlayers.Add(p);
                        }
                    }
                }

                if (now >= nextEspEntitiesTime)
                {
                    nextEspEntitiesTime = now + 1f;
                    cachedEntities.Clear();
                    var ents = UnityEngine.Object.FindObjectsOfType<Network_Entity>();
                    for (int i = 0; i < ents.Length; i++)
                    {
                        var ent = ents[i];
                        if (ent != null && ent.gameObject != player.gameObject) cachedEntities.Add(ent);
                    }
                }

                if (now >= nextEspSharkTime)
                {
                    nextEspSharkTime = now + 0.7f;
                    RefreshSharkCache(now);
                }

                if (now >= nextEspPickupTime)
                {
                    nextEspPickupTime = now + 0.5f;
                    if (espAllItems || espBarrels || espUnderwaterOres || itemMagnet) RefreshPickupCache(pp);
                }

                if (now >= nextEspTreasureTime)
                {
                    nextEspTreasureTime = now + 2f;
                    cachedTreasures.Clear();
                    if (espTreasures || instantTreasureDig)
                    {
                        var treasures = UnityEngine.Object.FindObjectsOfType<TreasurePoint>();
                        for (int i = 0; i < treasures.Length; i++)
                        {
                            var tp = treasures[i];
                            if (tp != null && tp.gameObject.activeInHierarchy) cachedTreasures.Add(tp);
                        }
                    }
                }

                if (now >= nextEspLandmarkTime)
                {
                    nextEspLandmarkTime = now + 3f;
                    cachedLandmarks.Clear();
                    var landmarks = UnityEngine.Object.FindObjectsOfType<Landmark>();
                    for (int i = 0; i < landmarks.Length; i++)
                    {
                        if (landmarks[i] != null) cachedLandmarks.Add(landmarks[i]);
                    }
                }

                BuildEspTags(pp);
            }
            catch {}
        }

        private SharkESPInfo GetPooledSharkInfo(Transform t, string n, float h, float maxH)
        {
            SharkESPInfo info;
            if (cachedSharksInfo.Count < sharkInfoPool.Count)
            {
                info = sharkInfoPool[cachedSharksInfo.Count];
            }
            else
            {
                info = new SharkESPInfo();
                sharkInfoPool.Add(info);
            }
            info.transform = t;
            info.name = n;
            info.health = h;
            info.maxHealth = maxH;
            return info;
        }

        private SharkESPInfo GetPooledNameShark(Transform t, string n, float h, float maxH)
        {
            SharkESPInfo info;
            if (espNameSharks.Count < espNameSharkPool.Count)
            {
                info = espNameSharkPool[espNameSharks.Count];
            }
            else
            {
                info = new SharkESPInfo();
                espNameSharkPool.Add(info);
            }
            info.transform = t;
            info.name = n;
            info.health = h;
            info.maxHealth = maxH;
            return info;
        }

        private void RefreshSharkCache(float now)
        {
            cachedSharksInfo.Clear();
            cachedSharks.Clear();

            var seen = sharkSeenTransforms;
            seen.Clear();
            var sharks = UnityEngine.Object.FindObjectsOfType<Shark>();
            for (int i = 0; i < sharks.Length; i++)
            {
                var s = sharks[i];
                if (s == null) continue;
                cachedSharks.Add(s);
                if (!espShark || s.state == SharkState.Dead || s.state == SharkState.DeadSink) continue;
                float cur = 100f;
                float max = 100f;
                if (s.entityComponent != null && s.entityComponent.stat_health != null)
                {
                    if (s.entityComponent.stat_health.IsDead()) continue;
                    cur = s.entityComponent.stat_health.Value;
                    max = s.entityComponent.stat_health.Max;
                }
                cachedSharksInfo.Add(GetPooledSharkInfo(s.transform, "Акула", cur, max));
                seen.Add(s.transform);
            }

            if (espShark)
            {
                var aiSharks = UnityEngine.Object.FindObjectsOfType<AI_NetworkBehavior_Shark>();
                for (int i = 0; i < aiSharks.Length; i++)
                {
                    var a = aiSharks[i];
                    if (a == null || seen.Contains(a.transform)) continue;
                    float cur = 100f;
                    float max = 100f;
                    if (a.networkEntity != null && a.networkEntity.stat_health != null)
                    {
                        if (a.networkEntity.stat_health.IsDead()) continue;
                        cur = a.networkEntity.stat_health.Value;
                        max = a.networkEntity.stat_health.Max;
                    }
                    cachedSharksInfo.Add(GetPooledSharkInfo(a.transform, "Акула", cur, max));
                    seen.Add(a.transform);
                }

                if (now >= nextEspNameSharkTime)
                {
                    nextEspNameSharkTime = now + 3f;
                    espNameSharks.Clear();
                    for (int i = 0; i < cachedEntities.Count; i++)
                    {
                        var ent = cachedEntities[i];
                        if (ent == null) continue;
                        string n = ent.name;
                        if (!ContainsIgnoreCase(n, "shark")) continue;
                        if (ent.stat_health != null && ent.stat_health.IsDead()) continue;
                        float cur = ent.stat_health != null ? ent.stat_health.Value : 100f;
                        float max = ent.stat_health != null ? ent.stat_health.Max : 100f;
                        string sName = (ContainsIgnoreCase(n, "varuna") || ContainsIgnoreCase(n, "boss")) ? "Босс-акула" : "Акула";
                        espNameSharks.Add(GetPooledNameShark(ent.transform, sName, cur, max));
                    }
                }
            }
            for (int i = 0; i < espNameSharks.Count; i++)
            {
                var info = espNameSharks[i];
                if (info.transform == null || seen.Contains(info.transform)) continue;
                cachedSharksInfo.Add(info);
                seen.Add(info.transform);
            }
        }

        private void RefreshPickupCache(Vector3 pp)
        {
            cachedPickups.Clear();
            cachedOres.Clear();

            float radius = Mathf.Max(espItemMaxDist, Mathf.Max(espOreMaxDist, magnetRadius));
            float radiusSqr = radius * radius;
            float itemMaxSqr = espItemMaxDist * espItemMaxDist;
            float oreMaxSqr = espOreMaxDist * espOreMaxDist;

            var all = UnityEngine.Object.FindObjectsOfType<PickupItem>();
            espPickupScratch.Clear();
            for (int i = 0; i < all.Length; i++)
            {
                var it = all[i];
                if (it == null) continue;
                float dSqr = (it.transform.position - pp).sqrMagnitude;
                if (dSqr <= radiusSqr)
                {
                    espPickupScratch.Add(new PickupDistEntry { item = it, sqrDist = dSqr });
                }
            }

            espPickupScratch.Sort(ItemDistComparison);

            for (int i = 0; i < espPickupScratch.Count; i++)
            {
                var entry = espPickupScratch[i];
                var it = entry.item;
                float dSqr = entry.sqrDist;
                if (dSqr <= itemMaxSqr && cachedPickups.Count < 50 && IsPickupAllowed(it)) cachedPickups.Add(it);
                if (espUnderwaterOres && dSqr <= oreMaxSqr && IsOreObject(it)) cachedOres.Add(it);
            }
        }

        private static bool IsOreObject(PickupItem it)
        {
            string n = it.name;
            return ContainsIgnoreCase(n, "ore") || ContainsIgnoreCase(n, "clay") || ContainsIgnoreCase(n, "sand") ||
                   ContainsIgnoreCase(n, "seaweed") || ContainsIgnoreCase(n, "clam") || ContainsIgnoreCase(n, "dirt");
        }

        private void AddEspTag(Vector3 position, string label, Color col, float maxDist, float buildDist, float offset = 0f)
        {
            EspTag tag;
            if (espTagsCount < espTagPool.Count)
            {
                tag = espTagPool[espTagsCount];
            }
            else
            {
                tag = new EspTag();
                espTagPool.Add(tag);
            }
            espTagsCount++;

            tag.Position = position;
            tag.Label = label;
            tag.Col = col;
            tag.MaxDist = maxDist;
            tag.Offset = offset;
            tag.BuildDist = buildDist;
        }

        private void BuildEspTags(Vector3 pp)
        {
            espTags.Clear();
            espTagsCount = 0;

            if (espRaft && cachedRaft != null)
            {
                Vector3 rPos = cachedRaft.transform.position + Vector3.up * 2f;
                AddEspTag(rPos, "ПЛОТ", new Color(0.25f, 0.85f, 1f), 2000f, (rPos - pp).sqrMagnitude);
            }

            if (espPlayers)
            {
                for (int i = 0; i < cachedPlayers.Count; i++)
                {
                    var p = cachedPlayers[i];
                    if (p == null) continue;
                    string pName = "Игрок";
                    if (p.playerNameTextMesh != null && !string.IsNullOrEmpty(p.playerNameTextMesh.text)) pName = p.playerNameTextMesh.text;
                    Vector3 pPos = p.transform.position + Vector3.up * 1.8f;
                    AddEspTag(pPos, "ИГРОК " + pName, new Color(0.2f, 1f, 0.4f), 2000f, (pPos - pp).sqrMagnitude);
                }
            }

            if (espShark)
            {
                for (int i = 0; i < cachedSharksInfo.Count; i++)
                {
                    var info = cachedSharksInfo[i];
                    if (info == null || info.transform == null) continue;
                    string label = info.name + " " + info.health.ToString("F0") + "/" + info.maxHealth.ToString("F0") + " HP";
                    Vector3 sPos = info.transform.position + Vector3.up * 0.8f;
                    AddEspTag(sPos, label, TagSharkCol, 2000f, (sPos - pp).sqrMagnitude);
                }
            }

            if (espAllItems || espBarrels)
            {
                for (int i = 0; i < cachedPickups.Count; i++)
                {
                    var p = cachedPickups[i];
                    if (p == null) continue;
                    string n = p.name;
                    bool isBarrel = ContainsIgnoreCase(n, "barrel");
                    bool box = isBarrel || ContainsIgnoreCase(n, "crate") || ContainsIgnoreCase(n, "box");
                    Vector3 iPos = p.transform.position + Vector3.up * 0.3f;
                    float dSqr = (iPos - pp).sqrMagnitude;
                    if (box)
                    {
                        if (espBarrels) AddEspTag(iPos, isBarrel ? "БОЧКА" : "ЯЩИК", TagBarrelCol, espItemMaxDist, dSqr);
                    }
                    else if (espAllItems)
                    {
                        string rus = GetRussianItemName(p);
                        AddEspTag(iPos, rus, GetItemTagColor(rus), espItemMaxDist, dSqr);
                    }
                }
            }

            if (espUnderwaterOres)
            {
                for (int i = 0; i < cachedOres.Count; i++)
                {
                    var p = cachedOres[i];
                    if (p == null) continue;
                    string rus = GetRussianItemName(p);
                    Vector3 oPos = p.transform.position;
                    AddEspTag(oPos, "РУДА " + rus, GetOreTagColor(rus), espOreMaxDist, (oPos - pp).sqrMagnitude);
                }
            }

            if (espTreasures)
            {
                for (int i = 0; i < cachedTreasures.Count; i++)
                {
                    var tp = cachedTreasures[i];
                    if (tp == null) continue;
                    Vector3 tPos = tp.transform.position;
                    AddEspTag(tPos, "КЛАД", TagTreasureCol, 120f, (tPos - pp).sqrMagnitude);
                }
            }

            if (waypointEsp)
            {
                for (int i = 0; i < waypoints.Count; i++)
                {
                    Vector3 wPos = waypoints[i].position + Vector3.up * 2f;
                    AddEspTag(wPos, "ТОЧКА " + waypoints[i].name, TagWaypointCol, 3000f, (wPos - pp).sqrMagnitude);
                }
            }

            for (int i = 0; i < espTagsCount; i++)
            {
                espTags.Add(espTagPool[i]);
            }

            AppendV13EspTags(pp);

            espTags.Sort(TagDistComparison);
        }

        private EspTag MakeEspTag(Vector3 position, string label, Color col, float maxDist, Vector3 origin)
        {
            return new EspTag { Position = position, Label = label, Col = col, MaxDist = maxDist, BuildDist = Vector3.Distance(position, origin) };
        }

        private void DrawEspOverlay()
        {
            Camera cam = GetCachedCamera();
            if (cam == null || espTags.Count == 0) return;

            Vector3 camPos = cam.transform.position;
            Vector3 camFwd = cam.transform.forward;
            int drawn = 0;
            int count = espTags.Count;
            for (int i = 0; i < count && drawn < EspMaxLabels; i++)
            {
                EspTag tag = espTags[i];
                Vector3 toTag = tag.Position - camPos;
                if (Vector3.Dot(camFwd, toTag) <= 0.1f) continue;

                float maxD = tag.MaxDist;
                float sqrD = toTag.sqrMagnitude;
                if (sqrD > maxD * maxD) continue;
                float dist = Mathf.Sqrt(sqrD);

                Vector3 sp = cam.WorldToScreenPoint(tag.Position + Vector3.up * tag.Offset);
                if (sp.z <= 0.3f || sp.x < -120f || sp.x > Screen.width + 120f || sp.y < -60f || sp.y > Screen.height + 60f) continue;

                int dInt = (int)dist;
                string dStr = (dInt >= 0 && dInt <= 3000) ? DistStrings[dInt] : ("  " + dInt + "м");
                DrawEspLabel(sp, tag.Label + dStr, tag.Col);
                drawn++;
            }

            if (espTracers) DrawTracerLines(cam);
        }

        private void DrawEspLabel(Vector3 screenPos, string text, Color col)
        {
            float x = screenPos.x;
            float y = Screen.height - screenPos.y;
            sharedContent.text = text;
            Vector2 size = espBoxStyle.CalcSize(sharedContent);
            Rect rect = new Rect(x - size.x * 0.5f, y - size.y * 0.5f, size.x + 8f, size.y + 4f);

            Color oldCol = GUI.color;
            col.a = 1f;
            GUI.color = col;
            GUI.Box(rect, sharedContent, espBoxStyle);
            GUI.color = oldCol;
        }

        private void DrawWorldOverlays()
        {
            if (wireframeBuilding && wireBlocks.Count > 0) DrawBlockOutlines(wireBlocks, true);
            if (selectMode && selectedBlocks.Count > 0) DrawBlockOutlines(selectedBlocks, false);
            if (rulerOn || hasRulerA) DrawRulerWorld();
            if (sharkTrailOn && sharkTrail.Count > 0) { Camera tcam = GetCachedCamera(); if (tcam != null) DrawSharkTrail(tcam); }
            if (thermalVision) { Camera tcam = GetCachedCamera(); if (tcam != null) DrawThermalOverlay(tcam); }
        }

        private void DrawBlockOutlines(List<Block> list, bool byHealth)
        {
            Camera cam = GetCachedCamera();
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;
            Vector3 camFwd = cam.transform.forward;

            Color prev = GUI.color;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                var block = list[i];
                if (block == null) continue;

                Vector3 bPos = block.transform.position;
                if (Vector3.Dot(camFwd, (bPos - camPos)) <= 0.1f) continue;

                Vector3 sp = cam.WorldToScreenPoint(bPos + Vector3.up * 0.5f);
                if (sp.z <= 0.5f || sp.x < -60f || sp.x > Screen.width + 60f || sp.y < -60f || sp.y > Screen.height + 60f) continue;

                float size = Mathf.Clamp(900f / sp.z, 8f, 140f);
                Color c;
                if (byHealth)
                {
                    float hp = block.MaxHealth > 0 ? Mathf.Clamp01((float)block.Health / block.MaxHealth) : 1f;
                    c = Color.Lerp(BlockDamagedColor, BlockHealthyColor, hp);
                }
                else
                {
                    c = BlockSelectedColor;
                }

                Rect r = new Rect(sp.x - size * 0.5f, Screen.height - sp.y - size * 0.5f, size, size);
                GUI.color = new Color(c.r, c.g, c.b, byHealth ? 0.14f : 0.28f);
                GUI.DrawTexture(r, texWhiteDot);
                GUI.color = new Color(c.r, c.g, c.b, 0.9f);
                DrawOutlineRect(r, 1.5f);
            }
            GUI.color = prev;
        }

        private void DrawRulerWorld()
        {
            Camera cam = GetCachedCamera();
            if (cam == null || !hasRulerA) return;

            Color prev = GUI.color;
            Vector3 spA = cam.WorldToScreenPoint(rulerA);
            if (spA.z > 0.3f)
            {
                GUI.color = new Color(0.2f, 0.95f, 1f, 1f);
                GUI.DrawTexture(new Rect(spA.x - 4f, Screen.height - spA.y - 4f, 8f, 8f), texKnob);
            }

            if (hasRulerB)
            {
                Vector3 spB = cam.WorldToScreenPoint(rulerB);
                if (spB.z > 0.3f)
                {
                    GUI.color = new Color(1f, 0.85f, 0.2f, 1f);
                    GUI.DrawTexture(new Rect(spB.x - 4f, Screen.height - spB.y - 4f, 8f, 8f), texKnob);
                }

                GUI.color = new Color(0.2f, 0.95f, 1f, 0.9f);
                for (int i = 0; i <= 24; i++)
                {
                    Vector3 p = Vector3.Lerp(rulerA, rulerB, i / 24f);
                    Vector3 sp = cam.WorldToScreenPoint(p);
                    if (sp.z <= 0.3f) continue;
                    GUI.DrawTexture(new Rect(sp.x - 1.5f, Screen.height - sp.y - 1.5f, 3f, 3f), texWhiteDot);
                }

                Vector3 mid = cam.WorldToScreenPoint((rulerA + rulerB) * 0.5f);
                if (mid.z > 0.3f)
                {
                    DrawEspLabel(mid, Vector3.Distance(rulerA, rulerB).ToString("F2") + " м", new Color(0.2f, 0.95f, 1f));
                }
            }
            GUI.color = prev;
        }

        private void DrawOutlineRect(Rect r, float t)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), texWhiteDot);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), texWhiteDot);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), texWhiteDot);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), texWhiteDot);
        }

        private string GetRussianItemName(PickupItem p)
        {
            if (p == null) return "Предмет";
            int id = p.GetInstanceID();
            string cached;
            if (espNameCache.TryGetValue(id, out cached)) return cached;

            string result = ComputeRussianItemName(p);
            if (espNameCache.Count > 800) espNameCache.Clear();
            espNameCache[id] = result;
            return result;
        }

        private static string ComputeRussianItemName(PickupItem p)
        {
            try
            {
                if (p.itemInstance != null && p.itemInstance.baseItem != null && p.itemInstance.baseItem.settings_Inventory != null)
                {
                    string dn = p.itemInstance.baseItem.settings_Inventory.DisplayName;
                    if (!string.IsNullOrEmpty(dn)) return dn;
                }
            }
            catch {}

            string raw = p.name;
            if (ContainsIgnoreCase(raw, "plank")) return "Доска";
            if (ContainsIgnoreCase(raw, "plastic")) return "Пластик";
            if (ContainsIgnoreCase(raw, "scrap")) return "Хлам";
            if (ContainsIgnoreCase(raw, "thatch") || ContainsIgnoreCase(raw, "leaf")) return "Листья";
            if (ContainsIgnoreCase(raw, "rope")) return "Веревка";
            if (ContainsIgnoreCase(raw, "stone") || ContainsIgnoreCase(raw, "rock")) return "Камень";
            if (ContainsIgnoreCase(raw, "clay")) return "Глина";
            if (ContainsIgnoreCase(raw, "sand")) return "Песок";
            if (ContainsIgnoreCase(raw, "ore_metal") || ContainsIgnoreCase(raw, "metalore")) return "Руда металла";
            if (ContainsIgnoreCase(raw, "ingot_metal") || ContainsIgnoreCase(raw, "metalingot")) return "Слиток металла";
            if (ContainsIgnoreCase(raw, "ore_copper") || ContainsIgnoreCase(raw, "copperore")) return "Руда меди";
            if (ContainsIgnoreCase(raw, "ingot_copper") || ContainsIgnoreCase(raw, "copperingot")) return "Слиток меди";
            if (ContainsIgnoreCase(raw, "titanium")) return "Титан";
            if (ContainsIgnoreCase(raw, "seaweed")) return "Водоросли";
            if (ContainsIgnoreCase(raw, "vinegoo")) return "Клей";
            if (ContainsIgnoreCase(raw, "feather")) return "Перо";
            if (ContainsIgnoreCase(raw, "nail")) return "Гвоздь";
            if (ContainsIgnoreCase(raw, "bolt")) return "Болт";
            if (ContainsIgnoreCase(raw, "hinge")) return "Петля";
            if (ContainsIgnoreCase(raw, "dirt")) return "Земля";
            if (ContainsIgnoreCase(raw, "clam")) return "Моллюск";
            if (ContainsIgnoreCase(raw, "potato")) return "Картофель";
            if (ContainsIgnoreCase(raw, "beet")) return "Свекла";
            if (ContainsIgnoreCase(raw, "meat")) return "Мясо";
            if (ContainsIgnoreCase(raw, "fish")) return "Рыба";
            if (ContainsIgnoreCase(raw, "mango")) return "Манго";
            if (ContainsIgnoreCase(raw, "coconut")) return "Кокос";
            if (ContainsIgnoreCase(raw, "watermelon")) return "Арбуз";
            if (ContainsIgnoreCase(raw, "pineapple")) return "Ананас";
            if (ContainsIgnoreCase(raw, "mushroom")) return "Гриб";
            if (ContainsIgnoreCase(raw, "battery")) return "Батарейка";
            if (ContainsIgnoreCase(raw, "barrel")) return "Бочка";
            if (ContainsIgnoreCase(raw, "crate") || ContainsIgnoreCase(raw, "box")) return "Ящик";
            return p.name.Replace("(Clone)", "").Trim();
        }

        private static Color GetItemTagColor(string rus)
        {
            if (ContainsIgnoreCase(rus, "доска") || ContainsIgnoreCase(rus, "дерево")) return ColItemWood;
            if (ContainsIgnoreCase(rus, "пластик")) return ColItemPlastic;
            if (ContainsIgnoreCase(rus, "хлам") || ContainsIgnoreCase(rus, "металл")) return ColItemScrap;
            if (ContainsIgnoreCase(rus, "титан")) return ColItemTitanium;
            return ColItemDefault;
        }

        private static Color GetOreTagColor(string rus)
        {
            if (ContainsIgnoreCase(rus, "титан")) return ColOreTitanium;
            if (ContainsIgnoreCase(rus, "металл")) return ColOreMetal;
            if (ContainsIgnoreCase(rus, "мед")) return ColOreCopper;
            if (ContainsIgnoreCase(rus, "водоросл")) return ColOreSeaweed;
            if (ContainsIgnoreCase(rus, "глин") || ContainsIgnoreCase(rus, "песок")) return ColOreSand;
            if (ContainsIgnoreCase(rus, "моллюск")) return ColOreClam;
            return ColOreDefault;
        }
    }
}
