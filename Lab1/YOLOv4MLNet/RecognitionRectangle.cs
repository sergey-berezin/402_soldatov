using System;

namespace YOLOv4MLNet
{
   public class RecognitionRectangle
    {
        private readonly double x;
        private readonly double y;
        private readonly double height;
        private readonly double width;
        private readonly string label;

        public RecognitionRectangle(double x, double y, double height, double width, string label)
        {
            this.x = x;
            this.y = y;
            this.height = height;
            this.width = width;
            this.label = label;
        }

        public override string ToString()
        {
            return label + " [coordinates: (" + Math.Round(x,2).ToString() + " ; " + Math.Round(y, 2).ToString() + ") - height: "
                + Math.Round(height, 2).ToString() + " - width: " + Math.Round(width, 2).ToString() + "]\n";
        }
    }
}
