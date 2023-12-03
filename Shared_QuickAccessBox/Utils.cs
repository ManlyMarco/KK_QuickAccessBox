﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KK_QuickAccessBox
{
    internal static class Utils
    {
        private static char[] _invalids;

        /// <summary>
        /// Replaces characters in <c>text</c> that are not allowed in file names with the specified replacement character.
        /// https://stackoverflow.com/questions/620605/how-to-make-a-valid-windows-filename-from-an-arbitrary-string/25223884#25223884
        /// </summary>
        /// <param name="text">Text to make into a valid filename. The same string is returned if it is valid already.</param>
        /// <param name="replacement">Replacement character, or null to simply remove bad characters.</param>
        /// <param name="fancy">Whether to replace quotes and slashes with the non-ASCII characters ” and ⁄.</param>
        /// <returns>A string that can be used as a filename. If the output string would otherwise be empty, returns "_".</returns>
        public static string MakeValidFileName(string text, char? replacement = '_', bool fancy = true)
        {
            StringBuilder sb = new StringBuilder(text.Length);
            var invalids = _invalids ?? (_invalids = Path.GetInvalidFileNameChars());
            bool changed = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (invalids.Contains(c))
                {
                    changed = true;
                    var repl = replacement ?? '\0';
                    if (fancy)
                    {
                        if (c == '"') repl = '”'; // U+201D right double quotation mark
                        else if (c == '\'') repl = '’'; // U+2019 right single quotation mark
                        else if (c == '/') repl = '⁄'; // U+2044 fraction slash
                    }
                    if (repl != '\0')
                        sb.Append(repl);
                }
                else
                    sb.Append(c);
            }
            if (sb.Length == 0)
                return "_";
            return changed ? sb.ToString() : text;
        }

        public static Bounds? CalculateBounds(IEnumerable<Transform> targets)
        {
            Bounds? b = null;
            foreach (var renderer in targets.SelectMany(x => x.GetComponentsInChildren<Renderer>()))
            {
                if (b == null)
                    b = renderer.bounds;
                else
                    b.Value.Encapsulate(renderer.bounds);
            }

            return b;
        }

        public static bool IsFavorited(this ItemInfo item) => QuickAccessBox.Instance.Favorited.Check(item.GUID, item.NewCacheId);
        public static bool IsBlacklisted(this ItemInfo item) => QuickAccessBox.Instance.Blacklisted.Check(item.GUID, item.NewCacheId);

        /// <summary>
        /// Repatable hash code for strings, not framework implementation dependent.
        /// https://stackoverflow.com/a/36845864
        /// </summary>
        public static int GetStableHashCode(this string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}