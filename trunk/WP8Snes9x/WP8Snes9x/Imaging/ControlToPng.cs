/**********************************************************
 * Control2Png.cs
 * Renders a Silverlight UIElement as an uncompressed PNG 
 * stream or byte array. Relies on Joe Stegman's great
 * PNGEncoder classes.
 *
 * Written by : Pierre BELIN <pierre.belin@inbox.com>
 *
 * Distributed under the Microsoft Public License (Ms-PL).
 * See accompanying file License.txt
 *
 **********************************************************/

using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;


using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using System.Windows.Resources;

using System.IO.IsolatedStorage;




namespace DucLe.Imaging
{
    /// <summary>
    /// See http://ree7.fr/blog/2010/10/composition-dimage-rendre-du-xaml-sous-forme-dimage/
    /// </summary>
    public class ControlToPng
    {
        

        public static Stream RenderAsPNGStream(UIElement e)
        {
            try
            {
                //this does not work well, often image does not get rendered - DL

                WriteableBitmap wb = new WriteableBitmap(e, null);


                EditableImage edit = new EditableImage(wb.PixelWidth, wb.PixelHeight);

                for (int y = 0; y < wb.PixelHeight; ++y)
                {
                    for (int x = 0; x < wb.PixelWidth; ++x)
                    {
                        byte[] rgba = ExtractRGBAfromPremultipliedARGB(wb.Pixels[wb.PixelWidth * y + x]);
                        edit.SetPixel(x, y, rgba[0], rgba[1], rgba[2], rgba[3]);
                    }
                }

                return edit.GetStream();

            }
            catch (Exception)
            {
                return null;
            }
        }

        public static byte[] RenderAsPNGBytes(UIElement e)
        {
            Stream s = RenderAsPNGStream(e);
            
            if (s == null)
                return null;

            byte[] bytes = ReadFully(s, (int)s.Length);
            return bytes;
        }

        /// <summary>
        /// Convert from premultiplied alpha ARGB to a non-premultiplied RGBA (fix 12/2011)
        /// </summary>
        /// <seealso cref="http://nokola.com/blog/post/2010/01/27/The-Most-Important-Silverlight-WriteableBitmap-Gotcha-Does-It-LoseChange-Colors.aspx"/>
        public static byte[] ExtractRGBAfromPremultipliedARGB(int pARGB)
        {
            byte[] sourcebytes = new byte[4];
            sourcebytes[0] = (byte)(pARGB >> 24);
            sourcebytes[1] = (byte)((pARGB & 0x00FF0000) >> 16);
            sourcebytes[2] = (byte)((pARGB & 0x0000FF00) >> 8);
            sourcebytes[3] = (byte)(pARGB & 0x000000FF);

            if (pARGB == 0) return sourcebytes; // optimization for images with many transparent pixels
            
            byte[] destbytes = new byte[4];

            if (sourcebytes[0] == 0 || sourcebytes[0] == 255)
            {
                destbytes[0] = sourcebytes[1];
                destbytes[1] = sourcebytes[2];
                destbytes[2] = sourcebytes[3];
                destbytes[3] = sourcebytes[0];
            }
            else
            {
                double factor = 255.0 / sourcebytes[0];
                double r = sourcebytes[1] * factor;
                double g = sourcebytes[2] * factor;
                double b = sourcebytes[3] * factor;

                destbytes[0] = Convert.ToByte(Math.Min(Byte.MaxValue, r));
                destbytes[1] = Convert.ToByte(Math.Min(Byte.MaxValue, g));
                destbytes[2] = Convert.ToByte(Math.Min(Byte.MaxValue, b));
                destbytes[3] = sourcebytes[0];
            }

            return destbytes;
        }

        /// <summary>
        /// Reads data from a stream until the end is reached. The
        /// data is returned as a byte array. An IOException is
        /// thrown if any of the underlying IO calls fail.
        /// </summary>
        /// <param name="stream">The stream to read data from</param>
        /// <param name="initialLength">The initial buffer length</param>
        private static byte[] ReadFully(Stream stream, int initialLength)
        {
            // If we've been passed an unhelpful initial length, just
            // use 32K.
            if (initialLength < 1)
            {
                initialLength = 32768;
            }

            byte[] buffer = new byte[initialLength];
            int read = 0;

            int chunk;
            while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
            {
                read += chunk;

                // If we've reached the end of our buffer, check to see if there's
                // any more information
                if (read == buffer.Length)
                {
                    int nextByte = stream.ReadByte();

                    // End of stream? If so, we're done
                    if (nextByte == -1)
                    {
                        return buffer;
                    }

                    // Nope. Resize the buffer, put in the byte we've just
                    // read, and continue
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[read] = (byte)nextByte;
                    buffer = newBuffer;
                    read++;
                }
            }
            // Buffer is now too big. Shrink it.
            byte[] ret = new byte[read];
            Array.Copy(buffer, ret, read);
            return ret;
        }
    }
}
