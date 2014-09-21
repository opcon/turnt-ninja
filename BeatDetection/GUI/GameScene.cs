using System;
using BeatDetection.Game;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Substructio.GUI;

namespace BeatDetection.GUI
{
    class GameScene : Scene
    {
        private Stage _stage;
        public GameScene(Stage stage)
        {
            _stage = stage;
        }

        public override void Load()
        {
            //SceneManager.ScreenCamera.TargetScale = new Vector2(10,10);
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
            SceneManager.DrawTextLine(_stage.FinishedEaseIn.ToString(), new Vector2(250, 50));
        }

        public override void UnLoad()
        {
            _stage = null;
        }
    }
}
