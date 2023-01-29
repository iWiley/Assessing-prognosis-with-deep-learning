using OpenSlideSharp.BruTile;
using ShellProgressBar;
using System.Collections.Concurrent;
using System.CommandLine;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Wistu.Lib.ClassifyModel;
using Pbar = Konsole.ProgressBar;
namespace Deep_Learning_Training_Command_Line_Tool
{
    static class WinSleepCtr
    {
        [DllImport("kernel32.dll")]
        static extern uint SetThreadExecutionState(uint esFlags);
        const uint ES_SYSTEM_REQUIRED = 0x00000001;
        const uint ES_DISPLAY_REQUIRED = 0x00000002;
        const uint ES_CONTINUOUS = 0x80000000;
        public static void SleepCtr(bool sleepOrNot)
        {
            if (sleepOrNot)
            {
                SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
            }
            else
            {
                SetThreadExecutionState(ES_CONTINUOUS);
            }
        }
    }
    internal class Program
    {
        public static Bitmap MatchingImage => Resource.Image;
        static double[] matchCpR, matchCpG, matchCpB;
        static readonly string I = "Deep Learning Training Command Line Tool Ver. 0.0.0.1, 2022 (c) by Weili Jia\n";
        static int Main(string[] args)
        {
            var rootCommand = new RootCommand("Deep Learning Training Command Line Tool.");

            var oFile = new Option<string>(new[] { "--file", "-f" }, "Path of the file to be processed.");
            var oDir = new Option<string>(new[] { "--dir", "-d" }, "Path of the directory to be processed.");
            var oLevel = new Option<int>(new[] { "--level", "-l" }, () => 0, "Image layer to view.");
            var oBlock = new Option<int>(new[] { "--block", "-b" }, () => 2, "Size of tiles.");
            var oIn = new Option<string>(new[] { "--dir-in", "-di" }, "Input path.");
            var oOut = new Option<string>(new[] { "--dir-out", "-do" }, "Output path.");
            var oBalance = new Option<bool>(new[] { "--balance" }, () => false, "Enable color balance.");
            var oGray = new Option<bool>(new[] { "--grey" }, () => false, "Enable image decolorization.");
            var oGrayMode = new Option<int>(new[] { "--grey-mode" }, () => 0, "Image decolorization mode, 0: weighted average; 1: arithmetic average; default is weighted average.");
            var oCorrect = new Option<int>(new[] { "--correct-comparison", "-cc" }, () => 0, "Correction contrast, 0: automatic contrast, the software will automatically decide whether to contrast according to the directory level; 1: turn on contrast; 2: turn off contrast; default is automatic contrast.");
            var oModel = new Option<string>(new[] { "--model", "-m" }, "The model file to be used, by default, automatically uses the latest version.");
            var oTileSize = new Option<int>(new[] { "--tile-size" }, () => 50, "Output block size, default is 50.");
            var oFontSize = new Option<int>(new[] { "--font-size" }, () => 10, "Output font size, default is 10.");
            var oSaveAccuracy = new Option<bool>(new[] { "--save-accuracy", "-sa" }, () => false, "Whether it is kept by accuracy classification.");
            var oAccuracy = new Option<float>(new[] { "--accuracy" }, () => .9f, "The saved accuracy range, 0-1, defaults to 0.9, i.e. 90%. When the provided accuracy is 0, it will be saved in categories of 100-99%, 99-95%, 95-90%, 90-80%, 80-50% and <50%.");
            var oSave = new Option<float>(new[] { "--save-version", "-sv" }, "The version number of the model to be saved.");

            Command cut = new("cut", "Automatically cut the entire WSI file.")
            {
                oFile,
                oLevel,
                oBlock,
                oOut,
                oBalance,
                oGray,
                oGrayMode
            };
            cut.SetHandler(CutHandler, oFile, oLevel, oBlock, oOut, oBalance, oGray, oGrayMode);
            rootCommand.AddCommand(cut);

            Command view = new("view", "View the specified location in the WSI file.")
            {
                oFile,
                oLevel,
                oBlock,
                oOut
            };
            view.SetHandler(ViewHandler, oFile, oLevel, oBlock, oOut);
            rootCommand.AddCommand(view);

            Command info = new("info", "View WSI file information.")
            {
                oFile
            };
            info.SetHandler(InfoHandler, oFile);
            rootCommand.AddCommand(info);

            Command gray = new("grey", "Image graying.")
            {
                oDir,
                oGrayMode
            };
            gray.SetHandler(GrayHandler, oDir, oGrayMode);
            rootCommand.AddCommand(gray);

            Command verify = new("verify", "Validate the results against the model.")
            {
                oDir,
                oCorrect,
                oModel,
                oTileSize,
                oFontSize,
                oOut,
                oAccuracy,
                oSaveAccuracy
            };
            verify.SetHandler(VerifyHandler, oDir, oCorrect, oModel, oTileSize, oFontSize, oOut, oAccuracy, oSaveAccuracy);
            rootCommand.AddCommand(verify);

            Command copystruct = new("cpstruct", "Copy the structure of the input folder according to the specified directory and save it to the output folder.")
            {
                oDir,
                oIn,
                oOut
            };
            copystruct.SetHandler(CopyStruct, oDir, oIn, oOut);
            rootCommand.AddCommand(copystruct);

            Command train = new("train", "Start training.")
            {
                oDir,
                oSave
            };
            train.SetHandler(Train, oDir, oSave);
            rootCommand.AddCommand(train);

            Command model = new("models", "Show all models.");
            model.SetHandler(() =>
            {
                Console.WriteLine("All models installed:");
                Console.WriteLine(string.Join('\n', Directory.GetFiles(Path.Combine(CurrentPath, "Model"), "*.zip")));
            });
            rootCommand.AddCommand(model);

            Command link = new("link", "Create hard links for files in the specified directory structure.")
            {
                oIn,
                oOut
            };
            link.SetHandler(LinkHander, oIn, oOut);
            rootCommand.AddCommand(link);

            Command score = new("score", "Calculates the immune species score for the slice in the specified slice catalog.")
            {
                oDir,
                oLevel,
                oBlock
            };
            score.SetHandler(ScoreHandler, oDir, oLevel, oBlock);
            rootCommand.AddCommand(score);

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            WinSleepCtr.SleepCtr(true);


            return rootCommand.InvokeAsync(args).Result;
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            WinSleepCtr.SleepCtr(false);
        }

        private static void ScoreHandler(string dir, int level, int block)
        {
            Init();
            Console.WriteLine("Start calculating the slice immunization type ...");
            if (!Directory.Exists(dir))
            {
                Console.WriteLine("Target directory does not exist.");
                return;
            }
            string[] files = Directory.GetFiles(dir, "*.svs");

            string sp = Path.Combine(dir, "score.csv");
            if (!File.Exists(sp))
            {
                File.WriteAllText(sp, $"File,{string.Join(',', Classifier.Labels)},\n");
            }
            else
            {
                Dictionary<string, string> fs = new Dictionary<string, string>();
                foreach (var item in files)
                {
                    fs.Add(Path.GetFileName(item), item);
                }
                foreach (var item in File.ReadAllLines(sp))
                {
                    string[] sps = item.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (fs.ContainsKey(sps[0]))
                    {
                        fs.Remove(sps[0]);
                        Console.WriteLine($"Skip file: {sps[0]}。");
                    }
                }
                files = fs.Values.ToArray();
            }

            ConcurrentBag<string> cfiles = new(files);
            var pbar = new Pbar(Konsole.PbStyle.DoubleLine, files.Length);
            int rc = 1;

            void task()
            {
                while (cfiles.Any())
                {
                    int i;
                    string f;
                    lock (cfiles)
                    {
                        i = files.Length - cfiles.Count;
                        cfiles.TryTake(out f);
                    }
                    pbar.Refresh(i, "");
                    ScoreFile(f, i, pbar, level, block, sp);
                }
            }

            Task[] tasks = new Task[rc];

            for (int i = 0; i < rc; i++)
            {
                tasks[i] = new Task(task);
                tasks[i].Start();
            }

            Task.WaitAll(tasks);
        }

        static readonly object SlideLocker = new();
        static readonly object PredictorLocker = new();

        private static void ScoreFile(string file, int index, Pbar pbar, int level, int block, string resultPath)
        {
            pbar.Refresh(index, $"Processing: {Path.GetFileName(file)}");

            int current = 0;

            ISlideSource slide;
            BruTile.ITileSchema schema;
            IEnumerable<BruTile.TileInfo> infos;

            slide = SlideSourceBase.Create(file);
            schema = slide.Schema;
            infos = schema.GetTileInfos(schema.Extent, level);

            int total = infos.Count();

            Dictionary<string, int> scores = new();
            foreach (var item in Classifier.Labels)
            {
                scores.Add(item, 0);
            }

            int superX = (int)schema.GetMatrixWidth(level) / block;
            int superY = (int)schema.GetMatrixHeight(level) / block;


            for (int x = 0; x < superX; x++)
            {
                for (int y = 0; y < superY; y++)
                {
                    var pts = GetSuperTilesIndex(x, y, (int)schema.GetMatrixHeight(level), block);
                    int n = 0;
                    try
                    {
                        Image image = new Bitmap(schema.GetTileWidth(level) * block, schema.GetTileHeight(level) * block);
                        Graphics g = Graphics.FromImage(image);

                        foreach (var item in pts)
                        {
                            var ti = infos.ElementAt(item);
                            var t = slide.GetTile(ti);
                            using MemoryStream stream = new(t);
                            using Image i = Image.FromStream(stream);
                            int ix = n / block;
                            int iy = n % block;
                            g.DrawImage(i, ix * schema.GetTileWidth(level), iy * schema.GetTileHeight(level));
                            n++;
                            current++;
                        }
                        g.Flush();

                        MemoryStream ms = new();
                        image.Save(ms, ImageFormat.Jpeg);
                        image.Dispose();
                        g.Dispose();

                        var re = Classifier.Predict(new ModelInput { Image = ms.ToArray() });
                        scores[re.BetterLable]++;

                        pbar.Refresh(index, $"Processing: {Path.GetFileName(file)}，{current}/{total},{string.Join(',', scores.Values)}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                }
            }

            File.AppendAllText(resultPath, $"{Path.GetFileName(file)},{string.Join(',', scores.Values)}\n");
        }

        static string CurrentPath => Path.GetDirectoryName(Environment.ProcessPath);

        private static DateTime predictTime;
        public static void SetPredictTime()
        {
            IsPausePredict = true;
            LastProgress = 0;
            CurrentProgress = 0;
            Thread.Sleep(600);
            IsPausePredict = false;
            Task.Run(() =>
            {
                while (!IsPausePredict)
                {
                    if (LastProgress == CurrentProgress & CurrentProgress == 0)
                    {
                        PredictTime = null;
                    }
                    else if (LastProgress == CurrentProgress)
                    {
                        PredictTime = TimeSpan.Zero;
                    }
                    else
                    {
                        var p = (TotalProgress - CurrentProgress) / (CurrentProgress - LastProgress) / 2;
                        PredictTime = TimeSpan.FromSeconds(p);
                    }
                    LastProgress = CurrentProgress;
                    Thread.Sleep(500);
                }
            });
            predictTime = DateTime.Now;
        }

        public static bool IsPausePredict = false;
        public static double LastProgress;
        public static double CurrentProgress;
        public static double TotalProgress;
        public static TimeSpan? PredictTime { get; private set; }

        public static string GetPredictTime(double current, double total)
        {
            TotalProgress = total;
            CurrentProgress = current;
            return PredictTime == null
                ? " ETA：#.##:##:##"
                : @$" ETA：{PredictTime:hh\:mm\:ss}";
        }

        static void Init()
        {
            Console.WriteLine("Tip: Initialize the TensorFlow environment...");
            Classifier.Init();
            Console.Clear();
            Console.WriteLine("Tip: Finish initializing the TensorFlow environment.");
        }

        private static void LinkHander(string iDir, string oDir)
        {
            Console.WriteLine($"{I} Start creating hard links for the files in the specified directory structure.");
            if (!Directory.Exists(iDir))
            {
                Console.WriteLine("The specified directory does not exist.");
                return;
            }
            iDir = Path.GetFullPath(iDir);
            if (!Directory.Exists(oDir))
            {
                Directory.CreateDirectory(oDir);
                Console.WriteLine($"The output directory {oDir} does not exist, but was created.");
            }
            oDir = Path.GetFullPath(oDir);

            Console.WriteLine($"Input directory: {iDir}");
            Console.WriteLine($"Output directory: {oDir}");

            IEnumerable<string> getSunDirFiles(string subDir)
            {
                List<string> files = new();
                foreach (var item in Directory.GetDirectories(subDir))
                    files.AddRange(getSunDirFiles(item));
                files.AddRange(Directory.GetFiles(subDir, "*.jpg"));
                return files;
            }
            int total = 0;

            Dictionary<string, IEnumerable<string>> allFiles = new();
            foreach (var item in Directory.GetDirectories(iDir))
            {
                allFiles.Add(Path.GetFileName(item), getSunDirFiles(item));
                total += allFiles[Path.GetFileName(item)].Count();
            }
            allFiles.Add("", Directory.GetFiles(iDir, "*.jpg"));
            total += allFiles[""].Count();

            Console.WriteLine($"Get the target directory structure finished with {total} files.");
            Console.WriteLine("Start creating hard links...");

            SetPredictTime();

            var pb = new Pbar(Konsole.PbStyle.DoubleLine, total);
            foreach (var item in allFiles)
            {
                foreach (var f in item.Value)
                {
                    try
                    {
                        pb.Refresh(pb.Current + 1, $"Processing: {f} {GetPredictTime(pb.Current + 1, pb.Max)}");
                        string ff = Path.GetFileName(f);
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(oDir, item.Key, ff)));

                        string target = Path.Combine(oDir, item.Key, ff);
                        string source = Path.Combine(iDir, item.Key, ff);
                        target = Path.Combine(Path.GetDirectoryName(target), pb.Current + ".jpg");

                        CreateHardLink(target, source, IntPtr.Zero);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            Console.WriteLine("Operation completed.");
        }

        [DllImport("Kernel32", CharSet = CharSet.Unicode)]
        public extern static bool CreateHardLink(string linkName, string sourceName, IntPtr attribute);

        private static void Train(string dir, float save)
        {
            Init();
            Console.WriteLine($"{I} Start training the model...");

            var sp = Path.Combine(CurrentPath, "Model", $"TLSModel.Ver{save:0.0}.zip");

            if (!Path.IsPathRooted(dir))
            {
                dir = Path.GetFullPath(dir);
            }
            Console.WriteLine($"Target directory: {dir}");
            Console.WriteLine($"Save path: {sp}");
            Trainner.Start(dir, sp);
        }

        private static void CopyStruct(string dir, string idir, string odir)
        {
            Console.WriteLine($"{I} Start copying the structure.");
            dir = dir?.Trim('\"');
            idir = idir?.Trim('\"');
            odir = odir?.Trim('\"');
            if (!Directory.Exists(dir))
            {
                Console.WriteLine($"Destination directory {dir} does not exist.");
                return;
            }
            Console.WriteLine($"Target directory: {dir}");
            if (!Directory.Exists(idir))
            {
                Console.WriteLine($"Input directory {idir} does not exist.");
                return;
            }
            Console.WriteLine($"Input directory: {idir}");
            if (!Directory.Exists(odir))
            {
                Directory.CreateDirectory(odir);
                Console.WriteLine($"The output directory {odir} does not exist, but has been created.");
            }
            Console.WriteLine($"Output directory: {odir}");
            Console.WriteLine("Note: The names of the files in the target directory cannot be duplicated, otherwise an error will be reported.");
            Console.WriteLine("Get the target directory structure...");
            IEnumerable<string> getSunDirFiles(string subDir)
            {
                List<string> files = new();
                foreach (var item in Directory.GetDirectories(subDir))
                    files.AddRange(getSunDirFiles(item));
                files.AddRange(Directory.GetFiles(subDir, "*.jpg"));
                return files;
            }
            List<string> allFiles = new();
            foreach (var item in Directory.GetDirectories(dir))
            {
                allFiles.AddRange(getSunDirFiles(item));
            }
            allFiles.AddRange(Directory.GetFiles(dir, "*.jpg"));

            Console.WriteLine($"Get the target directory structure finished, total {allFiles.Count} files.");
            Console.WriteLine("Generate reference tree, if error is reported, please check if there is duplicate file name...");

            Dictionary<string, string> tree = new Dictionary<string, string>();
            foreach (var item in allFiles)
            {
                tree.Add(Path.GetFileName(item), item.Replace(dir, ""));
            }
            Console.WriteLine("The generation of the reference tree is finished.");
            Console.WriteLine("Start copying files...");
            foreach (var item in tree)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(odir + item.Value));
                    File.Copy(Path.Combine(idir, item.Key), odir + item.Value, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            Console.WriteLine("Operation completed.");
        }

        private static void VerifyHandler(string dir, int correct, string? model, int tileSize, int fontSize, string oDir, float accuracy, bool saveAccuracy)
        {
            Init();
            Console.WriteLine($"{I}Start validating the model.");
            if (!Directory.Exists(dir))
            {
                Console.WriteLine("Target directory does not exist.");
                return;
            }
            if (model != null & !File.Exists(model))
            {
                Console.WriteLine("Target model does not exist.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(oDir) & !Directory.Exists(oDir))
            {
                Directory.CreateDirectory(oDir);
                Console.WriteLine("Save directory does not exist, but has been created.");
            }

            if (Directory.Exists(oDir))
            {
                saveAccuracy = true;
            }

            if (model != null)
                Classifier.ModelName = model;
            Console.WriteLine($"Model used: [{Classifier.ModelName}]");
            Console.WriteLine($"Tags in the model: {string.Join(",", Classifier.Labels)}");
            if (accuracy == 0f)
            {
                Console.WriteLine($"Tip: Save by accuracy is on: will be graded and saved.");
            }
            else
            {
                Console.WriteLine($"Hint: Save by Accuracy has been {(saveAccuracy ? $"On, target accuracy: {accuracy * 100f:00.0}%" : "Off")}");
            }

            string[] subDir = Directory.GetDirectories(dir);
            Dictionary<string, (Result, string)> results = new();

            bool isVerifySubDir = true;
            int containNum = 0;
            if (subDir.Length == Classifier.Labels.Length)
            {
                foreach (var item in subDir)
                {
                    if (!Classifier.Labels.Contains(Path.GetFileName(item)))
                    {
                        isVerifySubDir = false;
                        break;
                    }
                    else
                    {
                        containNum++;
                    }
                }
            }
            if (subDir.Length == 0 | containNum != Classifier.Labels.Length)
            {
                isVerifySubDir = false;
            }

            if (!isVerifySubDir)
            {
                Console.WriteLine("Ignored subdirectories: subdirectories in the target directory are not consistent with the model tag.");
                string[] files = Directory.GetFiles(dir, "*.jpg");
                string c = "";
                int[] counts = new int[Classifier.Labels.Length];
                int[] accuCounts = new int[Classifier.Labels.Length];

                for (int i = 0; i < Classifier.Labels.Length; i++)
                {
                    c += Classifier.Labels[i] + ":" + counts[i] + ",";
                }
                var pbar = new Pbar(Konsole.PbStyle.DoubleLine, files.Length);
                SetPredictTime();

                foreach (var item in files)
                {
                    var re = Classifier.Predict(new ModelInput { Image = File.ReadAllBytes(item) });
                    results.Add(item, (re, item));
                    int ind = Classifier.Labels.AsSpan().IndexOf(re.BetterLable);
                    counts[ind]++;
                    if (saveAccuracy & re.Scores[ind] >= accuracy)
                    {
                        accuCounts[ind]++;
                    }
                    c = "";
                    for (int i = 0; i < Classifier.Labels.Length; i++)
                    {
                        c += $"{Classifier.Labels[i]}:{counts[i]}{(saveAccuracy ? $"(a:{accuCounts[i]})" : "")},";
                    }

                    if (saveAccuracy)
                    {
                        int v = Classifier.Labels.AsSpan().IndexOf(re.BetterLable);

                        if (re.Scores[v] >= accuracy & Classifier.Labels[re.Scores.AsSpan().IndexOf(re.Scores.Max())] == re.BetterLable)
                        {
                            Directory.CreateDirectory(Path.Combine(oDir, Classifier.Labels[v]));
                            CreateHardLink(Path.Combine(oDir, Classifier.Labels[v], results.Count + ".jpg"), item, IntPtr.Zero);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.Combine(oDir, "#Others#"));
                            Directory.CreateDirectory(Path.Combine(oDir, "#Others#", Classifier.Labels[v]));
                            CreateHardLink(Path.Combine(oDir, "#Others#", Classifier.Labels[v], results.Count + ".jpg"), item, IntPtr.Zero);
                        }
                    }
                    pbar.Refresh(results.Count, c.Trim(',') + GetPredictTime(results.Count, files.Length));
                }
                Console.WriteLine($"Prediction Success.");
                Console.WriteLine(c);
            }
            else
            {
                Console.WriteLine("Recognized subcategories: Classification accuracy will be calculated automatically.");
                var pbar = new Pbar(Konsole.PbStyle.DoubleLine, subDir.Length);
                int total = 0;

                foreach (var item in subDir)
                {
                    total += Directory.GetFiles(item, "*.jpg").Length;
                }

                List<string> message = new();
                SetPredictTime();

                Dictionary<string, Result> res = new Dictionary<string, Result>();
                foreach (var item in results)
                {
                    res.Add(item.Key, item.Value.Item1);
                }

                foreach (var item in subDir)
                {
                    pbar.Refresh(pbar.Current + 1, "Classification calculation in progress, please wait...");
                    message.Add(VerifySubDir(item, Path.GetFileName(item), res, saveAccuracy ? oDir : null, accuracy, total));
                }
                if (saveAccuracy)
                {
                    Console.WriteLine($"The prediction is successful, the result has been output to {oDir}, the accuracy calculation does not output the result graph.");
                }
                else
                {
                    Console.WriteLine($"Prediction success, not turned on save results, accuracy calculation does not output results graph.");
                }
                for (int i = 0; i < subDir.Length; i++)
                {
                    subDir[i] = Path.GetFileName(subDir[i]);
                }
                for (int i = 0; i < subDir.Length; i++)
                {
                    Console.WriteLine($"{subDir[i]}：{message[i]}");
                }
                return;
            }

            Dictionary<Point, Result> points = new();
            Dictionary<Point, string> fs = new();
            int x = 0, y = 0;
            foreach (var item in results)
            {
                try
                {
                    var p = ClassifyHelper.FileName2Point(item.Key);
                    points.Add(p, item.Value.Item1);
                    fs.Add(p, item.Value.Item2);
                    if (x < p.X)
                    {
                        x = p.X;
                    }
                    if (y < p.Y)
                    {
                        y = p.Y;
                    }
                }
                catch
                {
                    Console.WriteLine($"Identify the coordinates failure: {item.Key}, directory: {dir}");
                }
            }

            var imagedata = new Bitmap(tileSize * (x + 1), tileSize * (y + 1));
            var g = Graphics.FromImage(imagedata); 
            var orginalata = new Bitmap(tileSize * (x + 1), tileSize * (y + 1));
            var go = Graphics.FromImage(orginalata);

            Brush[] brushes = new Brush[6];
            brushes[0] = new SolidBrush(Color.White);
            brushes[1] = new SolidBrush(Color.Green);
            brushes[2] = new SolidBrush(Color.Gray);
            brushes[3] = new SolidBrush(Color.Yellow);
            brushes[4] = new SolidBrush(Color.Red);
            brushes[5] = new SolidBrush(Color.FromArgb(255, 145, 0));

            Pen[] pens = new Pen[6];
            for (int i = 0; i < pens.Length; i++)
            {
                pens[i] = new Pen(brushes[i]);
            }

            var fbrush = new SolidBrush(Color.Black);
            string[] ls = new[] { "blank", "tumor", "edge", "lowTIL", "TLS", "highTIL" };

            if (Classifier.Labels.Contains("0.blank"))
            {
                List<string> lbs = new();
                List<SolidBrush> sbs = new();
                foreach (var item in Classifier.Labels)
                {
                    switch (item)
                    {
                        case "0.blank":
                            lbs.Add("blank");
                            sbs.Add(new SolidBrush(Color.White));
                            break;
                        case "1.edge":
                            lbs.Add("edge");
                            sbs.Add(new SolidBrush(Color.Gray));
                            break;
                        case "2.tumor":
                            lbs.Add("tumor");
                            sbs.Add(new SolidBrush(Color.Green));
                            break;
                        case "3.lowTIL":
                            lbs.Add("lowTIL");
                            sbs.Add(new SolidBrush(Color.Yellow));
                            break;
                        case "4.highTIL":
                            lbs.Add("highTIL");
                            sbs.Add(new SolidBrush(Color.Orange));
                            break;
                        case "5.TLS":
                            lbs.Add("TLS");
                            sbs.Add(new SolidBrush(Color.Red));
                            break;
                    }
                }
                brushes = sbs.ToArray();
                ls = lbs.ToArray();
            }

            if (Classifier.Labels.Contains("1.blank"))
            {
                List<string> lbs = new();
                List<SolidBrush> sbs = new();
                foreach (var item in Classifier.Labels)
                {
                    switch (item)
                    {
                        case "1.blank":
                            lbs.Add("blank");
                            sbs.Add(new SolidBrush(Color.White));
                            break;
                        case "2.edge":
                            lbs.Add("edge");
                            sbs.Add(new SolidBrush(Color.Gray));
                            break;
                        case "3.tumor":
                            lbs.Add("tumor");
                            sbs.Add(new SolidBrush(Color.Green));
                            break;
                        case "4.TIL":
                            lbs.Add("TIL");
                            sbs.Add(new SolidBrush(Color.Yellow));
                            break;
                        case "5.TLS":
                            lbs.Add("TLS");
                            sbs.Add(new SolidBrush(Color.Red));
                            break;
                    }
                }
                brushes = sbs.ToArray();
                ls = lbs.ToArray();
            }

            if (Classifier.Labels.Contains("1.invalid"))
            {
                List<string> lbs = new();
                List<SolidBrush> sbs = new();
                foreach (var item in Classifier.Labels)
                {
                    switch (item)
                    {
                        case "1.invalid":
                            lbs.Add("invalid");
                            sbs.Add(new SolidBrush(Color.FromArgb(217, 217, 217)));
                            break;
                        case "2.tumor":
                            lbs.Add("tumor");
                            sbs.Add(new SolidBrush(Color.FromArgb(0, 186, 56)));
                            break;
                        case "3.TIL":
                            lbs.Add("TIL");
                            sbs.Add(new SolidBrush(Color.FromArgb(97, 156, 255)));
                            break;
                        case "4.TLS":
                            lbs.Add("TLS");
                            sbs.Add(new SolidBrush(Color.FromArgb(248, 118, 109)));
                            break;
                    }
                }
                brushes = sbs.ToArray();
                ls = lbs.ToArray();
            }

            foreach (var item in points)
            {
                int v = Classifier.Labels.AsSpan().IndexOf(item.Value.BetterLable);
                if (saveAccuracy)
                {
                    //if (item.Value.Scores[v] >= accuracy & Classifier.Labels[item.Value.Scores.AsSpan().IndexOf(item.Value.Scores.Max())] == item.Value.BetterLable)
                    //{
                    //    Directory.CreateDirectory(Path.Combine(oDir, ls[v]));
                    //    File.Copy(Path.Combine(dir, $"{item.Key.Y}-{item.Key.X}.jpg"), Path.Combine(oDir, ls[v], $"{item.Key.Y}-{item.Key.X}.jpg"), true);
                    //}
                    //else
                    //{
                    //    Directory.CreateDirectory(Path.Combine(oDir, "#Others#"));
                    //    Directory.CreateDirectory(Path.Combine(oDir, "#Others#", ls[v]));
                    //    File.Copy(Path.Combine(dir, $"{item.Key.Y}-{item.Key.X}.jpg"), Path.Combine(oDir, "#Others#", ls[v], $"{item.Key.Y}-{item.Key.X}.jpg"), true);
                    //}
                }
                else
                {
                    string f = fs[item.Key];
                    Image s = Image.FromFile(f);
                    go.DrawImage(s, new Rectangle(x: item.Key.X * tileSize, y: item.Key.Y * tileSize, tileSize, tileSize));
                    s.Dispose();
                    g.FillRectangle(brushes[v], new Rectangle(x: item.Key.X * tileSize, y: item.Key.Y * tileSize, tileSize, tileSize));
                    //g.DrawString(ls[v], new Font("Arial", fontSize), fbrush, new PointF(x: item.Key.X * tileSize, y: item.Key.Y * tileSize));
                }
            }

            if (saveAccuracy)
            {
                Console.WriteLine($"The classification saves the accuracy without outputting the result picture, the accuracy result has been saved to{oDir}");
            }
            else
            {
                orginalata.Save(Path.Combine(dir, "0.Orginal.png"), ImageFormat.Png);
                imagedata.Save(Path.Combine(dir, "0.Result.png"), ImageFormat.Png);
                Console.WriteLine($"The results graph has been saved to{Path.Combine(dir, "0.Result.png")}");
            }
        }

        private static string VerifySubDir(string dir, string type, Dictionary<string, Result> result, string? oDir, float accuracy, int total)
        {
            string[] files = Directory.GetFiles(dir, "*.jpg");
            if (oDir != null)
            {
                if (accuracy == 0f)
                {
                    Directory.CreateDirectory(Path.Combine(oDir, Path.GetFileName(dir), "100-99"));
                    Directory.CreateDirectory(Path.Combine(oDir, Path.GetFileName(dir), "99-95"));
                    Directory.CreateDirectory(Path.Combine(oDir, Path.GetFileName(dir), "95-90"));
                    Directory.CreateDirectory(Path.Combine(oDir, Path.GetFileName(dir), "90-80"));
                    Directory.CreateDirectory(Path.Combine(oDir, Path.GetFileName(dir), "80-50"));
                    Directory.CreateDirectory(Path.Combine(oDir, Path.GetFileName(dir), "50-0"));
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(oDir, Path.GetFileName(dir), "Acc"));
                    Directory.CreateDirectory(Path.Combine(oDir, Path.GetFileName(dir), "In-Acc"));
                }
            }
            var pbar = new Pbar(Konsole.PbStyle.DoubleLine, files.Length);
            int correct = 0;
            int current = 0;
            foreach (var item in files)
            {
                var re = Classifier.Predict(new ModelInput { Image = File.ReadAllBytes(item) });
                result.Add(item, re);
                current++;
                if (re.BetterLable == type)
                    correct++;
                if (oDir != null)
                {
                    float score = re.Scores[Classifier.Labels.AsSpan().IndexOf(Path.GetFileName(dir))];
                    string fileName = result.Count + ".jpg";
                    string full = Path.GetFullPath(item);

                    if (accuracy == 0f)
                    {
                        if (score >= .99f)
                        {
                            CreateHardLink(Path.GetFullPath(Path.Combine(oDir, Path.GetFileName(dir), "100-99", fileName)), full, IntPtr.Zero);
                        }
                        else if (score < .99f & score >= .95f)
                        {
                            CreateHardLink(Path.GetFullPath(Path.Combine(oDir, Path.GetFileName(dir), "99-95", re.BetterLable + (score * 100).ToString("000") + fileName)), full, IntPtr.Zero);
                        }
                        else if (score < .95f & score >= .9f)
                        {
                            CreateHardLink(Path.GetFullPath(Path.Combine(oDir, Path.GetFileName(dir), "95-90", re.BetterLable + (score * 100).ToString("000") + fileName)), full, IntPtr.Zero);
                        }
                        else if (score < .9f & score >= .8f)
                        {
                            CreateHardLink(Path.GetFullPath(Path.Combine(oDir, Path.GetFileName(dir), "90-80", re.BetterLable + (score * 100).ToString("000") + fileName)), full, IntPtr.Zero);
                        }
                        else if (score < .8f & score >= .5f)
                        {
                            CreateHardLink(Path.GetFullPath(Path.Combine(oDir, Path.GetFileName(dir), "80-50", re.BetterLable + (score * 100).ToString("000") + fileName)), full, IntPtr.Zero);
                        }
                        else
                        {
                            CreateHardLink(Path.GetFullPath(Path.Combine(oDir, Path.GetFileName(dir), "50-0", re.BetterLable + (score * 100).ToString("000") + fileName)), full, IntPtr.Zero);
                        }
                    }
                    else
                    {
                        if (re.Scores[Classifier.Labels.AsSpan().IndexOf(Path.GetFileName(dir))] >= accuracy)
                        {
                            File.Copy(item, Path.Combine(oDir, Path.GetFileName(dir), "Acc", Path.GetFileName(item)));
                        }
                        else
                        {
                            File.Copy(item, Path.Combine(oDir, Path.GetFileName(dir), "In-Acc", $"{Path.GetFileNameWithoutExtension(item)}#{re.BetterLable}{Path.GetExtension(item)}"));
                        }
                    }
                }
                pbar.Refresh(current, $"  Verify the subdirectory {Path.GetFileName(dir)}... {correct}/{files.Length}(correct/total) accuracy.{(double)correct / current * 100:0.00}%  {GetPredictTime(result.Count, total)}");
            }
            pbar.Refresh(current, $"{Path.GetFileName(dir)}：{(double)correct / files.Length * 100:0.00}%({correct}/{files.Length})");
            return $"{(double)correct / files.Length * 100:0.00}% ({correct}/{files.Length})";
        }

        private static void GrayHandler(string dir, int grayMode)
        {
            Console.WriteLine($"{I}Start image decolorization.");

            if (!Directory.Exists(dir))
            {
                Console.WriteLine("Error: Directory does not exist.");
                return;
            }
            Console.WriteLine($"Hint: Image decolorization mode: {(grayMode == 0 ? "weighted" : "arithmetic")} average.");

            string[] files = Directory.GetFiles(dir, "*.jpg");

            var options = new ProgressBarOptions
            {
                ProgressCharacter = '=',
                DisableBottomPercentage = true,
                ProgressBarOnBottom = true,
                EnableTaskBarProgress = true
            };
            using var pbar = new ProgressBar(files.Length, "Processing...0.00%", options);
            int count = 0;
            foreach (var item in files)
            {
                count++;
                pbar.Tick(count, $"Processing...{((double)count / files.Length * 100):0.00}% ({count}/{files.Length})");
                using FileStream fileStream = new(item, FileMode.Open, FileAccess.ReadWrite);
                Bitmap bitmap = new(fileStream);
                var nbit = ToGray(bitmap, grayMode);
                fileStream.Position = 0;
                fileStream.SetLength(0);
                nbit.Save(fileStream, ImageFormat.Jpeg);
                nbit.Dispose();
                bitmap.Dispose();
            }
            Console.WriteLine($"Success, the file has been output to: {dir}。");
        }

        private static void InfoHandler(string file)
        {
            Console.WriteLine($"{I}");
            if (!File.Exists(file))
            {
                Console.WriteLine("Error: File does not exist!");
                return;
            }
            ISlideSource slide = SlideSourceBase.Create(file);

            var schema = slide.Schema;

            Console.WriteLine($@"
Name: {schema.Name}
Format: {schema.Format}
-------------------------------------
Embedded layers.
-------------------------------------");
            foreach (var item in schema.Resolutions)
            {
                Console.WriteLine($@"Level：{item.Key}
MatrixWidth：{item.Value.MatrixWidth}
TileWidth：{item.Value.TileWidth}
Top：{item.Value.Top}
Left：{item.Value.Left}
ScaleDenominator：{item.Value.ScaleDenominator}
UnitsPerPixel：{item.Value.UnitsPerPixel}
TotalTileCount：{schema.GetTileInfos(schema.Extent, item.Key).Count()}
-------------------------------------");
            }
        }

        private static void CutHandler(string file, int level, int block, string outD, bool balance, bool gray, int grayMode)
        {
            Console.WriteLine($"{I}Start cutting files...");
            if (!File.Exists(file))
            {
                Console.WriteLine("Error: File does not exist!");
                return;
            }
            if (File.Exists(outD))
            {
                Console.WriteLine("Error: wrong output path!");
                return;
            }
            Directory.CreateDirectory(outD);

            if (balance)
            {
                InitBlance();
            }
            Console.WriteLine($"Tip: Color balance has been {(balance ? "Enabled, this feature is for preliminary testing, do not use in research!" : "Disable.")}");
            Console.WriteLine($"Tip: Image de-colorization has been {(gray ? ($"enable; mode: {(grayMode == 0 ? "weighted" : "arithmetic")} average.") : "Disable.")}");
            Console.WriteLine($"Start splitting the file: {Path.GetFileName(file)}...");

            bool isRunning = true;

            int current = 0;
            int total = 0;
            ISlideSource slide = SlideSourceBase.Create(file);
            var schema = slide.Schema;
            var infos = schema.GetTileInfos(schema.Extent, level);
            total = infos.Count();
            Task.Run(() =>
            {
                int superX = (int)schema.GetMatrixWidth(level) / block;
                int superY = (int)schema.GetMatrixHeight(level) / block;


                for (int x = 0; x < superX; x++)
                {
                    for (int y = 0; y < superY; y++)
                    {
                        Image image = new Bitmap(schema.GetTileWidth(level) * block, schema.GetTileHeight(level) * block);
                        using Graphics g = Graphics.FromImage(image);

                        var pts = GetSuperTilesIndex(x, y, (int)schema.GetMatrixHeight(level), block);
                        int n = 0;
                        foreach (var item in pts)
                        {
                            var ti = infos.ElementAt(item);
                            var t = slide.GetTile(ti);
                            using MemoryStream stream = new(t);
                            using Image i = Image.FromStream(stream);
                            int ix = n / block;
                            int iy = n % block;
                            g.DrawImage(i, ix * schema.GetTileWidth(level), iy * schema.GetTileHeight(level));
                            n++;
                            current++;
                        }
                        g.Flush();
                        if (balance)
                        {
                            if (HistogramMatching(image, out Bitmap dst))
                            {
                                image.Clone();
                                image.Dispose();
                                image = dst;
                            }
                        }
                        if (gray)
                        {
                            var nimage = ToGray(new Bitmap(image), 1);
                            image.Clone();
                            image.Dispose();
                            image = nimage;
                        }
                        image.Save(Path.Combine(outD, $"{y}-{x}.jpg"), ImageFormat.Jpeg);
                        //image.Clone();
                        image.Dispose();
                    }
                }
                isRunning = false;
            });

            const int totalTicks = 100;
            var options = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Green,
                BackgroundColor = ConsoleColor.DarkGreen,
                ProgressCharacter = '=',
                DisableBottomPercentage = true,
                ProgressBarOnBottom = true,
                EnableTaskBarProgress = true
            };
            using var pbar = new ProgressBar(totalTicks, "Processing...", options);
            var prec = (double)current / total;
            prec *= 100;
            pbar.Tick((int)prec, $"Processing...{prec:0.00}% ({current}/{total})");
            do
            {
                Thread.Sleep(100);
                prec = (double)current / total;
                prec *= 100;
                pbar.Tick((int)prec, $"Processing...{prec:0.00}% ({current}/{total})");
            } while (isRunning);
            pbar.Tick(100, $"100% ({total}/{total})");
            pbar.Dispose();
            Console.WriteLine($"Success, the file has been output to: {outD}。");
        }

        private static void ViewHandler(string file, int level, int block, string outD)
        {
            Console.WriteLine($"Sorry, that part is not finished yet.");
        }

        static int[] GetSuperTilesIndex(int x, int y, int mHeight, int superTileSize)
        {
            x *= superTileSize;
            y *= superTileSize;
            var tiles = new List<int>();
            for (int ix = 0; ix < superTileSize; ix++)
            {
                for (int iy = 0; iy < superTileSize; iy++)
                {
                    tiles.Add((x + ix) * mHeight + (y + iy));
                }
            }
            return tiles.ToArray();
        }

        public static void InitBlance()
        {
            Bitmap tempMatchingBmp = new Bitmap(MatchingImage);
            getCumulativeProbabilityRGB(tempMatchingBmp, out matchCpR, out matchCpG, out matchCpB);
        }

        private static Bitmap ToGray(Bitmap bmp, int mode)
        {
            if (bmp == null)
            {
                return null;
            }

            int w = bmp.Width;
            int h = bmp.Height;
            try
            {
                byte newColor = 0;
                BitmapData srcData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                unsafe
                {
                    byte* p = (byte*)srcData.Scan0.ToPointer();
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {

                            if (mode == 0)
                            {
                                newColor = (byte)(p[0] * 0.114f + p[1] * 0.587f + p[2] * 0.299f);
                            }
                            else
                            {
                                newColor = (byte)((p[0] + p[1] + p[2]) / 3.0f);
                            }
                            p[0] = newColor;
                            p[1] = newColor;
                            p[2] = newColor;

                            p += 3;
                        }
                        p += srcData.Stride - w * 3;
                    }
                    bmp.UnlockBits(srcData);
                    return bmp;
                }
            }
            catch
            {
                return null;
            }

        }

        public static bool HistogramMatching(Image srcBmp, out Bitmap dstBmp)
        {
            if (srcBmp == null)
            {
                dstBmp = null;
                return false;
            }
            dstBmp = new Bitmap(srcBmp);
            using Bitmap tempSrcBmp = new Bitmap(srcBmp);

            getCumulativeProbabilityRGB(tempSrcBmp, out double[] srcCpR, out double[] srcCpG, out double[] srcCpB);

            double diffAR = 0, diffBR = 0, diffAG = 0, diffBG = 0, diffAB = 0, diffBB = 0;
            byte kR = 0, kG = 0, kB = 0;
            byte[] mapPixelR = new byte[256];
            byte[] mapPixelG = new byte[256];
            byte[] mapPixelB = new byte[256];
            //R
            for (int i = 0; i < 256; i++)
            {
                diffBR = 1;
                for (int j = kR; j < 256; j++)
                {
                    diffAR = Math.Abs(srcCpR[i] - matchCpR[j]);
                    if (diffAR - diffBR < 1.0E-08)
                    {
                        diffBR = diffAR;
                        kR = (byte)j;
                    }
                    else
                    {
                        kR = (byte)Math.Abs(j - 1);
                        break;
                    }
                }
                if (kR == 255)
                {
                    for (int l = i; l < 256; l++)
                    {
                        mapPixelR[l] = kR;
                    }
                    break;
                }
                mapPixelR[i] = kR;
            }
            //G
            for (int i = 0; i < 256; i++)
            {
                diffBG = 1;
                for (int j = kG; j < 256; j++)
                {
                    diffAG = Math.Abs(srcCpG[i] - matchCpG[j]);
                    if (diffAG - diffBG < 1.0E-08)
                    {
                        diffBG = diffAG;
                        kG = (byte)j;
                    }
                    else
                    {
                        kG = (byte)Math.Abs(j - 1);
                        break;
                    }
                }
                if (kG == 255)
                {
                    for (int l = i; l < 256; l++)
                    {
                        mapPixelG[l] = kG;
                    }
                    break;
                }
                mapPixelG[i] = kG;
            }
            //B
            for (int i = 0; i < 256; i++)
            {
                diffBB = 1;
                for (int j = kB; j < 256; j++)
                {
                    diffAB = Math.Abs(srcCpB[i] - matchCpB[j]);
                    if (diffAB - diffBB < 1.0E-08)
                    {
                        diffBB = diffAB;
                        kB = (byte)j;
                    }
                    else
                    {
                        kB = (byte)Math.Abs(j - 1);
                        break;
                    }
                }
                if (kB == 255)
                {
                    for (int l = i; l < 256; l++)
                    {
                        mapPixelB[l] = kB;
                    }
                    break;
                }
                mapPixelB[i] = kB;
            }
            BitmapData bmpData = dstBmp.LockBits(new Rectangle(0, 0, dstBmp.Width, dstBmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* ptr = null;
                for (int i = 0; i < dstBmp.Height; i++)
                {
                    ptr = (byte*)bmpData.Scan0 + i * bmpData.Stride;
                    for (int j = 0; j < dstBmp.Width; j++)
                    {
                        ptr[j * 3 + 2] = mapPixelR[ptr[j * 3 + 2]];
                        ptr[j * 3 + 1] = mapPixelG[ptr[j * 3 + 1]];
                        ptr[j * 3] = mapPixelB[ptr[j * 3]];
                    }
                }
            }
            dstBmp.UnlockBits(bmpData);
            return true;
        }
        private static void getCumulativeProbabilityRGB(Bitmap srcBmp, out double[] cpR, out double[] cpG, out double[] cpB)
        {
            if (srcBmp == null)
            {
                cpB = cpG = cpR = null;
                return;
            }
            cpR = new double[256];
            cpG = new double[256];
            cpB = new double[256];
            double[] tempR = new double[256];
            double[] tempG = new double[256];
            double[] tempB = new double[256];
            getHistogramRGB(srcBmp, out int[] hR, out int[] hG, out int[] hB);
            int totalPxl = srcBmp.Width * srcBmp.Height;
            for (int i = 0; i < 256; i++)
            {
                if (i != 0)
                {
                    tempR[i] = tempR[i - 1] + hR[i];
                    tempG[i] = tempG[i - 1] + hG[i];
                    tempB[i] = tempB[i - 1] + hB[i];
                }
                else
                {
                    tempR[0] = hR[0];
                    tempG[0] = hG[0];
                    tempB[0] = hB[0];
                }
                cpR[i] = (tempR[i] / totalPxl);
                cpG[i] = (tempG[i] / totalPxl);
                cpB[i] = (tempB[i] / totalPxl);
            }
        }
        public static void getHistogramRGB(Bitmap srcBmp, out int[] hR, out int[] hG, out int[] hB)
        {
            if (srcBmp == null)
            {
                hR = hB = hG = null;
                return;
            }
            hR = new int[256];
            hB = new int[256];
            hG = new int[256];
            BitmapData bmpData = srcBmp.LockBits(new Rectangle(0, 0, srcBmp.Width, srcBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* ptr = null;
                for (int i = 0; i < srcBmp.Height; i++)
                {
                    ptr = (byte*)bmpData.Scan0 + i * bmpData.Stride;
                    for (int j = 0; j < srcBmp.Width; j++)
                    {
                        hB[ptr[j * 3]]++;
                        hG[ptr[j * 3 + 1]]++;
                        hR[ptr[j * 3 + 2]]++;
                    }
                }
            }
            srcBmp.UnlockBits(bmpData);
            return;
        }
    }
}