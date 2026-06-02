using Core.Dependency;
using Cysharp.Threading.Tasks;
using Global.Initialization;
using MonoMod.RuntimeDetour;
using ShapezShifter.SharpDetour;
using System;

namespace CustomGameDataLoader
{
    public class CustomGameDataLoaderMod : IMod
    {
        Hook HookPostfixGameDataInitialization;

        public CustomGameDataLoaderMod(Core.Logging.ILogger logger)
        {
            CustomGameDataRegistrar.Logger = logger;

            HookPostfixGameDataInitialization = DetourHelper.CreatePostfixHook<LoadGameDataBlindStep, IDependencyContainer, UniTask>(
                original: (self, dc) => self.Execute(dc),
                postfix: (self, dc, ret) =>
                {
                    logger?.Info?.Log("Custom GameData Injection Begin");
                    GameData gameData = (GameData)dc.Resolve<IGameData>();
                    CustomGameDataRegistrar.OnGameDataInitialize(gameData);
                    logger?.Info?.Log("Custom GameData Injection Complete");
                    return ret;
                }
            );

            logger?.Info?.Log("CustomGameDataLoaderMod Initialized.");
        }

        void IDisposable.Dispose()
        {
            HookPostfixGameDataInitialization?.Dispose();
        }
    }
}
