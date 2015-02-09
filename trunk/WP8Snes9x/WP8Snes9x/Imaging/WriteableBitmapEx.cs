using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace DucLe.Imaging
{
    //http://weblogs.asp.net/broux/archive/2011/02/08/silverlight-how-to-watermark-a-writeablebitmapimage-with-a-text.aspx
    public static class WriteableBitmapEx
    {
        public static WriteableBitmap CreateTile(WriteableBitmap input, Color tilecolor)
        {
            WriteableBitmap result = new WriteableBitmap(input.PixelWidth, input.PixelHeight);
            if (tilecolor.A > 0)
                result.Clear(tilecolor);

            //blit the logo
            result.Blit(new Rect(0, 0, input.PixelWidth, input.PixelHeight), input, new Rect(0, 0, input.PixelWidth, input.PixelHeight));

            return result;
        }


        /// <summary> 
        /// Creates a watermark on the specified image 
        /// </summary> 
        /// <param name="input">The image to create the watermark from</param> 
        /// <param name="watermark">The text to watermark</param> 
        /// <param name="color">The color - default is White</param> 
        /// <param name="fontSize">The font size - default is 50</param> 
        /// <param name="opacity">The opacity - default is 0.25</param> 
        /// <param name="hasDropShadow">Specifies if a drop shadow effect must be added - default is true</param> 
        /// <returns>The watermarked image</returns> 
        public static WriteableBitmap CreateTile(WriteableBitmap input, string appname, double fontSize, Thickness margin, HorizontalAlignment halign, VerticalAlignment valign, Color tilecolor, Color bandcolor )
        {
            //the canvas
            WriteableBitmap result = new WriteableBitmap(input.PixelWidth, input.PixelHeight);
            if (tilecolor.A > 0)
                result.Clear(tilecolor);


            if (appname != null && appname != "")
            {

                //the text banner
                var watermarked = GetTextBitmap(appname, fontSize, Colors.White, 1.0);

                var width = watermarked.PixelWidth;
                var height = watermarked.PixelHeight;


                //the band on top (or bottom)
                int bandHeight = 40;
                WriteableBitmap wbtmp = new WriteableBitmap(input.PixelWidth, bandHeight);
                wbtmp.Clear(bandcolor);





                //blit the band
                result.Blit(new Rect(0, 0, input.PixelWidth, bandHeight), wbtmp, new Rect(0, 0, wbtmp.PixelWidth, wbtmp.PixelHeight));


                //white line below the band
                wbtmp = new WriteableBitmap(input.PixelWidth, 1);
                wbtmp.Clear(Colors.White);

                //blit the white line
                result.Blit(new Rect(0, bandHeight, input.PixelWidth, 1), wbtmp, new Rect(0, 0, wbtmp.PixelWidth, 1));

                //var result = input.Clone();

                Rect position = new Rect();

                double leftOffset = 0;
                double topOffset = 0;

                if (halign == HorizontalAlignment.Right)
                    leftOffset = input.PixelWidth - width - margin.Right;
                else if (halign == HorizontalAlignment.Left)
                    leftOffset = margin.Left;
                else if (halign == HorizontalAlignment.Center)
                    leftOffset = (input.PixelWidth - width) / 2.0;

                if (valign == VerticalAlignment.Bottom)
                    topOffset = input.PixelHeight - height - margin.Bottom;
                else if (valign == VerticalAlignment.Top)
                    topOffset = margin.Top;


                position = new Rect(leftOffset, topOffset, width, height);

                //blit the text
                result.Blit(position, watermarked, new Rect(0, 0, width, height));
            }
            

            //blit the logo
            result.Blit(new Rect(0, 0, input.PixelWidth, input.PixelHeight), input, new Rect(0, 0, input.PixelWidth, input.PixelHeight));

            
            
            

            
            return result;
        }

        /// <summary> 
        /// Creates a WriteableBitmap from a text 
        /// </summary> 
        /// <param name="text"></param> 
        /// <param name="fontSize"></param> 
        /// <param name="color"></param> 
        /// <param name="opacity"></param> 
        /// <param name="hasDropShadow"></param> 
        /// <returns></returns> 
        private static WriteableBitmap GetTextBitmap(string text, double fontSize, Color color, double opacity)
        {
            TextBlock txt = new TextBlock();
            txt.Text = text;
            txt.FontSize = fontSize;
            txt.Foreground = new SolidColorBrush(color);
            txt.Opacity = opacity;
            


            WriteableBitmap bitmap = new WriteableBitmap((int)txt.ActualWidth, (int)txt.ActualHeight);
            bitmap.Render(txt, null);

            bitmap.Invalidate();

            return bitmap;
        }
    }
}
