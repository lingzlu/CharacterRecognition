using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace BPSimplified
{
    class UnipenToBmp
    {
        private UPImage.Data.UPDataProvider dataProvider;
        private List<Dictionary<string, Bitmap>> datasets;
        public UnipenToBmp()
        {
            string dataFile = "UnipenData/data/1b/aga";
            datasets = new List<Dictionary<string, Bitmap>>();

            dataProvider = new UPImage.Data.UPDataProvider();
            dataProvider.IsDataStop = false;
            dataProvider.GetPatternsFromFiles(dataFile); //get patterns with default parameters
            dataProvider.IsDataStop = true;
            for (int i = 0; i < 50; i++)
            {
                datasets.Add(new Dictionary<string, Bitmap>());
            }
        }
        public void convertToBmp()
        {
  
            foreach (UPImage.Data.ByteImageData imgData in dataProvider.ByteImagePatterns)
            {
                Bitmap img = CopyDataToBitmap(imgData.Image, new Size(29, 29));
                string letter = new string(imgData.Label, 1);
                int index = 0;
                while (index < 60)
                {
                    if (!datasets[index].ContainsKey(letter))
                    {
                        datasets[index].Add(letter, img);
                        break;
                    }
                    index++;
                }
            }
            for (int i = 0; i < 50; i++)
            {
                foreach (var data in datasets[i])
                {
                    int num = i + 1;
                    String name = "Datasets/dataset" + num + "/" + data.Key + ".bmp";
                    Bitmap tmp = new Bitmap(data.Value, 29, 33);
                    tmp.Save(name, ImageFormat.Bmp);
                }
            }
        }
        public Bitmap CopyDataToBitmap(byte[] data, Size size)
        {
            //Here create the Bitmap to the know height, width and format
            Bitmap bmp = new Bitmap(size.Width, size.Height, PixelFormat.Format8bppIndexed);
            ColorPalette ncp = bmp.Palette;
            for (int i = 0; i < 256; i++)
                ncp.Entries[i] = Color.FromArgb(255, i, i, i);
            bmp.Palette = ncp;
            //Create a BitmapData and Lock all pixels to be written 
            BitmapData bmpData = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.WriteOnly, bmp.PixelFormat);
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bytes];
            for (int i = 0; i < bytes; i++)
            {
                rgbValues[i] = 255;
            }
            int bmpWidth = bmp.Width;
            int bmpHeight = bmp.Height;

            Parallel.For(0, bmpHeight, (h, loopstate) =>
            {
                for (int w = 0; w < bmpWidth; w++)
                {
                    rgbValues[h * bmpData.Stride + w] = data[h * bmpWidth + w];
                }
            });
            //Copy the data from the byte array into BitmapData.Scan0
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, bmpData.Scan0, rgbValues.Length);
            //Unlock the pixels
            bmp.UnlockBits(bmpData);

            //Return the bitmap 

            return bmp;
        }
    }
}
