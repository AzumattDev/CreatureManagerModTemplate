using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CreatureManager
{
	public enum Toggle
	{
		On,
		Off
	}

	public enum GlobalKey
	{
		[InternalName("")] None,
		[InternalName("defeated_bonemass")] KilledBonemass,
		[InternalName("defeated_gdking")] KilledElder,
		[InternalName("defeated_goblinking")] KilledYagluth,
		[InternalName("defeated_dragon")] KilledModer,
		[InternalName("defeated_eikthyr")] KilledEikthyr,
		[InternalName("KilledTroll")] KilledTroll,
		[InternalName("killed_surtling")] KilledSurtling
	}

	[Flags]
	public enum Weather
	{
		[InternalName("")] None = 0,
		[InternalName("Clear")] ClearSkies = 1 << 0,
		[InternalName("Heath clear")] MeadowsClearSkies = 1 << 2,
		[InternalName("LightRain")] LightRain = 1 << 3,
		[InternalName("Rain")] Rain = 1 << 4,
		[InternalName("ThunderStorm")] ThunderStorm = 1 << 5,
		[InternalName("nofogts")] ClearThunderStorm = 1 << 6,
		[InternalName("SwampRain")] SwampRain = 1 << 7,
		[InternalName("Darklands_dark")] MistlandsDark = 1 << 8,
		[InternalName("Ashrain")] AshlandsAshrain = 1 << 9,
		[InternalName("Snow")] MountainSnow = 1 << 10,
		[InternalName("SnowStorm")] MountainBlizzard = 1 << 11,
		[InternalName("DeepForest Mist")] BlackForestFog = 1 << 12,
		[InternalName("Misty")] Fog = 1 << 13,
		[InternalName("Twilight_Snow")] DeepNorthSnow = 1 << 14,
		[InternalName("Twilight_SnowStorm")] DeepNorthSnowStorm = 1 << 15,
		[InternalName("Twilight_Clear")] DeepNorthClear = 1 << 16,
		[InternalName("Eikthyr")] EikthyrsThunderstorm = 1 << 17,
		[InternalName("GDKing")] EldersHaze = 1 << 18,
		[InternalName("Bonemass")] BonemassDownpour = 1 << 19,
		[InternalName("Moder")] ModersVortex = 1 << 20,
		[InternalName("GoblinKing")] YagluthsMagicBlizzard = 1 << 21,
		[InternalName("Crypt")] Crypt = 1 << 22,
		[InternalName("SunkenCrypt")] SunkenCrypt = 1 << 23
	}

	public class InternalName : Attribute
	{
		public readonly string internalName;
		public InternalName(string internalName) => this.internalName = internalName;
	}

	public enum SpawnOption
	{
		Disabled,
		Default,
		Custom
	}

	public enum SpawnTime
	{
		Day,
		Night,
		Always
	}

	public enum SpawnArea
	{
		Center,
		Edge,
		Everywhere
	}

	public enum Forest
	{
		Yes,
		No,
		Both
	}

	[PublicAPI]
	public struct Range
	{
		public float min;
		public float max;

		public Range(float min, float max)
		{
			this.min = min;
			this.max = max;
		}
	}

	[PublicAPI]
	public class Creature
	{
		public bool ConfigurationEnabled = true;

		public readonly GameObject Prefab;

		public DropList Drops = new();

		public bool CanSpawn = true;
		public bool CanBeTamed = false;
		/// <summary>
		/// List of items the creature consumes to get tame.
		/// <para>For multiple item names, separate them with a comma.</para>
		/// </summary>
		public string FoodItems = "";
		/// <summary>
		/// Sets the time of day the creature can spawn.
		/// </summary>
		public SpawnTime SpecificSpawnTime = SpawnTime.Always;
		/// <summary>
		/// Sets the minimum and maximum altitude for the creature to spawn.
		/// </summary>
		public Range RequiredAltitude = new(5, 1000);
		/// <summary>
		/// Sets the minimum and maximum depth of the ocean for the creature to spawn.
		/// </summary>
		public Range RequiredOceanDepth = new(0, 0);
		/// <summary>
		/// Sets a global key required for the creature to spawn.
		/// </summary>
		public GlobalKey RequiredGlobalKey = GlobalKey.None;
		/// <summary>
		/// Sets a range for the group size the creature spawns in.
		/// </summary>
		public Range GroupSize = new(1, 1);
		/// <summary>
		/// Sets the biome the creature spawns in.
		/// </summary>
		public Heightmap.Biome Biome = Heightmap.Biome.Meadows;
		/// <summary>
		/// Sets spawning area for the creature inside the biome.
		/// <para>Use SpawnArea.Edge, to make the creature spawn more towards the edge of the biome.</para>
		/// <para>Use SpawnArea.Center, to make the creature spawn more towards the center of the biome.</para>
		/// </summary>
		public SpawnArea SpecificSpawnArea = SpawnArea.Everywhere;
		/// <summary>
		/// Sets the weather condition for the creature to spawn.
		/// <para>Use the Weather enum for easy configuration.</para>
		/// </summary>
		public Weather RequiredWeather = Weather.None;
		/// <summary>
		/// Sets altitude relative to the current ground level for the creature to spawn.
		/// <para>Should be a higher number for flying creatures, so they spawn in the sky.</para>
		/// </summary>
		public float SpawnAltitude = 0.5f;
		public bool CanHaveStars = true;
		/// <summary>
		/// Controls the first AI command right after spawn.
		/// <para>Set to true for the creature to immediately start to hunt down the player.</para>
		/// </summary>
		public bool AttackImmediately = false;
		/// <summary>
		/// The interval in seconds that Valheim checks if it should spawn the creature in.
		/// </summary>
		public int CheckSpawnInterval = 600;
		/// <summary>
		/// The chance in percent for the creature to spawn, every time Valheim checks if it should spawn.
		/// </summary>
		public float SpawnChance = 100;
		/// <summary>
		/// Can be used to make the creature spawn in forests or prevent it from spawning in forests.
		/// <para>Use the Forest enum for easy configuration.</para>
		/// </summary>
		public Forest ForestSpawn = Forest.Both;
		/// <summary>
		/// Sets the maximum number of the creature that can be near the player, before Valheim disables its spawn.
		/// </summary>
		public int Maximum = 1;

		[PublicAPI]
		public class DropList
		{
			private Dictionary<string, Drop>? drops = null;

			public void None() => drops = new Dictionary<string, Drop>();

			public Drop this[string prefabName] => (drops ??= new Dictionary<string, Drop>()).TryGetValue(prefabName, out Drop drop) ? drop : drops[prefabName] = new Drop();

			[HarmonyPriority(Priority.VeryHigh)]
			internal static void AddDropsToCreature(ZNetScene __instance)
			{
				foreach (Creature creature in registeredCreatures)
				{
					if (creature.Drops.drops is not null)
					{
						(creature.Prefab.GetComponent<CharacterDrop>() ?? creature.Prefab.AddComponent<CharacterDrop>()).m_drops = creature.Drops.drops.Select(kv => new CharacterDrop.Drop
						{
							m_prefab = __instance.GetPrefab(kv.Key),
							m_amountMin = (int)kv.Value.Amount.min,
							m_amountMax = (int)kv.Value.Amount.max,
							m_chance = kv.Value.DropChance / 100,
							m_onePerPlayer = kv.Value.DropOnePerPlayer,
							m_levelMultiplier = kv.Value.MultiplyDropByLevel
						}).ToList();
					}
				}
			}
		}

		[PublicAPI]
		public class Drop
		{
			public Range Amount = new(1, 1);
			/// <summary>
			/// Sets the drop chance for the game object in percent.
			/// </summary>
			public float DropChance = 100f;
			public bool DropOnePerPlayer = false;
			public bool MultiplyDropByLevel = true;
		}

		private static readonly List<Creature> registeredCreatures = new();

		public Creature(string assetBundleFileName, string prefabName, string folderName = "assets") : this(PrefabManager.RegisterAssetBundle(assetBundleFileName, folderName), prefabName) { }

		public Creature(AssetBundle bundle, string prefabName) : this(PrefabManager.RegisterPrefab(bundle, prefabName)) { }

		public Creature(GameObject creature)
		{
			Prefab = creature;
			registeredCreatures.Add(this);

			CanBeTamed = creature.GetComponent<Tameable>();
			FoodItems = string.Join(",", creature.GetComponent<MonsterAI>()?.m_consumeItems.Select(i => i.m_itemData.m_dropPrefab.name) ?? Enumerable.Empty<string>());
		}

		public LocalizeKey Localize() => new(Prefab.GetComponent<Character>().m_name);

		private class CustomConfig<T>
		{
			public Func<T> get = null!;
			public ConfigEntry<T>? config = null;
		}

		private class CreatureConfig
		{
			public readonly CustomConfig<SpawnOption> Spawn = new();
			public readonly CustomConfig<Toggle> CanBeTamed = new();
			public readonly CustomConfig<string> ConsumesItemName = new();
			public readonly CustomConfig<SpawnTime> SpecificSpawnTime = new();
			public readonly CustomConfig<Range> RequiredAltitude = new();
			public readonly CustomConfig<Range> RequiredOceanDepth = new();
			public readonly CustomConfig<GlobalKey> RequiredGlobalKey = new();
			public readonly CustomConfig<Range> GroupSize = new();
			public readonly CustomConfig<Heightmap.Biome> Biome = new();
			public readonly CustomConfig<SpawnArea> SpecificSpawnArea = new();
			public readonly CustomConfig<Weather> RequiredWeather = new();
			public readonly CustomConfig<float> SpawnAltitude = new();
			public readonly CustomConfig<Toggle> CanHaveStars = new();
			public readonly CustomConfig<Toggle> AttackImmediately = new();
			public readonly CustomConfig<int> CheckSpawnInterval = new();
			public readonly CustomConfig<float> SpawnChance = new();
			public readonly CustomConfig<Forest> ForestSpawn = new();
			public readonly CustomConfig<int> Maximum = new();
		}

		private static Dictionary<Creature, CreatureConfig> creatureConfigs = new();

		private class ConfigurationManagerAttributes
		{
			[UsedImplicitly] public int? Order;
			[UsedImplicitly] public bool? Browsable;
			[UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
		}

		private class AcceptableEnumValues<T> : AcceptableValueBase where T : struct, IConvertible
		{
			public AcceptableEnumValues(params T[] acceptableValues) : base(typeof(T))
			{
				AcceptableValues = acceptableValues;
			}

			[PublicAPI] public virtual T[] AcceptableValues { get; }

			public override object Clamp(object value) => IsValid(value) ? value : AcceptableValues[0];
			public override bool IsValid(object value) => AcceptableValues.Contains((T)value);
			public override string ToDescriptionString() => string.Join(", ", AcceptableValues);
		}

		internal static void Patch_FejdStartup()
		{
			Assembly? bepinexConfigManager = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ConfigurationManager");

			Type? configManagerType = bepinexConfigManager?.GetType("ConfigurationManager.ConfigurationManager");
			object? configManager = configManagerType == null ? null : BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(configManagerType);

			void reloadConfigDisplay() => configManagerType?.GetMethod("BuildSettingList")!.Invoke(configManager, Array.Empty<object>());

			TomlTypeConverter.AddConverter(typeof(Range), new TypeConverter
			{
				ConvertToObject = (s, _) =>
				{
					string[] parts = s.Split(new[] { '-' }, 2);
					float.TryParse(parts[0], out float from);
					if (parts.Length < 2 || !float.TryParse(parts[1], out float to))
					{
						to = from;
					}
					return new Range(from, to);
				},
				ConvertToString = (obj, _) =>
				{
					Range range = (Range)obj;
					return $"{range.min} - {range.max}";
				}
			});

			foreach (Creature creature in registeredCreatures)
			{
				CreatureConfig cfg = creatureConfigs[creature] = new CreatureConfig();
				string localizedName = new Regex("['[\"\\]]").Replace(english.Localize(creature.Prefab.GetComponent<Character>().m_name), "").Trim();

				int order = 0;
				void configWithDesc<T>(CustomConfig<T> cfg, Func<T> getter, Action configChanged, string name, ConfigDescription desc)
				{
					if (creature.ConfigurationEnabled)
					{
						cfg.config = Creature.config(localizedName, name, getter(), new ConfigDescription(desc.Description, desc.AcceptableValues, desc.Tags.Concat(new[] { new ConfigurationManagerAttributes { Order = --order, CustomDrawer = typeof(T) == typeof(Range) ? drawRange : null } }).ToArray()));
						cfg.config.SettingChanged += (_, _) => configChanged();
						cfg.get = () => cfg.config.Value;
					}
					else
					{
						cfg.get = getter;
					}
				}
				void config<T>(CustomConfig<T> cfg, Func<T> getter, Action configChanged, string name, string desc) => configWithDesc(cfg, getter, configChanged, name, new ConfigDescription(desc));

				void updateAllSpawnConfigs()
				{
					foreach (SpawnSystem spawnSystem in Object.FindObjectsOfType<SpawnSystem>())
					{
						foreach (SpawnSystemList spawnList in spawnSystem.m_spawnLists)
						{
							foreach (SpawnSystem.SpawnData spawnData in spawnList.m_spawners)
							{
								if (creature.Prefab == spawnData.m_prefab)
								{
									creature.updateSpawnData(spawnData);
								}
							}
						}
					}
				}

				void updateAI()
				{
					if (ObjectDB.instance)
					{
						foreach (BaseAI ai in Object.FindObjectsOfType<BaseAI>())
						{
							creature.updateAi(ai);
						}
						creature.updateAi(creature.Prefab.GetComponent<BaseAI>());
					}
				}

				ConfigurationManagerAttributes tameConfigVisibility = new();
				config(cfg.CanBeTamed, () => creature.CanBeTamed ? Toggle.On : Toggle.Off, () =>
				{
					tameConfigVisibility.Browsable = cfg.CanBeTamed.get() == Toggle.On;
					reloadConfigDisplay();
					updateAI();
				}, "Can be tamed", "Decides, if the creature can be tamed.");
				tameConfigVisibility.Browsable = cfg.CanBeTamed.get() == Toggle.On;
				configWithDesc(cfg.ConsumesItemName, () => creature.FoodItems, updateAI, "Food items", new ConfigDescription("The items the creature consumes to get tame.", null, tameConfigVisibility));

				ConfigurationManagerAttributes spawnConfigVisibility = new();
				void spawnConfig<T>(CustomConfig<T> cfg, Func<T> getter, string name, string desc, AcceptableValueBase? acceptableValues = null) => configWithDesc(cfg, getter, updateAllSpawnConfigs, name, new ConfigDescription(desc, acceptableValues, spawnConfigVisibility));

				config(cfg.Spawn, () => creature.CanSpawn ? SpawnOption.Default : SpawnOption.Disabled, () =>
				{
					spawnConfigVisibility.Browsable = cfg.Spawn.get() == SpawnOption.Custom;
					reloadConfigDisplay();
					updateAllSpawnConfigs();
				}, "Spawn", "Configures the spawn for the creature.");
				spawnConfigVisibility.Browsable = cfg.Spawn.get() == SpawnOption.Custom;
				spawnConfig(cfg.SpecificSpawnTime, () => creature.SpecificSpawnTime, "Spawn time", "Configures the time of day for the creature to spawn.");
				spawnConfig(cfg.RequiredAltitude, () => creature.RequiredAltitude, "Required altitude", "Configures the altitude required for the creature to spawn.");
				spawnConfig(cfg.RequiredOceanDepth, () => creature.RequiredOceanDepth, "Required ocean depth", "Configures the ocean depth required for the creature to spawn.");
				spawnConfig(cfg.RequiredGlobalKey, () => creature.RequiredGlobalKey, "Required global key", "Configures the global key required for the creature to spawn.");
				spawnConfig(cfg.GroupSize, () => creature.GroupSize, "Group size", "Configures the size of the groups in which the creature spawns.");
				spawnConfig(cfg.Biome, () => creature.Biome, "Biome", "Configures the biome required for the creature to spawn.", new AcceptableEnumValues<Heightmap.Biome>(Enum.GetValues(typeof(Heightmap.Biome)).Cast<Heightmap.Biome>().Where(e => e != Heightmap.Biome.BiomesMax).ToArray()));
				spawnConfig(cfg.SpecificSpawnArea, () => creature.SpecificSpawnArea, "Spawn area", "Configures if the creature spawns more towards the center or the edge of the biome.");
				spawnConfig(cfg.RequiredWeather, () => creature.RequiredWeather, "Required weather", "Configures the weather required for the creature to spawn.");
				spawnConfig(cfg.SpawnAltitude, () => creature.SpawnAltitude, "Spawn altitude", "Configures the height from the ground in which the creature will spawn.");
				spawnConfig(cfg.CanHaveStars, () => creature.CanHaveStars ? Toggle.On : Toggle.Off, "Can have stars", "If the creature can have stars.");
				spawnConfig(cfg.AttackImmediately, () => creature.AttackImmediately ? Toggle.On : Toggle.Off, "Hunt player", "Makes the creature immediately hunt down the player after it spawns.");
				spawnConfig(cfg.CheckSpawnInterval, () => creature.CheckSpawnInterval, "Maximum spawn interval", "Configures the timespan that Valheim has to make the creature spawn.");
				spawnConfig(cfg.SpawnChance, () => creature.SpawnChance, "Spawn chance", "Sets the chance for the creature to be spawned, every time Valheim checks the spawn.");
				spawnConfig(cfg.ForestSpawn, () => creature.ForestSpawn, "Forest condition", "If the creature can spawn in forests or cannot spawn in forests. Or both.");
				spawnConfig(cfg.Maximum, () => creature.Maximum, "Maximum creature count", "The maximum number of this creature near the player, before Valheim stops spawning it in. Setting this lower than the upper limit of the group size does not make sense.");
			}
		}

		private static void drawRange(ConfigEntryBase cfg)
		{
			bool locked = cfg.Description.Tags.Select(a => a.GetType().Name == "ConfigurationManagerAttributes" ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a) : null).FirstOrDefault(v => v != null) ?? false;

			ConfigEntry<Range> config = (ConfigEntry<Range>) cfg;

			GUILayout.BeginHorizontal();
			float.TryParse(GUILayout.TextField(config.Value.min.ToString(CultureInfo.InvariantCulture)), out float min);
			GUILayout.Label(" - ", new GUIStyle(GUI.skin.label) { fixedWidth = 14 });
			float.TryParse(GUILayout.TextField(config.Value.max.ToString(CultureInfo.InvariantCulture)), out float max);
			GUILayout.EndHorizontal();

			if (!locked && (config.Value.min != min || config.Value.max != max))
			{
				config.Value = new Range(min, max);
			}
		}

		private void updateAi(BaseAI ai)
		{
			CreatureConfig cfg = creatureConfigs[this];
			if (ai.GetComponent<Tameable>() != (cfg.CanBeTamed.get() == Toggle.On))
			{
				if (cfg.CanBeTamed.get() == Toggle.On)
				{
					ai.m_tamable = ai.gameObject.AddComponent<Tameable>();
				}
				else
				{
					Object.Destroy(ai.m_tamable);
					ai.m_tamable = null;
				}
			}

			if (ai is MonsterAI monsterAI)
			{
				monsterAI.m_consumeItems.Clear();
				string[] items = cfg.ConsumesItemName.get().Split(',');
				foreach (string itemName in items)
				{
					ItemDrop? item = ObjectDB.instance.GetItemPrefab(itemName.Trim())?.GetComponent<ItemDrop>();
					if (item is not null)
					{
						monsterAI.m_consumeItems.Add(item);
					}
				}
			}
		}

		internal static void UpdateCreatureAis(ObjectDB __instance)
		{
			foreach (Creature creature in registeredCreatures)
			{
				creature.updateAi(creature.Prefab.GetComponent<BaseAI>());
			}
		}

		private static List<SpawnSystem.SpawnData> lastRegisteredSpawns = new();

		private void updateSpawnData(SpawnSystem.SpawnData spawnData)
		{
			CreatureConfig cfg = creatureConfigs[this];
			spawnData.m_enabled = cfg.Spawn.get() != SpawnOption.Disabled;
			spawnData.m_biome = cfg.Biome.get();
			spawnData.m_biomeArea = cfg.SpecificSpawnArea.get() switch
			{
				SpawnArea.Center => Heightmap.BiomeArea.Median,
				SpawnArea.Edge => Heightmap.BiomeArea.Edge,
				_ => Heightmap.BiomeArea.Everything
			};
			spawnData.m_maxSpawned = cfg.Maximum.get();
			spawnData.m_spawnInterval = cfg.CheckSpawnInterval.get();
			spawnData.m_spawnChance = cfg.SpawnChance.get();
			spawnData.m_requiredGlobalKey = ((InternalName)typeof(GlobalKey).GetMember(cfg.RequiredGlobalKey.get().ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName;
			spawnData.m_requiredEnvironments = Enum.GetValues(typeof(Weather)).Cast<Weather>().Where(w => (w & cfg.RequiredWeather.get()) != Weather.None).Select(w => ((InternalName)typeof(Weather).GetMember(w.ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName).ToList();
			spawnData.m_groupSizeMin = (int)cfg.GroupSize.get().min;
			spawnData.m_groupSizeMax = (int)cfg.GroupSize.get().max;
			spawnData.m_spawnAtNight = cfg.SpecificSpawnTime.get() is SpawnTime.Always or SpawnTime.Night;
			spawnData.m_spawnAtDay = cfg.SpecificSpawnTime.get() is SpawnTime.Always or SpawnTime.Day;
			spawnData.m_minAltitude = cfg.RequiredAltitude.get().min;
			spawnData.m_maxAltitude = cfg.RequiredAltitude.get().max;
			spawnData.m_inForest = cfg.ForestSpawn.get() is Forest.Both or Forest.Yes;
			spawnData.m_outsideForest = cfg.ForestSpawn.get() is Forest.Both or Forest.No;
			spawnData.m_minOceanDepth = cfg.RequiredOceanDepth.get().min;
			spawnData.m_maxOceanDepth = cfg.RequiredOceanDepth.get().max;
			spawnData.m_huntPlayer = cfg.AttackImmediately.get() == Toggle.On;
			spawnData.m_groundOffset = cfg.SpawnAltitude.get();
			spawnData.m_maxLevel = cfg.CanHaveStars.get() == Toggle.On ? 3 : 1;
		}

		[HarmonyPriority(Priority.VeryHigh)]
		internal static void AddToSpawnSystem(SpawnSystem __instance)
		{
			SpawnSystemList spawnList = __instance.m_spawnLists.First();

			foreach (SpawnSystem.SpawnData spawnData in lastRegisteredSpawns)
			{
				spawnList.m_spawners.Remove(spawnData);
			}
			lastRegisteredSpawns.Clear();

			foreach (Creature creature in registeredCreatures)
			{
				SpawnSystem.SpawnData spawnData = new()
				{
					m_name = creature.Prefab.name,
					m_prefab = creature.Prefab
				};
				creature.updateSpawnData(spawnData);
				lastRegisteredSpawns.Add(spawnData);
				spawnList.m_spawners.Add(spawnData);
			}
		}

		private static Localization? _english;

		private static Localization english
		{
			get
			{
				if (_english == null)
				{
					_english = new Localization();
					_english.SetupLanguage("English");
				}

				return _english;
			}
		}

		private static BaseUnityPlugin? _plugin;

		private static BaseUnityPlugin plugin
		{
			get
			{
				if (_plugin is null)
				{
					IEnumerable<TypeInfo> types;
					try
					{
						types = Assembly.GetExecutingAssembly().DefinedTypes.ToList();
					}
					catch (ReflectionTypeLoadException e)
					{
						types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
					}
					_plugin = (BaseUnityPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(types.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));
				}
				return _plugin;
			}
		}

		private static bool hasConfigSync = true;
		private static object? _configSync;

		private static object? configSync
		{
			get
			{
				if (_configSync == null && hasConfigSync)
				{
					if (Assembly.GetExecutingAssembly().GetType("ServerSync.ConfigSync") is { } configSyncType)
					{
						_configSync = Activator.CreateInstance(configSyncType, plugin.Info.Metadata.GUID + " CreatureManager");
						configSyncType.GetField("CurrentVersion").SetValue(_configSync, plugin.Info.Metadata.Version.ToString());
						configSyncType.GetProperty("IsLocked")!.SetValue(_configSync, true);
					}
					else
					{
						hasConfigSync = false;
					}
				}

				return _configSync;
			}
		}

		private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
		{
			ConfigEntry<T> configEntry = plugin.Config.Bind(group, name, value, description);

			configSync?.GetType().GetMethod("AddConfigEntry")!.MakeGenericMethod(typeof(T)).Invoke(configSync, new object[] { configEntry });

			return configEntry;
		}

		private static ConfigEntry<T> config<T>(string group, string name, T value, string description) => config(group, name, value, new ConfigDescription(description));
	}

	[PublicAPI]
	public class LocalizeKey
	{
		public readonly string Key;

		public LocalizeKey(string key) => Key = key.Replace("$", "");

		public LocalizeKey English(string key) => addForLang("English", key);
		public LocalizeKey Swedish(string key) => addForLang("Swedish", key);
		public LocalizeKey French(string key) => addForLang("French", key);
		public LocalizeKey Italian(string key) => addForLang("Italian", key);
		public LocalizeKey German(string key) => addForLang("German", key);
		public LocalizeKey Spanish(string key) => addForLang("Spanish", key);
		public LocalizeKey Russian(string key) => addForLang("Russian", key);
		public LocalizeKey Romanian(string key) => addForLang("Romanian", key);
		public LocalizeKey Bulgarian(string key) => addForLang("Bulgarian", key);
		public LocalizeKey Macedonian(string key) => addForLang("Macedonian", key);
		public LocalizeKey Finnish(string key) => addForLang("Finnish", key);
		public LocalizeKey Danish(string key) => addForLang("Danish", key);
		public LocalizeKey Norwegian(string key) => addForLang("Norwegian", key);
		public LocalizeKey Icelandic(string key) => addForLang("Icelandic", key);
		public LocalizeKey Turkish(string key) => addForLang("Turkish", key);
		public LocalizeKey Lithuanian(string key) => addForLang("Lithuanian", key);
		public LocalizeKey Czech(string key) => addForLang("Czech", key);
		public LocalizeKey Hungarian(string key) => addForLang("Hungarian", key);
		public LocalizeKey Slovak(string key) => addForLang("Slovak", key);
		public LocalizeKey Polish(string key) => addForLang("Polish", key);
		public LocalizeKey Dutch(string key) => addForLang("Dutch", key);
		public LocalizeKey Portuguese_European(string key) => addForLang("Portuguese_European", key);
		public LocalizeKey Portuguese_Brazilian(string key) => addForLang("Portuguese_Brazilian", key);
		public LocalizeKey Chinese(string key) => addForLang("Chinese", key);
		public LocalizeKey Japanese(string key) => addForLang("Japanese", key);
		public LocalizeKey Korean(string key) => addForLang("Korean", key);
		public LocalizeKey Hindi(string key) => addForLang("Hindi", key);
		public LocalizeKey Thai(string key) => addForLang("Thai", key);
		public LocalizeKey Abenaki(string key) => addForLang("Abenaki", key);
		public LocalizeKey Croatian(string key) => addForLang("Croatian", key);
		public LocalizeKey Georgian(string key) => addForLang("Georgian", key);
		public LocalizeKey Greek(string key) => addForLang("Greek", key);
		public LocalizeKey Serbian(string key) => addForLang("Serbian", key);
		public LocalizeKey Ukrainian(string key) => addForLang("Ukrainian", key);

		private LocalizeKey addForLang(string lang, string value)
		{
			if (Localization.instance.GetSelectedLanguage() == lang)
			{
				Localization.instance.AddWord(Key, value);
			}
			else if (lang == "English" && !Localization.instance.m_translations.ContainsKey(Key))
			{
				Localization.instance.AddWord(Key, value);
			}
			return this;
		}
	}

	public static class PrefabManager
	{
		static PrefabManager()
		{
			Harmony harmony = new("org.bepinex.helpers.CreatureManager");
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PrefabManager), nameof(Patch_ZNetSceneAwake))));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Creature.DropList), nameof(Creature.DropList.AddDropsToCreature))));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(SpawnSystem), nameof(SpawnSystem.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Creature), nameof(Creature.AddToSpawnSystem))));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Creature), nameof(Creature.UpdateCreatureAis))));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Creature), nameof(Creature.Patch_FejdStartup))));
		}

		private struct BundleId
		{
			[UsedImplicitly]
			public string assetBundleFileName;
			[UsedImplicitly]
			public string folderName;
		}

		private static readonly Dictionary<BundleId, AssetBundle> bundleCache = new();

		public static AssetBundle RegisterAssetBundle(string assetBundleFileName, string folderName = "assets")
		{
			BundleId id = new() { assetBundleFileName = assetBundleFileName, folderName = folderName };
			if (!bundleCache.TryGetValue(id, out AssetBundle assets))
			{
				assets = bundleCache[id] = Resources.FindObjectsOfTypeAll<AssetBundle>().FirstOrDefault(a => a.name == assetBundleFileName) ?? AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + $".{folderName}." + assetBundleFileName));
			}
			return assets;
		}

		private static readonly List<GameObject> prefabs = new();

		public static GameObject RegisterPrefab(AssetBundle assets, string prefabName)
		{
			GameObject prefab = assets.LoadAsset<GameObject>(prefabName);

			prefabs.Add(prefab);

			return prefab;
		}

		[HarmonyPriority(Priority.VeryHigh)]
		private static void Patch_ZNetSceneAwake(ZNetScene __instance)
		{
			foreach (GameObject prefab in prefabs)
			{
				__instance.m_prefabs.Add(prefab);
			}
		}
	}
}