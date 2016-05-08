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
using BeatDetection.Game;

namespace BeatDetection.GUI
{
    class EndGameScene : Scene
    {
        private QFont _font;
        private QFontDrawing _fontDrawing;
        private SizeF _endGameTextSize;
        private string _endGameText = "Press Enter to Continue";
        private Stage _stage;

        public EndGameScene(Stage stage)
        {
            _stage = stage;
            Exclusive = true;
        }

        public override void Load()
        {
            _font = new QFont(SceneManager.FontPath, 50, new QFontBuilderConfiguration(true), FontStyle.Regular);
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

            float fontOffset = 0;
            fontOffset += _fontDrawing.Print(_font, string.Format("Score: {0}", _stage.StageGeometry.Player.Score), new Vector3(0, (1.0f * (WindowHeight / 2.0f) / 3.0f) + _endGameTextSize.Height/2.0f, 0), QFontAlignment.Centre, Color.White).Height;
            fontOffset += _fontDrawing.Print(_font, string.Format("Accuracy: {0}", 100 - (int)(((float)_stage.Hits / _stage.StageGeometry.BeatCount) * 100.0f)), new Vector3(0, (1.0f * (WindowHeight / 2.0f) / 3.0f) - _endGameTextSize.Height/2.0f, 0), QFontAlignment.Centre, Color.White).Height;
            _fontDrawing.Print(_font, _endGameText, new Vector3(0, (-WindowHeight/2) + _endGameTextSize.Height + 10, 0), QFontAlignment.Centre, Color.White);
            _fontDrawing.RefreshBuffers();
        }

        public override void Update(double time, bool focused = false)
        {
            if (InputSystem.NewKeys.Contains(Key.Enter) || InputSystem.NewKeys.Contains(Key.Escape) || InputSystem.NewKeys.Contains(Key.Space))
            {
                _stage.StageGeometry.Player.Reset();
                SceneManager.RemoveScene(this);
            }
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
