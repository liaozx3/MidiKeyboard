using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using System.Threading.Tasks;
using Windows.UI;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Shapes;
using Windows.UI.ViewManagement;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.System;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MidiKeyboard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        VirtualKey VK_AKey, VK_ASharpKey, VK_BKey, VK_BSharpKey, VK_CKey, VK_CSharpKey,
                                                VK_DKey, VK_DSharpKey, VK_EKey, VK_ESharpKey;
        // MidiOutPort of the output MIDI device
        IMidiOutPort midiOutPort;

        // MIDI message to change the instument
        IMidiMessage instrumentChange;

        // Integer to define the frequecy interval of an instrument
        int octaveInterval = 0;

        // The MIDI channel used for the MIDI output device
        byte channel = 0;

        // The MIDI velocity used for the MIDI output device
        byte velocity = 127;

        public MainPage()
        {
            this.InitializeComponent();

            var viewTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            viewTitleBar.BackgroundColor = Windows.UI.Colors.Black;
            viewTitleBar.ButtonBackgroundColor = Windows.UI.Colors.Black;

            // Set the initial instrument of the keyboard to Acoustic Grand Piano in channel 0
            instrumentChange = new MidiProgramChangeMessage(0, 0);

            // Get the MIDI output device
            GetMidiOutputDevicesAsync();

            // Reopen the midi connection when resuming after a suspension
            Application.Current.Resuming += new EventHandler<Object>(App_Resuming);

            // Close the midi connection on suspending the app
            Application.Current.Suspending += new SuspendingEventHandler(App_Suspending);

            // Inital the VirtualKey
            VK_AKey = VirtualKey.A;
            VK_ASharpKey = VirtualKey.S;
            VK_BKey = VirtualKey.D;
            VK_BSharpKey = VirtualKey.F;
            VK_CKey = VirtualKey.G;
            VK_CSharpKey = VirtualKey.H;
            VK_DKey = VirtualKey.J;
            VK_DSharpKey = VirtualKey.K;
            VK_EKey = VirtualKey.L;
            VK_ESharpKey = (VirtualKey)186;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataTransferManager.GetForCurrentView().DataRequested += OnShareDataRequested;
        }

        async private void OnShareDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var dp = args.Request.Data;
            var deferral = args.Request.GetDeferral();
            var photoFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/piano-keyboard-1427715038-hero-wide-0.jpg"));
            dp.Properties.Title = "MUSIC KEYBOARD";
            dp.Properties.Description = "SHARE YOUR MUSIC";
            dp.SetText("Share task completed.");
            dp.SetStorageItems(new List<StorageFile> { photoFile });
            deferral.Complete();
        }

        /// <summary>
        /// Eventhandler to clean up the MIDI connection, when the app is suspended.
        /// The object midiOutPort is disposed and set to null
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_Suspending(object sender, SuspendingEventArgs e)
        {
            try
            {
                // Dispose the Midi Output port
                midiOutPort.Dispose();

                // Set the midiOutPort to null
                midiOutPort = null;
            }
            catch
            {
                // Do noting. A cleanup has already been made
            }
        }

        /// <summary>
        /// Eventhandler to restore connection to the MIDI device when app is resuming after suspension.
        /// The method EnumerateMidiOutputDevices() will be called.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void App_Resuming(object sender, object e)
        {
            // Call method to set up output midi device
            await EnumerateMidiOutputDevices();
        }

        /// <summary>
        /// Method to call EnumarateMidiOutputDevice from the contructor of the MainPage.
        /// EnumerateMidiOutputDevices() has to be called via a await call, and that cannot be done from the constructor.
        /// </summary>
        private async void GetMidiOutputDevicesAsync()
        {
            await EnumerateMidiOutputDevices();
        }


        /// <summary>
        /// Method to enumrate all MIDI output devices connected and to setup the the first MIDI output device found.
        /// </summary>
        /// <returns></returns>
        private async Task EnumerateMidiOutputDevices()
        {
            // Create the query string for finding all MIDI output devices using MidiOutPort.GetDeviceSelector()
            string midiOutportQueryString = MidiOutPort.GetDeviceSelector();

            // Find all MIDI output devices and collect it in a DeviceInformationCollection using FindAllAsync
            DeviceInformationCollection midiOutputDevices = await DeviceInformation.FindAllAsync(midiOutportQueryString);

            // If the size of the midiOutputDevice colloction is xero,
            // set the StatusTextBlock foreground color to red and the text property to "No MIDI output devices found"
            // and return.
            // Else set the StatusTextBlock foreground color to green and the text property to midiOutputdevices[0].Name
            if (midiOutputDevices.Count == 0)
            {
                // Set the StatusTextBlock foreground color to red
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);

                // Set the StatusTextBlock text to "No MIDI output devices found"
                StatusTextBlock.Text = "No MIDI output devices found";
                return;
            }
            else
            {
                // Set the StatusTextBlock foreground color to green
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

                // Set the StatusTextBlock text to the name of the first item in midiOutputDevices collection
                //StatusTextBlock.Text = midiOutputDevices[0].Name;
            }

            // Create an instance of DeviceInformation and set it to the first midi device in DeviceInformationCollection, midiOutputDevices
            DeviceInformation devInfo = midiOutputDevices[0];

            // Return if DeviceInformation, devInfo, is null
            if (devInfo == null)
            {
                // Set the midi status TextBlock
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                StatusTextBlock.Text = "No device information of MIDI output";
                return;
            }

            // Set the IMidiOutPort for the output midi device by calling MidiOutPort.FromIdAsync passing the Id property of the DevicInformation
            midiOutPort = await MidiOutPort.FromIdAsync(devInfo.Id);

            // Return if midiOutPort is null
            if (midiOutPort == null)
            {
                // Set the midi status TextBlock
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                StatusTextBlock.Text = "Unable to create MidiOutPort from output device";
                return;
            }

            // Send the Program Change midi message to port of the output midi device
            // to set the initial instrument to Acoustic Grand Piano.
            midiOutPort.SendMessage(instrumentChange);
        }

        /// <summary>
        /// Eventhandler for key released at the keyboard.
        /// The specific key is extracted and the according pitch is found.
        /// A MIDI message Note Off is created from the channel field and velocity field.
        /// The Note Off message is send to the MIDI output device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Key_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // Field to hold the pitch of the note
            byte note;

            // Extract the key being pressed
            Rectangle keyPressed = (Rectangle)sender;

            // Get the name of the key and store it in a string, keyPressedName
            string keyPressedName = keyPressed.Name;

            if (keyPressedName.Length != 4)
            {
                keyPressed.Fill = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0));
            } else
            {
                keyPressed.Fill = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
            }

            // Switch/Case to set the pitch depending of the key pressed
            switch (keyPressedName)
            {
                case "AKey":
                    note = (byte)(60 + (octaveInterval * 12));
                    break;
                case "ASharpKey":
                    note = (byte)(61 + (octaveInterval * 12));
                    break;
                case "BKey":
                    note = (byte)(62 + (octaveInterval * 12));
                    break;
                case "BSharpKey":
                    note = (byte)(63 + (octaveInterval * 12));
                    break;
                case "CKey":
                    note = (byte)(64 + (octaveInterval * 12));
                    break;
                case "CSharpKey":
                    note = (byte)(65 + (octaveInterval * 12));
                    break;
                case "DKey":
                    note = (byte)(66 + (octaveInterval * 12));
                    break;
                case "DSharpKey":
                    note = (byte)(67 + (octaveInterval * 12));
                    break;
                case "EKey":
                    note = (byte)(68 + (octaveInterval * 12));
                    break;
                case "ESharpKey":
                    note = (byte)(69 + (octaveInterval * 12));
                    break;
                default:
                    note = (byte)(60 + (octaveInterval * 12));
                    break;
            }

            // Create the Note Off message to send to the MIDI output device
            IMidiMessage midiMessageToSend = new MidiNoteOffMessage(channel, note, velocity);

            // Send the Note Off MIDI message to the midiOutPort
            midiOutPort.SendMessage(midiMessageToSend);
        }

        /// <summary>
        /// Eventhandler for key pressed at the keyboard.
        /// The specific key is extracted and the according pitch is found.
        /// A MIDI message Note On is created from the channel field and velocity field.
        /// The Note On message is send to the MIDI output device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Key_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Field to hold the pitch of the note
            byte note;

            // Extract the key being pressed
            Rectangle keyPressed = (Rectangle)sender;
            keyPressed.Fill = new SolidColorBrush(Color.FromArgb(50, 250, 0, 0));
            // Get the name of the key and store it in a string, keyPressedName
            string keyPressedName = keyPressed.Name;

            // Switch/Case to set the pitch depending of the key pressed
            switch (keyPressedName)
            {
                case "AKey":
                    note = (byte)(60 + (octaveInterval * 12));
                    break;
                case "ASharpKey":
                    note = (byte)(61 + (octaveInterval * 12));
                    break;
                case "BKey":
                    note = (byte)(62 + (octaveInterval * 12));
                    break;
                case "BSharpKey":
                    note = (byte)(63 + (octaveInterval * 12));
                    break;
                case "CKey":
                    note = (byte)(64 + (octaveInterval * 12));
                    break;
                case "CSharpKey":
                    note = (byte)(65 + (octaveInterval * 12));
                    break;
                case "DKey":
                    note = (byte)(66 + (octaveInterval * 12));
                    break;
                case "DSharpKey":
                    note = (byte)(67 + (octaveInterval * 12));
                    break;
                case "EKey":
                    note = (byte)(68 + (octaveInterval * 12));
                    break;
                case "ESharpKey":
                    note = (byte)(69 + (octaveInterval * 12));
                    break;
                default:
                    note = (byte)(60 + (octaveInterval * 12));
                    break;
            }

            // Create the Note On message to send to the MIDI output device
            IMidiMessage midiMessageToSend = new MidiNoteOnMessage(channel, note, velocity);

            // Send the Note On MIDI message to the midiOutPort
            midiOutPort.SendMessage(midiMessageToSend);     
        }

        /// <summary>
        /// Eventhandler for the buttons that select the instrument being played.
        /// Each buttons has a different name, that will be used to get a different value of MIDI instrument using a Switch/Case.
        /// A Program Change message is created from that and the MIDI channel and the message is sent to the MIDI output device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Selection_Click(object sender, RoutedEventArgs e)
        {
            // Define a byte to hold the MIDI instrument number to be used in the Program Change MIDI message
            byte midiInstrument;
            
            // Create a Button element to get the pressed instrument button
            Button instrumentNameButton = (Button)sender;

            // Create a string to hold the name of the pressed button to be used in a switch/case
            string instrumentName = instrumentNameButton.Name;

            // Depending on the name of the button pressed, set a new MIDI instrument and frequency interval for the instrument
            switch (instrumentName)
            {
                case "Piano":
                    // Set the MIDI instrument number to 0, Piano
                    midiInstrument = 0;

                    // Set the octaveInterval to 0 to set the frequency interval around the middle C
                    octaveInterval = 0;
                    break;
                case "Trombone":
                    // Set the MIDI instrument number to 57, Trombone (58-1)
                    midiInstrument = 57;

                    // Set the octaveInterval to -1 to set the frequency interval around one octave lower than the middle C
                    octaveInterval = -1;
                    break;
                case "Trumpet":
                    // Set the MIDI instrument number to 56, Trumpet (57-1)
                    midiInstrument = 56;

                    // Set the octaveInterval to 0 to set the frequency interval around the middle C
                    octaveInterval = 0;
                    break;
                case "Flute":
                    // Set the MIDI instrument number to 73, Trumpet (74-1)
                    midiInstrument = 73;

                    // Set the octaveInterval to 1 to set the frequency interval around one octave higher than the middle C
                    octaveInterval = 1;
                    break;

                // Default value will be equal to piano
                default:
                    // Set the MIDI instrument number to 0
                    midiInstrument = 0;

                    // Set the octaveInterval to 0 to set the frequency interval around the middle C
                    octaveInterval = 0;
                    break;
            }

            // Create the new Program Change message with the new selected instrument
            instrumentChange = new MidiProgramChangeMessage(channel, midiInstrument);

            // Send the Program Change midi message to port of the output midi device.
            midiOutPort.SendMessage(instrumentChange);
        }

        DispatcherTimer timer = null;
        int len = 0;
        int index = 0;
        string text;

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".txt");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file == null) return;
            text = await Windows.Storage.FileIO.ReadTextAsync(file);
            //var i = new MessageDialog(text).ShowAsync();
            len = text.Length;
            index = 0;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.3);
            timer.Tick += timer_tick;
            timer.Start();
        }
        private void timer_tick(object sender, object e)
        {
            //var i = new MessageDialog(text[index].ToString()).ShowAsync();
           
            if (index > 0) {
                int b = text[index-1] - '0';
                Rectangle rec_after = (Rectangle)KeyboardGrid.Children.ElementAt(b-1);
                if (b % 2 == 0)
                {
                    rec_after.Fill = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0));
                }
                else
                {
                    rec_after.Fill = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
                }
                byte note1 = (byte)(60 + b + (octaveInterval * 12));
                IMidiMessage midiMessageToSend1 = new MidiNoteOffMessage(channel, note1, velocity);
                midiOutPort.SendMessage(midiMessageToSend1);
            }

            if (index == len)
            {
                timer.Stop();
                return;
            }

            int a = text[index] - '0';

            Rectangle rec_before = (Rectangle)KeyboardGrid.Children.ElementAt(a-1);
            rec_before.Fill = new SolidColorBrush(Color.FromArgb(50, 250, 0, 0));
            byte note = (byte)(60 + a + (octaveInterval * 12));
            IMidiMessage midiMessageToSend = new MidiNoteOnMessage(channel, note, velocity);
            midiOutPort.SendMessage(midiMessageToSend);

            index++;
            /*if (len <= index)
            {
                timer.Stop();
                midiMessageToSend = new MidiNoteOffMessage(channel, note, velocity);
                midiOutPort.SendMessage(midiMessageToSend);
            }*/
        }

        private async void PictureButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".jpg");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    // Set the image source to the selected bitmap 
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(fileStream);
                    ImageBrush imageBrush = new ImageBrush();
                    imageBrush.ImageSource = bitmapImage;
                    key.Background = imageBrush;
                }
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private void FullSreenButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            view.TryEnterFullScreenMode();
            FullSreenButton.Visibility = Visibility.Collapsed;
            BackToWindowButton.Visibility = Visibility.Visible;
        }

        private void BackToWindowButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            view.ExitFullScreenMode();
            FullSreenButton.Visibility = Visibility.Visible;
            BackToWindowButton.Visibility = Visibility.Collapsed;
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (e.Key == VK_AKey)
                Key_PointerPressed(AKey, null);
            else if (e.Key == VK_ASharpKey)
                Key_PointerPressed(ASharpKey, null);
            else if (e.Key == VK_BKey)
                Key_PointerPressed(BKey, null);
            else if (e.Key == VK_BSharpKey)
                Key_PointerPressed(BSharpKey, null);
            else if (e.Key == VK_CKey)
                Key_PointerPressed(CKey, null);
            else if (e.Key == VK_CSharpKey)
                Key_PointerPressed(CSharpKey, null);
            else if (e.Key == VK_DKey)
                Key_PointerPressed(DKey, null);
            else if (e.Key == VK_DSharpKey)
                Key_PointerPressed(DSharpKey, null);
            else if (e.Key == VK_EKey)
                Key_PointerPressed(EKey, null);
            else if (e.Key == VK_ESharpKey)
                Key_PointerPressed(ESharpKey, null);
        }

        protected override void OnKeyUp(KeyRoutedEventArgs e)
        {
            if (e.Key == VK_AKey)
                Key_PointerReleased(AKey, null);
            else if (e.Key == VK_ASharpKey)
                Key_PointerReleased(ASharpKey, null);
            else if (e.Key == VK_BKey)
                Key_PointerReleased(BKey, null);
            else if (e.Key == VK_BSharpKey)
                Key_PointerReleased(BSharpKey, null);
            else if (e.Key == VK_CKey)
                Key_PointerReleased(CKey, null);
            else if (e.Key == VK_CSharpKey)
                Key_PointerReleased(CSharpKey, null);
            else if (e.Key == VK_DKey)
                Key_PointerReleased(DKey, null);
            else if (e.Key == VK_DSharpKey)
                Key_PointerReleased(DSharpKey, null);
            else if (e.Key == VK_EKey)
                Key_PointerReleased(EKey, null);
            else if (e.Key == VK_ESharpKey)
                Key_PointerReleased(ESharpKey, null);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            if (Piano.FocusState == FocusState.Unfocused && Trombone.FocusState == FocusState.Unfocused
                && Trumpet.FocusState == FocusState.Unfocused && Flute.FocusState == FocusState.Unfocused
                && textBox.FocusState == FocusState.Unfocused)
                textBox.Focus(FocusState.Pointer);
            // Piano.Focus(FocusState.Pointer);
        }

        private void textBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            string str = ((TextBox)sender).Name.ToString();
            switch (str)
            {
                case "textBox0":
                    VK_AKey = e.Key;
                    break;
                case "textBox1":
                    VK_ASharpKey = e.Key;
                    break;
                case "textBox2":
                    VK_BKey = e.Key;
                    break;
                case "textBox3":
                    VK_BSharpKey = e.Key;
                    break;
                case "textBox4":
                    VK_CKey = e.Key;
                    break;
                case "textBox5":
                    VK_CSharpKey = e.Key;
                    break;
                case "textBox6":
                    VK_DKey = e.Key;
                    break;
                case "textBox7":
                    VK_DSharpKey = e.Key;
                    break;
                case "textBox8":
                    VK_EKey = e.Key;
                    break;
                case "textBox9":
                    VK_ESharpKey = e.Key;
                    break;
            }
            ((TextBox)sender).Text = e.Key.ToString();

        }
    }
}
