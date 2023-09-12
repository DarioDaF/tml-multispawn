using Terraria.ID;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using MultiSpawn.BetterCustomBossBars;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using System.Linq;
using MonoMod.RuntimeDetour;

/*

General notes to remember:

typeof(TwinsBigProgressBar), // Searches from start only for the other part
// Getting spawned plain so they are gonna always interleave pick the number correctly? do they pair at that point?
// Only one bar seems the best idea...
// Trigger end of fight only when ALL have been defeated! (like the eater)
typeof(EaterOfWorldsProgressBar), // Same segment count error like brain (MUST BE ONE! cause segments are legit)
// how to count total number??? should add them up when spawning/get max while post update npcs and clear when they no longer live
typeof(BrainOfCthuluBigProgressBar), // To check creeper count max health count wrong (might not even spawn extra cause of NPC.crimsonBoss static)
// Count all creepers toward both bosses! (counting idea like eater?)
// Or just one bar...
typeof(GolemHeadProgressBar), // Can change target and total health count wrong (num)
typeof(MoonLordProgressBar), // Needs fixing the total health count (num)
typeof(PirateShipBigProgressBar), // Can change target! check that it gets overwriting in case it does
typeof(MartianSaucerBigProgressBar), // To fix and try how it is handled in game the boss (you can use localAI, not net safe?!) (you can force natural AI and use order!)

PirateShip kinda works already, should match stuff with forced natural AI?

*/

namespace MultiSpawn
{
    public class TweakMultiBossBar : ModSystem // BigProgressBarSystem edits
    {
        private List<Tuple<IBigProgressBar, BigProgressBarInfo>> bars = new();
        private static Vector2 drawFancyBarOffset = new();
        private ILHook Hook_DFB_TML;

        private static readonly Dictionary<Type, IBigProgressBar> barSwaps = new()
        {
            { typeof(TwinsBigProgressBar), new CollectiveBar(new() { NPCID.Retinazer, NPCID.Spazmatism }) },
            { typeof(EaterOfWorldsProgressBar), new CollectiveMaxBar(new() { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail }, NPCID.EaterofWorldsHead) },
            { typeof(BrainOfCthuluBigProgressBar), new CollectiveMaxBar(new() { NPCID.BrainofCthulhu, NPCID.Creeper }, NPCID.BrainofCthulhu) },
            // NOTE: MartianSaucer should include Core in the types ONLY when in expert?
            // Also MartianSaucer seems to only be AI so no life or anything
            { typeof(MartianSaucerBigProgressBar), new CoreBar(NPCID.MartianSaucerCore, () => {
                var res = new HashSet<int>() { NPCID.MartianSaucerCannon, NPCID.MartianSaucerTurret };
                if (Main.expertMode)
                    res.Add(NPCID.MartianSaucerCore);
                return res;
            }, NPCID.MartianSaucerCore) },
            // NOTE: Golem fists don't count towards life bar
            { typeof(GolemHeadProgressBar), new CoreBar(NPCID.Golem, new HashSet<int>() { NPCID.Golem, NPCID.GolemHead }, NPCID.GolemHead) },
            { typeof(MoonLordProgressBar), new MoonLordBar() }
        };

        private static readonly HashSet<Type> collectiveBars = new()
        {
            // Vanilla bars
            typeof(TwinsBigProgressBar),
            typeof(EaterOfWorldsProgressBar),
            typeof(BrainOfCthuluBigProgressBar),
            // My bars
            typeof(CollectiveBar),
            typeof(CollectiveMaxBar)
        };

        private Dictionary<int, IBigProgressBar> _OldVanillaBossBarMap;

        public override void Load()
        {
            Terraria.GameContent.UI.BigProgressBar.On_BigProgressBarSystem.Update += BigProgressBarSystem_Update;
            Terraria.GameContent.UI.BigProgressBar.On_BigProgressBarSystem.Draw += BigProgressBarSystem_Draw;
            Terraria.GameContent.UI.BigProgressBar.On_BigProgressBarSystem.TryTracking += BigProgressBarSystem_TryTracking;
            Terraria.GameContent.UI.BigProgressBar.IL_BigProgressBarHelper.DrawFancyBar_SpriteBatch_float_float_Texture2D_Rectangle += BigProgressBarHelper_DrawFancyBar_Both;
            Terraria.GameContent.UI.BigProgressBar.IL_BigProgressBarHelper.DrawFancyBar_SpriteBatch_float_float_Texture2D_Rectangle_float_float += BigProgressBarHelper_DrawFancyBar_Both;
            Hook_DFB_TML = new ILHook(
                typeof(BossBarLoader).GetMethod(nameof(BossBarLoader.DrawFancyBar_TML)),
                BigProgressBarHelper_DrawFancyBar_Both
            );

            var vanillaBossBarMapField = typeof(BigProgressBarSystem).GetField("_bossBarsByNpcNetId", BindingFlags.Instance | BindingFlags.NonPublic);
            _OldVanillaBossBarMap = (Dictionary<int, IBigProgressBar>)vanillaBossBarMapField.GetValue(Main.BigBossProgressBar);
            vanillaBossBarMapField.SetValue(Main.BigBossProgressBar, new Dictionary<int, IBigProgressBar>(
                _OldVanillaBossBarMap.Select(old => KeyValuePair.Create(old.Key, barSwaps.GetValueOrDefault(old.Value.GetType(), old.Value)))
            ));
        }

        public override void Unload()
        {
            var vanillaBossBarMapField = typeof(BigProgressBarSystem).GetField("_bossBarsByNpcNetId", BindingFlags.Instance | BindingFlags.NonPublic);
            vanillaBossBarMapField.SetValue(Main.BigBossProgressBar, _OldVanillaBossBarMap);

            Hook_DFB_TML.Dispose();
            Terraria.GameContent.UI.BigProgressBar.IL_BigProgressBarHelper.DrawFancyBar_SpriteBatch_float_float_Texture2D_Rectangle_float_float -= BigProgressBarHelper_DrawFancyBar_Both;
            Terraria.GameContent.UI.BigProgressBar.IL_BigProgressBarHelper.DrawFancyBar_SpriteBatch_float_float_Texture2D_Rectangle -= BigProgressBarHelper_DrawFancyBar_Both;
            Terraria.GameContent.UI.BigProgressBar.On_BigProgressBarSystem.TryTracking -= BigProgressBarSystem_TryTracking;
            Terraria.GameContent.UI.BigProgressBar.On_BigProgressBarSystem.Draw -= BigProgressBarSystem_Draw;
            Terraria.GameContent.UI.BigProgressBar.On_BigProgressBarSystem.Update -= BigProgressBarSystem_Update;
        }

        private void BigProgressBarHelper_DrawFancyBar_Both(ILContext il)
        {
            var cur = new ILCursor(il);
            if (!cur.TryGotoNext(MoveType.Before,
                //i => i.MatchCall<Utils>(nameof(Utils.CenteredRectangle))
                i => i.MatchCall(typeof(Utils).GetMethod(nameof(Utils.CenteredRectangle)))
            ))
                return;
            cur.Remove(); // Replace Utils.CenteredRectangle call
            cur.EmitDelegate(
                (Vector2 center, Vector2 size) =>
                {
                    center += drawFancyBarOffset;
                    // Size changes HERE don't affect anything
                    return Utils.CenteredRectangle(center, size);
                }
            );
        }

        private Tuple<IBigProgressBar, BigProgressBarInfo> BPBSCurrentBar(BigProgressBarSystem self)
        {
            var fCurrentBar = typeof(BigProgressBarSystem).GetField("_currentBar", BindingFlags.NonPublic | BindingFlags.Instance);
            var fInfo = typeof(BigProgressBarSystem).GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance);
            return Tuple.Create((IBigProgressBar)fCurrentBar.GetValue(self), (BigProgressBarInfo)fInfo.GetValue(self));
        }
        private void BPBSCurrentBar(BigProgressBarSystem self, Tuple<IBigProgressBar, BigProgressBarInfo> val)
        {
            var fCurrentBar = typeof(BigProgressBarSystem).GetField("_currentBar", BindingFlags.NonPublic | BindingFlags.Instance);
            var fInfo = typeof(BigProgressBarSystem).GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance);
            fCurrentBar.SetValue(self, val.Item1);
            fInfo.SetValue(self, val.Item2);
        }

        private T CloneObject<T>(T obj)
        {
            var fMC = obj.GetType().GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)fMC.Invoke(obj, new object[] { });
        }

        private int findSimilarBar(Tuple<IBigProgressBar, BigProgressBarInfo> newBar, List<Tuple<IBigProgressBar, BigProgressBarInfo>> bars)
        {
            var t = newBar.Item1.GetType();
            var isCollective = collectiveBars.Contains(t);
            for (int i = 0; i < bars.Count; ++i)
            {
                var bar = bars[i];
                if (bar.Item2.npcIndexToAimAt == newBar.Item2.npcIndexToAimAt)
                    return i;
                if (isCollective && (t == bar.Item1.GetType()))
                    return i;
            }
            return -1;
        }

        private bool BigProgressBarSystem_TryTracking(Terraria.GameContent.UI.BigProgressBar.On_BigProgressBarSystem.orig_TryTracking orig, BigProgressBarSystem self, int npcIndex)
        {
            var valid = orig(self, npcIndex);
            if (valid)
            {
                var newBar = BPBSCurrentBar(self);
                var idx = findSimilarBar(newBar, bars);
                //var barCopy = (IBigProgressBar)newBar.Item1.GetType().GetConstructor(new Type[] { }).Invoke(new object[] { });
                // Should copy properties to avoid "blinking" the bar texture etc...
                var barCopy = CloneObject(newBar.Item1);
                var goodBar = Tuple.Create(barCopy, newBar.Item2);
                if (idx >= 0)
                {
                    bars[idx] = goodBar;
                } else
                {
                    // All the bars are "common" for the same boss, so create appropriate duplicate
                    bars.Add(goodBar);
                }
            }
            return valid;
        }

        private void BigProgressBarSystem_Draw(Terraria.GameContent.UI.BigProgressBar.On_BigProgressBarSystem.orig_Draw orig, BigProgressBarSystem self, SpriteBatch spriteBatch)
        {
            //var fTransformMatrix = typeof(SpriteBatch).GetField("transformMatrix", BindingFlags.NonPublic | BindingFlags.Instance);
            //var oldTM = (Matrix)fTransformMatrix.GetValue(spriteBatch);

            var conf = ModContent.GetInstance<MSConfig>();

            if (conf.BossBarSorting)
            {
                bars.Sort((b1, b2) => b2.Item2.npcIndexToAimAt - b1.Item2.npcIndexToAimAt);
            }

            for (var idx = 0; idx < bars.Count; ++idx)
            {
                BPBSCurrentBar(self, bars[idx]);
                //spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Matrix.CreateTranslation(0, -100*idx, 0));
                //fTransformMatrix.SetValue(spriteBatch, oldTM * Matrix.CreateTranslation(0, -100 * idx, 0));

                var row = (int)(idx / conf.BossBarColumns);
                var col = idx % conf.BossBarColumns;

                var startOfRow = row * conf.BossBarColumns;
                var columnsInRow = Math.Min(conf.BossBarColumns, bars.Count - startOfRow);

                var xSpace = (float)Main.ScreenSize.X / (float)columnsInRow;

                drawFancyBarOffset.X = xSpace * (col + 0.5f) - Main.ScreenSize.X / 2;
                drawFancyBarOffset.Y = -conf.BossBarYOffset * row;
                orig(self, spriteBatch);
                //spriteBatch.End();
            }

            //fTransformMatrix.SetValue(spriteBatch, oldTM);
            drawFancyBarOffset = new();
        }

        /*
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            base.ModifyInterfaceLayers(layers);
            var pbLayer = layers.Find(l => l.Name == "Vanilla: Invasion Progress Bars");
            // Cannot change tranformMatrix :(
        }
        */

        private void MyTryFindingNPCToTrack()
        {
            var conf = ModContent.GetInstance<MSConfig>();

            Rectangle value = new Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);
            value.Inflate(conf.IdleBossBarSeekExtra, conf.IdleBossBarSeekExtra);
            for (int i = 0; i < MultiSpawn.maxNPCs; i++)
            {
                NPC nPC = Main.npc[i];
                if (nPC.active && nPC.Hitbox.Intersects(value))
                {
                    Main.BigBossProgressBar.TryTracking(i);
                }
            }
        }

        private void BigProgressBarSystem_Update(Terraria.GameContent.UI.BigProgressBar.On_BigProgressBarSystem.orig_Update orig, BigProgressBarSystem self)
        {
            var conf = ModContent.GetInstance<MSConfig>();

            if (conf.ActiveBossBarSeek || bars.Count <= 0)
            {
                MyTryFindingNPCToTrack(); // Always? or only when no bars are visible?
            }

            for (var idx = bars.Count - 1; idx >= 0; --idx)
            {
                BPBSCurrentBar(self, bars[idx]);
                orig(self);
                var updatedBar = BPBSCurrentBar(self);

                if (updatedBar.Item1 == null)
                    goto invalidBar;

                var npcDef = new NPCDefinition(Main.npc[updatedBar.Item2.npcIndexToAimAt].type);

                if (conf.NPCSuppressBar.Contains(npcDef))
                    goto invalidBar;

                // If the bar "moved" to overlap another valid bar remove it and replace the old one
                var newIdx = findSimilarBar(updatedBar, bars);
                if ((newIdx >= 0) && (newIdx != idx))
                    goto invalidBar; // Don't replace bar! the other one should take the correct behaviour

                bars[idx] = updatedBar;
                continue;

            invalidBar:
                bars.RemoveAt(idx);
            }

            /*
            // TryFindingNPCToTrack trigger?
            BPBSCurrentBar(self, Tuple.Create(
                (IBigProgressBar)null,
                new BigProgressBarInfo() { npcIndexToAimAt = -1, validatedAtLeastOnce = false }
            ));
            orig(self);
            */
        }
    }
}
