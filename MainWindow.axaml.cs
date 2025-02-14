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

namespace AvaloniaApplication1
{
    

    public partial class MainWindow : Window
    {
        public IDeviceMonitor? Monitor { get; set; } = null;

        public MainWindow()
        {
            InitializeComponent();

            // Define Vendor and Product ID for ACR122U
            int vendorId = 0x072F;  // ACS Vendor ID
            int productId = 0x2200; // ACR122U Product ID

            //ACRReader = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(vendorId, productId));
            //lblInfo.Content = ACRReader == null ? "ACR112U not found." : "ACR122U found!";
            var factory = DeviceMonitorFactory.Instance;
            Monitor = factory.Create(SCardScope.System);

            Monitor.Initialized += OnInitialized;
            Monitor.StatusChanged += OnStatusChanged;
            Monitor.MonitorException += OnMonitorException;

            Monitor.Start();



            Task.Run(() => {
                
                //PollForNFCTag();
            });



        }

        private void Monitor_StatusChanged(object sender, StatusChangeEventArgs e)
        {
            Console.Write("Event");
        }

        static void PollForNFCTag()
        {
            var contextFactory = ContextFactory.Instance;
            using (var context = contextFactory.Establish(SCardScope.System))
            {
                try
                {
                    // Get available readers
                    var readers = context.GetReaders();

                    if (readers.Length == 0)
                    {
                        Console.WriteLine("No readers found.");
                        return;
                    }

                    Console.WriteLine("Available readers:");
                    foreach (var reader in readers)
                    {
                        Console.WriteLine(reader);
                    }

                    // Use the first available reader
                    string readerName = readers[0];



                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PCSC Exception: {ex.Message}");
                }
            }
        }

        private static void WaitUntilSpaceBarPressed()
        {
            while (Console.ReadKey().Key != ConsoleKey.Spacebar) { }
        }

        private static void OnMonitorException(object sender, DeviceMonitorExceptionEventArgs args)
        {
            Console.WriteLine($"Exception: {args.Exception}");
        }

        private static void OnStatusChanged(object sender, DeviceChangeEventArgs e)
        {
            foreach (var removed in e.DetachedReaders)
            {
                Console.WriteLine($"Reader detached: {removed}");
            }

            foreach (var added in e.AttachedReaders)
            {
                Console.WriteLine($"New reader attached: {added}");
            }
        }

        private static void OnInitialized(object sender, DeviceChangeEventArgs e)
        {
            Console.WriteLine("Current connected readers:");
            foreach (var name in e.AllReaders)
            {
                Console.WriteLine(name);
            }
        }

    }
}