using System.Diagnostics;
using System.Reflection;
using Wistu.Lib.ClassifyModel;

namespace Tile_Classifier
{
    public partial class Form1 : Form
    {
        class PicLable
        {
            public static Dictionary<string, string> Summary { get; } = new Dictionary<string, string>();
            public string File { get; set; }
            public string Lable { get; set; }
            public string SymmariedLable
            {
                get
                {
                    return Summary.ContainsKey(Lable) ? (string.IsNullOrWhiteSpace(Summary[Lable]) ? Lable : Summary[Lable]) : Lable;
                }
            }
            public override string ToString()
            {
                return $"{(string.IsNullOrEmpty(Lable) ? "" : $"[{(Summary.ContainsKey(Lable) ? (string.IsNullOrWhiteSpace(Summary[Lable]) ? Lable : Summary[Lable]) : Lable)}] ")}{Path.GetFileName(File)}";
            }
        }

        Dictionary<string, PicLable> Lables = new();
        string DirPath { get; set; }
        private string savePath;
        bool IsLoadedModel = false;
        bool IsPredicting = false;

        string CurrentPath => Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        string SavePath
        {
            get
            {
                if (savePath != null)
                {
                    return savePath;
                }
                if (DirPath == null)
                {
                    return null;
                }
                return Path.Combine(DirPath, $"{Path.GetFileName(DirPath)}.CProj");
            }
        }

        PicLable? CurrentLable
        {
            get
            {
                if (listBox1.SelectedIndex == -1)
                {
                    return null;
                }
                return listBox1.Items[listBox1.SelectedIndex] as PicLable;
            }
        }

        public Form1()
        {
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            InitializeComponent();
            pictureBox1.MouseWheel += MouseWheel;
            pictureBox1.MouseDown += MouseDown;
            pictureBox1.MouseMove += MouseMove;
            pictureBox1.MouseUp += MouseUp;
            string recFile = Path.Combine(CurrentPath, "recent.log");
            if (File.Exists(recFile))
            {
                foreach (var item in File.ReadAllLines(recFile))
                {
                    if (File.Exists(item))
                    {
                        最近打开RToolStripMenuItem.DropDownItems.Add(item);
                    }
                }
                最近打开RToolStripMenuItem.DropDownItemClicked += 最近打开RToolStripMenuItem_DropDownItemClicked;
                最近打开RToolStripMenuItem.Enabled = true;
            }
            foreach (var item in Classifier.TotalModels.Reverse())
            {
                加载LToolStripMenuItem.DropDownItems.Add(item);
            }
            加载LToolStripMenuItem.DropDownItemClicked += 模型MToolStripMenuItem_DropDownItemClicked;
            颜色ToolStripMenuItem.DropDownItemClicked += 颜色ToolStripMenuItem_DropDownItemClicked;
        }

        private void 颜色ToolStripMenuItem_DropDownItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            foreach (ToolStripMenuItem item in 颜色ToolStripMenuItem.DropDownItems)
            {
                item.Checked = false;
            }
            (e.ClickedItem as ToolStripMenuItem).Checked = true;
            switch (e.ClickedItem.Text)
            {
                case "Green":
                    corePen.Color = pen.Color = Color.Green;
                    break;
                case "Light Green":
                    corePen.Color = pen.Color = Color.LightGreen;
                    break;
                case "Dark Green":
                    corePen.Color = pen.Color = Color.DarkGreen;
                    break;
                case "Yellow":
                    corePen.Color = pen.Color = Color.Yellow;
                    break;
                case "Light Yellow":
                    corePen.Color = pen.Color = Color.LightYellow;
                    break;
                case "Yellow Green":
                    corePen.Color = pen.Color = Color.YellowGreen;
                    break;
            }
            pictureBox1.Refresh();
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            File.WriteAllText(Path.Combine(CurrentPath, "threadcrash.log"), e.Exception.ToString());
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText(Path.Combine(CurrentPath, "crash.log"), e.ExceptionObject.ToString());
        }

        private void 关闭CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseCProj();
        }

        private void CloseCProj()
        {
            savePath = null;
            DirPath = null;
            listBox1.Items.Clear();
            Lables.Clear();
            pictureBox1.Image = null;
            UpdateProgress();
            Text = "Tile Classifier";
        }

        private void 模型MToolStripMenuItem_DropDownItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            loadModel(e.ClickedItem.Text);
        }

        private void 加载最新模型NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadModel(Classifier.ModelName);
        }

        private void loadModel(string modelName)
        {
            Form3 form = new();
            Task.Run(() =>
            {
                Classifier.ModelName = modelName;
                if (Classifier.Labels.Length > 10)
                {
                    Invoke(() =>
                    {
                        MessageBox.Show("The maximum number of model tags supported is 10, and the current model has exceeded that limit.");
                        MessageBox.Show("Model not loaded.");
                    });
                    return;
                }
                PicLable.Summary.Clear();
                for (int i = 0; i < Classifier.Labels.Length; i++)
                {
                    Invoke(() =>
                    {
                        TextBox textBox = null;
                        switch (i)
                        {
                            case 0:
                                textBox = textBox1;
                                break;
                            case 1:
                                textBox = textBox2;
                                break;
                            case 2:
                                textBox = textBox3;
                                break;
                            case 3:
                                textBox = textBox4;
                                break;
                            case 4:
                                textBox = textBox5;
                                break;
                            case 5:
                                textBox = textBox6;
                                break;
                            case 6:
                                textBox = textBox7;
                                break;
                            case 7:
                                textBox = textBox8;
                                break;
                            case 8:
                                textBox = textBox9;
                                break;
                            case 9:
                                textBox = textBox10;
                                break;
                            default:
                                break;
                        }
                        textBox.Text = Classifier.Labels[i];
                        PicLable.Summary.Add((i + 1).ToString(), Classifier.Labels[i]);
                    });
                }

                if (PicLable.Summary.ContainsKey("10"))
                {
                    PicLable.Summary.Add("0", PicLable.Summary["10"]);
                    PicLable.Summary.Remove("10");
                }

                try
                {
                    Classifier.Init();
                }
                catch (Exception e)
                {
                    Invoke(() =>
                    {
                        MessageBox.Show($"Model loading failure: {e.Message}");
                        form.Close();
                        return;
                    });
                    return;
                }

                Invoke(() =>
                {
                    toolStripStatusLabel_Model.Text = "Model:" + Classifier.ModelName;
                    textBox1.Enabled = textBox2.Enabled = textBox3.Enabled = textBox4.Enabled = textBox5.Enabled = textBox6.Enabled
                        = textBox7.Enabled = textBox8.Enabled = textBox9.Enabled = textBox10.Enabled = false;
                    加载LToolStripMenuItem.Text = "Loaded";
                    加载LToolStripMenuItem.Enabled = false;
                    label11.Text = "Model ready";
                    label12.Visible = true;
                    IsLoadedModel = true;
                    MessageBox.Show("Editing of tags is no longer allowed because the recognition model is loaded.");
                    form.Close();
                });
            });
            form.ShowDialog(this);
        }

        private void 最近打开RToolStripMenuItem_DropDownItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            Open(e.ClickedItem.Text);
        }

        private void 新建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new();
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            CloseCProj();
            DirPath = folderBrowserDialog.SelectedPath;

            if (File.Exists(SavePath))
            {
                if (MessageBox.Show("There is already an item in this directory, is it overwritten?", "Ask", MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    if (MessageBox.Show("Want to help you open an existing project?", "Ask", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Open(SavePath);
                        return;
                    }
                    return;
                }
            }

            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            foreach (var item in Directory.GetFiles(DirPath, "*.jpg"))
            {
                PicLable lable = new() { File = item };
                listBox1.Items.Add(lable);
            }
            listBox1.EndUpdate();
            LoadNext();
            listBox1.Focus();
            Save();
            SaveRecent();
            Text += " " + SavePath;
            toolStripStatusLabel1.Text = $"Total {listBox1.Items.Count} items";
        }

        private void Save()
        {
            using FileStream memory = new(SavePath, FileMode.Create);
            using BinaryWriter writer = new(memory);
            writer.Write(PicLable.Summary.Count);
            foreach (var item in PicLable.Summary)
            {
                writer.Write(item.Key);
                writer.Write(item.Value ?? "");
            }
            writer.Write(Lables.Count);
            foreach (var item in Lables)
            {
                writer.Write(item.Key);
                writer.Write(item.Value.Lable ?? "");
            }
            writer.Write(listBox1.SelectedIndex);
            writer.Flush();
        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Classification Project(*.CProj)|*.CProj",
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            Open(openFileDialog.FileName);
        }

        private void Open(string path)
        {
            CloseCProj();
            savePath = path;
            DirPath = Path.GetDirectoryName(path);
            using FileStream memory = new(path, FileMode.Open);
            using BinaryReader reader = new(memory);

            int c = reader.ReadInt32();
            for (int i = 0; i < c; i++)
            {
                string key = reader.ReadString(), value = reader.ReadString();
                if (!IsLoadedModel)
                {
                    PicLable.Summary.Add(key, value);
                }
            }
            if (!IsLoadedModel)
                LoadSummary();

            c = reader.ReadInt32();
            for (int i = 0; i < c; i++)
            {
                string fp = reader.ReadString();
                fp = Path.Combine(DirPath, Path.GetFileName(fp));
                if (!File.Exists(fp))
                {
                    continue;
                }
                string lable = reader.ReadString();
                Lables.Add(fp, new PicLable { File = fp, Lable = lable == "" ? null : lable });
            }
            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            foreach (var item in Directory.GetFiles(DirPath, "*.jpg"))
            {
                if (Lables.ContainsKey(item))
                {
                    listBox1.Items.Add(Lables[item]);
                }
                else
                {
                    listBox1.Items.Add(new PicLable() { File = item });
                }
            }
            listBox1.EndUpdate();
            try
            {
                c = reader.ReadInt32();
                listBox1.SelectedIndex = c;
            }
            catch { }
            UpdateProgress();
            SaveRecent();

            Text += " " + SavePath;
            toolStripStatusLabel1.Text = $"Total {listBox1.Items.Count} items";
        }

        private void SaveRecent()
        {
            string recFile = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "recent.log");
            string r = SavePath;
            var oldLines = File.ReadAllLines(recFile);
            List<string> nOld = new List<string>();
            int n = 0;
            foreach (var item in oldLines)
            {
                if (n > 9)
                {
                    break;
                }
                if (item.ToLower().Trim() == r.ToLower().Trim())
                {
                    continue;
                }
                nOld.Add(item);
                n++;
            }
            nOld.Insert(0, r);
            File.WriteAllLines(recFile, nOld);
        }

        private void LoadSummary()
        {
            foreach (var item in PicLable.Summary)
            {
                switch (item.Key)
                {
                    case "1":
                        textBox1.Text = PicLable.Summary[item.Key];
                        break;
                    case "2":
                        textBox2.Text = PicLable.Summary[item.Key];
                        break;
                    case "3":
                        textBox3.Text = PicLable.Summary[item.Key];
                        break;
                    case "4":
                        textBox4.Text = PicLable.Summary[item.Key];
                        break;
                    case "5":
                        textBox5.Text = PicLable.Summary[item.Key];
                        break;
                    case "6":
                        textBox6.Text = PicLable.Summary[item.Key];
                        break;
                    case "7":
                        textBox7.Text = PicLable.Summary[item.Key];
                        break;
                    case "8":
                        textBox8.Text = PicLable.Summary[item.Key];
                        break;
                    case "9":
                        textBox9.Text = PicLable.Summary[item.Key];
                        break;
                    case "0":
                        textBox10.Text = PicLable.Summary[item.Key];
                        break;
                    default:
                        break;
                }
            }
        }

        private void LoadNext(bool isAutoNext = true)
        {
            if (listBox1.Items.Count == 0)
            {
                return;
            }
            if (!isAutoNext)
            {
                if (listBox1.SelectedIndex + 1 < listBox1.Items.Count)
                {
                    listBox1.SelectedIndex++;
                }
                else
                {
                    IsAutoShow = false;
                    MessageBox.Show("It is already the last picture.");
                }
                return;
            }
            do
            {
                if (listBox1.SelectedIndex + 1 < listBox1.Items.Count)
                {
                    listBox1.SelectedIndex++;
                }
                else
                {
                    IsAutoShow = false;
                    MessageBox.Show("It is already the last picture.");
                    break;
                }
            } while (Lables.ContainsKey(CurrentLable.File));
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CurrentLable == null)
            {
                return;
            }

            BetterLabel = null;

            FileStream pFileStream = new FileStream(CurrentLable.File, FileMode.Open, FileAccess.Read);
            pictureBox1.Image = Image.FromStream(pFileStream);
            pFileStream.Close();
            pFileStream.Dispose();

            //pictureBox1.Load(CurrentLable.File);
            if (label11.Text == "Model ready")
                label11.Text = "Identification in progress, the first identification is slow, please wait patiently";
            else
                label11.Text = "Identification in progress";

            if (IsLoadedModel)
            {
                IsPredicting = true;
                string file = CurrentLable.File;
                Task.Run(() =>
                {
                    try
                    {
                        var re = Classifier.Predict(new ModelInput { Image = File.ReadAllBytes(file) });
                        Dictionary<string, float> dts = new Dictionary<string, float>();
                        int i = 0;
                        foreach (var item in Classifier.Labels)
                        {
                            dts.Add(item, re.Scores[i]);
                            i++;
                        }
                        BetterLabel = re.BetterLable;
                        string s = "";
                        foreach (var item in dts.OrderByDescending((o) => o.Value))
                        {
                            s += $"{item.Key}({item.Value * 100:0.00}%) ";
                        }
                        Invoke(() =>
                        {
                            label11.Text = s;
                            label12.Text = re.BetterLable;
                            IsPredicting = false;
                            listBox1.Focus();
                        });
                    }
                    catch (Exception e)
                    {
                        Invoke(() =>
                        {
                            IsPredicting = false;
                            MessageBox.Show(e.Message);
                            listBox1.Focus();
                        });
                    }
                });
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            string tag = button.Tag.ToString();
            if (tag == "-")
            {
                listBox1.BeginUpdate();
                listBox1.Items.Clear();
                foreach (var item in Directory.GetFiles(DirPath, "*.jpg"))
                {
                    if (Lables.ContainsKey(item))
                    {
                        listBox1.Items.Add(Lables[item]);
                    }
                    else
                    {
                        listBox1.Items.Add(new PicLable() { File = item });
                    }
                }
                listBox1.EndUpdate();

                button11.Enabled = false;
            }
            else
            {
                listBox1.BeginUpdate();
                listBox1.Items.Clear();
                Lables.Values.Where((x) => x.Lable == tag).ToList().ForEach((x) => listBox1.Items.Add(x));
                listBox1.EndUpdate();

                button11.Enabled = true;
            }
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            string tag = "";
            switch (e.KeyCode)
            {
                case Keys.A:
                    IsAutoShow = true;
                    Task.Run(AutoShow);
                    e.SuppressKeyPress = true;
                    return;
                case Keys.D0:
                    tag = "0";
                    break;
                case Keys.D1:
                    tag = "1";
                    break;
                case Keys.D2:
                    tag = "2";
                    break;
                case Keys.D3:
                    tag = "3";
                    break;
                case Keys.D4:
                    tag = "4";
                    break;
                case Keys.D5:
                    tag = "5";
                    break;
                case Keys.D6:
                    tag = "6";
                    break;
                case Keys.D7:
                    tag = "7";
                    break;
                case Keys.D8:
                    tag = "8";
                    break;
                case Keys.D9:
                    tag = "9";
                    break;
                case Keys.Delete:
                    tag = "D";
                    break;
                case Keys.Space:
                    tag = "A";
                    break;
                case Keys.L:
                    if (十字辅助线ToolStripMenuItem.Checked)
                    {
                        十字辅助线ToolStripMenuItem.Checked = false;
                        网格辅助线ToolStripMenuItem.Checked = true;
                    }
                    else if (网格辅助线ToolStripMenuItem.Checked)
                    {
                        网格辅助线ToolStripMenuItem.Checked = false;
                        对角辅助线ToolStripMenuItem.Checked = true;
                    }
                    else if (对角辅助线ToolStripMenuItem.Checked)
                    {
                        对角辅助线ToolStripMenuItem.Checked = false;
                        复合辅助线ToolStripMenuItem.Checked = true;
                    }
                    else if (复合辅助线ToolStripMenuItem.Checked)
                    {
                        复合辅助线ToolStripMenuItem.Checked = false;
                    }
                    else
                    {
                        十字辅助线ToolStripMenuItem.Checked = true;
                    }
                    pictureBox1.Refresh();
                    break;
            }
            if (tag != "")
            {
                if (CurrentLable == null)
                {
                    return;
                }
                if (tag == "A" && string.IsNullOrEmpty(BetterLabel))
                {
                    return;
                }
                bool isAutoNext = true;
                if (!Lables.ContainsKey(CurrentLable.File))
                    Lables.Add(CurrentLable.File, CurrentLable);
                else if (!string.IsNullOrWhiteSpace(Lables[CurrentLable.File].Lable))
                    isAutoNext = false;
                if (tag == "D")
                    Lables[CurrentLable.File].Lable = null;
                else if (tag == "A")
                {
                    Lables[CurrentLable.File].Lable = PicLable.Summary.FirstOrDefault(o => o.Value == BetterLabel).Key;
                }
                else
                    Lables[CurrentLable.File].Lable = tag;
                var o = listBox1.Items[listBox1.SelectedIndex];
                listBox1.Items[listBox1.SelectedIndex] = new object();
                listBox1.Update();
                listBox1.Items[listBox1.SelectedIndex] = o;
                listBox1.Update();
                UpdateProgress();
                Save();
                LoadNext(isAutoNext);
                e.SuppressKeyPress = true;
            }
        }

        private void UpdateProgress()
        {
            if (SavePath != null)
            {
                toolStripProgressBar1.Maximum = Directory.GetFiles(Path.GetDirectoryName(SavePath), "*.jpg").Length;
                toolStripProgressBar1.Value = Lables.Values.Count((v) => !string.IsNullOrWhiteSpace(v.Lable));
            }
            else
            {
                toolStripProgressBar1.Maximum = 0;
                toolStripProgressBar1.Value = 0;
            }
            toolStripStatusLabel1.Text = $"{toolStripProgressBar1.Value}/{toolStripProgressBar1.Maximum} {(double)toolStripProgressBar1.Value / toolStripProgressBar1.Maximum * 100d:0.00}%";

            int[] c = new int[10];
            foreach (var item in Lables.Values)
            {
                if (!string.IsNullOrWhiteSpace(item.Lable))
                {
                    c[int.Parse(item.Lable)]++;
                }
            }

            num1.Text = c[1].ToString();
            num2.Text = c[2].ToString();
            num3.Text = c[3].ToString();
            num4.Text = c[4].ToString();
            num5.Text = c[5].ToString();
            num6.Text = c[6].ToString();
            num7.Text = c[7].ToString();
            num8.Text = c[8].ToString();
            num9.Text = c[9].ToString();
            num0.Text = c[0].ToString();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var textbox = sender as TextBox;
                string tag = textbox.Tag.ToString();
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    if (!PicLable.Summary.ContainsKey(tag))
                    {
                        PicLable.Summary.Add(tag, "");
                    }
                    if (PicLable.Summary[tag] == textbox.Text.Trim())
                    {
                        return;
                    }
                    if (textbox.Text.Trim() == "" & PicLable.Summary[tag] == null)
                    {
                        return;
                    }
                    PicLable.Summary[tag] = textbox.Text.Trim();
                    Save();
                    var items = new object[listBox1.Items.Count];
                    listBox1.Items.CopyTo(items, 0);
                    int sel = listBox1.SelectedIndex;
                    listBox1.Items.Clear();
                    listBox1.BeginUpdate();
                    listBox1.Items.AddRange(items);
                    listBox1.EndUpdate();
                    listBox1.SelectedIndex = sel;
                }
                e.SuppressKeyPress = true;
            }
        }

        private void 输出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new();
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string dirPath = folderBrowserDialog.SelectedPath;

            using Form2 form = new();
            form.progressBar1.Maximum = 100;
            Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(100);
                    double i = 0;
                    foreach (var item in Lables)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Value.Lable))
                        {
                            if (!Directory.Exists(Path.Combine(dirPath, item.Value.SymmariedLable)))
                            {
                                Directory.CreateDirectory(Path.Combine(dirPath, item.Value.SymmariedLable));
                            }
                            File.Copy(item.Key, Path.Combine(dirPath, item.Value.SymmariedLable, Path.GetFileName(item.Key)), true);
                        }
                        i++;
                        form.Invoke(() =>
                        {
                            form.progressBar1.Value = (int)(i / Lables.Count * 100);
                            form.label1.Text = $"{i / Lables.Count * 100:0.00}%";
                        });
                    }
                }
                catch (Exception e)
                {
                    Invoke(() =>
                    {
                        form.Close();
                        MessageBox.Show($"Export failure: {e.Message}");
                    });
                    return;
                }
                Invoke(() =>
                {
                    form.Close();
                    MessageBox.Show("Export success.");
                });
            });
            form.ShowDialog(this);
        }

        private void 关于AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Tile Classifier Ver. 0.0.0.1\nby Weili Jia");
        }

        public static int ZoomStep { get; set; } = 100;
        public string BetterLabel { get; private set; }
        public bool IsAutoShow { get; private set; }

        private static bool isMove = false;
        private static Point mouseDownPoint;

        public void MouseWheel(object? sender, MouseEventArgs e)
        {
            PictureBox pbox = sender as PictureBox;
            int x = e.Location.X;
            int y = e.Location.Y;
            int ow = pbox.Width;
            int oh = pbox.Height;
            int VX, VY;
            if (e.Delta > 0)
            {
                pbox.Width += ZoomStep;
                pbox.Height += ZoomStep;
                PropertyInfo pInfo = pbox.GetType().GetProperty("ImageRectangle", BindingFlags.Instance |
                 BindingFlags.NonPublic);
                Rectangle rect = (Rectangle)pInfo.GetValue(pbox, null);
                pbox.Width = rect.Width;
                pbox.Height = rect.Height;
            }
            if (e.Delta < 0)
            {
                if (pbox.Width < 300)
                    return;
                pbox.Width -= ZoomStep;
                pbox.Height -= ZoomStep;
                PropertyInfo pInfo = pbox.GetType().GetProperty("ImageRectangle", BindingFlags.Instance |
                 BindingFlags.NonPublic);
                Rectangle rect = (Rectangle)pInfo.GetValue(pbox, null);
                pbox.Width = rect.Width;
                pbox.Height = rect.Height;
            }
            VX = (int)((double)x * (ow - pbox.Width) / ow);
            VY = (int)((double)y * (oh - pbox.Height) / oh);
            pbox.Location = new Point(pbox.Location.X + VX, pbox.Location.Y + VY);

            pictureBox2.Width = pbox.Width;
            pictureBox2.Height = pbox.Height;

            if (pbox.Image == null)
            {
                return;
            }
            toolStripStatusLabel3.Text = (pbox.Width / (double)pbox.Image.Width * 100).ToString("0") + "%";
        }


        public static void MouseMove(object? sender, MouseEventArgs e)
        {
            PictureBox pbox = sender as PictureBox;
            pbox.Focus();
            if (isMove)
            {
                int x, y;
                int moveX, moveY;
                moveX = Cursor.Position.X - mouseDownPoint.X;
                moveY = Cursor.Position.Y - mouseDownPoint.Y;
                x = pbox.Location.X + moveX;
                y = pbox.Location.Y + moveY;
                pbox.Location = new Point(x, y);
                mouseDownPoint.X = Cursor.Position.X;
                mouseDownPoint.Y = Cursor.Position.Y;
            }
        }
        public static void MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMove = false;
            }
        }
        public static void MouseDown(object? sender, MouseEventArgs e)
        {
            PictureBox pbox = sender as PictureBox;
            if (e.Button == MouseButtons.Left)
            {
                mouseDownPoint.X = Cursor.Position.X; 
                mouseDownPoint.Y = Cursor.Position.Y;
                isMove = true;
                pbox.Focus();
            }
        }

        private void 帮助ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Loading a model allows you to load an already trained recognition model and use that model to assist your annotation.");
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not string & IsAutoShow)
            {
                IsAutoShow = false;
                e.SuppressKeyPress = true;
                listBox1.Focus();
                return;
            }
            if (IsPredicting == true)
            {
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Up)
            {
                if (listBox1.SelectedIndex - 1 != -1)
                {
                    listBox1.SelectedIndex--;
                }
                listBox1.Focus();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (listBox1.SelectedIndex + 1 < listBox1.Items.Count)
                {
                    listBox1.SelectedIndex++;
                }
                listBox1.Focus();
                e.SuppressKeyPress = true;
            }
            else
            {
                listBox1_KeyDown(sender, e);
                e.SuppressKeyPress = true;
            }
        }

        private void tILToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //pictureBox2.Image = Form1.ResourceManager.
            //Form4 form = new()
            //{
            //    TopMost = true
            //};
            //form.Show(this);
        }

        private void 安装模型ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dialog = new();
            dialog.Filter = "Identification model file(*.zip)|*.zip";
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            try
            {
                File.Copy(dialog.FileName, Path.Combine(new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName, "Model", dialog.SafeFileName), true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            MessageBox.Show("The program will restart automatically.");
            Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            Environment.Exit(0);
        }

        private void AutoShow()
        {
            int c = 0;
            while (IsAutoShow)
            {
                c++;
                if (c == 3 & DelayTime <= 300)
                {
                    c = 0;
                    Thread.Sleep(DelayTime);
                    if (!IsAutoShow)
                    {
                        break;
                    }
                }
                Thread.Sleep(DelayTime);
                if (!IsAutoShow)
                {
                    break;
                }
                Invoke(() =>
                {
                    Form1_KeyDown("Auto", new KeyEventArgs(Keys.Space));
                    //listBox1.SelectedIndex++;
                });
            }
        }

        int DelayTime = 500;

        private void 慢速ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Auto play is on, press any key to end");
            IsAutoShow = true;
            DelayTime = 1000;
            Task.Run(AutoShow);
        }

        private void 中速ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Auto play is on, press any key to end");
            IsAutoShow = true;
            DelayTime = 500;
            Task.Run(AutoShow);
        }

        private void 快速ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Auto play is on, press any key to end");
            IsAutoShow = true;
            DelayTime = 250;
            Task.Run(AutoShow);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (十字辅助线ToolStripMenuItem.Checked)
            {
                drawLine(e);
            }
            else if (网格辅助线ToolStripMenuItem.Checked)
            {
                drawGrids(e);
            }
            else if (对角辅助线ToolStripMenuItem.Checked)
            {
                drawX(e);
            }
            else if (复合辅助线ToolStripMenuItem.Checked)
            {
                drawComplex(e);
            }
        }

        private void drawComplex(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            drawGrids(e);
            //X
            g.DrawLine(corePen, new Point(0, 0), new Point(pictureBox1.Width, pictureBox1.Height));
            g.DrawLine(corePen, new Point(0, pictureBox1.Height), new Point(pictureBox1.Width, 0));
            //+
            int i = pictureBox1.Width / 2;
            int j = pictureBox1.Height / 2;
            g.DrawLine(corePen, new Point(i, 0), new Point(i, ClientRectangle.Bottom));
            g.DrawLine(corePen, new Point(0, j), new Point(ClientRectangle.Right, j));
        }

        Pen pen = new(new SolidBrush(Color.LightGreen), 2);
        Pen corePen = new(new SolidBrush(Color.LightGreen), 4);

        private void drawX(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.DrawLine(pen, new Point(0, 0), new Point(pictureBox1.Width, pictureBox1.Height));
            g.DrawLine(pen, new Point(0, pictureBox1.Height), new Point(pictureBox1.Width, 0));
        }


        private void drawLine(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            int i = pictureBox1.Width / 2;
            int j = pictureBox1.Height / 2;

            g.DrawLine(pen, new Point(i, 0), new Point(i, ClientRectangle.Bottom));
            g.DrawLine(pen, new Point(0, j), new Point(ClientRectangle.Right, j));
        }

        private void drawGrids(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            int i = pictureBox1.Width / 4;
            int j = pictureBox1.Height / 4;

            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    g.DrawLine(pen, new Point((x + 1) * i, 0), new Point((x + 1) * i, ClientRectangle.Bottom));
                    g.DrawLine(pen, new Point(0, j * (y + 1)), new Point(ClientRectangle.Right, j * (y + 1)));
                }
            }
        }

        private void 十字辅助线ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            十字辅助线ToolStripMenuItem.Checked = !十字辅助线ToolStripMenuItem.Checked;
            if (十字辅助线ToolStripMenuItem.Checked)
            {
                网格辅助线ToolStripMenuItem.Checked =
                复合辅助线ToolStripMenuItem.Checked =
                对角辅助线ToolStripMenuItem.Checked = false;
            }
            pictureBox1.Refresh();
        }

        private void 网格辅助线ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            网格辅助线ToolStripMenuItem.Checked = !网格辅助线ToolStripMenuItem.Checked;
            if (网格辅助线ToolStripMenuItem.Checked)
            {
                十字辅助线ToolStripMenuItem.Checked =
                复合辅助线ToolStripMenuItem.Checked =
                对角辅助线ToolStripMenuItem.Checked = false;
            }
            pictureBox1.Refresh();
        }

        private void 对角辅助线ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            对角辅助线ToolStripMenuItem.Checked = !对角辅助线ToolStripMenuItem.Checked;
            if (对角辅助线ToolStripMenuItem.Checked)
            {
                十字辅助线ToolStripMenuItem.Checked =
                复合辅助线ToolStripMenuItem.Checked =
                网格辅助线ToolStripMenuItem.Checked = false;
            }
            pictureBox1.Refresh();
        }

        private void 复合辅助线ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            复合辅助线ToolStripMenuItem.Checked = !复合辅助线ToolStripMenuItem.Checked;
            if (复合辅助线ToolStripMenuItem.Checked)
            {
                十字辅助线ToolStripMenuItem.Checked =
                对角辅助线ToolStripMenuItem.Checked =
                网格辅助线ToolStripMenuItem.Checked = false;
            }
            pictureBox1.Refresh();
        }

        private void label12_Click(object sender, EventArgs e)
        {
            Form1_KeyDown(this, new KeyEventArgs(Keys.Space));
        }
    }
}