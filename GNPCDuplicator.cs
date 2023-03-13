using Terraria.ID;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Microsoft.Xna.Framework;
using System;

namespace MultiSpawn
{

    public class MSDuplicateEntitySource : IEntitySource
    {
        public IEntitySource originSource;
        public int cloneNumber;
        public MSDuplicateEntitySource(IEntitySource originSource, int cloneNumber)
        {
            this.originSource = originSource;
            this.cloneNumber = cloneNumber;
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

            // Get NPC pos before AI (from position = { X - npc.width / 2, Y - npc.height } in NewNPC)
            var npcSpawnPosition = new Vector2(npc.position.X + npc.width / 2, npc.position.Y + npc.height);
            var npcSize = new Vector2(npc.width, npc.height);

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
                var invRelX = conf.InvRelX && !conf.DontInvRelX.Contains(npcDef);
                var invXFromWorld = conf.InvXFromWorld.Contains(npcDef);

                if (npc.type == NPCID.WallofFlesh)
                {
                    // Makes WoF spawn SAMESIDE when multiple clones
                    var wofMoonwalkOverride = (i > 0) && (conf.WoFXVelMul < 0.0f);
                    if (wofMoonwalkOverride)
                    {
                        invRelX = false;
                    }
                    // Makes WoF spawn CROSSING when clone 1
                    var wof1CrossingOverride = (i == 1) && (conf.HaveSecondDupWoFCrossing);
                    if (wof1CrossingOverride)
                    {
                        invRelX = true;
                        invXFromWorld = true;
                    }
                }

                var dupNpcSpawnPosition = npcSpawnPosition;
                if (invRelX)
                {
                    float refX;
                    if (invXFromWorld)
                    {
                        // Main.rightWorld = Main.maxTilesX * 16
                        refX = (Main.rightWorld - Main.leftWorld) / 2;
                    } else
                    {
                        refX = Main.CurrentPlayer.position.X + Main.CurrentPlayer.width / 2;
                    }
                    dupNpcSpawnPosition.X = 2 * refX - dupNpcSpawnPosition.X;
                }
                var off = Main.rand.NextVector2Square(-conf.PositionOffsetRange, conf.PositionOffsetRange);
                
                if (npc.type == NPCID.WallofFlesh || npc.type == NPCID.WallofFleshEye)
                {
                    // Try prevent WoF burrowing
                    off.Y = 0;
                }

                dupNpcSpawnPosition += off;
                // Constraint inside the world!
                dupNpcSpawnPosition.X = Math.Clamp(dupNpcSpawnPosition.X, Main.leftWorld + npcSize.X / 2, Main.rightWorld - npcSize.X / 2);
                dupNpcSpawnPosition.Y = Math.Clamp(dupNpcSpawnPosition.Y, Main.topWorld + npcSize.Y, Main.bottomWorld);

                var dup = NPC.NewNPCDirect(
                    new MSDuplicateEntitySource(source, i),
                    (int)(dupNpcSpawnPosition.X), (int)(dupNpcSpawnPosition.Y),
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
