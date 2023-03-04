using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace MultiSpawn
{

    public class MSDuplicateEntitySource : IEntitySource
    {
        public IEntitySource originSource;
        public MSDuplicateEntitySource(IEntitySource originSource)
        {
            this.originSource = originSource;
        }
        public string Context => "MultiSpawn: Duplicate Entity (" + originSource.Context + ")";
    }

    public class GNPCDuplicator : GlobalNPC
    {
        //public static bool reentrant = false;
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            base.OnSpawn(npc, source);

            if (source is MSDuplicateEntitySource)
                return;

            DebugLayer.naturalSpawnedMobs += 1;

            var conf = ModContent.GetInstance<MSConfig>();

            var npcDef = new NPCDefinition(npc.type);

            if (conf.NaturalForceAI.Contains(npcDef))
            {
                npc.AI(); // Force AI tick
                if (!npc.active)
                {
                    DebugLayer.oneFrameForcedNatural += 1;
                    return;
                }
            }

            if (conf.NeverDupe.Contains(npcDef))
                return;

            if (conf.AvoidAISpawnDuplication)
            {
                // AI spawned enemies will most probably not link back correctly (unless you copy them after AI linking and work around it)
                // So they just fill up the NPC limit probably despawning the next frame due to the boss not setting them
                var parentSource = source as EntitySource_Parent;
                if ((parentSource != null) && (parentSource.Entity is NPC))
                    return;
            }
            /*
			if (reentrant)
				return;
			reentrant = true;
			*/
            var spawns = Main.rand.Next(conf.MinSpawns, conf.MaxSpawns + 1);

            for (int i = 0; i < spawns; ++i)
            {
                var npcX = npc.position.X + npc.width / 2;
                if (conf.InvRelX && !conf.DontInvRelX.Contains(npcDef))
                {
                    float refX;
                    if (conf.InvXFromWorld.Contains(npcDef))
                    {
                        // Main.rightWorld = Main.maxTilesX * 16
                        refX = (Main.rightWorld - Main.leftWorld) / 2;
                    } else
                    {
                        refX = Main.CurrentPlayer.position.X + Main.CurrentPlayer.width / 2;
                    }
                    npcX = 2 * refX - npcX;
                }
                var off = Main.rand.NextVector2Square(-conf.PositionOffsetRange, conf.PositionOffsetRange);
                var dup = NPC.NewNPCDirect(
                    new MSDuplicateEntitySource(source),
                    (int)(npcX + off.X), (int)(npc.position.Y + npc.height + off.Y),
                    npc.type,
                    0,
                    npc.ai[0], npc.ai[1], npc.ai[2], npc.ai[3],
                    npc.target
                );
                if (dup.active) // If couldn't spawn don't do stuff
                {
                    if (conf.TryAIOneFrame)
                    {
                        try
                        {
                            dup.AI(); // Force AI tick to kill if was spurious
                        }
                        catch
                        {
                            DebugLayer.oneFrameMobsCrash += 1;
                            DebugLayer.crashOffenders.Add(dup.type);
                            dup.active = false;
                        }
                        /*
                        if (dup.active)
                        {
                            dup.active = false; // The AI modeled one could be messed up now, remake it
                            dup = NPC.NewNPCDirect(
                                new MSDuplicateEntitySource(source),
                                (int)(npcX + off.X), (int)(npc.position.Y + npc.height + off.Y),
                                npc.type,
                                0,
                                npc.ai[0], npc.ai[1], npc.ai[2], npc.ai[3],
                                npc.target
                            );
                        }
                        */
                        if (dup.active)
                        {
                            var tickFactor = Main.rand.Next(conf.AITickFactorMin, conf.AITickFactorMax);
                            for (int tick = 0; dup.active && (tick < tickFactor); ++tick)
                            {
                                dup.AI();
                            }
                            if (!dup.active)
                            {
                                DebugLayer.randomFrameMobs += 1;
                            }
                        }
                    }
                    if (dup.active)
                    {
                        DebugLayer.duplicatedMobs += 1;
                    } else
                    {
                        DebugLayer.oneFrameMobs += 1;
                    }
                } else
                {
                    DebugLayer.missedDuplications += 1;
                }
            }
            //reentrant = false;
        }
    }
}
