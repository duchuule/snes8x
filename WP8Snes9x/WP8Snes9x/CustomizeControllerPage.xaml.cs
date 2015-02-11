using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhoneDirect3DXamlAppComponent;
using System.IO.IsolatedStorage;
using System.Windows.Media;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class CustomizeControllerPage : PhoneApplicationPage
    {
        private CPositionDirect3DBackground m_d3dBackground = new CPositionDirect3DBackground();
        private int orientation = 0; //portrait = 2, landscape = 0

        public CustomizeControllerPage()
        {
            InitializeComponent();
            this.BackKeyPress += CustomizeControllerPage_BackKeyPress;
            this.InitAppBar();
        }

        private void CustomizeControllerPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
                if (this.ApplicationBar.Mode == ApplicationBarMode.Minimized) //if app bar is minimized, cancel the back action and show app bar
                {
                    e.Cancel = true;
                    this.ApplicationBar.Mode = ApplicationBarMode.Default;
                }

        }

        protected override void OnTap(System.Windows.Input.GestureEventArgs e)
        {
            if (this.ApplicationBar.Mode != ApplicationBarMode.Minimized)
                    this.ApplicationBar.Mode = ApplicationBarMode.Minimized;
                base.OnTap(e);

        }


        private void DrawingSurfaceBackground_Loaded(object sender, RoutedEventArgs e)
        {
            // Set window bounds in dips
            m_d3dBackground.WindowBounds = new Windows.Foundation.Size(
                (float)Application.Current.Host.Content.ActualWidth,
                (float)Application.Current.Host.Content.ActualHeight
                );

            // Set native resolution in pixels
            m_d3dBackground.NativeResolution = new Windows.Foundation.Size(
                (float)Math.Floor(Application.Current.Host.Content.ActualWidth * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f),
                (float)Math.Floor(Application.Current.Host.Content.ActualHeight * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f)
                );

            // Set render resolution to the full native resolution
            m_d3dBackground.RenderResolution = m_d3dBackground.NativeResolution;

            // Hook-up native component to DrawingSurfaceBackgroundGrid
            DrawingSurfaceBackground.SetBackgroundContentProvider(m_d3dBackground.CreateContentProvider());
            DrawingSurfaceBackground.SetBackgroundManipulationHandler(m_d3dBackground);

            string orientation_str = null;
            NavigationContext.QueryString.TryGetValue("orientation", out orientation_str);
            orientation = int.Parse(orientation_str);

            if (orientation_str != null)
                this.m_d3dBackground.ChangeOrientation(orientation);



        }


        private void InitAppBar()
        {
            ApplicationBar = new ApplicationBar();
            ApplicationBar.IsVisible = true;
            ApplicationBar.Opacity = 0.3;
            ApplicationBar.Mode = ApplicationBarMode.Minimized;
            ApplicationBar.BackgroundColor = (Color)App.Current.Resources["CustomChromeColor"];
            ApplicationBar.ForegroundColor = (Color)App.Current.Resources["CustomForegroundColor"];

          
            var okButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/check.png", UriKind.Relative))
            {
                Text = "ok"
            };
            okButton.Click += okButton_click;

            var cancelButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/cancel.png", UriKind.Relative))
            {
                Text = "cancel"
            };
            cancelButton.Click += cancelButton_Click;

            var resetButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/refresh.png", UriKind.Relative))
            {
                Text = "reset"
            };
            resetButton.Click += resetButton_Click;

           

            ApplicationBar.Buttons.Add(okButton);
            ApplicationBar.Buttons.Add(cancelButton);
            ApplicationBar.Buttons.Add(resetButton);

        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            //get default position
            int[] cpos = GetDefaultControllerPosition();

            //set position
            if (orientation == 2) //portrait
                m_d3dBackground.SetControllerPosition(cpos.Slice(0, 18));
            else
                m_d3dBackground.SetControllerPosition(cpos.Slice(18, 36));


        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }

        private void okButton_click(object sender, EventArgs e)
        {
            int[] cpos = new int[18];
            m_d3dBackground.GetControllerPosition(cpos);

            if (orientation == 2) //portrait
            {
                //save to setting object
                EmulatorSettings.Current.PadCenterXP = cpos[0];
                EmulatorSettings.Current.PadCenterYP = cpos[1];
                EmulatorSettings.Current.ACenterXP = cpos[2];
                EmulatorSettings.Current.ACenterYP = cpos[3];
                EmulatorSettings.Current.BCenterXP = cpos[4];
                EmulatorSettings.Current.BCenterYP = cpos[5];
                EmulatorSettings.Current.StartLeftP = cpos[6];
                EmulatorSettings.Current.StartTopP = cpos[7];
                EmulatorSettings.Current.SelectRightP = cpos[8];
                EmulatorSettings.Current.SelectTopP = cpos[9];
                EmulatorSettings.Current.LLeftP = cpos[10];
                EmulatorSettings.Current.LTopP = cpos[11];
                EmulatorSettings.Current.RRightP = cpos[12];
                EmulatorSettings.Current.RTopP = cpos[13];
                EmulatorSettings.Current.XCenterXP = cpos[14];
                EmulatorSettings.Current.XCenterYP = cpos[15];
                EmulatorSettings.Current.YCenterXP = cpos[16];
                EmulatorSettings.Current.YCenterYP = cpos[17];

                //save to disk
                IsolatedStorageSettings isoSettings = IsolatedStorageSettings.ApplicationSettings;
                EmulatorSettings settings = EmulatorSettings.Current;

                isoSettings[SettingsPage.PadCenterXPKey] = cpos[0];
                isoSettings[SettingsPage.PadCenterYPKey] = cpos[1];
                isoSettings[SettingsPage.ACenterXPKey] = cpos[2];
                isoSettings[SettingsPage.ACenterYPKey] = cpos[3];
                isoSettings[SettingsPage.BCenterXPKey] = cpos[4];
                isoSettings[SettingsPage.BCenterYPKey] = cpos[5];
                isoSettings[SettingsPage.StartLeftPKey] = cpos[6];
                isoSettings[SettingsPage.StartTopPKey] = cpos[7];
                isoSettings[SettingsPage.SelectRightPKey] = cpos[8];
                isoSettings[SettingsPage.SelectTopPKey] = cpos[9];
                isoSettings[SettingsPage.LLeftPKey] = cpos[10];
                isoSettings[SettingsPage.LTopPKey] = cpos[11];
                isoSettings[SettingsPage.RRightPKey] = cpos[12];
                isoSettings[SettingsPage.RTopPKey] = cpos[13];
                isoSettings[SettingsPage.XCenterXPKey] = cpos[14];
                isoSettings[SettingsPage.XCenterYPKey] = cpos[15];
                isoSettings[SettingsPage.YCenterXPKey] = cpos[16];
                isoSettings[SettingsPage.YCenterYPKey] = cpos[17];


                isoSettings.Save();

            }
            else
            {
                //save to setting object
                EmulatorSettings.Current.PadCenterXL = cpos[0];
                EmulatorSettings.Current.PadCenterYL = cpos[1];
                EmulatorSettings.Current.ACenterXL = cpos[2];
                EmulatorSettings.Current.ACenterYL = cpos[3];
                EmulatorSettings.Current.BCenterXL = cpos[4];
                EmulatorSettings.Current.BCenterYL = cpos[5];
                EmulatorSettings.Current.StartLeftL = cpos[6];
                EmulatorSettings.Current.StartTopL = cpos[7];
                EmulatorSettings.Current.SelectRightL = cpos[8];
                EmulatorSettings.Current.SelectTopL = cpos[9];
                EmulatorSettings.Current.LLeftL = cpos[10];
                EmulatorSettings.Current.LTopL = cpos[11];
                EmulatorSettings.Current.RRightL = cpos[12];
                EmulatorSettings.Current.RTopL = cpos[13];
                EmulatorSettings.Current.XCenterXL = cpos[14];
                EmulatorSettings.Current.XCenterYL = cpos[15];
                EmulatorSettings.Current.YCenterXL = cpos[16];
                EmulatorSettings.Current.YCenterYL = cpos[17];

                //save to disk
                IsolatedStorageSettings isoSettings = IsolatedStorageSettings.ApplicationSettings;
                EmulatorSettings settings = EmulatorSettings.Current;

                isoSettings[SettingsPage.PadCenterXLKey] = cpos[0];
                isoSettings[SettingsPage.PadCenterYLKey] = cpos[1];
                isoSettings[SettingsPage.ACenterXLKey] = cpos[2];
                isoSettings[SettingsPage.ACenterYLKey] = cpos[3];
                isoSettings[SettingsPage.BCenterXLKey] = cpos[4];
                isoSettings[SettingsPage.BCenterYLKey] = cpos[5];
                isoSettings[SettingsPage.StartLeftLKey] = cpos[6];
                isoSettings[SettingsPage.StartTopLKey] = cpos[7];
                isoSettings[SettingsPage.SelectRightLKey] = cpos[8];
                isoSettings[SettingsPage.SelectTopLKey] = cpos[9];
                isoSettings[SettingsPage.LLeftLKey] = cpos[10];
                isoSettings[SettingsPage.LTopLKey] = cpos[11];
                isoSettings[SettingsPage.RRightLKey] = cpos[12];
                isoSettings[SettingsPage.RTopLKey] = cpos[13];
                isoSettings[SettingsPage.XCenterXLKey] = cpos[14];
                isoSettings[SettingsPage.XCenterYLKey] = cpos[15];
                isoSettings[SettingsPage.YCenterXLKey] = cpos[16];
                isoSettings[SettingsPage.YCenterYLKey] = cpos[17];

                isoSettings.Save();
            }

            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();

        }



        public static int[] GetDefaultControllerPosition()
        {
            int nativeWidth = (int)(Application.Current.Host.Content.ActualWidth * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f);
            int nativeHeight = (int)(Application.Current.Host.Content.ActualHeight * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f);

            int[] ret = new int[36]; //see EmulatorSettings.h for order of the elements


                
            //portrait
            //joystick
            ret[0] = (int)(nativeWidth * 0.25f);
            ret[1] = (int)(nativeHeight * 0.7f);

            //a-b
            ret[2] = (int)(nativeWidth * 0.9f);
            ret[3] = (int)(nativeHeight * 0.7f); 

            ret[4] = (int)(nativeWidth * 0.76f);
            ret[5] = (int)(nativeHeight * 0.79f); 

            //start-select
            ret[6] = (int)(nativeWidth * 0.53f); 
            ret[7] = (int)(nativeHeight * 0.93f);

            ret[8] = (int)(nativeWidth * 0.47f); 
            ret[9] = (int)(nativeHeight * 0.93f); 

            //l-r button
            ret[10] = 0;
            ret[11] = (int)(nativeHeight * 0.87f);
            ret[12] = nativeWidth;
            ret[13] = (int)(nativeHeight * 0.87f); 

            //x-y button
            ret[14] = (int)(nativeWidth * 0.76f);
            ret[15] = (int)(nativeHeight * 0.61f);

            ret[16] = (int)(nativeWidth * 0.62f);
            ret[17] = (int)(nativeHeight * 0.7f); 


            //==landscape
            //joy stick
            ret[18] = (int)(nativeHeight * 0.17f);
            ret[19] =  (int)(nativeWidth * 0.75f);

            //a-b
            ret[20] = (int)(nativeHeight * 0.93f);
            ret[21] = (int)(nativeWidth * 0.75f);
            ret[22] = (int)(nativeHeight * 0.85f);
            ret[23] = (int)(nativeWidth * 0.9f);

            //start-select
            ret[24] = (int)(nativeHeight * 0.53f);
            ret[25] = (int)(nativeWidth * 0.90f);
            ret[26] = (int)(nativeHeight * 0.47f);
            ret[27] = (int)(nativeWidth * 0.90f);

            //L-R
            ret[28] = 0;
            ret[29] = (int)(nativeWidth * 0.3f);
            ret[30] = nativeHeight;
            ret[31] = (int)(nativeWidth * 0.3f);


            //x-y button
            ret[32] = (int)(nativeHeight * 0.85f);
            ret[33] = (int)(nativeWidth * 0.6f);

            ret[34] = (int)(nativeHeight * 0.77f);
            ret[35] = (int)(nativeWidth * 0.75f); 
                                                                                                

            return ret;
        }
    } //end class


    public static class Extensions
    {
        /// <summary>
        /// Get the array slice between the two indexes.
        /// ... Inclusive for start index, exclusive for end index.
        /// </summary>
        public static T[] Slice<T>(this T[] source, int start, int end)
        {
            // Handles negative ends.
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;

            // Return new array.
            T[] res = new T[len];
            for (int i = 0; i < len; i++)
            {
                res[i] = source[i + start];
            }
            return res;
        }
    }

}