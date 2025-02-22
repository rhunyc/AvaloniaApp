using Avalonia.Controls;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using Avalonia.Input;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCSC.Exceptions;
using Tmds.DBus.Protocol;
using PCSC;
using PCSC.Exceptions;
using PCSC.Utils;
using PCSC.Monitoring;
using Sydesoft.NfcDevice;
using Avalonia.Threading;
using System.Reflection.PortableExecutable;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using ReactiveUI;
using System.Windows.Input;
using Avalonia.Interactivity;
using System.ComponentModel;

namespace AvaloniaApplication1
{
    public partial class MainWindow : Window
    {
        private static UsbDevice ACRReader;
        public ICommand DoThingCommand { get; }
        public string tbFeedText { get; set; } = "";

        public MainWindow()
        {
            InitializeComponent();

            int vendorId = 0x072F;  // ACS Vendor ID
            int productId = 0x2200; // ACR122U Product ID
            //ACRReader = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(vendorId, productId));

            


        }

        public void GetStatusThing(object sender, RoutedEventArgs e) => Task.Run(() => { CheckTagPresence(ACRReader); });
        private void StartPolling(object sender, RoutedEventArgs e) => Task.Run(() => StartFullAutoPolling(ACRReader));
        private void StopPolling(object sender, RoutedEventArgs e) => Task.Run(() => StopAutoPolling(ACRReader));
        private void Scan(object sender, RoutedEventArgs e) => Task.Run(() => { QueryCardType(ACRReader); });

        public void ConnectThing(object sender, RoutedEventArgs e)
        {
            Task.Run(() => {
                UpdateText("Listing all connected USB devices...");

                foreach (UsbRegistry usbRegistry in UsbDevice.AllDevices)
                {
                    if (usbRegistry.Open(out UsbDevice device))
                    {
                        var descriptor = device.Info;
                        UpdateText($"Opened: {device.UsbRegistryInfo.Name}");
                        ACRReader = device;
                    }
                    else
                    {
                        UpdateText("Failed to open device.");
                    }
                }

            });
        }

        public void CloseThing(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                if (ACRReader != null)
                {
                    ACRReader.Close();
                    UpdateText("Reader closed!");
                }
            });
        }


        


        // Method to activate NFC reader for tag detection
         bool ActivateNfcReader(UsbDevice device)
        {
            // APDU Command to enable NFC auto-polling mode
            byte[] enablePollingCommand = new byte[]
            {
            0xFF, 0x00, 0x40, 0x01, 0x01
            };

            // Send APDU via Bulk Out
            return SendBulkOut(device, enablePollingCommand, "NFC Auto-Polling");
        }

        bool StartFullAutoPolling(UsbDevice device)
        {
            // Command to start polling (auto-polling)
            byte[] command = new byte[]
            {
                0xFF,  // Command Class (FFh)
                0x00,  // INS (00h)
                0x51,  // P1 (51h) - Command to set PICC operating parameter
                0xFF,  // New PICC Operating Parameter: All bits set to 1 (0xFF)
                0x00   // Le (00h) - Length of expected response (no additional data)
            };

            byte[] responseBuffer = new byte[64]; // Buffer for response
            UpdateText("Trying to activate reader...");

            if (SendBulkOut(device, command, "Polling") && ReceiveBulkIn(device, responseBuffer))
            {
                if (responseBuffer[0] == 0x00 && responseBuffer[1] == 0x00)
                {
                    // Successfully entered polling mode
                    UpdateText("Polling Mode Active");
                    return true; // Polling mode is active
                }
                else
                {
                    UpdateText("Polling Mode Not Active or Error");
                    return false; // Error or polling not active
                }
            }

            UpdateText("Failed to start polling.");
            return false;
        }

        // Method to check if an NFC tag is present
        bool CheckTagPresence(UsbDevice device)
        {
            byte[] command = { 0xFF, 0x00, 0x50, 0x00, 0x00 }; // Get Status Command
            byte[] responseBuffer = new byte[64]; // Buffer for response

            if (SendBulkOut(device, command, "TagPresence") && ReceiveBulkIn(device, responseBuffer))
            {
                // Check if status byte indicates a tag presence (this may vary depending on reader/model)
                if (responseBuffer[0] == 0x00 && responseBuffer[1] == 0x00)
                {
                    UpdateText("Tag detected!");
                    return true; // Tag is detected
                }
                else
                {
                    UpdateText("No tag detected or error in response.");
                    return false; // No tag detected
                }
            }

            UpdateText("Failed to get tag presence status.");
            return false;
        }

        bool QueryCardType(UsbDevice device)
        {
            byte[] command = { 0xFF, 0x00, 0x52, 0x00, 0x08 }; // Query Card Type
            byte[] responseBuffer = new byte[64]; // Buffer for response

            if (SendBulkOut(device, command, "Query") && ReceiveBulkIn(device, responseBuffer))
            {
                if (responseBuffer[0] == 0x00 && responseBuffer[1] == 0x00)
                {
                    // Check response to find out the card type
                    UpdateText($"Card Detected: {BitConverter.ToString(responseBuffer)}");
                    return true;
                }
                else
                {
                    UpdateText("No card detected or error in response.");
                    return false;
                }
            }

            UpdateText("Failed to query card type.");
            return false;
        }

        bool GetCardUID(UsbDevice device)
        {
            byte[] command = { 0xFF, 0x00, 0x52, 0x00, 0x00 }; // Command to retrieve UID
            byte[] responseBuffer = new byte[16]; // Increase buffer size to capture UID properly

            if (SendBulkOut(device, command, "GetUID") && ReceiveBulkIn(device, responseBuffer))
            {
                if (responseBuffer[0] == 0x00 && responseBuffer[1] == 0x00)
                {
                    // Extract UID from the response, typically after the first two bytes
                    byte[] uid = new byte[8]; // Assuming the UID is 8 bytes long for Mifare cards
                    Array.Copy(responseBuffer, 2, uid, 0, 8); // Copy the UID into the array

                    UpdateText($"Card UID: {BitConverter.ToString(uid)}");
                    return true;
                }
                else
                {
                    UpdateText("Failed to retrieve valid UID.");
                    return false;
                }
            }

            UpdateText("Failed to retrieve card UID.");
            return false;
        }

        bool GetReaderStatus(UsbDevice device)
        {
            byte[] command = { 0xFF, 0x00, 0x50, 0x00, 0x00 }; // Get Parameters command
            byte[] responseBuffer = new byte[10];  // Adjust buffer size if needed

            if (SendBulkOut(device, command, "Get Reader Status") && ReceiveBulkIn(device, responseBuffer))
            {
                UpdateText($"Reader Status Response: {BitConverter.ToString(responseBuffer)}");
                return true;
            }

            UpdateText("Failed to get reader status.");
            return false;
        }

        bool StopAutoPolling(UsbDevice device)
        {
            byte[] command = { 0xE0, 0x00, 0x00, 0x40, 0x01, 0x00 }; // Stop auto-polling command
            byte[] responseBuffer = new byte[10];

            if (SendBulkOut(device, command, "StopPolling") && ReceiveBulkIn(device, responseBuffer))
            {
                UpdateText($"Auto-Polling Stopped: {BitConverter.ToString(responseBuffer)}");
                return true;
            }

            UpdateText("Failed to stop auto-polling.");
            return false;
        }

        // Sends APDU command via Bulk Out transfer
        bool SendBulkOut(UsbDevice device, byte[] command, string operation)
        {
            int bytesWritten;
            //UsbEndpointWriter writer = device.OpenEndpointWriter(WriteEndpointID.Ep01);
            UsbEndpointWriter writer = device.OpenEndpointWriter(WriteEndpointID.Ep02);

            ErrorCode ec = writer.Write(command, 25000, out bytesWritten);
            if (ec == ErrorCode.None)
            {
                UpdateText($"{operation} command sent successfully, bytes written: {bytesWritten}");
                return true;
            }
            else
            {
                UpdateText($"{operation} command failed: {ec}");
                return false;
            }
        }

        // Reads response via Bulk In transfer
         bool ReceiveBulkIn(UsbDevice device, byte[] responseBuffer)
        {
            int bytesRead;
            UsbEndpointReader reader = device.OpenEndpointReader(ReadEndpointID.Ep01);

            ErrorCode ec = reader.Read(responseBuffer, 25000, out bytesRead);
            if (ec == ErrorCode.None)
            {
                UpdateText($"Response received: {BitConverter.ToString(responseBuffer)}");
                return true;
            }
            else
            {
                UpdateText($"Failed to read response: {ec}");
                return false;
            }
        }

        private void UpdateText(string text)
        {
            Dispatcher.UIThread.Post(() =>
            {
                tbFeed.Text += $"{text}\n";
            });
        }

        private void Good()
        {
            UpdateText("Listing all connected USB devices...");
            foreach (UsbRegistry usbRegistry in UsbDevice.AllDevices)
            {
                // Attempt to open the device
                if (usbRegistry.Open(out UsbDevice device))
                {
                    var descriptor = device.Info;
                    UpdateText($"Opened: {device.Info.ProductString}");

                    // Set up the control transfer using UsbSetupPacket
                    // First experiment with Request = 0x01 (example)
                    UsbSetupPacket setupPacket1 = new UsbSetupPacket
                    {
                        RequestType = 0x40, // Control transfer type (out)
                        Request = 0x52,     // Request ID (for example, a custom command)
                        Value = 0x00,       // Value parameter (if applicable)
                        Index = 0x00,       // Index parameter (if applicable)
                        Length = 0          // Length of the data (0 if no data is sent)
                    };

                    // Prepare buffer and lengthTransferred variables for the first command
                    IntPtr buffer1 = IntPtr.Zero;
                    int bufferLength1 = 0;
                    int lengthTransferred1 = 0;

                    // Perform the control transfer for the first command
                    bool result1 = device.ControlTransfer(ref setupPacket1, buffer1, bufferLength1, out lengthTransferred1);

                    if (result1)
                    {
                        UpdateText($"Control transfer 1 successful.: {lengthTransferred1}");

                    }
                    else
                    {
                        UpdateText("Control transfer 1 failed.");
                    }


                    device.Close();
                }
                else
                {
                    UpdateText("Failed to open device.");
                }
            }
        }
    }
}