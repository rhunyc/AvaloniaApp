using Avalonia.Controls;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;
using LibUsbDotNet;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;


namespace AvaloniaApplication1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Define Vendor and Product ID for ACR122U
            int vendorId = 0x072F;  // ACS Vendor ID
            int productId = 0x2200; // ACR122U Product ID

            var devices = UsbDevice.AllDevices;

            if (devices.Count > 0)
            {
                //
            }

            UsbDevice device = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(vendorId, productId));

            if (device == null)
            {
                Console.WriteLine("ACR122U not found.");
                return;
            }

            lblInfo.Content="ACR122U found!";

            

            device.Close();
        }

        public static UsbDevice MyUsbDevice;
    }
}