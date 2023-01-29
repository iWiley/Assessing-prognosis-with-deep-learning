using Microsoft.ML.Data;

namespace Deep_Learning_Training_Command_Line_Tool.DataModels
{
    public class ImagePrediction
    {
        [ColumnName("Score")]
        public float[] Score;

        [ColumnName("PredictedLabel")]
        public string PredictedLabel;
    }
}
