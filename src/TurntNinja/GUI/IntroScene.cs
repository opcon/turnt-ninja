using Substructio.Graphics.OpenGL;
using Substructio.GUI;
using Substructio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Substructio.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK;
using System.Diagnostics;

namespace TurntNinja.GUI
{
    class IntroScene : Scene
    {
        ShaderProgram _shaderProgram;
        Sprite _logo;

        float alpha = 0.0f;
        double _totalTime = 0.0f;
        bool _loadFirstRunScene = false;

        const double ADVANCE_TIME = 4.5;
        public IntroScene(bool loadFirstRunScene)
        {
            Exclusive = true;
            _loadFirstRunScene = loadFirstRunScene;
        }
        public override void CallBack(GUICallbackEventArgs e)
        {
        }

        public override void Dispose()
        {
            _logo.Dispose();
            _shaderProgram.Dispose();
        }

        public override void Draw(double time)
        {
            GL.Enable(EnableCap.Texture2D);
            _shaderProgram.Bind();
            _shaderProgram.SetUniform("mvp", SceneManager.ScreenCamera.ScreenModelViewProjection);
            _shaderProgram.SetUniform("alpha", alpha);

            if (_totalTime > 0.1)
                _logo.Draw(time);
            //Cleanup the program
            _shaderProgram.UnBind();
        }

        public override void EnterFocus()
        {
        }

        public override void ExitFocus()
        {
        }

        public override void Load()
        {
            // Choose correct version directive because OSX is dumb
            string version = "#version 130";
            if (PlatformDetection.RunningPlatform() == Platform.MacOSX)
                version = "#version 150";

            // Load shaders
            var vert = new Shader(Path.Combine(SceneManager.Directories["Shaders"].FullName, "simple-texture.vs"), version);
            var frag = new Shader(Path.Combine(SceneManager.Directories["Shaders"].FullName, "simple-texture.fs"), version);
            _shaderProgram = new ShaderProgram();
            _shaderProgram.Load(vert, frag);

            _logo = new Sprite(_shaderProgram);
            _logo.Initialise(SceneManager.Directories.Locate("Images", "banner-isolated.png"));
            _logo.Scale = 0.75f;

            SetLogoPositions();

            GL.ClearColor(System.Drawing.Color.White);

            Loaded = true;
        }

        public override void Resize(EventArgs e)
        {
            SetLogoPositions();
        }

        public override void Update(double time, bool focused = false)
        {
            var e = GL.GetError();
            _logo.Update(time);
            alpha = (float)MathHelper.Clamp(alpha + 0.0015 + time * alpha, 0f, 1f);
            //alpha = (float)MathHelper.Clamp(Math.Sin((_totalTime - 0.1)*0.75f), 0, 1);
            _totalTime += time;
            if (InputSystem.CurrentKeys.Count > 0 || _totalTime > ADVANCE_TIME)
            {
                AdvanceToMenu();
            }
        }

        private void AdvanceToMenu()
        {
            GL.ClearColor(OpenTK.Graphics.Color4.Black);
            SceneManager.RemoveScene(this, true);
            if (_loadFirstRunScene)
                SceneManager.AddScene(new FirstRunScene(), null);
        }

        private void SetLogoPositions()
        {
            _logo.Position = new Vector2(-_logo.Size.X * 0.5f + 70f, -_logo.Size.Y * 0.1f);
        }
    }
}
