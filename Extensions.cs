using System.IO;
using UnityEngine;

namespace GODump
{
    internal static class Extensions
    {
        public static void SaveToFile(this AudioClip clip, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var fileStream = SaveWav.CreateEmpty(filePath))
            {
                SaveWav.ConvertAndWrite(fileStream, clip);
                SaveWav.WriteHeader(fileStream, clip);
            }
        }

        public static Texture2D Duplicate(this Texture2D texture)
        {
            RenderTexture temporaryTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, temporaryTexture);
            RenderTexture activeTexture = RenderTexture.active;
            RenderTexture.active = temporaryTexture;
            Texture2D outTexture = new Texture2D(texture.width, texture.height);
            outTexture.ReadPixels(new Rect(0, 0, temporaryTexture.width, temporaryTexture.height), 0, 0);
            outTexture.Apply();
            RenderTexture.active = activeTexture;
            RenderTexture.ReleaseTemporary(temporaryTexture);
            return outTexture;
        }

        public static void SaveToFile(this Texture2D texture, string path)
        {
            byte[] buffer = texture.EncodeToPNG();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            using (var binaryWriter = new BinaryWriter(fileStream))
            {
                binaryWriter.Write(buffer);
                binaryWriter.Close();
            }
        }

        public static Texture2D SubTexture(this Texture2D texture, RectInt rect)
        {
            var outTexture = new Texture2D(rect.width, rect.height);
            outTexture.SetPixels(texture.Duplicate().GetPixels(rect.x, rect.y, rect.width, rect.height));
            outTexture.Apply();
            return outTexture;
        }
    }
}
