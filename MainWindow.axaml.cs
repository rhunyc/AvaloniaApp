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

            Task.Run(() => {
                UpdateText("Listing all connected USB devices...");

                foreach (UsbRegistry usbRegistry in UsbDevice.AllDevices)
                {
                    if (usbRegistry.Open(out UsbDevice device))
                    {
                        var descriptor = device.Info;
                        UpdateText($"Opened: {device.Info.Descriptor}");
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
            if (ACRReader != null)
            {
                ACRReader.Close();
            }
        }

        private void DoThing(object sender, RoutedEventArgs e)
        {
            Task.Run(() => {
                UpdateText("Trying to activate reader...");
                // Activate NFC reader and attempt to detect a tag
                if (ActivateNfcReader(ACRReader))
                {
                    Thread.Sleep(500);  // Wait for the device to process

                    DetectTagStatus(ACRReader);
                }
                else
                {
                    UpdateText("Failed to activate NFC tag detection.");
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

        // Method to check if an NFC tag is present
         void DetectTagStatus(UsbDevice device)
        {
            // APDU Command to get status
            byte[] getStatusCommand = new byte[]
            {
            0xFF, 0x00, 0x50, 0x00, 0x00
            };

            // Send APDU command
            if (SendBulkOut(device, getStatusCommand, "Get Status"))
            {
                byte[] responseBuffer = new byte[10];  // Expecting a response
                if (ReceiveBulkIn(device, responseBuffer))
                {
                    // The last byte usually indicates tag presence
                    if ((responseBuffer[1] & 0x01) == 0x01)
                    {
                        UpdateText("Tag detected!");
                    }
                    else
                    {
                        UpdateText("No tag detected.");
                    }
                }
            }
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