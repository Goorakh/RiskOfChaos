using System;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendColoredString(this StringBuilder sb, string text, Color32 color)
        {
            if (sb is null)
                throw new ArgumentNullException(nameof(sb));

            const int COLOR_TAG_TOTAL_LENGTH
                = 8  // <color=#
                + 6  // color hex code
                + 1  // >
                + 8; // </color>

            sb.EnsureCapacity(sb.Length + text.Length + COLOR_TAG_TOTAL_LENGTH);
            return sb.Append("<color=#")
                     .AppendColor32RGBHexValues(color)
                     .Append('>')
                     .Append(text)
                     .Append("</color>");
        }
    }
}
