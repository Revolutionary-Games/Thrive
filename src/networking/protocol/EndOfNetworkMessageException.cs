using System;

/// <summary>
///   Thrown when a network message ends before all the expected data was read
/// </summary>
public class EndOfNetworkMessageException() : Exception("Network message ended unexpectedly");
