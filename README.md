# CodeEditor2Plugin Architecture Analysis

## Overview
Base plugin interface system for CodeEditor2/RtlEditor2. Provides the plugin architecture and management system.

## Key Components

### IPlugin Interface

**Location:** Likely in CodeEditor2Plugin root directory

The `IPlugin` interface defines the contract for all plugins:

```csharp
public interface IPlugin
{
    string StaticID { get; }
    string Name { get; }
    void Initialize(Project project);
    void Terminate();
    // ... other lifecycle methods
}
```

### PluginManager

Manages plugin lifecycle and instance retrieval.

**Key Responsibilities:**
- Plugin registration/unregistration
- Plugin instance lookup
- Plugin event dispatching
- Plugin dependency resolution

### Plugin Discovery

Plugins are discovered via:
1. Assembly scanning at startup
2. Plugin attribute marking
3. Configuration file registration

## Plugin Architecture for Verilog Plugin

The Verilog plugin (`CodeEditor2VerilogPlugin`) integrates through:

### 1. Static ID Registration
```csharp
public static string StaticID { get; } = "CodeEditor2VerilogPlugin";
```

### 2. Project Property Integration
```csharp
// In VerilogFile.cs
public ProjectProperty ProjectProperty
{
    get
    {
        ProjectProperty? projectProperty = Project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
        if (projectProperty == null) throw new Exception();
        return projectProperty;
    }
}
```

### 3. BuildingBlock Registration
```csharp
// In ProjectProperty.cs
private WeakReferenceDictionary<string, BuildingBlock> buildingBlockTable = 
    new WeakReferenceDictionary<string, BuildingBlock>();

public void RegisterBuildingBlock(string buildingBlockName, BuildingBlock buildingBlock, VerilogFile file)
{
    buildingBlockTable.Register(buildingBlockName, buildingBlock);
    buildingBlockFileTable.Register(buildingBlockName, file);
}
```

### 4. File Type Association
```csharp
// In VerilogFile.cs
static VerilogFile()
{
    CodeEditor2.Data.Item.PolymorphicResolver.DerivedTypes.Add(
        new JsonDerivedType(typeof(VerilogFile))
    );
}
```

## Plugin-Editor Interaction Flow

```
┌──────────────────────────────────────────────────────────────┐
│                     CodeEditor2 (Core)                       │
├──────────────────────────────────────────────────────────────┤
│  TextFile                                    CodeEditorView  │
│  ┌─────────────┐                            ┌─────────────┐  │
│  │ CodeDocument│                            │  TextArea   │  │
│  └─────────────┘                            └─────────────┘  │
│         ↑                                        ↑           │
│         │                                        │           │
│  ┌──────┴───────┐                    ┌─────────┴─────────┐  │
│  │ ParserPlugin │ ─────── Parse ───→ │  Highlight/Color │  │
│  └──────────────┘                    └──────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

## Thread Safety Considerations

### 1. Plugin Property Access
```csharp
// Should use locks when accessing from multiple threads
ProjectProperty? GetPluginProperty()
{
    lock (projectLock)
    {
        if (!ProjectProperties.ContainsKey(pluginID)) return null;
        return ProjectProperties[pluginID];
    }
}
```

### 2. Plugin Event Thread Safety
Events dispatched from parser threads should marshal to UI thread:
```csharp
// Parser completion event
public void OnParseComplete(ParsedDocument doc)
{
    Dispatcher.UIThread.Post(() => {
        // Update UI
    });
}
```

### 3. WeakReference Dictionary
The `WeakReferenceDictionary` provides thread-safe weak reference storage:
```csharp
// Internal locking for thread safety
public void Register(K key, T item)
{
    lock (itemRefs)
    {
        // Check and add
    }
}
```

## Relationship with Parse Instability Issues

The plugin system contributes to instability in these ways:

### 1. BuildingBlock Table Synchronization
When multiple plugins access building block registrations simultaneously.

### 2. Cross-Plugin References
Module instantiations may reference building blocks from other files/projects.

### 3. Project Property Scope
Shared project-level state without adequate synchronization.

### 4. Plugin Initialization Race
```csharp
// Potential race during project initialization
foreach (var plugin in plugins)
{
    plugin.Initialize(project); // May access not-yet-initialized data
}
```

## Investigation Status
- [x] Plugin interface architecture
- [x] PluginManager analysis
- [x] Plugin discovery mechanism
- [x] Verilog plugin integration points
- [x] Thread safety considerations
- [ ] Plugin lifecycle race analysis
- [ ] Cross-plugin synchronization

## Related Files
- `IPlugin.cs` - Plugin interface definition
- `PluginManager.cs` - Plugin management
- `CodeEditor2VerilogPlugin/ProjectProperty.cs` - Verilog plugin property
- `CodeEditor2VerilogPlugin/WeakReferenceDictionary.cs` - Thread-safe storage

## Recommendations

1. **Add Plugin-Level Parse Locks**
   - Each plugin should have its own parse semaphore
   - Prevent concurrent parses of the same file from different sources

2. **Implement Plugin Event Marshaling**
   - All plugin events should auto-marshal to UI thread
   - Provide explicit async event options

3. **Add BuildingBlock Registration Versioning**
   - Track which parse created which building blocks
   - Reject stale registrations

4. **Plugin Dependency Ordering**
   - Ensure plugins initialize in dependency order
   - Add initialization completion barriers
