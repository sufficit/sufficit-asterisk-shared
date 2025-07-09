<h1>
  Sufficit.Asterisk.Shared
  <a href="https://github.com/sufficit"><img src="https://avatars.githubusercontent.com/u/66928451?s=200&v=4" alt="Sufficit Logo" width="80" align="right"></a>
</h1>

[![NuGet](https://img.shields.io/nuget/v/Sufficit.Asterisk.Shared.svg)](https://www.nuget.org/packages/Sufficit.Asterisk.Shared/)

## 📖 About the Project

`Sufficit.Asterisk.Shared` provides **shared components, extensions, and utilities** used across the Sufficit Asterisk ecosystem. This library serves as the **common foundation** that prevents code duplication and ensures consistency throughout all Asterisk-related projects in the Sufficit platform.

### 🎯 Purpose

This library acts as the **shared layer** that provides:
- **Common data models** and DTOs (Data Transfer Objects)
- **Shared interfaces** and contracts used across multiple projects
- **Extension methods** that enhance base .NET types for telephony scenarios
- **Utility classes** and helper functions
- **Constants and enumerations** for Asterisk-specific values
- **Validation helpers** for telephony data
- **Cross-cutting concerns** like logging adapters and configuration helpers

## ✨ Key Features

### 🔧 Core Shared Components
* **Common data models** for telephony entities (calls, channels, peers)
* **Shared interfaces** for service contracts and abstractions
* **Extension methods** for enhanced type functionality
* **Utility classes** for common operations
* **Constants and enums** for Asterisk-specific values
* **Validation helpers** for data integrity

### 📞 Telephony Shared Models
* **Call information** models and DTOs
* **Channel state** representations
* **SIP/IAX registration** models
* **Queue and agent** data structures
* **CDR (Call Detail Record)** shared models
* **Configuration** data transfer objects

### 🔗 Cross-Project Integration
* **Service abstractions** used by multiple projects
* **Event models** shared between AMI and AGI components
* **Configuration interfaces** for consistent settings
* **Logging abstractions** and adapters
* **Health check** shared components
* **Metrics and monitoring** shared contracts

### 🌐 Framework Support
* **Multi-target framework support** (.NET Standard 2.0, .NET 7, 8, 9)
* **Dependency injection** ready interfaces and implementations
* **ASP.NET Core integration** helpers
* **Modern async/await** patterns in shared contracts

## 🚀 Getting Started

### 📦 Installationdotnet add package Sufficit.Asterisk.Shared
### 📋 Prerequisites
* **.NET SDK** - Version depends on your target framework
* **Basic understanding** of Asterisk concepts and the Sufficit ecosystem

## 🛠️ Usage Examples

### Common Data Modelsusing Sufficit.Asterisk.Shared.Models;

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
### Shared Extension Methodsusing Sufficit.Asterisk.Shared.Extensions;

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
### Shared Interfaces and Contractsusing Sufficit.Asterisk.Shared.Contracts;

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
### Configuration Modelsusing Sufficit.Asterisk.Shared.Configuration;

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
### Validation Helpersusing Sufficit.Asterisk.Shared.Validation;

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
### Constants and Enumerationsusing Sufficit.Asterisk.Shared.Constants;

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
### Logging Abstractionsusing Sufficit.Asterisk.Shared.Logging;

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
## 🏗️ Architecture Integration

### Cross-Project Usage┌─────────────────────────────────────────────────────────────────┐
│                    Application Projects                         │
├─────────────────┬───────────────────────┬─────────────────────┤
│ Sufficit.AMI    │  Sufficit.Asterisk   │  Sufficit.Asterisk  │
│ Events          │  Manager              │  FastAGI            │
├─────────────────┼───────────────────────┼─────────────────────┤
│                 │  Sufficit.Asterisk   │  Sufficit.Asterisk  │
│                 │  Core                 │  Utils              │
├─────────────────┴───────────────────────┴─────────────────────┤
│                 Sufficit.Asterisk.Shared                       │
│            (Common Models, Extensions, Contracts)              │
├───────────────────────────────────────────────────────────────┤
│                    .NET Base Libraries                         │
└───────────────────────────────────────────────────────────────┘
### Dependency Injection Integration// Shared service registration extension
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
### Health Check Shared Componentsusing Sufficit.Asterisk.Shared.Health;

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
## 📊 Performance Considerations

### Efficient Shared Types// Value types for high-performance scenarios
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
### Memory-Efficient Extensions// Span-based parsing for better performance
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
## 🧪 Testing Utilities

### Shared Test Helpersusing Sufficit.Asterisk.Shared.Testing;

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
### Mock Factoriespublic static class MockFactory
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
## 🔧 Version Compatibility

### Multi-Framework Support
The library targets multiple frameworks to ensure maximum compatibility:

| Framework | Use Case | Features |
|-----------|----------|----------|
| **.NET Standard 2.0** | Legacy applications, Xamarin | Basic functionality, maximum compatibility |
| **.NET 7** | Current applications | Full feature set, good performance |
| **.NET 8** | LTS applications | Enhanced performance, latest stable features |
| **.NET 9** | Cutting-edge applications | Latest features and optimizations |

### Breaking Change Management// Version-aware interfaces for backward compatibility
public interface ICallInfo
{
    string UniqueId { get; }
    string CallerIdNumber { get; }
    string DestinationNumber { get; }
}

// V2 interface extends V1 without breaking changes
public interface ICallInfoV2 : ICallInfo
{
    TimeSpan Duration { get; }
    CallDisposition Disposition { get; }
}

// Implementation supports both versions
public class CallInfo : ICallInfoV2
{
    public string UniqueId { get; set; } = string.Empty;
    public string CallerIdNumber { get; set; } = string.Empty;
    public string DestinationNumber { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public CallDisposition Disposition { get; set; }
}
## 🤝 Contributing

We welcome contributions! This shared library benefits from:

### 🎯 Contribution Opportunities
* **New Shared Models** - Additional data models used across projects
* **Extension Methods** - Useful extensions for common telephony operations
* **Validation Helpers** - Enhanced validation for telephony data types
* **Performance Optimizations** - Memory and CPU efficiency improvements
* **Testing Utilities** - Shared test helpers and mock factories
* **Documentation** - Usage examples and best practices

### 📝 Contribution Guidelines
1. **Fork the Project**
2. **Create Feature Branch** (`git checkout -b feature/SharedUtility`)
3. **Add Tests** for new shared functionality
4. **Update Documentation** with usage examples
5. **Ensure Backward Compatibility** - Don't break existing APIs
6. **Commit Changes** (`git commit -m 'Add SharedUtility'`)
7. **Push to Branch** (`git push origin feature/SharedUtility`)
8. **Open Pull Request**

### 🧪 Testing Requirements
* **Unit Tests** for all shared functionality
* **Cross-Framework Tests** ensuring compatibility
* **Performance Tests** for critical paths
* **Integration Tests** with consuming projects

## 📄 License

This project is licensed under the [MIT License](LICENSE).

## 🆘 Support

- 📖 **Documentation**: [GitHub Repository](https://github.com/sufficit/sufficit-asterisk-shared)
- 🐛 **Issues**: [GitHub Issues](https://github.com/sufficit/sufficit-asterisk-shared/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/sufficit/sufficit-asterisk-shared/discussions)
- 📧 **Email**: support@sufficit.com

## 🔗 Related Projects

This shared library is used by:
- **[Sufficit.Asterisk.Core](https://github.com/sufficit/sufficit-asterisk-core)** - Core events, actions, and configuration models
- **[Sufficit.Asterisk.Manager](https://github.com/sufficit/sufficit-asterisk-manager)** - AMI provider and service infrastructure
- **[Sufficit.AMIEvents](https://github.com/sufficit/sufficit-ami-events)** - Business-specific AMI integration service
- **[Sufficit.Asterisk.FastAGI](https://github.com/sufficit/sufficit-asterisk-fastagi)** - FastAGI server implementation
- **[Sufficit.Asterisk.Utils](https://github.com/sufficit/sufficit-asterisk-utils)** - Asterisk utility functions and helpers

---

**Made with ❤️ by the Sufficit Team**