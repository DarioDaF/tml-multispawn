using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Steamworks;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace MultiSpawn
{
    public class DebugLayer : ModSystem
    {
        //public static string text = "Test text\nMultiline";

        public static int naturalSpawnedMobs = 0;
        public static int oneFrameForcedNatural = 0;
        public static int duplicatedMobs = 0;
        public static int oneFrameMobs = 0;
        public static int oneFrameMobsCrash = 0;
        public static int randomFrameMobs = 0;
        public static int missedDuplications = 0;
        public static int currentMobs = 0;
        public static HashSet<int> crashOffenders = new();

        public class Layer : GameInterfaceLayer
        {
            public Layer() : base("MultiSpawn.DebugLayer", InterfaceScaleType.UI)
            {
            }
            private void DrawShadedText(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 pos, Color c)
            {
                const int off = 2;
                batch.DrawString(font, text, pos + new Vector2(+off, +off), Color.Black);
                batch.DrawString(font, text, pos + new Vector2(+off, -off), Color.Black);
                batch.DrawString(font, text, pos + new Vector2(-off, -off), Color.Black);
                batch.DrawString(font, text, pos + new Vector2(-off, +off), Color.Black);
                batch.DrawString(font, text, pos, c);
            }
            protected override bool DrawSelf()
            {
                DrawShadedText(
                    Main.spriteBatch, FontAssets.MouseText.Value,
                    $"Current: {currentMobs}/{MultiSpawn.maxNPCs}\n" +
                    $"Natural: {naturalSpawnedMobs}\n" +
                    $"OneFrameForcedNatural: {oneFrameForcedNatural}\n" +
                    $"Dups: {duplicatedMobs}\n" +
                    $"OneFrame: {oneFrameMobs}\n" +
                    $"OneFrameCrash: {oneFrameMobsCrash} -> ({string.Join(",", crashOffenders)})\n" +
                    $"RandomFrame: {randomFrameMobs}\n" +
                    $"Missed: {missedDuplications}",
                    new Vector2(25, 70), Color.White
                );
                DrawBossSlots(Main.spriteBatch);
                return true;
            }
            private Tuple<Texture2D, Rectangle> GetNPCImage(int type)
            {
                var texture = TextureAssets.Npc[type].Value;
                int frameCounter = (int)(Main.GameUpdateCount / 8);
                int frames = Main.npcFrameCount[type];
                return Tuple.Create(texture, texture.Frame(1, frames, 0, frameCounter % frames));
            }
            public static void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color)
            {
                var btt = TextureAssets.BlackTile.Value;
                spriteBatch.Draw(btt, rect, color);
            }
            private void DrawBossSlots(SpriteBatch batch)
            {
                const int SIZE = 64;
                var conf = ModContent.GetInstance<MSConfig>();
                var basePos = new Vector2(300, 70);
                int j = 0;
                for (int i = 0; i < MultiSpawn.maxNPCs; ++i)
                {
                    var npc = Main.npc[i];
                    if (!npc.active)
                        continue;
                    /*
                    if (npc.boss != true)
                        continue;
                    */
                    if (!conf.NPCToShowSlots.Contains(new NPCDefinition(npc.type)))
                        continue;

                    var img = GetNPCImage(npc.type);
                    var dest = new Rectangle((int)basePos.X + SIZE * j, (int)basePos.Y, SIZE, SIZE);
                    DrawRect(batch, dest, Color.Black);
                    dest.Inflate(-2, -2);
                    DrawRect(batch, dest, Color.White);
                    batch.Draw(img.Item1, dest, img.Item2, Color.White);
                    DrawShadedText(batch, FontAssets.MouseText.Value, $"{npc.type}", dest.BottomLeft(), Color.White);
                    ++j;
                }
            }
        }
        private static Layer layer = null;

        public override void PostUpdateNPCs()
        {
            currentMobs = 0;
            for (int i = 0; i < MultiSpawn.maxNPCs; i++)
            {
                if (Main.npc[i].active)
                    ++currentMobs;
            }
            var conf = ModContent.GetInstance<MSConfig>();
            if (layer != null)
                layer.Active = conf.ShowSpawnsDebug;
        }
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (layer == null) // This gets called every frame (weird?) don't create a new layer each time
                layer = new Layer();
            layers.Add(layer); // Add as last, don't care about anything
        }
    }
}
