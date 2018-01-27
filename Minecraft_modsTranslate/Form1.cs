using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static Minecraft_modsTranslate.Ini;

namespace Minecraft_modsTranslate
{
    public partial class Form1 : Form
    {
        PrivateFunction.ChineseConverter CC = new PrivateFunction.ChineseConverter();
        public class CustomStaticDataSource : IStaticDataSource
        {
            private Stream _stream;
            // Implement method from IStaticDataSource
            public Stream GetSource()
            {
                return _stream;
            }

            // Call this to provide the memorystream
            public void SetStream(Stream inputStream)
            {
                _stream = inputStream;
                _stream.Position = 0;
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void button_ChooseFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = textBox1.Text == "" ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft") : textBox1.Text;
            if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                textBox1.Text = fbd.SelectedPath;
                Properties.Settings.Default.LastPath = textBox1.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void button_Run_Click(object sender, EventArgs e)
        {
            CC.ReloadFastReplaceDic();
            TranslateAndUpdatingZip(textBox1.Text, "*.jar");
            TranslateAndUpdatingZip(textBox1.Text, "*.zip");
            MessageBox.Show("Success!");
        }

        public void TranslateAndUpdatingZip(string path, string searchPattern)
        {           
            List<ZipEntity> list_en_US = new List<ZipEntity>();
            List<ZipEntity> list_zh_CN = new List<ZipEntity>();
            List<ZipEntity> list_zh_TW = new List<ZipEntity>();
            if (!Directory.Exists(path))
            {
                MessageBox.Show("找不到資料夾");
                return;
            }
            string[] fileList = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
            //建立清單
            foreach (string zipFile in fileList)
            {
                if (File.Exists(zipFile))
                {
                    using (ZipFile zip = new ZipFile(zipFile))
                    {
                        Regex r = new Regex("(en_US)|(zh_CN)|(zh_TW)", RegexOptions.IgnoreCase);
                        foreach (ZipEntry entry in zip)
                        {
                            if (entry.IsFile)
                            {
                                var m = r.Match(entry.Name);
                                if (m.Groups.Count > 1)
                                {
                                    if (m.Groups[1].Length != 0)
                                        list_en_US.Add(new ZipEntity() { ZipPath = zipFile, Name = entry.Name });
                                    else if (m.Groups[2].Length != 0)
                                        list_zh_CN.Add(new ZipEntity() { ZipPath = zipFile, Name = entry.Name });
                                    else if (m.Groups[3].Length != 0)
                                        list_zh_TW.Add(new ZipEntity() { ZipPath = zipFile, Name = entry.Name });
                                }
                            }
                        }
                    }
                }
            }
            /*取得副檔名清單
            List<string> ggggg = new List<string>();
            list_zh_CN.ForEach(x => ggggg.Add(Path.GetExtension(x.Name)));
            ggggg = ggggg.Distinct().ToList();
            foreach (var jj in ggggg)
                System.Diagnostics.Debug.Print(list_zh_CN.Find(x => Path.GetExtension(x.Name) == jj).Name);*/
            var needUpdatingFiles = list_zh_CN.Select(x => x.ZipPath).Distinct();
            foreach (var FileListTemp in needUpdatingFiles)
            {
                bool IsIgnore = false;
                while (!IsIgnore&&PrivateFunction.FileStatusHelper.IsFileOccupied(FileListTemp))
                {
                    switch(MessageBox.Show(FileListTemp + "\n該檔案已被占用，請自行關閉該檔案，是否繼續?", "檔案被占用啦!!", MessageBoxButtons.AbortRetryIgnore))                    
                    {
                        case DialogResult.Abort:
                            return;
                        case DialogResult.Ignore:
                            IsIgnore = true;
                            break;                         
                    }
                }
                if (IsIgnore)
                    continue;
                var temp = list_zh_CN.Where(x => x.ZipPath == FileListTemp);
                using (ZipFile zip = new ZipFile(FileListTemp))
                {
                    zip.BeginUpdate();
                    foreach (var entity in temp)
                    {
                        Stream zipStream_CN = zip.GetInputStream(zip.GetEntry(entity.Name));
                        // Manipulate the output filename here as desired.
                        using (StreamReader sr_CN = new StreamReader(zipStream_CN, Encoding.UTF8))
                        {
                            StringBuilder sb_CN = new StringBuilder();

                            

                            if (checkedListBox1.GetItemChecked(0))

                                sb_CN.AppendLine(CC.Convert(PrivateFunction.ChineseConverter.ToTraditional(sr_CN.ReadToEnd())));
                            else
                                sb_CN.AppendLine(PrivateFunction.ChineseConverter.ToTraditional(sr_CN.ReadToEnd()));

                            sr_CN.Close();
                            if ((!checkedListBox1.GetItemChecked(1)) && (!checkedListBox1.GetItemChecked(2)))
                            {
                                MemoryStream ms2 = new MemoryStream(Encoding.UTF8.GetBytes(sb_CN.ToString()));
                                CustomStaticDataSource sds = new CustomStaticDataSource();
                                sds.SetStream(ms2);
                                zip.Add(sds, entity.Name.Replace("zh_CN", "zh_TW").Replace("zh_cn", "zh_tw"));
                                ms2.Close();
                                continue;
                            }
                            if (entity.Name.Contains("blankpattern.json"))
                            {
                                Stream zipStream_CN2 = zip.GetInputStream(zip.GetEntry(entity.Name));
                                StreamReader sr_CNs = new StreamReader(zipStream_CN2, Encoding.UTF8);
                                System.Diagnostics.Debug.Print(CC.Convert(PrivateFunction.ChineseConverter.ToTraditional(sr_CNs.ReadToEnd())));

                            }
                                
                            string stringOutput = "";
                            //簡單判斷是否是ini格式
                            if (Path.GetExtension(entity.Name).Replace(".properties", "").Replace(".lang", "") == "")
                            {
                                Ini ini_EN = new Ini(), ini_CN = new Ini(), ini_TW = new Ini();
                                PrivateFunction.ReadAllIniFile(sb_CN.ToString(), ref ini_CN);

                                //讀取zh_TW
                                if (checkedListBox1.GetItemChecked(1) && list_zh_TW.Exists(x => x.ZipPath == FileListTemp && x.Name == entity.Name.Replace("zh_CN", "zh_TW").Replace("zh_cn", "zh_tw")))
                                {
                                    Stream zipStream_TW = zip.GetInputStream(zip.GetEntry(entity.Name.Replace("zh_CN", "zh_TW").Replace("zh_cn", "zh_tw")));
                                    using (StreamReader sr_TW = new StreamReader(zipStream_TW, Encoding.UTF8))
                                    {
                                        PrivateFunction.ReadAllIniFile(sr_TW, ref ini_TW);
                                        sr_TW.Close();
                                        zipStream_TW.Close();
                                    }
                                }
                                //讀取en_US
                                if (checkedListBox1.GetItemChecked(2) && list_en_US.Exists(x => x.ZipPath == FileListTemp && x.Name == entity.Name.Replace("zh_CN", "en_US").Replace("zh_cn", "en_us")))
                                {
                                    Stream zipStream_EN = zip.GetInputStream(zip.GetEntry(entity.Name.Replace("zh_CN", "en_US").Replace("zh_cn", "en_us")));
                                    using (StreamReader sr_EN = new StreamReader(zipStream_EN, Encoding.UTF8))
                                    {
                                        PrivateFunction.ReadAllIniFile(sr_EN, ref ini_EN);
                                        sr_EN.Close();
                                        zipStream_EN.Close();
                                    }
                                }

                                List<Line> tempLineList = new List<Line>();


                                //如果有簡體又有繁體，則在暫存加入繁體的
                                foreach (Line l in ini_CN.Lines)
                                {
                                    var Ltemp = ini_TW.Lines.Find(x => x.Section == l.Section && x.Key == l.Key);
                                    if (Ltemp != null)
                                        tempLineList.Add(Ltemp);
                                }
                                //如果只有簡體沒有繁體，加入簡體
                                foreach (Line l in ini_CN.Lines)
                                {
                                    if (!ini_TW.Lines.Exists(x => x.Section == l.Section && x.Key == l.Key))
                                        tempLineList.Add(l);
                                }
                                //如果只有繁體沒有簡體，加入繁體
                                foreach (Line l in ini_TW.Lines)
                                {
                                    if (!ini_CN.Lines.Exists(x => x.Section == l.Section && x.Key == l.Key))
                                        tempLineList.Add(l);
                                }
                                //如果有英文沒有中文，則在暫存加入英文的
                                foreach (Line l in ini_EN.Lines)
                                {
                                    if (!tempLineList.Exists(x => x.Section == l.Section && x.Key == l.Key))
                                        tempLineList.Add(l);
                                }

                                //雙語翻譯         
                                /*由於.lang中不能換行，因此暫時失敗
                                if (checkedListBox1.GetItemChecked(3))
                                    foreach (Line l in ini_EN.Lines)
                                    {
                                        var t = tempLineList.FirstOrDefault(x => x.Section == l.Section && x.Key == l.Key && !x.Value.EndsWith(l.Value));
                                        if (t != null)
                                        {
                                            //%s不能重複使用，因此需要特別處理
                                            int sCount = (l.Value.Length - l.Value.Replace("%s", "").Length) / 2;
                                            if (sCount > 0)
                                            {
                                                string[] stringtemp = l.Value.Split(new string[] { "%s" }, StringSplitOptions.None);
                                                int i = 0;
                                                StringBuilder sb = new StringBuilder(l.Value.Length + sCount * 2);
                                                foreach (var s in stringtemp)
                                                {
                                                    sb.Append(s);
                                                    i++;
                                                    if (i < stringtemp.Length)
                                                        sb.AppendFormat("%{0}$s", i);
                                                }
                                                t.Value = String.Format("{0}\\n{1}", t.Value, sb.ToString());
                                            }
                                            else
                                                t.Value = String.Format("{0}\\n{1}", t.Value, l.Value);
                                        }
                                    }*/
                                stringOutput = PrivateFunction.IniConvertToText(tempLineList);
                            }
                            else
                            {
                                stringOutput = sb_CN.ToString();
                            }
                            MemoryStream ms_output = new MemoryStream(Encoding.UTF8.GetBytes(stringOutput));
                            CustomStaticDataSource sdss = new CustomStaticDataSource();
                            sdss.SetStream(ms_output);

                            zip.Add(sdss, entity.Name.Replace("zh_CN", "zh_TW").Replace("zh_cn", "zh_tw"));
                            //ms_output.Close();
                        }
                    }
                    try { zip.CommitUpdate(); }
                    catch (Exception e1)
                    {
                        MessageBox.Show(String.Format("{0}寫入失敗\n\n錯誤資訊如下：\n\n{1}\n\n請稍後再嘗試", FileListTemp, e1.Message));
                    }                    
                    zip.Close();
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.LastPath != "")
                textBox1.Text = Properties.Settings.Default.LastPath;
            checkedListBox1.SetItemChecked(0, true);
            checkedListBox1.SetItemChecked(1, true);
            checkedListBox1.SetItemChecked(2, true);
            string[] fileList = Directory.GetFiles(Path.Combine(Application.StartupPath, "Dictionary"), "*.dat", SearchOption.AllDirectories);
            foreach (var t in fileList)
                CC.Load(t);
        }
    }
}
