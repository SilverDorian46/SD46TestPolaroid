using System;

namespace Celeste.Mod.SD46TestPolaroid;

public class SD46TestPolaroidModule : EverestModule
{
    public static SD46TestPolaroidModule Instance { get; private set; } = null!;

    /*public override Type SettingsType => typeof(SD46TestPolaroidModuleSettings);
    public static SD46TestPolaroidModuleSettings Settings => (SD46TestPolaroidModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(SD46TestPolaroidModuleSession);
    public static SD46TestPolaroidModuleSession Session => (SD46TestPolaroidModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(SD46TestPolaroidModuleSaveData);
    public static SD46TestPolaroidModuleSaveData SaveData => (SD46TestPolaroidModuleSaveData) Instance._SaveData;*/

    public SD46TestPolaroidModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(SD46TestPolaroidModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(SD46TestPolaroidModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        // TODO: apply any hooks that should always be active
        //On.Celeste.Level.Render += OnLevelRender;
    }

    public override void Unload()
    {
        // TODO: unapply any hooks applied in Load()
        //On.Celeste.Level.Render -= OnLevelRender;
    }

    /*private static void OnLevelRender(On.Celeste.Level.orig_Render orig, Level level)
    {
        orig(level);

        foreach (var e in level.Tracker.GetEntities<PolaroidCamera>())
        {
            (e as PolaroidCamera)?.BeforeRender();
        }
    }*/
}