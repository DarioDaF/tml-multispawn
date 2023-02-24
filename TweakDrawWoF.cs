﻿using Steamworks;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MultiSpawn
{

    public class TweakDrawWoF : ModSystem
    {
        public override void Load()
        {
            On.Terraria.Main.DrawWoF += Main_DrawWoF;
        }

        public override void Unload()
        {
            On.Terraria.Main.DrawWoF -= Main_DrawWoF;
        }

        public struct WoFData
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
            public int wofNPCIndex;
            public int wofDrawAreaTop;
            public int wofDrawAreaBottom;
        }
        public static List<WoFData> wofData = new();

        public class TweakDrawWoF_GNPC : GlobalNPC
        {
            public override void PostAI(NPC npc)
            {
                if (npc.aiStyle == NPCAIStyleID.WallOfFleshMouth)
                {
                    var datum = WoFData.FromMain();
                    if (datum.IsValid())
                        wofData.Add(datum);
                }
            }
        }

        public override void PreUpdateNPCs()
        {
            wofData.Clear();
        }

        private void Main_DrawWoF(On.Terraria.Main.orig_DrawWoF orig, Main self)
        {
            foreach (var datum in wofData)
            {
                datum.ToMain();
                orig(self);
            }

            /*
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && Main.player[i].tongued && !Main.player[i].dead)
                    Main.DrawWOFTongueToPlayer(i);
            }

            for (int j = 0; j < Main.maxNPCs; j++)
            {
                if (Main.npc[j].active && Main.npc[j].aiStyle == NPCAIStyleID.TheHungry)
                    Main.DrawWOFRopeToTheHungry(j);
            }

            Main.DrawWOFBody();
            */
        }
    }
}