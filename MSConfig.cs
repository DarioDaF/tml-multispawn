using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Terraria.ModLoader.Config;
using Terraria.ModLoader;
using Terraria.ID;
using Newtonsoft.Json;

namespace MultiSpawn
{

    // @ERROR: Moving stuff in a separate config IS A PROBLEM cause save does not apply to the other config!!! (so presets don't work)
    //public class MSPropConfig : ModConfig

    public class MSConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [DefaultValue(100)]
        [Range(0, 300)]
        public int PositionOffsetRange;

        [DefaultValue(true)]
        public bool InvRelX;

        [DefaultValue(1)]
        [Range(0, 20)]
        public int MinSpawns;

        [DefaultValue(1)]
        [Range(0, 20)]
        public int MaxSpawns;

        [Range(5, 100)]
        [DefaultValue(50)]
        public int BossBarYOffset;

        [Range(1, 3)]
        [DefaultValue(2)]
        public int BossBarColumns;

        [DefaultValue(true)]
        public bool ActiveBossBarSeek;

        [Range(0, 240)]
        [DefaultValue(97)]
        public int AITickFactorMin;

        [Range(0, 240)]
        [DefaultValue(230)]
        public int AITickFactorMax;

        [DefaultValue(true)]
        public bool WoFDrawNoClutter;

        //[SeparatePage] // (WORKS BAD!)
        [Header("Debug")]

        public HashSet<NPCDefinition> DontInvRelX = new();

        [DefaultValue(-1.0f)]
        [Range(-1.0f, 1.0f)]
        public float WoFXVelMul;

        [DefaultValue(5000)]
        [Range(0, 5000)]
        public int IdleBossBarSeekExtra;

        [DefaultValue(false)]
        public bool AvoidAISpawnDuplication;

        [DefaultValue(true)]
        public bool TryAIOneFrame;

        public HashSet<NPCDefinition> NeverDupe = new(new[]
        {
            // EoW parts will become heads if not matched... :( lots of heads
            NPCID.EaterofWorldsBody,
            NPCID.EaterofWorldsTail,
            // Doesn't check AI slot so index out of bounds
            NPCID.MoonLordLeechBlob
        }.Select(t => new NPCDefinition(t)));

        public HashSet<NPCDefinition> InvXFromWorld = new(new[]
        {
            NPCID.LunarTowerNebula, NPCID.LunarTowerSolar, NPCID.LunarTowerStardust, NPCID.LunarTowerVortex
        }.Select(t => new NPCDefinition(t)));

        public HashSet<NPCDefinition> NaturalForceAI = new(new[]
        {
            NPCID.WallofFlesh,
            NPCID.MartianSaucerCore,
            NPCID.Golem // Need to make NewNPC in Golem AI to trigger with Start!
            //NPCID.MoonLordCore // Cannot force AI because it waits before spawning stuff for the drama
        }.Select(t => new NPCDefinition(t)));

        [DefaultValue(true)]
        public bool BossBarSorting;

        public HashSet<NPCDefinition> NPCSuppressBar = new(new[]
        {
            NPCID.GolemHeadFree
        }.Select(t => new NPCDefinition(t)));

        [DefaultValue(false)]
        public bool ShowSpawnsDebug;

        public HashSet<NPCDefinition> NPCToShowSlots = new(new[]
        {
            NPCID.Retinazer, NPCID.Spazmatism,
            NPCID.BrainofCthulhu, //NPCID.Creeper,
            NPCID.Golem, NPCID.GolemFistLeft, NPCID.GolemFistRight, NPCID.GolemHead, NPCID.GolemHeadFree,
            NPCID.MoonLordCore, NPCID.MoonLordHand, NPCID.MoonLordHead,
            NPCID.MartianSaucer, NPCID.MartianSaucerCannon, NPCID.MartianSaucerCore, NPCID.MartianSaucerTurret
        }.Select(t => new NPCDefinition(t)));

        [Header("WoF")]

        [DefaultValue(true)]
        public bool HaveSecondDupWoFCrossing;

        // PROPERTIES
        // @WARNING: Properties get processed AFTER fields, so you get them at the bottom even if you shouldn't

        [JsonIgnore]
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

        [Header("DebugProps")]

        public int NPCLimit => MultiSpawn.maxNPCs;

    }
}
