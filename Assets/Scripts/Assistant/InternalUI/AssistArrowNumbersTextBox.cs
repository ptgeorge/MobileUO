﻿using System;

using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistArrowNumbersTextBox : Control
    {
        private const int TIME_BETWEEN_CLICKS = 250;
        private readonly int _Min, _Max;
        private readonly StbTextBox _textBox;
        private readonly Button _up, _down;
        private float _timeUntilNextClick;

        public AssistArrowNumbersTextBox(int x, int y, int width, int raiseamount, int minvalue, int maxvalue, byte font = 0, int maxcharlength = -1, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0)
        {
            int height = 20;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _Min = minvalue;
            _Max = maxvalue;

            Add(new ResizePic(0x0BB8)
            {
                Width = width,
                Height = height + 4
            });

            _up = new Button(raiseamount, 0x983, 0x984)
            {
                X = width - 12,
                ButtonAction = ButtonAction.Activate
            };

            _up.MouseDown += (sender, e) =>
            {
                if (_up.IsClicked)
                {
                    UpdateValue();
                    _timeUntilNextClick = TIME_BETWEEN_CLICKS * 2;
                }
            };
            Add(_up);

            _down = new Button(-raiseamount, 0x985, 0x986)
            {
                X = width - 12,
                Y = height - 7,
                ButtonAction = ButtonAction.Activate
            };

            _down.MouseDown += (sender, e) =>
            {
                if (_down.IsClicked)
                {
                    UpdateValue();
                    _timeUntilNextClick = TIME_BETWEEN_CLICKS * 2;
                }
            };
            Add(_down);
            Add(_textBox = new StbTextBox(font, maxcharlength, width, isunicode, style, hue)
            {
                X = 2,
                Y = 2,
                Height = height,
                Width = width - 17,
                NumbersOnly = true
            });
            _textBox.FocusLost += TextBox_FocusLost;
        }

        private void TextBox_FocusLost(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            int.TryParse(_textBox.Text, out int i);
            ValidateValue(i);
        }

        internal string Text
        {
            get
            {
                return _textBox?.Text ?? string.Empty;
            }
            set
            {
                _textBox?.SetText(value);
            }
        }

        private void UpdateValue()
        {
            int.TryParse(_textBox.Text, out int i);

            if (_up.IsClicked)
                i += _up.ButtonID;
            else
                i += _down.ButtonID;
            ValidateValue(i);
        }

        public event EventHandler<int> ValueChanged;
        private void ValidateValue(int val)
        {
            Tag = val = Math.Max(_Min, Math.Min(_Max, val));
            if(_textBox.Text != val.ToString())//value has effectively changed, notify it
                ValueChanged.Raise(val);
            _textBox.SetText(val.ToString());
        }

        public override void Update()
        {
            if (IsDisposed)
                return;

            if (_up.IsClicked || _down.IsClicked)
            {
                if (_timeUntilNextClick <= 0f)
                {
                    _timeUntilNextClick += TIME_BETWEEN_CLICKS;
                    UpdateValue();
                }

                _timeUntilNextClick -= (float)frameMS;
            }

            base.Update();
        }
    }
}