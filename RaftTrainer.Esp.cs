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
        private readonly List<PickupItem> espPickupScratch = new List<PickupItem>();
        private readonly List<SharkESPInfo> espNameSharks = new List<SharkESPInfo>();
        private readonly Dictionary<int, string> espNameCache = new Dictionary<int, string>();
        private Vector3 espSortOrigin = Vector3.zero;
        private float nextEspPlayersTime = 0f;
        private float nextEspSharkTime = 0f;
        private float nextEspNameSharkTime = 0f;
        private float nextEspPickupTime = 0f;
        private float nextEspTreasureTime = 0f;
        private float nextEspLandmarkTime = 0f;
        private const int EspMaxLabels = 70;

        private int CompareItemsByDistance(PickupItem a, PickupItem b)
        {
            float da = (a.transform.position - espSortOrigin).sqrMagnitude;
            float db = (b.transform.position - espSortOrigin).sqrMagnitude;
            return da.CompareTo(db);
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

                if (now >= nextEspSharkTime)
                {
                    nextEspSharkTime = now + 0.7f;
                    RefreshSharkCache(now);
                }

                if (now >= nextEspPickupTime)
                {
                    nextEspPickupTime = now + 0.5f;
                    RefreshPickupCache(pp);
                }

                if (now >= nextEspTreasureTime)
                {
                    nextEspTreasureTime = now + 2f;
                    cachedTreasures.Clear();
                    if (espTreasures)
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

        private void RefreshSharkCache(float now)
        {
            cachedSharksInfo.Clear();
            if (!espShark) return;

            var seen = new HashSet<Transform>();
            var sharks = UnityEngine.Object.FindObjectsOfType<Shark>();
            for (int i = 0; i < sharks.Length; i++)
            {
                var s = sharks[i];
                if (s == null || s.state == SharkState.Dead || s.state == SharkState.DeadSink) continue;
                float cur = 100f;
                float max = 100f;
                if (s.entityComponent != null && s.entityComponent.stat_health != null)
                {
                    if (s.entityComponent.stat_health.IsDead()) continue;
                    cur = s.entityComponent.stat_health.Value;
                    max = s.entityComponent.stat_health.Max;
                }
                cachedSharksInfo.Add(new SharkESPInfo { transform = s.transform, name = "Акула", health = cur, maxHealth = max });
                seen.Add(s.transform);
            }

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
                cachedSharksInfo.Add(new SharkESPInfo { transform = a.transform, name = "Акула", health = cur, maxHealth = max });
                seen.Add(a.transform);
            }

            if (now >= nextEspNameSharkTime)
            {
                nextEspNameSharkTime = now + 3f;
                espNameSharks.Clear();
                var entities = UnityEngine.Object.FindObjectsOfType<Network_Entity>();
                for (int i = 0; i < entities.Length; i++)
                {
                    var ent = entities[i];
                    if (ent == null) continue;
                    string n = ent.name.ToLower();
                    if (!n.Contains("shark")) continue;
                    if (ent.stat_health != null && ent.stat_health.IsDead()) continue;
                    float cur = ent.stat_health != null ? ent.stat_health.Value : 100f;
                    float max = ent.stat_health != null ? ent.stat_health.Max : 100f;
                    string sName = (n.Contains("varuna") || n.Contains("boss")) ? "Босс-акула" : "Акула";
                    espNameSharks.Add(new SharkESPInfo { transform = ent.transform, name = sName, health = cur, maxHealth = max });
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
            var all = UnityEngine.Object.FindObjectsOfType<PickupItem>();
            espPickupScratch.Clear();
            for (int i = 0; i < all.Length; i++)
            {
                var it = all[i];
                if (it != null && Vector3.Distance(it.transform.position, pp) <= radius) espPickupScratch.Add(it);
            }

            espSortOrigin = pp;
            espPickupScratch.Sort(CompareItemsByDistance);

            for (int i = 0; i < espPickupScratch.Count; i++)
            {
                var it = espPickupScratch[i];
                float d = Vector3.Distance(it.transform.position, pp);
                if (d <= espItemMaxDist && cachedPickups.Count < 50 && IsPickupAllowed(it)) cachedPickups.Add(it);
                if (espUnderwaterOres && d <= espOreMaxDist && IsOreObject(it)) cachedOres.Add(it);
            }
        }

        private static bool IsOreObject(PickupItem it)
        {
            string n = it.name.ToLower();
            return n.Contains("ore") || n.Contains("clay") || n.Contains("sand") || n.Contains("seaweed") || n.Contains("clam") || n.Contains("dirt");
        }

        private void BuildEspTags(Vector3 pp)
        {
            espTags.Clear();

            if (espRaft && cachedRaft != null)
            {
                espTags.Add(MakeEspTag(cachedRaft.transform.position + Vector3.up * 2f, "ПЛОТ", new Color(0.25f, 0.85f, 1f), 2000f, pp));
            }

            if (espPlayers)
            {
                for (int i = 0; i < cachedPlayers.Count; i++)
                {
                    var p = cachedPlayers[i];
                    if (p == null) continue;
                    string pName = "Игрок";
                    if (p.playerNameTextMesh != null && !string.IsNullOrEmpty(p.playerNameTextMesh.text)) pName = p.playerNameTextMesh.text;
                    espTags.Add(MakeEspTag(p.transform.position + Vector3.up * 1.8f, "ИГРОК " + pName, new Color(0.2f, 1f, 0.4f), 2000f, pp));
                }
            }

            if (espShark)
            {
                for (int i = 0; i < cachedSharksInfo.Count; i++)
                {
                    var info = cachedSharksInfo[i];
                    if (info == null || info.transform == null) continue;
                    string label = info.name + " " + info.health.ToString("F0") + "/" + info.maxHealth.ToString("F0") + " HP";
                    espTags.Add(MakeEspTag(info.transform.position + Vector3.up * 0.8f, label, new Color(1f, 0.25f, 0.35f), 2000f, pp));
                }
            }

            if (espAllItems || espBarrels)
            {
                for (int i = 0; i < cachedPickups.Count; i++)
                {
                    var p = cachedPickups[i];
                    if (p == null) continue;
                    string n = p.name.ToLower();
                    bool box = n.Contains("barrel") || n.Contains("crate") || n.Contains("box");
                    if (box)
                    {
                        if (espBarrels) espTags.Add(MakeEspTag(p.transform.position + Vector3.up * 0.3f, n.Contains("barrel") ? "БОЧКА" : "ЯЩИК", new Color(1f, 0.85f, 0.2f), espItemMaxDist, pp));
                    }
                    else if (espAllItems)
                    {
                        string rus = GetRussianItemName(p);
                        espTags.Add(MakeEspTag(p.transform.position + Vector3.up * 0.3f, rus, GetItemTagColor(rus), espItemMaxDist, pp));
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
                    espTags.Add(MakeEspTag(p.transform.position, "РУДА " + rus, GetOreTagColor(rus), espOreMaxDist, pp));
                }
            }

            if (espTreasures)
            {
                for (int i = 0; i < cachedTreasures.Count; i++)
                {
                    var tp = cachedTreasures[i];
                    if (tp == null) continue;
                    espTags.Add(MakeEspTag(tp.transform.position, "КЛАД", new Color(1f, 0.85f, 0.1f), 120f, pp));
                }
            }

            if (waypointEsp)
            {
                for (int i = 0; i < waypoints.Count; i++)
                {
                    espTags.Add(MakeEspTag(waypoints[i].position + Vector3.up * 2f, "ТОЧКА " + waypoints[i].name, new Color(0.2f, 0.9f, 1f), 3000f, pp));
                }
            }

            AppendV13EspTags(pp);

            espTags.Sort(CompareTagsByDistance);
        }

        private EspTag MakeEspTag(Vector3 position, string label, Color col, float maxDist, Vector3 origin)
        {
            return new EspTag { Position = position, Label = label, Col = col, MaxDist = maxDist, BuildDist = Vector3.Distance(position, origin) };
        }

        private void DrawEspOverlay()
        {
            Camera cam = Camera.main;
            if (cam == null || espTags.Count == 0) return;

            Vector3 camPos = cam.transform.position;
            int drawn = 0;
            for (int i = 0; i < espTags.Count && drawn < EspMaxLabels; i++)
            {
                EspTag tag = espTags[i];
                float dist = Vector3.Distance(camPos, tag.Position);
                if (dist > tag.MaxDist) continue;

                Vector3 sp = cam.WorldToScreenPoint(tag.Position + Vector3.up * tag.Offset);
                if (sp.z <= 0.3f || sp.x < -120f || sp.x > Screen.width + 120f || sp.y < -60f || sp.y > Screen.height + 60f) continue;

                DrawEspLabel(sp, tag.Label + "  " + dist.ToString("F0") + "м", tag.Col);
                drawn++;
            }

            if (espTracers) DrawTracerLines(cam);
        }

        private void DrawEspLabel(Vector3 screenPos, string text, Color col)
        {
            float x = screenPos.x;
            float y = Screen.height - screenPos.y;
            Vector2 size = espBoxStyle.CalcSize(new GUIContent(text));
            Rect rect = new Rect(x - size.x * 0.5f, y - size.y * 0.5f, size.x + 8f, size.y + 4f);

            Color oldCol = GUI.color;
            GUI.color = new Color(col.r, col.g, col.b, 1f);
            GUI.Box(rect, text, espBoxStyle);
            GUI.color = oldCol;
        }

        private void DrawWorldOverlays()
        {
            if (wireframeBuilding && wireBlocks.Count > 0) DrawBlockOutlines(wireBlocks, true);
            if (selectMode && selectedBlocks.Count > 0) DrawBlockOutlines(selectedBlocks, false);
            if (rulerOn || hasRulerA) DrawRulerWorld();
            if (sharkTrailOn && sharkTrail.Count > 0) { Camera tcam = Camera.main; if (tcam != null) DrawSharkTrail(tcam); }
            if (thermalVision) { Camera tcam = Camera.main; if (tcam != null) DrawThermalOverlay(tcam); }
        }

        private void DrawBlockOutlines(List<Block> list, bool byHealth)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Color prev = GUI.color;
            for (int i = 0; i < list.Count; i++)
            {
                var block = list[i];
                if (block == null) continue;

                Vector3 sp = cam.WorldToScreenPoint(block.transform.position + Vector3.up * 0.5f);
                if (sp.z <= 0.5f || sp.x < -60f || sp.x > Screen.width + 60f || sp.y < -60f || sp.y > Screen.height + 60f) continue;

                float size = Mathf.Clamp(900f / sp.z, 8f, 140f);
                Color c;
                if (byHealth)
                {
                    float hp = block.MaxHealth > 0 ? Mathf.Clamp01((float)block.Health / block.MaxHealth) : 1f;
                    c = Color.Lerp(new Color(1f, 0.25f, 0.30f), new Color(0.20f, 1f, 0.60f), hp);
                }
                else
                {
                    c = new Color(1f, 0.85f, 0.20f);
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
            Camera cam = Camera.main;
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

            string raw = p.name.Replace("(Clone)", "").Trim().ToLower();
            if (raw.Contains("plank")) return "Доска";
            if (raw.Contains("plastic")) return "Пластик";
            if (raw.Contains("scrap")) return "Хлам";
            if (raw.Contains("thatch") || raw.Contains("leaf")) return "Листья";
            if (raw.Contains("rope")) return "Веревка";
            if (raw.Contains("stone") || raw.Contains("rock")) return "Камень";
            if (raw.Contains("clay")) return "Глина";
            if (raw.Contains("sand")) return "Песок";
            if (raw.Contains("ore_metal") || raw.Contains("metalore")) return "Руда металла";
            if (raw.Contains("ingot_metal") || raw.Contains("metalingot")) return "Слиток металла";
            if (raw.Contains("ore_copper") || raw.Contains("copperore")) return "Руда меди";
            if (raw.Contains("ingot_copper") || raw.Contains("copperingot")) return "Слиток меди";
            if (raw.Contains("titanium")) return "Титан";
            if (raw.Contains("seaweed")) return "Водоросли";
            if (raw.Contains("vinegoo")) return "Клей";
            if (raw.Contains("feather")) return "Перо";
            if (raw.Contains("nail")) return "Гвоздь";
            if (raw.Contains("bolt")) return "Болт";
            if (raw.Contains("hinge")) return "Петля";
            if (raw.Contains("dirt")) return "Земля";
            if (raw.Contains("clam")) return "Моллюск";
            if (raw.Contains("potato")) return "Картофель";
            if (raw.Contains("beet")) return "Свекла";
            if (raw.Contains("meat")) return "Мясо";
            if (raw.Contains("fish")) return "Рыба";
            if (raw.Contains("mango")) return "Манго";
            if (raw.Contains("coconut")) return "Кокос";
            if (raw.Contains("watermelon")) return "Арбуз";
            if (raw.Contains("pineapple")) return "Ананас";
            if (raw.Contains("mushroom")) return "Гриб";
            if (raw.Contains("battery")) return "Батарейка";
            if (raw.Contains("barrel")) return "Бочка";
            if (raw.Contains("crate") || raw.Contains("box")) return "Ящик";
            return p.name.Replace("(Clone)", "").Trim();
        }

        private static Color GetItemTagColor(string rus)
        {
            string low = rus.ToLower();
            if (low.Contains("доска") || low.Contains("дерево")) return new Color(0.9f, 0.7f, 0.4f);
            if (low.Contains("пластик")) return new Color(0.4f, 0.85f, 1f);
            if (low.Contains("хлам") || low.Contains("металл")) return new Color(0.75f, 0.8f, 0.9f);
            if (low.Contains("титан")) return new Color(0.3f, 1f, 0.7f);
            return new Color(0.85f, 0.85f, 0.95f);
        }

        private static Color GetOreTagColor(string rus)
        {
            string low = rus.ToLower();
            if (low.Contains("титан")) return new Color(0.3f, 1f, 0.6f);
            if (low.Contains("металл")) return new Color(0.4f, 0.8f, 1f);
            if (low.Contains("мед")) return new Color(1f, 0.6f, 0.2f);
            if (low.Contains("водоросл")) return new Color(0.4f, 1f, 0.3f);
            if (low.Contains("глин") || low.Contains("песок")) return new Color(0.9f, 0.9f, 0.4f);
            if (low.Contains("моллюск")) return new Color(0.9f, 0.4f, 1f);
            return new Color(0.2f, 0.9f, 1f);
        }
    }
}
