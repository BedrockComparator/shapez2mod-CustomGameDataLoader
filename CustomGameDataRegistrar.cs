using Game.Core.GameData.GameModeDefinition;
using Game.Core.Research;
using Global.Store;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomGameDataLoader
{
    public static class CustomGameDataRegistrar
    {
        private static bool _isFrozen = false;
        private static bool _isInjecting = false;
        private static List<Action<IGameDataHelper>> _callbacks = new List<Action<IGameDataHelper>>();

        public interface IGameDataHelper
        {
            void AddImage(GameImageId id, Sprite sprite);
            void AddImage(string id, Sprite sprite);
            void AddIcon(GameIconId id, Sprite sprite);
            void AddIcon(string id, Sprite sprite);
            void AddVideo(GameVideoId id, MetaVideoDefinition video);
            void AddVideo(string id, MetaVideoDefinition video);
            void AddShapesConfiguration(ShapesConfigurationId id, ShapesConfiguration config);
            void AddShapesConfiguration(string id, ShapesConfiguration config);
            void AddColorScheme(ShapeColorSchemeId id, ShapeColorScheme scheme);
            void AddColorScheme(string id, ShapeColorScheme scheme);
            void AddWikiEntry(WikiEntryId id, MetaWikiEntry entry);
            void AddWikiEntry(string id, MetaWikiEntry entry);
            void AddTutorialConfig(TutorialConfigId id, MetaTutorialConfig config);
            void AddTutorialConfig(string id, MetaTutorialConfig config);
            void AddGameRule(GameRuleId id, MetaGameRule rule);
            void AddGameRule(string id, MetaGameRule rule);
            void AddGameMode(GameModeId id, GameModeDefinition mode);
            void AddGameMode(string id, GameModeDefinition mode);
            void AddUnlockableStoreContent(UnlockableStoreContentId id, IUnlockableStoreContent content);
            void AddUnlockableStoreContent(string id, IUnlockableStoreContent content);

            GameData getGameData();
        }

        public class GameDataHelper : IGameDataHelper
        {
            private GameData _gameData;
            public GameData getGameData()
            {
                return _gameData;
            }

            public GameDataHelper(GameData gameData)
            {
                _gameData = gameData;
            }
            //Todo: Add helper function
            public void AddImage(GameImageId id, Sprite sprite)
            {
                _gameData._Images.TryAdd(id, sprite);
            }
            public void AddImage(string id, Sprite sprite)
            {
                AddImage(new GameImageId(id), sprite);
            }
            public void AddIcon(GameIconId id, Sprite sprite)
            {
                _gameData._Icons.AddIcon(id, sprite);
            }
            public void AddIcon(string id, Sprite sprite)
            {
                AddIcon(new GameIconId(id), sprite);
            }
            public void AddVideo(GameVideoId id, MetaVideoDefinition video)
            {
                _gameData._Videos.TryAdd(id, video);
            }
            public void AddVideo(string id, MetaVideoDefinition video)
            {
                AddVideo(new GameVideoId(id), video);
            }
            public void AddShapesConfiguration(ShapesConfigurationId id, ShapesConfiguration config)
            {
                _gameData._ShapesConfigurations.TryAdd(id, config);
            }
            public void AddShapesConfiguration(string id, ShapesConfiguration config)
            {
                AddShapesConfiguration(new ShapesConfigurationId(id), config);
            }
            public void AddColorScheme(ShapeColorSchemeId id, ShapeColorScheme scheme)
            {   
                _gameData._ColorSchemes.TryAdd(id, scheme);
            }
            public void AddColorScheme(string id, ShapeColorScheme scheme)
            {
                AddColorScheme(new ShapeColorSchemeId(id), scheme);
            }
            public void AddWikiEntry(WikiEntryId id, MetaWikiEntry entry)
            {
                _gameData._WikiEntries.TryAdd(id,entry);
            }
            public void AddWikiEntry(string id, MetaWikiEntry entry)
            {
                AddWikiEntry(new WikiEntryId(id), entry);
            }
            public void AddTutorialConfig(TutorialConfigId id, MetaTutorialConfig config)
            {
                _gameData._TutorialConfigs.TryAdd(id, config);
            }
            public void AddTutorialConfig(string id, MetaTutorialConfig config)
            {
                AddTutorialConfig(new TutorialConfigId(id), config);
            }
            public void AddGameRule(GameRuleId id, MetaGameRule rule)
            {
                _gameData._GameRules.TryAdd(id, rule);
            }
            public void AddGameRule(string id, MetaGameRule rule)
            {
                AddGameRule(new GameRuleId(id), rule);
            }
            public void AddGameMode(GameModeId id, GameModeDefinition mode)
            {
                _gameData._GameModes.TryAdd(id, mode);
            }
            public void AddGameMode(string id, GameModeDefinition mode)
            {
                AddGameMode(new GameModeId(id), mode);
            }
            public void AddUnlockableStoreContent(UnlockableStoreContentId id, IUnlockableStoreContent content)
            {
                _gameData._UnlockableStoreContents.TryAdd(id, content);
            }
            public void AddUnlockableStoreContent(string id, IUnlockableStoreContent content)
            {
                AddUnlockableStoreContent(new UnlockableStoreContentId(id), content);
            }
        }

        public static void Register(Action<IGameDataHelper> registerCallback)
        {
            if (_isInjecting)
            {
                throw new InvalidOperationException("Cannot call RegisterLazy in registerCallback!");
            }
            if (_isFrozen)
            {
                throw new InvalidOperationException("Resources registration has been done and cannot register anymore, try registering during MOD construction");
            }
            if (registerCallback != null) _callbacks.Add(registerCallback);
        }
        
        private static void ExecuteInject(GameData gameData)
        {
            _isInjecting = true;
            try
            {
                foreach(var callback in _callbacks)
                {
                    try
                    {
                        GameDataHelper helper = new GameDataHelper(gameData);
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
        }
        public static void OnGameDataInitlize(GameData gamedata)
        {
            ExecuteInject(gamedata);
        }
    }
}
