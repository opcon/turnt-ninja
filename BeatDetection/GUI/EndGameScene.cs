using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using QuickFont;
using Substructio.Core;
using Substructio.GUI;

namespace BeatDetection.GUI
{
    class EndGameScene : Scene
    {
        private QFont _font;
        private QFontDrawing _fontDrawing;
        private SizeF _endGameTextSize;
        private string _endGameText = "Song Finished\nPress Enter to Continue";

        public EndGameScene()
        {
            Exclusive = true;
        }

        public override void Load()
        {
            _font = new QFont(SceneManager.FontPath, 50, new QFontBuilderConfiguration(true), FontStyle.Italic);
            _fontDrawing = new QFontDrawing();
            UpdateText();
            SceneManager.RemoveScene(ParentScene);
            SceneManager.ScreenCamera.ExtraScale = 0;
            SceneManager.ScreenCamera.Scale = new Vector2(1, 1);
            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
        }

        public override void Resize(EventArgs e)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            _fontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _fontDrawing.DrawingPrimitives.Clear();
            _endGameTextSize = _font.Measure(_endGameText);
            _fontDrawing.Print(_font, _endGameText, new Vector3(0, +_endGameTextSize.Height*0.5f, 0), QFontAlignment.Centre, Color.White);
            _fontDrawing.RefreshBuffers();
        }

        public override void Update(double time, bool focused = false)
        {
            if (InputSystem.NewKeys.Contains(Key.Enter) || InputSystem.NewKeys.Contains(Key.Escape))
                SceneManager.RemoveScene(this);
        }

        public override void Draw(double time)
        {
            _fontDrawing.Draw();
        }

        public override void Dispose()
        {
            _font.Dispose();
            _fontDrawing.Dispose();
        }
    }
}
