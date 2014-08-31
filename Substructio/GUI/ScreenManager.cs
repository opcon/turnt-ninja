using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using QuickFont;
using Substructio.Core;

namespace Substructio.GUI
{
    public class ScreenManager
    {
        #region Member Variables

        #endregion

        #region Properties

        private readonly List<Screen> ScreensToAdd;
        private readonly List<Screen> ScreensToRemove;

        private bool InputScreenFound;
        public List<Screen> Screens;

        public Camera ScreenCamera { get; private set; }
        public static QFont Font { get; private set; }
        public GameWindow Game { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        public ScreenManager(GameWindow g, Camera c)
        {
            Game = g;
            Screens = new List<Screen>();
            ScreensToAdd = new List<Screen>();
            ScreensToRemove = new List<Screen>();
            Font = new QFont(Directories.LibrariesDirectory + Directories.TextFile, 18);
            ScreenCamera = c;
            ScreenCamera.Center = Vector2.Zero;
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
            InputSystem.Update(Game.Focused);
        }

        public void DrawTextLine(string text, Vector2 position)
        {
            Utilities.TranslateTo(position, ScreenCamera.PreferredWidth, ScreenCamera.PreferredHeight);

            Font.Print(text);
        }

        public void DrawProcessedText(ProcessedText pText, Vector2 position, QFont font)
        {
            Utilities.TranslateTo(position, ScreenCamera.PreferredWidth, ScreenCamera.PreferredHeight);

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
            ScreenCamera.UpdateResize(Game.Width, Game.Height);
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