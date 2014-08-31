using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using QuickFont;

namespace DownTrodden.Core
{
    internal class ScreenManager
    {
        #region Member Variables

        #endregion

        #region Properties

        private readonly List<Screen> ScreensToAdd;
        private readonly List<Screen> ScreensToRemove;
        public static QFont m_Font;

        private readonly Camera m_ScreenCamera;
        private bool InputScreenFound;
        public List<Screen> Screens;

        public Camera ScreenCamera
        {
            get { return m_ScreenCamera; }
        }

        public static QFont Font
        {
            get { return m_Font; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        public ScreenManager()
        {
            Screens = new List<Screen>();
            ScreensToAdd = new List<Screen>();
            ScreensToRemove = new List<Screen>();
            m_Font = new QFont(Directories.LibrariesDirectory + Directories.TextFile, 18);
            m_ScreenCamera = new Camera();
            m_ScreenCamera.CameraBounds = new Polygon(Vector2.Zero, 1920, 1080);
            m_ScreenCamera.Center = Vector2.Zero;
        }

        #endregion

        #region Public Methods

        public void Draw(double time)
        {
            Screen excl = Screens.Where(screen => screen.Visible).Where(screen => screen.Exclusive).FirstOrDefault();
            if (excl == null)
            {
                foreach (Screen screen in Screens.Where(screen => screen.Visible))
                {
                    screen.Draw(time);
                }
            }
            else
            {
                excl.Draw(time);
            }
        }

        public void Update(double time)
        {
            AddRemoveScreens();

            ScreenCamera.Update(time);
            //ScreenCamera.SnapToCenter();
            ScreenCamera.UpdateProjectionMatrix();
            ScreenCamera.UpdateModelViewMatrix();

            //if (!Screens.Last().Loaded)
            //{
            //    Screens.Last().Load();
            //}
            //else
            //{

            //    Screens.Last().Update(time, true);
            //}
            for (int i = Screens.Count - 1; i >= 0; i--)
            {
                if (!Screens[i].Loaded)
                {
                    Screens[i].Load();
                }
                else
                {
                    if (!InputScreenFound && Screens[i].Visible)
                    {
                        Screens[i].Update(time, true);
                        InputScreenFound = true;
                    }
                    else
                    {
                        Screens[i].Update(time);
                    }
                }
            }
            InputScreenFound = false;
            InputSystem.Update();
        }

        public static void DrawTextLine(string text, Vector2 position)
        {
            Utilities.TranslateTo(position);

            Font.Print(text);
        }

        public void DrawProcessedText(ProcessedText pText, Vector2 position, QFont font)
        {
            Utilities.TranslateTo(position);

            font.Print(pText);
        }

        private void AddRemoveScreens()
        {
            foreach (Screen screen in ScreensToRemove)
            {
                Screens.Remove(screen);
            }

            foreach (Screen screen in ScreensToAdd)
            {
                Screens.Add(screen);
            }

            ScreensToAdd.Clear();
            ScreensToRemove.Clear();
        }

        public void Resize(EventArgs e)
        {
            ScreenCamera.UpdateResize();
            foreach (Screen screen in Screens)
            {
                screen.Resize(e);
            }
        }

        public void AddScreen(Screen s)
        {
            s.ScreenManager = this;
            ScreensToAdd.Add(s);
        }

        public void RemoveScreen(Screen s)
        {
            s.UnLoad();
            ScreensToRemove.Add(s);
        }

        public void UnLoad()
        {
            foreach (Screen screen in Screens)
            {
                screen.UnLoad();
            }
            Screens.Clear();
        }

        #endregion

        #region Private Methods

        #endregion
    }
}