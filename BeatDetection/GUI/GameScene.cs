using System;
using System.Diagnostics;
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

        private double _elapsedTime = 0;

        public GameScene(Stage stage)
        {
            _stage = stage;
            _stage.StageGeometry.UpdateColours();
        }

        public override void Load()
        {
            SceneManager.ScreenCamera.Scale = SceneManager.ScreenCamera.TargetScale = new Vector2(1.5f);
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

            //Debug.WriteLine("Collection count generation\n0\t1\t2\n{0}\t{1}\t{2}\n", GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
        }

        public override void Draw(double time)
        {
            _elapsedTime += time;
            var rot = Matrix4.CreateRotationX((float)((MathHelper.PiOver4 / 2.5)*Math.Sin(_elapsedTime*0.05)));
            ShaderProgram.Bind();
            ShaderProgram.SetUniform("mvp", Matrix4.Mult(rot, SceneManager.ScreenCamera.ModelViewProjection));
            _stage.Draw(time);
            //Cleanup the program
            ShaderProgram.UnBind();

            float yOffset = -SceneManager.Height * 0.5f + 30f;
            float xOffset = -SceneManager.Width * 0.5f + 20;
            xOffset += SceneManager.Font.Print(_stage.Overlap.ToString(), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
            xOffset += SceneManager.Font.Print(_stage.Hits.ToString(), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.Red).Width + 20;
            xOffset += SceneManager.Font.Print(string.Format("{0}/{1}", _stage.CurrentPolygon, _stage.PolygonCount), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
            //xOffset +=
            //    SceneManager.Font.Print(string.Format("{0}/{1}", SceneManager.ScreenCamera.TargetScale, SceneManager.ScreenCamera.Scale), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left,
            //        Color.White).Width + 20;
            xOffset += SceneManager.Font.Print(string.Format("Current score is {0}", _stage.StageGeometry.Player.Score), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
            xOffset += SceneManager.Font.Print(string.Format("Scale is {0}", SceneManager.ScreenCamera.Scale), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
            xOffset += SceneManager.Font.Print(string.Format("Pulse Multiplier is {0}", _stage.StageGeometry.CenterPolygon.PulseMultiplier), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;

            //if (_stage.Ended) SceneManager.Font.Print("Song Finished", Vector3.Zero, QFontAlignment.Centre, Color.White);
        }

        public override void UnLoad()
        {
            _stage = null;
        }
    }
}
