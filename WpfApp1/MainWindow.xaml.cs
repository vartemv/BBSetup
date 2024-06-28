using System;
using System.IO.Ports;
using System.Windows;
using WpfApp1;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Management;
using System.Text.RegularExpressions;


namespace ArduinoController
{
    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;
        private TaskCompletionSource<string> _tcs;
        private TaskCompletionSource<int> key_press;
        private bool was_opened;
        string Serial_port;

        public MainWindow()
        {
            was_opened = false;
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeButton.IsEnabled = false;
            if (!was_opened)
            {
                try
                {
                    string[] portNames = SerialPort.GetPortNames();

                    if (portNames.Length == 0)
                    {
                        ErrorTextBlock.Text = "No COM ports found.";
                    }
                    else
                    {
                        Parallel.ForEach(portNames, port =>
                        {
                            string query = $"SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%({port})%'";

                            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                            {
                                bool common = false;
                                foreach (ManagementObject obj in searcher.Get())
                                {
                                    string caption = obj["Caption"].ToString();
                                    if (GetBeforeParentheses(caption) == "Arduino Leonardo")
                                    {
                                        Serial_port = GetBetweenParentheses(caption);
                                        common = true;
                                        break;
                                    }else if(GetBeforeParentheses(caption) == "Arduino Leonardo")
                                    {

                                    }
                                }
                                if (!common)
                                {

                                }
                            } 
                        });
                    }
                }
                catch (Exception error)
                {
                    ErrorTextBlock.Text = "An error occurred: " + error.Message;
                }
            }
            if (Serial_port != null && !was_opened)
            {
                try
                {
                    _serialPort = new SerialPort(Serial_port, 9600);
                    _serialPort.DtrEnable = true;
                    _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    _serialPort.Open();
                    was_opened = true;
                    MessageTextBlock.Text = "Button box is connected successfully";
                }catch (Exception error)
                {
                    ErrorTextBlock.Text = "An error occurred: " + error.Message;
                }
            }
            else
            {
                if (was_opened)
                {
                    MessageTextBlock.Text = "Button box is connected";
                }
                else
                {
                    MessageTextBlock.Text = "Button box wasn't found. Connect it to the PC and try again";
                }
            }
            ChangeButton.IsEnabled = true;
        }

        private async void ChangeButton_Click(object sender, RoutedEventArgs e)
        {

            if (was_opened)
            {
                ConnectButton.IsEnabled = false;

                _tcs = new TaskCompletionSource<string>();
                key_press = new TaskCompletionSource<int>();

                WriteIntToSerialPort(1);

                MessageTextBlock.Text = "Press the button on the button box you would like to change";

                await _tcs.Task;

                MessageTextBlock.Text = "Press the button on the keyboard";

                this.KeyDown += Window_KeyDown;
                this.Focus();

                int new_value = await key_press.Task;

                WriteIntToSerialPort(new_value);

                MessageTextBlock.Text = "Changed";

                ConnectButton.IsEnabled = true;
            }
            else
            {
                MessageTextBlock.Text = "You have to connect before changing values";
            }

        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            string receivedData = _serialPort.ReadExisting();
            Console.WriteLine("Data received is " + receivedData);
            Dispatcher.Invoke(() =>
            {
                _tcs.TrySetResult(receivedData);
            });
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            int new_value = KeyInterop.VirtualKeyFromKey(e.Key);
            key_press.TrySetResult(new_value);
            this.KeyDown -= Window_KeyDown;
            Console.WriteLine("Key pressed " + e.Key.ToString() + " ascii " + new_value);
        }

        private void WriteIntToSerialPort(int value)
        {
            string valueString = value.ToString();
            _serialPort.WriteLine(valueString);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (was_opened)
                _serialPort.Close();

            App.Current.Shutdown();
        }

        static string GetBeforeParentheses(string input)
        {
            int index = input.IndexOf('(');
            return index == -1 ? input : input.Substring(0, index).Trim();
        }

        static string GetBetweenParentheses(string input)
        {
            Match match = Regex.Match(input, @"\(([^)]*)\)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private void ErrorTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ErrorTextBlock.Text = "";
        }

    }
}