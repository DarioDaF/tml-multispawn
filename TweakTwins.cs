using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MultiSpawn
{
    class TweakTwins : ModSystem
    {
        public override void Load()
        {
            On.Terraria.NPC.DoDeathEvents_BeforeLoot += NPC_DoDeathEvents_BeforeLoot;
            IL.Terraria.Main.DrawNPCs += Main_DrawNPCs;
            On.Terraria.GameContent.ItemDropRules.Conditions.MissingTwin.CanDrop += MissingTwin_CanDrop;
        }

        public override void Unload()
        {
            On.Terraria.GameContent.ItemDropRules.Conditions.MissingTwin.CanDrop -= MissingTwin_CanDrop;
            IL.Terraria.Main.DrawNPCs -= Main_DrawNPCs;
            On.Terraria.NPC.DoDeathEvents_BeforeLoot -= NPC_DoDeathEvents_BeforeLoot;
        }

        private bool MissingTwin_CanDrop(On.Terraria.GameContent.ItemDropRules.Conditions.MissingTwin.orig_CanDrop orig, Terraria.GameContent.ItemDropRules.Conditions.MissingTwin self, Terraria.GameContent.ItemDropRules.DropAttemptInfo info)
        {
            return !AnyOtherTwin(info.npc);
        }

        private static bool AnyOtherTwin(NPC npc)
        {
            // Called before this "active" is set to false so simulate deactivation to use AnyNPCs
            var oldActive = npc.active;
            npc.active = false;
            var res = NPC.AnyNPCs(NPCID.Retinazer) || NPC.AnyNPCs(NPCID.Spazmatism);
            npc.active = oldActive;
            return res;
        }

        private void NPC_DoDeathEvents_BeforeLoot(On.Terraria.NPC.orig_DoDeathEvents_BeforeLoot orig, Terraria.NPC self, Terraria.Player closestPlayer)
        {
            orig(self, closestPlayer);
            // This bypasses ONLY the bag etc... souls are given anyway?!?! (that is in the condition MissingTwin)
            if (self.type == NPCID.Retinazer || self.type == NPCID.Spazmatism)
            {
                if (AnyOtherTwin(self))
                {
                    self.value = 0;
                    self.boss = false;
                }
            }
        }

        private void Main_DrawNPCs(ILContext il)
        {
            var cur = new ILCursor(il);
            // Logic to draw Twins connections: draw connections only to others and disable flag = true

            // Go to the if containing a match for a Twin with Main.npc[i].type
            // Important that you match the first or you are gonna get into the drawing logic
            int currNPCIndex = -1;
            if (!cur.TryGotoNext(
                i => i.MatchLdsfld<Main>(nameof(Main.npc)),
                i => i.MatchLdloc(out currNPCIndex),
                i => i.MatchLdelemRef(),
                // Checking for a match in any NPC.type with it is fine
                // BUT I NEED THE VAR!
                i => i.MatchLdfld<NPC>(nameof(NPC.type)),
                i => i.MatchLdcI4(NPCID.Retinazer),
                i => i.MatchBneUn(out _) || i.MatchBeq(out _)
            ))
                return;

            // Inhibit the "only once" flag
            int _flagVar = -1;
            if (!cur.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(out _flagVar),
                i => i.MatchBrtrue(out _),
                i => i.MatchLdcI4(1),
                i => i.MatchStloc(_flagVar) // Can you use it inside itself?
            ))
                return;
            // Remove the store
            cur.Index -= 2;
            cur.RemoveRange(2);

            // Match the drawing logic call to scroll the other Twins and get the loop var
            int innerLoopVar = -1;
            if (!cur.TryGotoNext(
                i => i.MatchLdsfld<Main>(nameof(Main.npc)),
                i => i.MatchLdloc(out innerLoopVar),
                i => i.MatchLdelemRef(),
                i => i.MatchLdfld<NPC>(nameof(NPC.type)),
                i => i.MatchLdcI4(NPCID.Retinazer),
                i => i.MatchBneUn(out _) || i.MatchBeq(out _)
            ))
                return;

            // Now go back to the innerLoopVar initialization
            if (!cur.TryGotoPrev(MoveType.Before,
                i => i.MatchLdcI4(0),
                i => i.MatchStloc(innerLoopVar)
            ))
                return;

            // Delete the constant and place the npc index as a start
            cur.Remove();
            cur.Emit(OpCodes.Ldloc, currNPCIndex);
        }
    }
}
