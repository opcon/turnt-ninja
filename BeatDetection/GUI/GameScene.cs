using System;
using System.Collections.Generic;
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
using Gwen;
using Substructio.Core;
using Key = OpenTK.Input.Key;

namespace BeatDetection.GUI
{
    class GameScene : Scene
    {
        private Stage _stage;
        private Stage _backStage;
        private ProcessedText _multiplerText;
        public ShaderProgram ShaderProgram { get; set; }
        public bool UsingPlaylist { get; set; }
        public List<string> PlaylistFiles { get; set; }
        private Gwen.Renderer.OpenTK _renderer;
        private Gwen.Skin.TexturedBase _skin;
        private Gwen.Control.Canvas _canvas;
        private Gwen.Control.Button _testButton;
        private Gwen.Input.OpenTKAlternative _input;

        private double _elapsedTime = 0;

        public GameScene(Stage stage)
        {
            Exclusive = true;
            _stage = stage;
            _stage.StageGeometry.UpdateColours();
        }

        public override void Load()
        {
            SceneManager.ScreenCamera.Scale = SceneManager.ScreenCamera.TargetScale = new Vector2(1.5f);

            _renderer = new Gwen.Renderer.OpenTK();            
            var renderMatrix = Matrix4.CreateTranslation(-SceneManager.Width/2.0f, -SceneManager.Height/2.0f,0) * Matrix4.CreateScale(1, -1, 1)*SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _renderer.Resize(renderMatrix, this.SceneManager.Width, this.SceneManager.Height);
            _skin = new Gwen.Skin.TexturedBase(_renderer, "DefaultSkin.png");

            _canvas = new Gwen.Control.Canvas(_skin);

            _input = new Gwen.Input.OpenTKAlternative(_canvas);

            _canvas.SetSize(1280, 768);
            _canvas.ShouldDrawBackground = false;

            _testButton = new Gwen.Control.Button(_canvas);
            _testButton.Text = "TEST!!";
            _testButton.SetBounds(200, 30, 300, 200);

            _testButton.IsTabable = true;
            _testButton.AutoSizeToContents = true;

            _testButton.Focus();

            _testButton.Clicked += (sender, arguments) =>
            {
                _stage.Reset();
                SceneManager.ScreenCamera.ExtraScale = 0;
                SceneManager.ScreenCamera.Scale = new Vector2(1, 1);
                SceneManager.RemoveScene(this);
            };

            _skin.DefaultFont = new Gwen.Font(_renderer, "Arial", 10);
            if (UsingPlaylist)
            {
                
            }
            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }
            
        public override void Resize(EventArgs e)
        {
            _stage.MultiplierFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;

            //_renderer.Resize();
            var renderMatrix = Matrix4.CreateTranslation(-SceneManager.Width/2.0f, -SceneManager.Height/2.0f,0) * Matrix4.CreateScale(1, -1, 1)*SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _renderer.Resize(renderMatrix, this.SceneManager.Width, this.SceneManager.Height);
            _canvas.SetSize(this.SceneManager.Width, this.SceneManager.Height);
        }

        public override void Update(double time, bool focused = false)
        {
            _stage.Update(time);
            //if (InputSystem.NewKeys.Contains(Key.Enter))
            //    _testButton.IsTabable;
            foreach (var pressedButton in InputSystem.PressedButtons)
            {
                _input.ProcessMouseButton(pressedButton, true);
            }
            foreach (var releasedButton in InputSystem.ReleasedButtons)
            {
                _input.ProcessMouseButton(releasedButton, false);
            }
            _input.ProcessMouseWheel((int) InputSystem.MouseWheelDelta);
            if (InputSystem.HasMouseMoved)
                _input.ProcessMouseMove((int) InputSystem.MouseXY.X, (int) InputSystem.MouseXY.Y);
            foreach (var newKey in InputSystem.NewKeys)
            {
                _input.ProcessKeyDown(newKey);
            }
            foreach (var releasedKey in InputSystem.ReleasedKeys)
            {
                _input.ProcessKeyUp(releasedKey);
            }
            foreach (var pressedChar in InputSystem.PressedChars)
            {
                _input.KeyPress(pressedChar);
            }
        }

        public override void Draw(double time)
        {
            _elapsedTime += time;
            var rot = Matrix4.CreateRotationX((float)((MathHelper.PiOver4 / 2.0)*Math.Sin(_elapsedTime*0.09)));
            ShaderProgram.Bind();
            ShaderProgram.SetUniform("mvp", Matrix4.Mult(rot, SceneManager.ScreenCamera.ModelViewProjection));
            _stage.Draw(time);
            //Cleanup the program
            ShaderProgram.UnBind();

            float yOffset = -SceneManager.Height * 0.5f + 30f;
            float xOffset = -SceneManager.Width * 0.5f + 20;
            xOffset += SceneManager.DrawTextLine(_stage.Overlap.ToString(), new Vector3(xOffset, yOffset, 0), Color.White, QFontAlignment.Left).Width;;
            xOffset += SceneManager.DrawTextLine(_stage.Hits.ToString(), new Vector3(xOffset, yOffset, 0), Color.Red, QFontAlignment.Left).Width + 20;
            xOffset += SceneManager.DrawTextLine(string.Format("{0}/{1}", _stage.CurrentPolygon, _stage.PolygonCount), new Vector3(xOffset, yOffset, 0), Color.White, QFontAlignment.Left).Width + 20;
            //xOffset +=
            //    SceneManager.Font.Print(string.Format("{0}/{1}", SceneManager.ScreenCamera.TargetScale, SceneManager.ScreenCamera.Scale), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left,
            //        Color.White).Width + 20;
            //xOffset += SceneManager.Font.Print(string.Format("Current score is {0}", _stage.StageGeometry.Player.Score), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
            //xOffset += SceneManager.Font.Print(string.Format("Scale is {0}", SceneManager.ScreenCamera.Scale), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
            //xOffset += SceneManager.Font.Print(string.Format("Pulse Multiplier is {0}", _stage.StageGeometry.CenterPolygon.PulseMultiplier), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
            xOffset += SceneManager.DrawTextLine(string.Format("Mouse coordinates are {0}", InputSystem.MouseXY), new Vector3(xOffset, yOffset, 0), Color.White,  QFontAlignment.Left).Width + 20; 

            //if (_stage.Ended) SceneManager.Font.Print("Song Finished", Vector3.Zero, QFontAlignment.Centre, Color.White);

            _canvas.RenderCanvas();
        }

        public override void Dispose()
        {
            _canvas.Dispose();
            _skin.Dispose();
            _renderer.Dispose();
            _stage.Dispose(); 
            _stage = null;
        }
    }
}
