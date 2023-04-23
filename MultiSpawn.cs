using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace MultiSpawn
{
	public class MultiSpawn : Mod
	{
        public override void Load()
        {
            base.Load();

            if (ModLoader.TryGetMod("NPCUnlimiter", out var unlimiter))
			{
                fi_maxNPCs = unlimiter.Code.GetType("NPCUnlimiter.MaxNPCHandler").GetField("maxNPCs");
			}
        }

		private static FieldInfo fi_maxNPCs = null;

        public static int maxNPCs => (int?)fi_maxNPCs?.GetValue(null) ?? Main.maxNPCs;
	}
}
