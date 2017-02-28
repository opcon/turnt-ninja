using Substructio.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickFont;
using QuickFont.Configuration;
using Substructio.Core;
using OpenTK;
using System.Drawing;
using OpenTK.Graphics;
using Substructio.Logging;
using System.Diagnostics;

namespace TurntNinja.GUI
{
    class FirstRunScene : Scene
    {
        GameFont _bodyFont;
        GameFont _headerFont;
        QFontDrawing _fontDrawing;

        SizeF _headerSize;
        SizeF _bodyText1Size;
        SizeF _bodyListSize;
        SizeF _bodyText2Size;

        float _bodyTextWidth;

        string _headerText = "WELCOME TO TURNT NINJA";

        string _bodyList =
            "* Operating system version\n" +
            "* Game resolution\n" +
            "* Number of songs played\n" +
            "* Crash reports\n";

        string _bodyText1 =
            "Thank you for downloading my game!\nThis game has optional analytics " +
            "and crash reporting, which helps with development. The following "+
            "data is collected if you opt in:\n";

        string _bodyText2 = 
            "\nPlease press Enter to opt in, or Escape to opt out.\n\nThank you,\nPatrick";

        public FirstRunScene()
        {
            Exclusive = true;
        }

        public override void Load()
        {
            _bodyFont = SceneManager.GameFontLibrary.GetFirstOrDefault("largebody");
            _headerFont = SceneManager.GameFontLibrary.GetFirstOrDefault(GameFontType.Menu);

            _fontDrawing = new QFontDrawing();
            _fontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;

            _headerSize = _headerFont.Font.Measure(_headerText);

            _bodyTextWidth = 0.75f;
            _bodyText1Size = _bodyFont.Font.Measure(_bodyText1, _bodyTextWidth * WindowWidth, QFontAlignment.Centre);
            _bodyText2Size = _bodyFont.Font.Measure(_bodyText2, _bodyTextWidth * WindowWidth, QFontAlignment.Centre);
            _bodyListSize = _bodyFont.Font.Measure(_bodyList, _bodyTextWidth * WindowWidth, QFontAlignment.Left);

            ServiceLocator.Settings["FirstRun"] = false;

            var informationalVersionAttribute = System.Reflection.Assembly.GetExecutingAssembly().CustomAttributes.FirstOrDefault(cad => cad.AttributeType == typeof(System.Reflection.AssemblyInformationalVersionAttribute));
            string tag = ((string)informationalVersionAttribute.ConstructorArguments.First().Value).Split(' ')[0].Split(':')[1];
            if (tag.Length > 1)
                ServiceLocator.Settings["GetAlphaReleases"] = true;

            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
        }

        public override void Dispose()
        {
            _fontDrawing.Dispose();
        }

        public override void Draw(double time)
        {
            _fontDrawing.RefreshBuffers();
            _fontDrawing.Draw();
        }

        public override void Resize(EventArgs e)
        {
            _fontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
        }

        public override void Update(double time, bool focused = false)
        {
            if (InputSystem.NewKeys.Contains(OpenTK.Input.Key.Enter))
            {
                ServiceLocator.Settings["Analytics"] = true;
                ServiceLocator.Settings.Save();

                // Track application startup
                ServiceLocator.Analytics.TrackApplicationStartup();

                SceneManager.RemoveScene(this, true);
            }
            else if (InputSystem.NewKeys.Contains(OpenTK.Input.Key.Escape))
            {
                ServiceLocator.Settings["Analytics"] = false;

                // Disable analytics and error reporting
                ServiceLocator.Analytics = new NullAnalytics();
                ServiceLocator.ErrorReporting = new NullErrorReporting();

                SceneManager.RemoveScene(this, true);
            }

            _fontDrawing.DrawingPrimitives.Clear();

            var headerOffset = (WindowHeight/2.0f - WindowHeight/12.0f);
            _fontDrawing.Print(_headerFont.Font, _headerText, new Vector3(0, headerOffset + (_headerSize.Height / 2.0f), 0), QFontAlignment.Centre, Color.White);
            _fontDrawing.Print(_bodyFont.Font, _bodyText1, new Vector3(0, headerOffset - (_headerSize.Height), 0), new SizeF(WindowWidth * _bodyTextWidth, -1), QFontAlignment.Centre);
            _fontDrawing.Print(_bodyFont.Font, _bodyList, new Vector3(-WindowWidth*0.25f, (headerOffset - (_headerSize.Height)) - _bodyText1Size.Height, 0), new SizeF(WindowWidth * _bodyTextWidth, -1), QFontAlignment.Left);
            _fontDrawing.Print(_bodyFont.Font, _bodyText2, new Vector3(0, (headerOffset - (_headerSize.Height)) - _bodyText1Size.Height - _bodyListSize.Height, 0), new SizeF(WindowWidth * _bodyTextWidth, -1), QFontAlignment.Centre);
        }

        public override void EnterFocus()
        {
        }

        public override void ExitFocus()
        {
        }
    }
}
