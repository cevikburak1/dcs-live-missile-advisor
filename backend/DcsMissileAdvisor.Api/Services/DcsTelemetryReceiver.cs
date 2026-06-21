using System.Text.Json;
using System.Text.Json.Serialization;
using DcsMissileAdvisor.Api.Models;

namespace DcsMissileAdvisor.Api.Services;

public class DcsTelemetryState
{
    public DcsRawPacket? LastPacket { get; set; }
    public DateTime LastReceivedUtc { get; set; }
    public long LastSeq { get; set; }
}

public class DcsTelemetryReceiver : BackgroundService
{
    public const int UdpPort = 17777;
    public static readonly TimeSpan StaleThreshold = TimeSpan.FromSeconds(2);

    private readonly DcsTelemetryState _state = new();
    private readonly AdvisorStateAggregator _aggregator;
    private readonly ILogger<DcsTelemetryReceiver> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DcsTelemetryReceiver(
        AdvisorStateAggregator aggregator,
        ILogger<DcsTelemetryReceiver> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public DcsTelemetryState GetState() => _state;

    public DcsConnectionStatus GetConnectionStatus()
    {
        if (_state.LastPacket is null)
            return DcsConnectionStatus.Disconnected;

        var age = DateTime.UtcNow - _state.LastReceivedUtc;
        return age > StaleThreshold
            ? DcsConnectionStatus.Stale
            : DcsConnectionStatus.Connected;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = new System.Net.Sockets.UdpClient(UdpPort);

        _logger.LogInformation("DCS telemetry UDP listener started on port {Port}", UdpPort);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await client.ReceiveAsync(stoppingToken);
                await ProcessDatagramAsync(result.Buffer);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UDP receive error");
            }
        }
    }

    private async Task ProcessDatagramAsync(byte[] buffer)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(buffer);
            var packet = JsonSerializer.Deserialize<DcsRawPacket>(json, JsonOptions);
            if (packet is null) return;

            _state.LastPacket = packet;
            _state.LastReceivedUtc = DateTime.UtcNow;
            _state.LastSeq = packet.Seq;

            await _aggregator.ProcessPacketAsync(packet, GetConnectionStatus());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize DCS packet");
        }
    }
}
