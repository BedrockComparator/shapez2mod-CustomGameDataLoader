# CustomGameDataLoader

A shapez 2 mod that provides an API framework for other mods to inject custom GameData resources at runtime — without Unity Editor asset workflow.

From v1.2.0 : Provides access to json files provided by mods, making it easier to decompose large scenario config json file into many parts.
Usage: `#include:MOD@path\to\file` in the scenario json file, where MOD is the entrypoint class(the only one implements IMod) name
and path\to\file is relative path from the target mod's root directory.

## Architecture

```
Game Loads → LoadGameDataBlindStep.Execute (original)
                         │
          MonoMod Detour Postfix Hook
                         │
          CustomGameDataRegistrar.OnGameDataInitialize(gameData)
                         │
          All registered callbacks fire sequentially
                         │
          Each receives IGameDataHelper → Add / Replace / AddOrReplace
                         │
          Summary logged to mod console
```

`CustomGameDataLoaderMod` hooks `LoadGameDataBlindStep.Execute` via a MonoMod postfix detour. After the game populates `GameData`, all registered callbacks receive an `IGameDataHelper` that directly manipulates the publicized private backing fields.

Registration is **open during mod construction only**. Once `OnGameDataInitialize` fires, the registrar freezes and further `Register()` calls throw.

## Quick Start (for third-party mods)

### Resources Loading

```csharp
public class MyMod : IMod
{
    public MyMod(ILogger logger)
    {
        CustomGameDataRegistrar.Register(helper =>
        {
            // Add a new wiki entry, the translation key will be @wiki.MyEntry.title
            var entry = WikiEntryBuilder.Create("MyEntry")
                .ShowInKnowledgePanel()
                .AddHeading("Welcome")
                .AddText("This is my custom content.")
                .Build();

            helper.AddWikiEntry("MyEntry", entry);//The entry can be referenced in scenario.json by "MyEntry"

            //Notice: The id used in `Create()` will determine the translation key of the entry,
            //        but the id used in `Add*`/`Replace*` will determine the id to be referenced in scenario.json .
            //        They can be different, but it's recommended to keep them the same for clarity.

            // Or replace an existing one
            helper.ReplaceImage("MyImage", mySprite);//The sprite can be referenced in scenario.json by "MyImage"
        });
    }

    public void Dispose() { }
}
```

No dependency on `CustomGameDataLoaderMod` — just reference the DLL and register callbacks.

### Scenario JSON "#include" redirection

No C# code is required for mods that **reference** the files. 
But mods that **provide** the files must implement `IMod` interface to be recognized by the redirection system. 

A mod can also reference its own files to achieve modularization. In this case, the mod must implement `IMod` interface as well.

Example:

```csharp
namespace ExampleProviderMod
{
    public class ExampleProviderMod : IMod
    {
        public ExampleProviderMod()
        {
        }
        public void Dispose()
        {
        }
    }
}
```
And its file structure:
```
ExampleProviderMod/
├── scenarios/
│   ├── common/
│   │   ├── researchConfig
│   │   ├── milestones
│   │   └── ...
│   └── ...
├── ExampleProviderMod.dll
├── manifest.json
└── ...
```
To reference the file like `scenarios/common/researchConfig` in another mod's scenario json file:
```
#include:ExampleProviderMod@scenarios/common/researchConfig
```

## IGameDataHelper API

All 10 resource types support three operations × two ID overloads:

| Operation | Behavior |
|-----------|----------|
| `Add*` | Inserts; throws if ID already exists |
| `Replace*` | Overwrites; throws if ID does not exist |
| `AddOrReplace*` | Inserts or overwrites (upsert) |

### Typed vs string overloads

Each operation has both `(TypedId, value)` and `(string, value)` overloads. The string overload constructs the typed ID internally.

```csharp
helper.AddImage(new GameImageId("MyImage"), sprite);  // typed
helper.AddImage("MyImage", sprite);                    // string shortcut
```

### Resource categories

| Category | Type | Backing |
|----------|------|---------|
| Images | `GameImageId` → `Sprite` | `Dictionary` |
| Icons | `GameIconId` → `Sprite` | `Dictionary` (nested in `GameIconCollection`) |
| Videos | `GameVideoId` → `MetaVideoDefinition` | `Dictionary` |
| ShapesConfigurations | `ShapesConfigurationId` → `ShapesConfiguration` | `Dictionary` |
| ColorSchemes | `ShapeColorSchemeId` → `ShapeColorScheme` | `Dictionary` |
| WikiEntries | `WikiEntryId` → `MetaWikiEntry` | `Dictionary` |
| TutorialConfigs | `TutorialConfigId` → `MetaTutorialConfig` | `Dictionary` |
| GameRules | `GameRuleId` → `MetaGameRule` | `Dictionary` |
| GameModes | `GameModeId` → `GameModeDefinition` | `Dictionary` |
| DifficultyPresets | (list) → `GameDifficultyPreset` | `List` (append only) |

`DifficultyPresets` is list-backed — use `AppendDifficultyPreset(preset)` to add.

### Advanced: direct GameData access

```csharp
helper.getGameData()  // returns the live GameData instance
```

## CustomGameDataRegistrar

### Static configuration

```csharp
// Enable per-resource ID lists in the summary log
CustomGameDataRegistrar.DetailedLogging = true;
```

### Summary output

After all callbacks fire, a summary is logged automatically. Normal mode:

```
Resource registration summary:
  ColorSchemes: +1 ~0
  DifficultyPresets: +0 ~0
  GameModes: +0 ~0
  GameRules: +0 ~0
  Icons: +0 ~0
  Images: +2 ~1
  ShapesConfigurations: +1 ~0
  TutorialConfigs: +0 ~0
  Videos: +0 ~0
  WikiEntries: +3 ~0
```

With `DetailedLogging = true`, each category also lists the specific IDs:

```
  Images: +2 ~1
    Added: MyImage, OtherImage
    Replaced: VanillaImage
```

## Builder APIs

All builders live in the `CustomResourcesLoader.Builders` namespace and follow a consistent pattern:

- **`Create()`** — new empty ScriptableObject instance
- **`CloneFrom(original)`** — deep copy of an existing instance
- **`Build()`** — validate and return the constructed object
- **`Build("name")`** — set `.name` and build (not all builders provide this)

### WikiEntryBuilder

Constructs `MetaWikiEntry` with full wiki content pipeline support.

```csharp
var entry = WikiEntryBuilder.Create("MyEntry")//The title, the translation key will be @wiki.MyEntry.title
    .ShowInKnowledgePanel(true)
    .AddRelatedEntry(new WikiEntryId("RelatedEntry"))
    .AddHeading("Getting Started")
    .AddText("This is plain text.")
    .AddTextLocalized("@wiki.myentry.desc")
    .AddNarrativeText("Once upon a time...")
    .AddShapeCodes()//show all available shape subparts and colors
    .AddImage(mySprite)
    .AddBlueprint(mySprite, "BP-CODE")//put your blueprint string at 2nd parameter
    .AddVideo(myVideoDef)
    .AddTutorialPopup(myPopup)
    .AddBuildingPanel(buildingId, "Building description")
    .AddIslandPanel(islandId, "Island description")
    .AddTextWithLinks("Click here", ("link1", "https://example.com"))
    .AddLinearUpgrade(upgradeId)
    .AddSideUpgrade(upgradeId)
    .AddContent(myCustomContent)
    .Build();
```

Text methods have three overloads: `(IText)`, `(string plainText)` → internal `RawStringText`, `*Localized(string key)` → `LazyLocalizedText`.

Clone entry points:
- `CloneFrom(MetaWikiEntry original)` — deep-clones content via `Create()` factory
- `CloneFrom(GameData gameData, WikiEntryId id)` — clone from registered entry

### MetaVideoDefinitionBuilder

**This builder API hasn't been tested completely. Issue if you encounter any problems.**

```csharp
var video = MetaVideoDefinitionBuilder.Create()
    .SetVideo(myVideoClip)
    .AddMarker(m => m
        .SetKeybinding("interact.confirm")
        .SetStartTime(1500)
        .SetDuration(3000)
        .SetPosition(HudVideoMarkerPosition.BottomCenter)
        .SetVisibleWhileHidden(false))
    .Build("Video.MyTutorial");
```

Marker sub-builder (`MarkerBuilder`) is accessed via `AddMarker(Action<MarkerBuilder>)`.

Clone entry points:
- `CloneFrom(MetaVideoDefinition original)`
- `CloneFrom(GameData gameData, GameVideoId id)`

### MetaShapeSubPartBuilder

**This builder API hasn't been tested completely. Issue if you encounter any problems.**

```csharp
var part = MetaShapeSubPartBuilder.Create()
    .SetCode('C')
    .SetMaterial(ShapeShaderMaterialType.NormalColor)
    .AllowColor(true)
    .AllowChangingColor(true)
    .DestroyOnFallDown(false)
    .OverrideColorMaterial(false)
    .SetHighDetailMesh(myMesh)
    .SetMesh(myLODMesh)
    .Build("ShapePart.Cr");
```

Defaults: `AllowColor = true`, `AllowChangingColor = true`, `Material = NormalColor`.

Clone entry point: `CloneFrom(MetaShapeSubPart original)`

### MetaShapesConfigurationBuilder

**This builder API hasn't been tested completely. Issue if you encounter any problems.**

```csharp
var config = MetaShapesConfigurationBuilder.Create()
    .SetPartCount(4)  // must be even, 1-32
    .SetPinShapePart(pinPart)
    .SetCrystalShapePart(crystalPart)
    .AddPart(circlePart, MetaShapesConfiguration.PartGenerationRarity.Common)
    .AddPart(squarePart, MetaShapesConfiguration.PartGenerationRarity.Rare)
    .Build("Shapes.MyConfig");
```

`Build()` validates: PinShapePart and CrystalShapePart must both be present in the Parts list.

Clone entry point: `CloneFrom(MetaShapesConfiguration original)`

### MetaShapeColorBuilder

**This builder API hasn't been tested completely. Issue if you encounter any problems.**

```csharp
var color = MetaShapeColorBuilder.Create()
    .SetCode('r')
    .SetMaterial(ShapeShaderMaterialType.NormalColor)
    .Build("Color.Red");
```

Default material: `ShapeShaderMaterialType.NormalColor`.

**`MetaShapeColor` only owns code and material.** Render data (per-visualization-scheme `MetaShapeColorRenderData` and display `Color`) is held by `MetaShapeColorScheme`, not the color itself. Configure rendering via `MetaShapeColorSchemeBuilder`.

Clone entry point: `CloneFrom(MetaShapeColor original)`

### MetaShapeColorSchemeBuilder

**This builder API hasn't been tested completely. Issue if you encounter any problems.**

**CloneFrom is the recommended path.** Color schemes have complex visualization data (`MetaShapeColorVisualizationScheme` per `ColorVisualizationSchemeType`) that is difficult to construct manually. `CloneFrom` now deep-clones the visualization scheme objects so modifications don't affect the original.

```csharp
var scheme = MetaShapeColorSchemeBuilder.CloneFrom(originalScheme)
    .ClearColors()
    .SetDefaultColor(red)
    .AddColor(red, MetaShapeColorSchemeBuilder.ColorType.Primary)
    .AddColor(blue, MetaShapeColorSchemeBuilder.ColorType.Primary)
    .AddColor(cyan, MetaShapeColorSchemeBuilder.ColorType.Secondary)
    // Fill all unspecified color pairs with a default result
    .SetDefaultMixResult()
    .Build("ColorScheme.MyScheme");
```

#### Color registration

**This builder API hasn't been tested completely. Issue if you encounter any problems.**

Three equivalent styles — individual methods, typed `AddColor`, or both:

```csharp
// Individual list methods
builder.AddPrimaryColor(red).AddSecondaryColor(blue);

// Typed registration — routes to the correct list by ColorType, optionally player-obtainable
builder.AddColor(red, MetaShapeColorSchemeBuilder.ColorType.Primary, playerObtainable: true);
builder.AddColor(blue, MetaShapeColorSchemeBuilder.ColorType.Secondary, playerObtainable: false);

// Player-obtainable can also be set separately
builder.AddPlayerObtainableColor(red);
```

#### Mixing rules

```csharp
// Explicit per-pair results
builder.AddMixResult(red, blue, green);

// Fill all as-yet-unspecified color pairs with a default (falls back to DefaultColor)
builder.SetDefaultMixResult();               // uses DefaultColor
builder.SetDefaultMixResult(someColor);      // explicit
```

#### Render data (visualization schemes)

The data model: `MetaShapeColorScheme` → `VisualizationSchemes[SchemeType]` → `RenderData[MetaShapeColor]` → `{Color, MetaShapeColorRenderData}`. Four scheme types exist (`RGB`, `RYB`, `CMYK`, `RGBColorBlind`).

```csharp
// Set the entire visualization scheme for a type (required when using Create)
builder.SetVisualizationScheme(ColorVisualizationSchemeType.RGB, myRgbScheme);

// Set full render data (Color + MetaShapeColorRenderData) per scheme
builder.SetRenderData(red, new Dictionary<ColorVisualizationSchemeType, MetaShapeColorVisualizationScheme.ColorRenderData>
{
    [ColorVisualizationSchemeType.RGB] = new MetaShapeColorVisualizationScheme.ColorRenderData
    {
        Color = Color.red,
        RenderData = myRenderData,
    },
});

// Change only the display Color, preserving existing MetaShapeColorRenderData
builder.SetRenderColor(red, new Dictionary<ColorVisualizationSchemeType, Color>
{
    [ColorVisualizationSchemeType.RGB] = Color.red,
    [ColorVisualizationSchemeType.CMYK] = new Color(0.1f, 0.8f, 0.2f),
});

// Copy all render-data entries from one color to another
builder.CopyVisualizationScheme(originalRed, newRed);
```

When using `Create()`, you must supply visualization schemes via `SetVisualizationScheme(type, scheme)`. When using `CloneFrom`, existing visualization data is preserved and deep-cloned.

## Project Structure

```
CustomGameDataLoader/
├── Builders/
│   ├── MetaShapeColorBuilder.cs
│   ├── MetaShapeColorSchemeBuilder.cs
│   ├── MetaShapesConfigurationBuilder.cs
│   ├── MetaShapeSubPartBuilder.cs
│   ├── MetaVideoDefinitionBuilder.cs
│   └── WikiEntryBuilder.cs
├── CustomGameDataLoader.csproj
├── CustomGameDataLoaderMod.cs      (mod entry point + detour)
├── CustomGameDataRegistrar.cs      (IGameDataHelper + registrar)
├── JsonRedirector.cs               (json #include redirector)
└── manifest.json
```

## Dependencies

- **Shapez Shifter** — `DetourHelper.CreatePostfixHook` for MonoMod detour
- **MonoMod.RuntimeDetour** — detour runtime
- **Krafs.Publicizer** — compile-time access to `GameData` private fields and `GameIconCollection` internals
- **UniTask** — async return type matching
- **Game.Core.Content** — `BuildingDefinitionGroupId`
- **Game.Core.HUD** — `HudVideoMarkerPosition`
- **Core.Localization** — `IText`, `LazyLocalizedText`, `TranslationId`
