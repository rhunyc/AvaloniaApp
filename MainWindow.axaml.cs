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

namespace AvaloniaApplication1
{
    public partial class MainWindow : Window
    {
        private static ACR122U acr122u = new ACR122U();
        public string tbFeedText { get; set; } = "";

        public MainWindow()
        {
            InitializeComponent();
            
            acr122u.Init(false, 50, 4, 4, 200);

            tbFeedText+= "ACR122U initialized\n";

            acr122u.CardInserted += Acr122u_CardInserted;
            acr122u.CardRemoved += Acr122u_CardRemoved;

        }

        private void Acr122u_CardRemoved()
        {
            Dispatcher.UIThread.Post(() =>
            {
                tbFeedText += "Card removed\n";
            });
        }

        private void Acr122u_CardInserted(ICardReader reader)
        {
            Dispatcher.UIThread.Post(() =>
            {
                tbFeedText += "Card inserted: " + BitConverter.ToString(acr122u.GetUID(reader)).Replace("-", "") + "\n";
            });
        }
    }
}