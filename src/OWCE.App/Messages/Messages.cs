using CommunityToolkit.Mvvm.Messaging.Messages;
using OWCE.Contracts;

namespace OWCE.Messages;

// ── Navigation Messages ───────────────────────────────────────────────────────
// ViewModels send these; the Shell/App handles them. This keeps ViewModels
// completely free of any navigation or UI API references.

/// <summary>Sent when the user selects a discovered board to connect to.</summary>
public sealed class ConnectToBoardMessage : ValueChangedMessage<DiscoveredBoard>
{
    public ConnectToBoardMessage(DiscoveredBoard board) : base(board) { }
}

/// <summary>Sent when the board connection is fully established and telemetry is flowing.</summary>
public sealed class BoardConnectedMessage : ValueChangedMessage<BoardState>
{
    public BoardConnectedMessage(BoardState state) : base(state) { }
}

/// <summary>Sent when the board disconnects (user-initiated or unexpected).</summary>
public sealed class BoardDisconnectedMessage : ValueChangedMessage<string>
{
    public BoardDisconnectedMessage(string reason) : base(reason) { }
}

/// <summary>Sent when a handshake error occurs that requires user notification.</summary>
public sealed class HandshakeErrorMessage : ValueChangedMessage<string>
{
    public HandshakeErrorMessage(string errorMessage) : base(errorMessage) { }
}

// ── Settings Messages ─────────────────────────────────────────────────────────

/// <summary>Broadcast when the user changes speed or temperature units.</summary>
public sealed class UnitsChangedMessage : ValueChangedMessage<(SpeedUnit Speed, TempUnit Temp)>
{
    public UnitsChangedMessage(SpeedUnit speed, TempUnit temp) : base((speed, temp)) { }
}
