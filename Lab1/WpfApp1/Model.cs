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
        //public List<RecognitionRectangle> RecognitionRectangle { get; set; }
        public Blob ImageContext { get; set; }
    }

    public class ImageObject
    {
        public string StringResults { get; set; }
        //public List<RecognitionRectangle> RecognitionRectangle { get; set; }
        public ImageObject(string results/*,List<RecognitionRectangle> rectangles*/)
        {
            StringResults = results;
           // RecognitionRectangle = rectangles;
        }
    }

    public class ClassLabel
    {
        public int ClassLabelId { get; set; }
        public string StringClassLabel { get; set; }
        public ICollection<ModelImageInformation> ImagesInformation { get; set; }
    }

    public class ModelContext: DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite("DataSource=model.db");
        public DbSet<Blob> ImageContext { get; set; }
        public DbSet<ClassLabel> ClassLabels { get; set; }
        public DbSet<ModelImageInformation> ImagesInformation { get; set; }

/*        public ModelContext()
        {
            Database.EnsureCreated();
        }*/

        public ImageObject DatabaseCheck(string path) //???
        {
            ImageObject image = null;
            bool flag = true;
            byte[] BinaryFile = File.ReadAllBytes(path);

            foreach(var item in ImagesInformation.Include(obj => obj.ClassLabels).Where(obj => obj.Hash == GetHashCode(BinaryFile)))
            {
                Entry(item).Reference(obj => obj.ImageContext).Load();

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
                    image = new ImageObject(item.ClassLabels.StringClassLabel/*, item.RecognitionRectangle*/); //уже нет ошибки???
                    //SaveChanges();
                    break;
                }
            }

            return image;
        }

        public void DatabaseAdding(ImageInformation image) //???
        {
            ModelImageInformation AddedImage = new ModelImageInformation
            {
                Path = image.Path,
                ImageContext = new Blob() { ImageContext = File.ReadAllBytes(image.Path) },
                Hash = GetHashCode(File.ReadAllBytes(image.Path)),
            };

            AddedImage.ClassLabels = new ClassLabel()
            {
                StringClassLabel = image.StringResults
            };

            AddedImage.ClassLabels.ImagesInformation = new List<ModelImageInformation>
            {
                AddedImage
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

// Add-Migration InitialCreate
// Update-Database
// Drop-Database // Y
// Remove-Migration
