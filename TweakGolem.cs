using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MultiSpawn
{
    class TweakGolem : ModSystem
    {
        public override void Load()
        {
            //On.Terraria.NPC.NewNPC += NPC_NewNPC;
            On.Terraria.Main.DrawNPCs += Main_DrawNPCs;
        }

        public override void Unload()
        {
            On.Terraria.Main.DrawNPCs -= Main_DrawNPCs;
            //On.Terraria.NPC.NewNPC -= NPC_NewNPC;
        }

        private void Main_DrawNPCs(On.Terraria.Main.orig_DrawNPCs orig, Main self, bool behindTiles)
        {
            List<int> wasVisible = new();
            if (!behindTiles)
            {
                // Draw Golem BELOW others (even if they are in the wrong position)
                // Do this only for Golem and in a fast way so not to load every frame
                for (int i = 0; i < MultiSpawn.maxNPCs; ++i)
                {
                    var npc = Main.npc[i];
                    if (npc.active && npc.type == NPCID.Golem && !npc.hide)
                    {
                        self.DrawNPCCheckAlt(npc);
                        self.DrawNPC(i, behindTiles);
                        npc.hide = true;
                        wasVisible.Add(i);
                    }
                }
            }
            orig(self, behindTiles);
            foreach (var i in wasVisible)
            {
                Main.npc[i].hide = false;
            }
        }
        public override void SetStaticDefaults()
        {
            NPCID.Sets.SpawnFromLastEmptySlot[NPCID.Golem] = false;
        }

        private int NPC_NewNPC(On.Terraria.NPC.orig_NewNPC orig, IEntitySource source, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target)
        {
            if (Start == 0)
            {
                var parentNPC = (source as EntitySource_Parent)?.Entity as NPC;
                if (parentNPC is not null && parentNPC.type == NPCID.Golem)
                {
                    Start = parentNPC.whoAmI;
                }
            }
            return orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
        }

    }
}
