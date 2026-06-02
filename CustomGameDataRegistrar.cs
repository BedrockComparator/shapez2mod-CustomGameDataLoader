using Game.Core.GameData.GameModeDefinition;
using Game.Core.GameData.Presets;
using Game.Core.Research;
using Global.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CustomGameDataLoader
{
    public static class CustomGameDataRegistrar
    {
        private static bool _isFrozen = false;
        private static bool _isInjecting = false;
        private static List<Action<IGameDataHelper>> _callbacks = new List<Action<IGameDataHelper>>();

        /// <summary>
        /// When set to true by any mod, the registration summary will include per-resource ID lists.
        /// </summary>
        public static bool DetailedLogging { get; set; } = false;

        /// <summary>
        /// Optional logger. Set before <see cref="OnGameDataInitialize"/> to receive summary output.
        /// </summary>
        public static Core.Logging.ILogger Logger { get; set; }

        // ── Public Interface ──────────────────────────────────

        public interface IGameDataHelper
        {
            // ── Images ────────────────────────
            void AddImage(GameImageId id, Sprite sprite);
            void AddImage(string id, Sprite sprite);
            void ReplaceImage(GameImageId id, Sprite sprite);
            void ReplaceImage(string id, Sprite sprite);
            void AddOrReplaceImage(GameImageId id, Sprite sprite);
            void AddOrReplaceImage(string id, Sprite sprite);

            // ── Icons ─────────────────────────
            void AddIcon(GameIconId id, Sprite sprite);
            void AddIcon(string id, Sprite sprite);
            void ReplaceIcon(GameIconId id, Sprite sprite);
            void ReplaceIcon(string id, Sprite sprite);
            void AddOrReplaceIcon(GameIconId id, Sprite sprite);
            void AddOrReplaceIcon(string id, Sprite sprite);

            // ── Videos ────────────────────────
            void AddVideo(GameVideoId id, MetaVideoDefinition video);
            void AddVideo(string id, MetaVideoDefinition video);
            void ReplaceVideo(GameVideoId id, MetaVideoDefinition video);
            void ReplaceVideo(string id, MetaVideoDefinition video);
            void AddOrReplaceVideo(GameVideoId id, MetaVideoDefinition video);
            void AddOrReplaceVideo(string id, MetaVideoDefinition video);

            // ── ShapesConfigurations ──────────
            void AddShapesConfiguration(ShapesConfigurationId id, ShapesConfiguration config);
            void AddShapesConfiguration(string id, ShapesConfiguration config);
            void ReplaceShapesConfiguration(ShapesConfigurationId id, ShapesConfiguration config);
            void ReplaceShapesConfiguration(string id, ShapesConfiguration config);
            void AddOrReplaceShapesConfiguration(ShapesConfigurationId id, ShapesConfiguration config);
            void AddOrReplaceShapesConfiguration(string id, ShapesConfiguration config);

            // ── ColorSchemes ──────────────────
            void AddColorScheme(ShapeColorSchemeId id, ShapeColorScheme scheme);
            void AddColorScheme(string id, ShapeColorScheme scheme);
            void ReplaceColorScheme(ShapeColorSchemeId id, ShapeColorScheme scheme);
            void ReplaceColorScheme(string id, ShapeColorScheme scheme);
            void AddOrReplaceColorScheme(ShapeColorSchemeId id, ShapeColorScheme scheme);
            void AddOrReplaceColorScheme(string id, ShapeColorScheme scheme);

            // ── WikiEntries ───────────────────
            void AddWikiEntry(WikiEntryId id, MetaWikiEntry entry);
            void AddWikiEntry(string id, MetaWikiEntry entry);
            void ReplaceWikiEntry(WikiEntryId id, MetaWikiEntry entry);
            void ReplaceWikiEntry(string id, MetaWikiEntry entry);
            void AddOrReplaceWikiEntry(WikiEntryId id, MetaWikiEntry entry);
            void AddOrReplaceWikiEntry(string id, MetaWikiEntry entry);

            // ── TutorialConfigs ───────────────
            void AddTutorialConfig(TutorialConfigId id, MetaTutorialConfig config);
            void AddTutorialConfig(string id, MetaTutorialConfig config);
            void ReplaceTutorialConfig(TutorialConfigId id, MetaTutorialConfig config);
            void ReplaceTutorialConfig(string id, MetaTutorialConfig config);
            void AddOrReplaceTutorialConfig(TutorialConfigId id, MetaTutorialConfig config);
            void AddOrReplaceTutorialConfig(string id, MetaTutorialConfig config);

            // ── GameRules ─────────────────────
            void AddGameRule(GameRuleId id, MetaGameRule rule);
            void AddGameRule(string id, MetaGameRule rule);
            void ReplaceGameRule(GameRuleId id, MetaGameRule rule);
            void ReplaceGameRule(string id, MetaGameRule rule);
            void AddOrReplaceGameRule(GameRuleId id, MetaGameRule rule);
            void AddOrReplaceGameRule(string id, MetaGameRule rule);

            // ── GameModes ─────────────────────
            void AddGameMode(GameModeId id, GameModeDefinition mode);
            void AddGameMode(string id, GameModeDefinition mode);
            void ReplaceGameMode(GameModeId id, GameModeDefinition mode);
            void ReplaceGameMode(string id, GameModeDefinition mode);
            void AddOrReplaceGameMode(GameModeId id, GameModeDefinition mode);
            void AddOrReplaceGameMode(string id, GameModeDefinition mode);

            // ── UnlockableStoreContents ───────
            void AddUnlockableStoreContent(UnlockableStoreContentId id, IUnlockableStoreContent content);
            void AddUnlockableStoreContent(string id, IUnlockableStoreContent content);
            void ReplaceUnlockableStoreContent(UnlockableStoreContentId id, IUnlockableStoreContent content);
            void ReplaceUnlockableStoreContent(string id, IUnlockableStoreContent content);
            void AddOrReplaceUnlockableStoreContent(UnlockableStoreContentId id, IUnlockableStoreContent content);
            void AddOrReplaceUnlockableStoreContent(string id, IUnlockableStoreContent content);

            // ── DifficultyPresets (list-backed) ──
            void AppendDifficultyPreset(GameDifficultyPreset preset);

            /// <summary>Direct access to the underlying GameData for advanced scenarios.</summary>
            GameData getGameData();
        }

        // ── Implementation ────────────────────────────────────

        public class GameDataHelper : IGameDataHelper
        {
            private GameData _gameData;
            private ChangeTracker _tracker;

            internal GameDataHelper(GameData gameData, ChangeTracker tracker)
            {
                _gameData = gameData;
                _tracker = tracker;
            }

            public GameData getGameData() => _gameData;

            // ── Images ────────────────────────────────────────
            public void AddImage(GameImageId id, Sprite sprite)
            {
                if (_gameData._Images.TryAdd(id, sprite))
                    _tracker.RecordAdd(Categories.Images, id.Id);
                else
                    throw new InvalidOperationException($"Image with id '{id.Id}' already exists.");
            }
            public void AddImage(string id, Sprite sprite) => AddImage(new GameImageId(id), sprite);

            public void ReplaceImage(GameImageId id, Sprite sprite)
            {
                if (_gameData._Images.ContainsKey(id))
                {
                    _gameData._Images[id] = sprite;
                    _tracker.RecordReplace(Categories.Images, id.Id);
                }
                else
                    throw new InvalidOperationException($"Image with id '{id.Id}' does not exist.");
            }
            public void ReplaceImage(string id, Sprite sprite) => ReplaceImage(new GameImageId(id), sprite);

            public void AddOrReplaceImage(GameImageId id, Sprite sprite)
            {
                if (_gameData._Images.ContainsKey(id))
                {
                    _gameData._Images[id] = sprite;
                    _tracker.RecordReplace(Categories.Images, id.Id);
                }
                else
                {
                    _gameData._Images[id] = sprite;
                    _tracker.RecordAdd(Categories.Images, id.Id);
                }
            }
            public void AddOrReplaceImage(string id, Sprite sprite) => AddOrReplaceImage(new GameImageId(id), sprite);

            // ── Icons ─────────────────────────────────────────
            public void AddIcon(GameIconId id, Sprite sprite)
            {
                if (_gameData._Icons._Icons.TryAdd(id, sprite))
                    _tracker.RecordAdd(Categories.Icons, id.Id);
                else
                    throw new InvalidOperationException($"Icon with id '{id.Id}' already exists.");
            }
            public void AddIcon(string id, Sprite sprite) => AddIcon(new GameIconId(id), sprite);

            public void ReplaceIcon(GameIconId id, Sprite sprite)
            {
                if (_gameData._Icons._Icons.ContainsKey(id))
                {
                    _gameData._Icons._Icons[id] = sprite;
                    _tracker.RecordReplace(Categories.Icons, id.Id);
                }
                else
                    throw new InvalidOperationException($"Icon with id '{id.Id}' does not exist.");
            }
            public void ReplaceIcon(string id, Sprite sprite) => ReplaceIcon(new GameIconId(id), sprite);

            public void AddOrReplaceIcon(GameIconId id, Sprite sprite)
            {
                if (_gameData._Icons._Icons.ContainsKey(id))
                {
                    _gameData._Icons._Icons[id] = sprite;
                    _tracker.RecordReplace(Categories.Icons, id.Id);
                }
                else
                {
                    _gameData._Icons._Icons[id] = sprite;
                    _tracker.RecordAdd(Categories.Icons, id.Id);
                }
            }
            public void AddOrReplaceIcon(string id, Sprite sprite) => AddOrReplaceIcon(new GameIconId(id), sprite);

            // ── Videos ────────────────────────────────────────
            public void AddVideo(GameVideoId id, MetaVideoDefinition video)
            {
                if (_gameData._Videos.TryAdd(id, video))
                    _tracker.RecordAdd(Categories.Videos, id.Id);
                else
                    throw new InvalidOperationException($"Video with id '{id.Id}' already exists.");
            }
            public void AddVideo(string id, MetaVideoDefinition video) => AddVideo(new GameVideoId(id), video);

            public void ReplaceVideo(GameVideoId id, MetaVideoDefinition video)
            {
                if (_gameData._Videos.ContainsKey(id))
                {
                    _gameData._Videos[id] = video;
                    _tracker.RecordReplace(Categories.Videos, id.Id);
                }
                else
                    throw new InvalidOperationException($"Video with id '{id.Id}' does not exist.");
            }
            public void ReplaceVideo(string id, MetaVideoDefinition video) => ReplaceVideo(new GameVideoId(id), video);

            public void AddOrReplaceVideo(GameVideoId id, MetaVideoDefinition video)
            {
                if (_gameData._Videos.ContainsKey(id))
                {
                    _gameData._Videos[id] = video;
                    _tracker.RecordReplace(Categories.Videos, id.Id);
                }
                else
                {
                    _gameData._Videos[id] = video;
                    _tracker.RecordAdd(Categories.Videos, id.Id);
                }
            }
            public void AddOrReplaceVideo(string id, MetaVideoDefinition video) => AddOrReplaceVideo(new GameVideoId(id), video);

            // ── ShapesConfigurations ───────────────────────────
            public void AddShapesConfiguration(ShapesConfigurationId id, ShapesConfiguration config)
            {
                if (_gameData._ShapesConfigurations.TryAdd(id, config))
                    _tracker.RecordAdd(Categories.ShapesConfigurations, id.Id);
                else
                    throw new InvalidOperationException($"ShapesConfiguration with id '{id.Id}' already exists.");
            }
            public void AddShapesConfiguration(string id, ShapesConfiguration config) => AddShapesConfiguration(new ShapesConfigurationId(id), config);

            public void ReplaceShapesConfiguration(ShapesConfigurationId id, ShapesConfiguration config)
            {
                if (_gameData._ShapesConfigurations.ContainsKey(id))
                {
                    _gameData._ShapesConfigurations[id] = config;
                    _tracker.RecordReplace(Categories.ShapesConfigurations, id.Id);
                }
                else
                    throw new InvalidOperationException($"ShapesConfiguration with id '{id.Id}' does not exist.");
            }
            public void ReplaceShapesConfiguration(string id, ShapesConfiguration config) => ReplaceShapesConfiguration(new ShapesConfigurationId(id), config);

            public void AddOrReplaceShapesConfiguration(ShapesConfigurationId id, ShapesConfiguration config)
            {
                if (_gameData._ShapesConfigurations.ContainsKey(id))
                {
                    _gameData._ShapesConfigurations[id] = config;
                    _tracker.RecordReplace(Categories.ShapesConfigurations, id.Id);
                }
                else
                {
                    _gameData._ShapesConfigurations[id] = config;
                    _tracker.RecordAdd(Categories.ShapesConfigurations, id.Id);
                }
            }
            public void AddOrReplaceShapesConfiguration(string id, ShapesConfiguration config) => AddOrReplaceShapesConfiguration(new ShapesConfigurationId(id), config);

            // ── ColorSchemes ──────────────────────────────────
            public void AddColorScheme(ShapeColorSchemeId id, ShapeColorScheme scheme)
            {
                if (_gameData._ColorSchemes.TryAdd(id, scheme))
                    _tracker.RecordAdd(Categories.ColorSchemes, id.Id);
                else
                    throw new InvalidOperationException($"ColorScheme with id '{id.Id}' already exists.");
            }
            public void AddColorScheme(string id, ShapeColorScheme scheme) => AddColorScheme(new ShapeColorSchemeId(id), scheme);

            public void ReplaceColorScheme(ShapeColorSchemeId id, ShapeColorScheme scheme)
            {
                if (_gameData._ColorSchemes.ContainsKey(id))
                {
                    _gameData._ColorSchemes[id] = scheme;
                    _tracker.RecordReplace(Categories.ColorSchemes, id.Id);
                }
                else
                    throw new InvalidOperationException($"ColorScheme with id '{id.Id}' does not exist.");
            }
            public void ReplaceColorScheme(string id, ShapeColorScheme scheme) => ReplaceColorScheme(new ShapeColorSchemeId(id), scheme);

            public void AddOrReplaceColorScheme(ShapeColorSchemeId id, ShapeColorScheme scheme)
            {
                if (_gameData._ColorSchemes.ContainsKey(id))
                {
                    _gameData._ColorSchemes[id] = scheme;
                    _tracker.RecordReplace(Categories.ColorSchemes, id.Id);
                }
                else
                {
                    _gameData._ColorSchemes[id] = scheme;
                    _tracker.RecordAdd(Categories.ColorSchemes, id.Id);
                }
            }
            public void AddOrReplaceColorScheme(string id, ShapeColorScheme scheme) => AddOrReplaceColorScheme(new ShapeColorSchemeId(id), scheme);

            // ── WikiEntries ─────────────────────────────────────
            public void AddWikiEntry(WikiEntryId id, MetaWikiEntry entry)
            {
                if (_gameData._WikiEntries.TryAdd(id, entry))
                    _tracker.RecordAdd(Categories.WikiEntries, id.Id);
                else
                    throw new InvalidOperationException($"WikiEntry with id '{id.Id}' already exists.");
            }
            public void AddWikiEntry(string id, MetaWikiEntry entry) => AddWikiEntry(new WikiEntryId(id), entry);

            public void ReplaceWikiEntry(WikiEntryId id, MetaWikiEntry entry)
            {
                if (_gameData._WikiEntries.ContainsKey(id))
                {
                    _gameData._WikiEntries[id] = entry;
                    _tracker.RecordReplace(Categories.WikiEntries, id.Id);
                }
                else
                    throw new InvalidOperationException($"WikiEntry with id '{id.Id}' does not exist.");
            }
            public void ReplaceWikiEntry(string id, MetaWikiEntry entry) => ReplaceWikiEntry(new WikiEntryId(id), entry);

            public void AddOrReplaceWikiEntry(WikiEntryId id, MetaWikiEntry entry)
            {
                if (_gameData._WikiEntries.ContainsKey(id))
                {
                    _gameData._WikiEntries[id] = entry;
                    _tracker.RecordReplace(Categories.WikiEntries, id.Id);
                }
                else
                {
                    _gameData._WikiEntries[id] = entry;
                    _tracker.RecordAdd(Categories.WikiEntries, id.Id);
                }
            }
            public void AddOrReplaceWikiEntry(string id, MetaWikiEntry entry) => AddOrReplaceWikiEntry(new WikiEntryId(id), entry);

            // ── TutorialConfigs ────────────────────────────────
            public void AddTutorialConfig(TutorialConfigId id, MetaTutorialConfig config)
            {
                if (_gameData._TutorialConfigs.TryAdd(id, config))
                    _tracker.RecordAdd(Categories.TutorialConfigs, id.Id);
                else
                    throw new InvalidOperationException($"TutorialConfig with id '{id.Id}' already exists.");
            }
            public void AddTutorialConfig(string id, MetaTutorialConfig config) => AddTutorialConfig(new TutorialConfigId(id), config);

            public void ReplaceTutorialConfig(TutorialConfigId id, MetaTutorialConfig config)
            {
                if (_gameData._TutorialConfigs.ContainsKey(id))
                {
                    _gameData._TutorialConfigs[id] = config;
                    _tracker.RecordReplace(Categories.TutorialConfigs, id.Id);
                }
                else
                    throw new InvalidOperationException($"TutorialConfig with id '{id.Id}' does not exist.");
            }
            public void ReplaceTutorialConfig(string id, MetaTutorialConfig config) => ReplaceTutorialConfig(new TutorialConfigId(id), config);

            public void AddOrReplaceTutorialConfig(TutorialConfigId id, MetaTutorialConfig config)
            {
                if (_gameData._TutorialConfigs.ContainsKey(id))
                {
                    _gameData._TutorialConfigs[id] = config;
                    _tracker.RecordReplace(Categories.TutorialConfigs, id.Id);
                }
                else
                {
                    _gameData._TutorialConfigs[id] = config;
                    _tracker.RecordAdd(Categories.TutorialConfigs, id.Id);
                }
            }
            public void AddOrReplaceTutorialConfig(string id, MetaTutorialConfig config) => AddOrReplaceTutorialConfig(new TutorialConfigId(id), config);

            // ── GameRules ──────────────────────────────────────
            public void AddGameRule(GameRuleId id, MetaGameRule rule)
            {
                if (_gameData._GameRules.TryAdd(id, rule))
                    _tracker.RecordAdd(Categories.GameRules, id.Id);
                else
                    throw new InvalidOperationException($"GameRule with id '{id.Id}' already exists.");
            }
            public void AddGameRule(string id, MetaGameRule rule) => AddGameRule(new GameRuleId(id), rule);

            public void ReplaceGameRule(GameRuleId id, MetaGameRule rule)
            {
                if (_gameData._GameRules.ContainsKey(id))
                {
                    _gameData._GameRules[id] = rule;
                    _tracker.RecordReplace(Categories.GameRules, id.Id);
                }
                else
                    throw new InvalidOperationException($"GameRule with id '{id.Id}' does not exist.");
            }
            public void ReplaceGameRule(string id, MetaGameRule rule) => ReplaceGameRule(new GameRuleId(id), rule);

            public void AddOrReplaceGameRule(GameRuleId id, MetaGameRule rule)
            {
                if (_gameData._GameRules.ContainsKey(id))
                {
                    _gameData._GameRules[id] = rule;
                    _tracker.RecordReplace(Categories.GameRules, id.Id);
                }
                else
                {
                    _gameData._GameRules[id] = rule;
                    _tracker.RecordAdd(Categories.GameRules, id.Id);
                }
            }
            public void AddOrReplaceGameRule(string id, MetaGameRule rule) => AddOrReplaceGameRule(new GameRuleId(id), rule);

            // ── GameModes ──────────────────────────────────────
            public void AddGameMode(GameModeId id, GameModeDefinition mode)
            {
                if (_gameData._GameModes.TryAdd(id, mode))
                    _tracker.RecordAdd(Categories.GameModes, id.Id);
                else
                    throw new InvalidOperationException($"GameMode with id '{id.Id}' already exists.");
            }
            public void AddGameMode(string id, GameModeDefinition mode) => AddGameMode(new GameModeId(id), mode);

            public void ReplaceGameMode(GameModeId id, GameModeDefinition mode)
            {
                if (_gameData._GameModes.ContainsKey(id))
                {
                    _gameData._GameModes[id] = mode;
                    _tracker.RecordReplace(Categories.GameModes, id.Id);
                }
                else
                    throw new InvalidOperationException($"GameMode with id '{id.Id}' does not exist.");
            }
            public void ReplaceGameMode(string id, GameModeDefinition mode) => ReplaceGameMode(new GameModeId(id), mode);

            public void AddOrReplaceGameMode(GameModeId id, GameModeDefinition mode)
            {
                if (_gameData._GameModes.ContainsKey(id))
                {
                    _gameData._GameModes[id] = mode;
                    _tracker.RecordReplace(Categories.GameModes, id.Id);
                }
                else
                {
                    _gameData._GameModes[id] = mode;
                    _tracker.RecordAdd(Categories.GameModes, id.Id);
                }
            }
            public void AddOrReplaceGameMode(string id, GameModeDefinition mode) => AddOrReplaceGameMode(new GameModeId(id), mode);

            // ── UnlockableStoreContents ────────────────────────
            public void AddUnlockableStoreContent(UnlockableStoreContentId id, IUnlockableStoreContent content)
            {
                if (_gameData._UnlockableStoreContents.TryAdd(id, content))
                    _tracker.RecordAdd(Categories.UnlockableStoreContents, id.Id);
                else
                    throw new InvalidOperationException($"UnlockableStoreContent with id '{id.Id}' already exists.");
            }
            public void AddUnlockableStoreContent(string id, IUnlockableStoreContent content) => AddUnlockableStoreContent(new UnlockableStoreContentId(id), content);

            public void ReplaceUnlockableStoreContent(UnlockableStoreContentId id, IUnlockableStoreContent content)
            {
                if (_gameData._UnlockableStoreContents.ContainsKey(id))
                {
                    _gameData._UnlockableStoreContents[id] = content;
                    _tracker.RecordReplace(Categories.UnlockableStoreContents, id.Id);
                }
                else
                    throw new InvalidOperationException($"UnlockableStoreContent with id '{id.Id}' does not exist.");
            }
            public void ReplaceUnlockableStoreContent(string id, IUnlockableStoreContent content) => ReplaceUnlockableStoreContent(new UnlockableStoreContentId(id), content);

            public void AddOrReplaceUnlockableStoreContent(UnlockableStoreContentId id, IUnlockableStoreContent content)
            {
                if (_gameData._UnlockableStoreContents.ContainsKey(id))
                {
                    _gameData._UnlockableStoreContents[id] = content;
                    _tracker.RecordReplace(Categories.UnlockableStoreContents, id.Id);
                }
                else
                {
                    _gameData._UnlockableStoreContents[id] = content;
                    _tracker.RecordAdd(Categories.UnlockableStoreContents, id.Id);
                }
            }
            public void AddOrReplaceUnlockableStoreContent(string id, IUnlockableStoreContent content) => AddOrReplaceUnlockableStoreContent(new UnlockableStoreContentId(id), content);

            // ── DifficultyPresets (list-backed) ─────────────────
            public void AppendDifficultyPreset(GameDifficultyPreset preset)
            {
                _gameData._DifficultyPresets.Add(preset);
                _tracker.RecordAdd(Categories.DifficultyPresets, preset.UniqueId ?? "?");
            }
        }

        // ── Categories (ordered for summary output) ────────────

        private static class Categories
        {
            public const string ColorSchemes = "ColorSchemes";
            public const string DifficultyPresets = "DifficultyPresets";
            public const string GameModes = "GameModes";
            public const string GameRules = "GameRules";
            public const string Icons = "Icons";
            public const string Images = "Images";
            public const string ShapesConfigurations = "ShapesConfigurations";
            public const string TutorialConfigs = "TutorialConfigs";
            public const string UnlockableStoreContents = "UnlockableStoreContents";
            public const string Videos = "Videos";
            public const string WikiEntries = "WikiEntries";

            public static readonly string[] AllSorted = new[]
            {
                ColorSchemes,
                DifficultyPresets,
                GameModes,
                GameRules,
                Icons,
                Images,
                ShapesConfigurations,
                TutorialConfigs,
                UnlockableStoreContents,
                Videos,
                WikiEntries,
            };
        }

        // ── Change Tracking ────────────────────────────────────

        internal class ChangeTracker
        {
            private Dictionary<string, ChangeRecord> _records = new Dictionary<string, ChangeRecord>();

            public void RecordAdd(string category, string id)
            {
                GetOrCreate(category).Added.Add(id);
            }

            public void RecordReplace(string category, string id)
            {
                GetOrCreate(category).Replaced.Add(id);
            }

            private ChangeRecord GetOrCreate(string category)
            {
                if (!_records.TryGetValue(category, out var record))
                {
                    record = new ChangeRecord();
                    _records[category] = record;
                }
                return record;
            }

            public string BuildSummary(bool detailed)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Resource registration summary:");

                foreach (var category in Categories.AllSorted)
                {
                    if (!_records.TryGetValue(category, out var record))
                        continue;

                    sb.Append("  ");
                    sb.Append(category);
                    sb.Append(": +");
                    sb.Append(record.Added.Count);
                    sb.Append(" ~");
                    sb.Append(record.Replaced.Count);

                    if (detailed && (record.Added.Count > 0 || record.Replaced.Count > 0))
                    {
                        sb.AppendLine();
                        if (record.Added.Count > 0)
                        {
                            sb.Append("    Added: ");
                            sb.AppendJoin(", ", record.Added);
                        }
                        if (record.Replaced.Count > 0)
                        {
                            if (record.Added.Count > 0) sb.AppendLine();
                            sb.Append("    Replaced: ");
                            sb.AppendJoin(", ", record.Replaced);
                        }
                    }

                    sb.AppendLine();
                }

                return sb.ToString().TrimEnd('\r', '\n');
            }

            private class ChangeRecord
            {
                public List<string> Added = new List<string>();
                public List<string> Replaced = new List<string>();
            }
        }

        // ── Public API ─────────────────────────────────────────

        public static void Register(Action<IGameDataHelper> registerCallback)
        {
            if (_isInjecting)
            {
                throw new InvalidOperationException("Cannot call Register in registerCallback!");
            }
            if (_isFrozen)
            {
                throw new InvalidOperationException("Resources registration has been done and cannot register anymore, try registering during MOD construction");
            }
            if (registerCallback != null) _callbacks.Add(registerCallback);
        }

        public static void OnGameDataInitialize(GameData gameData)
        {
            ExecuteInject(gameData);
        }

        // ── Internal ───────────────────────────────────────────

        private static void ExecuteInject(GameData gameData)
        {
            _isInjecting = true;
            var tracker = new ChangeTracker();

            try
            {
                foreach (var callback in _callbacks)
                {
                    try
                    {
                        var helper = new GameDataHelper(gameData, tracker);
                        callback(helper);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            finally
            {
                _isFrozen = true;
                _isInjecting = false;
            }

            LogSummary(tracker);
        }

        private static void LogSummary(ChangeTracker tracker)
        {
            var summary = tracker.BuildSummary(DetailedLogging);
            Logger?.Info?.Log(summary);
        }
    }
}
