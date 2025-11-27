using Messages.Connection;
using Messages.EventArgs.Network;
using Network.Thread;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Timers;
using Utils = TeamScreenClientPortable.Utils;

namespace TeamScreenClientPortableWPF.Views
{
    /// <summary>
    /// Interaction logic for RemoteWindow.xaml
    /// </summary>
    public partial class RemoteWindow : Window
    {
        private float _ratio = 1.0f;
        private System.Drawing.Rectangle _bounds = System.Drawing.Rectangle.Empty;
        private System.Timers.Timer? _onlineCheckTimer;
        private ClientThread? _clientThread;
        private string _systemId;
        private string _password;
        private readonly Utils.Config.Manager _configManager;

        public RemoteWindow(string systemId, string password)
        {
            InitializeComponent();
            
            _systemId = systemId;
            _password = password;
            Title = $"Host: {_systemId}";
            
            _configManager = new Utils.Config.Manager();
            
            // Initialize online check timer
            _onlineCheckTimer = new System.Timers.Timer(5000);
            _onlineCheckTimer.Elapsed += OnlineCheckTimer_Elapsed;
        }

        public void SetThread(ClientThread clientThread)
        {
            _clientThread = clientThread;
        }

        public void Start()
        {
            if (_clientThread != null)
            {
                _clientThread.Events.OnScreenshotReceived += Events_OnScreenshotReceived;
                
                // Send start screen sharing message
                _clientThread.Manager.sendMessage(new StartScreenSharingMessage 
                { 
                    SymmetricKey = _clientThread.Manager.getSymmetricKeyForRemoteId(_systemId), 
                    HostSystemId = _systemId, 
                    ClientSystemId = _clientThread.Manager.SystemId 
                });
                
                _onlineCheckTimer?.Start();
            }
        }

        private void OnlineCheckTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_clientThread != null)
            {
                _clientThread.Manager.sendMessage(new ClientAliveMessage 
                { 
                    SymmetricKey = _clientThread.Manager.getSymmetricKeyForRemoteId(_systemId), 
                    HostSystemId = _systemId, 
                    ClientSystemId = _clientThread.Manager.SystemId 
                });
            }
        }

        private void Events_OnScreenshotReceived(object? sender, ScreenshotReceivedEventArgs e)
        {
            if (e.Fullscreen)
            {
                _bounds = e.Bounds;
            }

            // Calculate ratio for scaling
            _ratio = (float)ActualWidth / (float)_bounds.Width;

            // Update UI on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.Nothing)
                {
                    return;
                }

                if (e.SystemId == _systemId)
                {
                    try
                    {
                        using (var stream = new MemoryStream(e.Image))
                        {
                            // Convert byte array to BitmapImage
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze(); // Make it thread-safe

                            // Update the image control
                            remoteImage.Source = bitmap;

                            // Update the size of the border to match the image
                            drawingAreaBorder.Width = bitmap.Width;
                            drawingAreaBorder.Height = bitmap.Height;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle exception if needed
                        System.Diagnostics.Debug.WriteLine($"Error updating image: {ex.Message}");
                    }
                }
            });
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _onlineCheckTimer?.Stop();
            _onlineCheckTimer?.Dispose();

            if (_clientThread != null)
            {
                _clientThread.Manager.sendMessage(new StopScreenSharingMessage 
                { 
                    SymmetricKey = _clientThread.Manager.getSymmetricKeyForRemoteId(_systemId), 
                    HostSystemId = _systemId, 
                    ClientSystemId = _clientThread.Manager.SystemId 
                });
            }
        }

        // Handle mouse events on the image
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (_clientThread != null)
            {
                var position = e.GetPosition(remoteImage);
                var x = (int)(position.X / _ratio);
                var y = (int)(position.Y / _ratio);

                Messages.Connection.OneWay.MouseClickMessage.ButtonType buttonType = e.ChangedButton switch
                {
                    MouseButton.Left => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Left,
                    MouseButton.Middle => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Middle,
                    MouseButton.Right => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Right,
                    _ => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Left
                };

                _clientThread.Manager.sendMessage(new Messages.Connection.OneWay.MouseClickMessage 
                { 
                    SymmetricKey = _clientThread.Manager.getSymmetricKeyForRemoteId(_systemId), 
                    Down = true, 
                    Button = buttonType, 
                    ClientSystemId = _clientThread.Manager.SystemId, 
                    HostSystemId = _systemId, 
                    X = x, 
                    Y = y 
                });
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (_clientThread != null)
            {
                var position = e.GetPosition(remoteImage);
                var x = (int)(position.X / _ratio);
                var y = (int)(position.Y / _ratio);

                Messages.Connection.OneWay.MouseClickMessage.ButtonType buttonType = e.ChangedButton switch
                {
                    MouseButton.Left => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Left,
                    MouseButton.Middle => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Middle,
                    MouseButton.Right => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Right,
                    _ => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Left
                };

                _clientThread.Manager.sendMessage(new Messages.Connection.OneWay.MouseClickMessage 
                { 
                    SymmetricKey = _clientThread.Manager.getSymmetricKeyForRemoteId(_systemId), 
                    Up = true, 
                    Button = buttonType, 
                    ClientSystemId = _clientThread.Manager.SystemId, 
                    HostSystemId = _systemId, 
                    X = x, 
                    Y = y 
                });
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_clientThread != null)
            {
                var position = e.GetPosition(remoteImage);
                var x = position.X / _ratio;
                var y = position.Y / _ratio;

                _clientThread.Manager.SendMessage(new Messages.Connection.OneWay.MouseMoveMessage 
                { 
                    SymmetricKey = _clientThread.Manager.GetSymmetricKeyForRemoteId(_systemId), 
                    ClientSystemId = _clientThread.Manager.SystemId, 
                    HostSystemId = _systemId, 
                    X = (int)x, 
                    Y = (int)y 
                });
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (_clientThread != null)
            {
                var position = e.GetPosition(remoteImage);
                var x = (int)(position.X / _ratio);
                var y = (int)(position.Y / _ratio);

                Messages.Connection.OneWay.MouseClickMessage.ButtonType buttonType = e.ChangedButton switch
                {
                    MouseButton.Left => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Left,
                    MouseButton.Middle => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Middle,
                    MouseButton.Right => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Right,
                    _ => Messages.Connection.OneWay.MouseClickMessage.ButtonType.Left
                };

                _clientThread.Manager.SendMessage(new Messages.Connection.OneWay.MouseClickMessage 
                { 
                    SymmetricKey = _clientThread.Manager.GetSymmetricKeyForRemoteId(_systemId), 
                    DoubleClick = true, 
                    Button = buttonType, 
                    ClientSystemId = _clientThread.Manager.SystemId, 
                    HostSystemId = _systemId, 
                    X = x, 
                    Y = y 
                });
            }

            base.OnMouseDoubleClick(e);
        }

        private void BtnCtrlAltDel_Click(object sender, RoutedEventArgs e)
        {
            if (_clientThread != null)
            {
                _clientThread.Manager.SendMessage(new Messages.Connection.OneWay.KeyMessage 
                { 
                    SymmetricKey = _clientThread.Manager.GetSymmetricKeyForRemoteId(_systemId), 
                    Key = 46, // VK_DELETE
                    ClientSystemId = _clientThread.Manager.SystemId, 
                    HostSystemId = _systemId, 
                    Alt = true, 
                    Control = true, 
                    Shift = false, 
                    Mode = Messages.Connection.OneWay.KeyMessage.KeyMode.Down 
                });
                
                _clientThread.Manager.SendMessage(new Messages.Connection.OneWay.KeyMessage 
                { 
                    SymmetricKey = _clientThread.Manager.GetSymmetricKeyForRemoteId(_systemId), 
                    Key = 46, // VK_DELETE
                    ClientSystemId = _clientThread.Manager.SystemId, 
                    HostSystemId = _systemId, 
                    Alt = true, 
                    Control = true, 
                    Shift = false, 
                    Mode = Messages.Connection.OneWay.KeyMessage.KeyMode.Up 
                });
            }
        }

        private void BtnSendKey_Click(object sender, RoutedEventArgs e)
        {
            if (_clientThread != null && int.TryParse(txtKeyCode.Text, out int keyCode))
            {
                _clientThread.Manager.SendMessage(new Messages.Connection.OneWay.KeyMessage 
                { 
                    SymmetricKey = _clientThread.Manager.GetSymmetricKeyForRemoteId(_systemId), 
                    Key = (uint)keyCode, 
                    ClientSystemId = _clientThread.Manager.SystemId, 
                    HostSystemId = _systemId, 
                    Alt = false, 
                    Control = false, 
                    Shift = false, 
                    Mode = Messages.Connection.OneWay.KeyMessage.KeyMode.Down 
                });
                
                _clientThread.Manager.SendMessage(new Messages.Connection.OneWay.KeyMessage 
                { 
                    SymmetricKey = _clientThread.Manager.GetSymmetricKeyForRemoteId(_systemId), 
                    Key = (uint)keyCode, 
                    ClientSystemId = _clientThread.Manager.SystemId, 
                    HostSystemId = _systemId, 
                    Alt = false, 
                    Control = false, 
                    Shift = false, 
                    Mode = Messages.Connection.OneWay.KeyMessage.KeyMode.Up 
                });
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}