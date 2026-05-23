using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Titan.Client.Services.Models;
using Titan.Client.Services.Apibase;
using System;
using System.Text.Json;

namespace Titan.Client.Services.AppServices
{
    public class SignalRService : IAsyncDisposable
    {
        private HubConnection? _hub;
        private readonly AuthService _auth;
        private readonly NavigationManager _navigation;
        private readonly IConfiguration _configuration;

        public event Action<NotificationDto>? OnNotification;
        public event Action<OrderDto>? OnOrderUpdate;

        public bool IsConnected => _hub?.State == HubConnectionState.Connected;

        public SignalRService(AuthService auth, NavigationManager navigation, IConfiguration configuration)
        {
            _auth = auth;
            _navigation = navigation;
            _configuration = configuration;
        }

        private bool IsTokenExpired(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3) return true;
                
                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                
                var bytes = Convert.FromBase64String(payload);
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("exp", out var expProp) && expProp.TryGetInt64(out var exp))
                {
                    var expTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                    return expTime <= DateTimeOffset.UtcNow.AddSeconds(15); // Expired or expiring within 15 seconds
                }
            }
            catch
            {
                return true;
            }
            return true;
        }

        public async Task ConnectAsync()
        {
            if (_hub?.State == HubConnectionState.Connected) return;

            if (_hub is not null)
            {
                await _hub.DisposeAsync();
                _hub = null;
            }

            var apiBase = _configuration["ApiBaseUrl"] ?? "https://titans.runasp.net/";
            if (!apiBase.EndsWith("/")) apiBase += "/";
            var hubUrl = new Uri(new Uri(apiBase), "hubs/titan").ToString();

            _hub = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                                         Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;

                    options.AccessTokenProvider = async () =>
                    {
                        var token = await _auth.GetAccessTokenAsync();
                        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
                        {
                            Console.WriteLine("[SignalR] Token is expired or missing. Attempting proactive refresh...");
                            var refreshed = await _auth.RefreshAsync();
                            if (refreshed)
                            {
                                token = await _auth.GetAccessTokenAsync();
                                Console.WriteLine("[SignalR] Token proactively refreshed successfully.");
                            }
                            else
                            {
                                Console.WriteLine("[SignalR] Proactive token refresh failed.");
                            }
                        }
                        return token;
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            _hub.On<NotificationDto>("ReceiveNotification", dto => OnNotification?.Invoke(dto));
            _hub.On<OrderDto>("OrderStatusUpdated", dto => OnOrderUpdate?.Invoke(dto));

            try
            {
                await _hub.StartAsync();
                Console.WriteLine($"[SignalR] Connected to {hubUrl}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Connection failed: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hub is not null)
            {
                await _hub.StopAsync();
                await _hub.DisposeAsync();
                _hub = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
        }
    }
}