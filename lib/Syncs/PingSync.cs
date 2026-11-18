using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace SilklessLib.Syncs;

public class ForwardPingPacket : SilklessPacket
{
    public string TargetId;
    public int PingId;
}

public class BackwardPingPacket : SilklessPacket
{
    public string TargetId;
    public int PingId;
}

public class PingSync : Sync
{
    private int _pingId;
    private readonly Dictionary<int, float> _pingTimes = [];
    private readonly HashSet<string> _players = [];
    protected override void OnEnable()
    {
        SilklessAPI.AddHandler<ForwardPingPacket>(OnForwardPingPacket);
        SilklessAPI.AddHandler<BackwardPingPacket>(OnBackwardPingPacket);
    }
    protected override void OnDisable()
    {
        SilklessAPI.RemoveHandler<ForwardPingPacket>(OnForwardPingPacket);
        SilklessAPI.RemoveHandler<BackwardPingPacket>(OnBackwardPingPacket);
    }
    protected override void OnPlayerJoin(string id)
    {
        _players.Add(id);
    }
    protected override void OnPlayerLeave(string id)
    {
        _players.Remove(id);
    }

    protected override void Tick()
    {
        foreach (string id in _players)
        {
            SendForwardPing(id);
        }
    }

    private void SendForwardPing(string targetId)
    {
        LogUtil.LogDebug($"Sending forward ping to player {targetId} with PingId {_pingId}.");
        _pingTimes[_pingId] = DateTime.Now.Ticks;
        SilklessAPI.SendPacket(new ForwardPingPacket()
        {
            TargetId = targetId,
            PingId = _pingId
        });
        _pingId++;
    }

    private void OnForwardPingPacket(ForwardPingPacket packet)
    {
        if (packet.TargetId == SilklessAPI.GetId())
        {
            LogUtil.LogDebug($"Received forward ping packet from player {packet.ID} with PingId {packet.PingId}. Sending backward ping.");
            SilklessAPI.SendPacket(new BackwardPingPacket()
            {
                TargetId = packet.ID,
                PingId = packet.PingId
            });
        }
        else
        {
            LogUtil.LogWarning($"Received forward ping packet for player {packet.TargetId} from player {packet.ID}, but this player is not the target.");
        }
    }

    private void OnBackwardPingPacket(BackwardPingPacket packet)
    {
        if (_pingTimes.TryGetValue(packet.PingId, out float timestamp))
        {
            float latency = (float)(DateTime.Now.Ticks - timestamp) / TimeSpan.TicksPerMillisecond;
            LogUtil.LogDebug($"Player {packet.TargetId} has round-trip latency {latency} ms.");
        }
        else
        {
            LogUtil.LogWarning($"Received backward ping packet with unknown PingId {packet.PingId} from player {packet.TargetId}.");
        }
    }

    protected override void Reset() { }
}