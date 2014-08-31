using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace Substructio.Core
{
    public class Directories
    {
        private static readonly string Resources = @"..\Resources";
        private static readonly string Sprites = @"..\Sprites";
        private static readonly string Libraries = @"\Libraries";
        private static readonly string Backgrounds = @"\Backgrounds";
        private static readonly string Tiles = @"\Tiles";
        private static readonly string Entities = @"\Entities";
        private static readonly string Worlds = @"\Worlds";
        private static readonly string UI = @"\UI";
        public static readonly string TextFile = @"\Comfortaa-Regular.ttf";

        public static DirectoryInfo ResourcesDirectory,
                                    SpritesDirectory,
                                    LibrariesDirectory,
                                    BackgroundsDirectory,
                                    TilesDirectory,
                                    EntitiesDirectory,
                                    WorldsDirectory,
                                    UIDirectory;

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

            ResourcesDirectory = new DirectoryInfo(Resources);
            SpritesDirectory = new DirectoryInfo(Resources + Sprites);
            LibrariesDirectory = new DirectoryInfo(Resources + Libraries);
            TilesDirectory = new DirectoryInfo(Resources + Tiles);
            BackgroundsDirectory = new DirectoryInfo(Resources + Backgrounds);
            EntitiesDirectory = new DirectoryInfo(Resources + Entities);
            WorldsDirectory = new DirectoryInfo(Resources + Worlds);
            UIDirectory = new DirectoryInfo(Resources + UI);
        }

        private static void FixPathSeparators(ref string path)
        {
            path = path.Replace('\\', Path.DirectorySeparatorChar);
        }
    }

    public static class InfoNotUsed
    {
        private static List<Key> m_LastKeys = new List<Key>();
        private static readonly List<Key> m_CurrentKeys = new List<Key>();

        private static List<MouseButton> m_LastButtons = new List<MouseButton>();
        private static readonly List<MouseButton> m_CurrentButtons = new List<MouseButton>();

        public static bool IsMouseButtonClicked(MouseButton button)
        {
            //if (Mouse.GetState().IsButtonDown(button) && Game.MouseInWindow)
            //{
            //    m_CurrentButtons.Add(button);
            //    return m_LastButtons.Contains(button);
            //}
            //return false;
            throw new Exception("Info class is deprecated.");
        }

        public static void UpdateMouseButtons()
        {
            m_LastButtons = new List<MouseButton>(m_CurrentButtons);
            m_CurrentButtons.Clear();
        }

        public static bool IsKeyPressed(Key k)
        {
            m_CurrentKeys.Add(k);
            return !m_LastKeys.Contains(k);
        }

        public static void UpdateKeys()
        {
            m_LastKeys = new List<Key>(m_CurrentKeys);
            m_CurrentKeys.Clear();
        }
    }

    public static class Utilities
    {
        private static Random m_Random;

        public static Random RandomGenerator
        {
            get { return m_Random ?? (m_Random = new Random()); }
        }

        public static List<Vector2> StringToVecList(IEnumerable<string> bcoords)
        {
            try
            {
                return bcoords.Select(s => Array.ConvertAll(s.Split(','), str => (Single.Parse(str)))).Select(
            point => new Vector2(point[0], point[1])).ToList();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public static string VecListToString(List<Vector2> positions)
        {
            string s = "";
            foreach (var p in positions)
            {
                s += String.Format("{0},{1} ", p.X, p.Y);
            }
            return s.Trim();
        }

        public static void TranslateTo(Vector2 position, float width, float height)
        {
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            GL.Translate(-(width/2) + position.X, -(height/2) + position.Y, 0);

            GL.Scale(1, -1, 1);
        }

        // Handles IPv4 and IPv6 notation.
        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            IPAddress ip;
            if (ep.Length == 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    throw new FormatException("Invalid IP Address");
                }
            }
            else if (ep.Length ==1)
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid IP Address");
                }
                return new IPEndPoint(ip, 0);
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid IP Address");
                }
            }
            int port;
            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid Port");
            }
            return new IPEndPoint(ip, port);
        }

        public static void TakeScreenShot(GameWindow g, string file)
        {
            Bitmap b = ScreenToBitmap(g);
            b.Save(file, ImageFormat.Png);
        }

        public static void Dummy(bool b) { }

        public static Bitmap ScreenToBitmap(GameWindow g)
        {
            Bitmap b = new Bitmap(g.Width, g.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            var bd = b.LockBits(new Rectangle(0, 0, g.Width, g.Height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            GL.ReadPixels(0, 0, g.Width, g.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, bd.Scan0);

            b.UnlockBits(bd);
            b.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return b;
        }
    }
}