using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;

namespace LanShooter
{
    public sealed class LanShooterSession : MonoBehaviour
    {
        private enum SessionMode
        {
            None,
            Solo,
            HostRoom,
            ClientRoom,
        }

        private const ushort DefaultPort = 7777;

        private static LanShooterSession s_Instance;

        [Header("Editable References")]
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport transport;
        [SerializeField] private LanShooterSceneContext sceneContext;

        [Header("Default Room Settings")]
        [SerializeField] private string defaultRoomName = "My Room";
        [SerializeField] private ushort defaultPort = DefaultPort;

        private string _addressInput = string.Empty;
        private string _roomName = string.Empty;
        private string _statusMessage = "Choose solo play, host a LAN room, or join one from another device.";
        private ushort _port;
        private SessionMode _sessionMode;

        public static LanShooterSession Instance => s_Instance;

        public string AddressInput
        {
            get => _addressInput;
            set => _addressInput = value;
        }

        public string PortInput
        {
            get => _port.ToString();
            set
            {
                if (ushort.TryParse(value, out var parsedPort))
                {
                    _port = parsedPort;
                }
            }
        }

        public string RoomName
        {
            get => _roomName;
            set => _roomName = value;
        }

        public string StatusMessage => _statusMessage;

        public bool IsInSession => networkManager != null && networkManager.IsListening;

        public bool IsHost => networkManager != null && networkManager.IsHost;

        public bool IsClient => networkManager != null && networkManager.IsClient;

        public bool IsSoloSession => _sessionMode == SessionMode.Solo;

        public string LocalLanAddress => GetLocalIpv4Address();

        public int ConnectedPlayerCount => LanShooterPlayer.ActivePlayers.Count;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(this);
                return;
            }

            s_Instance = this;

            _port = defaultPort;
            if (string.IsNullOrWhiteSpace(_addressInput))
            {
                _addressInput = LocalLanAddress;
            }

            if (string.IsNullOrWhiteSpace(_roomName))
            {
                _roomName = defaultRoomName;
            }

            ResolveReferences();
            EnsureNetworkManager();
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }

            UnregisterCallbacks();
        }

        public bool StartSolo()
        {
            if (!PrepareForStart())
            {
                return false;
            }

            _sessionMode = SessionMode.Solo;
            ConfigureTransport("127.0.0.1", _port, "127.0.0.1");

            if (!networkManager.StartHost())
            {
                _statusMessage = "Solo session failed to start. Check the Unity Console.";
                _sessionMode = SessionMode.None;
                return false;
            }

            _statusMessage = "Solo practice is starting.";
            return true;
        }

        public bool StartHostRoom()
        {
            if (!PrepareForStart())
            {
                return false;
            }

            _sessionMode = SessionMode.HostRoom;
            ConfigureTransport("127.0.0.1", _port, "0.0.0.0");

            if (!networkManager.StartHost())
            {
                _statusMessage = "Room creation failed. Check the Unity Console.";
                _sessionMode = SessionMode.None;
                return false;
            }

            _statusMessage = $"Room \"{_roomName}\" is live. LAN players can join via {LocalLanAddress}:{_port}.";
            return true;
        }

        public bool StartClient()
        {
            if (!PrepareForStart())
            {
                return false;
            }

            _sessionMode = SessionMode.ClientRoom;
            var address = string.IsNullOrWhiteSpace(_addressInput) ? LocalLanAddress : _addressInput.Trim();
            ConfigureTransport(address, _port);

            if (!networkManager.StartClient())
            {
                _statusMessage = "Join failed. Re-check the host IP and port.";
                _sessionMode = SessionMode.None;
                return false;
            }

            _statusMessage = $"Joining {address}:{_port} ...";
            return true;
        }

        public void Shutdown()
        {
            if (networkManager == null || !networkManager.IsListening)
            {
                _statusMessage = "No session is currently running.";
                return;
            }

            networkManager.Shutdown();
            _sessionMode = SessionMode.None;
            _statusMessage = "Session closed.";
        }

        private bool PrepareForStart()
        {
            ResolveReferences();
            if (!EnsureNetworkManager())
            {
                return false;
            }

            if (networkManager.IsListening)
            {
                _statusMessage = "You are already in a session. Leave it before switching modes.";
                return false;
            }

            if (sceneContext == null)
            {
                _statusMessage = "LanShooterSceneContext is missing from the scene.";
                return false;
            }

            if (sceneContext.PlayerPrefab == null)
            {
                _statusMessage = "The scene context does not have a player prefab assigned yet.";
                return false;
            }

            return true;
        }

        private void ResolveReferences()
        {
            if (sceneContext == null)
            {
                sceneContext = FindFirstObjectByType<LanShooterSceneContext>();
            }

            if (networkManager == null)
            {
                networkManager = FindFirstObjectByType<NetworkManager>();
            }

            if (transport == null && networkManager != null)
            {
                transport = networkManager.GetComponent<UnityTransport>();
            }
        }

        private bool EnsureNetworkManager()
        {
            ResolveReferences();

            if (networkManager == null)
            {
                var networkRoot = new GameObject("LanShooterNetworkManager");
                DontDestroyOnLoad(networkRoot);
                networkManager = networkRoot.AddComponent<NetworkManager>();
            }

            if (transport == null)
            {
                transport = networkManager.GetComponent<UnityTransport>();
            }

            if (transport == null)
            {
                transport = networkManager.gameObject.AddComponent<UnityTransport>();
            }

            networkManager.NetworkConfig.EnableSceneManagement = false;
            networkManager.NetworkConfig.NetworkTransport = transport;

            if (sceneContext != null && sceneContext.PlayerPrefab != null)
            {
                networkManager.NetworkConfig.PlayerPrefab = sceneContext.PlayerPrefab;
                RegisterNetworkPrefabIfNeeded(sceneContext.PlayerPrefab);
            }

            if (sceneContext != null && sceneContext.ProjectilePrefab != null)
            {
                RegisterNetworkPrefabIfNeeded(sceneContext.ProjectilePrefab);
            }

            if (sceneContext != null && sceneContext.EnemyPrefab != null)
            {
                RegisterNetworkPrefabIfNeeded(sceneContext.EnemyPrefab);
            }

            RegisterCallbacks();
            return networkManager.NetworkConfig.PlayerPrefab != null;
        }

        private void RegisterNetworkPrefabIfNeeded(GameObject prefab)
        {
            if (prefab == null || networkManager == null)
            {
                return;
            }

            if (!networkManager.NetworkConfig.Prefabs.Contains(prefab))
            {
                networkManager.AddNetworkPrefab(prefab);
            }
        }

        private void RegisterCallbacks()
        {
            if (networkManager == null)
            {
                return;
            }

            UnregisterCallbacks();
            networkManager.OnServerStarted += HandleServerStarted;
            networkManager.OnClientConnectedCallback += HandleClientConnected;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        private void UnregisterCallbacks()
        {
            if (networkManager == null)
            {
                return;
            }

            networkManager.OnServerStarted -= HandleServerStarted;
            networkManager.OnClientConnectedCallback -= HandleClientConnected;
            networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        private void HandleServerStarted()
        {
            switch (_sessionMode)
            {
                case SessionMode.Solo:
                    _statusMessage = "Solo practice is live. Enemy waves will start shortly.";
                    break;
                case SessionMode.HostRoom:
                    _statusMessage = $"Room \"{_roomName}\" is up. Other players can connect to {LocalLanAddress}:{_port}.";
                    break;
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (networkManager == null)
            {
                return;
            }

            if (networkManager.LocalClientId == clientId)
            {
                switch (_sessionMode)
                {
                    case SessionMode.Solo:
                        _statusMessage = "Solo practice running. Survive the waves and tune the arena as you like.";
                        break;
                    case SessionMode.HostRoom:
                        _statusMessage = $"Hosting room \"{_roomName}\" at {LocalLanAddress}:{_port}.";
                        break;
                    case SessionMode.ClientRoom:
                        _statusMessage = $"Connected to room {_addressInput}:{_port}.";
                        break;
                }

                return;
            }

            _statusMessage = $"Player {clientId} joined. Current player count: {ConnectedPlayerCount}.";
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (networkManager != null && networkManager.LocalClientId == clientId)
            {
                _sessionMode = SessionMode.None;
                _statusMessage = "You left the current room.";
                return;
            }

            _statusMessage = $"Player {clientId} left the room.";
        }

        private void ConfigureTransport(string address, ushort port, string listenAddress = null)
        {
            if (transport == null)
            {
                return;
            }

            var transportType = typeof(UnityTransport);
            var overloadWithListen = transportType.GetMethod(
                "SetConnectionData",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(string), typeof(ushort), typeof(string) },
                null);

            if (overloadWithListen != null)
            {
                overloadWithListen.Invoke(transport, new object[] { address, port, listenAddress ?? address });
                return;
            }

            var overloadSimple = transportType.GetMethod(
                "SetConnectionData",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(string), typeof(ushort) },
                null);

            if (overloadSimple != null)
            {
                overloadSimple.Invoke(transport, new object[] { address, port });
                return;
            }

            var connectionDataField = transportType.GetField("ConnectionData", BindingFlags.Instance | BindingFlags.Public);
            if (connectionDataField == null)
            {
                return;
            }

            var connectionData = connectionDataField.GetValue(transport);
            if (connectionData == null)
            {
                return;
            }

            var dataType = connectionData.GetType();
            dataType.GetField("Address")?.SetValue(connectionData, address);
            dataType.GetField("Port")?.SetValue(connectionData, port);

            if (!string.IsNullOrWhiteSpace(listenAddress))
            {
                dataType.GetField("ServerListenAddress")?.SetValue(connectionData, listenAddress);
            }

            connectionDataField.SetValue(transport, connectionData);
        }

        private static string GetLocalIpv4Address()
        {
            try
            {
                var hostName = Dns.GetHostName();
                var address = Dns.GetHostEntry(hostName)
                    .AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

                return address?.ToString() ?? "127.0.0.1";
            }
            catch (Exception)
            {
                return "127.0.0.1";
            }
        }

#if UNITY_EDITOR
        public void SetEditorReferences(NetworkManager manager, UnityTransport unityTransport, LanShooterSceneContext context)
        {
            networkManager = manager;
            transport = unityTransport;
            sceneContext = context;
        }
#endif
    }
}
