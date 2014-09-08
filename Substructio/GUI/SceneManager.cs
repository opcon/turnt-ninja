using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using QuickFont;
using Substructio.Core;

namespace Substructio.GUI
{
    public class SceneManager
    {
        #region Member Variables

        #endregion

        #region Properties

        private readonly List<Scene> _screensToAdd;
        private readonly List<Scene> _screensToRemove;

        private bool InputScreenFound;
        public List<Scene> ScreenList;

        public Camera ScreenCamera { get; private set; }
        public static QFont Font { get; private set; }
        public GameWindow GameWindow { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        public SceneManager(GameWindow gameWindow, Camera camera)
        {
            GameWindow = gameWindow;
            ScreenList = new List<Scene>();
            _screensToAdd = new List<Scene>();
            _screensToRemove = new List<Scene>();
            //Font = new QFont(Directories.LibrariesDirectory + Directories.TextFile, 18);
            ScreenCamera = camera;
            ScreenCamera.Center = Vector2.Zero;
        }

        #endregion

        #region Public Methods

        public void Draw(double time)
        {
            Scene excl = ScreenList.Where(screen => screen.Visible).FirstOrDefault(screen => screen.Exclusive);
            if (excl == null)
            {
                foreach (Scene screen in ScreenList.Where(screen => screen.Visible))
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

            //if (!ScreenList.Last().Loaded)
            //{
            //    ScreenList.Last().Load();
            //}
            //else
            //{

            //    ScreenList.Last().Update(time, true);
            //}
            for (int i = ScreenList.Count - 1; i >= 0; i--)
            {
                if (!ScreenList[i].Loaded)
                {
                    ScreenList[i].Load();
                }
                else
                {
                    if (!InputScreenFound && ScreenList[i].Visible)
                    {
                        ScreenList[i].Update(time, true);
                        InputScreenFound = true;
                    }
                    else
                    {
                        ScreenList[i].Update(time);
                    }
                }
            }
            InputScreenFound = false;
            InputSystem.Update(GameWindow.Focused);
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
            foreach (Scene screen in _screensToRemove)
            {
                ScreenList.Remove(screen);
            }

            foreach (Scene screen in _screensToAdd)
            {
                ScreenList.Add(screen);
            }

            _screensToAdd.Clear();
            _screensToRemove.Clear();
        }

        public void Resize(EventArgs e)
        {
            ScreenCamera.UpdateResize(GameWindow.Width, GameWindow.Height);
            foreach (Scene screen in ScreenList)
            {
                screen.Resize(e);
            }
        }

        public void AddScreen(Scene s)
        {
            s.SceneManager = this;
            _screensToAdd.Add(s);
        }

        public void RemoveScreen(Scene s)
        {
            s.UnLoad();
            _screensToRemove.Add(s);
        }

        public void UnLoad()
        {
            foreach (Scene screen in ScreenList)
            {
                screen.UnLoad();
            }
            ScreenList.Clear();
        }

        #endregion

        #region Private Methods

        #endregion
    }
}