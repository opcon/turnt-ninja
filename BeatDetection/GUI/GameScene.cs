using System;
using BeatDetection.Game;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using QuickFont;
using Substructio.GUI;

namespace BeatDetection.GUI
{
    class GameScene : Scene
    {
        private Stage _stage;
        private QFont _multiplierFont;
        private ProcessedText _multiplerText;
        public GameScene(Stage stage)
        {
            _stage = stage;
            _stage.UpdateColours();
        }

        public override void Load()
        {
            _multiplierFont = new QFont(SceneManager.FontPath, 30);
            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }
            
        public override void Resize(EventArgs e)
        {
        }

        public override void Update(double time, bool focused = false)
        {
            _stage.Update(time);
        }

        public override void Draw(double time)
        {
            SceneManager.ScreenCamera.EnableWorldDrawing();
            GL.Disable(EnableCap.Texture2D);
            _stage.Draw(time);
            SceneManager.ScreenCamera.EnableScreenDrawing();
            SceneManager.DrawTextLine(_stage.Overlap.ToString(), new Vector2(50, 50));
            SceneManager.DrawTextLine(_stage.Hits.ToString(), new Vector2(100, 50));
            SceneManager.DrawTextLine(String.Format("{0}/{1}", _stage.CurrentPolygon, _stage.PolygonCount), new Vector2(150, 50));

            _multiplerText = _multiplierFont.ProcessText(String.Format("{0}x", _stage.Multiplier == -1 ? 0 : _stage.Multiplier), 100,
                QFontAlignment.Centre);
            SceneManager.DrawProcessedText(_multiplerText,
                new Vector2(SceneManager.GameWindow.Width/2,
                    SceneManager.GameWindow.Height/2 + _multiplierFont.Measure(_multiplerText).Height / 2),
                _multiplierFont);
        }

        public override void UnLoad()
        {
            _stage = null;
        }
    }
}
