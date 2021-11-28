using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YOLOv4MLNet;

namespace WpfApp1
{
    public class Blob
    {
        public int BlobId { get; set; }
        public byte[] ImageContext { get; set; }
    }

    public class ModelImageInformation
    {
        public int Id { get; set; }
        public int Hash { get; set; }
        public string Path { get; set; }
        public ClassLabel ClassLabels { get; set; }
        public ICollection<Rectangle> RecognitionRectangle { get; set; }
        public Blob ImageContext { get; set; }
    }

    public class Rectangle
    {
        public int Id { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double height { get; set; }
        public double width { get; set; }
        public string label { get; set; }

        public Rectangle(double x, double y, double height, double width, string label)
        {
            this.x = x;
            this.y = y;
            this.height = height;
            this.width = width;
            this.label = label;
        }
    }

    public class ClassLabel
    {
        public int ClassLabelId { get; set; }
        public string StringClassLabel { get; set; }
    }


    public class ImageObject
    {
        public string StringResults { get; set; }
        public ICollection<Rectangle> RecognitionRectangle { get; set; }
        public ImageObject(string results, ICollection<Rectangle> rectangles)
        {
            StringResults = results;
            RecognitionRectangle = rectangles;
        }
    }

    public class ModelContext: DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite("DataSource=model.db");

        public DbSet<Blob> ImageContext { get; set; }
        public DbSet<ClassLabel> ClassLabels { get; set; }
        public DbSet<ModelImageInformation> ImagesInformation { get; set; }

        public ModelContext()
        {
            Database.EnsureCreated(); 
        }

        public ImageObject DatabaseCheck(string path)
        {
            ImageObject image = null;
            bool flag = true;

            byte[] BinaryFile = File.ReadAllBytes(path);

            foreach(var item in ImagesInformation.Where(obj => obj.Hash == GetHashCode(BinaryFile)))
            {
                Entry(item).Reference(obj => obj.ImageContext).Load();
                Entry(item).Collection(obj => obj.RecognitionRectangle).Load();
                Entry(item).Reference(obj => obj.ClassLabels).Load();

                if (BinaryFile.Length == item.ImageContext.ImageContext.Length)
                {
                    flag = true;
                    for (int i = 0; i <= BinaryFile.Length - 1; i++)
                    {
                        if (BinaryFile[i] != item.ImageContext.ImageContext[i])
                        {
                            flag = false;
                            break;
                        }
                    }
                }

                if (flag == true)
                {
                    image = new ImageObject(item.ClassLabels.StringClassLabel, item.RecognitionRectangle); 
                    break;
                }
            }

            return image;
        }

        public void DatabaseAdding(ImageInformation image)
        {

            ModelImageInformation AddedImage = new ModelImageInformation
            {
                Path = image.Path,
                ImageContext = new Blob() { ImageContext = File.ReadAllBytes(image.NewPath) },
                Hash = GetHashCode(File.ReadAllBytes(image.Path)),
            };

            AddedImage.RecognitionRectangle = new List<Rectangle>();
            foreach (var item in image.RecognitionRectangle)
            {
                AddedImage.RecognitionRectangle.Add(new Rectangle(item.x, item.y, item.height, item.width, item.label));
            }


            AddedImage.ClassLabels = new ClassLabel()
            {
                StringClassLabel = image.StringResults
            };

            ImagesInformation.Add(AddedImage);
            ClassLabels.Add(AddedImage.ClassLabels);
            ImageContext.Add(AddedImage.ImageContext);

            SaveChanges();
        }

        public void DatabaseCleanup()
        {
            foreach (var context in ImageContext)
            {
                ImageContext.Remove(context);
            }

            foreach (var label in ClassLabels)
            {
                ClassLabels.Remove(label);
            }

            foreach (var information in ImagesInformation)
            {
                ImagesInformation.Remove(information);
            }

            SaveChanges();
        }

        public static int GetHashCode(byte[] data)
        {
            unchecked
            {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < data.Length; i++)
                    hash = (hash ^ data[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;

                return hash;
            }
        }

    }
}

// Entity Framework Core database instruction:
// 1) Add-Migration InitialCreate
// 2) Update-Database
// 3) Drop-Database // 4) Y
// 5) Remove-Migration
