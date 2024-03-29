/*
    MIT License

    Copyright (c) 2020 Don Cross

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Gearbox
{
    public enum GameResult
    {
        InProgress,
        WhiteWon,
        BlackWon,
        Draw,
    }

    public class GameTags
    {
        private static readonly string[] RequiredKeys =
        {
            "Event", "Site", "Date", "Round", "White", "Black", "Result"
        };

        private static readonly Dictionary<string, string> DefaultValues = new Dictionary<string, string>()
        {
            { "Event", "?"  },
            { "Site",  "?"  },
            { "Date", "????.??.??" },
            { "Round", "?"  },
            { "White", "?"  },
            { "Black", "?"  },
            { "Result", "*" }
        };

        private readonly Dictionary<string, string> tags = new Dictionary<string, string>();

        private static string Normalize(string raw)
        {
            return Regex.Replace(raw ?? "", @"\s+", " ").Trim();
        }

        public GameTags(string initialFen = null)
        {
            InitialState = initialFen;
        }

        private GameTags(Dictionary <string, string> tags)
        {
            this.tags = tags;
        }

        public GameTags Clone()
        {
            var copy = new Dictionary<string, string>();

            foreach (var kv in this.tags)
                copy.Add(kv.Key, kv.Value);

            return new GameTags(copy);
        }

        public string InitialState
        {
            get
            {
                return GetTag("FEN");
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value) || Normalize(value) == Board.StandardSetup)
                {
                    tags.Remove("SetUp");
                    tags.Remove("FEN");
                }
                else
                {
                    SetTag("SetUp", "1");
                    SetTag("FEN", value);
                }
            }
        }

        public string GetTag(string key)
        {
            string value;
            if (tags.TryGetValue(key, out value))
                return value;

            if (DefaultValues.TryGetValue(key, out value))
                return value;

            return null;
        }

        public void SetTag(string key, string value)
        {
            if (value == null)
                tags.Remove(key);
            else
                tags[key] = Normalize(value);
        }

        public string Event
        {
            get { return GetTag("Event"); }
            set { SetTag("Event", value); }
        }

        public string Site
        {
            get { return GetTag("Site"); }
            set { SetTag("Site", value); }
        }

        public string Date
        {
            get { return GetTag("Date"); }
            set { SetTag("Date", value); }
        }

        public string Round
        {
            get { return GetTag("Round"); }
            set { SetTag("Round", value); }
        }

        public string White
        {
            get { return GetTag("White"); }
            set { SetTag("White", value); }
        }

        public string Black
        {
            get { return GetTag("Black"); }
            set { SetTag("Black", value); }
        }

        public static string FormatResult(GameResult result)
        {
            switch (result)
            {
                case GameResult.WhiteWon:   return "1-0";
                case GameResult.BlackWon:   return "0-1";
                case GameResult.Draw:       return "1/2-1/2";
                default:                    return "*";
            }
        }

        public static GameResult ParseResult(string text)
        {
            switch (text)
            {
                case "1-0":     return GameResult.WhiteWon;
                case "0-1":     return GameResult.BlackWon;
                case "1/2-1/2": return GameResult.Draw;
                default:        return GameResult.InProgress;
            }
        }

        public GameResult Result
        {
            get { return ParseResult(GetTag("Result")); }
            set { SetTag("Result", FormatResult(value)); }
        }

        private string TagLine(string key)
        {
            string value = GetTag(key);
            if (value == null)
                return "";

            var sb = new StringBuilder();
            sb.Append("[");
            sb.Append(key);
            sb.Append(" \"");
            sb.Append(value.Replace("\\", "\\\\").Replace("\"", "\\\""));
            sb.Append("\"]");
            sb.AppendLine();
            return sb.ToString();
        }

        public override string ToString()
        {
            // Emit the PGN header.
            var sb = new StringBuilder();

            // The PGN spec requires the "Seven Tag Roster" to appear first, and in the required order.
            foreach (string key in RequiredKeys)
                sb.Append(TagLine(key));

            // Any additional keys must appear sorted in case-sensitive ASCII order.
            string[] otherKeys = tags.Keys
                .Where(key => !RequiredKeys.Contains(key))
                .OrderBy(key => key)
                .ToArray();

            foreach (string key in otherKeys)
                sb.Append(TagLine(key));

            sb.AppendLine();
            return sb.ToString();
        }
    }
}
