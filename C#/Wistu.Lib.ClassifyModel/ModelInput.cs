using Microsoft.ML.Data;

namespace Wistu.Lib.ClassifyModel
{
    public class ModelInput
    {
        [ColumnName(@"Image")]
        public byte[] Image { get; set; }
        [ColumnName(@"Label")]
        public string Label { get; set; }

        [ColumnName(@"ImageFileName")]
        public string ImagePath { get; set; }
    }
}