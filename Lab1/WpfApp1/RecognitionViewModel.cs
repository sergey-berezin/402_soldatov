using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using YOLOv4MLNet;

namespace WpfApp1
{
    public class RecognitionViewModel: INotifyPropertyChanged
    {
        private RecognitionClass LibraryObject;

        private ModelContext model;

        private readonly object LockObject;

        readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        private void EventHandler(object sender, ImageInformation information)
        {
            dispatcher.BeginInvoke(new Action(() =>
            {
                //ImageCollection.Add(information);
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageCollection")); 
                ImageCollection.Add(new ImageInformation(information.Path, information.NewPath, information.Results, information.StringResults,
                    information.RecognitionRectangle));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageCollection"));

                Task.Run(() =>
                {
                    lock (LockObject)
                    {
                        model.DatabaseAdding(information);
                    }
                });
            }));
        }

        private void ImageRecognitionWPF()
        {
            RecognitionStatus = true;

/*            foreach (var path in Directory.GetFiles(ChosenDirectoryPath, "*.jpg"))
            {
                if (path.EndsWith("processed.jpg"))
                {
                    File.Delete(path);
                }
            }*/

            ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
            {
                foreach (var path in Directory.GetFiles(ChosenDirectoryPath, "*.jpg"))
                {
                    /*                    if (path.EndsWith("processed.jpg"))
                                        {
                                            File.Delete(path);
                                        }*/

                    /*                    dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            ImageCollection.Add(new ImageInformation()
                                            {
                                                Path = path,
                                                NewPath = path,
                                                Results = null,
                                                StringResults = "",
                                                RecognitionRectangle = null,
                                            });
                                        }));*/

                    if (!path.EndsWith("processed.jpg")) //smth new
                    {

                        ImageObject ObjectCheck = model.DatabaseCheck(path);

                        dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (ObjectCheck == null)
                            {
                                Task.Run(() =>
                                {
                                    LibraryObject = new RecognitionClass();
                                    LibraryObject.ResultEvent += EventHandler;
                                    LibraryObject.ProgramStart(path);
                                });
                            }
                            else
                            {
                                ImageCollection.Add(new ImageInformation(path, path, null, ObjectCheck.StringResults, null)); //???
                                //image.RecognitionRectangle = ObjectCheck.RecognitionRectangle;
                                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageCollection"));
                            }
                        }));
                    }

                }
                RecognitionStatus = false;

                //LibraryObject.ProgramStart(ChosenDirectoryPath);

                //dispatcher.BeginInvoke(new Action(() =>
                //{
                //    RecognitionStatus = false;
                //}));

            }));
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public bool RecognitionStatus = false;
        public bool DatabaseCleaningStatus = false;

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

            model = new ModelContext();
            LockObject = new object();

            for (int i = 0; i <= 79; i++)
            {
                AllClassLabelsCollection.Add(new AllClassLabels()
                {
                    ClassLabel = classesNames[i]
                });
            }


            foreach (var item in model.ImagesInformation)
            {
                ImageObject ObjectCheck = model.DatabaseCheck(item.Path);

                dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ObjectCheck != null)
                    {
                        ImageCollection.Add(new ImageInformation(item.Path, item.Path, null, ObjectCheck.StringResults, null)); //wtf
                        //image.RecognitionRectangle = ObjectCheck.RecognitionRectangle;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageCollection"));
                    }
                }));
            }
        }

        public void NewOpeningAndRecognition()
        {
            //LibraryObject = new RecognitionClass();
            //LibraryObject.ResultEvent += EventHandler;

            //ImageCollection.Clear(); по заданию?...
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

        public void DatabaseCleaning()
        {
            if (RecognitionStatus == false)
            {
                lock (LockObject)
                {
                    DatabaseCleaningStatus = true;

                    model.DatabaseCleanup();
                    model = new ModelContext();

                    DatabaseCleaningStatus = false;
                }
            }
        }
        
    }

    public class AllClassLabels
    {
        public string ClassLabel { get; set; }
    }
}
