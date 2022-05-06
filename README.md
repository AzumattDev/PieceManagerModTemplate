# Piece Manager

Can be used to easily add new building pieces to Valheim. Will automatically add config options to your mod and sync the
configuration from a server, if the mod is installed on the server as well.

## How to add pieces

Copy the asset bundle into your project and make sure to set it as an EmbeddedResource in the properties of the asset
bundle. Default path for the asset bundle is an `assets` directory, but you can override this. This way, you don't have
to distribute your assets with your mod. They will be embedded into your mods DLL.

### Merging the DLLs into your mod

Download the PieceManager.dll and the ServerSync.dll from the release section to the right. Including the DLLs is best
done via ILRepack (https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task). You can load this package (
ILRepack.Lib.MSBuild.Task) from NuGet.

If you have installed ILRepack via NuGet, simply create a file named `ILRepack.targets` in your project and copy the
following content into the file

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Target Name="ILRepacker" AfterTargets="Build">
        <ItemGroup>
            <InputAssemblies Include="$(TargetPath)"/>
            <InputAssemblies Include="$(OutputPath)\PieceManager.dll"/>
            <InputAssemblies Include="$(OutputPath)\ServerSync.dll"/>
        </ItemGroup>
        <ILRepack Parallel="true" DebugInfo="true" Internalize="true" InputAssemblies="@(InputAssemblies)"
                  OutputFile="$(TargetPath)" TargetKind="SameAsPrimaryAssembly" LibraryPath="$(OutputPath)"/>
    </Target>
</Project>
```

Make sure to set the PieceManager.dll and the ServerSync.dll in your project to "Copy to output directory" in the
properties of the DLLs and to add a reference to it. After that, simply add `using PieceManager;` to your mod and use
the `BuildPiece` class, to add your items.

## Example project

This adds three different pieces from two different asset bundles. The `funward` asset bundle is in a directory
called `FunWard`, while the `bamboo` asset bundle is in a directory called `assets`.

```csharp
using System.IO;
using BepInEx;
using HarmonyLib;
using PieceManager;

namespace PieceManagerExampleMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class PieceManagerExampleMod : BaseUnityPlugin
    {
        private const string ModName = "PieceManagerExampleMod";
        private const string ModVersion = "1.0.0";
        internal const string Author = "azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        private void Awake()
        {
            // Format: new("AssetBundleName", "PrefabName", "FolderName");
            BuildPiece examplePiece1 = new("funward", "funward", "FunWard");

            examplePiece1.Name.English("Fun Ward"); // Localize the name and description for the building piece for a language.
            examplePiece1.Description.English("Ward For testing the Piece Manager");
            examplePiece1.RequiredItems.Add("FineWood", 20, false); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            examplePiece1.RequiredItems.Add("SurtlingCore", 20, false);


            BuildPiece examplePiece2 = new("bamboo", "Bamboo_Wall"); // Note: If you wish to use the default "assets" folder for your assets, you can omit it!
            examplePiece2.Name.English("Bamboo Wall");
            examplePiece2.Description.English("A wall made of bamboo!");
            examplePiece2.RequiredItems.Add("BambooLog", 20, false);


            // If you want to add your item to the cultivator or another hammer with vanilla categories
            // Format: (AssetBundle, "PrefabName", addToCustom, "Item that has a piecetable")
            BuildPiece examplePiece3 = new(PiecePrefabManager.RegisterAssetBundle("bamboo"), "Bamboo_Sapling", true, "Cultivator");
            examplePiece3.Name.English("Bamboo Sapling");
            examplePiece3.Description.English("A young bamboo tree, called a sapling");
            examplePiece3.RequiredItems.Add("BambooSeed", 20, false);

            // Need to add something to ZNetScene but not the hammer, cultivator or other? 
            PiecePrefabManager.RegisterPrefab("bamboo", "Bamboo_Beam_Light");
            
            // Does your model need to swap materials with a vanilla material? Format: (GameObject, isJotunnMock)
            MaterialReplacer.RegisterGameObjectForMatSwap(examplePiece3.Prefab, false);
        }
    }
}
```