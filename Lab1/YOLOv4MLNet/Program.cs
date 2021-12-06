using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace YOLOv4MLNet
{
   //https://towardsdatascience.com/yolo-v4-optimal-speed-accuracy-for-object-detection-79896ed47b50

    class Program
    {
        static int filePathCount = 0;

        public static void EventHandler(object sender, ImageInformation info)
        {

            string str = $"{info.Path}\n";

            str += "\nFound classes with additional information:\n";

            foreach (var res in info.RecognitionRectangle)
            {
                str += res;
            }

            str += "\nTotal number of found classes with number of its objects:\n";

            foreach (var res in info.Results)
            {
                str += res.Key + " - " + res.Count() + "\n";
            }

            RecognitionClass.numberOfProcessedImages++;
            Console.WriteLine(str + $"\n{Math.Round((RecognitionClass.numberOfProcessedImages / (float)filePathCount) * 100)}% of images is processed.\n");

            Console.WriteLine("\n # # # # # # # # # # # # # # # #\n");
        }

        static void Main()
        {
            Console.WriteLine("Enter the name of the directory with images: ");
            string directory = Console.ReadLine();
            filePathCount = Directory.GetFiles(directory, "*.jpg").Count();


            Console.WriteLine($"0% of images is processed.\n");
            RecognitionClass recognitionClassObj = new RecognitionClass();
            recognitionClassObj.ResultEvent += EventHandler;

            Task tokenTask = Task.Factory.StartNew(() =>
            {
                while (!(Console.ReadLine() == "s"))
                {
                    recognitionClassObj.RecognitionStop();
                }
            }, TaskCreationOptions.LongRunning);


            recognitionClassObj.ProgramStart(directory);
            Console.WriteLine($"Processing is over!");
        }
    }
}
