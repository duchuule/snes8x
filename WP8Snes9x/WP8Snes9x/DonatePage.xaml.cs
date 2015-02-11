using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

using System.Collections.ObjectModel;

using System.Threading.Tasks;

using Windows.ApplicationModel.Store;
using Store = Windows.ApplicationModel.Store;
using PhoneDirect3DXamlAppComponent;

using PhoneDirect3DXamlAppInterop.Resources;
using Microsoft.Phone.Info;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Telerik.Windows.Controls;



namespace PhoneDirect3DXamlAppInterop
{
    public partial class DonatePage : PhoneApplicationPage
    {
        public DonatePage()
        {
            
            InitializeComponent();

            pics.ItemsSource = picItems;
        }

        public ObservableCollection<ProductItem> picItems = new ObservableCollection<ProductItem>();

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            RenderStoreItems();
            base.OnNavigatedTo(e);
        }

        private async Task RenderStoreItems()
        {
            picItems.Clear();

            try
            {

                //StoreManager mySM = new StoreManager();
                ListingInformation li = await Store.CurrentApp.LoadListingInformationAsync();
                txtError.Visibility = Visibility.Collapsed; //if error, it would go to catch.

                string key = "";
                ProductListing pListing = null;
                string imageLink = "";
                string status = "";
                string pname = "";
                Visibility buyButtonVisibility = Visibility.Collapsed;


                // get bronze in-app purcase
                key = "bronzedonation";
                imageLink = "/Assets/Icons/bronze_dollar_icon.png";
                if (li.ProductListings.TryGetValue(key, out pListing))
                {
                    ProductLicense license = Store.CurrentApp.LicenseInformation.ProductLicenses[key];
                    status = license.IsActive ? "Donated, thank you!" : pListing.FormattedPrice;
                    //string receipt = await Store.CurrentApp.GetProductReceiptAsync(license.ProductId);

                    buyButtonVisibility = Store.CurrentApp.LicenseInformation.ProductLicenses[key].IsActive ? Visibility.Collapsed : Visibility.Visible;
                    pname = pListing.Name;
                }
                else
                {
                    status = "Product is in certification with MS. Please try again in tomorrow.";
                    buyButtonVisibility = Visibility.Collapsed;
                    pname = "Bronze Donation";
                }

                picItems.Add(
                    new ProductItem
                    {
                        imgLink = imageLink,
                        Name = pname,
                        Status = status,
                        key = key,
                        BuyNowButtonVisible = buyButtonVisibility
                    }
                );


                // get silver in-app purcase
                key = "silverdonation";
                imageLink = "/Assets/Icons/silver_dollar_icon.png";
                if (li.ProductListings.TryGetValue(key, out pListing))
                {
                    status = Store.CurrentApp.LicenseInformation.ProductLicenses[key].IsActive ? "Donated, thank you!" : pListing.FormattedPrice;
                    buyButtonVisibility = Store.CurrentApp.LicenseInformation.ProductLicenses[key].IsActive ? Visibility.Collapsed : Visibility.Visible;
                    pname = pListing.Name;
                }
                else
                {
                    status = "Product is in certification with MS. Please try again in tomorrow.";
                    buyButtonVisibility = Visibility.Collapsed;
                    pname = "Silver Donation";
                }

                picItems.Add(
                    new ProductItem
                    {
                        imgLink = imageLink,
                        Name = pname,
                        Status = status,
                        key = key,
                        BuyNowButtonVisible = buyButtonVisibility
                    }
                );

                // get gold in-app purcase
                key = "golddonation";
                imageLink = "/Assets/Icons/gold_dollar_icon.png";
                if (li.ProductListings.TryGetValue(key, out pListing))
                {
                    status = Store.CurrentApp.LicenseInformation.ProductLicenses[key].IsActive ? "Donated, thank you!" : pListing.FormattedPrice;
                    buyButtonVisibility = Store.CurrentApp.LicenseInformation.ProductLicenses[key].IsActive ? Visibility.Collapsed : Visibility.Visible;
                    pname = pListing.Name;
                }
                else
                {
                    status = "Product is in certification with MS. Please try again in tomorrow.";
                    buyButtonVisibility = Visibility.Collapsed;
                    pname = "Gold Donation";
                }

                picItems.Add(
                    new ProductItem
                    {
                        imgLink = imageLink,
                        Name = pname,
                        Status = status,
                        key = key,
                        BuyNowButtonVisible = buyButtonVisibility
                    }
                );




            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                txtError.Visibility = Visibility.Visible;
            }
            finally
            {
                txtLoading.Visibility = Visibility.Collapsed;
            }
        } //end renderstoresitem

        private async void ButtonBuyNow_Clicked(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            string key = btn.Tag.ToString();

            if (!Store.CurrentApp.LicenseInformation.ProductLicenses[key].IsActive)
            {
                ListingInformation li = await Store.CurrentApp.LoadListingInformationAsync();
                string pID = li.ProductListings[key].ProductId;

                try
                {
                    string receipt = await Store.CurrentApp.RequestProductPurchaseAsync(pID, false);

                    
                }
                catch (Exception)
                { }
            }
        }


    }


    public class ProductItem 
    { 
        public string imgLink { get; set; } 
        public string Status { get; set; } 
        public string Name { get; set; } 
        public string key { get; set; } 
        public Visibility BuyNowButtonVisible { get; set; } }
}