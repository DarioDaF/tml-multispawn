using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MultiSpawn
{
    public class TweakNPCLimit : ModSystem
    {
        private static int originalMaxNPCs = 0;
        public bool loadedILMaxNpc { get; private set; }

        public override void Load()
        {
            originalMaxNPCs = Main.npc.Length; // 201!!! casue 200 is a dummy???
            loadedILMaxNpc = false;
            IL.Terraria.NPC.NewNPC += NPC_NewNPC;
        }

        private void NPC_NewNPC(ILContext il)
        {
            var cur = new ILCursor(il);
            /*
			var labelDoSpawn = cur.DefineLabel();
            cur.TryGotoNext(MoveType.Before,
				i => i.MatchLdcI4(-1),
				i => i.MatchStloc(out _)
			);
			*/
            /*
			cur.TryGotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>(nameof(Main.npc)),
                i => i.MatchLdloc(out _),
                i => i.MatchNewobj<ù>(),
				i => i.MatchStelemRef()
            );
			*/
            if (!cur.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(-1),
                i => i.MatchStloc(out _)
            ))
                return;
            int localNumVar;
            if (!cur.Prev.MatchStloc(out localNumVar))
                return;
            if (!cur.TryGotoNext(MoveType.Before,
                i => i.MatchLdloc(localNumVar),
                i => i.MatchLdcI4(0),
                i => i.MatchBlt(out _)
            ))
                return;

            // Inputs
            cur.Emit(OpCodes.Ldloc, localNumVar); // num
            cur.Emit(OpCodes.Ldarg, 3); // Type
            cur.Emit(OpCodes.Ldarg, 4); // Start

            cur.EmitDelegate(
                (int num, int Type, int Start) =>
                {
                    if (num >= 0)
                        return num;

                    // Search in the extra slots
                    if (Type >= 0 && NPCID.Sets.SpawnFromLastEmptySlot[Type])
                    {
                        for (int i = Main.npc.Length - 1; i >= originalMaxNPCs; --i)
                        {
                            if (!Main.npc[i].active)
                            {
                                num = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = Math.Max(Start, originalMaxNPCs); i < Main.npc.Length; ++i)
                        {
                            if (!Main.npc[i].active)
                            {
                                num = i;
                                break;
                            }
                        }
                    }

                    return num;
                }
            );

            // Outputs
            cur.Emit(OpCodes.Stloc, localNumVar); // num

            loadedILMaxNpc = true;
        }

        public void AlterNPCLimit(int extraNPCs)
        {
            if (originalMaxNPCs <= 0)
            {
                return;
            }
            /*
            var newMaxNPCs = originalMaxNPCs + extraNPCs;
            //Main.maxNPCs = newMaxNPCs; // It's a const...
            var newNpcArr = new NPC[newMaxNPCs];
            int copyLen = Math.Min(Main.npc.Length, newNpcArr.Length);
            Main.npc.AsSpan(0, copyLen).CopyTo(newNpcArr);
            for (int i = copyLen; i < Main.npc.Length; ++i)
            {
                // Destroy?
            }
            for (int i = copyLen; i < newNpcArr.Length; ++i)
            {
                newNpcArr[i] = new NPC();
                newNpcArr[i].whoAmI = i;
            }
            Main.npc = newNpcArr;
            */
            Array.Resize(ref Main.npc, originalMaxNPCs + extraNPCs);
        }
        public override void Unload()
        {
            var conf = ModContent.GetInstance<MSConfig>();
            if (conf.ExtraNPCs != 0)
            {
                AlterNPCLimit(0); // Restore NPC limit
            }
            IL.Terraria.NPC.NewNPC -= NPC_NewNPC;
            loadedILMaxNpc = false;
        }
    }
}
