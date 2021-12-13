using System;
using System.Collections.Generic;
using System.Drawing;

namespace Contract
{
    public class RequestImage
    {
        public string ImageClass { get; set; }
        public byte[] Bitmap { get; set; }
    }
    public class ProcessedImage
    {
        public int ProcessedImageId { get; set; }
        public byte[] ImageContent { get; set; }
        public int ImageHashCode { get; set; }
        virtual public ICollection<RecognizedObject> Objects { get; set; }
    }
    public class RecognizedObject
    {
        public int RecognizedObjectId { get; set; }
        public float X1 { get; set; }
        public float Y1 { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }
        public string ClassName { get; set; }

        public int ProcessedImageId { get; set; }
    }
}
