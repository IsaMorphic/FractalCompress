/*
 *  Copyright (C) 2020 Chosen Few Software
 *  This file is part of FractalCompress.ConsoleApp.
 *
 *  FractalCompress.ConsoleApp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  FractalCompress.ConsoleApp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with FractalCompress.ConsoleApp.  If not, see <https://www.gnu.org/licenses/>.
 */
using SkiaSharp;
using System;

namespace FractalCompress.ConsoleApp
{
    public static class ImageExtensions
    {
        public static Image[] FromBitmap(SKBitmap bitmap)
        {
            Image[] images = new Image[3]
            {
                    new Image(bitmap.Height, bitmap.Width),
                    new Image(bitmap.Height, bitmap.Width),
                    new Image(bitmap.Height, bitmap.Width)
            };
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    SKColor pixel = bitmap.GetPixel(x, y);
                    images[0].Data[y, x] = (float)pixel.Red / 255;
                    images[1].Data[y, x] = (float)pixel.Green / 255;
                    images[2].Data[y, x] = (float)pixel.Blue / 255;
                }
            }
            return images;
        }

        public static SKBitmap ToBitmap(Image[] imageData)
        {
            SKBitmap bitmap = new SKBitmap(imageData[0].Width, imageData[0].Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    byte red = (byte)Math.Clamp(imageData[0].Data[y, x] * 255, 0, 255);
                    byte green = (byte)Math.Clamp(imageData[1].Data[y, x] * 255, 0, 255);
                    byte blue = (byte)Math.Clamp(imageData[2].Data[y, x] * 255, 0, 255);
                    bitmap.SetPixel(x, y, new SKColor(red, green, blue));
                }
            }
            return bitmap;
        }
    }
}
