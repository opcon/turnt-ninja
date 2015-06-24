using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Substructio.GUI;
using Gwen;
using Gwen.Control;
using Gwen.Control.Layout;
using Gwen.Input;
using Substructio.Core;
using Key = OpenTK.Input.Key;

namespace BeatDetection.GUI
{
    class OptionsScene : Scene
    {
        private GUIComponentContainer _GUIComponents;
        private Canvas _canvas;
        private OpenTKAlternative _input;
        private HorizontalSlider _correctionSlider;
        private HorizontalSlider _volumeSlider;
        private NumericUpDown _numericCorrection;
        private NumericUpDown _numericVolume;
        private Label _correctionLabel;
        private Label _volumeLabel;
        private Base _correctionOptionsContainer;
        private Base _volumeOptionsContainer;

        private float _audioCorrection;
        private float _maxAudioVolume;

        public OptionsScene(GUIComponentContainer guiComponents)
        {
            _GUIComponents = guiComponents;
            Exclusive = true;
        }

        public override void Load()
        {
            _audioCorrection = (float) SceneManager.GameSettings["AudioCorrection"]*1000f;
            var vol = (float) SceneManager.GameSettings["MaxAudioVolume"];
            _maxAudioVolume = vol*100f;

            _GUIComponents.Resize(SceneManager.ScreenCamera.ScreenProjectionMatrix, WindowWidth, WindowHeight);
            _canvas = new Canvas(_GUIComponents.Skin);
            _canvas.SetSize(WindowWidth, WindowHeight);
            _input = new OpenTKAlternative(_canvas);
            InputSystem.AddGUIInput(_input);

            _correctionOptionsContainer = new Base(_canvas);

            _correctionLabel = new Label(_correctionOptionsContainer)
            {
                Text = "Audio Correction in Milliseconds",
                AutoSizeToContents = true,
                TextColor = Color.White
            };

            var range = 2000;
            var notchSpacing = 200;
            _correctionSlider = new HorizontalSlider(_correctionOptionsContainer);
            _correctionSlider.SetRange(-range, range);
            _correctionSlider.SetSize(200, 20);
            _correctionSlider.SetNotchSpacing(notchSpacing);
            _correctionSlider.SnapToNotches = false;

            _numericCorrection = new NumericUpDown(_correctionOptionsContainer)
            {
                Min = -2000,
                Max = 2000,
                IsTabable = false

            };
            _numericCorrection.SetSize(130, _correctionLabel.Height + 10);
            _numericCorrection.Margin = Margin.Two;

            _correctionSlider.ValueChanged += (sender, arguments) =>
            {
                if (_correctionSlider.Value != (int) Math.Round(_correctionSlider.Value))
                    _correctionSlider.Value = (int) Math.Round(_correctionSlider.Value);
                if (_numericCorrection.Value != _correctionSlider.Value)
                    _numericCorrection.Value = _correctionSlider.Value; 
                Debug.Print("Correction slider changed, value is " + _correctionSlider.Value);
            };

            _numericCorrection.ValueChanged += (sender, arguments) =>
            {
                if (_numericCorrection.Value != (int) Math.Round(_numericCorrection.Value))
                    _numericCorrection.Value = (int) Math.Round(_numericCorrection.Value);
                if (_correctionSlider.Value != _numericCorrection.Value)
                    _correctionSlider.Value = _numericCorrection.Value;
                Debug.Print("Numeric Correction changed, value is " + _numericCorrection.Value);
            };

            _volumeOptionsContainer = new Base(_canvas);

            _volumeLabel = new Label(_volumeOptionsContainer)
            {
                Text = "Audio Volume",
                AutoSizeToContents = true,
                TextColor = Color.White
            };

            _volumeSlider = new HorizontalSlider(_volumeOptionsContainer);
            _volumeSlider.SetSize(200, 20);
            _volumeSlider.SetRange(0,100);
            _volumeSlider.SetNotchSpacing(10);
            _volumeSlider.SnapToNotches = false;

            _numericVolume = new NumericUpDown(_volumeOptionsContainer)
            {
                Min = 0,
                Max = 100,
                IsTabable = false
            };
            _numericVolume.SetSize(100, _correctionLabel.Height + 10);
            _numericVolume.Margin = Margin.Two;

            _volumeSlider.ValueChanged += (sender, arguments) =>
            {
                if (_volumeSlider.Value != (int)Math.Round(_volumeSlider.Value))
                    _volumeSlider.Value = (int)Math.Round(_volumeSlider.Value);
                if (_numericVolume.Value != _volumeSlider.Value)
                    _numericVolume.Value = _volumeSlider.Value;
                Debug.Print("Volume slider changed, value is " + _volumeSlider.Value);
            };

            _numericVolume.ValueChanged += (sender, arguments) =>
            {
                if (_numericVolume.Value != (int)Math.Round(_numericVolume.Value))
                    _numericVolume.Value = (int)Math.Round(_numericVolume.Value);
                if (_volumeSlider.Value != _numericVolume.Value)
                    _volumeSlider.Value = _numericVolume.Value;
                Debug.Print("Numeric volume changed, value is " + _numericVolume.Value);
            };

            _correctionSlider.Value = _numericCorrection.Value = _audioCorrection;
            _volumeSlider.Value = _numericVolume.Value = _maxAudioVolume;

            LayoutGUI();

            Loaded = true;
        }

        private void LayoutGUI()
        {
            //layout correction slider
            _correctionSlider.SetPosition(-(_correctionSlider.Width + _numericCorrection.Width + 10) / 2 + _correctionLabel.TextWidth / 2, _correctionLabel.Height * 2);

            //layout numeric correction
            Align.PlaceRightBottom(_numericCorrection, _correctionSlider, 10);

            //layout volume slider
            _volumeSlider.SetPosition(0, _volumeLabel.Height * 2);

            //layout volume label
            _volumeLabel.SetPosition((_volumeSlider.Width + _numericVolume.Width + 10)/2 - _volumeLabel.Width/2, 0);

            //layout numeric volume
            Align.PlaceRightBottom(_numericVolume, _volumeSlider, 10);

            //size containers to their children
            _correctionOptionsContainer.SizeToChildren();
            _volumeOptionsContainer.SizeToChildren();

            //find max width
            var maxWidth = Math.Max(_correctionOptionsContainer.Width, _volumeOptionsContainer.Width);

            //layout correction options container
            _correctionOptionsContainer.SetPosition((int)(WindowWidth / 2 - maxWidth + (maxWidth - _correctionOptionsContainer.Width)/2), WindowHeight / 2 - _correctionOptionsContainer.Height / 2);

            //layout volume options container
            _volumeOptionsContainer.SetPosition((int) (WindowWidth / 2 + (maxWidth - _volumeOptionsContainer.Width)/2), WindowHeight / 2 - _volumeOptionsContainer.Height / 2);

        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void Resize(EventArgs e)
        {
            _GUIComponents.Resize(SceneManager.ScreenCamera.ScreenProjectionMatrix, WindowWidth, WindowHeight);
            _canvas.SetSize(WindowWidth, WindowHeight);
            LayoutGUI();
        }

        public override void Update(double time, bool focused = false)
        {
            if (InputSystem.NewKeys.Contains(Key.Escape))
            {
                _audioCorrection = _correctionSlider.Value*0.001f;
                _maxAudioVolume = _volumeSlider.Value*0.01f;
                SceneManager.GameSettings["AudioCorrection"] = _audioCorrection;
                SceneManager.GameSettings["MaxAudioVolume"] = _maxAudioVolume;
                SceneManager.RemoveScene(this);
            }
        }

        public override void Draw(double time)
        {
            _canvas.RenderCanvas();
        }

        public override void Dispose()
        {
            InputSystem.RemoveGUIInput(_input);
            _canvas.Dispose();
        }
    }
}
