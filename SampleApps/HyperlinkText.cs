﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace SampleApplication
{
    public static class HyperlinkText
    {
        private static readonly Regex regex = new Regex(@"\[([^\]]+)\]\(([^\)]+)\)");

        /// <summary>
        /// Converts text containing hyperlinks in markdown syntax [text](url)
        /// to a collection of Run and Hyperlink inlines.
        /// </summary>
        public static IEnumerable<Inline> TextToInlines(this string text)
        {
            var inlines = new List<Inline>();

            while (!string.IsNullOrEmpty(text))
            {
                var match = regex.Match(text);

                if (match.Success &&
                    match.Groups.Count == 3 &&
                    Uri.TryCreate(match.Groups[2].Value, UriKind.Absolute, out Uri uri))
                {
                    inlines.Add(new Run { Text = text.Substring(0, match.Index) });
                    text = text.Substring(match.Index + match.Length);

                    var link = new Hyperlink { NavigateUri = uri };
                    link.Inlines.Add(new Run { Text = match.Groups[1].Value });
                    inlines.Add(link);
                }
                else
                {
                    inlines.Add(new Run { Text = text });
                    text = null;
                }
            }

            return inlines;
        }

        public static readonly DependencyProperty InlinesSourceProperty = DependencyProperty.RegisterAttached(
            "InlinesSource", typeof(string), typeof(HyperlinkText), new PropertyMetadata(null, InlinesSourcePropertyChanged));

        public static string GetInlinesSource(DependencyObject element)
        {
            return (string)element.GetValue(InlinesSourceProperty);
        }

        public static void SetInlinesSource(DependencyObject element, string value)
        {
            element.SetValue(InlinesSourceProperty, value);
        }

        private static void InlinesSourcePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            InlineCollection inlines = null;

            if (obj is TextBlock block)
            {
                inlines = block.Inlines;
            }
            else if (obj is Paragraph paragraph)
            {
                inlines = paragraph.Inlines;
            }

            if (inlines != null)
            {
                inlines.Clear();

                foreach (var inline in TextToInlines((string)e.NewValue))
                {
                    inlines.Add(inline);
                }
            }
        }
    }
}
