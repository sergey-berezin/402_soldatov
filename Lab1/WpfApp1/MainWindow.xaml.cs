using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using YOLOv4MLNet;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private string ChosenDirectoryPath { get; set; }
        
        private bool RecognitionStatus = false;
        
        private RecognitionClass RecognitionClassObject = new RecognitionClass();
        private ObservableCollection<ImageObjectInformation> ImageCollection;
        private ObservableCollection<AllClassLabels> AllClassLabelsCollection;
        private ObservableCollection<SingleClassLabel> SingleClassLabelCollection;
        private ObservableCollection<BitmapImage> SingleClassBitmapImages;

        private void EventHandler(object sender, ImageInformation information)
        {
            ImageObjectInformation image = ImageCollection.First(picture => picture.ImagePath == information.Path);
            image.Image = new BitmapImage(new Uri(information.NewPath)); // with rectangles
            image.Image.Freeze(); // error is occurred here
            image.Label = information.Results;
            foreach (var res in image.Label)
            {
                image.RecognizedClasses += res.Key + " - " + res.Count() + "\n";
                AllClassLabels ClassLabel = AllClassLabelsCollection.First(picture => picture.ClassLabel == res.Key);
            }
        }

        private void RecognitionStop_Click(object sender, RoutedEventArgs e)
        {
            RecognitionClassObject.RecognitionStop();
        }

        private void FolderOpeningButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                DirectoryName.Text = dialog.SelectedPath;
                ChosenDirectoryPath = dialog.SelectedPath;
            }

            ImageCollection.Clear();
            SingleClassLabelCollection.Clear();

            string[] filePathsToDelete = Directory.GetFiles(ChosenDirectoryPath, "*processed.jpg");
            foreach (var file in filePathsToDelete)
            {
                File.Delete(file);
            }

            string[] filePaths = Directory.GetFiles(ChosenDirectoryPath, "*.jpg");

            for (int i = 0; i <= filePaths.Count() - 1; i++)
            {
                ImageCollection.Add(new ImageObjectInformation()
                {
                    Image = new BitmapImage(new Uri(filePaths[i])),
                    ImagePath = filePaths[i],
                    Label = null,
                    RecognizedClasses = ""
                }) ;
            }

            ImageRecognitionWPF();
        }

        private void ImageRecognitionWPF()
        {
            RecognitionClass RecognitionClassObject = new RecognitionClass();
            RecognitionClassObject.ResultEvent += EventHandler;
            RecognitionStatus = true;
            RecognitionClassObject.ProgramStart(ChosenDirectoryPath);
        }

        private void AllClassLabelsListBoxChanged(object sender, EventArgs e)
        {
            SingleClassLabelCollection.Clear();
            SingleClassBitmapImages.Clear();

            if (AllClassLabelsListBox.SelectedItem is AllClassLabels ClassLabelElement)
            {

                for (int i = 0; i < ImageCollection.Count(); i++)
                {
                    foreach (var res in ImageCollection[i].Label)
                    {
                        if (res.Key == ClassLabelElement.ClassLabel)
                        {
                            //SingleClassBitmapImages.Add(ImageCollection[i].Image); // with rectangles
                            SingleClassBitmapImages.Add(new BitmapImage(new Uri(ImageCollection[i].ImagePath)));
                        }
                    }
                }

                foreach (var image in SingleClassBitmapImages) 
                {
                    SingleClassLabelCollection.Add(new SingleClassLabel()
                    {
                        ImageOfClassLabel = image
                    });
                }

            }
        }

        public MainWindow()
        {
            InitializeComponent();

            ImageCollection = new ObservableCollection<ImageObjectInformation>();
            Binding ImageCollectionBinding = new Binding
            {
                Source = ImageCollection
            };
            ImagesAndInformationListBox.SetBinding(ItemsControl.ItemsSourceProperty, ImageCollectionBinding);

            AllClassLabelsCollection = new ObservableCollection<AllClassLabels>();
            Binding AllClassLabelsCollectionBinding = new Binding
            {
                Source = AllClassLabelsCollection
            };
            AllClassLabelsListBox.SetBinding(ItemsControl.ItemsSourceProperty, AllClassLabelsCollectionBinding);

            SingleClassLabelCollection = new ObservableCollection<SingleClassLabel>();
            Binding SingleClassLabelCollectionBinding = new Binding
            {
                Source = SingleClassLabelCollection
            };
            SingleClassListBox.SetBinding(ItemsControl.ItemsSourceProperty, SingleClassLabelCollectionBinding);

            SingleClassBitmapImages = new ObservableCollection<BitmapImage>();
            
            for (int i = 0; i <= 79; i++)
            {
                AllClassLabelsCollection.Add(new AllClassLabels()
                {
                    ClassLabel = classesNames[i]
                });
            }

        }

        public static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };
    }

    public class ImageObjectInformation: INotifyPropertyChanged
    {
        private IEnumerable<IGrouping<string, YOLOv4MLNet.DataStructures.YoloV4Result>> label;
        private BitmapImage image;
        public string ImagePath { get; set; }
        public BitmapImage Image
        {
            get
            {
                return image;
            }
            set
            {
                image = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Image"));
            }
        }

        public IEnumerable<IGrouping<string, YOLOv4MLNet.DataStructures.YoloV4Result>> Label
        { 
            get
            { 
                return label; 
            }
            set
            {
                label = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Label"));
            }
        }

        public string RecognizedClasses { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class AllClassLabels
    {
        public string ClassLabel { get; set; }
    }

    public class SingleClassLabel: INotifyPropertyChanged
    {
        private BitmapImage image;

        public event PropertyChangedEventHandler PropertyChanged;

        public BitmapImage ImageOfClassLabel
        {
            get
            {
                return image;
            }

            set
            {
                image = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageOfClassLabel"));
            }
        }
    }
}
