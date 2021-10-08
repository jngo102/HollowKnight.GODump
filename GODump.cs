using Modding;
using Modding.Patches;
using MonoMod.Utils;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;
using ReflectionHelper = Modding.ReflectionHelper;
using UObject = UnityEngine.Object;

namespace GODump
{
    public class GODump : Mod, IGlobalSettings<Settings>
    {
        public static GODump Instance;

        private static Settings _settings = new Settings();
        public static Settings Settings => _settings;

        public override string GetVersion() => "1.0.0";

        public GODump() : base("Game Object Dump") {}

        public override void Initialize()
        {
            Instance = this;

            GameObject dumpObj = new GameObject("GODump");
            dumpObj.AddComponent<Dump>();
            UObject.DontDestroyOnLoad(dumpObj);
        }

        public void OnLoadGlobal(Settings settings) => _settings = settings;

        public Settings OnSaveGlobal() => _settings;

		public void LoadSettings()
		{
			try
			{
				Type type = ReflectionHelper.GetField<Mod, Type>(this, "globalSettingsType");
				if (type != null)
				{
					string globalSettingsPath = ReflectionHelper.GetField<Mod, string>(this, "_globalSettingsPath");
					if (File.Exists(globalSettingsPath))
					{
						using (FileStream fileStream = File.OpenRead(globalSettingsPath))
						using (StreamReader streamReader = new StreamReader(fileStream))
						{
							object obj = JsonConvert.DeserializeObject(streamReader.ReadToEnd(), type, new JsonSerializerSettings
							{
								ContractResolver = ShouldSerializeContractResolver.Instance,
								TypeNameHandling = TypeNameHandling.Auto,
								ObjectCreationHandling = ObjectCreationHandling.Replace,
								Converters = JsonConverterTypes.ConverterTypes
							});
							var onLoadGlobalSettings = ReflectionHelper.GetField<Mod, FastReflectionDelegate>(this, "onLoadGlobalSettings");
							onLoadGlobalSettings(this, new object[]
							{
								obj
							});
						}
					}
				}
			}
			catch (Exception message)
			{
				LogError(message);
			}
		}
		public void SaveSettings() => SaveGlobalSettings();
	}
}