using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.Config;
using Terraria.ModLoader;
using Terraria.ID;
using Newtonsoft.Json;
using Terraria;

namespace MultiSpawn
{

    // @ERROR: Moving stuff in a separate config IS A PROBLEM cause save does not apply to the other config!!! (so presets don't work)
    //public class MSPropConfig : ModConfig

    public class MSConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Label("Position Offset Range")]
        [Tooltip("Randomized maximum distance between duplicate spawns")]
        [DefaultValue(100)]
        [Range(0, 300)]
        public int PositionOffsetRange;

        [Label("Position Invert Relative X")]
        [Tooltip("Invert side of duplicated enemies (if enabled try to spawn pillars while in the center of the map)")]
        [DefaultValue(true)]
        public bool InvRelX;

        [Label("Minimum Spawns")]
        [Tooltip("Minimum number of extra spawns for each event")]
        [DefaultValue(1)]
        [Range(0, 20)]
        public int MinSpawns;

        [Label("Maximum Spawns")]
        [Tooltip("Maximum number of extra spawns for each event")]
        [DefaultValue(1)]
        [Range(0, 20)]
        public int MaxSpawns;

        [Label("Boss Bar Y Offset")]
        [Tooltip("Vertical offset between multiple boss bars")]
        [Range(5, 100)]
        [DefaultValue(50)]
        public int BossBarYOffset;

        [Label("Boss Bar Columns")]
        [Tooltip("Number of bars to try to fit horizontally")]
        [Range(1, 3)]
        [DefaultValue(2)]
        public int BossBarColumns;

        [Label("Active Boss Bar Seek")]
        [Tooltip("Actively seek all boss bars in the area, even when one is already shown")]
        [DefaultValue(true)]
        public bool ActiveBossBarSeek;

        [Label("AI Tick Randomization Factor Min")]
        [Tooltip("Advance clones AI so they don't match their parent (can disrupt movement)")]
        [Range(0, 240)]
        [DefaultValue(97)]
        public int AITickFactorMin;

        [Label("AI Tick Randomization Factor Max")]
        [Tooltip("Advance clones AI so they don't match their parent (can disrupt movement)")]
        [Range(0, 240)]
        [DefaultValue(230)]
        public int AITickFactorMax;

        [Label("WoF Draw No Clutter")]
        [Tooltip("Reduces clutter by bypassing the WoF drawing code to link hungries only to nearest WoF")]
        [DefaultValue(true)]
        public bool WoFDrawNoClutter;

        //[SeparatePage] // (WORKS BAD!)
        [Header("DEBUG and NOTWORKING")]

        [Label("NPCs to NOT Invert Relative X")]
        [Tooltip("List of NPC not to invert across X axis")]
        public HashSet<NPCDefinition> DontInvRelX = new();

        [Label("WoF Dupe X Velocity Multiplyer")]
        [Tooltip("Multiply X velocity of WoF if it is a clone")]
        [DefaultValue(-1.0f)]
        [Range(-1.0f, 1.0f)]
        public float WoFXVelMul;

        [Label("Idle Boss Bar Seek Extra")]
        [Tooltip("Extra area out of player screen to seek for active bosses while idle [Valilla default 5000]")]
        [DefaultValue(5000)]
        [Range(0, 5000)]
        public int IdleBossBarSeekExtra;

        [Label("Avoid AI Spawn Duplication [DEPRECATED]")]
        [Tooltip("Don't duplicate NPC spawned entities (like skeletron hands or worm segments)")]
        [DefaultValue(false)]
        public bool AvoidAISpawnDuplication;

        [Label("Try AI for One Frame [RECOMMENDED]")]
        [Tooltip("Try to spawn the duplicate, make it live for one AI cycle and determine if it is resident")]
        [DefaultValue(true)]
        public bool TryAIOneFrame;

        [Label("Never Duplicate")]
        [Tooltip("Enemies to never try to duplicate")]
        public HashSet<NPCDefinition> NeverDupe = new(new[]
        {
            // EoW parts will become heads if not matched... :( lots of heads
            NPCID.EaterofWorldsBody,
            NPCID.EaterofWorldsTail,
            // Doesn't check AI slot so index out of bounds
            NPCID.MoonLordLeechBlob
        }.Select(t => new NPCDefinition(t)));

        [Label("Invert X World")]
        [Tooltip("Enemies to invert X position wrt world center instead of player")]
        public HashSet<NPCDefinition> InvXFromWorld = new(new[]
        {
            NPCID.LunarTowerNebula, NPCID.LunarTowerSolar, NPCID.LunarTowerStardust, NPCID.LunarTowerVortex
        }.Select(t => new NPCDefinition(t)));

        [Label("Natural Spawn Force AI")]
        [Tooltip("Force AI tick of natural spawn to guarantee direct subordinate spawn order")]
        public HashSet<NPCDefinition> NaturalForceAI = new(new[]
        {
            NPCID.WallofFlesh,
            NPCID.MartianSaucerCore,
            NPCID.Golem // Need to make NewNPC in Golem AI to trigger with Start!
            //NPCID.MoonLordCore // Cannot force AI because it waits before spawning stuff for the drama
        }.Select(t => new NPCDefinition(t)));

        [Label("Boss Bar Sorting [RECOMMENDED]")]
        [Tooltip("Try to keep boss bars in a consistent order")]
        [DefaultValue(true)]
        public bool BossBarSorting;

        [Label("NPCs to Suppress Health Bar")]
        [Tooltip("List of NPC types to block from displaying boss bars")]
        public HashSet<NPCDefinition> NPCSuppressBar = new(new[]
        {
            NPCID.GolemHeadFree
        }.Select(t => new NPCDefinition(t)));

        [Label("Show Spawns Debug")]
        [Tooltip("Show debug duplication info and caps")]
        [DefaultValue(false)]
        public bool ShowSpawnsDebug;

        [Label("NPCs to Show Slots")]
        [Tooltip("List of NPC types to show in the slots view to define the order")]
        public HashSet<NPCDefinition> NPCToShowSlots = new(new[]
        {
            NPCID.Retinazer, NPCID.Spazmatism,
            NPCID.BrainofCthulhu, //NPCID.Creeper,
            NPCID.Golem, NPCID.GolemFistLeft, NPCID.GolemFistRight, NPCID.GolemHead, NPCID.GolemHeadFree,
            NPCID.MoonLordCore, NPCID.MoonLordHand, NPCID.MoonLordHead,
            NPCID.MartianSaucer, NPCID.MartianSaucerCannon, NPCID.MartianSaucerCore, NPCID.MartianSaucerTurret
        }.Select(t => new NPCDefinition(t)));

        [Header("WoF Mode")]

        [Label("SecondDup WoF Crossing")]
        [Tooltip("Have the second dupe (3rd wof) behaviour be forced to crossing")]
        [DefaultValue(true)]
        public bool HaveSecondDupWoFCrossing;

        // PROPERTIES
        // @WARNING: Properties get processed AFTER fields, so you get them at the bottom even if you shouldn't

        [JsonIgnore]
        [Label("WoF Presets")]
        [Tooltip("tML is broken for property updates, so save right after each change of this property!")]
        public WoFPresets WoFPreset
        {
            get
            {
                var conf = ModContent.GetInstance<MSConfig>();
                var wofDef = new NPCDefinition(NPCID.WallofFlesh);
                var wofDontInvX = conf.DontInvRelX.Contains(wofDef);
                var wofInvXFromWorld = conf.InvXFromWorld.Contains(wofDef);
                var wofMoonwalk = conf.WoFXVelMul < 0.0f;

                if (wofDontInvX && !wofMoonwalk)
                    return WoFPresets.SAMESIDE;

                if (!wofDontInvX && wofInvXFromWorld && !wofMoonwalk)
                    return WoFPresets.CROSSING;

                if (!wofDontInvX && !wofInvXFromWorld && wofMoonwalk)
                    return WoFPresets.BOX;

                return WoFPresets.UNKNOWN;
            }
            set
            {
                var conf = ModContent.GetInstance<MSConfig>();
                var wofDef = new NPCDefinition(NPCID.WallofFlesh);
                switch (value)
                {
                    case WoFPresets.SAMESIDE:
                        conf.DontInvRelX.Add(wofDef);
                        conf.WoFXVelMul = 1.0f;
                        break;
                    case WoFPresets.CROSSING:
                        conf.DontInvRelX.Remove(wofDef);
                        conf.InvXFromWorld.Add(wofDef);
                        conf.WoFXVelMul = 1.0f;
                        break;
                    case WoFPresets.BOX:
                        conf.DontInvRelX.Remove(wofDef);
                        conf.InvXFromWorld.Remove(wofDef);
                        conf.WoFXVelMul = -1.0f;
                        break;
                }
            }
        }
        public enum WoFPresets
        {
            UNKNOWN,
            SAMESIDE,
            CROSSING,
            BOX
        }

        [Header("DEBUG and NOTWORKING")]

        [Label("Actual NPC Limit used")]
        [Tooltip("NPC limit used taking into account vanilla and NPCUnlimiter if present")]
        public int NPCLimit => MultiSpawn.maxNPCs;

    }
}
