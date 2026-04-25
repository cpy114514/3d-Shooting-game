using UnityEngine;

namespace LanShooter
{
    public sealed class LanShooterHud : MonoBehaviour
    {
        private GUIStyle _panelStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _crosshairStyle;
        private string _portInput = "7777";

        private void OnGUI()
        {
            var session = LanShooterSession.Instance;
            if (session == null)
            {
                return;
            }

            EnsureStyles();
            DrawSessionPanel(session);
            DrawCrosshair(session);
            DrawHelpBar(session);
        }

        private void DrawSessionPanel(LanShooterSession session)
        {
            GUILayout.BeginArea(new Rect(18f, 18f, 470f, 430f), _panelStyle);
            GUILayout.Label("LAN Shooter", _titleStyle);
            GUILayout.Space(10f);

            GUILayout.Label($"Local LAN IP: {session.LocalLanAddress}", _labelStyle);
            GUILayout.Label($"Status: {session.StatusMessage}", _labelStyle);
            GUILayout.Space(8f);

            GUILayout.Label("Room Name", _labelStyle);
            session.RoomName = GUILayout.TextField(session.RoomName, 32);

            GUILayout.Label("Port", _labelStyle);
            _portInput = GUILayout.TextField(_portInput, 8);
            session.PortInput = _portInput;

            GUILayout.Label("Host Address", _labelStyle);
            session.AddressInput = GUILayout.TextField(session.AddressInput, 32);

            GUILayout.Space(10f);

            if (!session.IsInSession)
            {
                if (GUILayout.Button("Solo Practice", GUILayout.Height(42f)))
                {
                    session.StartSolo();
                }

                if (GUILayout.Button("Create Room", GUILayout.Height(42f)))
                {
                    session.StartHostRoom();
                }

                if (GUILayout.Button("Join Room", GUILayout.Height(42f)))
                {
                    session.StartClient();
                }
            }
            else
            {
                GUILayout.Label(
                    $"Mode: {(session.IsSoloSession ? "Solo" : session.IsHost ? "Host" : "Client")} | Players: {session.ConnectedPlayerCount}",
                    _labelStyle);

                if (GUILayout.Button("Leave Session", GUILayout.Height(42f)))
                {
                    session.Shutdown();
                }
            }

            var localPlayer = LanShooterPlayer.LocalPlayer;
            if (localPlayer != null)
            {
                GUILayout.Space(10f);
                GUILayout.Label($"Health: {localPlayer.Health}/{LanShooterPlayer.MaxHealthValue}", _labelStyle);
                GUILayout.Label($"Score: {localPlayer.Score}", _labelStyle);
                GUILayout.Label(localPlayer.IsAlive ? "State: Fighting" : "State: Down, waiting to respawn", _labelStyle);

                var waveDirector = LanShooterSoloWaveDirector.Instance;
                if (session.IsSoloSession && waveDirector != null)
                {
                    GUILayout.Label($"Wave: {waveDirector.CurrentWave}", _labelStyle);
                    GUILayout.Label(
                        $"Enemies Alive: {waveDirector.AliveEnemies} | Waiting To Spawn: {waveDirector.EnemiesRemainingToSpawn}",
                        _labelStyle);
                    GUILayout.Label($"Wave Status: {waveDirector.WaveStatusText}", _labelStyle);
                }
            }

            GUILayout.Space(12f);
            GUILayout.Label("Scoreboard", _labelStyle);
            foreach (var player in LanShooterPlayer.ActivePlayers)
            {
                if (player == null)
                {
                    continue;
                }

                GUILayout.Label($"{player.DisplayName}  HP {player.Health}  Score {player.Score}", _labelStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawCrosshair(LanShooterSession session)
        {
            var localPlayer = LanShooterPlayer.LocalPlayer;
            if (!session.IsInSession || localPlayer == null || !localPlayer.IsCursorLocked || !localPlayer.IsAlive)
            {
                return;
            }

            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var spread = localPlayer.CrosshairSpread;
            var crosshairColor = Color.Lerp(
                new Color(1f, 1f, 1f, 0.92f),
                new Color(1f, 0.86f, 0.26f, 1f),
                localPlayer.KillMarkerAlpha);

            DrawRect(new Rect(center.x - 1f, center.y - spread - 10f, 2f, 10f), crosshairColor);
            DrawRect(new Rect(center.x - 1f, center.y + spread, 2f, 10f), crosshairColor);
            DrawRect(new Rect(center.x - spread - 10f, center.y - 1f, 10f, 2f), crosshairColor);
            DrawRect(new Rect(center.x + spread, center.y - 1f, 10f, 2f), crosshairColor);
            DrawRect(new Rect(center.x - 2f, center.y - 2f, 4f, 4f), crosshairColor);

            var hitAlpha = Mathf.Max(localPlayer.HitMarkerAlpha, localPlayer.KillMarkerAlpha);
            if (hitAlpha > 0f)
            {
                var markerColor = Color.Lerp(
                    new Color(1f, 1f, 1f, hitAlpha),
                    new Color(1f, 0.86f, 0.26f, hitAlpha),
                    localPlayer.KillMarkerAlpha);

                var previousColor = GUI.color;
                GUI.color = markerColor;
                GUI.Label(new Rect(center.x - 18f, center.y - 22f, 36f, 36f), "x", _crosshairStyle);
                GUI.color = previousColor;
            }

            var waveDirector = LanShooterSoloWaveDirector.Instance;
            if (session.IsSoloSession && waveDirector != null && waveDirector.ShouldShowWaveBanner)
            {
                GUI.Label(
                    new Rect(center.x - 180f, 72f, 360f, 40f),
                    waveDirector.WaveStatusText,
                    _crosshairStyle);
            }
        }

        private static void DrawRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawHelpBar(LanShooterSession session)
        {
            GUILayout.BeginArea(new Rect(18f, Screen.height - 108f, 760f, 90f), _panelStyle);
            GUILayout.Label("WASD Move | Shift Slide | Space Jump | Right Mouse ADS | Left Mouse Fire | Esc Unlock Cursor", _labelStyle);

            if (!session.IsInSession)
            {
                GUILayout.Label("Solo mode spawns scaling enemy waves. Scene objects, spawn points, and prefabs remain fully editable in Unity.", _labelStyle);
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(18, 18, 18, 18),
                alignment = TextAnchor.UpperLeft,
                fontSize = 16,
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
            };

            _crosshairStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
        }
    }
}
