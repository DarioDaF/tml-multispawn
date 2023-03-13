using Microsoft.Xna.Framework;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MultiSpawn
{

    public class TweakWoF : ModSystem
    {
        public override void Load()
        {
            On.Terraria.Main.DrawWoF += Main_DrawWoF;
            On.Terraria.Player.WOFTongue += Player_WOFTongue;
        }

        public override void Unload()
        {
            On.Terraria.Player.WOFTongue -= Player_WOFTongue;
            On.Terraria.Main.DrawWoF -= Main_DrawWoF;
        }

        public struct WoFData : IEquatable<WoFData>
        {
            public static WoFData FromMain()
            {
                return new WoFData()
                {
                    wofNPCIndex = Main.wofNPCIndex,
                    wofDrawAreaTop = Main.wofDrawAreaTop,
                    wofDrawAreaBottom = Main.wofDrawAreaBottom,
                };
            }
            public bool IsValid()
            {
                return (wofNPCIndex >= 0) && (Main.npc[wofNPCIndex].active) && (Main.npc[wofNPCIndex].life > 0);
            }
            public void ToMain()
            {
                Main.wofNPCIndex = this.wofNPCIndex;
                Main.wofDrawAreaTop = this.wofDrawAreaTop;
                Main.wofDrawAreaBottom = this.wofDrawAreaBottom;
            }

            public bool Equals(WoFData other)
            {
                if (!this.IsValid() && !other.IsValid())
                {
                    return true;
                }
                return this.wofNPCIndex == other.wofNPCIndex;
            }

            public int wofNPCIndex;
            public int wofDrawAreaTop;
            public int wofDrawAreaBottom;

        }
        public static HashSet<WoFData> wofData = new(); // (Could as well be a list, BUT out of frame AI advance will mess up the clear!!!)
        public static WoFData? wofTonguer = null;

        public class TweakDrawWoF_GNPC : GlobalNPC
        {
            public override bool InstancePerEntity => true;

            public bool isDuplicate;

            public override void OnSpawn(NPC npc, IEntitySource source)
            {
                isDuplicate = source is MSDuplicateEntitySource msdeSource && msdeSource.cloneNumber == 0;
            }

            public float? oldAIVelX = null; // Must be for this INSTANCE (used on entry!)

            public override bool PreAI(NPC npc)
            {
                if (oldAIVelX.HasValue)
                {
                    npc.velocity.X = oldAIVelX.Value;
                    oldAIVelX = null;
                }
                return true;
            }
            public override void PostAI(NPC npc)
            {
                if (npc.aiStyle == NPCAIStyleID.WallOfFleshMouth)
                {
                    var datum = WoFData.FromMain();
                    if (datum.IsValid())
                        wofData.Add(datum);

                    if (isDuplicate)
                    {
                        var conf = ModContent.GetInstance<MSConfig>();
                        oldAIVelX = npc.velocity.X;
                        npc.velocity.X *= conf.WoFXVelMul;
                    }
                }
            }
        }

        public override void PreUpdateNPCs()
        {
            wofData.Clear();
        }

        private void Main_DrawWoF(On.Terraria.Main.orig_DrawWoF orig, Main self)
        {
            // Clear invalid data  (Could as well be absent, BUT out of frame AI advance will mess up the clear!!!)
            wofData.RemoveWhere(d => !d.IsValid());
            if (!wofData.Any())
            {
                return;
            }

            var conf = ModContent.GetInstance<MSConfig>();
            if (conf.WoFDrawNoClutter)
            {
                // Manual drawing

                var DrawWOFTongueToPlayer = typeof(Main).GetMethod("DrawWOFTongueToPlayer", BindingFlags.Static | BindingFlags.NonPublic);
                var DrawWOFRopeToTheHungry = typeof(Main).GetMethod("DrawWOFRopeToTheHungry", BindingFlags.Static | BindingFlags.NonPublic);
                var DrawWOFBody = typeof(Main).GetMethod("DrawWOFBody", BindingFlags.Static | BindingFlags.NonPublic);

                if (wofTonguer.HasValue && wofTonguer.Value.IsValid())
                {
                    wofTonguer.Value.ToMain();
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active && Main.player[i].tongued && !Main.player[i].dead)
                            DrawWOFTongueToPlayer.Invoke(null, new object[] { i });
                    }
                }


                // Map hungries to WoFs
                var hungryMap = wofData.ToDictionary(d => d, d => new List<int>());

                for (int j = 0; j < Main.maxNPCs; j++)
                {
                    if (Main.npc[j].active && Main.npc[j].aiStyle == NPCAIStyleID.TheHungry)
                    {
                        var hungryX = Main.npc[j].position.X;
                        var nearestWoF = wofData.MinBy( // wofData is NEVER empty here (guard above)
                            datum => Math.Abs(Main.npc[datum.wofNPCIndex].position.X - hungryX) // They are all valid cause check above!
                        );
                        hungryMap[nearestWoF].Add(j);
                    }
                }

                foreach (var datum in wofData)
                {
                    datum.ToMain();
                    foreach (var hungryIdx in hungryMap[datum])
                    {
                        DrawWOFRopeToTheHungry.Invoke(null, new object[] { hungryIdx });
                    }

                    DrawWOFBody.Invoke(null, new object[] { });
                }
            } else
            {
                // Draw the rest
                foreach (var datum in wofData)
                {
                    datum.ToMain();
                    orig(self);
                }
            }
        }

        public override void PreUpdatePlayers()
        {
            // Restore correct WoF so that target pos for tonguing is valid!
            if (wofTonguer.HasValue && wofTonguer.Value.IsValid())
            {
                wofTonguer.Value.ToMain();
            }
            else
            {
                wofTonguer = null;
            }
        }

        private void Player_WOFTongue(On.Terraria.Player.orig_WOFTongue orig, Player self)
        {
            foreach (var datum in wofData)
            {
                // Player.tongued gets updated on buff update so it's not a good test here to find if resulted
                // Check buff 38 then
                var oldTongued = self.HasBuff(BuffID.TheTongue);
                //var oldTongued = self.tongued;
                datum.ToMain();
                orig(self);
                var newTongued = self.HasBuff(BuffID.TheTongue);
                // var newTongued = self.tongued;
                if (newTongued && !oldTongued)
                {
                    wofTonguer = datum;
                }
            }
        }
    }
}
