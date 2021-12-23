using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace CreatureManager
{
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

	public enum Weather
	{
		[InternalName("Clear")] ClearSkies,
		[InternalName("Heath clear")] MeadowsClearSkies,
		[InternalName("LightRain")] LightRain,
		[InternalName("Rain")] Rain,
		[InternalName("ThunderStorm")] ThunderStorm,
		[InternalName("nofogts")] ClearThunderStorm,
		[InternalName("SwampRain")] SwampRain,
		[InternalName("Darklands_dark")] MistlandsDark,
		[InternalName("Ashrain")] AshlandsAshrain,
		[InternalName("Snow")] MountainSnow,
		[InternalName("SnowStorm")] MountainBlizzard,
		[InternalName("DeepForest Mist")] BlackForestFog,
		[InternalName("Misty")] Fog,
		[InternalName("Twilight_Snow")] DeepNorthSnow,
		[InternalName("Twilight_SnowStorm")] DeepNorthSnowStorm,
		[InternalName("Twilight_Clear")] DeepNorthClear,
		[InternalName("Eikthyr")] EikthyrsThunderstorm,
		[InternalName("GDKing")] EldersHaze,
		[InternalName("Bonemass")] BonemassDownpour,
		[InternalName("Moder")] ModersVortex,
		[InternalName("GoblinKing")] YagluthsMagicBlizzard,
		[InternalName("Crypt")] Crypt,
		[InternalName("SunkenCrypt")] SunkenCrypt
	}

	public class InternalName : Attribute
	{
		public readonly string internalName;
		public InternalName(string internalName) => this.internalName = internalName;
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
		public readonly GameObject Prefab;

		public DropList Drops = new();

		public bool CanSpawn = true;
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
		public List<Weather> RequiredWeather = new();
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
		}

		public LocalizeKey Localize() => new LocalizeKey(Prefab.GetComponent<Character>().m_name);

		[HarmonyPriority(Priority.VeryHigh)]
		internal static void AddToSpawnSystem(SpawnSystem __instance)
		{
			foreach (Creature creature in registeredCreatures)
			{
				SpawnSystem.SpawnData spawnData = new()
				{
					m_name = creature.Prefab.name,
					m_prefab = creature.Prefab,
					m_enabled = creature.CanSpawn,
					m_biome = creature.Biome,
					m_biomeArea = creature.SpecificSpawnArea switch
					{
						SpawnArea.Center => Heightmap.BiomeArea.Median,
						SpawnArea.Edge => Heightmap.BiomeArea.Edge,
						_ => Heightmap.BiomeArea.Everything
					},
					m_maxSpawned = creature.Maximum,
					m_spawnInterval = creature.CheckSpawnInterval,
					m_spawnChance = creature.SpawnChance,
					m_requiredGlobalKey = ((InternalName)typeof(GlobalKey).GetMember(creature.RequiredGlobalKey.ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName,
					m_requiredEnvironments = creature.RequiredWeather.Select(w => ((InternalName)typeof(Weather).GetMember(w.ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName).ToList(),
					m_groupSizeMin = (int)creature.GroupSize.min,
					m_groupSizeMax = (int)creature.GroupSize.max,
					m_spawnAtNight = creature.SpecificSpawnTime is SpawnTime.Always or SpawnTime.Night,
					m_spawnAtDay = creature.SpecificSpawnTime is SpawnTime.Always or SpawnTime.Day,
					m_minAltitude = creature.RequiredAltitude.min,
					m_maxAltitude = creature.RequiredAltitude.max,
					m_inForest = creature.ForestSpawn is Forest.Both or Forest.Yes,
					m_outsideForest = creature.ForestSpawn is Forest.Both or Forest.No,
					m_minOceanDepth = creature.RequiredOceanDepth.min,
					m_maxOceanDepth = creature.RequiredOceanDepth.max,
					m_huntPlayer = creature.AttackImmediately,
					m_groundOffset = creature.SpawnAltitude,
					m_maxLevel = creature.CanHaveStars ? 3 : 1
				};
				__instance.m_spawnLists.First().m_spawners.Add(spawnData);
			}
		}
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