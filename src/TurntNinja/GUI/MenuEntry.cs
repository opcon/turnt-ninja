using OpenTK;
using QuickFont;
using Substructio.Core.Math;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetection.GUI
{
    class MenuEntry
    {
        public string Text { get; set; }
        public MainMenuOptions Option { get; set; }
        public Matrix4 ModelView { get; set; }
        public SizeF Size { get; set; }
        QFont _font;

        public QFont Font
        {
            get
            {
                return _font;
            }

            set
            {
                _font = value;
                Size = Font.Measure(Text);
            }
        }

        public MenuState NewState { get; set; }
        MenuState _internalState { get; set; }

        public float Scale { get; private set; }
        public float TransitionPercentage { get; private set; }
        public float TransitionDuration { get; set; }

        double _elapsedTransitionTime { get; set; }
        double _elapsedTime { get; set; }

        double _angleBetweenSides { get; }

        Player _player { get; }

        public MenuEntry(string text, MainMenuOptions option, double angleBetweenSides, Player player, QFont font)
        {
            _font = null;
            Text = text;
            Option = option;
            ModelView = Matrix4.Identity;
            Size = SizeF.Empty;
            NewState = _internalState = MenuState.Unselected;
            Scale = 1;
            TransitionDuration = 1;
            TransitionPercentage = 0;
            _elapsedTime = _elapsedTransitionTime = 0;
            _angleBetweenSides = angleBetweenSides;
            _player = player;
            Font = font;
        }

        public void Update(double time)
        {
            float easeScale = 0f;
            float pulseScale = 0f;
            float baseScale = 0.5f;
            // Tween in or out as required
            if (NewState != _internalState)
            {
                _elapsedTransitionTime += time;
                switch (NewState)
                {
                    case MenuState.Selected:
                        easeScale = (float)Math.Pow(Math.Sin(_elapsedTransitionTime * Math.PI / 2 * TransitionDuration), 2) * 0.40f;
                        break;
                    case MenuState.Unselected:
                        easeScale = 0.4f - (float)Math.Pow(Math.Sin(_elapsedTransitionTime * Math.PI / 2 * TransitionDuration), 2) * 0.40f;
                        break;
                }
                if (_elapsedTransitionTime >= TransitionDuration)
                {
                    _internalState = NewState;
                    _elapsedTransitionTime = 0;
                    _elapsedTime = 0;
                }
            }
            else
            {
                _elapsedTime += time;
                switch (_internalState)
                {
                    case MenuState.Selected:
                        pulseScale = 0.4f + (float)Math.Pow(Math.Sin(_elapsedTime * Math.PI / 2), 2) * 0.10f;
                        break;
                    case MenuState.Unselected:
                        pulseScale = 0.0f;
                        break;
                }
            }

            Scale = baseScale + easeScale + pulseScale;

            var selectedSide = (int)Option;
            var newPos = new PolarVector(selectedSide * _angleBetweenSides + _angleBetweenSides * 0.5f, _player.Position.Radius + _player.Width + Size.Height * 0.9);

            var extraRotation = (selectedSide >= 0 && selectedSide < 3) ? (-Math.PI / 2.0) : (Math.PI / 2.0);
            var extraOffset = (selectedSide >= 0 && selectedSide < 3) ? (0) : (-Size.Height / 4);

            newPos.Radius += extraOffset;
            var cart = newPos.ToCartesianCoordinates();
            ModelView = Matrix4.CreateTranslation(0, Size.Height / 2, 0)
                        * Matrix4.CreateScale(Scale)
                        * Matrix4.CreateRotationZ((float)(newPos.Azimuth + extraRotation))
                        * Matrix4.CreateTranslation(cart.X, cart.Y, 0);

        }

        public QFontDrawingPrimitive Print(QFontRenderOptions rOptions)
        {
            var ret = new QFontDrawingPrimitive(Font, rOptions);
            ret.Print(Text, Vector3.Zero, QFontAlignment.Centre);
            ret.ModelViewMatrix = ModelView;
            return ret;
        }
    }

    enum MenuState
    {
        Selected,
        Unselected
    }
}
