using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetection.Core
{
    public class Directories
    {
        private static readonly string Resources = @"..\..\Resources";
        private static readonly string Sprites = @"\Sprites";
        private static readonly string Libraries = @"\Libraries";
        private static readonly string Backgrounds = @"\Backgrounds";
        private static readonly string Tiles = @"\Tiles";
        private static readonly string Entities = @"\Entities";
        private static readonly string Worlds = @"\Worlds";
        private static readonly string UI = @"\UI";
        private static readonly string Fonts = @"\Fonts";
        private static readonly string Shaders = @"\Shaders";
        public static readonly string TextFile = @"\Comfortaa-Regular.ttf";

        public static DirectoryInfo ResourcesDirectory,
            SpritesDirectory,
            LibrariesDirectory,
            BackgroundsDirectory,
            TilesDirectory,
            EntitiesDirectory,
            WorldsDirectory,
            UIDirectory,
            FontsDirectory,
            ShaderDirectory;

        static Directories()
        {
            FixPathSeparators(ref Resources);
            FixPathSeparators(ref Sprites);
            FixPathSeparators(ref Libraries);
            FixPathSeparators(ref Backgrounds);
            FixPathSeparators(ref Tiles);
            FixPathSeparators(ref Entities);
            FixPathSeparators(ref Worlds);
            FixPathSeparators(ref UI);
            FixPathSeparators(ref Fonts);
            FixPathSeparators(ref Shaders);

            ResourcesDirectory = new DirectoryInfo(Resources);
            SpritesDirectory = new DirectoryInfo(Resources + Sprites);
            LibrariesDirectory = new DirectoryInfo(Resources + Libraries);
            TilesDirectory = new DirectoryInfo(Resources + Tiles);
            BackgroundsDirectory = new DirectoryInfo(Resources + Backgrounds);
            EntitiesDirectory = new DirectoryInfo(Resources + Entities);
            WorldsDirectory = new DirectoryInfo(Resources + Worlds);
            UIDirectory = new DirectoryInfo(Resources + UI);
            FontsDirectory = new DirectoryInfo(Resources + Fonts);
            ShaderDirectory = new DirectoryInfo(Resources + Shaders);
        }

        private static void FixPathSeparators(ref string path)
        {
            path = path.Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
