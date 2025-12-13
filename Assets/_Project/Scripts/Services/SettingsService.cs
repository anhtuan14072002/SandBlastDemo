using System;
using UnityEngine;
using HootyBird.ColoringBook.Tools;
using static HootyBird.ColoringBook.Tools.Settings;

namespace HootyBird.ColoringBook.Services
{
    /// <summary>
    /// Service for handling game settings.
    /// </summary>
    public class SettingsService
    {
        private static string Prefix = "App_Settings_";

        public static Action<SettingsOptions, bool> OnSettingsChanged { get; set; }

        public static bool GetSettingValue(SettingsOptions setting)
        {
            return PlayerPrefs.GetInt($"{Prefix}{setting}", AppSettings.DefaultSettings[setting] ? 1 : 0) == 1;
        }

        public static void SetSettingValue(SettingsOptions setting, bool value)
        {
            PlayerPrefs.SetInt($"{Prefix}{setting}", value ? 1 : 0);

            OnSettingsChanged?.Invoke(setting, value);
        }
    }
}
