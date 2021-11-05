using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using YOLOv4MLNet;

namespace WpfApp1
{
    public class RecognitionViewModel: INotifyPropertyChanged
    {
        private RecognitionClass LibraryObject;

        readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        private void EventHandler(object sender, ImageInformation information)
        {
            dispatcher.BeginInvoke(new Action(() =>
            {
                ImageCollection.Add(information);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageCollection"));
            }));
        }

        private void ImageRecognitionWPF()
        {
            RecognitionStatus = true;

            ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
            {
                LibraryObject.ProgramStart(ChosenDirectoryPath);

                dispatcher.BeginInvoke(new Action(() =>
                {
                    RecognitionStatus = false;
                }));

            }));
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public bool RecognitionStatus = false;

        public string ChosenDirectoryPath { get; set; }

        public ObservableCollection<ImageInformation> ImageCollection { get; set; }
        public ObservableCollection<ImageInformation> SingleClassLabelCollection { get; set; }
        public ObservableCollection<AllClassLabels> AllClassLabelsCollection { get; set; }

        public static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

        public RecognitionViewModel()
        {
            ImageCollection = new ObservableCollection<ImageInformation>();
            SingleClassLabelCollection = new ObservableCollection<ImageInformation>();
            AllClassLabelsCollection = new ObservableCollection<AllClassLabels>();

            for (int i = 0; i <= 79; i++)
            {
                AllClassLabelsCollection.Add(new AllClassLabels()
                {
                    ClassLabel = classesNames[i]
                });
            }
        }

        public void NewOpeningAndRecognition()
        {
            LibraryObject = new RecognitionClass();
            LibraryObject.ResultEvent += EventHandler;

            ImageCollection.Clear();
            SingleClassLabelCollection.Clear();

            ImageRecognitionWPF();
        }

        public void Stop()
        {
            LibraryObject.RecognitionStop();
            RecognitionStatus = false;
        }

        public void CollectionFilter(AllClassLabels ClassLabelElement)
        {
                for (int i = 0; i < ImageCollection.Count(); i++)
                {
                    foreach (var res in ImageCollection[i].Results)
                    {
                        if (res.Key == ClassLabelElement.ClassLabel)
                        {
                        SingleClassLabelCollection.Add(ImageCollection[i]);
                        }
                    }
                }
        }
        
    }

    public class AllClassLabels
    {
        public string ClassLabel { get; set; }
    }
}
