using Messages.Connection.OneWay;
using Messages.Connection.Request;
using Messages.EventArgs.Network;
using Network.Thread;
using System;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using TeamScreenClientPortableWPF.Commands;
using TeamScreenClientPortableWPF.Views;
using Utils = TeamScreenClientPortable.Utils;

namespace TeamScreenClientPortableWPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ClientThread _clientThread;
        private readonly Utils.Config.Manager _configManager;
        private readonly System.Timers.Timer _connectionStatusTimer;
        private readonly System.Timers.Timer _onlineCheckTimer;

        // Properties for binding
        private string _serverName = string.Empty;
        private string _serverPort = string.Empty;
        private string _systemId = string.Empty;
        private string _password = string.Empty;
        private string _statusText = "Disconnected";

        public string ServerName
        {
            get => _serverName;
            set => SetProperty(ref _serverName, value);
        }

        public string ServerPort
        {
            get => _serverPort;
            set => SetProperty(ref _serverPort, value);
        }

        public string SystemId
        {
            get => _systemId;
            set => SetProperty(ref _systemId, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        // Commands
        public RelayCommand SaveCommand { get; }
        public RelayCommand ConnectCommand { get; }

        public MainViewModel()
        {
            // Initialize network components
            _clientThread = Network.Instance.Client.Instance.Thread;
            _configManager = new Utils.Config.Manager();

            // Initialize timers
            _connectionStatusTimer = new System.Timers.Timer(1000);
            _connectionStatusTimer.Elapsed += Connection_Elapsed;

            _onlineCheckTimer = new System.Timers.Timer(5000);

            // Initialize commands
            SaveCommand = new RelayCommand(SaveExecute);
            ConnectCommand = new RelayCommand(ConnectExecute);

            // Setup event handlers
            _clientThread.Events.OnHostInitalizeConnected += Events_OnHostInitalizeConnected;
            _clientThread.Events.OnClientConnected += ClientListener_OnClientConnected;
            _clientThread.Events.onNetworkError += ClientListener_onNetworkError;
            _clientThread.Events.onPeerConnected += ClientListener_onPeerConnected;
            _clientThread.Events.onPeerDisconnected += ClientListener_onPeerDisconnected;

            // Load initial configuration
            LoadConfiguration();

            // Start the client thread
            _clientThread.Start();
        }

        private void LoadConfiguration()
        {
            ServerName = _configManager.ClientConfig.ServerName;
            ServerPort = _configManager.ClientConfig.ServerPort.ToString();
        }

        private void SaveExecute(object? parameter)
        {
            try
            {
                _configManager.ClientConfig.ServerName = ServerName;
                _configManager.ClientConfig.ServerPort = Convert.ToInt32(ServerPort);
                _configManager.SaveClientConfig();
                StatusText = "Configuration saved";
            }
            catch (Exception ex)
            {
                StatusText = $"Error saving configuration: {ex.Message}";
            }
        }

        private void ConnectExecute(object? parameter)
        {
            try
            {
                var pair = _clientThread.Manager.CreateNewKeyPairKey(this.SystemId);
                _clientThread.Manager.sendMessage(
                    new InitalizeHostConnectionMessage
                    {
                        ClientSystemId = _clientThread.Manager.SystemId,
                        HostSystemId = this.SystemId,
                        ClientPublicKey = pair.PublicKey
                    }
                );
            }
            catch (Exception ex)
            {
                StatusText = $"Error connecting: {ex.Message}";
            }
        }

        private void Events_OnHostInitalizeConnected(object? sender, HostInitalizeConnectedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Connection.Request.HostConnectionMessage ms = new Messages.Connection.Request.HostConnectionMessage();
                ms.HostSystemId = e.HostSystemId;
                ms.ClientSystemId = e.ClientSystemId;
                ms.Password = _clientThread.Manager.Encode(e.HostSystemId, this.Password);
                ms.SymmetricKey = _clientThread.Manager.Encode(e.HostSystemId, _clientThread.Manager.getSymmetricKeyForRemoteId(e.HostSystemId));

                _clientThread.Manager.sendMessage(ms);
            });
        }

        private void ClientListener_onPeerDisconnected(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = "Broker Disconnected";
                _connectionStatusTimer.Start();
            });
        }

        private void ClientListener_onPeerConnected(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = "Broker Connected";
                _connectionStatusTimer.Stop();
            });
        }

        private void ClientListener_onNetworkError(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = "Network Error";
                _connectionStatusTimer.Start();
            });
        }

        private void Connection_Elapsed(object? sender, ElapsedEventArgs e)
        {
            _clientThread.Reconnect();
        }

        private void ClientListener_OnClientConnected(object? sender, ClientConnectedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.PasswordOk)
                {
                    // Open the remote window on the UI thread
                    OpenRemoteWindow(e.SystemId);
                    StatusText = $"Password Ok Connected with: {e.SystemId}";
                }
                else
                {
                    StatusText = $"Password Wrong Connection closed by: {e.SystemId}";
                }
            });
        }

        private void OpenRemoteWindow(string systemId)
        {
            var remoteWindow = new RemoteWindow(systemId, this.Password);
            remoteWindow.SetThread(_clientThread);
            remoteWindow.Show();
            remoteWindow.Start();
        }

        public void OnWindowClosing()
        {
            _clientThread.Manager.SendMessage(new DisconnectFromIntroducerMessage() { SystemId = _clientThread.Manager.SystemId });
        }
    }
}