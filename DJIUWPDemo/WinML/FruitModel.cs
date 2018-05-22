using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// Fruit

namespace FruitWinML
{
    public sealed class FruitModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class FruitModelOutput
    {
        public IList<string> classLabel { get; set; }
        public IDictionary<string, float> loss { get; set; }
        public FruitModelOutput()
        {
            this.classLabel = new List<string>();

            this.loss = new Dictionary<string, float>()
            {
                { "apple", float.NaN },
                { "banana", float.NaN },
                { "coconut", float.NaN },
                { "orange", float.NaN },
                { "passionfruit", float.NaN },
                { "pineapple", float.NaN },
                { "strawberry", float.NaN },
            };
        }
    }

    public sealed class FruitModel
    {
        private LearningModelPreview learningModel;
        public static async Task<FruitModel> CreateFruitModel(StorageFile file)
        {
            LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            FruitModel model = new FruitModel();
            model.learningModel = learningModel;
            return model;
        }
        public async Task<FruitModelOutput> EvaluateAsync(FruitModelInput input) {
            FruitModelOutput output = new FruitModelOutput();
            LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
            return output;
        }
    }
}
