using Microsoft.ML;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using YOLOv4MLNet.DataStructures;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;
using System.Threading;
using System.Threading.Tasks;

namespace YOLOv4MLNet
{
    public struct ImageInformation
    {
        public string Path { get; }

        public IEnumerable<IGrouping<string, YoloV4Result>> Results { get; }

        public List<RecognitionRectangle> RecognitionRectangle { get; }

        public ImageInformation(string path, IEnumerable<IGrouping<string, YoloV4Result>> results, List<RecognitionRectangle> rectangle)
        {
            this.Path = path;
            this.Results = results;
            this.RecognitionRectangle = rectangle;
        }
    }

    public delegate void EventHandler(object sender, ImageInformation info);

    public class RecognitionClass
    {

        public static int numberOfProcessedImages;

        public static CancellationTokenSource cts = new CancellationTokenSource();

        public RecognitionClass() { }
        public event EventHandler ResultEvent;

        const string modelPath = @"/Users/u0da/Documents/Lab1/YOLOv4MLNet/yolov4.onnx";

        private static readonly object obj = new object();
        static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

        public static ImageInformation ImageRecognition(string imageName)
        {

            MLContext mlContext = new MLContext();

            // Define scoring pipeline
            var pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input_1:0", scaleImage: 1f / 255f, interleavePixelColors: true))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    shapeDictionary: new Dictionary<string, int[]>()
                    {
                            { "input_1:0", new[] { 1, 416, 416, 3 } },
                            { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
                            { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                            { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                    },
                    inputColumnNames: new[]
                    {
                            "input_1:0"
                    },
                    outputColumnNames: new[]
                    {
                            "Identity:0",
                            "Identity_1:0",
                            "Identity_2:0"
                    },
                    modelFile: modelPath, recursionLimit: 100));

            // Fit on empty list to obtain input data schema
            var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));

            // Create prediction engine
            var predictionEngine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);

            using var bitmap = new Bitmap(Image.FromFile(imageName));

            // predict
            var predict = predictionEngine.Predict(new YoloV4BitmapData() { Image = bitmap });
            var results = predict.GetResults(classesNames, 0.3f, 0.7f);
            var groupedResults = predict.GetResults(classesNames, 0.3f, 0.7f).GroupBy(e => e.Label);
            List<RecognitionRectangle> recognitionRectangleList = new List<RecognitionRectangle>();
            foreach (var res in results)
            {
                var x1 = res.BBox[0];
                var y1 = res.BBox[1];
                var x2 = res.BBox[2];
                var y2 = res.BBox[3];
                recognitionRectangleList.Add(new RecognitionRectangle(x1, y1, y2 - y1, x2 - x1, res.Label)); 
            }
            return new ImageInformation(imageName, groupedResults, recognitionRectangleList);
        }

        public void RecognitionStop(CancellationTokenSource cts)
        {
            cts.Cancel();
        }

        public void ProgramStart(string path)
        {

            // LOGIC TIME :)
            string[] filePaths = Directory.GetFiles(@path, "*.jpg");

            // Making tasks
            var tasks = new Task[filePaths.Count()];
            for (int i = 0; i <= filePaths.Count() - 1; i++)
            {
                tasks[i] = Task.Factory.StartNew(pi =>
                {
                    int idx = (int)pi;

                    if (!cts.IsCancellationRequested)
                    {
                        ImageInformation stats = ImageRecognition(filePaths[idx]);
                        this.ResultEvent?.Invoke(this, stats);
                    }
                }, i, cts.Token);
  
            }

            Task.WaitAll(tasks);
        }
   
    }
}
