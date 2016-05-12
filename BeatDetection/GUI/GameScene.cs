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

        private const float TIMETOWAIT = 1.0f;

        private double _elapsedTime = 0;

        public GameScene(Stage stage)
        {
            Exclusive = true;
            _stage = stage;
            _stage.StageGeometry.UpdateColours(0);
        }

        public override void Load()
        {
            SceneManager.ScreenCamera.Scale = SceneManager.ScreenCamera.TargetScale = new Vector2(1.5f);
            if (UsingPlaylist)
            {
                
            }
            Loaded = true;
        }

        private void Exit(bool GoToEndScene = false)
        {
            _stage.Reset();
            SceneManager.ScreenCamera.ExtraScale = 0;
            SceneManager.ScreenCamera.Scale = SceneManager.ScreenCamera.TargetScale = new Vector2(1, 1);
            SceneManager.GameWindow.Cursor = MouseCursor.Default;
            if (GoToEndScene) SceneManager.AddScene(new EndGameScene(_stage), this);
            SceneManager.RemoveScene(this, !GoToEndScene);
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }
            
        public override void Resize(EventArgs e)
        {
            _stage.MultiplierFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _stage.ScoreFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
        }

        public override void Update(double time, bool focused = false)
        {
            if (InputSystem.NewKeys.Contains(Key.Escape))
            {
                Exit();
                return;
            }
            _stage.Update(time);

            if (_stage.Ended && (_stage.TotalTime - _stage.EndTime) > TIMETOWAIT)
            {
                    Exit(true);
            }
        }

        public override void Draw(double time)
        {
            _elapsedTime += time;
            var rot = Matrix4.CreateRotationX((float)((MathHelper.PiOver4 / 1.5)*Math.Sin((_elapsedTime*0.18))));
            ShaderProgram.Bind();
            ShaderProgram.SetUniform("mvp", Matrix4.Mult(rot, SceneManager.ScreenCamera.ModelViewProjection));
            _stage.Draw(time);
            //Cleanup the program
            ShaderProgram.UnBind();

            if (SceneManager.Debug)
            {
                float yOffset = -SceneManager.Height * 0.5f + 30f;
                float xOffset = -SceneManager.Width * 0.5f + 20;
                xOffset += SceneManager.DrawTextLine(_stage.Overlap.ToString(), new Vector3(xOffset, yOffset, 0), Color.White, QFontAlignment.Left).Width + 20;
                xOffset += SceneManager.DrawTextLine(_stage.Hits.ToString(), new Vector3(xOffset, yOffset, 0), Color.Red, QFontAlignment.Left).Width + 20;
                xOffset += SceneManager.DrawTextLine(string.Format("{0}/{1}", _stage.CurrentPolygon, _stage.PolygonCount), new Vector3(xOffset, yOffset, 0), Color.White, QFontAlignment.Left).Width + 20;
                //xOffset +=
                //    SceneManager.Font.Print(string.Format("{0}/{1}", SceneManager.ScreenCamera.TargetScale, SceneManager.ScreenCamera.Scale), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left,
                //        Color.White).Width + 20;
                //xOffset += SceneManager.Font.Print(string.Format("Current score is {0}", _stage.StageGeometry.Player.Score), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
                //xOffset += SceneManager.Font.Print(string.Format("Scale is {0}", SceneManager.ScreenCamera.Scale), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
                //xOffset += SceneManager.Font.Print(string.Format("Pulse Multiplier is {0}", _stage.StageGeometry.CenterPolygon.PulseMultiplier), new Vector3(xOffset, yOffset, 0), QFontAlignment.Left, Color.White).Width + 20;
                xOffset += SceneManager.DrawTextLine(string.Format("Mouse coordinates are {0}", InputSystem.MouseXY), new Vector3(xOffset, yOffset, 0), Color.White, QFontAlignment.Left).Width + 20;
                xOffset += SceneManager.DrawTextLine(string.Format("Song Playing {0}", !_stage._stageAudio.IsStopped), new Vector3(xOffset, yOffset, 0), Color.White, QFontAlignment.Left).Width + 20;
                xOffset += SceneManager.DrawTextLine(string.Format("Beat Frequency {0}", _stage.StageGeometry.CurrentBeatFrequency), new Vector3(xOffset, yOffset, 0), Color.White, QFontAlignment.Left).Width + 20;

                yOffset = SceneManager.Height * 0.5f;
                xOffset = -SceneManager.Width * 0.5f + 20;
                yOffset -= SceneManager.DrawTextLine(_stage.StageGeometry.ColourModifiers.ToString(), new Vector3(xOffset, yOffset, 0), Color.White, QFontAlignment.Left).Height + 20;
            }
        }

        public override void Dispose()
        {
            _stage.Dispose(); 
            _stage = null;
        }
    }
}
