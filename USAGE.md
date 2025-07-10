# Usage Examples - Sufficit.Asterisk.Shared

## Getting Started

### Prerequisites
* **.NET SDK** - Version depends on your target framework
* **Basic understanding** of Asterisk concepts and the Sufficit ecosystem

## Common Data Models

### Call Information Models

```csharp
using Sufficit.Asterisk.Shared.Models;

// Call information model
var callInfo = new CallInfo
{
    UniqueId = "1678886400.123",
    CallerIdNumber = "1001", 
    CallerIdName = "John Doe",
    Channel = "SIP/1001-00000001",
    DestinationChannel = "SIP/1002-00000002",
    StartTime = DateTime.UtcNow,
    AnswerTime = DateTime.UtcNow.AddSeconds(5),
    EndTime = DateTime.UtcNow.AddMinutes(3),
    Duration = TimeSpan.FromMinutes(3),
    BillableSeconds = 175,
    Disposition = CallDisposition.Answered
};

Console.WriteLine($"Processing call: {callInfo.CallerIdNumber} -> {callInfo.DestinationNumber}");
Console.WriteLine($"Duration: {callInfo.Duration}");
Console.WriteLine($"Cost: ${callInfo.CalculateCost(0.05m):F2}"); // $0.15
```

## Shared Extension Methods

### String Extensions for Telephony

```csharp
using Sufficit.Asterisk.Shared.Extensions;

// String extensions for telephony
string phoneNumber = "+5511999887766";
bool isValid = phoneNumber.IsValidPhoneNumber();
string normalized = phoneNumber.NormalizePhoneNumber(); // "5511999887766"
string formatted = phoneNumber.FormatForDisplay();     // "+55 (11) 99988-7766"

// Channel string extensions
string channel = "SIP/1001-00000001";
string technology = channel.GetTechnology();    // "SIP"
string endpoint = channel.GetEndpoint();        // "1001"
string uniqueId = channel.GetUniqueId();        // "00000001"
bool isActive = channel.IsActiveChannel();

// DateTime extensions for telephony
DateTime callTime = DateTime.UtcNow;
long unixTimestamp = callTime.ToUnixTimestamp();
DateTime fromUnix = unixTimestamp.FromUnixTimestamp();
string cdrFormat = callTime.ToCDRFormat(); // "2024-01-15 10:30:00"
```

## Shared Interfaces and Contracts

### Service Contracts

```csharp
using Sufficit.Asterisk.Shared.Contracts;

// Service contract example
public interface ICallProcessingService
{
    Task<CallResult> ProcessIncomingCallAsync(IncomingCallInfo callInfo);
    Task<bool> TransferCallAsync(string channelId, string destination);
    Task EndCallAsync(string channelId, HangupReason reason);
}

// Implementation in your service project
public class CallProcessingService : ICallProcessingService
{
    public async Task<CallResult> ProcessIncomingCallAsync(IncomingCallInfo callInfo)
    {
        // Your business logic here
        return new CallResult { Success = true, Message = "Call processed" };
    }
    
    public async Task<bool> TransferCallAsync(string channelId, string destination)
    {
        // Transfer implementation
        return true;
    }
    
    public async Task EndCallAsync(string channelId, HangupReason reason)
    {
        // Hangup implementation
        await Task.CompletedTask;
    }
}
```

## Configuration Models

### Shared Configuration

```csharp
using Sufficit.Asterisk.Shared.Configuration;

// Shared configuration models
public class AsteriskServerConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5038;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSSL { get; set; } = false;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
}

// Usage in multiple projects
public class AMIServiceOptions
{
    public List<AsteriskServerConfig> Servers { get; set; } = new();
    public bool EnableReconnection { get; set; } = true;
    public TimeSpan ReconnectionInterval { get; set; } = TimeSpan.FromSeconds(30);
}

public class FastAGIServerOptions  
{
    public AsteriskServerConfig TargetServer { get; set; } = new();
    public int ListenPort { get; set; } = 4573;
    public int MaxConcurrentConnections { get; set; } = 100;
}
```

## Validation Helpers

### Data Validation

```csharp
using Sufficit.Asterisk.Shared.Validation;

// Phone number validation
var phoneValidator = new PhoneNumberValidator();
var result = phoneValidator.Validate("+5511999887766");

if (result.IsValid)
{
    Console.WriteLine($"Valid phone: {result.NormalizedNumber}");
}
else
{
    Console.WriteLine($"Invalid phone: {string.Join(", ", result.Errors)}");
}

// Channel validation
var channelValidator = new ChannelValidator();
var channelResult = channelValidator.Validate("SIP/1001-00000001");

if (channelResult.IsValid)
{
    Console.WriteLine($"Valid channel: {channelResult.ParsedChannel.Technology}/{channelResult.ParsedChannel.Endpoint}");
}

// Extension validation
var extensionValidator = new ExtensionValidator();
bool isValidExtension = extensionValidator.IsValid("1001");
bool matchesPattern = extensionValidator.MatchesPattern("18005551234", "_1NXXXXXX");
```

## Constants and Enumerations

### Asterisk Constants

```csharp
using Sufficit.Asterisk.Shared.Constants;

// Asterisk-specific constants
public static class AsteriskConstants
{
    public const int DEFAULT_AMI_PORT = 5038;
    public const int DEFAULT_FASTAGI_PORT = 4573;
    public const string DEFAULT_CONTEXT = "default";
    
    // Channel states
    public static class ChannelStates
    {
        public const string DOWN = "0";
        public const string RESERVED = "1";
        public const string OFFHOOK = "2";
        public const string DIALING = "3";
        public const string RING = "4";
        public const string RINGING = "5";
        public const string UP = "6";
        public const string BUSY = "7";
    }
    
    // Hangup causes
    public static class HangupCauses
    {
        public const string NORMAL_CLEARING = "16";
        public const string USER_BUSY = "17";
        public const string NO_USER_RESPONSE = "18";
        public const string NO_ANSWER = "19";
        public const string CALL_REJECTED = "21";
    }
}

// Enumerations
public enum CallDisposition
{
    NoAnswer,
    Failed,
    Busy,
    Answered,
    Cancelled
}

public enum ChannelState
{
    Down = 0,
    Reserved = 1,
    OffHook = 2,
    Dialing = 3,
    Ring = 4,
    Ringing = 5,
    Up = 6,
    Busy = 7
}

public enum HangupReason
{
    NormalClearing,
    UserBusy,
    NoUserResponse,
    NoAnswer,
    CallRejected,
    NetworkCongestion
}
```

## Logging Abstractions

### Shared Logging Interface

```csharp
using Sufficit.Asterisk.Shared.Logging;

// Shared logging interface for Asterisk components
public interface IAsteriskLogger
{
    void LogCallStart(string channelId, string callerNumber, string destination);
    void LogCallEnd(string channelId, TimeSpan duration, CallDisposition disposition);
    void LogChannelStateChange(string channelId, ChannelState oldState, ChannelState newState);
    void LogError(Exception exception, string context, params object[] parameters);
}

// Implementation that can be shared across projects
public class AsteriskLogger : IAsteriskLogger
{
    private readonly ILogger<AsteriskLogger> _logger;
    
    public AsteriskLogger(ILogger<AsteriskLogger> logger)
    {
        _logger = logger;
    }
    
    public void LogCallStart(string channelId, string callerNumber, string destination)
    {
        _logger.LogInformation("Call started - Channel: {ChannelId}, From: {Caller}, To: {Destination}", 
            channelId, callerNumber, destination);
    }
    
    public void LogCallEnd(string channelId, TimeSpan duration, CallDisposition disposition)
    {
        _logger.LogInformation("Call ended - Channel: {ChannelId}, Duration: {Duration}, Disposition: {Disposition}", 
            channelId, duration, disposition);
    }
    
    public void LogChannelStateChange(string channelId, ChannelState oldState, ChannelState newState)
    {
        _logger.LogDebug("Channel state changed - Channel: {ChannelId}, From: {OldState}, To: {NewState}", 
            channelId, oldState, newState);
    }
    
    public void LogError(Exception exception, string context, params object[] parameters)
    {
        _logger.LogError(exception, "Asterisk error in {Context} - Parameters: {@Parameters}", 
            context, parameters);
    }
}
```

## Dependency Injection Integration

### Service Registration

```csharp
// Shared service registration extension
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAsteriskShared(this IServiceCollection services)
    {
        // Register shared services
        services.AddSingleton<IAsteriskLogger, AsteriskLogger>();
        services.AddSingleton<PhoneNumberValidator>();
        services.AddSingleton<ChannelValidator>();
        services.AddSingleton<ExtensionValidator>();
        
        // Register shared configuration
        services.AddOptions<AsteriskServerConfig>();
        
        return services;
    }
}

// Usage in consuming projects
// In Sufficit.AMIEvents/Program.cs
services.AddAsteriskShared();

// In Sufficit.Asterisk.Manager/Program.cs  
services.AddAsteriskShared();

// In Sufficit.Asterisk.FastAGI/Program.cs
services.AddAsteriskShared();
```

## Health Check Shared Components

### Base Health Check Class

```csharp
using Sufficit.Asterisk.Shared.Health;

// Shared health check base class
public abstract class AsteriskHealthCheckBase : IHealthCheck
{
    protected readonly IAsteriskLogger _logger;
    
    protected AsteriskHealthCheckBase(IAsteriskLogger logger)
    {
        _logger = logger;
    }
    
    public abstract Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default);
        
    protected HealthCheckResult CreateResult(bool isHealthy, string description, Dictionary<string, object>? data = null)
    {
        return isHealthy 
            ? HealthCheckResult.Healthy(description, data)
            : HealthCheckResult.Unhealthy(description, null, data);
    }
}

// Usage in specific projects
public class AMIConnectionHealthCheck : AsteriskHealthCheckBase
{
    private readonly IAMIService _amiService;
    
    public AMIConnectionHealthCheck(IAMIService amiService, IAsteriskLogger logger) : base(logger)
    {
        _amiService = amiService;
    }
    
    public override async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var isConnected = await _amiService.IsConnectedAsync();
        var data = new Dictionary<string, object>
        {
            ["connection_count"] = _amiService.ConnectionCount,
            ["last_event_time"] = _amiService.LastEventTime
        };
        
        return CreateResult(isConnected, 
            isConnected ? "AMI connections are healthy" : "AMI connections are down", 
            data);
    }
}
```

## Performance Considerations

### Efficient Shared Types

```csharp
// Value types for high-performance scenarios
public readonly struct ChannelInfo : IEquatable<ChannelInfo>
{
    public readonly string Technology;
    public readonly string Endpoint;
    public readonly string UniqueId;
    
    public ChannelInfo(string technology, string endpoint, string uniqueId)
    {
        Technology = technology;
        Endpoint = endpoint;
        UniqueId = uniqueId;
    }
    
    public bool Equals(ChannelInfo other) => 
        Technology == other.Technology && 
        Endpoint == other.Endpoint && 
        UniqueId == other.UniqueId;
        
    public override bool Equals(object? obj) => obj is ChannelInfo other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Technology, Endpoint, UniqueId);
}

// Object pooling for frequently used objects
public class CallInfoPool
{
    private readonly ConcurrentQueue<CallInfo> _pool = new();
    
    public CallInfo Get()
    {
        return _pool.TryDequeue(out var item) ? item : new CallInfo();
    }
    
    public void Return(CallInfo item)
    {
        item.Reset(); // Clear all properties
        _pool.Enqueue(item);
    }
}
```

### Memory-Efficient Extensions

```csharp
// Span-based parsing for better performance
public static class ChannelExtensions
{
    public static bool TryParseChannel(ReadOnlySpan<char> channel, out ChannelInfo result)
    {
        result = default;
        
        var firstSlash = channel.IndexOf('/');
        if (firstSlash == -1) return false;
        
        var technology = channel.Slice(0, firstSlash);
        var remaining = channel.Slice(firstSlash + 1);
        
        var lastDash = remaining.LastIndexOf('-');
        if (lastDash == -1) return false;
        
        var endpoint = remaining.Slice(0, lastDash);
        var uniqueId = remaining.Slice(lastDash + 1);
        
        result = new ChannelInfo(technology.ToString(), endpoint.ToString(), uniqueId.ToString());
        return true;
    }
}
```

## Testing Utilities

### Shared Test Helpers

```csharp
using Sufficit.Asterisk.Shared.Testing;

// Test data builders
public class CallInfoBuilder
{
    private readonly CallInfo _callInfo = new();
    
    public CallInfoBuilder WithCaller(string number, string name = "")
    {
        _callInfo.CallerIdNumber = number;
        _callInfo.CallerIdName = name;
        return this;
    }
    
    public CallInfoBuilder WithDestination(string number)
    {
        _callInfo.DestinationNumber = number;
        return this;
    }
    
    public CallInfoBuilder WithDuration(TimeSpan duration)
    {
        _callInfo.Duration = duration;
        _callInfo.BillableSeconds = (int)duration.TotalSeconds;
        return this;
    }
    
    public CallInfo Build() => _callInfo;
}

// Usage in tests
var callInfo = new CallInfoBuilder()
    .WithCaller("1001", "John Doe")
    .WithDestination("1002")
    .WithDuration(TimeSpan.FromMinutes(3))
    .Build();
```

### Mock Factories

```csharp
public static class MockFactory
{
    public static Mock<IAsteriskLogger> CreateMockLogger()
    {
        var mock = new Mock<IAsteriskLogger>();
        // Setup common expectations
        return mock;
    }
    
    public static Mock<ICallProcessingService> CreateMockCallService()
    {
        var mock = new Mock<ICallProcessingService>();
        mock.Setup(x => x.ProcessIncomingCallAsync(It.IsAny<IncomingCallInfo>()))
            .ReturnsAsync(new CallResult { Success = true });
        return mock;
    }
}
```