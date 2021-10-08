using System;

namespace GODump
{
    [Serializable]
    public class Settings
    {
        private bool _dumpAnimInfo = true;
        private bool _dumpSpriteInfo = true;
        private bool _fixSpriteSize = true;
        private bool _spriteBorder = true;
        private string _animationsToDump;
        private string _audioClipsToDump;

        public bool DumpAnimInfo { get => _dumpAnimInfo; set => _dumpAnimInfo = value; }
        public bool DumpSpriteInfo { get => _dumpSpriteInfo; set => _dumpSpriteInfo = value; }
        public bool FixSpriteSize { get => _fixSpriteSize; set => _fixSpriteSize = value; }
        public bool SpriteBorder { get => _spriteBorder; set => _spriteBorder = value; }
        public string AnimationsToDump { get => _animationsToDump; set => _animationsToDump = value; }
        public string AudioClipsToDump { get => _audioClipsToDump; set => _audioClipsToDump = value; }
    }
}
