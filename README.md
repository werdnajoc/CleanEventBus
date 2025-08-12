# 🔥 Clean EventBus for Unity

A **layered, type-safe event bus system** for Unity implementing **Clean Architecture principles** with context-aware targeting.

Perfect for developers who want to build **maintainable, decoupled Unity applications** without sacrificing performance.

![Unity](https://img.shields.io/badge/Unity-2021.3+-blue?logo=unity) ![License](https://img.shields.io/badge/License-MIT-green) ![Tests](https://img.shields.io/badge/Tests-Passing-brightgreen) ![Performance](https://img.shields.io/badge/Performance-Optimized-orange)

---

## 🎯 What is Clean EventBus?

**Clean EventBus** is an advanced messaging system designed specifically for Unity projects that follow **Clean Architecture patterns**. Unlike traditional Unity event systems, it provides:

- **Layer separation** - Different event buses for Domain, Application, and Adapter layers
- **Type safety** - Prevents cross-layer pollution and ensures architectural integrity
- **Smart targeting** - Events can be directed to specific instances using context
- **Zero configuration** - Works out of the box with intuitive APIs

### Why Clean EventBus?

```csharp
// ❌ Traditional approach - tight coupling
public class StoreView : MonoBehaviour 
{
    public StoreManager storeManager;  // Direct reference
    
    void OnUpgradeClick() 
    {
        storeManager.UpgradeStore(storeId);  // Tightly coupled
    }
}

// ✅ Clean EventBus approach - decoupled
public class StoreView : MonoBehaviour 
{
    void OnUpgradeClick() 
    {
        eventBus.Publish(new StoreUpgradeRequested(storeId));  // Decoupled
    }
}
```

---

## ⚡ Features

### 🏗️ **Clean Architecture Ready**
- **Domain EventBus** - For business logic events
- **Application EventBus** - For use case coordination and UI updates
- **Adapter EventBus** - For infrastructure operations
- **Zero cross-layer pollution** - Each layer stays clean

### 🎯 **Context-Aware Targeting**
```csharp
// Only the specific store receives this event
using (EventContext.SetContext("store_123"))
{
    eventBus.Subscribe<StoreUpdated>(OnStoreUpdated);
}

eventBus.Publish(new StoreUpdated("store_123")); // Only goes to store_123
```

### 🚀 **High Performance**
- **Sub-microsecond latency** per callback
- **Minimal memory allocation** (<200 bytes per subscription)
- **Efficient targeting** - No unnecessary callbacks
- **Thread-safe** operations

### 🛡️ **Type Safety**
```csharp
// ✅ This works - same layer
appEventBus.Publish(new StoreUIUpdated());

// ❌ This won't compile - cross-layer
appEventBus.Publish(new StoreDomainEvent()); // Compile error!
```

### 📝 **Simple API**
- **Familiar syntax** - Similar to C# events but better
- **IntelliSense friendly** - Clear autocomplete
- **Minimal boilerplate** - Just inherit from base classes

---

## 📦 Installation

### Via Package Manager (Git URL)

1. Open **Package Manager** in Unity
2. Click **"+"** and select **"Add package from git URL"**
3. Enter: `https://github.com/werdnajoc/CleanEventBus.git`
4. Click **"Add"**

### Via manifest.json

Copy And Past this line to your `Packages/manifest.json`:

```text
"com.werdnajoc.clean-event-bus": "https://github.com/werdnajoc/CleanEventBus.git?path=Packages/com.werdnajoc.clean-event-bus",
```
Example
```json
{
  "dependencies": {
    "com.werdnajoc.clean-event-bus": "https://github.com/werdnajoc/CleanEventBus.git?path=Packages/com.werdnajoc.clean-event-bus",
    // ... other dependencies
  }
}
```


### Requirements
- **Unity 2021.3** or higher
- **.NET Standard 2.1** compatible

---

## 🎮 Use Cases & Examples

### 1. **Basic Store Management System**

```csharp
using CleanEventBus.Application;
using CleanEventBus.Application.Events;

// Define your events
[TargetedEvent("StoreId")]
public class StoreUpgraded : BaseApplicationEvent
{
    public string StoreId { get; set; }
    public int NewLevel { get; set; }
    public decimal Cost { get; set; }
}

// In your View (UI)
public class StoreView : MonoBehaviour
{
    [SerializeField] private string storeId;
    private IEventBusApplication eventBus;
    
    void Start()
    {
        eventBus = new ApplicationEventBus();
        
        // Subscribe to events for this specific store
        using (EventContext.SetContext(storeId))
        {
            eventBus.Subscribe<StoreUpgraded>(OnStoreUpgraded);
        }
    }
    
    void OnUpgradeButtonClick()
    {
        // Publish upgrade request
        eventBus.Publish(new StoreUpgradeRequested(storeId));
    }
    
    void OnStoreUpgraded(StoreUpgraded evt)
    {
        // Only this store receives this event!
        ShowUpgradeEffect(evt.NewLevel);
        UpdateUI();
    }
}

// In your ViewModel/UseCase
public class UpgradeStoreUseCase
{
    private readonly IEventBusApplication eventBus;
    
    public UpgradeStoreUseCase(IEventBusApplication eventBus)
    {
        this.eventBus = eventBus;
        eventBus.Subscribe<StoreUpgradeRequested>(HandleUpgrade);
    }
    
    void HandleUpgrade(StoreUpgradeRequested request)
    {
        // Business logic here
        var store = storeService.UpgradeStore(request.StoreId);
        
        // Notify UI of success
        eventBus.Publish(new StoreUpgraded(request.StoreId)
        {
            NewLevel = store.Level,
            Cost = store.UpgradeCost
        });
    }
}
```

### 2. **Clean Architecture Example**

```csharp
// Domain Layer - Business Events
using CleanEventBus.Domain;
using CleanEventBus.Domain.Events;

public class PlayerLeveledUp : BaseDomainEvent
{
    public string PlayerId { get; set; }
    public int NewLevel { get; set; }
    public int ExperienceGained { get; set; }
}

// Application Layer - UI Coordination
using CleanEventBus.Application;
using CleanEventBus.Application.Events;

[TargetedEvent("PlayerId")]
public class PlayerUIUpdateRequired : BaseApplicationEvent
{
    public string PlayerId { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public List<string> UnlockedFeatures { get; set; }
}

// Usage in Domain Service
public class PlayerService
{
    private readonly IEventBusDomain domainEventBus;
    
    public void GainExperience(string playerId, int experience)
    {
        // Domain logic...
        
        if (playerLeveledUp)
        {
            domainEventBus.Publish(new PlayerLeveledUp 
            { 
                PlayerId = playerId,
                NewLevel = newLevel,
                ExperienceGained = experience
            });
        }
    }
}

// Application Layer - Coordinate between Domain and UI
public class PlayerApplicationService
{
    private readonly IEventBusDomain domainEventBus;
    private readonly IEventBusApplication appEventBus;
    
    public PlayerApplicationService(IEventBusDomain domainEventBus, IEventBusApplication appEventBus)
    {
        this.domainEventBus = domainEventBus;
        this.appEventBus = appEventBus;
        
        // Listen to domain events and coordinate UI updates
        domainEventBus.Subscribe<PlayerLeveledUp>(OnPlayerLeveledUp);
    }
    
    void OnPlayerLeveledUp(PlayerLeveledUp domainEvent)
    {
        // Coordinate UI update
        appEventBus.Publish(new PlayerUIUpdateRequired(domainEvent.PlayerId)
        {
            Level = domainEvent.NewLevel,
            Experience = GetPlayerExperience(domainEvent.PlayerId),
            UnlockedFeatures = GetUnlockedFeatures(domainEvent.PlayerId)
        });
    }
}
```

### 3. **Multiple Store Instances Example**

```csharp
// You have 10 store prefabs in your scene
// Each one has a unique storeId: "store_1", "store_2", etc.

// When you publish this event:
eventBus.Publish(new StoreUpgraded("store_5") { NewLevel = 3 });

// Only the store with storeId "store_5" will receive it
// The other 9 stores won't be notified - efficient!
```

### 4. **Global vs Targeted Events**

```csharp
// Global event - all subscribers receive it
public class GamePaused : BaseApplicationEvent
{
    public bool IsPaused { get; set; }
}

eventBus.Publish(new GamePaused { IsPaused = true });
// All UI elements pause

// Targeted event - only specific subscriber receives it  
[TargetedEvent("DialogId")]
public class DialogClosed : BaseApplicationEvent
{
    public string DialogId { get; set; }
    public DialogResult Result { get; set; }
}

eventBus.Publish(new DialogClosed("settings_dialog") { Result = DialogResult.OK });
// Only the settings dialog handler receives this
```

---

## 📊 Performance Test Results

Our comprehensive test suite ensures Clean EventBus performs excellently in production scenarios:

### ⚡ **Speed Benchmarks**
```
✅ Subscribe 1000 events: <100ms
✅ Publish to 1000 subscribers: <50ms  
✅ Targeted event delivery: <1ms (even with 1000+ potential targets)
✅ EventBus overhead: ~12x vs direct calls (comparable to Unity Events)
```

### 💾 **Memory Efficiency**
```
✅ Average memory per subscription: <200 bytes
✅ Single subscription allocation: <500 bytes
✅ Memory per publish operation: <1KB
```

### 🎯 **Targeting Accuracy**
```
✅ 100% accuracy - targeted events only reach intended recipients
✅ Zero wrong callbacks - other instances never receive targeted events
✅ Efficient routing - no unnecessary processing for non-target instances
```

### 🧪 **Stress Testing**
```
✅ Handles 10,000+ subscribers without issues
✅ Processes large payloads (1MB+) efficiently  
✅ Thread-safe concurrent operations
✅ Rapid subscribe/unsubscribe cycles (1000+ cycles)
```

### 📈 **Cache Performance**
```
✅ Metadata caching provides 2-3x performance improvement on subsequent calls
✅ Property access optimization via compiled expressions
✅ Zero reflection overhead after warmup
```

---

## 🏗️ Architecture Benefits

### **Clean Separation**
```csharp
// Each layer has its own event bus - no mixing!
using CleanEventBus.Domain;     // Business logic events IF needed
using CleanEventBus.Application; // For use cases publish events
using CleanEventBus.Adapter;   // For view models, controllers, presenters
```

### **Type Safety Enforcement**
```csharp
// ✅ This compiles
domainEventBus.Publish(new PlayerLeveledUp());

// ❌ This doesn't compile - prevents architectural violations
domainEventBus.Publish(new UIButtonClicked()); // Compile error!
```

### **Testability**
```csharp
// Easy to mock and test
var mockEventBus = new Mock<IEventBusApplication>();
var useCase = new MyUseCase(mockEventBus.Object);

// Verify events were published
mockEventBus.Verify(x => x.Publish(It.IsAny<MyEvent>()), Times.Once);
```

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup
1. Clone the repository
2. Open in Unity 2021.3+
3. Run tests in Test Runner (Window → General → Test Runner)

### Running Tests
```
Window → General → Test Runner → PlayMode → Run All
```

All performance tests should pass with the benchmarks listed above.

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🔗 Links

- **Repository**: [GitHub](https://github.com/werdnajoc/CleanEventBus)
- **Issues**: [Report bugs or request features](https://github.com/werdnajoc/CleanEventBus/issues)
- **Author**: [Andrew Ortiz](https://github.com/werdnajoc)

---

## 💡 Tips for Success

### **Start Simple**
Begin with Application layer events for UI coordination, then expand to Domain and Adapter layers as needed.

### **Use Descriptive Names**
```csharp
// ✅ Good
public class StoreUpgradeCompleted : BaseApplicationEvent

// ❌ Less clear  
public class StoreEvent : BaseApplicationEvent
```

### **Leverage Targeting**
Use targeted events for instance-specific operations, global events for system-wide notifications.

### **Keep Events Immutable**
Design events as data containers - avoid business logic in event classes.

---

**Ready to build cleaner, more maintainable Unity applications?** 🚀

[Get started now](https://github.com/werdnajoc/CleanEventBus) • [View Examples](Samples~/) • [Report Issues](https://github.com/werdnajoc/CleanEventBus/issues)