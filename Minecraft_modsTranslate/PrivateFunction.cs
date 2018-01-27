using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static Minecraft_modsTranslate.Ini;

namespace Minecraft_modsTranslate
{
    public class PrivateFunction
    {
        //部分參考自https://github.com/huanlin/Chinese-Converter/blob/83c061b716e4f30d1bff8a4b9646a5ddff274774/Source/ChineseConverter/TSChineseDictionary.cs
        public class ChineseConverter
        {
            #region OS的轉換
            internal const int LOCALE_SYSTEM_DEFAULT = 0x0800;
            internal const int LCMAP_SIMPLIFIED_CHINESE = 0x02000000;
            internal const int LCMAP_TRADITIONAL_CHINESE = 0x04000000;

            /// <summary> 
            /// 使用OS的kernel.dll做為簡繁轉換工具，只要有裝OS就可以使用，不用額外引用dll，但只能做逐字轉換，無法進行詞意的轉換 
            /// <para>所以無法將電腦轉成計算機</para> 
            /// </summary> 
            [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern int LCMapString(int Locale, int dwMapFlags, string lpSrcStr, int cchSrc, [Out] string lpDestStr, int cchDest);

            /// <summary> 
            /// 繁體轉簡體 
            /// </summary> 
            /// <param name="pSource">要轉換的繁體字：體</param> 
            /// <returns>轉換後的簡體字：體</returns> 
            public static string ToSimplified(string pSource)
            {
                String tTarget = new String(' ', pSource.Length);
                int tReturn = LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_SIMPLIFIED_CHINESE, pSource, pSource.Length, tTarget, pSource.Length);
                return tTarget;
            }

            /// <summary> 
            /// 簡體轉繁體 
            /// </summary> 
            /// <param name="pSource">要轉換的繁體字：體</param> 
            /// <returns>轉換後的簡體字：體</returns> 
            public static string ToTraditional(string pSource)
            {
                String tTarget = new String(' ', pSource.Length);
                int tReturn = LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_TRADITIONAL_CHINESE, pSource, pSource.Length, tTarget, pSource.Length);
                return tTarget;
            }

            #endregion OS的轉換

            private SortedDictionary<string, string> _dictionary;
            private bool _hasError;
            private StringBuilder _logs;
            FastReplace FR = new FastReplace();
            public ChineseConverter()
            {
                var cmp = new WordMappingComparer();
                _dictionary = new SortedDictionary<string, string>(cmp);
                _logs = new StringBuilder();
                _hasError = false;
            }
            public void ClearLogs()
            {
                _logs.Clear();
                _hasError = false;
            }
            public void Load(string fileName)
            {
                int i = 0;
                using (var reader = new StreamReader(fileName, Encoding.UTF8))
                {
                    i++;
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        this.Add(line);
                        line = reader.ReadLine();
                        i++;
                    }
                }

            }

            public void Load(string[] fileNames)
            {
                foreach (string fname in fileNames)
                {
                    Load(fname);
                }
            }
            public void ReloadFastReplaceDic()
            {
                FR = new FastReplace(_dictionary);
            }

            public ChineseConverter Add(string sourceWord, string targetWord)
            {
                if (sourceWord == targetWord)
                    return this;
                // Skip duplicated words.
                if (_dictionary.ContainsKey(sourceWord))
                {
                    _hasError = true;
                    _logs.AppendLine(String.Format("警告: '{0}={1}' 的來源字串重複定義, 故忽略此項。", sourceWord, targetWord));
                    return this;
                }
                _dictionary.Add(sourceWord, targetWord);
                return this;
            }

            public ChineseConverter Add(string mapping)
            {
                if (!String.IsNullOrWhiteSpace(mapping) && !mapping.StartsWith(";"))
                {
                    Regex r = new Regex("(.*?),(.*)");
                    var m = r.Match(mapping);
                    if (m.Success)
                    {
                        this.Add(m.Groups[1].ToString(), m.Groups[2].ToString());
                    }
                }
                return this;
            }

            public ChineseConverter Add(IDictionary<string, string> dict)
            {
                foreach (var key in dict.Keys)
                {
                    Add(key, dict[key]);
                }
                return this;
            }

            public string Convert(string input)
            {
                //這個方法最快
                return FR.ReplaceAll(input);
                /* 第二快
                foreach (var temp in _dictionary)
                {
                    input = input.Replace(temp.Key, temp.Value);
                }
                return input;*/
                /* 最慢
                StringBuilder sb = new StringBuilder(input);
                foreach(var temp in _dictionary)
                {
                    sb.Replace(temp.Key, temp.Value);                    
                }
                return input;*/
            }

            public void DumpKeys()
            {
                foreach (var key in _dictionary.Keys)
                {
                    Console.WriteLine(key);
                }
            }

            public bool HasError
            {
                get
                {
                    return _hasError;
                }
            }

            public string Logs
            {
                get
                {
                    return _logs.ToString();
                }
            }
        }


        internal class WordMappingComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                // 越長的字串排在越前面，字串長度相等者，則採用字串預設的比序。
                if (x.Length == y.Length)
                {
                    return x.CompareTo(y);
                }
                return y.Length - x.Length;
            }
        }

        public static void ReadAllIniFile(string text, ref Ini iniBox)
        {
            iniBox.Lines.Clear();
            string temp;
            string Sec = "";
            Match match;
            Regex m = new Regex(@"^\[(.*)\]");
            Regex m2 = new Regex(@"(.*[^=])\=(.*)");
            using (StringReader sr = new StringReader(text))
            {
                while (null != (temp = sr.ReadLine()))
                {
                    if (temp.IndexOf(";") == 0)
                        continue;
                    match = m.Match(temp);
                    if (match.Success)
                    {
                        Sec = match.Groups[1].ToString();
                        continue;
                    }
                    match = m2.Match(temp);
                    if (match.Success)
                        iniBox.Lines.Add(new Line { Section = Sec, Key = match.Groups[1].ToString(), Value = match.Groups[2].ToString() });
                }
            }
        }
        public static void ReadAllIniFile(StreamReader stream, ref Ini iniBox)
        {
            iniBox.Lines.Clear();
            string temp;
            string Sec = "";
            Match match;
            Regex m = new Regex(@"^\[(.*)\]");
            Regex m2 = new Regex(@"(.*[^=])\=(.*)");
            while (!stream.EndOfStream)
            {
                temp = stream.ReadLine();
                if (temp.IndexOf(";") == 0)
                    continue;
                match = m.Match(temp);
                if (match.Success)
                {
                    Sec = match.Groups[1].ToString();
                    continue;
                }
                match = m2.Match(temp);
                if (match.Success)
                    iniBox.Lines.Add(new Line { Section = Sec, Key = match.Groups[1].ToString(), Value = match.Groups[2].ToString() });
            }
        }

        public static string IniConvertToText(List<Line> iniBox)
        {
            StringBuilder sb = new StringBuilder();
            var sectionList = iniBox.Select(x => x.Section).Distinct();
            if (sectionList.FirstOrDefault() != "" && sectionList.FirstOrDefault() != null)
            {
                var g = iniBox.GroupBy(x => x.Section).ToDictionary(x => x.Key, x => x.ToList());
                foreach (var t in sectionList)
                {
                    sb.AppendLine(String.Format("[{0}]", t));
                    var temp = g[t].ToList();
                    temp.ForEach(x => sb.AppendLine(String.Format("{0}={1}", x.Key, x.Value)));
                }
            }
            else
            {
                iniBox.ForEach(x => sb.AppendLine(String.Format("{0}={1}", x.Key, x.Value)));
            }
            return sb.ToString();
        }


        public class FileStatusHelper
        {            
            /// <summary>
            /// 檔案室否被占用
            /// </summary>
            /// <param name="filePath">檔案路徑</param>
            /// <returns></returns>
            public static bool IsFileOccupied(string filePath)
            {
                bool inUse = true;
                FileStream fs = null;
                try
                {
                    fs = new FileStream(filePath, FileMode.Open, FileAccess.Read,

                    FileShare.None);

                    inUse = false;
                }
                catch{}
                finally
                {
                    if (fs != null)
                        fs.Close();
                }
                return inUse;//true表示正在使用,false没有使用
            }            
        }
    }
}


