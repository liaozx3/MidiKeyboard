using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace MidiKeyboard
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Startpage : Page
    {
        public Startpage()
        {
            this.InitializeComponent();

            var viewTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            viewTitleBar.BackgroundColor = Windows.UI.Colors.Black;
            viewTitleBar.ButtonBackgroundColor = Windows.UI.Colors.Black;
            UpdateTile();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            UpdateTile();
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }

        private void UpdateTile()
        {
            TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
            Windows.Data.Xml.Dom.XmlDocument xdoc = new Windows.Data.Xml.Dom.XmlDocument();
            xdoc.LoadXml(System.IO.File.ReadAllText("tile.xml"));
            var notification = new Windows.UI.Notifications.TileNotification(xdoc);
            var updator = Windows.UI.Notifications.TileUpdateManager.CreateTileUpdaterForApplication();
            updator.Update(notification);
        }

    }
}
