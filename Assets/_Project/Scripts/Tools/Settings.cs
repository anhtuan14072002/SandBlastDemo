using System.Collections.Generic;

namespace HootyBird.ColoringBook.Tools
{
    public static class Settings
    {
        public static class EditorSettings
        {
            /// <summary>
            /// When number of polygons in PolygonColoringBook value is higher than this value, editor will build (and save) meshes for
            /// the coloring book that is currently being edited. This is done to speed-up coloring book loading time on a target device.
            /// </summary>
            public readonly static int BuildMeshesCountValue = 10;
        }

        public static class InternalAppSettings
        {
            public static string MainMenuControllerName = "MainMenuCanvas";
            public static string GameplayMenuControllerName = "GameplayCanvas";
            /// <summary>
            /// Target framerate.
            /// </summary>
            public static int TargetFramerate = 120;

            /// <summary>
            /// What downscale value to use for pixel counts less than specified below.
            /// </summary>
            public readonly static (int size, int downscale)[] DownscaleSettings = new (int size, int downscale)[]
            {
                new (65_536, 1),        // for less than 256*256
                new (262_144, 2),       // for less than 512*512
                new (1_048_576, 4),     // for less than 1024*1024
                new (4_194_304, 8),    // for less than 2048*2048
                new (16_777_216, 16),   // for less than 4096*4096
            };

            /// <summary>
            /// Error sizes. Region counts as "colored" when value within error is reached.
            /// </summary>
            public static (int size, float errorSize)[] RegionsErrorSize = new (int size, float errorSize)[] 
            { 
                new (1_024, .92f), // 32*32, 8% error
                new (2_048, .94f), // 32*64, 6% error
                new (4_096, .95f), // 64*64, 5% error
                new (8_192, .96f), // 128*64, 4% error
                new (16_384, .975f), // 128*128, 2.5% error
                new (65_536, .985f), // 256*256, 1.5% error
            };

            // Brush size settings.
            public static readonly float MinBrushSize = .2f;
            public static readonly float MaxBrushSize = .5f;
            public static readonly float DefaultBrushSize = .3f;

            public static readonly bool SaveProgress = true;
            public static readonly string SaveFolderPath = "SavedGames";
            public static readonly int MinFilledPixelsInRegionToSave = 10;
        }

        /// <summary>
        /// Default app settings.
        /// </summary>
        public static class AppSettings
        {
            public static Dictionary<SettingsOptions, bool> DefaultSettings = new Dictionary<SettingsOptions, bool>()
            {
                [SettingsOptions.ShuffleColors] = true,
            };
        }
    }

    public enum SettingsOptions
    {
        ShuffleColors,
    }
}
