using System;
using System.Drawing;
using BeatDetection.Core;
using BeatDetection.Game;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using QuickFont;
using Substructio.Graphics.OpenGL;
using Substructio.GUI;

namespace BeatDetection.GUI
{
    class GameScene : Scene
    {
        private Stage _stage;
        private ProcessedText _multiplerText;
        public ShaderProgram ShaderProgram { get; set; }

        public GameScene(Stage stage)
        {
            _stage = stage;
            _stage.UpdateColours();
        }

        public override void Load()
        {


            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }
            
        public override void Resize(EventArgs e)
        {
            _stage.MultiplierFont.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
        }

        public override void Update(double time, bool focused = false)
        {
            _stage.Update(time);
        }

        public override void Draw(double time)
        {
            ShaderProgram.Bind();
            ShaderProgram.SetUniform("mvp", SceneManager.ScreenCamera.ModelViewProjection);
            _stage.Draw(time);
            //Cleanup the program
            ShaderProgram.UnBind();

            float yOffset = -SceneManager.Height * 0.5f + 30f;
            float xOffset = -SceneManager.Width * 0.5f + 20;
            xOffset += SceneManager.Font.Print(_stage.Overlap.ToString(), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
            xOffset += SceneManager.Font.Print(_stage.Hits.ToString(), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.Red).Width + 20;
            xOffset += SceneManager.Font.Print(string.Format("{0}/{1}", _stage.CurrentPolygon, _stage.PolygonCount), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
        }

        public override void UnLoad()
        {
            _stage = null;
        }
    }
}
