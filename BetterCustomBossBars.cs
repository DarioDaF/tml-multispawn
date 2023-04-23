using Terraria.ID;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using System.Reflection;

namespace MultiSpawn.BetterCustomBossBars
{
    public static class MyBarHelper
    {
        private static NPC _dummy = new NPC();
        public static int GetDummyMaxLife(int type, NPCSpawnParams spawnParams = default)
        {
            _dummy.SetDefaults(type, spawnParams);
            return _dummy.lifeMax;
        }
        public static int GetDummyMaxLife(int type, NPC relative = null)
        {
            return GetDummyMaxLife(type, relative?.GetMatchingSpawnParams() ?? default);
        }
        public static int FindCore(int coreType, int npcIndexToAimAt)
        {
            for (int res = npcIndexToAimAt; res >= 0; --res)
            {
                var npc = Main.npc[res];
                if (npc.active && npc.type == coreType)
                {
                    return res;
                }
            }
            return -1;
        }
        public static (int, int) LifeOfCore(HashSet<int> types, int npcIndexToAimAt = -1)
        {
            int life = 0;
            int lifeMax = 0;
            int coreType = int.MinValue;
            if (npcIndexToAimAt >= 0)
            {
                coreType = Main.npc[npcIndexToAimAt].type;
            }
            for (int i = Math.Max(npcIndexToAimAt, 0); i < MultiSpawn.maxNPCs; ++i)
            {
                var npc = Main.npc[i];
                if (npc.active)
                {
                    if ((i != npcIndexToAimAt) && (npc.type == coreType))
                        break;
                    if (types.Contains(npc.type))
                    {
                        life += npc.life;
                        lifeMax += npc.lifeMax;
                    }
                }
            }
            return (life, lifeMax);
        }
    }

    // Good for TwinsBigProgressBar
    public class CollectiveBar : IBigProgressBar
    {
        private float _lifePercentToShow;
        private int _headIndex;

        private HashSet<int> types;
        public CollectiveBar(HashSet<int> types)
        {
            this.types = types;
        }

        public bool ValidateAndCollectNecessaryInfo(ref BigProgressBarInfo info)
        {
            if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt > MultiSpawn.maxNPCs)
                return false;

            NPC npc = Main.npc[info.npcIndexToAimAt];
            if (!npc.active) // Should check npc type?
                return false;

            var (life, lifeMax) = MyBarHelper.LifeOfCore(this.types);

            _lifePercentToShow = Utils.Clamp((float)life / (float)lifeMax, 0f, 1f);
            _headIndex = npc.GetBossHeadTextureIndex();
            return true;
        }

        public void Draw(ref BigProgressBarInfo info, SpriteBatch spriteBatch)
        {
            Texture2D value = TextureAssets.NpcHeadBoss[_headIndex].Value;
            Rectangle barIconFrame = value.Frame();
            BigProgressBarHelper.DrawFancyBar(spriteBatch, _lifePercentToShow, value, barIconFrame);
        }
    }

    // Good for EaterOfWorldsProgressBar and BrainOfCthuluBigProgressBar
    public class CollectiveMaxBar : IBigProgressBar
    {
        private float _lifeMax;
        private float _lifePercentToShow;

        private HashSet<int> types;
        private int npcHeadType;
        public CollectiveMaxBar(HashSet<int> types, int npcHeadType)
        {
            this.types = types;
            this.npcHeadType = npcHeadType;
            this._lifeMax = 0;
        }

        public bool ValidateAndCollectNecessaryInfo(ref BigProgressBarInfo info)
        {
            if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt > MultiSpawn.maxNPCs)
                goto invalidBar;

            NPC npc = Main.npc[info.npcIndexToAimAt];
            if (!npc.active) // Should check npc type?
                goto invalidBar;

            var (life, lifeMax) = MyBarHelper.LifeOfCore(this.types);
            if (this._lifeMax < lifeMax)
            {
                this._lifeMax = lifeMax;
            }

            _lifePercentToShow = Utils.Clamp((float)life / (float)this._lifeMax, 0f, 1f);
            return true;

        invalidBar:
            this._lifeMax = 0;
            return false;
        }

        public void Draw(ref BigProgressBarInfo info, SpriteBatch spriteBatch)
        {
            var headIndex = NPCID.Sets.BossHeadTextures[this.npcHeadType];
            Texture2D value = TextureAssets.NpcHeadBoss[headIndex].Value;
            Rectangle barIconFrame = value.Frame();
            BigProgressBarHelper.DrawFancyBar(spriteBatch, _lifePercentToShow, value, barIconFrame);
        }
    }

    // Good for MartianSaucerBigProgressBar (with natural AI force) and GolemHeadProgressBar (with tweaks for correct order)
    public class CoreBar : IBigProgressBar
    {
        private float _lifePercentToShow;

        private int coreType;
        //private HashSet<int> types;
        private Func<HashSet<int>> typesFunc;
        private int npcHeadType;
        public CoreBar(int coreType, HashSet<int> types, int npcHeadType) : this(coreType, () => types, npcHeadType)
        {
        }

        public CoreBar(int coreType, Func<HashSet<int>> types, int npcHeadType)
        {
            this.coreType = coreType;
            this.typesFunc = types;
            this.npcHeadType = npcHeadType;
        }

        public bool ValidateAndCollectNecessaryInfo(ref BigProgressBarInfo info)
        {
            if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt > MultiSpawn.maxNPCs)
                return false;

            NPC npc = Main.npc[info.npcIndexToAimAt];
            if (!npc.active || npc.type != this.coreType)
            {
                info.npcIndexToAimAt = MyBarHelper.FindCore(this.coreType, info.npcIndexToAimAt);
                if (info.npcIndexToAimAt < 0)
                    return false;
                npc = Main.npc[info.npcIndexToAimAt];
            }

            var types = typesFunc();
            var (life, lifeMax) = MyBarHelper.LifeOfCore(types, info.npcIndexToAimAt);
            
            _lifePercentToShow = Utils.Clamp((float)life / (float)lifeMax, 0f, 1f);
            return true;
        }

        public void Draw(ref BigProgressBarInfo info, SpriteBatch spriteBatch)
        {
            var headIndex = NPCID.Sets.BossHeadTextures[this.npcHeadType];
            Texture2D value = TextureAssets.NpcHeadBoss[headIndex].Value;
            Rectangle barIconFrame = value.Frame();
            BigProgressBarHelper.DrawFancyBar(spriteBatch, _lifePercentToShow, value, barIconFrame);
        }
    }

    // Custom bar for moonlord using AI crawling
    public class MoonLordBar : IBigProgressBar
    {
        private float _lifePercentToShow;

        private static readonly HashSet<int> _CHILDRENTYPES = new()
        {
            NPCID.MoonLordHand,
            NPCID.MoonLordHead
        };
        enum _AI0_Phase
        {
            HandsAndEyePhase = 0,
            CorePhase = 1
        }

        private static int GetCore(NPC npc)
        {
            if (!npc.active)
                return -1;
            if (npc.type == NPCID.MoonLordCore)
            {
                if (IsInBadAI(npc))
                    return -1;
                return npc.whoAmI;
            }
            if (_CHILDRENTYPES.Contains(npc.type))
            {
                var coreIdx = (int)npc.ai[3];
                var coreNPC = Main.npc[coreIdx];
                if (coreNPC.active && coreNPC.type == NPCID.MoonLordCore)
                    return coreIdx;
            }
            return -1;
        }
        private static IEnumerable<int> GetParts(NPC coreNPC)
        {
            if (!coreNPC.active || coreNPC.type != NPCID.MoonLordCore || IsInBadAI(coreNPC))
                yield break;

            yield return coreNPC.whoAmI;
            if (coreNPC.ai[0] == (int)_AI0_Phase.HandsAndEyePhase || coreNPC.ai[0] == (int)_AI0_Phase.CorePhase)
            {
                for (int i = 0; i < 3; ++i)
                {
                    var childIdx = (int)coreNPC.localAI[i];
                    var childNPC = Main.npc[childIdx];
                    if (childNPC.active && _CHILDRENTYPES.Contains(childNPC.type) && !IsInBadAI(childNPC))
                        yield return childIdx;
                }
            }
        }

        private static Func<NPC, bool> _badAI_Delegate = null;
        private static bool IsInBadAI(NPC npc)
        {
            //BigProgressBarSystem._moonlordBar.IsInBadAI
            if (_badAI_Delegate is null)
            {
                var _moonlordBar = (MoonLordProgressBar)typeof(BigProgressBarSystem).GetField("_moonlordBar", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                _badAI_Delegate = typeof(MoonLordProgressBar).GetMethod("IsInBadAI", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate<Func<NPC, bool>>(_moonlordBar);
            }

            var res = _badAI_Delegate(npc);
            return res;
        }

        public bool ValidateAndCollectNecessaryInfo(ref BigProgressBarInfo info)
        {
            if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt > MultiSpawn.maxNPCs)
                return false;

            info.npcIndexToAimAt = GetCore(Main.npc[info.npcIndexToAimAt]);
            if (info.npcIndexToAimAt < 0)
                return false;

            NPC coreNPC = Main.npc[info.npcIndexToAimAt];

            int life = 0;
            int lifeMax = 0;
            foreach (var idx in GetParts(coreNPC))
            {
                var npc = Main.npc[idx];
                life += npc.life;
                lifeMax += npc.lifeMax;
            }

            _lifePercentToShow = Utils.Clamp((float)life / (float)lifeMax, 0f, 1f);
            return true;
        }

        public void Draw(ref BigProgressBarInfo info, SpriteBatch spriteBatch)
        {
            var headIndex = NPCID.Sets.BossHeadTextures[NPCID.MoonLordHead];
            Texture2D value = TextureAssets.NpcHeadBoss[headIndex].Value;
            Rectangle barIconFrame = value.Frame();
            BigProgressBarHelper.DrawFancyBar(spriteBatch, _lifePercentToShow, value, barIconFrame);
        }
    }

}
