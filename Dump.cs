using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace  GODump
{
    public class Dump : MonoBehaviour
    {
        private static readonly string _dllFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string _spritesPath = Path.Combine(_dllFolder, "Sprites");
        private static readonly string _atlasesPath = Path.Combine(_dllFolder, "Atlases");
        private static readonly string _audioPath = Path.Combine(_dllFolder, "Audio");

        private tk2dSpriteAnimation[] _animations = { };
        private tk2dSpriteCollectionData[] _collections = { };
        private Dictionary<GameObject, List<AudioClip>> _gameObjectsWithAudio;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                StartCoroutine(DumpAtlases());
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                _collections = Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>().ToArray();
                _animations = Resources.FindObjectsOfTypeAll<tk2dSpriteAnimation>().ToArray();
                GODump.Settings.AnimationsToDump = string.Join("|", _animations.Select(animation => animation.name));

                _gameObjectsWithAudio = new();

                foreach (GameObject go in FindObjectsOfType<GameObject>(true).Where(go => go.GetComponent<AudioSource>()))
                {
                    bool skipIteration = false;
                    foreach (KeyValuePair<GameObject, List<AudioClip>> pair in _gameObjectsWithAudio)
                    {
                        if (pair.Key.name == go.name)
                        {
                            skipIteration = true;
                            break;
                        }
                    }

                    if (skipIteration) continue;

                    _gameObjectsWithAudio.Add(go, new());
                    if (go.GetComponent<PlayMakerFSM>())
                    {
                        foreach (PlayMakerFSM fsm in go.GetComponents<PlayMakerFSM>())
                        {
                            foreach (FsmState state in fsm.FsmStates)
                            {
                                foreach (FsmStateAction action in state.Actions)
                                {
                                    switch (action)
                                    {
                                        case AudioPlay audioPlay:
                                            _gameObjectsWithAudio[go].Add(audioPlay.oneShotClip.Value as AudioClip);
                                            break;
                                        case AudioPlayRandom audioPlayRandom:
                                            audioPlayRandom.audioClips.ToList().ForEach(clip => _gameObjectsWithAudio[go].Add(clip));
                                            break;
                                        case AudioPlaySimple audioPlaySimple:
                                            _gameObjectsWithAudio[go].Add(audioPlaySimple.oneShotClip.Value as AudioClip);
                                            break;
                                        case AudioPlayerOneShot audioPlayerOneShot:
                                            audioPlayerOneShot.audioClips.ToList().ForEach(clip => _gameObjectsWithAudio[go].Add(clip));
                                            break;
                                        case AudioPlayerOneShotSingle audioPlayerOneShotSingle:
                                            _gameObjectsWithAudio[go].Add(audioPlayerOneShotSingle.audioClip.Value as AudioClip);
                                            break;
                                        case AudioPlayRandomSingle audioPlayRandomSingle:
                                            _gameObjectsWithAudio[go].Add(audioPlayRandomSingle.audioClip.Value as AudioClip);
                                            break;
                                        case AudioPlayV2 audioPlayV2:
                                            _gameObjectsWithAudio[go].Add(audioPlayV2.oneShotClip.Value as AudioClip);
                                            break;
                                        case PlayRandomSound playRandomSound:
                                            playRandomSound.audioClips.ToList().ForEach(clip => _gameObjectsWithAudio[go].Add(clip));
                                            break;
                                        case PlaySound playSound:
                                            _gameObjectsWithAudio[go].Add(playSound.clip.Value as AudioClip);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        AudioClip clip = go.GetComponent<AudioSource>().clip;
                        if (clip != null)
                            _gameObjectsWithAudio[go].Add(clip);
                    }
                }

                GODump.Settings.AudioClipsToDump = string.Join("|", _gameObjectsWithAudio.Keys.Select(go => go.name));
                GODump.Instance.SaveSettings();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                GODump.Instance.LoadSettings();
                _animations = Resources.FindObjectsOfTypeAll<tk2dSpriteAnimation>().Where(animation => GODump.Settings.AnimationsToDump.Split('|').Contains(animation.name)).ToArray();
                StartCoroutine(DumpSprites());
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                GODump.Instance.LoadSettings();
                Dictionary<GameObject, List<AudioClip>> gameObjectsWithAudio = new();
                foreach (KeyValuePair<GameObject, List<AudioClip>> pair in _gameObjectsWithAudio)
                {
                    GameObject go = pair.Key;
                    if (!GODump.Settings.AudioClipsToDump.Contains(go.name)) continue;

                    gameObjectsWithAudio.Add(go, pair.Value);
                }

                _gameObjectsWithAudio = gameObjectsWithAudio;
                StartCoroutine(DumpAudio());
            }
        }

        private IEnumerator DumpAtlases()
        {
            GODump.Instance.Log("Begin dumping atlases.");
            int num = 0;
            _collections = Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>().ToArray();
            GODump.Instance.Log("Found " + _collections.Length + " collections.");
            foreach (tk2dSpriteCollectionData collection in _collections)
            {
                if (collection.allowMultipleAtlases && collection.textures.Length > 1)
                {
                    GODump.Instance.LogWarn("Collection " + collection.name + "has multiple textures.");
                    yield return new WaitForSeconds(0.5f);
                }

                num++;
                Texture2D temporaryTexture = ((Texture2D)collection.textures[0]).Duplicate();
                temporaryTexture.SaveToFile(Path.Combine(_atlasesPath, string.Join("@", GetUsages(collection, _animations).Select(animation => animation.name).ToArray()) + "#" + collection.name + ".png"));
                DestroyImmediate(temporaryTexture);
                yield return new WaitForSeconds(0.5f);
            }

            GODump.Instance.Log($"End dumping {num} atlases.");
        }

        private IEnumerator DumpSprites()
        {
            int num = 0;
            foreach (var animation in _animations)
            {
                int i = 0;
                var spriteInfo = new SpriteInfo();
                GODump.Instance.Log("Begin dumping sprites in tk2dSpriteAnimator [" + animation.name + "].");
                foreach (tk2dSpriteAnimationClip clip in animation.clips)
                {
                    i++;
                    int j = -1;
                    float Xmax = -10000f;
                    float Ymax = -10000f;
                    float Xmin = 10000f;
                    float Ymin = 10000f;
                    foreach (tk2dSpriteAnimationFrame frame in clip.frames)
                    {
                        tk2dSpriteDefinition tk2DSpriteDefinition = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                        Vector3[] position = tk2DSpriteDefinition.positions;

                        float xMin = position.Min(v => v.x);
                        float yMin = position.Min(v => v.y);
                        float xMax = position.Max(v => v.x);
                        float yMax = position.Max(v => v.y);

                        Xmin = Xmin < xMin ? Xmin : xMin;
                        Ymin = Ymin < yMin ? Ymin : yMin;
                        Xmax = Xmax > xMax ? Xmax : xMax;
                        Ymax = Ymax > yMax ? Ymax : yMax;

                    }
                    foreach (tk2dSpriteAnimationFrame frame in clip.frames)
                    {
                        j++;

                        tk2dSpriteDefinition tk2dSpriteDefinition = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                        Vector2[] uv = tk2dSpriteDefinition.uvs;
                        Vector3[] pos = tk2dSpriteDefinition.positions;
                        Texture texture = tk2dSpriteDefinition.material.mainTexture;
                        Texture2D texture2D = ((Texture2D)texture).Duplicate();

                        string collectionName = frame.spriteCollection.spriteCollectionName;
                        string atlasPath = Path.Combine(_spritesPath, animation.name, "0.Atlases", $"{collectionName}.png");
                        string path1 = Path.Combine(_spritesPath, animation.name, string.Format("{0:D3}", i) + "." + clip.name, string.Format("{0:D3}", i) + "-" + string.Format("{0:D2}", j) + "-" + string.Format("{0:D3}", frame.spriteId) + "_position.png");
                        string framePathRelative = Path.Combine(animation.name, string.Format("{0:D3}", i) + "." + clip.name, string.Format("{0:D3}", i) + "-" + string.Format("{0:D2}", j) + "-" + string.Format("{0:D3}", frame.spriteId) + ".png");
                        string framePath = Path.Combine(_spritesPath, framePathRelative);

                        bool flipped = tk2dSpriteDefinition.flipped == tk2dSpriteDefinition.FlipMode.Tk2d;

                        float xmin = pos.Min(v => v.x);
                        float ymin = pos.Min(v => v.y);
                        float xmax = pos.Max(v => v.x);
                        float ymax = pos.Max(v => v.y);

                        int x1 = (int)(uv.Min(v => v.x) * texture2D.width);
                        int y1 = (int)(uv.Min(v => v.y) * texture2D.height);
                        int x2 = (int)(uv.Max(v => v.x) * texture2D.width);
                        int y2 = (int)(uv.Max(v => v.y) * texture2D.height);

                        // symmetry transformation
                        int x11 = x1;
                        int y11 = y1;
                        int x22 = x2;
                        int y22 = y2;
                        if (flipped)
                        {
                            x22 = y2 + x1 - y1;
                            y22 = x2 - x1 + y1;
                        }

                        int x3 = (int)((Xmin - Xmin) / tk2dSpriteDefinition.texelSize.x);
                        int y3 = (int)((Ymin - Ymin) / tk2dSpriteDefinition.texelSize.y);
                        int x4 = (int)((Xmax - Xmin) / tk2dSpriteDefinition.texelSize.x);
                        int y4 = (int)((Ymax - Ymin) / tk2dSpriteDefinition.texelSize.y);

                        int x5 = (int)((xmin - Xmin) / tk2dSpriteDefinition.texelSize.x);
                        int y5 = (int)((ymin - Ymin) / tk2dSpriteDefinition.texelSize.y);
                        int x6 = (int)((xmax - Xmin) / tk2dSpriteDefinition.texelSize.x);
                        int y6 = (int)((ymax - Ymin) / tk2dSpriteDefinition.texelSize.y);

                        var uvPixel = new RectInt(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
                        var posBorder = new RectInt(x11 - x5 + x3, y11 - y5 + y3, x4 - x3 + 1, y4 - y3 + 1);
                        var uvPixelR = new RectInt(x5 - x3, y5 - y3, x22 - x11 + 1, y22 - y11 + 1);


                        if (!File.Exists(atlasPath))
                        {
                            texture2D.SaveToFile(atlasPath);
                            num++;
                        }

                        if (GODump.Settings.DumpSpriteInfo)
                        {
                            spriteInfo.Add(frame.spriteId, x1, y1, uvPixelR.x, uvPixelR.y, uvPixelR.width, uvPixelR.height, collectionName, framePathRelative, flipped);
                        }

                        if (!File.Exists(framePath))
                        {
                            try
                            {
                                Texture2D subTexture = texture2D.SubTexture(uvPixel);
                                if (flipped)
                                {
                                    SpriteDump.Tk2dFlip(ref subTexture);
                                }
                                if (GODump.Settings.FixSpriteSize)
                                {
                                    Texture2D fixedTexture = SpriteDump.FixSpriteSize(subTexture, uvPixelR, posBorder);
                                    fixedTexture.SaveToFile(framePath);
                                    DestroyImmediate(fixedTexture);
                                }
                                else
                                {
                                    subTexture.SaveToFile(framePath);
                                }

                                DestroyImmediate(subTexture);
                                num++;
                            }
                            catch (Exception e)
                            {
                                GODump.Instance.LogError("Could not subtexture: " + e);
                            }
                        }

                        DestroyImmediate(texture2D);
                    }

                    yield return new WaitForSeconds(0.5f);

                    if (GODump.Settings.DumpAnimInfo && clip.frames.Length > 0)
                    {
                        string animInfoPath = Path.Combine(_spritesPath, animation.name, string.Format("{0:D3}", i) + "." + clip.name, "AnimInfo.json");
                        Directory.CreateDirectory(Path.GetDirectoryName(animInfoPath));
                        var animInfo = new AnimationInfo(clip.frames.Length, clip.fps, clip.wrapMode, clip.loopStart, clip.frames[0].spriteCollection.spriteCollectionName);
                        using (FileStream fileStream = File.Create(animInfoPath))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(fileStream))
                            {
                                string value = JsonConvert.SerializeObject(animInfo, Formatting.Indented);
                                streamWriter.Write(value);
                            }
                        }
                    }
                }

                if (GODump.Settings.DumpSpriteInfo)
                {
                    string spriteInfoPath = Path.Combine(_spritesPath, animation.name, "0.Atlases", "SpriteInfo.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(spriteInfoPath));
                    using (FileStream fileStream = File.Create(spriteInfoPath))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(fileStream))
                        {
                            string value = JsonConvert.SerializeObject(spriteInfo, Formatting.Indented);
                            streamWriter.Write(value);
                        }
                    }
                }

                GODump.Instance.Log("End dumping sprites in tk2dSpriteAnimator [" + animation.name + "].");
            }

            GODump.Instance.Log($"End dumping {num} sprites.");
        }
        public static IEnumerator DumpSpriteInUExplorer(tk2dSpriteAnimation animation,string dumppath )
        {
            /*locate tk2dSpriteAnimation in UExplorer and use{ GameManager.instance.StartCoroutine(GODump.Dump.DumpSpriteInUExplorer((tk2dSpriteAnimation)CurrentTarget,<dumppath>))} to dump it by using Unity Explorer console;
             */
            int num = 0;
                int i = 0;
                var spriteInfo = new SpriteInfo();
                GODump.Instance.Log("Begin dumping sprites in tk2dSpriteAnimator [" + animation.name + "].");
                foreach (tk2dSpriteAnimationClip clip in animation.clips)
                {
                    i++;
                    int j = -1;
                    float Xmax = -10000f;
                    float Ymax = -10000f;
                    float Xmin = 10000f;
                    float Ymin = 10000f;
                    foreach (tk2dSpriteAnimationFrame frame in clip.frames)
                    {
                        tk2dSpriteDefinition tk2DSpriteDefinition = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                        Vector3[] position = tk2DSpriteDefinition.positions;

                        float xMin = position.Min(v => v.x);
                        float yMin = position.Min(v => v.y);
                        float xMax = position.Max(v => v.x);
                        float yMax = position.Max(v => v.y);

                        Xmin = Xmin < xMin ? Xmin : xMin;
                        Ymin = Ymin < yMin ? Ymin : yMin;
                        Xmax = Xmax > xMax ? Xmax : xMax;
                        Ymax = Ymax > yMax ? Ymax : yMax;

                    }
                    foreach (tk2dSpriteAnimationFrame frame in clip.frames)
                    {
                        j++;

                        tk2dSpriteDefinition tk2dSpriteDefinition = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                        Vector2[] uv = tk2dSpriteDefinition.uvs;
                        Vector3[] pos = tk2dSpriteDefinition.positions;
                        Texture texture = tk2dSpriteDefinition.material.mainTexture;
                        Texture2D texture2D = ((Texture2D)texture).Duplicate();

                        string collectionName = frame.spriteCollection.spriteCollectionName;
                        string atlasPath = Path.Combine(dumppath, animation.name, "0.Atlases", $"{collectionName}.png");
                        string path1 = Path.Combine(dumppath, animation.name, string.Format("{0:D3}", i) + "." + clip.name, string.Format("{0:D3}", i) + "-" + string.Format("{0:D2}", j) + "-" + string.Format("{0:D3}", frame.spriteId) + "_position.png");
                        string framePathRelative = Path.Combine(animation.name, string.Format("{0:D3}", i) + "." + clip.name, string.Format("{0:D3}", i) + "-" + string.Format("{0:D2}", j) + "-" + string.Format("{0:D3}", frame.spriteId) + ".png");
                        string framePath = Path.Combine(dumppath, framePathRelative);

                        bool flipped = tk2dSpriteDefinition.flipped == tk2dSpriteDefinition.FlipMode.Tk2d;

                        float xmin = pos.Min(v => v.x);
                        float ymin = pos.Min(v => v.y);
                        float xmax = pos.Max(v => v.x);
                        float ymax = pos.Max(v => v.y);

                        int x1 = (int)(uv.Min(v => v.x) * texture2D.width);
                        int y1 = (int)(uv.Min(v => v.y) * texture2D.height);
                        int x2 = (int)(uv.Max(v => v.x) * texture2D.width);
                        int y2 = (int)(uv.Max(v => v.y) * texture2D.height);

                        // symmetry transformation
                        int x11 = x1;
                        int y11 = y1;
                        int x22 = x2;
                        int y22 = y2;
                        if (flipped)
                        {
                            x22 = y2 + x1 - y1;
                            y22 = x2 - x1 + y1;
                        }

                        int x3 = (int)((Xmin - Xmin) / tk2dSpriteDefinition.texelSize.x);
                        int y3 = (int)((Ymin - Ymin) / tk2dSpriteDefinition.texelSize.y);
                        int x4 = (int)((Xmax - Xmin) / tk2dSpriteDefinition.texelSize.x);
                        int y4 = (int)((Ymax - Ymin) / tk2dSpriteDefinition.texelSize.y);

                        int x5 = (int)((xmin - Xmin) / tk2dSpriteDefinition.texelSize.x);
                        int y5 = (int)((ymin - Ymin) / tk2dSpriteDefinition.texelSize.y);
                        int x6 = (int)((xmax - Xmin) / tk2dSpriteDefinition.texelSize.x);
                        int y6 = (int)((ymax - Ymin) / tk2dSpriteDefinition.texelSize.y);

                        var uvPixel = new RectInt(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
                        var posBorder = new RectInt(x11 - x5 + x3, y11 - y5 + y3, x4 - x3 + 1, y4 - y3 + 1);
                        var uvPixelR = new RectInt(x5 - x3, y5 - y3, x22 - x11 + 1, y22 - y11 + 1);


                        if (!File.Exists(atlasPath))
                        {
                            texture2D.SaveToFile(atlasPath);
                            num++;
                        }

                        if (GODump.Settings.DumpSpriteInfo)
                        {
                            spriteInfo.Add(frame.spriteId, x1, y1, uvPixelR.x, uvPixelR.y, uvPixelR.width, uvPixelR.height, collectionName, framePathRelative, flipped);
                        }

                        if (!File.Exists(framePath))
                        {
                            try
                            {
                                Texture2D subTexture = texture2D.SubTexture(uvPixel);
                                if (flipped)
                                {
                                    SpriteDump.Tk2dFlip(ref subTexture);
                                }
                                if (GODump.Settings.FixSpriteSize)
                                {
                                    Texture2D fixedTexture = SpriteDump.FixSpriteSize(subTexture, uvPixelR, posBorder);
                                    fixedTexture.SaveToFile(framePath);
                                    DestroyImmediate(fixedTexture);
                                }
                                else
                                {
                                    subTexture.SaveToFile(framePath);
                                }

                                DestroyImmediate(subTexture);
                                num++;
                            }
                            catch (Exception e)
                            {
                                GODump.Instance.LogError("Could not subtexture: " + e);
                            }
                        }

                        DestroyImmediate(texture2D);
                    }

                    yield return new WaitForSeconds(0.5f);

                    if (GODump.Settings.DumpAnimInfo && clip.frames.Length > 0)
                    {
                        string animInfoPath = Path.Combine(_spritesPath, animation.name, string.Format("{0:D3}", i) + "." + clip.name, "AnimInfo.json");
                        Directory.CreateDirectory(Path.GetDirectoryName(animInfoPath));
                        var animInfo = new AnimationInfo(clip.frames.Length, clip.fps, clip.wrapMode, clip.loopStart, clip.frames[0].spriteCollection.spriteCollectionName);
                        using (FileStream fileStream = File.Create(animInfoPath))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(fileStream))
                            {
                                string value = JsonConvert.SerializeObject(animInfo, Formatting.Indented);
                                streamWriter.Write(value);
                            }
                        }
                    }
                }

                if (GODump.Settings.DumpSpriteInfo)
                {
                    string spriteInfoPath = Path.Combine(_spritesPath, animation.name, "0.Atlases", "SpriteInfo.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(spriteInfoPath));
                    using (FileStream fileStream = File.Create(spriteInfoPath))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(fileStream))
                        {
                            string value = JsonConvert.SerializeObject(spriteInfo, Formatting.Indented);
                            streamWriter.Write(value);
                        }
                    }
                }

                GODump.Instance.Log("End dumping sprites in tk2dSpriteAnimator [" + animation.name + "].");
            GODump.Instance.Log($"End dumping {num} sprites.");
        }
        private IEnumerator DumpAudio()
        {
            int num = 0;
            GODump.Instance.Log("Begin dumping audio clips.");
            foreach (KeyValuePair<GameObject, List<AudioClip>> pair in _gameObjectsWithAudio)
            {
                GameObject go = pair.Key;
                if (go == null) continue;
                GODump.Instance.Log($"Dumping audio clips for game object: {go.name}.");
                foreach (AudioClip audioClip in pair.Value)
                {
                    if (audioClip == null) continue;
                    audioClip.SaveToFile(Path.Combine(_audioPath, go.name, $"{audioClip.name}.wav"));
                    num++;
                    yield return new WaitForSeconds(0.5f);
                }
            }

            GODump.Instance.Log($"End dumping {num} audio clips.");
        }

        private tk2dSpriteAnimation[] GetUsages(tk2dSpriteCollectionData collection, tk2dSpriteAnimation[] animations)
        {
            List<tk2dSpriteAnimation> usages = new();
            foreach (tk2dSpriteAnimation animation in animations)
            {
                foreach (tk2dSpriteAnimationClip clip in animation.clips)
                {
                    foreach (tk2dSpriteAnimationFrame frame in clip.frames)
                    {
                        if (frame.spriteCollection.name == collection.name && !usages.Contains(animation))
                        {
                            usages.Add(animation);
                        }
                    }
                }
            }

            return usages.ToArray();
        }
    }
}
