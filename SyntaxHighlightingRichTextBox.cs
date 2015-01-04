using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RichText
{
    public class SyntaxHighlightingRichTextBox : RichTextBox
    {
        private readonly Regex _functionsMatch = new Regex(@"\b(\w+?)(?=\()", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly SolidColorBrush _functionsBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#01A0E4"));

        private readonly Regex _stringsMatch = new Regex(@"""(.+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly SolidColorBrush _stringsBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#01A252"));

        private readonly Regex _numbersMatch = new Regex(@"\b([0-9]+)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly SolidColorBrush _numbersBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ED0C8C"));

        private readonly Regex _fieldMatch = new Regex(@"\[\w+\s?\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly SolidColorBrush _fieldBrush = Brushes.MediumSlateBlue;

        private readonly VisualBrush _validationBrush = new VisualBrush()
            {
                Viewbox = new Rect(0,0,3,2),
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0,0.8,6,4),
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile,
                Visual = new Path()
                    {
                        Data = Geometry.Parse("M 0,1 C 1,0 2,2 3,1"),
                        Stroke = Brushes.Red,
                        StrokeThickness = 0.2,
                        StrokeEndLineCap = PenLineCap.Square,
                        StrokeStartLineCap = PenLineCap.Square
                    }
            };

        private TextPointerTranslator _textPointerTranslator;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _functionsBrush.Freeze();
            _stringsBrush.Freeze();
            _numbersBrush.Freeze();

            _textPointerTranslator = new TextPointerTranslator(this.Document);
        }

        private bool _ignoreTextChanged = false;

        //private const string OpenBracket = "(";
        //private const string CloseBracket = ")";

        //protected override void OnSelectionChanged(RoutedEventArgs e)
        //{
        //    var textPointer = this.Selection.Start;
        //    var afterText = GetNextTextPointer(textPointer, LogicalDirection.Forward).GetTextInRun(LogicalDirection.Forward);
        //    var beforeText = GetNextTextPointer(textPointer, LogicalDirection.Backward).GetTextInRun(LogicalDirection.Backward);

        //    Debug.WriteLine("BEFORE: " + beforeText);
        //    Debug.WriteLine("AFTER: " + afterText);

        //    if (beforeText == CloseBracket || beforeText == OpenBracket)
        //    {
        //        HighlightMatchingBracket(textPointer, Position.Before);
        //    }
        //    else if (afterText == CloseBracket || afterText == OpenBracket)
        //    {
        //        HighlightMatchingBracket(textPointer, Position.After);
        //    }
        //}

        //private enum Position
        //{
        //    Before,
        //    After
        //}

        //private TextPointer GetNextTextPointer(TextPointer textPointer, LogicalDirection direction)
        //{
        //    while (true)
        //    {
        //        var textPointerContext = textPointer.GetPointerContext(direction);
        //        if (textPointerContext == TextPointerContext.Text || textPointerContext == TextPointerContext.None)
        //        {
        //            break;
        //        }
        //        textPointer = textPointer.GetNextContextPosition(direction);
        //    }

        //    return textPointer;
        //}

        //private void HighlightMatchingBracket(TextPointer textPointer, Position position)
        //{
        //    var textRange = new TextRange(this.Document.ContentStart, this.Document.ContentEnd);
        //    _ignoreTextChanged = true;
        //    textRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
        //    _ignoreTextChanged = false;
        //    var openBrackets = Regex.Matches(textRange.Text, @"\(").OfType<Match>().Select(m => m.Index + 1);
        //    var closedBrackets = Regex.Matches(textRange.Text, @"\)").OfType<Match>().Select(m => m.Index + 1);
        //    var characterOffset = _textPointerTranslator.GetCharacterOffset(textPointer);
        //    Debug.WriteLine("Highlight matching bracket at " + (position == Position.Before ? characterOffset : characterOffset + 1));
        //    TextRange r;
        //    if (position == Position.Before)
        //    {
        //        r = new TextRange(textPointer, textPointer.GetPositionAtOffset(-1));
        //    }
        //    else
        //    {
        //        r = new TextRange(textPointer, textPointer.GetPositionAtOffset(1));
        //    }
        //    _ignoreTextChanged = true;
        //    r.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);
        //    _ignoreTextChanged = false;
        //}

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (this.Document == null || this._ignoreTextChanged)
                return;

            var textRange = new TextRange(this.Document.ContentStart, this.Document.ContentEnd);

            try
            {
                _ignoreTextChanged = true;
                textRange.ClearAllProperties();
            }
            finally 
            {
                _ignoreTextChanged = false;
            }

            var text = textRange.Text;

            if (text.Length > 60)
            {
                MarkInvalid(13, 8);
            }

            HighlightForeground(text, _functionsMatch, _functionsBrush);
            HighlightForeground(text, _numbersMatch, _numbersBrush);
            HighlightForeground(text, _stringsMatch, _stringsBrush);
            HighlightForeground(text, _fieldMatch, _fieldBrush);
        }

        private void MarkInvalid(int from, int length)
        {
            var start = _textPointerTranslator.GetTextPointer(from);
            var end = _textPointerTranslator.GetTextPointer(from + length);
            var textRange = new TextRange(start, end);
            var textDecorationCollection = new TextDecorationCollection();
            textDecorationCollection.Add(new TextDecoration(TextDecorationLocation.Underline, new Pen(_validationBrush, 6), 2.5, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));
            try
            {
                _ignoreTextChanged = true;
                textRange.ApplyPropertyValue(Inline.TextDecorationsProperty, textDecorationCollection);
            }
            finally
            {
                _ignoreTextChanged = false;
            }
        }

        private void HighlightForeground(string text, Regex regexToMatch, SolidColorBrush foregroundBrush)
        {
            var matches = regexToMatch.Matches(text);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    var start = _textPointerTranslator.GetTextPointer(match.Index);
                    var end = _textPointerTranslator.GetTextPointer(match.Index + match.Length);
                    var textRange = new TextRange(start, end);

                    try
                    {
                        _ignoreTextChanged = true;
                        textRange.ApplyPropertyValue(TextElement.ForegroundProperty, foregroundBrush);
                    }
                    finally 
                    {
                        _ignoreTextChanged = false;
                    }
                }
            }
        }
    }
}
