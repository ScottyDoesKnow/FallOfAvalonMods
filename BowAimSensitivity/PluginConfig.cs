using BepInEx.Configuration;

namespace BowAimSensitivity;

public class PluginConfig
{
    public ConfigEntry<float> ZoomSensitivityFactor { get; private set; }
    public ConfigEntry<float> ExtraZoomSensitivityFactor { get; private set; }
    public ConfigEntry<float> SlowTimeSensitivityFactor { get; private set; }

    public PluginConfig(ConfigFile config)
    {
        config.SaveOnConfigSet = false;
        try
        {
            const string section = "SensitivityFactors";
            ZoomSensitivityFactor = config.Bind(section, nameof(ZoomSensitivityFactor), 0.9f,
                $"Multiplier for mouse sensitivity when aiming without '{Plugin.ExtraZoomTalentInfo.Name}' skill.");
            ExtraZoomSensitivityFactor = config.Bind(section, nameof(ExtraZoomSensitivityFactor), 0.7f,
                $"Multiplier for mouse sensitivity when aiming with '{Plugin.ExtraZoomTalentInfo.Name}' skill.");
            SlowTimeSensitivityFactor = config.Bind(section, nameof(SlowTimeSensitivityFactor), 0.5f,
                $"Multiplier for mouse sensitivity when aiming and drawing with '{Plugin.SlowTimeTalentInfo.Name}' skill.");
        }
        finally
        {
            config.Save();
            config.SaveOnConfigSet = true;
        }
    }
}
