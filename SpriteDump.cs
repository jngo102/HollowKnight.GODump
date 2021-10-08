using UnityEngine;

namespace GODump
{
    internal static class SpriteDump
    {
        private static readonly Color[] colors = new Color[4096 * 4096];
        public static void Tk2dFlip(ref Texture2D texture)
        {
            Texture2D flippedTexture = new Texture2D(texture.height, texture.width);
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    flippedTexture.SetPixel(y, x, texture.GetPixel(x, y));
                }
            }

            texture = flippedTexture;
        }

        public static Texture2D FixSpriteSize(this Texture2D texture, RectInt rect, RectInt border)
        {
            Texture2D outTexture = new Texture2D(border.width, border.height);
            outTexture.SetPixels(colors);
            outTexture.SetPixels(rect.x, rect.y, rect.width, rect.height, texture.GetPixels());
            if (GODump.Settings.SpriteBorder)
            {
                for (int x = 0; x < outTexture.width; x++)
                {
                    for (int y = 0; y < outTexture.height; y++)
                    {
                        if (((x == rect.xMin - 1 || x == rect.xMax + 1) && (rect.yMin - 1 <= y && y <= rect.yMax + 1)) ||
                            ((rect.xMin - 1 <= x && x <= rect.xMax + 1) && (y == rect.yMin - 1 || y == rect.yMax + 1)))
                        {
                            outTexture.SetPixel(x, y, Color.red);
                        }
                    }
                }
            }

            outTexture.Apply();
            return outTexture;
        }
    }
}
