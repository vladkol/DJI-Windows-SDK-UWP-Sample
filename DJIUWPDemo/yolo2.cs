using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// YOLO2

namespace DJIDemo
{
    public sealed class YOLO2ModelInput
    {
        public VideoFrame input__0 { get; set; }
    }

    public sealed class YOLO2ModelOutput
    {
        public IList<float> output__0 { get; set; }
        public YOLO2ModelOutput()
        {
            this.output__0 = new List<float>();
        }
    }

    public sealed class YOLO2Model
    {
        private LearningModelPreview learningModel;
        public static async Task<YOLO2Model> CreateYOLO2Model(StorageFile file)
        {
            LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            YOLO2Model model = new YOLO2Model();
            model.learningModel = learningModel;
            return model;
        }
        public async Task<YOLO2ModelOutput> EvaluateAsync(YOLO2ModelInput input) {
            YOLO2ModelOutput output = new YOLO2ModelOutput();
            LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
            binding.Bind("input__0", input.input__0);
            binding.Bind("output__0", output.output__0);
            LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
            return output;
        }
    }
}
