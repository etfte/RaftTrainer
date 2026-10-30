using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UltimateWater;

namespace RaftTrainer
{
    public class Loader
    {
        private static GameObject _instance;

        public static void Init()
        {
            if (_instance != null) return;
            try
            {
                _instance = new GameObject("__RaftTrainer__");
                _instance.AddComponent<TrainerGUI>();
                UnityEngine.Object.DontDestroyOnLoad(_instance);
                Debug.Log("[RaftTrainer] Initialized v14.0: world map, smart reminders, floor copier, thermal vision and 15 new tools");
            }
            catch (Exception ex)
            {
                Debug.LogError("[RaftTrainer] Failed to initialize: " + ex);
            }
        }
    }

    public class SharkESPInfo
    {
        public Transform transform;
        public string name;
        public float health;
        public float maxHealth;
    }

    public class TeleportWaypoint
    {
        public string name;
        public Vector3 position;
    }

    public partial class TrainerGUI : MonoBehaviour
    {
        public static bool ContainsIgnoreCase(string source, string pattern)
        {
            if (source == null || pattern == null) return false;
            return source.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPlankItem(string n)
        {
            return ContainsIgnoreCase(n, "plank");
        }

        // Reflection Cache for High Performance
        private static readonly FieldInfo fMeleeDamage = typeof(MeleeWeapon).GetField("damage", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fMeleeInv = typeof(MeleeWeapon).GetField("goThroughInvurnability", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fArrowDamage = typeof(Arrow).GetField("damage", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fFishOnHook = typeof(Bobber).GetField("fishIsOnHook", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fFuelCount = typeof(Fuel).GetField("fuelCount", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fMaxFuel = typeof(Fuel).GetField("maxFuel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fCookTimer = typeof(CookingTable).GetField("cookTimer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fSharkCanAttack = typeof(Shark).GetField("canAttack", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fSharkBitePlayer = typeof(Shark).GetField("bitePlayerRange", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fSharkBiteRaft = typeof(Shark).GetField("biteRaftRange", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fSharkTargetToAttack = typeof(Shark).GetField("targetToAttack", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fKeypadCorrectString = typeof(Keypad).GetField("correctString", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fDetectorSearchRadius = typeof(MetalDetector).GetField("searchRadius", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fChaseTarget = typeof(AI_State_Chase).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fPlantFullyGrown = typeof(Plant).GetField("fullyGrown", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fPlantGrowTimer = typeof(Plant).GetField("growTimer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fThrowDelay = typeof(ThrowableComponent).GetField("throwDelay", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fThrowCharge = typeof(ThrowableComponent).GetField("chargeMeter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fChargeSpeed = typeof(ChargeMeter).GetField("chargeSpeed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fHostEntities = typeof(Network_Host).GetField("hostEntities", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fCookingFuel = typeof(Block_CookingStand).GetField("fuel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fWaterRoughness = typeof(WaterRoughnessManager).GetField("roughness", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo fWaterMaxRoughness = typeof(WaterRoughnessManager).GetField("maxRoughness", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo mNetGunReload = typeof(ThrowableComponent_NetGun).GetMethod("SetIsReloaded", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo mNetGunCooldown = typeof(ThrowableComponent_NetGun).GetMethod("ResetShotCooldown", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo mWeighAnchor = typeof(Anchor_Stationary).GetMethod("WeighAnchor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo mDropAnchor = typeof(Anchor_Stationary).GetMethod("DropAnchor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        // Menu State & Scaling (CLOSED by default on launch!)
        public bool showMenu = false;
        private bool lastMenuState = false;
        public float uiScale = 1.0f;
        private Vector2 menuPos = new Vector2(-1f, -1f);
        private bool isDragging = false;
        private Vector2 dragMouseOffset = Vector2.zero;
        private float lastScreenWidth = 0f;
        private float lastScreenHeight = 0f;

        private string globalSearch = "";
        private string statusMessage = "Raft Trainer v14.0 готов. Открыть/закрыть меню: F4 или Insert.";

        private Vector2 spawnerListScroll = Vector2.zero;

        // COMBAT & AIMBOT
        public bool oneHitKill = true;
        public bool pacifyShark = false;
        public bool aimbotAnimals = false;
        public bool aimbotItems = false;
        public bool aimbotAlwaysOn = false;
        public KeyCode aimbotKey = KeyCode.X;
        public float aimbotFOV = 90f;
        public float aimbotRange = 55f;
        public float aimSmoothness = 0.0f;
        public bool fastChop = false;
        public bool fastHook = false;
        public bool noArrowDrop = false;
        public bool infiniteAmmo = false;

        // MOVEMENT & RAFT
        public bool flyMode = false;
        public float flySpeed = 18f;
        public bool turboRaft = false;
        public float turboRaftMultiplier = 3.0f;
        public bool superSpeed = false;
        public float speedMultiplier = 2.5f;
        public bool superJump = false;
        public float jumpMultiplier = 2.0f;
        public bool noFallDamage = true;

        // VISUALS & RUSSIAN ESP
        public bool espMaster = true;
        public bool espPlayers = true;
        public bool espAllItems = true;
        public float espItemMaxDist = 50f;
        public bool espShark = true;
        public bool espRaft = true;
        public bool espBarrels = true;
        public bool nightVision = false;
        public bool clearWater = false;
        private Light nightVisionLight;

        // ESP Cached Data
        private float nextEspCacheTime = 0f;
        private readonly List<PickupItem> cachedPickups = new List<PickupItem>();
        private readonly List<SharkESPInfo> cachedSharksInfo = new List<SharkESPInfo>();
        private readonly List<Shark> cachedSharks = new List<Shark>();
        private readonly List<Network_Player> cachedPlayers = new List<Network_Player>();
        private readonly List<Network_Entity> cachedEntities = new List<Network_Entity>();
        private Raft cachedRaft = null;
        private Rigidbody cachedRaftRb = null;

        // Camera Caching
        private static Camera cachedCamera = null;
        private static float nextCamCheckTime = 0f;

        public static Camera GetCachedCamera()
        {
            float now = Time.time;
            if (cachedCamera == null || !cachedCamera.gameObject.activeInHierarchy || now > nextCamCheckTime)
            {
                nextCamCheckTime = now + 1.0f;
                cachedCamera = Camera.main;
            }
            return cachedCamera;
        }

        // High-Performance Execution Throttle Timers
        private float nextArrowThrottleTime = 0f;
        private float nextSharkAiThrottleTime = 0f;
        private float nextToolsThrottleTime = 0f;
        private float nextMachineryThrottleTime = 0f;
        private float nextInventoryThrottleTime = 0f;
        private bool lastClearWaterState = false;
        private float nextClearWaterScanTime = 0f;
        private float nextCalmSeaTime = 0f;
        private bool lastCheatGodMode = false;
        private bool lastFreeCraftState = false;
        private float nextTailwindTime = 0f;
        private MeshPath_Zipline[] cachedZiplinePaths = null;
        private float nextZiplineScanTime = 0f;
        private Block_CookingStand[] cachedCookingStands = null;
        private Storage_Small[] cachedChests = null;
        private float nextMachineryScanTime = 0f;
        private float nextAutoCropHarvestTime = 0f;
        private AI_NetworkBehaviour_Domestic_Resource[] cachedDomesticResources = null;
        private float nextDomesticResourcesScanTime = 0f;
        private AI_NetworkBehaviour_Domestic[] cachedDomesticAnimals = null;
        private float nextDomesticAnimalsScanTime = 0f;
        private Plant[] cachedPlants = null;
        private float nextPlantsScanTime = 0f;
        private Placeable_BiofuelExtractor[] cachedBiofuelExtractors = null;
        private float nextBiofuelScanTime = 0f;
        private BeeHive[] cachedBeeHives = null;
        private float nextBeeHiveScanTime = 0f;
        private Cropplot_Grass[] cachedGrassPlots = null;
        private float nextGrassScanTime = 0f;
        private Cropplot[] cachedCropplots = null;
        private float nextCropplotsScanTime = 0f;
        private static BlockCreator cachedBlockCreator = null;
        private static RaftBounds cachedRaftBounds = null;
        private static PlantManager cachedPlantManager = null;
        private float nextStableScanTime = 0f;
        private CookingSlot[] cachedCookingSlots = null;
        private CookingTable[] cachedCookingTables = null;
        private Tank[] cachedTanks = null;
        private Battery[] cachedBatteries = null;
        private Fuel[] cachedFuels = null;
        private float nextFuelsScanTime = 0f;
        private float nextMachineryScanFullTime = 0f;
        private BatteryCharger[] cachedChargers = null;
        private float nextChargersScanTime = 0f;
        private Throwable_Object[] cachedThrowables = null;
        private float nextThrowablesScanTime = 0f;
        private Seagull[] cachedSeagulls = null;
        private float nextSeagullsScanTime = 0f;
        private ThrowableComponent_NetGun[] cachedNetGuns = null;
        private float nextNetGunsScanTime = 0f;
        private ThrowableComponent_Bow[] cachedBows = null;
        private float nextBowsScanTime = 0f;
        private Sprinkler[] cachedSprinklers = null;
        private float nextSprinklersScanTime = 0f;
        private MetalDetector[] cachedDetectors = null;
        private float nextDetectorsScanTime = 0f;
        private LightNightNDayToggler[] cachedLights = null;
        private float nextLightsScanTime = 0f;

        // PLAYER & CRAFTING
        public bool godMode = true;
        public bool infiniteNeeds = true;
        public bool infiniteDurability = true;
        public bool freeCraft = false;
        public bool freeBuild = false;

        // AUTOMATION, WORLD & GATHERING
        public bool instantHarvest = true;
        public bool instantSmeltCook = false;
        public bool infiniteBattery = false;
        public bool infiniteFuel = false;
        public bool instantFishing = false;
        public bool instantTame = false;
        public bool itemMagnet = false;
        public float magnetRadius = 40f;
        public bool autoLootPickups = false;
        public float autoLootRadius = 35f;
        public bool antiSeagull = true;
        public bool autoWaterCrops = false;
        public bool freezeRaft = false;
        private float nextAutoLootTime = 0f;
        private float nextAntiSeagullTime = 0f;
        private float nextAutoWaterTime = 0f;

        // V8 ADDITIONS
        public bool autoFishing = false;
        public bool instantDomesticProduction = false;
        public bool alwaysTailwind = false;
        public float windSpeedMultiplier = 2.0f;
        public bool infinitePaint = false;
        public bool superDiving = false;
        public float swimSpeedMultiplier = 3.5f;
        public bool killHostilesAura = false;
        public float hostileAuraRadius = 22f;

        private float nextAutoFishTime = 0f;
        private float nextDomesticProdTime = 0f;
        private float nextHostileAuraTime = 0f;
        private static readonly MethodInfo mPullItems = typeof(FishingRod).GetMethod("PullItemsFromSea", BindingFlags.NonPublic | BindingFlags.Instance);

        // V9 ADDITIONS
        public bool noBuildRestrictions = false;
        public bool freeZiplineAir = false;
        public float ziplineAirSpeed = 22f;
        public float weaponDamageMultiplier = 1.0f;
        public bool espUnderwaterOres = true;
        public float espOreMaxDist = 80f;
        private readonly List<PickupItem> cachedOres = new List<PickupItem>();
                private float nextWeaponDmgTime = 0f;

        // V10 TITAN AUTOMATION ADDITIONS (20 NEW FEATURES)
        public bool triggerbot = false;
        private float lastTriggerbotTime = 0f;
        public bool multishotBow = false;
        private readonly HashSet<int> processedArrows = new HashSet<int>();
        public bool ghostMode = false;
        public bool infiniteBait = false;
        public bool instantDemolish = false;
        public bool infinitePipeWater = false;
        public bool indestructibleRaft = false;
        public bool zoomEnabled = false;
        public float zoomMultiplier = 4.0f;
        private float originalFOV = 60f;
        private bool isZooming = false;
        public bool subseaFloodlight = false;
        private GameObject floodlightObj = null;
        private Light floodlightComp = null;
        public bool alwaysActiveDetector = false;
        public bool instantTreasureDig = false;
        public bool autoCropHarvest = false;
        public bool instantBiofuel = false;
        public bool hyperBeehives = false;
        public bool noFuelLanterns = false;
        public bool perpetualGrass = false;
        public bool espTreasures = true;
        private readonly List<TreasurePoint> cachedTreasures = new List<TreasurePoint>();
        private float nextV10ThrottleTime = 0f;
        private Electric_Purifier[] cachedPurifiers = null;
        private Block_CookingStand_Purifier[] cachedStandPurifiers = null;
        private float nextPurifierScanTime = 0f;
        private Block[] cachedAllBlocks = null;
        private float nextAllBlocksScanTime = 0f;

        // V11 AUTO / LOGISTICS / CAMERA / PAINT
        public bool autoAnchorOnArrival = false;
        public float autoAnchorDistance = 25f;
        private bool autoAnchorArmed = true;
        private float nextAutoAnchorTime = 0f;

        public bool sharkRiding = false;
        public float sharkRideSpeed = 14f;
        private Shark ridingShark = null;
        private float nextSharkSearchTime = 0f;

        public bool perfectBowDraw = false;
        private readonly Dictionary<int, float> savedBowDelay = new Dictionary<int, float>();
        private readonly Dictionary<int, float> savedBowCharge = new Dictionary<int, float>();
        private float nextBowScanTime = 0f;

        public bool infiniteHarpoonNet = false;
        private float nextNetGunTime = 0f;
        private Item_Base netCanisterItem = null;

        public bool flipperGlider = false;
        public float flipperGlideSpeed = 14f;
        public bool infiniteOxygen = false;

        public bool blueprintCamera = false;
        private bool blueprintCameraOn = false;
        private FlightCamera flightCamera = null;

        public bool autoZipline = false;
        private float nextAutoZiplineTime = 0f;
        private float ziplineReattachTime = 0f;

        private const int MaxWaypoints = 10;
        private readonly List<TeleportWaypoint> waypoints = new List<TeleportWaypoint>();
        private string newWaypointName = "";
        public bool waypointEsp = true;

        public bool autoCooker = false;
        public bool autoSmelter = false;
        public float autoLogisticsRadius = 30f;
        private float nextAutoCookTime = 0f;

        public bool autoShearer = false;
        private float nextAutoShearTime = 0f;

        public bool raftStabilizer = false;
        public float stabilizerStrength = 1.0f;

        public bool calmSea = false;
        private float savedRoughness = -1f;
        private float savedMaxRoughness = -1f;

        public bool instantDesalinator = false;
        private float nextDesalTime = 0f;

        public bool wireframeBuilding = false;
        private float nextWireScanTime = 0f;
        private readonly List<Block> wireBlocks = new List<Block>();

        public bool soundBooster = false;
        public float soundBoostMultiplier = 3.0f;
        private float nextSoundScanTime = 0f;
        private readonly List<AudioSource> boostedSources = new List<AudioSource>();
        private readonly List<float> boostedRanges = new List<float>();
        private readonly List<AudioLowPassFilter> mutedLowPass = new List<AudioLowPassFilter>();

        public bool animalBrush = false;
        public int brushAnimalIndex = 0;
        private readonly AI_NetworkBehaviourType[] brushAnimalTypes = new AI_NetworkBehaviourType[] { AI_NetworkBehaviourType.Llama, AI_NetworkBehaviourType.Goat, AI_NetworkBehaviourType.PolarBear, AI_NetworkBehaviourType.StoneBird };
        private readonly string[] brushAnimalNames = new string[] { "Лама", "Коза", "Медведь", "Визгун" };

        public int junkRainAmount = 20;

        public float paintR = 255f;
        public float paintG = 140f;
        public float paintB = 0f;
        private string paintHex = "#FF8C00";

        // SPAWNER & ITEM CATALOG
        private List<Item_Base> allCatalogItems = null;
        private List<Item_Base> filteredCatalogItems = null;
        private string spawnerSearch = "";
        private int selectedCategory = 0;
        private readonly string[] categoryNames = new string[] { "Все", "Оружие", "Ресурсы", "Еда", "Другое" };
        private Item_Base selectedItem = null;
        private string customAmountStr = "20";

        private bool stylesInitialized = false;


        private void Start()
        {
            try
            {
                Cheat.UseCheats = true;
                Cheat.UseGodMode = godMode;
            }
            catch {}
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public static void DrawSprite(Rect screenRect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            try
            {
                Rect tr = sprite.rect;
                Texture2D tex = sprite.texture;
                float tw = (float)tex.width;
                float th = (float)tex.height;
                if (tw <= 0f || th <= 0f) return;

                Rect texCoords = new Rect(
                    tr.x / tw,
                    tr.y / th,
                    tr.width / tw,
                    tr.height / th
                );
                GUI.DrawTextureWithTexCoords(screenRect, tex, texCoords);
            }
            catch {}
        }

        private void Update()
        {
            // Toggle Menu (F4 or Insert only)
            if (Input.GetKeyDown(KeyCode.Insert) || Input.GetKeyDown(KeyCode.F4))
            {
                showMenu = !showMenu;
            }

            // Cursor Lock Management
            if (showMenu != lastMenuState)
            {
                lastMenuState = showMenu;
                if (showMenu)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }

            bool desiredGodCheat = godMode || freeBuild;
            if (desiredGodCheat != lastCheatGodMode)
            {
                lastCheatGodMode = desiredGodCheat;
                try
                {
                    Cheat.UseCheats = true;
                    Cheat.UseGodMode = desiredGodCheat;
                }
                catch {}
            }

            ProcessKeybinds();

            Network_Player player = null;
            try
            {
                player = ComponentManager<Network_Player>.Value;
            }
            catch {}

            if (player == null) return;
            if (noBuildRestrictions) ApplyNoBuildRestrictions(player);

            // Player Stats (God Mode, Infinite Needs - guarded property updates)
            if (player.Stats != null)
            {
                var stats = player.Stats;
                if (godMode && stats.stat_health != null && stats.stat_health.Value < stats.stat_health.Max)
                    stats.stat_health.Value = stats.stat_health.Max;

                if (godMode || infiniteNeeds)
                {
                    if (stats.stat_oxygen != null && stats.stat_oxygen.Value < stats.stat_oxygen.Max)
                        stats.stat_oxygen.Value = stats.stat_oxygen.Max;
                    if (stats.stat_hunger != null && stats.stat_hunger.normalConsumable != null && stats.stat_hunger.normalConsumable.Value < stats.stat_hunger.normalConsumable.Max)
                        stats.stat_hunger.normalConsumable.Value = stats.stat_hunger.normalConsumable.Max;
                    if (stats.stat_thirst != null && stats.stat_thirst.normalConsumable != null && stats.stat_thirst.normalConsumable.Value < stats.stat_thirst.normalConsumable.Max)
                        stats.stat_thirst.normalConsumable.Value = stats.stat_thirst.normalConsumable.Max;
                }
            }

            // Free Crafting (Unlimited Resources)
            if (freeCraft != lastFreeCraftState)
            {
                lastFreeCraftState = freeCraft;
                try
                {
                    var gmv = GameModeValueManager.GetCurrentGameModeValue();
                    if (gmv != null && gmv.playerSpecificVariables != null)
                    {
                        gmv.playerSpecificVariables.unlimitedResources = freeCraft;
                    }
                }
                catch {}
            }

            // Movement & Flight Physics Handling
            if (player.PersonController != null)
            {
                var controller = player.PersonController;

                if (noFallDamage || godMode || flyMode) controller.ResetFallDuration();

                float targetGround = superSpeed ? speedMultiplier : 1f;
                float targetWater = superDiving ? swimSpeedMultiplier : (superSpeed ? speedMultiplier : 1f);

                if (controller.GroundVelocityModifier != targetGround)
                    controller.GroundVelocityModifier = targetGround;
                if (controller.WaterVelocityModifier != targetWater)
                    controller.WaterVelocityModifier = targetWater;

                if (superJump)
                {
                    float targetJump = 7f * jumpMultiplier;
                    if (controller.jumpSpeed != targetJump) controller.jumpSpeed = targetJump;
                }

                if (flyMode)
                {
                    HandleFlight(player);
                }

                if (freeZiplineAir)
                {
                    ApplyFreeZiplineAir(player);
                }
                else if (controller.gravity == 0f)
                {
                    controller.gravity = 20f;
                    controller.stopGravitySpeed = 5f;
                    controller.ResetFallDuration();
                }
            }

            // Turbo Raft Movement Speed
            if (turboRaft)
            {
                try
                {
                    if (cachedRaft == null) cachedRaft = ComponentManager<Raft>.Value;
                    if (cachedRaft != null)
                    {
                        float targetMax = 8f * turboRaftMultiplier;
                        float targetDrift = 4f * turboRaftMultiplier;
                        if (cachedRaft.maxSpeed != targetMax) cachedRaft.maxSpeed = targetMax;
                        if (cachedRaft.waterDriftSpeed != targetDrift) cachedRaft.waterDriftSpeed = targetDrift;
                    }
                }
                catch {}
            }

            // Throttled Inventory Cheats (Durability, Ammo, Paint, Demolish)
            if (Time.time > nextInventoryThrottleTime)
            {
                nextInventoryThrottleTime = Time.time + 0.35f;
                if (player.Inventory != null)
                {
                    if (infiniteDurability) ApplyInfiniteDurability(player.Inventory);
                    if (infiniteAmmo) ApplyInfiniteAmmo(player.Inventory);
                    if (infinitePaint) ApplyInfinitePaint(player.Inventory);
                }
                if (instantDemolish) ApplyInstantDemolish(player);
            }

            // Weapon Damage (One-Hit or Multiplier)
            if (oneHitKill || weaponDamageMultiplier > 1.05f)
            {
                if (Time.time > nextWeaponDmgTime)
                {
                    nextWeaponDmgTime = Time.time + 0.35f;
                    ApplyWeaponDamage(player);
                }
            }

            // Projectiles (No Arrow Drop & Multishot)
            if ((noArrowDrop || multishotBow) && Time.time > nextArrowThrottleTime)
            {
                nextArrowThrottleTime = Time.time + 0.12f;
                var arrows = UnityEngine.Object.FindObjectsOfType<Arrow>();
                if (arrows != null && arrows.Length > 0)
                {
                    if (noArrowDrop) ApplyNoArrowDrop(arrows);
                    if (multishotBow) ApplyMultishotBow(player, arrows);
                }
            }

            if (killHostilesAura && Time.time > nextHostileAuraTime)
            {
                nextHostileAuraTime = Time.time + 0.35f;
                ApplyKillHostilesAura(player);
            }

            // Shark AI & Ghost Mode
            if ((pacifyShark || ghostMode) && Time.time > nextSharkAiThrottleTime)
            {
                nextSharkAiThrottleTime = Time.time + 0.35f;
                if (pacifyShark) ApplyPacifyShark();
                if (ghostMode) ApplyGhostMode(player);
            }
            else if (!ghostMode && player.gameObject.tag != "Player")
            {
                player.gameObject.tag = "Player";
            }

            // Gathering, Mining, Fishing Bite, Taming, Shovel Dig
            if (Time.time > nextToolsThrottleTime)
            {
                nextToolsThrottleTime = Time.time + 0.3f;
                if (instantHarvest || fastHook || fastChop) ApplyInstantHarvest(player);
                if (instantFishing) ApplyInstantFishing(player);
                if (instantTame) ApplyInstantTame();
                if (instantTreasureDig) ApplyInstantShovelDig(player);
            }

            // Automation: Instant Smelt/Cook, Batteries, Fuel
            if (Time.time > nextMachineryThrottleTime)
            {
                nextMachineryThrottleTime = Time.time + 0.45f;
                if (instantSmeltCook) ApplyInstantSmeltAndCook();
                if (infiniteBattery) ApplyInfiniteBatteries();
                if (infiniteFuel) ApplyInfiniteFuel();
            }

            // Item Magnet
            if (itemMagnet) ApplyItemMagnet(player);

            // Auto-Loot Pickups directly to inventory
            if (autoLootPickups && Time.time > nextAutoLootTime)
            {
                nextAutoLootTime = Time.time + 0.25f;
                ApplyAutoLoot(player);
            }

            // Anti-Seagull Aura
            if (antiSeagull && Time.time > nextAntiSeagullTime)
            {
                nextAntiSeagullTime = Time.time + 0.35f;
                ApplyAntiSeagull();
            }

            // Auto-Water Crops
            if (autoWaterCrops && Time.time > nextAutoWaterTime)
            {
                nextAutoWaterTime = Time.time + 2.0f;
                WaterAllCrops();
            }

            // Freeze Raft & Tailwind
            if (freezeRaft) ApplyFreezeRaft();
            if (alwaysTailwind && Time.time > nextTailwindTime)
            {
                nextTailwindTime = Time.time + 0.35f;
                ApplyAlwaysTailwind();
            }

            if (autoFishing && Time.time > nextAutoFishTime)
            {
                nextAutoFishTime = Time.time + 1.6f;
                ApplyAutoFishing(player);
            }

            if (instantDomesticProduction && Time.time > nextDomesticProdTime)
            {
                nextDomesticProdTime = Time.time + 1.0f;
                ApplyInstantDomesticProduction();
            }

            // Night Vision & Clear Water
            UpdateNightVision(player);
            UpdateClearWater();

            // V10 TITAN AUTOMATION HOOKS
            if (triggerbot) ApplyTriggerbot(player);
            HandleZoom(player);
            UpdateSubseaFloodlight(player);

            // Throttled V10 Periodic Scans (0.35s)
            if (Time.time > nextV10ThrottleTime)
            {
                nextV10ThrottleTime = Time.time + 1.8f;
                if (infiniteBait) ApplyInfiniteBait();
                if (infinitePipeWater) ApplyInfinitePipePressure();
                if (indestructibleRaft) ApplyIndestructibleRaft();
                if (alwaysActiveDetector) ApplyAlwaysActiveMetalDetector();
                if (autoCropHarvest && Time.time > nextAutoCropHarvestTime)
                {
                    nextAutoCropHarvestTime = Time.time + 2.0f;
                    ApplyAutoCropHarvest(player);
                }
                if (instantBiofuel) ApplyInstantBiofuel();
                if (hyperBeehives) ApplyHyperBeehives();
                if (noFuelLanterns) ApplyNoFuelLanterns();
                if (perpetualGrass) ApplyPerpetualGrass();
            }

            // High-Performance ESP Cache Update
            if (espMaster && Time.time > nextEspCacheTime)
            {
                nextEspCacheTime = Time.time + 0.35f;
                UpdateEspCache(player);
            }

            UpdateV14Features(player);
            UpdateV11Features(player);

            // AIMBOT EXECUTION
            HandleAimbots(player);
        }

        // ==========================================
        // V11 AUTO / LOGISTICS / CAMERA / PAINT / WORLD TOOLS
        // ==========================================

        private void UpdateV11Features(Network_Player player)
        {
            if (perfectBowDraw || savedBowDelay.Count > 0)
            {
                if (Time.time > nextBowScanTime)
                {
                    nextBowScanTime = Time.time + 0.5f;
                    ApplyPerfectBowDraw();
                }
            }

            if (infiniteHarpoonNet && Time.time > nextNetGunTime)
            {
                nextNetGunTime = Time.time + 0.35f;
                ApplyInfiniteHarpoonNet(player);
            }

            if (flipperGlider) ApplyFlipperGlider(player);

            if (infiniteOxygen && player.Stats != null && player.Stats.stat_oxygen != null && player.Stats.stat_oxygen.Value < player.Stats.stat_oxygen.Max)
            {
                player.Stats.stat_oxygen.Value = player.Stats.stat_oxygen.Max;
            }

            if (blueprintCamera != blueprintCameraOn) ApplyBlueprintCamera();

            if (autoZipline && Time.time > nextAutoZiplineTime)
            {
                nextAutoZiplineTime = Time.time + 0.4f;
                ApplyAutoZipline(player);
            }

            if (autoAnchorOnArrival && Time.time > nextAutoAnchorTime)
            {
                nextAutoAnchorTime = Time.time + 0.5f;
                ApplyAutoAnchorOnArrival();
            }

            if ((autoCooker || autoSmelter) && Time.time > nextAutoCookTime)
            {
                nextAutoCookTime = Time.time + 1.2f;
                ApplyAutoCookerAndSmelter(player);
            }

            if (autoShearer && Time.time > nextAutoShearTime)
            {
                nextAutoShearTime = Time.time + 1.0f;
                ApplyAutoShearer(player);
            }

            if (instantDesalinator && Time.time > nextDesalTime)
            {
                nextDesalTime = Time.time + 0.5f;
                ApplyInstantDesalinator(player);
            }

            if (wireframeBuilding && Time.time > nextWireScanTime)
            {
                nextWireScanTime = Time.time + 0.6f;
                ScanWireframeBlocks(player);
            }
            else if (!wireframeBuilding && wireBlocks.Count > 0)
            {
                wireBlocks.Clear();
            }

            if (soundBooster && Time.time > nextSoundScanTime)
            {
                nextSoundScanTime = Time.time + 2.0f;
                ApplySoundBooster(player);
            }
            else if (!soundBooster && (boostedSources.Count > 0 || mutedLowPass.Count > 0))
            {
                RestoreSoundBooster();
            }

            if (!sharkRiding && ridingShark != null) ridingShark = null;

            if (animalBrush && !showMenu && Input.GetMouseButtonDown(0)) SpawnBrushAnimal(player);
        }

        private void LateUpdate()
        {
            if (sharkRiding) UpdateSharkRiding();
            if (calmSea || savedRoughness >= 0f) ApplyCalmSea();
        }

        private void FixedUpdate()
        {
            if (raftStabilizer) ApplyRaftStabilizer();
            UpdateAutopilotPhysics();
        }

        private void ApplyPerfectBowDraw()
        {
            try
            {
                if (cachedBows == null || Time.time > nextBowsScanTime)
                {
                    nextBowsScanTime = Time.time + 3.0f;
                    cachedBows = UnityEngine.Object.FindObjectsOfType<ThrowableComponent_Bow>();
                }
                var bows = cachedBows != null ? cachedBows : new ThrowableComponent_Bow[0];
                for (int i = 0; i < bows.Length; i++)
                {
                    var bow = bows[i];
                    if (bow == null) continue;
                    int id = bow.GetInstanceID();

                    float savedDelay;
                    if (perfectBowDraw)
                    {
                        if (fThrowDelay != null && !savedBowDelay.ContainsKey(id)) savedBowDelay[id] = (float)fThrowDelay.GetValue(bow);
                        if (fThrowDelay != null) fThrowDelay.SetValue(bow, 0f);
                    }
                    else if (savedBowDelay.TryGetValue(id, out savedDelay) && fThrowDelay != null)
                    {
                        fThrowDelay.SetValue(bow, savedDelay);
                    }

                    if (fThrowCharge == null) continue;
                    var meter = fThrowCharge.GetValue(bow) as ChargeMeter;
                    if (meter == null || fChargeSpeed == null) continue;

                    float savedCharge;
                    if (perfectBowDraw)
                    {
                        if (!savedBowCharge.ContainsKey(id)) savedBowCharge[id] = (float)fChargeSpeed.GetValue(meter);
                        fChargeSpeed.SetValue(meter, 1000f);
                    }
                    else if (savedBowCharge.TryGetValue(id, out savedCharge))
                    {
                        fChargeSpeed.SetValue(meter, savedCharge);
                    }
                }

                if (!perfectBowDraw)
                {
                    savedBowDelay.Clear();
                    savedBowCharge.Clear();
                }
            }
            catch {}
        }

        private void ApplyInfiniteHarpoonNet(Network_Player player)
        {
            try
            {
                if (cachedNetGuns == null || Time.time > nextNetGunsScanTime)
                {
                    nextNetGunsScanTime = Time.time + 3.0f;
                    cachedNetGuns = UnityEngine.Object.FindObjectsOfType<ThrowableComponent_NetGun>();
                }
                var guns = cachedNetGuns != null ? cachedNetGuns : new ThrowableComponent_NetGun[0];
                for (int i = 0; i < guns.Length; i++)
                {
                    var gun = guns[i];
                    if (gun == null) continue;
                    if (mNetGunReload != null) mNetGunReload.Invoke(gun, null);
                    if (mNetGunCooldown != null) mNetGunCooldown.Invoke(gun, null);
                }

                if (player.Inventory == null) return;
                if (netCanisterItem == null) netCanisterItem = FindItemByKeyword("netcanister", "net_canister");
                if (netCanisterItem != null && player.Inventory.GetItemCount(netCanisterItem.UniqueName) < 5)
                {
                    player.Inventory.AddItem(netCanisterItem.UniqueName, 10);
                }
            }
            catch {}
        }

        private Item_Base FindItemByKeyword(string keyA, string keyB)
        {
            try
            {
                var all = ItemManager.GetAllItems();
                if (all == null) return null;
                int count = all.Count;
                for (int i = 0; i < count; i++)
                {
                    var item = all[i];
                    if (item == null || string.IsNullOrEmpty(item.UniqueName)) continue;
                    if (ContainsIgnoreCase(item.UniqueName, keyA) || (keyB != null && ContainsIgnoreCase(item.UniqueName, keyB))) return item;
                }
            }
            catch {}
            return null;
        }

        private void ApplyFlipperGlider(Network_Player player)
        {
            if (player == null || player.PersonController == null || player.Camera == null || flyMode) return;
            try
            {
                var controller = player.PersonController;
                if (controller.SubmersionState != UltimateWater.SubmersionState.Full) return;
                if (!Input.GetKey(KeyCode.W)) return;

                Vector3 forward = player.Camera.transform.forward;
                controller.externalVelocity = Vector3.Lerp(controller.externalVelocity, forward * flipperGlideSpeed, 5f * Time.deltaTime);
            }
            catch {}
        }

        private void ApplyBlueprintCamera()
        {
            try
            {
                if (flightCamera == null) flightCamera = UnityEngine.Object.FindObjectOfType<FlightCamera>();
                if (flightCamera == null)
                {
                    blueprintCamera = false;
                    statusMessage = "Дрон-камера не найдена в сцене";
                    return;
                }

                if (blueprintCamera && !blueprintCameraOn)
                {
                    flightCamera.Enable(true);
                    blueprintCameraOn = true;
                    statusMessage = "Дрон-камера включена: облетите плот и оцените постройку";
                }
                else if (!blueprintCamera && blueprintCameraOn)
                {
                    flightCamera.Disable(false);
                    blueprintCameraOn = false;
                    statusMessage = "Дрон-камера выключена";
                }
            }
            catch (Exception ex)
            {
                blueprintCamera = false;
                blueprintCameraOn = false;
                statusMessage = "Ошибка дрон-камеры: " + ex.Message;
            }
        }

        private void ApplyAutoZipline(Network_Player player)
        {
            if (player == null) return;
            try
            {
                var zip = player.ZiplinePlayer;
                if (zip == null) return;

                if (zip.IsAttachedToZipline)
                {
                    CollectPickupsWithin(player, 7f);
                    return;
                }

                if (Time.time < ziplineReattachTime) return;

                if (cachedZiplinePaths == null || Time.time > nextZiplineScanTime)
                {
                    nextZiplineScanTime = Time.time + 3.0f;
                    cachedZiplinePaths = UnityEngine.Object.FindObjectsOfType<MeshPath_Zipline>();
                }
                if (cachedZiplinePaths == null) return;

                Vector3 pp = player.transform.position;
                for (int i = 0; i < cachedZiplinePaths.Length; i++)
                {
                    var path = cachedZiplinePaths[i];
                    if (path == null) continue;
                    int idx = path.GetClosestPointIndexFromWorldPosition(pp, true);
                    Vector3 point = path.GetWorldPointPositionFromIndex(idx);
                    if ((point - pp).sqrMagnitude > 6.25f) continue;

                    ziplineReattachTime = Time.time + 4f;
                    if (zip.AttachToZipline(path, 0))
                    {
                        statusMessage = "Зиплайн: скольжение и сбор ресурсов по пути";
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка зиплайна: " + ex.Message;
            }
        }

        private void CollectPickupsWithin(Network_Player player, float radius)
        {
            if (player.PickupScript == null) return;
            Vector3 pos = player.transform.position;
            float rSqr = radius * radius;
            int count = cachedPickups.Count;
            for (int i = 0; i < count; i++)
            {
                var p = cachedPickups[i];
                if (p == null || !p.gameObject.activeInHierarchy) continue;
                if ((p.transform.position - pos).sqrMagnitude <= rSqr)
                {
                    player.PickupScript.PickupItem(p, true, false);
                }
            }
        }

        private void ApplyAutoAnchorOnArrival()
        {
            try
            {
                var raft = ComponentManager<Raft>.Value;
                if (raft == null) return;

                float shore = GetDistanceToShore(raft);
                if (shore < 0f || shore > autoAnchorDistance + 20f)
                {
                    autoAnchorArmed = true;
                    return;
                }

                if (shore <= autoAnchorDistance && autoAnchorArmed && !raft.IsAnchored)
                {
                    raft.AddAnchor(false, null);
                    autoAnchorArmed = false;
                    statusMessage = "Якорь брошен: берег в " + shore.ToString("F0") + "м";
                }
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка авто-якоря: " + ex.Message;
            }
        }

        private float GetDistanceToShore(Raft raft)
        {
            Vector3 rp = raft.transform.position;
            Landmark nearest = null;
            float bestSqr = float.MaxValue;
            int count = cachedLandmarks.Count;
            for (int i = 0; i < count; i++)
            {
                var lm = cachedLandmarks[i];
                if (lm == null) continue;
                float dSqr = (rp - lm.transform.position).sqrMagnitude;
                if (dSqr < bestSqr)
                {
                    bestSqr = dSqr;
                    nearest = lm;
                }
            }
            if (nearest == null || bestSqr > 32400f) return -1f;

            Vector3 dir = nearest.transform.position - rp;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return 0f;
            dir.Normalize();

            float halfSize = 6f;
            Vector3 origin = rp + Vector3.up * 1.5f + dir * halfSize;

            RaycastHit hit;
            if (Physics.Raycast(origin, dir, out hit, 180f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.collider.GetComponentInParent<Raft>() == null)
                    return hit.distance;
            }
            return Mathf.Sqrt(bestSqr);
        }

        private void ApplyAutoCookerAndSmelter(Network_Player player)
        {
            if (player == null || player.Inventory == null) return;
            try
            {
                Vector3 pp = player.transform.position;
                if (cachedCookingStands == null || Time.time > nextMachineryScanTime)
                {
                    nextMachineryScanTime = Time.time + 3.0f;
                    cachedCookingStands = UnityEngine.Object.FindObjectsOfType<Block_CookingStand>();
                    cachedChests = UnityEngine.Object.FindObjectsOfType<Storage_Small>();
                }

                float rSqr = autoLogisticsRadius * autoLogisticsRadius;
                var chests = new List<Storage_Small>();
                if (cachedChests != null)
                {
                    for (int i = 0; i < cachedChests.Length; i++)
                    {
                        var st = cachedChests[i];
                        if (st != null && (st.transform.position - pp).sqrMagnitude <= rSqr)
                        {
                            chests.Add(st);
                        }
                    }
                }

                var machines = cachedCookingStands ?? new Block_CookingStand[0];
                int moves = 0;
                for (int m = 0; m < machines.Length && moves < 8; m++)
                {
                    var machine = machines[m];
                    if (machine == null || machine is Block_CookingStand_Purifier) continue;
                    bool isSmelter = machine is Block_CookingStand_Smelter;
                    if (isSmelter ? !autoSmelter : !autoCooker) continue;
                    if ((machine.transform.position - pp).sqrMagnitude > rSqr) continue;

                    if (machine.cookingSlots != null)
                    {
                        for (int k = 0; k < machine.cookingSlots.Length; k++)
                        {
                            var doneSlot = machine.cookingSlots[k];
                            if (doneSlot != null && doneSlot.IsComplete) machine.CollectItem(doneSlot, player);
                        }
                    }

                    if (chests.Count == 0) continue;

                    if (isSmelter && GetCookingFuelCount(machine) < 4)
                    {
                        Storage_Small fuelChest;
                        string fuelName;
                        if (FindChestItem(chests, IsPlankItem, out fuelChest, out fuelName) && MoveOneFromChest(fuelChest, fuelName, player))
                        {
                            if (machine.IncrementFuel(1, player)) moves++;
                            else ReturnToChest(fuelChest, fuelName, player);
                        }
                    }

                    for (int c = 0; c < chests.Count && moves < 8; c++)
                    {
                        var chestInv = chests[c].GetInventoryReference();
                        if (chestInv == null || chestInv.allSlots == null) continue;
                        for (int i = 0; i < chestInv.allSlots.Count && moves < 8; i++)
                        {
                            var itemSlot = chestInv.allSlots[i];
                            if (itemSlot == null || !itemSlot.HasValidItemInstance() || itemSlot.itemInstance.baseItem == null) continue;

                            Item_Base food = itemSlot.itemInstance.baseItem;
                            if (isSmelter && ContainsIgnoreCase(food.UniqueName, "plank")) continue;

                            CookingSlot[] freeSlots = machine.GetCookingSlotsForItem(food);
                            if (freeSlots == null || freeSlots.Length == 0) continue;
                            if (!MoveOneFromChest(chests[c], food.UniqueName, player)) break;

                            if (machine.InsertItem(food, freeSlots, player)) moves++;
                            else ReturnToChest(chests[c], food.UniqueName, player);
                        }
                    }
                }

                if (moves > 0) statusMessage = "Авто-кухня: в работу отправлено " + moves + " ед.";
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка авто-кухни: " + ex.Message;
            }
        }

        private int GetCookingFuelCount(Block_CookingStand machine)
        {
            try
            {
                if (fCookingFuel == null) return 0;
                var fuel = fCookingFuel.GetValue(machine) as Fuel;
                if (fuel == null) return 0;
                return fuel.GetFuelCount();
            }
            catch {}
            return 0;
        }

        private bool FindChestItem(List<Storage_Small> chests, Func<string, bool> filter, out Storage_Small foundChest, out string foundName)
        {
            foundChest = null;
            foundName = null;
            for (int c = 0; c < chests.Count; c++)
            {
                var inv = chests[c].GetInventoryReference();
                if (inv == null || inv.allSlots == null) continue;
                for (int i = 0; i < inv.allSlots.Count; i++)
                {
                    var slot = inv.allSlots[i];
                    if (slot == null || !slot.HasValidItemInstance() || slot.itemInstance.baseItem == null) continue;
                    string name = slot.itemInstance.baseItem.UniqueName;
                    if (filter(name))
                    {
                        foundChest = chests[c];
                        foundName = name;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool MoveOneFromChest(Storage_Small chest, string name, Network_Player player)
        {
            var chestInv = chest.GetInventoryReference();
            if (chestInv == null || chestInv.GetItemCount(name) < 1) return false;
            if (!HasFreeInventorySlot(player.Inventory)) return false;
            chestInv.RemoveItem(name, 1);
            player.Inventory.AddItem(name, 1);
            return true;
        }

        private void ReturnToChest(Storage_Small chest, string name, Network_Player player)
        {
            player.Inventory.RemoveItem(name, 1);
            var inv = chest.GetInventoryReference();
            if (inv != null) inv.AddItem(name, 1);
        }

        private bool HasFreeInventorySlot(Inventory inv)
        {
            if (inv.allSlots == null) return false;
            for (int i = 0; i < inv.allSlots.Count; i++)
            {
                var s = inv.allSlots[i];
                if (s != null && !s.HasValidItemInstance()) return true;
            }
            return false;
        }

        private void ApplyAutoShearer(Network_Player player)
        {
            if (player == null || player.Inventory == null) return;
            try
            {
                if (cachedDomesticResources == null || Time.time > nextDomesticResourcesScanTime)
                {
                    nextDomesticResourcesScanTime = Time.time + 3.0f;
                    cachedDomesticResources = UnityEngine.Object.FindObjectsOfType<AI_NetworkBehaviour_Domestic_Resource>();
                }
                if (cachedDomesticResources == null) return;

                int got = 0;
                Vector3 pp = player.transform.position;
                for (int i = 0; i < cachedDomesticResources.Length; i++)
                {
                    var animal = cachedDomesticResources[i];
                    if (animal == null || animal.Resource == null || !animal.Resource.IsReady) continue;
                    if ((animal.transform.position - pp).sqrMagnitude > 2025f) continue;

                    Item_Base item = animal.Resource.HarvestResource();
                    if (item != null)
                    {
                        player.Inventory.AddItem(item.UniqueName, 1);
                        got++;
                    }
                }
                if (got > 0) statusMessage = "Стрижка / дойка: собрано " + got + " шт.";
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка стрижки: " + ex.Message;
            }
        }

        private void ApplyInstantDesalinator(Network_Player player)
        {
            try
            {
                if (cachedPurifiers == null || Time.time > nextPurifierScanTime)
                {
                    nextPurifierScanTime = Time.time + 3.0f;
                    cachedPurifiers = UnityEngine.Object.FindObjectsOfType<Electric_Purifier>();
                    cachedStandPurifiers = UnityEngine.Object.FindObjectsOfType<Block_CookingStand_Purifier>();
                }

                if (cachedPurifiers != null)
                {
                    for (int i = 0; i < cachedPurifiers.Length; i++)
                    {
                        var purifier = cachedPurifiers[i];
                        if (purifier != null && purifier.tank != null) purifier.tank.SetTankAmount(purifier.tank.maxCapacity);
                    }
                }

                if (cachedStandPurifiers != null)
                {
                    for (int i = 0; i < cachedStandPurifiers.Length; i++)
                    {
                        var stand = cachedStandPurifiers[i];
                        if (stand == null || stand.cookingSlots == null) continue;
                        for (int k = 0; k < stand.cookingSlots.Length; k++)
                        {
                            var slot = stand.cookingSlots[k];
                            if (slot == null) continue;
                            if (slot.IsBusy && !slot.IsComplete) slot.ForceSetTimer(9999f);
                            if (slot.IsComplete && player != null) stand.CollectItem(slot, player);
                        }
                    }
                }
            }
            catch {}
        }

        private void ApplyRaftStabilizer()
        {
            try
            {
                if (cachedRaft == null) cachedRaft = ComponentManager<Raft>.Value;
                if (cachedRaft == null) return;
                if (cachedRaftRb == null) cachedRaftRb = cachedRaft.GetComponent<Rigidbody>();
                if (cachedRaftRb == null) return;

                Vector3 levelAxis = Vector3.Cross(cachedRaft.transform.up, Vector3.up);
                Vector3 rollSpin = Vector3.Scale(cachedRaftRb.angularVelocity, new Vector3(1f, 0f, 1f));
                cachedRaftRb.AddTorque(levelAxis * (14f * stabilizerStrength) - rollSpin * 3.5f, ForceMode.Acceleration);
            }
            catch {}
        }

        private void ApplyCalmSea()
        {
            try
            {
                if (fWaterRoughness == null || fWaterMaxRoughness == null) return;
                if (calmSea)
                {
                    if (savedRoughness < 0f)
                    {
                        savedRoughness = (float)fWaterRoughness.GetValue(null);
                        savedMaxRoughness = (float)fWaterMaxRoughness.GetValue(null);
                        fWaterRoughness.SetValue(null, 0f);
                        fWaterMaxRoughness.SetValue(null, 0f);
                    }
                    else if (Time.time > nextCalmSeaTime)
                    {
                        nextCalmSeaTime = Time.time + 0.35f;
                        fWaterRoughness.SetValue(null, 0f);
                        fWaterMaxRoughness.SetValue(null, 0f);
                    }
                }
                else if (savedRoughness >= 0f)
                {
                    fWaterRoughness.SetValue(null, savedRoughness);
                    fWaterMaxRoughness.SetValue(null, savedMaxRoughness);
                    savedRoughness = -1f;
                    savedMaxRoughness = -1f;
                }
            }
            catch {}
        }

        private void ScanWireframeBlocks(Network_Player player)
        {
            wireBlocks.Clear();
            if (player == null || player.Camera == null) return;
            Vector3 cp = player.Camera.transform.position;

            if (cachedAllBlocks == null || Time.time > nextAllBlocksScanTime)
            {
                nextAllBlocksScanTime = Time.time + 2.5f;
                if (cachedRaft != null) cachedAllBlocks = cachedRaft.GetComponentsInChildren<Block>();
                else cachedAllBlocks = UnityEngine.Object.FindObjectsOfType<Block>();
            }

            if (cachedAllBlocks == null) return;
            for (int i = 0; i < cachedAllBlocks.Length && wireBlocks.Count < 300; i++)
            {
                var b = cachedAllBlocks[i];
                if (b != null && (b.transform.position - cp).sqrMagnitude <= 3025f) wireBlocks.Add(b);
            }
        }

        private void ApplySoundBooster(Network_Player player)
        {
            if (player == null) return;
            try
            {
                Vector3 pp = player.transform.position;
                var sources = UnityEngine.Object.FindObjectsOfType<AudioSource>();
                for (int i = 0; i < sources.Length; i++)
                {
                    var src = sources[i];
                    if (src == null || boostedSources.Contains(src)) continue;
                    if ((src.transform.position - pp).sqrMagnitude > 19600f) continue;
                    boostedSources.Add(src);
                    boostedRanges.Add(src.maxDistance);
                }
                for (int i = 0; i < boostedSources.Count; i++)
                {
                    if (boostedSources[i] != null) boostedSources[i].maxDistance = boostedRanges[i] * soundBoostMultiplier;
                }

                var filters = UnityEngine.Object.FindObjectsOfType<AudioLowPassFilter>();
                for (int i = 0; i < filters.Length; i++)
                {
                    var f = filters[i];
                    if (f == null || !f.enabled || (f.transform.position - pp).sqrMagnitude > 19600f) continue;
                    f.enabled = false;
                    mutedLowPass.Add(f);
                }
            }
            catch {}
        }

        private void RestoreSoundBooster()
        {
            for (int i = 0; i < boostedSources.Count; i++)
            {
                if (boostedSources[i] != null) boostedSources[i].maxDistance = boostedRanges[i];
            }
            for (int i = 0; i < mutedLowPass.Count; i++)
            {
                if (mutedLowPass[i] != null) mutedLowPass[i].enabled = true;
            }
            boostedSources.Clear();
            boostedRanges.Clear();
            mutedLowPass.Clear();
        }

        private void SpawnBrushAnimal(Network_Player player)
        {
            try
            {
                if (player == null || player.Camera == null) return;
                Ray ray = player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit hit;
                if (!Physics.Raycast(ray, out hit, 250f))
                {
                    statusMessage = "Наведитесь на поверхность для спавна";
                    return;
                }

                var host = ComponentManager<Network_Host>.Value;
                var entities = (host != null && fHostEntities != null) ? fHostEntities.GetValue(host) as Network_Host_Entities : null;
                if (entities == null)
                {
                    statusMessage = "Менеджер существ не найден";
                    return;
                }

                entities.CreateAINetworkBehaviour(brushAnimalTypes[brushAnimalIndex], hit.point + Vector3.up * 0.5f, null);
                statusMessage = "Заспавнен: " + brushAnimalNames[brushAnimalIndex];
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка кисти: " + ex.Message;
            }
        }

        private void SpawnJunkRain(int count)
        {
            try
            {
                var raft = ComponentManager<Raft>.Value;
                var mgr = UnityEngine.Object.FindObjectOfType<ObjectSpawnerManager>();
                if (raft == null || mgr == null)
                {
                    statusMessage = "Менеджер мусора не найден";
                    return;
                }

                int spawned = 0;
                for (int i = 0; i < count; i++)
                {
                    ObjectSpawner spawner = (i % 2 == 0 || mgr.plankSpawner == null) ? (ObjectSpawner)mgr.itemSpawner : (ObjectSpawner)mgr.plankSpawner;
                    if (spawner == null) continue;

                    SpawnableFloatingObject obj = spawner.GetObjectToSpawn(true);
                    if (obj == null) continue;

                    int poolIndex = spawner.GetRandomPoolDuplicateIndexFromSpawnable(obj);
                    Vector3 pos = raft.transform.position + new Vector3(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(14f, 26f), UnityEngine.Random.Range(-12f, 12f));
                    spawner.SpawnItemNetwork(pos, obj, poolIndex);
                    spawned++;
                }
                statusMessage = "Мусорный дождь: " + spawned + " объектов";
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка дождя: " + ex.Message;
            }
        }

        private void DuplicateChestItems(int multiplier)
        {
            try
            {
                var player = ComponentManager<Network_Player>.Value;
                if (player == null) return;

                Vector3 pp = player.transform.position;
                Storage_Small target = null;
                float bestScore = float.MaxValue;
                Storage_Small[] chests = (cachedChests != null && cachedChests.Length > 0) ? cachedChests : UnityEngine.Object.FindObjectsOfType<Storage_Small>();
                for (int i = 0; i < chests.Length; i++)
                {
                    var c = chests[i];
                    if (c == null) continue;
                    float dSqr = (c.transform.position - pp).sqrMagnitude;
                    if (c.IsOpen) dSqr = -1000f;
                    else if (dSqr > 36f) continue;
                    if (dSqr < bestScore)
                    {
                        bestScore = dSqr;
                        target = c;
                    }
                }
                if (target == null)
                {
                    statusMessage = "Откройте сундук или встаньте рядом (до 6м)";
                    return;
                }

                var inv = target.GetInventoryReference();
                if (inv == null || inv.allSlots == null) return;

                var names = new List<string>();
                var amounts = new List<int>();
                for (int i = 0; i < inv.allSlots.Count; i++)
                {
                    var s = inv.allSlots[i];
                    if (s == null || !s.HasValidItemInstance() || s.itemInstance.baseItem == null) continue;
                    names.Add(s.itemInstance.baseItem.UniqueName);
                    amounts.Add(s.itemInstance.Amount);
                }

                int added = 0;
                for (int i = 0; i < names.Count; i++)
                {
                    int extra = amounts[i] * (multiplier - 1);
                    if (extra <= 0) continue;
                    inv.AddItem(names[i], extra);
                    added += extra;
                }
                statusMessage = "Сундук x" + multiplier + ": добавлено " + added + " шт.";
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка дупликатора: " + ex.Message;
            }
        }

        private void UpdateSharkRiding()
        {
            try
            {
                var player = ComponentManager<Network_Player>.Value;
                if (player == null || player.PersonController == null || player.Camera == null) return;

                if (ridingShark == null || ridingShark.state == SharkState.Dead || ridingShark.state == SharkState.DeadSink)
                {
                    if (Time.time < nextSharkSearchTime) return;
                    nextSharkSearchTime = Time.time + 0.5f;
                    ridingShark = FindNearestShark(player.transform.position, 25f);
                    if (ridingShark == null)
                    {
                        statusMessage = "Рядом нет акулы: подплывите ближе";
                        return;
                    }
                    ridingShark.ChangeState(SharkState.PassiveWater);
                    statusMessage = "Вы оседлали акулу: WASD — курс, Space/Ctrl — глубина";
                }

                if (fSharkCanAttack != null) fSharkCanAttack.SetValue(ridingShark, false);
                if (fSharkTargetToAttack != null) fSharkTargetToAttack.SetValue(ridingShark, null);

                Vector3 forward = player.Camera.transform.forward;
                forward.y = 0f;
                forward.Normalize();
                Vector3 right = player.Camera.transform.right;
                right.y = 0f;
                right.Normalize();

                Vector3 move = Vector3.zero;
                if (Input.GetKey(KeyCode.W)) move += forward;
                if (Input.GetKey(KeyCode.S)) move -= forward;
                if (Input.GetKey(KeyCode.D)) move += right;
                if (Input.GetKey(KeyCode.A)) move -= right;
                if (move.sqrMagnitude > 1f) move.Normalize();

                float dt = Time.deltaTime;
                Transform sharkT = ridingShark.transform;
                Vector3 pos = sharkT.position + move * sharkRideSpeed * dt;
                if (Input.GetKey(KeyCode.Space)) pos.y += 3f * dt;
                if (Input.GetKey(KeyCode.LeftControl)) pos.y -= 3f * dt;

                if (move.sqrMagnitude > 0.01f)
                {
                    sharkT.rotation = Quaternion.Slerp(sharkT.rotation, Quaternion.LookRotation(move), 6f * dt);
                }
                sharkT.position = pos;

                var controller = player.PersonController;
                controller.gravity = 0f;
                controller.stopGravitySpeed = 0f;
                controller.externalVelocity = Vector3.zero;
                controller.ResetFallDuration();
                player.transform.position = pos + Vector3.up * 1.6f;
            }
            catch (Exception ex)
            {
                ridingShark = null;
                sharkRiding = false;
                statusMessage = "Ошибка акулы: " + ex.Message;
            }
        }

        private Shark FindNearestShark(Vector3 pos, float radius)
        {
            Shark best = null;
            float bestSqr = radius * radius;
            int cCount = cachedSharks.Count;
            if (cCount > 0)
            {
                for (int i = 0; i < cCount; i++)
                {
                    var s = cachedSharks[i];
                    if (s == null) continue;
                    float dSqr = (s.transform.position - pos).sqrMagnitude;
                    if (dSqr < bestSqr)
                    {
                        bestSqr = dSqr;
                        best = s;
                    }
                }
                if (best != null) return best;
            }
            var sharks = UnityEngine.Object.FindObjectsOfType<Shark>();
            for (int i = 0; i < sharks.Length; i++)
            {
                var s = sharks[i];
                if (s == null) continue;
                float dSqr = (s.transform.position - pos).sqrMagnitude;
                if (dSqr < bestSqr)
                {
                    bestSqr = dSqr;
                    best = s;
                }
            }
            return best;
        }

        private void AddWaypointHere()
        {
            var player = ComponentManager<Network_Player>.Value;
            if (player == null) return;
            if (waypoints.Count >= MaxWaypoints)
            {
                statusMessage = "Максимум точек: " + MaxWaypoints;
                return;
            }

            string name = newWaypointName.Trim();
            if (string.IsNullOrEmpty(name)) name = DefaultWaypointName(waypoints.Count);
            waypoints.Add(new TeleportWaypoint { name = name, position = player.transform.position });
            newWaypointName = "";
            statusMessage = "Точка сохранена: " + name;
        }

        private static readonly string[] WaypointPresets = { "Дом", "Шахта", "Склад", "Маяк" };
        private string DefaultWaypointName(int index)
        {
            return index < WaypointPresets.Length ? WaypointPresets[index] : "Точка " + (index + 1);
        }

        private void TeleportToWaypoint(int index)
        {
            try
            {
                var player = ComponentManager<Network_Player>.Value;
                if (player == null || index < 0 || index >= waypoints.Count) return;
                player.transform.position = waypoints[index].position + Vector3.up * 2f;
                if (player.PersonController != null) player.PersonController.ResetFallDuration();
                statusMessage = "Телепорт: " + waypoints[index].name;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка телепорта: " + ex.Message;
            }
        }

        private Color CurrentPaintColor()
        {
            return new Color(paintR / 255f, paintG / 255f, paintB / 255f, 1f);
        }

        private static string ColorToHex(Color c)
        {
            return "#" + Mathf.RoundToInt(c.r * 255f).ToString("X2") + Mathf.RoundToInt(c.g * 255f).ToString("X2") + Mathf.RoundToInt(c.b * 255f).ToString("X2");
        }

        private static bool TryParseHex(string text, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(text)) return false;
            string s = text.Trim().TrimStart('#');
            if (s.Length == 3) s = new string(new char[] { s[0], s[0], s[1], s[1], s[2], s[2] });
            if (s.Length != 6) return false;
            int value;
            if (!int.TryParse(s, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out value)) return false;
            color = new Color(((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f, 1f);
            return true;
        }

        private SO_ColorValue CreatePaintAsset()
        {
            Color c = CurrentPaintColor();
            var asset = ScriptableObject.CreateInstance<SO_ColorValue>();
            asset.paintColor = c;
            asset.buttonColor = c;
            return asset;
        }

        private void PaintBlockUnderCrosshair()
        {
            try
            {
                var player = ComponentManager<Network_Player>.Value;
                if (player == null || player.Camera == null) return;
                Ray ray = player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit hit;
                if (!Physics.Raycast(ray, out hit, 60f))
                {
                    statusMessage = "Наведитесь на блок плота";
                    return;
                }
                Block block = hit.collider.GetComponentInParent<Block>();
                if (block == null)
                {
                    statusMessage = "Это не блок плота";
                    return;
                }
                SO_ColorValue asset = CreatePaintAsset();
                block.SetInstanceColorAndPattern(asset, asset, 0, 0u);
                statusMessage = "Блок покрашен в " + ColorToHex(CurrentPaintColor());
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка покраски: " + ex.Message;
            }
        }

        private void PaintWholeRaftCustom()
        {
            try
            {
                SO_ColorValue asset = CreatePaintAsset();
                var blocks = UnityEngine.Object.FindObjectsOfType<Block>();
                int count = 0;
                for (int i = 0; i < blocks.Length; i++)
                {
                    if (blocks[i] == null) continue;
                    blocks[i].SetInstanceColorAndPattern(asset, asset, 0, 0u);
                    count++;
                }
                statusMessage = "Покрашено блоков: " + count + " (" + ColorToHex(CurrentPaintColor()) + ")";
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка покраски плота: " + ex.Message;
            }
        }

        private Texture2D MakeRoundedTex(int radius, Color fill, Color edge)
        {
            int size = radius * 2 + 4;
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] pix = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x, radius, size - 1 - radius);
                    float cy = Mathf.Clamp(y, radius, size - 1 - radius);
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    Color c = fill;
                    if (dist > radius + 0.5f) c = new Color(0f, 0f, 0f, 0f);
                    else if (dist > radius - 1f) c = edge;
                    pix[y * size + x] = c;
                }
            }
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private static Color Lighten(Color c, float k)
        {
            return new Color(Mathf.Min(1f, c.r * k), Mathf.Min(1f, c.g * k), Mathf.Min(1f, c.b * k), c.a);
        }

        private void ApplyOneHitKill()
        {
            try
            {
                var melee = UnityEngine.Object.FindObjectsOfType<MeleeWeapon>();
                foreach (var m in melee)
                {
                    if (m != null)
                    {
                        if (fMeleeDamage != null) fMeleeDamage.SetValue(m, 999999);
                        if (fMeleeInv != null) fMeleeInv.SetValue(m, true);
                    }
                }
                var arrows = UnityEngine.Object.FindObjectsOfType<Arrow>();
                foreach (var a in arrows)
                {
                    if (a != null && fArrowDamage != null) fArrowDamage.SetValue(a, 999999f);
                }
            }
            catch {}
        }

        private void ApplyPacifyShark()
        {
            try
            {
                int count = cachedSharks.Count;
                for (int i = 0; i < count; i++)
                {
                    var s = cachedSharks[i];
                    if (s != null)
                    {
                        if (fSharkCanAttack != null) fSharkCanAttack.SetValue(s, false);
                        if (fSharkBitePlayer != null) fSharkBitePlayer.SetValue(s, 0f);
                        if (fSharkBiteRaft != null) fSharkBiteRaft.SetValue(s, 0f);
                    }
                }
            }
            catch {}
        }

        private void ApplyInstantHarvest(Network_Player player)
        {
            if (player == null) return;
            try
            {
                var h = player.GetComponentInChildren<Hook>();
                if (h != null)
                {
                    h.gatherTime = 0.001f;
                    h.pullSpeed = 120f;
                }

                var c = player.GetComponentInChildren<PickupChanneling>();
                if (c != null)
                {
                    c.pickupTime = 0.001f;
                }

                if (player.AxeScript != null)
                {
                    player.AxeScript.chopBlockTime = 0.001f;
                }
            }
            catch {}
        }

        private void RefreshMachineryCache()
        {
            if (Time.time < nextMachineryScanFullTime) return;
            nextMachineryScanFullTime = Time.time + 3.0f;
            cachedCookingSlots = UnityEngine.Object.FindObjectsOfType<CookingSlot>();
            cachedCookingTables = UnityEngine.Object.FindObjectsOfType<CookingTable>();
            cachedTanks = UnityEngine.Object.FindObjectsOfType<Tank>();
            cachedBatteries = UnityEngine.Object.FindObjectsOfType<Battery>();
            cachedFuels = UnityEngine.Object.FindObjectsOfType<Fuel>();
        }

        private void ApplyInstantSmeltAndCook()
        {
            try
            {
                RefreshMachineryCache();
                if (cachedCookingSlots != null)
                {
                    for (int i = 0; i < cachedCookingSlots.Length; i++)
                    {
                        var s = cachedCookingSlots[i];
                        if (s != null && s.IsBusy) s.ForceSetTimer(9999f);
                    }
                }
                if (cachedCookingTables != null && fCookTimer != null)
                {
                    for (int i = 0; i < cachedCookingTables.Length; i++)
                    {
                        var t = cachedCookingTables[i];
                        if (t != null) fCookTimer.SetValue(t, 9999f);
                    }
                }
                if (cachedTanks != null)
                {
                    for (int i = 0; i < cachedTanks.Length; i++)
                    {
                        var tk = cachedTanks[i];
                        if (tk != null) tk.SetTankAmount(tk.maxCapacity);
                    }
                }
            }
            catch {}
        }

        private void ApplyInfiniteBatteries()
        {
            try
            {
                RefreshMachineryCache();
                if (cachedBatteries != null)
                {
                    for (int i = 0; i < cachedBatteries.Length; i++)
                    {
                        var b = cachedBatteries[i];
                        if (b != null)
                        {
                            var inst = b.GetBatteryInstance();
                            if (inst != null) inst.Uses = inst.BaseItemMaxUses;
                        }
                    }
                }
            }
            catch {}
        }

        private void ApplyInfiniteFuel()
        {
            try
            {
                RefreshMachineryCache();
                if (cachedFuels != null && fFuelCount != null && fMaxFuel != null)
                {
                    for (int i = 0; i < cachedFuels.Length; i++)
                    {
                        var f = cachedFuels[i];
                        if (f != null)
                        {
                            int max = (int)fMaxFuel.GetValue(f);
                            fFuelCount.SetValue(f, max);
                        }
                    }
                }
            }
            catch {}
        }

        private void ApplyInstantFishing(Network_Player player)
        {
            if (player == null || fFishOnHook == null) return;
            try
            {
                var rod = player.GetComponentInChildren<FishingRod>();
                if (rod == null) return;
                var b = rod.GetComponentInChildren<Bobber>();
                if (b != null)
                {
                    fFishOnHook.SetValue(b, true);
                    return;
                }
                var bobbers = UnityEngine.Object.FindObjectsOfType<Bobber>();
                for (int i = 0; i < bobbers.Length; i++)
                {
                    var bob = bobbers[i];
                    if (bob != null) fFishOnHook.SetValue(bob, true);
                }
            }
            catch {}
        }

        private void ApplyInstantTame()
        {
            try
            {
                if (cachedDomesticAnimals == null || Time.time > nextDomesticAnimalsScanTime)
                {
                    nextDomesticAnimalsScanTime = Time.time + 3.0f;
                    cachedDomesticAnimals = UnityEngine.Object.FindObjectsOfType<AI_NetworkBehaviour_Domestic>();
                }
                if (cachedDomesticAnimals != null)
                {
                    for (int i = 0; i < cachedDomesticAnimals.Length; i++)
                    {
                        var d = cachedDomesticAnimals[i];
                        if (d != null && !d.IsTamed)
                        {
                            d.IsTamed = true;
                            d.QuickTameLate();
                        }
                    }
                }
            }
            catch {}
        }

        private void UpdateClearWater()
        {
            if (clearWater == lastClearWaterState && !clearWater) return;
            try
            {
                if (clearWater != lastClearWaterState || Time.time > nextClearWaterScanTime)
                {
                    lastClearWaterState = clearWater;
                    nextClearWaterScanTime = Time.time + 3.0f;

                    if (clearWater)
                    {
                        RenderSettings.fog = false;
                        RenderSettings.fogDensity = 0f;

                        var uimes = UnityEngine.Object.FindObjectsOfType<UnderwaterIME>();
                        for (int i = 0; i < uimes.Length; i++)
                        {
                            var u = uimes[i];
                            if (u != null)
                            {
                                u.enabled = false;
                                u.EffectEnabled = false;
                            }
                        }
                    }
                    else
                    {
                        var uimes = UnityEngine.Object.FindObjectsOfType<UnderwaterIME>();
                        for (int i = 0; i < uimes.Length; i++)
                        {
                            var u = uimes[i];
                            if (u != null && !u.enabled)
                            {
                                u.enabled = true;
                                u.EffectEnabled = true;
                            }
                        }
                    }
                }
            }
            catch {}
        }

        private void ApplyNoArrowDrop(Arrow[] arrows)
        {
            try
            {
                if (arrows != null)
                {
                    for (int i = 0; i < arrows.Length; i++)
                    {
                        var a = arrows[i];
                        if (a == null || a.IsExpended) continue;
                        var rb = a.GetComponent<Rigidbody>();
                        if (rb != null && rb.useGravity)
                        {
                            rb.useGravity = false;
                            rb.velocity = rb.velocity.normalized * 65f;
                        }
                    }
                }

                if (cachedThrowables == null || Time.time > nextThrowablesScanTime)
                {
                    nextThrowablesScanTime = Time.time + 0.5f;
                    cachedThrowables = UnityEngine.Object.FindObjectsOfType<Throwable_Object>();
                }
                var throwables = cachedThrowables != null ? cachedThrowables : new Throwable_Object[0];
                for (int i = 0; i < throwables.Length; i++)
                {
                    var to = throwables[i];
                    if (to == null) continue;
                    var rb = to.GetComponent<Rigidbody>();
                    if (rb != null && rb.useGravity)
                    {
                        rb.useGravity = false;
                        rb.velocity = rb.velocity.normalized * 50f;
                    }
                }
            }
            catch {}
        }

        private void ApplyInfiniteAmmo(PlayerInventory inv)
        {
            if (inv == null || inv.allSlots == null) return;
            try
            {
                for (int i = 0; i < inv.allSlots.Count; i++)
                {
                    var s = inv.allSlots[i];
                    if (s != null && s.HasValidItemInstance())
                    {
                        var item = s.itemInstance;
                        if (item != null && item.baseItem != null && item.baseItem.settings_Inventory != null)
                        {
                            string un = item.baseItem.UniqueName;
                            if (ContainsIgnoreCase(un, "arrow") || ContainsIgnoreCase(un, "ammo") || ContainsIgnoreCase(un, "net_canister") || ContainsIgnoreCase(un, "stone"))
                            {
                                item.Amount = item.baseItem.settings_Inventory.StackSize;
                            }
                        }
                    }
                }
            }
            catch {}
        }

        private void ApplyAntiSeagull()
        {
            try
            {
                if (cachedSeagulls == null || Time.time > nextSeagullsScanTime)
                {
                    nextSeagullsScanTime = Time.time + 1.5f;
                    cachedSeagulls = UnityEngine.Object.FindObjectsOfType<Seagull>();
                }
                var gulls = cachedSeagulls != null ? cachedSeagulls : new Seagull[0];
                for (int i = 0; i < gulls.Length; i++)
                {
                    var g = gulls[i];
                    if (g == null) continue;
                    if (g.currentState == SeagullState.Hover || g.currentState == SeagullState.Dive || g.currentState == SeagullState.Peck)
                    {
                        g.targetCropplot = null;
                        g.SwitchState(SeagullState.FlyAway);
                    }
                }

                if (cachedCropplots == null || Time.time > nextCropplotsScanTime)
                {
                    nextCropplotsScanTime = Time.time + 3.0f;
                    cachedCropplots = UnityEngine.Object.FindObjectsOfType<Cropplot>();
                }
                var plots = cachedCropplots != null ? cachedCropplots : new Cropplot[0];
                for (int i = 0; i < plots.Length; i++)
                {
                    var cp = plots[i];
                    if (cp != null && cp.peckable) cp.peckable = false;
                }
            }
            catch {}
        }

        private void ApplyAutoLoot(Network_Player player)
        {
            if (player == null || player.PickupScript == null) return;
            try
            {
                float rSqr = autoLootRadius * autoLootRadius;
                Vector3 pPos = player.transform.position;
                for (int i = 0; i < cachedPickups.Count; i++)
                {
                    var p = cachedPickups[i];
                    if (p == null || !p.gameObject.activeInHierarchy) continue;
                    if ((p.transform.position - pPos).sqrMagnitude <= rSqr)
                    {
                        player.PickupScript.PickupItem(p, true, false);
                    }
                }
            }
            catch {}
        }

        private void WaterAllCrops()
        {
            try
            {
                int watered = 0;
                var staticPlots = PlantManager.allCropplots;
                if (staticPlots != null && staticPlots.Count > 0)
                {
                    for (int i = 0; i < staticPlots.Count; i++)
                    {
                        var cp = staticPlots[i];
                        if (cp != null) { cp.AddWater(false); watered++; }
                    }
                }
                else
                {
                    if (cachedCropplots == null || Time.time > nextCropplotsScanTime)
                    {
                        nextCropplotsScanTime = Time.time + 3.5f;
                        cachedCropplots = UnityEngine.Object.FindObjectsOfType<Cropplot>();
                    }
                    if (cachedCropplots != null)
                    {
                        for (int i = 0; i < cachedCropplots.Length; i++)
                        {
                            var cp = cachedCropplots[i];
                            if (cp != null) { cp.AddWater(false); watered++; }
                        }
                    }
                }
                statusMessage = "Полито грядок: " + watered;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка полива: " + ex.Message;
            }
        }

        private void InstantGrowCrops()
        {
            try
            {
                if (cachedPlants == null || Time.time > nextPlantsScanTime)
                {
                    nextPlantsScanTime = Time.time + 3.0f;
                    cachedPlants = UnityEngine.Object.FindObjectsOfType<Plant>();
                }
                int count = 0;
                if (cachedPlants != null)
                {
                    for (int i = 0; i < cachedPlants.Length; i++)
                    {
                        var p = cachedPlants[i];
                        if (p != null)
                        {
                            p.Complete();
                            count++;
                        }
                    }
                }
                statusMessage = "Созрело растений: " + count;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка роста: " + ex.Message;
            }
        }

        private void ApplyFreezeRaft()
        {
            try
            {
                if (cachedRaftRb != null)
                {
                    cachedRaftRb.velocity = Vector3.zero;
                    cachedRaftRb.angularVelocity = Vector3.zero;
                }
                else if (cachedRaft != null)
                {
                    cachedRaftRb = cachedRaft.GetComponent<Rigidbody>();
                }
            }
            catch {}
        }

        private void ToggleStationaryAnchor()
        {
            try
            {
                var anchors = UnityEngine.Object.FindObjectsOfType<Anchor_Stationary>();
                if (anchors != null && anchors.Length > 0)
                {
                    foreach (var a in anchors)
                    {
                        if (a != null)
                        {
                            if (a.AtBottom)
                            {
                                if (mWeighAnchor != null) mWeighAnchor.Invoke(a, null);
                                statusMessage = "Якорь поднят!";
                            }
                            else
                            {
                                if (mDropAnchor != null) mDropAnchor.Invoke(a, null);
                                statusMessage = "Якорь сброшен на дно!";
                            }
                        }
                    }
                }
                else
                {
                    var raft = ComponentManager<Raft>.Value;
                    if (raft != null)
                    {
                        if (raft.IsAnchored)
                        {
                            raft.RemoveAnchor(raft.AnchorCount);
                            statusMessage = "Виртуальный якорь снят!";
                        }
                        else
                        {
                            raft.AddAnchor(false, null);
                            statusMessage = "Виртуальный якорь закреплен!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка якоря: " + ex.Message;
            }
        }

        public string GetFrequencyCode(int index)
        {
            try
            {
                if (RecieverFrequency.AllFrequencies != null && index >= 0 && index < RecieverFrequency.AllFrequencies.Length)
                {
                    var rf = RecieverFrequency.AllFrequencies[index];
                    if (rf != null && rf.numbers != null && rf.numbers.Length > 0)
                    {
                        return "#" + string.Concat(rf.numbers);
                    }
                }
            }
            catch {}
            return "";
        }

        private void SetRadarFrequency(int index)
        {
            try
            {
                var recs = UnityEngine.Object.FindObjectsOfType<Reciever>();
                if (recs == null || recs.Length == 0)
                {
                    statusMessage = "Приемник (Радар) не найден на плоту!";
                    return;
                }
                foreach (var r in recs)
                {
                    if (r != null)
                    {
                        r.SetFrequencyIndexCheat(index);
                        r.CheckSignal();
                    }
                }
                string code = GetFrequencyCode(index);
                statusMessage = "Частота " + code + " установлена на радаре!";
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка: " + ex.Message;
            }
        }

        private void RepairAllRaftBlocks()
        {
            try
            {
                var blocks = UnityEngine.Object.FindObjectsOfType<Block>();
                int count = 0;
                for (int i = 0; i < blocks.Length; i++)
                {
                    var b = blocks[i];
                    if (b != null && b.Health < b.MaxHealth)
                    {
                        b.SetHealth(b.MaxHealth);
                        count++;
                    }
                }
                statusMessage = "Починено блоков: " + count;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка ремонта: " + ex.Message;
            }
        }

        private void ReinforceAllRaftFoundations()
        {
            try
            {
                var blocks = UnityEngine.Object.FindObjectsOfType<Block>();
                int count = 0;
                for (int i = 0; i < blocks.Length; i++)
                {
                    var b = blocks[i];
                    if (b != null && b.CanBeReinforced() && !b.Reinforced)
                    {
                        b.Reinforced = true;
                        count++;
                    }
                }
                statusMessage = "Укреплено металлом оснований: " + count;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка укрепления: " + ex.Message;
            }
        }

        private void UnlockAllJournalNotes()
        {
            try
            {
                var nb = UnityEngine.Object.FindObjectOfType<NoteBook>();
                if (nb != null)
                {
                    nb.UnlockAllNotes();
                    statusMessage = "Все заметки блокнота открыты (T)!";
                }
                else
                {
                    statusMessage = "Блокнот пока не загружен в мир!";
                }
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка открытия заметок: " + ex.Message;
            }
        }

        private void ApplyAutoFishing(Network_Player player)
        {
            if (player == null || Time.time < nextAutoFishTime) return;
            nextAutoFishTime = Time.time + 1.6f;
            try
            {
                var rod = player.GetComponentInChildren<FishingRod>() ?? UnityEngine.Object.FindObjectOfType<FishingRod>();
                if (rod == null) return;
                if (mPullItems != null)
                {
                    mPullItems.Invoke(rod, null);
                    statusMessage = "Авто-рыбалка: рыба поймана в сумку!";
                }
            }
            catch {}
        }

        private void ApplyInstantDomesticProduction()
        {
            try
            {
                if (cachedDomesticResources == null || Time.time > nextDomesticResourcesScanTime)
                {
                    nextDomesticResourcesScanTime = Time.time + 3.0f;
                    cachedDomesticResources = UnityEngine.Object.FindObjectsOfType<AI_NetworkBehaviour_Domestic_Resource>();
                }
                if (cachedDomesticResources != null)
                {
                    for (int i = 0; i < cachedDomesticResources.Length; i++)
                    {
                        var d = cachedDomesticResources[i];
                        if (d != null && d.Resource != null)
                        {
                            if (!d.Resource.IsReady)
                            {
                                d.Resource.IsReady = true;
                                d.Resource.SendIsReady();
                            }
                        }
                    }
                }

                if (BirdsNest.AllNests != null)
                {
                    int nCount = BirdsNest.AllNests.Count;
                    for (int i = 0; i < nCount; i++)
                    {
                        var nest = BirdsNest.AllNests[i];
                        if (nest != null)
                        {
                            if (nest.EggCount < 3) nest.LayEgg(3 - nest.EggCount);
                            if (nest.FeatherCount < 3) nest.LayFeather(3 - nest.FeatherCount);
                        }
                    }
                }
            }
            catch {}
        }

        private void CollectAllDomesticYield(Network_Player player)
        {
            try
            {
                int count = 0;
                if (BirdsNest.AllNests != null && player != null)
                {
                    int nCount = BirdsNest.AllNests.Count;
                    for (int i = 0; i < nCount; i++)
                    {
                        var nest = BirdsNest.AllNests[i];
                        if (nest != null)
                        {
                            if (nest.HasEgg) { nest.PickupEgg(player); count++; }
                            if (nest.HasFeather) { nest.PickupFeather(player); count++; }
                        }
                    }
                }
                ApplyInstantDomesticProduction();
                statusMessage = "Собраны ресурсы животных: " + count;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка сбора животных: " + ex.Message;
            }
        }

        private void ApplyAlwaysTailwind()
        {
            try
            {
                if (Sail.AllSails != null)
                {
                    int sCount = Sail.AllSails.Count;
                    for (int i = 0; i < sCount; i++)
                    {
                        var s = Sail.AllSails[i];
                        if (s != null && s.open)
                        {
                            s.forceMultiplier = 2.5f * windSpeedMultiplier;
                            s.lookingForward = false;
                            s.lookingBackward = false;
                        }
                    }
                }
            }
            catch {}
        }

        private void SetRaftDriftDirection(Vector3 dir, string dirName)
        {
            try
            {
                Raft.direction = dir;
                statusMessage = "Ветер направлен: " + dirName;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка ветра: " + ex.Message;
            }
        }

        private void ApplyInfinitePaint(PlayerInventory inv)
        {
            if (inv == null || inv.allSlots == null) return;
            try
            {
                bool foundPaint = false;
                for (int i = 0; i < inv.allSlots.Count; i++)
                {
                    var s = inv.allSlots[i];
                    if (s != null && s.HasValidItemInstance() && s.itemInstance.baseItem != null)
                    {
                        if (s.itemInstance.baseItem.UniqueName.Equals("Paint", StringComparison.OrdinalIgnoreCase))
                        {
                            s.itemInstance.Amount = s.itemInstance.baseItem.settings_Inventory.StackSize;
                            foundPaint = true;
                        }
                    }
                }
                if (!foundPaint)
                {
                    inv.AddItem("Paint", 100);
                }
            }
            catch {}
        }

        private void PaintAllRaftBlocks()
        {
            try
            {
                var colorMenu = ComponentManager<ColorMenu>.Value;
                if (colorMenu == null || colorMenu.primaryColorPicker == null)
                {
                    statusMessage = "Откройте меню кисти (R), чтобы выбрать цвет!";
                    return;
                }

                var colPrim = colorMenu.primaryColorPicker.SelectedColorValue;
                var colSec = colorMenu.secondaryColorPicker != null ? colorMenu.secondaryColorPicker.SelectedColorValue : colPrim;

                var blocks = UnityEngine.Object.FindObjectsOfType<Block>();
                int count = 0;
                foreach (var b in blocks)
                {
                    if (b != null)
                    {
                        b.SetInstanceColorAndPattern(colPrim, colSec, 0, 0u);
                        count++;
                    }
                }
                statusMessage = "Покрашено блоков плота: " + count;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка покраски: " + ex.Message;
            }
        }

        private void CollectAllQuestItems(Network_Player player)
        {
            if (player == null || player.PickupScript == null) return;
            try
            {
                var quests = UnityEngine.Object.FindObjectsOfType<QuestItemPickup>();
                int count = 0;
                foreach (var q in quests)
                {
                    if (q != null && q.gameObject.activeInHierarchy)
                    {
                        player.PickupScript.PickupItem(q, true, false);
                        count++;
                    }
                }
                statusMessage = "Собрано квестовых предметов: " + count;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка сбора квестов: " + ex.Message;
            }
        }

        private void ApplyKillHostilesAura(Network_Player player)
        {
            if (player == null) return;
            try
            {
                var host = ComponentManager<Network_Host>.Value;
                Vector3 playerPos = player.transform.position;
                float rSqr = hostileAuraRadius * hostileAuraRadius;

                int eCount = cachedEntities.Count;
                for (int i = 0; i < eCount; i++)
                {
                    var ent = cachedEntities[i];
                    if (ent == null || ent.gameObject == player.gameObject) continue;

                    if ((ent.transform.position - playerPos).sqrMagnitude > rSqr) continue;
                    if (ent.stat_health != null && ent.stat_health.IsDead()) continue;
                    if (ent.GetComponent<AI_NetworkBehaviour_Domestic>() != null) continue;

                    if (host != null)
                    {
                        host.DamageEntity(ent, ent.transform, 999999f, ent.transform.position, Vector3.up, EntityType.Enemy, null);
                    }
                    else if (ent.stat_health != null)
                    {
                        ent.stat_health.Value = 0f;
                    }
                }
            }
            catch {}
        }

        private void ApplyWeaponDamage(Network_Player player)
        {
            if (player == null) return;
            try
            {
                float targetDmg = oneHitKill ? 999999f : (20f * weaponDamageMultiplier);
                var m = player.GetComponentInChildren<MeleeWeapon>();
                if (m != null)
                {
                    if (fMeleeDamage != null) fMeleeDamage.SetValue(m, (int)targetDmg);
                    if (fMeleeInv != null) fMeleeInv.SetValue(m, oneHitKill || weaponDamageMultiplier > 2f);
                }
            }
            catch {}
        }

        private void ApplyNoBuildRestrictions(Network_Player player)
        {
            if (player == null) return;
            try
            {
                if (player.BlockCreator != null)
                {
                    if (player.BlockCreator.buildDelay > 0f) player.BlockCreator.buildDelay = 0f;
                    if (BlockCreator.MaxHeight < 9000f) BlockCreator.MaxHeight = 9999f;
                }

                if (Time.time > nextStableScanTime)
                {
                    nextStableScanTime = Time.time + 3.0f;
                    var stables = UnityEngine.Object.FindObjectsOfType<StableComponent>();
                    for (int i = 0; i < stables.Length; i++)
                    {
                        var sc = stables[i];
                        if (sc != null)
                        {
                            sc.requiredHitCount = 0;
                            sc.requireAll = false;
                        }
                    }
                }
            }
            catch {}
        }

        private void OpenNearestChest(Network_Player player)
        {
            if (player == null) return;
            try
            {
                var chests = (cachedChests != null && cachedChests.Length > 0) ? cachedChests : UnityEngine.Object.FindObjectsOfType<Storage_Small>();
                if (chests == null || chests.Length == 0)
                {
                    statusMessage = "Сундуки на плоту не найдены!";
                    return;
                }
                Storage_Small nearest = null;
                float minSqr = float.MaxValue;
                Vector3 playerPos = player.transform.position;

                for (int i = 0; i < chests.Length; i++)
                {
                    var c = chests[i];
                    if (c != null)
                    {
                        float dSqr = (c.transform.position - playerPos).sqrMagnitude;
                        if (dSqr < minSqr)
                        {
                            minSqr = dSqr;
                            nearest = c;
                        }
                    }
                }

                if (nearest != null)
                {
                    nearest.Open(player);
                    statusMessage = "Сундук открыт удаленно (" + Mathf.Sqrt(minSqr).ToString("F1") + "м)!";
                }
                else
                {
                    statusMessage = "Сундуки на плоту не найдены!";
                }
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка открытия сундука: " + ex.Message;
            }
        }

        private void AutoChopAndHarvestAll(Network_Player player)
        {
            if (player == null || player.Inventory == null) return;
            try
            {
                var trees = UnityEngine.Object.FindObjectsOfType<HarvestableTree>();
                int count = 0;
                foreach (var t in trees)
                {
                    if (t != null && !t.Depleted)
                    {
                        t.Harvest(player.Inventory);
                        count++;
                    }
                }
                var plants = UnityEngine.Object.FindObjectsOfType<Plant>();
                foreach (var p in plants)
                {
                    if (p != null)
                    {
                        p.Complete();
                        if (p.pickupComponent != null && player.PickupScript != null)
                        {
                            player.PickupScript.PickupItem(p.pickupComponent, true, false);
                            count++;
                        }
                    }
                }
                statusMessage = "Срублено и собрано объектов: " + count;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка сбора: " + ex.Message;
            }
        }

        private void ApplyFreeZiplineAir(Network_Player player)
        {
            if (player == null || player.PersonController == null) return;
            try
            {
                var controller = player.PersonController;
                if (Input.GetKey(KeyCode.Space) && Input.GetKey(KeyCode.W))
                {
                    controller.ResetFallDuration();
                    var cam = player.Camera;
                    if (cam != null)
                    {
                        Vector3 forward = cam.transform.forward;
                        player.transform.position += forward * ziplineAirSpeed * Time.deltaTime;
                    }
                }
            }
            catch {}
        }

        private void SpawnIslandBiome(ChunkPointType pointType, string biomeName)
        {
            try
            {
                var cm = UnityEngine.Object.FindObjectOfType<ChunkManager>();
                if (cm != null)
                {
                    cm.AddChunkPointCheat(pointType, Vector3.forward * 220f);
                    statusMessage = "Остров (" + biomeName + ") заспавнен впереди плота!";
                }
                else
                {
                    statusMessage = "Менеджер мира не найден!";
                }
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка спавна острова: " + ex.Message;
            }
        }

        private void HandleAimbots(Network_Player player)
        {
            if (!aimbotAnimals && !aimbotItems) return;
            if (player.CameraTransform == null) return;

            bool isAimTriggered = aimbotAlwaysOn || Input.GetKey(aimbotKey) || Input.GetMouseButton(1);
            if (!isAimTriggered) return;

            Vector3 camPos = player.CameraTransform.position;
            Vector3 camFwd = player.CameraTransform.forward;
            Vector3 aimTargetPos = Vector3.zero;
            bool targetFound = false;
            float maxRangeSqr = aimbotRange * aimbotRange;
            float minCos = Mathf.Cos(aimbotFOV * Mathf.Deg2Rad);
            float bestCos = minCos;

            // 1. Animals / Shark Aimbot
            if (aimbotAnimals)
            {
                int sCount = cachedSharksInfo.Count;
                for (int i = 0; i < sCount; i++)
                {
                    var s = cachedSharksInfo[i];
                    if (s == null || s.transform == null) continue;
                    Vector3 target = s.transform.position + Vector3.up * 0.4f;
                    Vector3 dir = target - camPos;
                    float dSqr = dir.sqrMagnitude;
                    if (dSqr > maxRangeSqr) continue;
                    float dot = Vector3.Dot(camFwd, dir);
                    if (dot <= 0f) continue;
                    float cosVal = dot / Mathf.Sqrt(dSqr);
                    if (cosVal > bestCos)
                    {
                        bestCos = cosVal;
                        aimTargetPos = target;
                        targetFound = true;
                    }
                }

                if (!targetFound)
                {
                    int eCount = cachedEntities.Count;
                    for (int i = 0; i < eCount; i++)
                    {
                        var ent = cachedEntities[i];
                        if (ent == null || ent.gameObject == player.gameObject) continue;

                        Vector3 entPos = ent.transform.position + Vector3.up * 0.6f;
                        Vector3 dir = entPos - camPos;
                        float dSqr = dir.sqrMagnitude;
                        if (dSqr > maxRangeSqr) continue;
                        float dot = Vector3.Dot(camFwd, dir);
                        if (dot <= 0f) continue;

                        if (ent.stat_health != null && ent.stat_health.IsDead()) continue;
                        if (ent.GetComponent<Network_Player>() != null) continue;

                        float cosVal = dot / Mathf.Sqrt(dSqr);
                        if (cosVal > bestCos)
                        {
                            bestCos = cosVal;
                            aimTargetPos = entPos;
                            targetFound = true;
                        }
                    }
                }
            }

            // 2. Items Aimbot
            if (!targetFound && aimbotItems)
            {
                int pCount = cachedPickups.Count;
                for (int i = 0; i < pCount; i++)
                {
                    var item = cachedPickups[i];
                    if (item == null) continue;

                    Vector3 itemPos = item.transform.position + Vector3.up * 0.2f;
                    Vector3 dir = itemPos - camPos;
                    float dSqr = dir.sqrMagnitude;
                    if (dSqr > maxRangeSqr) continue;
                    float dot = Vector3.Dot(camFwd, dir);
                    if (dot <= 0f) continue;

                    float cosVal = dot / Mathf.Sqrt(dSqr);
                    if (cosVal > bestCos)
                    {
                        bestCos = cosVal;
                        aimTargetPos = itemPos;
                        targetFound = true;
                    }
                }
            }

            if (targetFound)
            {
                ApplyAimbotRotation(player, aimTargetPos);
            }
        }

        private void ApplyAimbotRotation(Network_Player player, Vector3 targetPos)
        {
            try
            {
                Vector3 toTarget = (targetPos - player.CameraTransform.position).normalized;
                Quaternion lookRot = Quaternion.LookRotation(toTarget);

                float targetYaw = lookRot.eulerAngles.y;
                float targetPitch = lookRot.eulerAngles.x;
                if (targetPitch > 180f) targetPitch -= 360f;

                if (player.PlayerScript != null)
                {
                    if (player.PlayerScript.mouseLookXScript != null)
                    {
                        player.PlayerScript.mouseLookXScript.SetTargetRotX(targetYaw, true);
                    }
                    if (player.PlayerScript.mouseLookYCameraScript != null)
                    {
                        player.PlayerScript.mouseLookYCameraScript.SetTargetRotY(-targetPitch, true);
                    }
                    else if (player.PlayerScript.mouseLookYScript != null)
                    {
                        player.PlayerScript.mouseLookYScript.SetTargetRotY(-targetPitch, true);
                    }
                }

                if (aimSmoothness <= 0.05f)
                {
                    player.CameraTransform.rotation = lookRot;
                    player.transform.rotation = Quaternion.Euler(0, targetYaw, 0);
                }
                else
                {
                    float speed = Mathf.Lerp(45f, 8f, aimSmoothness / 10f);
                    player.CameraTransform.rotation = Quaternion.Slerp(player.CameraTransform.rotation, lookRot, speed * Time.deltaTime);
                    player.transform.rotation = Quaternion.Euler(0, player.CameraTransform.eulerAngles.y, 0);
                }
            }
            catch {}
        }

        private void HandleFlight(Network_Player player)
        {
            var cam = player.Camera;
            if (cam == null) return;

            Transform camTransform = cam.transform;
            Vector3 dir = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) dir += camTransform.forward;
            if (Input.GetKey(KeyCode.S)) dir -= camTransform.forward;
            if (Input.GetKey(KeyCode.D)) dir += camTransform.right;
            if (Input.GetKey(KeyCode.A)) dir -= camTransform.right;
            if (Input.GetKey(KeyCode.Space)) dir += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) dir -= Vector3.up;

            float currentSpeed = flySpeed;
            if (Input.GetKey(KeyCode.LeftShift)) currentSpeed *= 2.2f;

            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                player.transform.position += dir * currentSpeed * Time.deltaTime;
            }

            if (player.PersonController != null)
            {
                player.PersonController.gravity = 0f;
                player.PersonController.stopGravitySpeed = 0f;
                player.PersonController.externalVelocity = Vector3.zero;
                player.PersonController.ResetFallDuration();
            }
        }

        private void ApplyInfiniteDurability(PlayerInventory inv)
        {
            try
            {
                if (inv.allSlots != null)
                {
                    for (int i = 0; i < inv.allSlots.Count; i++)
                    {
                        var slot = inv.allSlots[i];
                        if (slot != null && slot.HasValidItemInstance())
                        {
                            var item = slot.itemInstance;
                            if (item != null && item.HasMaxUses) item.Uses = item.BaseItemMaxUses;
                        }
                    }
                }
            }
            catch {}
        }

        private void ApplyItemMagnet(Network_Player player)
        {
            try
            {
                Vector3 target = player.transform.position + Vector3.up * 0.5f;
                float rSqr = magnetRadius * magnetRadius;
                int count = cachedPickups.Count;
                for (int i = 0; i < count; i++)
                {
                    var p = cachedPickups[i];
                    if (p == null) continue;
                    Vector3 curPos = p.transform.position;
                    Vector3 diff = curPos - target;
                    float distSqr = diff.sqrMagnitude;
                    if (distSqr < rSqr && distSqr > 1.44f && IsMagnetAllowed(p))
                    {
                        p.transform.position = Vector3.MoveTowards(curPos, target, 28f * Time.deltaTime);
                    }
                }
            }
            catch {}
        }

        private void UpdateNightVision(Network_Player player)
        {
            if (!nightVision && nightVisionLight == null) return;
            try
            {
                if (nightVision)
                {
                    if (nightVisionLight == null)
                    {
                        var lightObj = new GameObject("__Trainer_NightVision__");
                        nightVisionLight = lightObj.AddComponent<Light>();
                        nightVisionLight.type = LightType.Point;
                        nightVisionLight.range = 80f;
                        nightVisionLight.intensity = 2.0f;
                        nightVisionLight.color = new Color(0.95f, 0.95f, 1f);
                        UnityEngine.Object.DontDestroyOnLoad(lightObj);
                    }
                    if (player.Camera != null)
                    {
                        nightVisionLight.transform.position = player.Camera.transform.position + player.Camera.transform.forward * 0.5f;
                        nightVisionLight.enabled = true;
                    }
                }
                else
                {
                    if (nightVisionLight != null) nightVisionLight.enabled = false;
                }
            }
            catch {}
        }

        public void KillAllEnemies()
        {
            try
            {
                int killed = 0;
                var host = ComponentManager<Network_Host>.Value;
                var player = ComponentManager<Network_Player>.Value;
                int count = cachedEntities.Count;
                for (int i = 0; i < count; i++)
                {
                    var ent = cachedEntities[i];
                    if (ent == null || (player != null && ent.gameObject == player.gameObject)) continue;

                    if (host != null)
                    {
                        host.DamageEntity(ent, ent.transform, 999999f, ent.transform.position, Vector3.up, EntityType.Enemy, null);
                        killed++;
                    }
                }
                statusMessage = "Уничтожено врагов: " + killed;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка: " + ex.Message;
            }
        }

        public void UnlockAllGameCharacters()
        {
            try
            {
                CharacterManager.UnlockAllCharacters();
                statusMessage = "Все персонажи и костюмы моментально разблокированы!";
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка открытия персонажей: " + ex.Message;
            }
        }

        public void EmptyAllCollectionNets()
        {
            try
            {
                var player = ComponentManager<Network_Player>.Value;
                if (player == null) return;

                var nets = UnityEngine.Object.FindObjectsOfType<ItemNet>();
                int totalCollected = 0;
                for (int i = 0; i < nets.Length; i++)
                {
                    var net = nets[i];
                    if (net != null && net.itemCollector != null)
                    {
                        if (net.itemCollector.collectedItems != null)
                            totalCollected += net.itemCollector.collectedItems.Count;
                        net.itemCollector.AddCollectedItemsToPlayer(player);
                    }
                }
                statusMessage = "Собрано предметов из сеток: " + totalCollected + " шт!";
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка сбора из сеток: " + ex.Message;
            }
        }

        public void TeleportToNearestIsland()
        {
            try
            {
                var player = ComponentManager<Network_Player>.Value;
                if (player == null) return;

                Landmark closest = null;
                float minSqr = float.MaxValue;
                Vector3 pPos = player.transform.position;
                int count = cachedLandmarks.Count;

                for (int i = 0; i < count; i++)
                {
                    var lm = cachedLandmarks[i];
                    if (lm == null) continue;
                    float dSqr = (pPos - lm.transform.position).sqrMagnitude;
                    if (dSqr < minSqr)
                    {
                        minSqr = dSqr;
                        closest = lm;
                    }
                }

                if (closest != null)
                {
                    player.transform.position = closest.transform.position + Vector3.up * 5f;
                    if (player.PersonController != null) player.PersonController.ResetFallDuration();
                    statusMessage = "Телепортирован к острову (" + Mathf.Sqrt(minSqr).ToString("F0") + "м)!";
                }
                else
                {
                    statusMessage = "Острова поблизости не найдены.";
                }
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка телепортации: " + ex.Message;
            }
        }

        public void TeleportToRaft()
        {
            try
            {
                var player = ComponentManager<Network_Player>.Value;
                var raft = ComponentManager<Raft>.Value;
                if (player != null && raft != null)
                {
                    player.transform.position = raft.transform.position + Vector3.up * 2f;
                    if (player.PersonController != null) player.PersonController.ResetFallDuration();
                    statusMessage = "Телепортирован на плот!";
                }
            }
            catch {}
        }

        public void UnlockAllResearchRecipes()
        {
            try
            {
                var table = ComponentManager<Inventory_ResearchTable>.Value;
                if (table == null) table = UnityEngine.Object.FindObjectOfType<Inventory_ResearchTable>();
                if (table != null)
                {
                    table.LearnAllRecipesInstantly();
                    statusMessage = "Все рецепты крафта моментально изучены!";
                }
            }
            catch {}
        }

        public void SetGameTime(int hour)
        {
            try
            {
                var sky = UnityEngine.Object.FindObjectOfType<UnityEngine.AzureSky.AzureSkyController>();
                if (sky != null && sky.timeOfDay != null)
                {
                    sky.timeOfDay.GotoTime(hour, 0);
                    statusMessage = "Время установлено: " + hour.ToString("00") + ":00";
                }
            }
            catch {}
        }

        public void SetWeather(string weather)
        {
            try
            {
                var wm = UnityEngine.Object.FindObjectOfType<WeatherManager>();
                if (wm != null)
                {
                    wm.SetWeather(weather, false);
                    statusMessage = "Погода: " + weather;
                }
            }
            catch {}
        }

        public void SpawnItem(string itemName, int count)
        {
            try
            {
                var player = ComponentManager<Network_Player>.Value;
                if (player == null || player.Inventory == null) return;

                var item = ItemManager.GetItemByName(itemName);
                if (item == null) item = ItemManager.GetItemByNameContains(itemName, "");

                if (item != null)
                {
                    player.Inventory.AddItem(item.UniqueName, count);
                    statusMessage = "Выдано: " + count + "x " + item.UniqueName;
                }
                else
                {
                    statusMessage = "Не найден предмет: " + itemName;
                }
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка спавна: " + ex.Message;
            }
        }

        private void EnsureItemCatalogLoaded()
        {
            if (allCatalogItems == null || allCatalogItems.Count == 0)
            {
                try
                {
                    var all = ItemManager.GetAllItems();
                    if (all != null && all.Count > 0)
                    {
                        allCatalogItems = new List<Item_Base>(all);
                        allCatalogItems.Sort((a, b) => string.Compare(GetItemTitle(a), GetItemTitle(b), StringComparison.OrdinalIgnoreCase));
                        UpdateFilteredCatalog();
                    }
                }
                catch {}
            }
        }

        private string GetItemTitle(Item_Base item)
        {
            if (item == null) return "";
            if (item.settings_Inventory != null && !string.IsNullOrEmpty(item.settings_Inventory.DisplayName))
            {
                return item.settings_Inventory.DisplayName;
            }
            return item.UniqueName;
        }

        private void UpdateFilteredCatalog()
        {
            if (allCatalogItems == null) return;

            if (filteredCatalogItems == null) filteredCatalogItems = new List<Item_Base>();
            else filteredCatalogItems.Clear();

            string searchLower = spawnerSearch.Trim();
            int catCount = allCatalogItems.Count;

            for (int i = 0; i < catCount; i++)
            {
                var item = allCatalogItems[i];
                if (item == null) continue;

                string title = GetItemTitle(item);
                string id = item.UniqueName;

                if (!string.IsNullOrEmpty(searchLower))
                {
                    if (!ContainsIgnoreCase(title, searchLower) && !ContainsIgnoreCase(id, searchLower)) continue;
                }

                if (selectedCategory == 1) // Оружие / Инструменты
                {
                    if (!ContainsIgnoreCase(id, "axe") && !ContainsIgnoreCase(id, "hook") && !ContainsIgnoreCase(id, "bow") && !ContainsIgnoreCase(id, "machete") && 
                        !ContainsIgnoreCase(id, "spear") && !ContainsIgnoreCase(id, "rod") && !ContainsIgnoreCase(id, "shovel") && !ContainsIgnoreCase(id, "bottle") &&
                        !ContainsIgnoreCase(id, "shears") && !ContainsIgnoreCase(id, "backpack") && !ContainsIgnoreCase(id, "flipper") && !ContainsIgnoreCase(id, "oxygen") &&
                        !ContainsIgnoreCase(id, "arrow"))
                        continue;
                }
                else if (selectedCategory == 2) // Ресурсы
                {
                    if (!ContainsIgnoreCase(id, "plank") && !ContainsIgnoreCase(id, "plastic") && !ContainsIgnoreCase(id, "scrap") && !ContainsIgnoreCase(id, "thatch") &&
                        !ContainsIgnoreCase(id, "rope") && !ContainsIgnoreCase(id, "ingot") && !ContainsIgnoreCase(id, "ore") && !ContainsIgnoreCase(id, "bolt") && 
                        !ContainsIgnoreCase(id, "hinge") && !ContainsIgnoreCase(id, "clay") && !ContainsIgnoreCase(id, "sand") && !ContainsIgnoreCase(id, "stone") &&
                        !ContainsIgnoreCase(id, "glass") && !ContainsIgnoreCase(id, "nail") && !ContainsIgnoreCase(id, "metal") && !ContainsIgnoreCase(id, "copper"))
                        continue;
                }
                else if (selectedCategory == 3) // Еда
                {
                    if (!id.Contains("raw") && !id.Contains("cooked") && !id.Contains("meat") && !id.Contains("fish") &&
                        !id.Contains("beet") && !id.Contains("potato") && !id.Contains("mango") && !id.Contains("water") &&
                        !id.Contains("berry") && !id.Contains("egg") && !id.Contains("soup"))
                        continue;
                }
                else if (selectedCategory == 4) // Другое
                {
                    if (id.Contains("axe") || id.Contains("hook") || id.Contains("bow") || id.Contains("machete") ||
                        id.Contains("spear") || id.Contains("plank") || id.Contains("plastic") || id.Contains("scrap") ||
                        id.Contains("meat") || id.Contains("fish") || id.Contains("potato"))
                        continue;
                }

                filteredCatalogItems.Add(item);
            }
        }

        // ==========================================
        // V10 TITAN AUTOMATION: 20 IMPLEMENTED MODULES
        // ==========================================

        private void ApplyTriggerbot(Network_Player player)
        {
            try
            {
                if (player == null || Time.time - lastTriggerbotTime < 0.22f) return;
                Camera cam = player.Camera;
                if (cam == null) return;

                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 35f))
                {
                    bool isHostile = false;
                    var animal = hit.collider.GetComponentInParent<AI_NetworkBehaviour_Animal>();
                    if (animal != null) isHostile = true;
                    var shark = hit.collider.GetComponentInParent<Shark>();
                    if (shark != null) isHostile = true;

                    if (isHostile)
                    {
                        var bow = player.GetComponentInChildren<ThrowableComponent_Bow>();
                        if (bow != null)
                        {
                            bow.HandleShoot();
                            lastTriggerbotTime = Time.time;
                            return;
                        }
                        var melee = player.GetComponentInChildren<MeleeWeapon>();
                        if (melee != null && hit.distance <= 4.5f)
                        {
                            melee.OnMeleeStart();
                            lastTriggerbotTime = Time.time;
                            return;
                        }
                    }
                }
            }
            catch {}
        }

        private void ApplyMultishotBow(Network_Player player, Arrow[] arrows)
        {
            if (player == null || arrows == null) return;
            try
            {
                Vector3 pPos = player.transform.position;
                for (int i = 0; i < arrows.Length; i++)
                {
                    var arr = arrows[i];
                    if (arr == null) continue;
                    int id = arr.GetInstanceID();
                    if (processedArrows.Contains(id)) continue;
                    processedArrows.Add(id);

                    if ((arr.transform.position - pPos).sqrMagnitude < 12.25f)
                    {
                        var rb = arr.GetComponent<Rigidbody>();
                        if (rb != null && rb.velocity.sqrMagnitude > 25f)
                        {
                            Vector3 vel = rb.velocity;
                            Vector3 pos = arr.transform.position;
                            SpawnExtraArrow(arr, pos, Quaternion.Euler(0, -5f, 0) * vel);
                            SpawnExtraArrow(arr, pos, Quaternion.Euler(0, 5f, 0) * vel);
                        }
                    }
                }
                if (processedArrows.Count > 100) processedArrows.Clear();
            }
            catch {}
        }

        private void SpawnExtraArrow(Arrow original, Vector3 pos, Vector3 vel)
        {
            try
            {
                var clone = UnityEngine.Object.Instantiate(original.gameObject, pos, original.transform.rotation);
                var cArr = clone.GetComponent<Arrow>();
                if (cArr != null)
                {
                    processedArrows.Add(cArr.GetInstanceID());
                    var rb = clone.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = vel;
                        rb.useGravity = !noArrowDrop;
                    }
                }
            }
            catch {}
        }

        private void ApplyRepulsorPulse(Network_Player player)
        {
            try
            {
                if (player == null) return;
                Vector3 pPos = player.transform.position;

                var animals = UnityEngine.Object.FindObjectsOfType<AI_NetworkBehaviour_Animal>();
                for (int i = 0; i < animals.Length; i++)
                {
                    var a = animals[i];
                    if (a == null) continue;
                    float dist = Vector3.Distance(a.transform.position, pPos);
                    if (dist <= 30f)
                    {
                        Vector3 pushDir = (a.transform.position - pPos).normalized;
                        if (pushDir.sqrMagnitude < 0.01f) pushDir = Vector3.forward;
                        a.transform.position += pushDir * 20f;
                        a.networkPosition = a.transform.position;
                        var rb = a.GetComponent<Rigidbody>();
                        if (rb != null) rb.velocity = pushDir * 30f;
                    }
                }

                var sharks = UnityEngine.Object.FindObjectsOfType<Shark>();
                for (int i = 0; i < sharks.Length; i++)
                {
                    var s = sharks[i];
                    if (s == null) continue;
                    float dist = Vector3.Distance(s.transform.position, pPos);
                    if (dist <= 35f)
                    {
                        Vector3 pushDir = (s.transform.position - pPos).normalized;
                        if (pushDir.sqrMagnitude < 0.01f) pushDir = Vector3.back;
                        s.transform.position += pushDir * 25f;
                        s.ChangeState(SharkState.PassiveWater);
                    }
                }
                statusMessage = "💥 Силовой импульс активирован (Враги отброшены на 20м)";
            }
            catch {}
        }

        private void ApplyGhostMode(Network_Player player)
        {
            try
            {
                if (player == null) return;
                if (ghostMode)
                {
                    if (player.gameObject.tag == "Player")
                    {
                        player.gameObject.tag = "Untagged";
                    }

                    int sCount = cachedSharks.Count;
                    for (int i = 0; i < sCount; i++)
                    {
                        var s = cachedSharks[i];
                        if (s == null) continue;
                        if (fSharkTargetToAttack != null) fSharkTargetToAttack.SetValue(s, null);
                        if (fSharkCanAttack != null) fSharkCanAttack.SetValue(s, false);
                        if (s.state == SharkState.AttackPlayer) s.ChangeState(SharkState.PassiveWater);
                    }

                    var chases = UnityEngine.Object.FindObjectsOfType<AI_State_Chase>();
                    for (int i = 0; i < chases.Length; i++)
                    {
                        if (chases[i] != null && fChaseTarget != null)
                        {
                            var target = fChaseTarget.GetValue(chases[i]) as Network_Player;
                            if (target == player) fChaseTarget.SetValue(chases[i], null);
                        }
                    }
                }
                else
                {
                    if (player.gameObject.tag != "Player")
                    {
                        player.gameObject.tag = "Player";
                    }
                }
            }
            catch {}
        }

        private void ApplyInfiniteBait()
        {
            try
            {
                var baits = UnityEngine.Object.FindObjectsOfType<SharkBait>();
                for (int i = 0; i < baits.Length; i++)
                {
                    var b = baits[i];
                    if (b == null) continue;
                    b.SetHealth(b.MaxHealth);
                }
            }
            catch {}
        }

        private void ApplyInstantDemolish(Network_Player player)
        {
            try
            {
                if (player == null) return;
                if (player.AxeScript != null)
                {
                    player.AxeScript.chopBlockTime = 0.001f;
                }
                else
                {
                    var axe = player.GetComponentInChildren<Axe>();
                    if (axe != null) axe.chopBlockTime = 0.001f;
                }
            }
            catch {}
        }

        private void BatchExpandRaftFoundations(int ringSize)
        {
            try
            {
                if (cachedBlockCreator == null) cachedBlockCreator = UnityEngine.Object.FindObjectOfType<BlockCreator>();
                if (cachedRaftBounds == null) cachedRaftBounds = UnityEngine.Object.FindObjectOfType<RaftBounds>();
                var bc = cachedBlockCreator;
                var bounds = cachedRaftBounds;
                Raft raft = ComponentManager<Raft>.Value;
                if (bc == null || bounds == null || raft == null) { statusMessage = "Плот не найден"; return; }

                Item_Base foundItem = ItemManager.GetItemByName("Foundation");
                if (foundItem == null) foundItem = ItemManager.GetItemByNameContains("foundation", "");
                if (foundItem == null) { statusMessage = "Предмет Foundation не найден"; return; }

                float bSize = BlockCreator.BlockSize > 0.5f ? BlockCreator.BlockSize : 1.5f;
                float hw = bounds.HalfWidth;
                float hl = bounds.HalfLength;

                // Высота существующих фундаментов
                float baseY = 0f;
                var existing = UnityEngine.Object.FindObjectsOfType<Block_Foundation>();
                if (existing != null && existing.Length > 0)
                    baseY = raft.transform.InverseTransformPoint(existing[0].transform.position).y;

                HashSet<string> used = new HashSet<string>();
                int placed = 0, failed = 0, guard = 0;

                // Основной проход: прицеливаемся в мировые координаты, снапим к сетке игры
                for (float x = -hw - ringSize * bSize; x <= hw + ringSize * bSize + 0.01f; x += bSize)
                {
                    if (guard > 1200) break;
                    for (float z = -hl - ringSize * bSize; z <= hl + ringSize * bSize + 0.01f; z += bSize)
                    {
                        if (guard > 1200) break;
                        guard++;
                        if (Mathf.Abs(x) <= hw - 0.15f && Mathf.Abs(z) <= hl - 0.15f) continue;

                        int r = TryPlaceExpanded(bc, foundItem, raft, new Vector3(x, 0f, z), baseY, used, bSize);
                        if (r > 0) placed++; else if (r < 0) failed++;
                    }
                }

                // Резервный путь: наращиваем ровным шагом сетки от крайних фундаментов
                if (placed == 0 && existing != null && existing.Length > 0)
                {
                    for (int e = 0; e < existing.Length; e++)
                    {
                        Block_Foundation fb = existing[e];
                        if (fb == null) continue;
                        Vector3 lp = raft.transform.InverseTransformPoint(fb.transform.position);
                        bool edgeXp = lp.x > hw - 0.15f;
                        bool edgeXn = lp.x < -hw + 0.15f;
                        bool edgeZp = lp.z > hl - 0.15f;
                        bool edgeZn = lp.z < -hl + 0.15f;
                        if (!edgeXp && !edgeXn && !edgeZp && !edgeZn) continue;

                        for (int k = 1; k <= ringSize; k++)
                        {
                            if (edgeXp) { int r = TryPlaceExpanded(bc, foundItem, raft, lp + new Vector3(bSize * k, 0, 0), baseY, used, bSize); if (r > 0) placed++; else if (r < 0) failed++; }
                            if (edgeXn) { int r = TryPlaceExpanded(bc, foundItem, raft, lp - new Vector3(bSize * k, 0, 0), baseY, used, bSize); if (r > 0) placed++; else if (r < 0) failed++; }
                            if (edgeZp) { int r = TryPlaceExpanded(bc, foundItem, raft, lp + new Vector3(0, 0, bSize * k), baseY, used, bSize); if (r > 0) placed++; else if (r < 0) failed++; }
                            if (edgeZn) { int r = TryPlaceExpanded(bc, foundItem, raft, lp - new Vector3(0, 0, bSize * k), baseY, used, bSize); if (r > 0) placed++; else if (r < 0) failed++; }
                        }
                    }
                }

                statusMessage = placed > 0
                    ? "🏗 Расширено фундаментов: " + placed + (failed > 0 ? " (не удалось " + failed + ")" : "")
                    : "🏗 Расширение не удалось (" + failed + " блоков) — проверьте лимит блоков";
                if (placed > 0) LogEvent("Расширение плота: +" + placed);
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка расширения: " + ex.Message;
            }
        }

        // Ставит один фундамент расширения: мир -> снап к сетке -> локально, с защитой от дублей
        private int TryPlaceExpanded(BlockCreator bc, Item_Base item, Raft raft, Vector3 localTarget, float baseY, HashSet<string> used, float bSize)
        {
            try
            {
                Vector3 world = raft.transform.TransformPoint(new Vector3(localTarget.x, baseY, localTarget.z));
                BlockCreator.SnapBuildPosition(ref world);
                Vector3 local = raft.transform.InverseTransformPoint(world);
                local.y = baseY;
                string key = Mathf.RoundToInt(local.x / bSize * 2f) + ":" + Mathf.RoundToInt(local.z / bSize * 2f);
                if (!used.Add(key)) return 0;
                bc.CreateBlockCheat(item, local, Vector3.zero, DPS.Default, 0);
                return 1;
            }
            catch { return -1; }
        }

        private void SmartChestAutoSort(Network_Player player)
        {
            try
            {
                if (player == null || player.Inventory == null) return;
                var storages = (cachedChests != null && cachedChests.Length > 0) ? cachedChests : UnityEngine.Object.FindObjectsOfType<Storage_Small>();
                Vector3 pPos = player.transform.position;
                int stashCount = 0;

                for (int s = 0; s < storages.Length; s++)
                {
                    var storage = storages[s];
                    if (storage == null) continue;
                    if ((storage.transform.position - pPos).sqrMagnitude > 625f) continue;

                    var inv = storage.GetInventoryReference();
                    if (inv == null) continue;

                    inv.SortPlayerInventoryIntoStorage();
                    inv.Sort();
                    stashCount++;
                }
                player.Inventory.Sort();
                statusMessage = "📦 Выгрузка и сортировка завершена (" + stashCount + " ящиков)";
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка выгрузки: " + ex.Message;
            }
        }

        private void ApplyInfinitePipePressure()
        {
            try
            {
                if (cachedTanks == null || Time.time > nextMachineryScanFullTime)
                {
                    RefreshMachineryCache();
                }
                var tanks = cachedTanks != null ? cachedTanks : new Tank[0];
                for (int i = 0; i < tanks.Length; i++)
                {
                    var t = tanks[i];
                    if (t == null) continue;
                    t.SetTankAmount(t.maxCapacity);
                }

                if (cachedSprinklers == null || Time.time > nextSprinklersScanTime)
                {
                    nextSprinklersScanTime = Time.time + 3.0f;
                    cachedSprinklers = UnityEngine.Object.FindObjectsOfType<Sprinkler>();
                }
                var sprinklers = cachedSprinklers != null ? cachedSprinklers : new Sprinkler[0];
                for (int i = 0; i < sprinklers.Length; i++)
                {
                    var sp = sprinklers[i];
                    if (sp == null) continue;
                    if (sp.waterTank != null) sp.waterTank.SetTankAmount(sp.waterTank.maxCapacity);
                }
            }
            catch {}
        }

        private void ApplyIndestructibleRaft()
        {
            try
            {
                Block[] blocks = cachedAllBlocks;
                if (blocks == null || Time.time > nextAllBlocksScanTime)
                {
                    nextAllBlocksScanTime = Time.time + 2.5f;
                    if (cachedRaft != null) cachedAllBlocks = cachedRaft.GetComponentsInChildren<Block>();
                    else cachedAllBlocks = UnityEngine.Object.FindObjectsOfType<Block>();
                    blocks = cachedAllBlocks;
                }
                if (blocks == null) return;

                for (int i = 0; i < blocks.Length; i++)
                {
                    var b = blocks[i];
                    if (b != null && b.Health < b.MaxHealth)
                    {
                        b.SetHealth(b.MaxHealth);
                    }
                }
            }
            catch {}
        }

        private void HandleZoom(Network_Player player)
        {
            if (!zoomEnabled && !isZooming && !Input.GetKey(KeyCode.Z)) return;
            try
            {
                if (player == null || player.Camera == null) return;
                Camera cam = player.Camera;

                bool keyHeld = UnityEngine.Input.GetKey(KeyCode.Z);
                if ((keyHeld || zoomEnabled) && !isZooming)
                {
                    isZooming = true;
                    originalFOV = cam.fieldOfView > 15f ? cam.fieldOfView : 60f;
                }
                else if (!keyHeld && !zoomEnabled && isZooming)
                {
                    isZooming = false;
                    cam.fieldOfView = originalFOV;
                }

                if (isZooming)
                {
                    cam.fieldOfView = Mathf.Max(5f, originalFOV / Mathf.Max(1.2f, zoomMultiplier));
                }
            }
            catch {}
        }

        private void UpdateSubseaFloodlight(Network_Player player)
        {
            if (!subseaFloodlight && floodlightComp == null) return;
            try
            {
                if (player == null || player.Camera == null) return;
                if (subseaFloodlight)
                {
                    if (floodlightObj == null)
                    {
                        floodlightObj = new GameObject("SubseaFloodlight");
                        floodlightObj.transform.SetParent(player.Camera.transform, false);
                        floodlightComp = floodlightObj.AddComponent<Light>();
                        floodlightComp.type = LightType.Spot;
                        floodlightComp.range = 220f;
                        floodlightComp.spotAngle = 110f;
                        floodlightComp.intensity = 3.5f;
                        floodlightComp.color = new Color(0.9f, 0.96f, 1.0f);
                    }
                    else if (floodlightComp != null && !floodlightComp.enabled)
                    {
                        floodlightComp.enabled = true;
                    }
                }
                else
                {
                    if (floodlightComp != null && floodlightComp.enabled)
                    {
                        floodlightComp.enabled = false;
                    }
                }
            }
            catch {}
        }

        private void PuzzleAutoSolver()
        {
            try
            {
                int solvedCount = 0;
                var keypads = UnityEngine.Object.FindObjectsOfType<Keypad>();
                for (int i = 0; i < keypads.Length; i++)
                {
                    var kp = keypads[i];
                    if (kp == null) continue;
                    if (fKeypadCorrectString != null)
                    {
                        string code = fKeypadCorrectString.GetValue(kp) as string;
                        if (!string.IsNullOrEmpty(code))
                        {
                            kp.SetCode(code);
                            if (kp.OnCodeComplete != null) kp.OnCodeComplete.Invoke();
                            solvedCount++;
                        }
                    }
                }

                var doors = UnityEngine.Object.FindObjectsOfType<Door>();
                for (int i = 0; i < doors.Length; i++)
                {
                    var d = doors[i];
                    if (d != null && !d.open)
                    {
                        d.Open();
                        solvedCount++;
                    }
                }
                statusMessage = "🔓 Решено кодов и открыто дверей: " + solvedCount;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка решения: " + ex.Message;
            }
        }

        private void ApplyAlwaysActiveMetalDetector()
        {
            try
            {
                if (cachedDetectors == null || Time.time > nextDetectorsScanTime)
                {
                    nextDetectorsScanTime = Time.time + 3.0f;
                    cachedDetectors = UnityEngine.Object.FindObjectsOfType<MetalDetector>();
                }
                var detectors = cachedDetectors != null ? cachedDetectors : new MetalDetector[0];
                for (int i = 0; i < detectors.Length; i++)
                {
                    var md = detectors[i];
                    if (md == null) continue;
                    if (fDetectorSearchRadius != null) fDetectorSearchRadius.SetValue(md, 9999f);
                }

                int tCount = cachedTreasures.Count;
                if (tCount > 0)
                {
                    for (int i = 0; i < tCount; i++)
                    {
                        var tp = cachedTreasures[i];
                        if (tp != null) tp.isRevealedViaDetector = true;
                    }
                }
                else
                {
                    var treasures = UnityEngine.Object.FindObjectsOfType<TreasurePoint>();
                    for (int i = 0; i < treasures.Length; i++)
                    {
                        var tp = treasures[i];
                        if (tp != null) tp.isRevealedViaDetector = true;
                    }
                }
            }
            catch {}
        }

        private void UnearthAllTreasures()
        {
            try
            {
                var treasures = UnityEngine.Object.FindObjectsOfType<TreasurePoint>();
                int dugCount = 0;
                for (int i = 0; i < treasures.Length; i++)
                {
                    var tp = treasures[i];
                    if (tp == null) continue;
                    tp.ProgressExcevation(tp.excevationCount, true);
                    tp.SetExcevationProgress(tp.excevationCount, true);
                    tp.SendNetworkProgressExcevation(tp.excevationCount);
                    tp.SendNetworkRevealTreasure();
                    dugCount++;
                }
                statusMessage = "🏆 Выкопано кладов на острове: " + dugCount;
            }
            catch (Exception ex)
            {
                statusMessage = "Ошибка раскопки: " + ex.Message;
            }
        }

        private void ApplyInstantShovelDig(Network_Player player)
        {
            try
            {
                if (player == null) return;
                var shovel = player.GetComponentInChildren<Shovel>();
                if (shovel != null)
                {
                    Vector3 pPos = player.transform.position;
                    float rSqr = 8f * 8f;
                    for (int i = 0; i < cachedTreasures.Count; i++)
                    {
                        var tp = cachedTreasures[i];
                        if (tp == null || !tp.gameObject.activeInHierarchy) continue;
                        if ((tp.transform.position - pPos).sqrMagnitude < rSqr)
                        {
                            tp.ProgressExcevation(tp.excevationCount, true);
                            tp.SetExcevationProgress(tp.excevationCount, true);
                            tp.SendNetworkProgressExcevation(tp.excevationCount);
                            tp.SendNetworkRevealTreasure();
                        }
                    }
                }
            }
            catch {}
        }

        private void ApplyAutoCropHarvest(Network_Player player)
        {
            try
            {
                if (player == null || player.Inventory == null) return;
                if (cachedPlants == null || Time.time > nextPlantsScanTime)
                {
                    nextPlantsScanTime = Time.time + 3.0f;
                    cachedPlants = UnityEngine.Object.FindObjectsOfType<Plant>();
                }
                if (cachedPlants == null) return;

                for (int i = 0; i < cachedPlants.Length; i++)
                {
                    var p = cachedPlants[i];
                    if (p == null) continue;

                    bool isRipe = false;
                    if (fPlantFullyGrown != null) isRipe = (bool)fPlantFullyGrown.GetValue(p);
                    if (!isRipe && fPlantGrowTimer != null)
                    {
                        float t = (float)fPlantGrowTimer.GetValue(p);
                        if (t >= p.growTime) isRipe = true;
                    }

                    if (isRipe && p.item != null)
                    {
                        player.Inventory.AddItem(p.item.UniqueName, 1);
                        p.PullRoots();
                    }
                }
            }
            catch {}
        }

        private void ApplyInstantBiofuel()
        {
            try
            {
                if (cachedBiofuelExtractors == null || Time.time > nextBiofuelScanTime)
                {
                    nextBiofuelScanTime = Time.time + 3.5f;
                    cachedBiofuelExtractors = UnityEngine.Object.FindObjectsOfType<Placeable_BiofuelExtractor>();
                }
                if (cachedBiofuelExtractors == null) return;

                for (int i = 0; i < cachedBiofuelExtractors.Length; i++)
                {
                    var ex = cachedBiofuelExtractors[i];
                    if (ex == null) continue;
                    if (ex.outputTank != null) ex.outputTank.SetTankAmount(ex.outputTank.maxCapacity);
                    if (ex.processingTank != null) ex.processingTank.SetTankAmount(ex.processingTank.maxCapacity);
                    if (ex.honeyTank != null) ex.honeyTank.SetTankAmount(ex.honeyTank.maxCapacity);
                }
            }
            catch {}
        }

        private void ApplyHyperBeehives()
        {
            try
            {
                if (cachedBeeHives == null || Time.time > nextBeeHiveScanTime)
                {
                    nextBeeHiveScanTime = Time.time + 3.5f;
                    cachedBeeHives = UnityEngine.Object.FindObjectsOfType<BeeHive>();
                }
                if (cachedBeeHives == null) return;

                for (int i = 0; i < cachedBeeHives.Length; i++)
                {
                    var h = cachedBeeHives[i];
                    if (h == null) continue;
                    h.UpdateHoneyLevel(3);
                    h.currentHoneyLevelIndex = 3;
                }
            }
            catch {}
        }

        private void ApplyNoFuelLanterns()
        {
            try
            {
                if (cachedFuels == null || Time.time > nextFuelsScanTime)
                {
                    nextFuelsScanTime = Time.time + 3.0f;
                    cachedFuels = UnityEngine.Object.FindObjectsOfType<Fuel>();
                }
                var fuels = cachedFuels != null ? cachedFuels : new Fuel[0];
                for (int i = 0; i < fuels.Length; i++)
                {
                    var f = fuels[i];
                    if (f == null) continue;
                    f.SetFuelCount(99);
                    if (!f.IsBurning()) f.StartBurning();
                }

                if (cachedLights == null || Time.time > nextLightsScanTime)
                {
                    nextLightsScanTime = Time.time + 4.0f;
                    cachedLights = UnityEngine.Object.FindObjectsOfType<LightNightNDayToggler>();
                }
                var lights = cachedLights != null ? cachedLights : new LightNightNDayToggler[0];
                for (int i = 0; i < lights.Length; i++)
                {
                    var l = lights[i];
                    if (l == null) continue;
                    l.enabled = true;
                }
            }
            catch {}
        }

        private void ApplyPerpetualGrass()
        {
            try
            {
                if (cachedGrassPlots == null || Time.time > nextGrassScanTime)
                {
                    nextGrassScanTime = Time.time + 3.5f;
                    cachedGrassPlots = UnityEngine.Object.FindObjectsOfType<Cropplot_Grass>();
                }
                if (cachedGrassPlots == null) return;

                for (int i = 0; i < cachedGrassPlots.Length; i++)
                {
                    var gp = cachedGrassPlots[i];
                    if (gp == null) continue;
                    gp.AddWater(false);
                }
            }
            catch {}
        }

    }
}

