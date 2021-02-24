using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace SmartEntityFrameworkPocoGenerator
{
    class WordSplitter
    {
        private static List<string> globalKnownWords;
        private static List<string> globalWordsOne;
        private static List<string> globalWordsMany;
        
        public static string[] split(string s)
        {
            s = s.ToLowerInvariant();

            if (globalKnownWords == null)
            {
                globalKnownWords = new List<string>();
                globalWordsOne = new List<string>();
                globalWordsMany = new List<string>();

                foreach (var line in File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "known_words.lst")).Where(x => x.Length > 0))
                {
                    var words = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    globalKnownWords.AddRange(words);
                    if (words.Length == 2)
                    {
                        globalWordsOne.Add(words[0]);
                        globalWordsMany.Add(words[1]);
                    }
                }
            }

            var knownWords = globalKnownWords;
            var founds = new List<string[]>();
            find(s, knownWords, new string[0], founds);

            if (founds.Count == 0) return new [] { s };
            founds.Sort((a, b) => a.Length - b.Length);
            return founds[0];
        }

        private static void find(string s, IEnumerable<string> knownWords, IEnumerable<string> curWords, List<string[]> founds)
        {
            if (s == "") { founds.Add(curWords.ToArray()); return; }

            var m = Regex.Match(s, "^[0-9]+");
            if (m.Success)
            {
                find(s.Substring(m.Value.Length), knownWords, curWords.Concat(new[] { m.Value }), founds);
                return;
            }

            foreach (var knownWord in knownWords)
            {
                if (s.StartsWith(knownWord))
                {
                    find(s.Substring(knownWord.Length), knownWords, curWords.Concat(new[] { knownWord }), founds);
                }
            }
        }

        public static string toOne(string s)
        {
            var metaWords = s.Split('_');
            
            var words = split(metaWords[metaWords.Length - 1]);

            var n = globalWordsMany.IndexOf(words[words.Length - 1]);
            if (n >= 0)
            {
                return string.Join("_", metaWords.Take(metaWords.Length - 1).Concat(new [] {string.Join("", words.Take(words.Length - 1).Select(capitalize)) + capitalize(globalWordsOne[n].ToLowerInvariant()) }));
            }
            
            return s;
        }

        public static string toMany(string s)
        {
            var metaWords = s.Split('_');
            
            var words = split(metaWords[metaWords.Length - 1]);

            var n = globalWordsOne.IndexOf(words[words.Length - 1]);
            if (n >= 0)
            {
                return string.Join("_", metaWords.Take(metaWords.Length - 1).Concat(new [] {string.Join("", words.Take(words.Length - 1).Select(capitalize)) + capitalize(globalWordsMany[n].ToLowerInvariant()) }));
            }
            
            return s;
        }

        private static string capitalize(string s)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
        }
    }
}
