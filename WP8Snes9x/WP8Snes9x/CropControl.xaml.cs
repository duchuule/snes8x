using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;


namespace CropControl
{

    public enum AspectRatios { None, R43 }

    public partial class CropControl : UserControl
    {
        #region Public Members

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(BitmapSource), typeof(CropControl), new PropertyMetadata(null));

        public static readonly DependencyProperty AspectRatioProperty = DependencyProperty.Register("AspectRatio", typeof(AspectRatios), typeof(CropControl), new PropertyMetadata(AspectRatios.None, new PropertyChangedCallback(RatioChanged)));

        public static void RatioChanged(DependencyObject o, DependencyPropertyChangedEventArgs dp)
        {
            if (dp.NewValue != null)
            {
                (o as CropControl).InitCropPointers();
            }
        }

        public BitmapSource Source
        {
            get { return (BitmapSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public double MinimalCropSize { get; set; }

        public AspectRatios AspectRatio
        {
            get { return (AspectRatios)GetValue(AspectRatioProperty); }
            set { SetValue(AspectRatioProperty, value); }
        }

        #endregion

        #region Private Members

        bool isMouseCaptured;

        Point mousePrevPosition;


        double biasX, biasY;

        Rect bounds;

        ClipRect clip
        {
            get { 
                ClipRect cr = this.Resources["ClipRect"] as ClipRect;
                return cr;
            }
        }

        Dictionary<AspectRatios, double> mapRatio = new Dictionary<AspectRatios, double>() { { AspectRatios.None, 0.0 }, { AspectRatios.R43, 4.0 / 3 } };

        #endregion


        public CropControl()
        {
            InitializeComponent();
        }



        public WriteableBitmap CropImage()
        {
            try
            {
                WriteableBitmap wbSource = this.Source as WriteableBitmap;

                int sourceWidth = wbSource.PixelWidth;



                int w = (int)(clip.Width * wbSource.PixelWidth / Picture.Width); 
                int h = (int)(clip.Height * wbSource.PixelHeight / Picture.Height);
                WriteableBitmap wbResult = new WriteableBitmap(w, h);

                for (int y = 0; y <= h - 1; y++)
                {
                    int sourceIndex = (int)(clip.Left * wbSource.PixelWidth / Picture.Width) + (int)(clip.Top * wbSource.PixelHeight / Picture.Height + y) * sourceWidth;
                    int destinationIndex = y * w;
                    Array.Copy(wbSource.Pixels, sourceIndex, wbResult.Pixels, destinationIndex, w);
                }

                return wbResult;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        #region Event Handlers

        private void CropTopLeft_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = GetCorrectedPosition(e.GetPosition(Picture));

            if (isMouseCaptured)
            {
                if (AspectRatio == AspectRatios.None)
                {
                    clip.Top = Math.Max(Math.Min(clip.Bottom - MinimalCropSize, mousePos.Y), bounds.Top);
                    clip.Left = Math.Max(Math.Min(clip.Right - MinimalCropSize, mousePos.X), bounds.Left);
                }
                else
                {
                    double deltaV, deltaH;

                    deltaV = Math.Max(Math.Min(clip.Bottom - MinimalCropSize, mousePos.Y), bounds.Top) - clip.Top;

                    clip.Top += deltaV;

                    deltaH = deltaV * mapRatio[AspectRatio];

                    clip.Left += deltaH;

                    //correct size if out of bounds

                    if (clip.Left < 0.0)
                    {
                        clip.Left = bounds.Left;
                        var h = clip.Width / mapRatio[AspectRatio];
                        clip.Top = clip.Bottom - h;
                    }

                }
            }
        }

        private void CropBottomRight_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = GetCorrectedPosition(e.GetPosition(Picture));

            if (isMouseCaptured)
            {
                if (AspectRatio == AspectRatios.None)
                {
                    clip.Bottom = Math.Min(Math.Max(clip.Top + MinimalCropSize, mousePos.Y), bounds.Bottom);
                    clip.Right = Math.Min(Math.Max(clip.Left + MinimalCropSize, mousePos.X), bounds.Right);
                }
                else
                {
                    double deltaV, deltaH;

                    deltaV = Math.Min(Math.Max(clip.Top + MinimalCropSize, mousePos.Y), bounds.Bottom) - clip.Bottom;

                    clip.Bottom += deltaV;

                    deltaH = deltaV * mapRatio[AspectRatio];

                    clip.Right += deltaH;

                    //correct size if out of bounds

                    if (clip.Right > bounds.Right)
                    {
                        clip.Right = bounds.Right;
                        var h = clip.Width / mapRatio[AspectRatio];
                        clip.Bottom = clip.Top + h;
                    }

                }
            }
        }

        private void CropBottomLeft_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = GetCorrectedPosition(e.GetPosition(Picture));

            if (isMouseCaptured)
            {
                if (AspectRatio == AspectRatios.None)
                {
                    clip.Bottom = Math.Min(Math.Max(clip.Top + MinimalCropSize, mousePos.Y), bounds.Bottom);
                    clip.Left = Math.Max(Math.Min(clip.Right - MinimalCropSize, mousePos.X), bounds.Left);
                }
                else
                {
                    double deltaV, deltaH;

                    deltaV = Math.Min(Math.Max(clip.Top + MinimalCropSize, mousePos.Y), bounds.Bottom) - clip.Bottom;

                    clip.Bottom += deltaV;

                    deltaH = -1 * deltaV * mapRatio[AspectRatio];

                    clip.Left += deltaH;

                    //correct size if out of bounds

                    if (clip.Left < 0.0)
                    {
                        clip.Left = bounds.Left;
                        var h = clip.Width / mapRatio[AspectRatio];
                        clip.Bottom = clip.Top + h;
                    }

                }
            }
        }

        private void CropTopRight_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = GetCorrectedPosition(e.GetPosition(Picture));

            if (isMouseCaptured)
            {
                if (AspectRatio == AspectRatios.None)
                {
                    clip.Top = Math.Max(Math.Min(clip.Bottom - MinimalCropSize, mousePos.Y), bounds.Top);
                    clip.Right = Math.Min(Math.Max(clip.Left + MinimalCropSize, mousePos.X), bounds.Right);
                }
                else
                {
                    double deltaV, deltaH;

                    deltaV = Math.Max(Math.Min(clip.Bottom - MinimalCropSize, mousePos.Y), bounds.Top) - clip.Top;

                    clip.Top += deltaV;

                    deltaH = -1 * deltaV * mapRatio[AspectRatio];

                    clip.Right += deltaH;

                    //correct size if out of bounds

                    if (clip.Right > bounds.Right)
                    {
                        clip.Right = bounds.Right;
                        var h = clip.Width / mapRatio[AspectRatio];
                        clip.Top = clip.Bottom - h;
                    }

                }
            }
        }


        private void MouseButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            Image item = sender as Image;
            isMouseCaptured = false;
            item.ReleaseMouseCapture();
        }

        private void MouseButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            Image item = sender as Image;

            isMouseCaptured = true;
            item.CaptureMouse();

            biasX = -item.Margin.Left - (item.RenderTransform as TranslateTransform).X - e.GetPosition(item).X;
            biasY = -item.Margin.Top - (item.RenderTransform as TranslateTransform).Y - e.GetPosition(item).Y;
        }

        private void Picture_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Image item = sender as Image;


            if (e.GetPosition(item).X > clip.Left && e.GetPosition(item).X < clip.Right &&
                 e.GetPosition(item).Y > clip.Top && e.GetPosition(item).Y < clip.Bottom)
            {
                isMouseCaptured = true;
                item.CaptureMouse();

                mousePrevPosition = e.GetPosition(item);
            }
        }

        private void Picture_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseButtonUpHandler(sender, e);
        }

        private void Picture_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseCaptured)
            {
                Image item = sender as Image;

                Point mousePos = new Point(clip.Center.X + e.GetPosition(item).X - mousePrevPosition.X, clip.Center.Y + e.GetPosition(item).Y - mousePrevPosition.Y);

                if (mousePos.X > clip.Left && mousePos.X < clip.Right &&
                    mousePos.Y > clip.Top && mousePos.Y < clip.Bottom)
                {
                    double x, y, deltaV, deltaH;

                    var hc2 = clip.Width / 2;
                    var vc2 = clip.Height / 2;

                    x = Math.Min(Math.Max(mousePos.X, bounds.Left + hc2), bounds.Right - hc2);
                    y = Math.Min(Math.Max(mousePos.Y, bounds.Top + vc2), bounds.Bottom - vc2);

                    deltaH = x - clip.Center.X;
                    deltaV = y - clip.Center.Y;

                    clip.Bottom += deltaV;
                    clip.Top += deltaV;

                    clip.Left += deltaH;
                    clip.Right += deltaH;

                    mousePrevPosition = e.GetPosition(item);
                }
            }

        }

        private void Picture_MouseEnter(object sender, MouseEventArgs e)
        {
            Image item = sender as Image;

            Point mousePos = e.GetPosition(item);

            if (mousePos.X > clip.Left && mousePos.X < clip.Right &&
                mousePos.Y > clip.Top && mousePos.Y < clip.Bottom)
            {
                item.Cursor = Cursors.Hand;
            }
        }

        private void Picture_MouseLeave(object sender, MouseEventArgs e)
        {
            Image item = sender as Image;

            Point mousePos = e.GetPosition(item);

            if (mousePos.X > clip.Left && mousePos.X < clip.Right &&
                mousePos.Y > clip.Top && mousePos.Y < clip.Bottom)
            {
                item.Cursor = Cursors.Arrow;
            }
        }


        #endregion

        #region Helper Functions



        public void Init()
        {
            var cutH = this.Source.PixelWidth - MainCanvas.ActualWidth;
            var cutV = this.Source.PixelHeight - MainCanvas.ActualHeight;

            var ratio = (double)(this.Source.PixelHeight) / this.Source.PixelWidth;

            if (cutH > 0 || cutV > 0)
            {
                // shrink image
                if (cutH > cutV)
                {
                    Picture.Width = MainCanvas.ActualWidth;
                    Picture.Height = MainCanvas.ActualWidth * ratio;
                }
                else
                {
                    Picture.Height = MainCanvas.ActualHeight;
                    Picture.Width = MainCanvas.ActualHeight / ratio;
                }
            }
            else
            {
                Picture.Width = this.Source.PixelWidth;
                Picture.Height = this.Source.PixelHeight;
            }



            PictureMask.Width = Picture.Width;
            PictureMask.Height = Picture.Height;

            var shiftX = (MainCanvas.ActualWidth - Picture.Width) / 2;
            var shiftY = (MainCanvas.ActualHeight - Picture.Height) / 2;

            // center image
            Canvas.SetLeft(Picture, shiftX);
            Canvas.SetTop(Picture, shiftY);

            Canvas.SetLeft(PictureMask, shiftX);
            Canvas.SetTop(PictureMask, shiftY);

            bounds = new Rect(0, 0, Picture.Width, Picture.Height);

            clip.ShiftX = shiftX;
            clip.ShiftY = shiftY;

            InitCropPointers();
        }

        private void InitCropPointers()
        {
            clip.Reset();

            if (AspectRatio == AspectRatios.None)
            {
                clip.Left = bounds.Left; clip.Top = bounds.Top; clip.Right = bounds.Right; clip.Bottom = bounds.Bottom;
            }
            else
            {
                if (Picture.Height > Picture.Width)
                {
                    var h = Picture.Width / mapRatio[AspectRatio];
                    clip.Left = bounds.Left; clip.Top = (bounds.Bottom - h) / 2; clip.Right = bounds.Right; clip.Bottom = clip.Top + h;
                }
                else if (Picture.Height <= Picture.Width)
                {
                    var w = Picture.Height * mapRatio[AspectRatio];
                    clip.Left = (Picture.Width - w) / 2; clip.Top = bounds.Top; clip.Right = clip.Left + w; clip.Bottom = bounds.Bottom;
                }
            }
        }

        private Point GetCorrectedPosition(Point p)
        {
            return new Point(p.X + biasX, p.Y + biasY);
        }


        #endregion


        private void MainCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            this.Init();
        }




    }
}
