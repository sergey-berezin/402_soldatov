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
        public string Path { get; set; }

        public string NewPath { get; set; }

        public IEnumerable<IGrouping<string, YoloV4Result>> Results { get; set; }

        public string StringResults { get; set; }

        public List<RecognitionRectangle> RecognitionRectangle { get; set; }

        public ImageInformation(string path, string NewPath, IEnumerable<IGrouping<string, YoloV4Result>> results, string stringres, List<RecognitionRectangle> rectangle)
        {
            this.Path = path;
            this.NewPath = NewPath;
            this.Results = results;
            this.StringResults = stringres;
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

        const string modelPath = @"C:\...\yolov4.onnx";

        private static readonly object obj = new object();
        public static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

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

            using var g = Graphics.FromImage(bitmap);

            using var brushes = new SolidBrush(Color.FromArgb(50, Color.Red));

            // predict
            var predict = predictionEngine.Predict(new YoloV4BitmapData() { Image = bitmap });
            var results = predict.GetResults(classesNames, 0.3f, 0.7f);
            var groupedResults = predict.GetResults(classesNames, 0.3f, 0.7f).GroupBy(e => e.Label);

            List<RecognitionRectangle> recognitionRectangleList = new List<RecognitionRectangle>();

            string str = "";

            foreach (var res in results)
            {
                var x1 = res.BBox[0];
                var y1 = res.BBox[1];
                var x2 = res.BBox[2];
                var y2 = res.BBox[3];

                recognitionRectangleList.Add(new RecognitionRectangle(x1, y1, y2 - y1, x2 - x1, res.Label));

                g.DrawRectangle(Pens.Red, x1, y1, x2 - x1, y2 - y1);
                g.FillRectangle(brushes, x1, y1, x2 - x1, y2 - y1);
                g.DrawString(res.Label, new Font("Arial", 52), Brushes.Blue, new PointF(x1, y1));
            }

            foreach(var res in groupedResults)
            {
                str += res.Key + " - " + res.Count() + "\n";
            }

            string NewImagePath = Path.ChangeExtension(imageName, "_processed" + Path.GetExtension(imageName));

            bitmap.Save(NewImagePath);

            return new ImageInformation(imageName, NewImagePath, groupedResults, str, recognitionRectangleList);
        }

        public void RecognitionStop()
        {
            cts.Cancel();
        }

        public void ProgramStart(string path)
        {

            /*            string[] filePathsToDelete = Directory.GetFiles(@path, "*processed.jpg");
                        foreach(var file in filePathsToDelete)
                        {
                            File.Delete(file);
                        }

                        // LOGIC TIME :)
                        string[] filePaths = Directory.GetFiles(@path, "*.jpg");*/

            string[] filePathsToDelete;
            string[] filePaths;

            try
            {
                filePathsToDelete = Directory.GetFiles(@path, "*processed.jpg");
                foreach (var file in filePathsToDelete)
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                if (path.EndsWith("processed.jpg"))
                {
                    File.Delete(path);
                }
            }

            try
            {
                filePaths = Directory.GetFiles(@path, "*.jpg");
            }
            catch (IOException)
            {
                filePaths = new string[1];
                filePaths[0] = path;
            }

            // Making tasks
            var tasks = new Task[filePaths.Count()];
            for (int i = 0; i <= filePaths.Count() - 1; i++)
            {
                tasks[i] = Task.Factory.StartNew(pi =>
                {
                    int idx = (int)pi;

                    if (!cts.IsCancellationRequested)
                    {
                        //lock (obj)
                        //{
                            ImageInformation stats = ImageRecognition(filePaths[idx]);
                            this.ResultEvent?.Invoke(this, stats);
                        //} 
                    }
                }, i, cts.Token);
  
            }

            Task.WaitAll(tasks);
        }
   
    }
}
