#region Copyright (c), Some Rights Reserved
/*##########################################################################
 * 
 * ImageProcessing.cs
 * -------------------------------------------------------------------------
 * By
 * Murat FIRAT, September 2007
 * 
 * -------------------------------------------------------------------------
 * Description:
 * Some Useful Methods For Handling Images..
 * 
 * -------------------------------------------------------------------------
 ###########################################################################*/
#endregion

using System;
using System.Drawing;
using System.IO;
using AForge;
using AForge.Imaging;
using AForge.Math;
using AForge.Imaging.Filters;
using AForge.Imaging.Textures;
using System.Drawing.Imaging;
using System.Collections.Generic;
using AForge.Math.Geometry;

namespace CharacterRecognition
{
    public class ImageProcessing
    {
        private static Bitmap ProcessedImage = null;
        private static bool Processed = false;


        public static Bitmap GetRectangles(Bitmap sourceImage)
        {
            Bitmap rectImage = Grayscale.CommonAlgorithms.RMY.Apply(sourceImage);
            Threshold th = new Threshold(30);
            th.ApplyInPlace(rectImage);
            Invert filter = new Invert();
            filter.ApplyInPlace(rectImage);
            // process image with blob counter
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.ProcessImage(rectImage);
          //  Blob[] blobs = blobCounter.GetObjectsInformation();
            Rectangle[] rects = blobCounter.GetObjectsRectangles();

            // lock image to draw on it
            BitmapData data = sourceImage.LockBits(
                new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
                    ImageLockMode.ReadWrite, sourceImage.PixelFormat);

            foreach (Rectangle rect in rects)
            {
                Drawing.Rectangle(data, rect, Color.Red);
            }

            sourceImage.UnlockBits(data);

            return sourceImage;
        }
        public static Bitmap Preprocessing(Bitmap sourceImage)
        {
            ProcessedImage = Grayscale.CommonAlgorithms.RMY.Apply(sourceImage);
            ApplyFilter(new Invert());
            ApplyFilter(new ResizeBicubic(60, 90));
            ApplyFilter(new Threshold(30));    //binarization

            ApplyFilter(new BinaryDilatation3x3());
            ApplyFilter(new BinaryErosion3x3());

            CroppedImage();
            ApplyFilter(new ResizeBicubic(40, 60));
            Skeletonization();
            Processed = true;
            return ProcessedImage;
        }
        public static List<double> FeatureExtraction(Bitmap processedImage)
        {
            if (Processed == false)
            {
                throw new System.InvalidOperationException("Pre-Processing Operation required before feature extraction");
            }
            List<double> Features = new List<double>();
            double rowFeatures = 0;
            double colFeatures = 0;
            int zonesPerRow = processedImage.Height / 10;
            int zonesPerCol = processedImage.Width / 10;


            Bitmap tmp = new Bitmap(processedImage.Width, processedImage.Height);
            Graphics g = Graphics.FromImage(tmp);
            g.DrawImage(processedImage, 0, 0);
            for (int row = 0; row < zonesPerRow; row++)
            {
                for (int col = 0; col < zonesPerCol; col++)
                {
                    Rectangle subZone = new Rectangle(10 * col, 10 * row, 10, 10);

                    Pen redPen = new Pen(Color.Red, 1);
                    g.DrawRectangle(redPen, subZone);

                    Crop zone = new Crop(subZone);
                    double zoneValue = diagonalFeature(zone.Apply(processedImage));
                    Features.Add(zoneValue);
                }
            }

            // box.Image = tmp;
            g.Dispose();
            int totalZones = Features.Count;

            for (int i = 0; i < totalZones; i += zonesPerCol)
            {
                rowFeatures = 0;
                for (int j = 0; j < zonesPerCol; j++)
                {
                    rowFeatures += Features[i + j];
                }
                Features.Add(rowFeatures / zonesPerCol);
            }


            for (int i = 0; i < zonesPerCol; i++)
            {
                colFeatures = 0;
                for (int j = 0; j < totalZones; j += zonesPerCol)
                {
                    colFeatures += Features[i + j];
                }
                Features.Add(colFeatures / zonesPerRow);
            }
            processedImage.Dispose();
            Processed = false;
            return Features;
        }
        private static double diagonalFeature(Bitmap zone)
        {
            double sum = 0;
            int n = 10;
            int count = 0;
            for (int diagLine = 0; diagLine < 2 * n - 1; ++diagLine)
            {
                int tmp = diagLine < n ? 0 : diagLine - n + 1;
                for (int j = tmp; j <= diagLine - tmp; ++j)
                {
                    Color pixel = zone.GetPixel(j, diagLine - j);

                    if (pixel.R == 255)
                        sum++;
              
                    count++;
                }
            }
            count = count * 1;
            return sum / 19;
        }
        private static void CroppedImage()
        {
            int topLeftX = 99;
            int topLeftY = 99;
            int buttomRightX = 0;
            int buttomRightY = 0;
            BlobCounter bc = new BlobCounter();
            bc.ProcessImage(ProcessedImage);
            Rectangle[] rects = bc.GetObjectsRectangles();
            foreach (Rectangle rect in rects)
            {
                if (rect.X < topLeftX)
                    topLeftX = rect.X;
                if (rect.Y < topLeftY)
                    topLeftY = rect.Y;
                if ((rect.X + rect.Width) > buttomRightX)
                    buttomRightX = rect.X + rect.Width;
                if ((rect.Y + rect.Height) > buttomRightY)
                    buttomRightY = rect.Y + rect.Height;
            }
            Rectangle boundary = new Rectangle(topLeftX, topLeftY, buttomRightX - topLeftX, buttomRightY - topLeftY);
            ApplyFilter(new Crop(boundary));
        }
        private static void Skeletonization()
        {
            ApplyFilter(new Threshold(50));    //binarization before Skeletonization
            // create filter sequence
            FiltersSequence filterSequence = new FiltersSequence();
            // add 8 thinning filters with different structuring elements
            filterSequence.Add(new HitAndMiss(new short[,] { { 0, 0, 0 }, { -1, 1, -1 }, { 1, 1, 1 } },
                HitAndMiss.Modes.Thinning));
            filterSequence.Add(new HitAndMiss(new short[,] { { -1, 0, 0 }, { 1, 1, 0 }, { -1, 1, -1 } },
                HitAndMiss.Modes.Thinning));
            filterSequence.Add(new HitAndMiss(new short[,] { { 1, -1, 0 }, { 1, 1, 0 }, { 1, -1, 0 } },
                HitAndMiss.Modes.Thinning));
            filterSequence.Add(new HitAndMiss(new short[,] { { -1, 1, -1 }, { 1, 1, 0 }, { -1, 0, 0 } },
                HitAndMiss.Modes.Thinning));
            filterSequence.Add(new HitAndMiss(new short[,] { { 1, 1, 1 }, { -1, 1, -1 }, { 0, 0, 0 } },
                HitAndMiss.Modes.Thinning));
            filterSequence.Add(new HitAndMiss(new short[,] { { -1, 1, -1 }, { 0, 1, 1 }, { 0, 0, -1 } },
                HitAndMiss.Modes.Thinning));
            filterSequence.Add(new HitAndMiss(new short[,] { { 0, -1, 1 }, { 0, 1, 1 }, { 0, -1, 1 } },
                HitAndMiss.Modes.Thinning));
            filterSequence.Add(new HitAndMiss(new short[,] { { 0, 0, -1 }, { 0, 1, 1 }, { -1, 1, -1 } },
                HitAndMiss.Modes.Thinning));
            // create filter iterator for 10 iterations
            FilterIterator filter = new FilterIterator(filterSequence, 8);
            // apply the filter
            ApplyFilter(filter);

        }
 
        private static void ApplyFilter(IFilter filter)
        {
            //box.Image = null;
            ProcessedImage = filter.Apply(ProcessedImage);
            // display filtered image
            //box.Image = ProcessedImage;
        }

    }
}
