using Microsoft.ML;
using Microsoft.ML.Data;
using System.Diagnostics;
namespace Wistu.Lib.ClassifyModel
{
    public static class Classifier
    {
        static string CurrentPath => Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private static string modelName = TotalModels[^1];
        public static string ModelName
        {
            get => modelName; set
            {
                modelName = value;
                PredictEngine = CreatePredictEngine();
            }
        }

        public static string[] TotalModels
        {
            get
            {
                if (!Directory.Exists(Path.Combine(CurrentPath, "Model")))
                {
                    return Array.Empty<string>();
                }
                if (totalModels == null)
                {
                    totalModels = Directory.GetFiles(Path.Combine(CurrentPath, "Model"), "*.zip");
                    totalModels = totalModels.OrderBy(ss => new FileInfo(ss).CreationTime).ToArray();
                }
                return totalModels;
            }
        }
        private static string[] totalModels;

        public static PredictionEngine<ModelInput, ModelOutput> PredictEngine { get; private set; } = CreatePredictEngine();
        private static PredictionEngine<ModelInput, ModelOutput> CreatePredictEngine()
        {
            var mlContext = new MLContext(666666);
            // wistu.cn is the author's blog URL, the first namespace used for the class library project in this solution is Wistu.Lib.ClassifyModel,
            // the previous name is used here for forward compatibility
            string tp = Path.Combine(Path.GetTempPath(), "Wistu.Lib.ClassifyModel");
            Directory.CreateDirectory(tp);
            mlContext.TempFilePath = tp;
            //using FileStream memory = new(ModelName, FileMode.Open, FileAccess.Read);
            ITransformer mlModel = mlContext.Model.Load(ModelName, out var _);
            return mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);
        }

        private static string[] labels;

        public static string[] Labels
        {
            get
            {
                if (labels == null)
                {
                    var labelBuffer = new VBuffer<ReadOnlyMemory<char>>();
                    PredictEngine.OutputSchema["Score"].Annotations.GetValue("SlotNames", ref labelBuffer);
                    labels = labelBuffer.DenseValues().Select(x => x.ToString()).ToArray();
                }
                return labels;
            }
        }

        public static Result Predict(ModelInput input)
        {
            var re = PredictEngine.Predict(input);
            return new Result { BetterLable = re.PredictedLabel, Scores = re.Score };
        }

        public static Result[] Predict(IEnumerable<ModelInput> inputs, Action<int> progress)
        {
            List<Result> results = new();
            int i = 0;
            foreach (var item in inputs)
            {
                var re = PredictEngine.Predict(item);
                results.Add(new Result { BetterLable = re.PredictedLabel, Scores = re.Score });
                i++;
                progress?.Invoke(i);
            }
            return results.ToArray();
        }

        public static void Init()
        {
            PredictEngine = CreatePredictEngine();
            Predict(new ModelInput { Image = Resource.image });
        }
    }
}