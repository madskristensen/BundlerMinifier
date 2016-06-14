using System;

namespace BundlerMinifier
{
    public class ColoredTextRegion : IDisposable
    {
        private readonly string _after;
        private bool _isDisposed;

        private ColoredTextRegion(Func<string, ColoredText> colorization)
        {
            string[] parts = colorization("|").ToString().Split('|');
            Console.Write(parts[0]);
            _after = parts[1];
        }

        public static IDisposable Create(Func<string, ColoredText> colorization)
        {
            return new ColoredTextRegion(colorization);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Console.Write(_after);
        }
    }

    public class ColoredText
    {
        private int _color;
        private string _message;
        private bool _bright;

        public ColoredText(string message)
        {
            _message = message;
        }

        public ColoredText Bright()
        {
            _bright = true;
            return this;
        }

        public ColoredText Red()
        {
            _color = 31;
            return this;
        }

        public ColoredText Black()
        {
            _color = 30;
            return this;
        }

        public ColoredText Green()
        {
            _color = 32;
            return this;
        }

        public ColoredText Orange()
        {
            _color = 33;
            return this;
        }

        public ColoredText Blue()
        {
            _color = 34;
            return this;
        }

        public ColoredText Purple()
        {
            _color = 35;
            return this;
        }

        public ColoredText Cyan()
        {
            _color = 36;
            return this;
        }

        public ColoredText LightGray()
        {
            _color = 37;
            return this;
        }

        public static implicit operator string(ColoredText t)
        {
            return t.ToString();
        }

        public override string ToString()
        {
            if(_color == 0)
            {
                return _message;
            }

            string colorString = _color.ToString();
            if (_bright)
            {
                colorString += "m\x1B[1";
            }

            return $"\x1B[{colorString}m{_message}\x1B[0m\x1B[39m\x1B[49m";
        }
    }

    public static class StringExtensions
    {
        public static ColoredText Orange(this string s)
        {
            return new ColoredText(s).Orange();
        }

        public static ColoredText Black(this string s)
        {
            return new ColoredText(s).Black();
        }

        public static ColoredText Red(this string s)
        {
            return new ColoredText(s).Red();
        }

        public static ColoredText Green(this string s)
        {
            return new ColoredText(s).Green();
        }

        public static ColoredText Blue(this string s)
        {
            return new ColoredText(s).Blue();
        }

        public static ColoredText Purple(this string s)
        {
            return new ColoredText(s).Purple();
        }

        public static ColoredText Cyan(this string s)
        {
            return new ColoredText(s).Cyan();
        }

        public static ColoredText LightGray(this string s)
        {
            return new ColoredText(s).LightGray();
        }
    }
}
