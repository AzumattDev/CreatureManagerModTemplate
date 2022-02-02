# Creature Manager

Makes your custom creature come to life in Valheim.

## How to add creatures

Copy the asset bundle into your project and make sure to set it as an EmbeddedResource in the properties of the asset bundle.
Default path for the asset bundle is an `assets` directory, but you can override this.
This way, you don't have to distribute your assets with your mod. They will be embedded into your mods DLL.

### Merging the precompiled DLL into your mod

Add the following three lines to the bottom of the first PropertyGroup in your .csproj file, to enable C# V10.0 features and to allow the use of publicized DLLs.

```xml
<LangVersion>10</LangVersion>
<Nullable>enable</Nullable>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

Download the CreatureManager.dll and the ServerSync.dll from the release section to the right.
Including the dll is best done via ILRepack (https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task). You can load this package (ILRepack.Lib.MSBuild.Task) from NuGet.

If you have installed ILRepack via NuGet, simply create a file named `ILRepack.targets` in your project and copy the following content into the file

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Target Name="ILRepacker" AfterTargets="Build">
        <ItemGroup>
            <InputAssemblies Include="$(TargetPath)" />
            <InputAssemblies Include="$(OutputPath)\CreatureManager.dll" />
            <InputAssemblies Include="$(OutputPath)\ServerSync.dll" />
        </ItemGroup>
        <ILRepack Parallel="true" DebugInfo="true" Internalize="true" InputAssemblies="@(InputAssemblies)" OutputFile="$(TargetPath)" TargetKind="SameAsPrimaryAssembly" LibraryPath="$(OutputPath)" />
    </Target>
</Project>
```

Make sure to set the CreatureManager.dll and the ServerSync.dll in your project to "Copy to output directory" in the properties of the DLL and to add a reference to both of them.
After that, simply add `using CreatureManager;` to your mod and use the `Creature` class, to add your creatures.

## Example project

This adds two different creatures from the same asset bundle.

```csharp
using System.Collections.Generic;
using BepInEx;
using CreatureManager;

namespace CustomCreatures
{
	[BepInPlugin(ModGUID, ModName, ModVersion)]
	public class CustomCreatures : BaseUnityPlugin
	{
		private const string ModName = "CustomCreatures";
		private const string ModVersion = "1.0";
		private const string ModGUID = "org.bepinex.plugins.customcreatures";

		public void Awake()
		{
			Creature wereBearBlack = new("werebear", "WereBearBlack")
			{
				Biome = Heightmap.Biome.Meadows,
				GroupSize = new Range(1, 2),
				CheckSpawnInterval = 600,
				RequiredWeather = Weather.Rain | Weather.Fog,
				Maximum = 2
			};
			wereBearBlack.Localize()
				.English("Black Werebear")
				.German("Schwarzer Werbär")
				.French("Ours-Garou Noir");
			wereBearBlack.Drops["Wood"].Amount = new Range(1, 2);
			wereBearBlack.Drops["Wood"].DropChance = 100f;
			
			Creature wereBearRed = new("werebear", "WereBearRed")
			{
				Biome = Heightmap.Biome.AshLands,
				GroupSize = new Range(1, 1),
				CheckSpawnInterval = 900,
				AttackImmediately = true,
				RequiredGlobalKey = GlobalKey.KilledYagluth,
			};
			wereBearRed.Localize()
				.English("Red Werebear")
				.German("Roter Werbär")
				.French("Ours-Garou Rouge");
			wereBearRed.Drops["Coal"].Amount = new Range(1, 2);
			wereBearRed.Drops["Coal"].DropChance = 100f;
			wereBearRed.Drops["Flametal"].Amount = new Range(1, 1);
			wereBearRed.Drops["Flametal"].DropChance = 10f;
		}
	}
}
```