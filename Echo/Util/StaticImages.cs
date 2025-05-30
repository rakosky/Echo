using System.Drawing;

namespace Echo.Util
{
    public static class StaticImages
    {
        public static readonly Bitmap RuneCdImgTop;
        public static readonly Bitmap RuneCdImgBot;

        public static readonly Bitmap MapImageTopLeft;
        public static readonly Bitmap MapImageBottomRight;

        public static readonly Bitmap RespawnImg;

        static StaticImages()
        {
            try
            {
                RuneCdImgTop = (Bitmap)Image.FromFile(@"imgs/rune_cd_icon_top.png");
                RuneCdImgBot = (Bitmap)Image.FromFile(@"imgs/rune_cd_icon_bot.png");

                MapImageTopLeft = (Bitmap)Image.FromFile(@"imgs/minimap_tl.png");
                MapImageBottomRight = (Bitmap)Image.FromFile(@"imgs/minimap_br.png");

                RespawnImg = (Bitmap)Image.FromFile(@"imgs/respawnok.png");
            }
            catch (Exception ex)
            {
                // Optional: log or rethrow with more context
                throw new InvalidOperationException("Failed to load one or more static images.", ex);
            }
        }
    }

}
