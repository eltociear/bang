using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bang.Analyzers.Tests;

using Verify = BangAnalyzerVerifier<AttributeAnalyzer>;

[TestClass]
public sealed class AttributeAnalyzerTests
{
    [TestMethod(displayName: "Correctly annotated systems do not trigger the analyzer.")]
    public async Task CorrectlyAnnotatedSystemsDoNotTriggerTheAnalyzer()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

public readonly record struct Message : IMessage;
public readonly record struct Component : IComponent;

[Filter(ContextAccessorKind.Read, typeof(Component))]
public sealed class CorrectSystem : ISystem { }

[Messager(typeof(Message))]
[Filter(ContextAccessorKind.Read, typeof(Message))]
public sealed class CorrectMessagerSystem : IMessagerSystem
{
    public void OnMessage(World world, Entity entity, IMessage message) { }
}

[Watch(typeof(Component))]
[Filter(ContextAccessorKind.Read, typeof(Component))]
public sealed class ReactiveSystem : IReactiveSystem
{
    public void OnAdded(World world, ImmutableArray<Entity> entities) { }
    public void OnRemoved(World world, ImmutableArray<Entity> entities) { }
    public void OnModified(World world, ImmutableArray<Entity> entities) { }
}";
        await Verify.VerifyAnalyzerAsync(source);
    }

    [TestMethod(displayName: "Filter attributes containing non-component types trigger one message per wrong type.")]
    public async Task ISystemWithNonComponentsOnFilter()
    {
        const string source = @"
using Bang.Components;
using Bang.Contexts;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct Message : IMessage;
public readonly record struct Component : IComponent;
public readonly record struct SomeRandomType;

[Filter(ContextAccessorKind.Read, typeof(Message), typeof(Component), typeof(SomeRandomType))]
public sealed class System : ISystem { }";

        var expectedDiagnostics = new[]
        {
            Verify
                .Diagnostic(AttributeAnalyzer.NonComponentsOnFilterAttribute)
                .WithSpan(12, 35, 12, 50),
            Verify
                .Diagnostic(AttributeAnalyzer.NonComponentsOnFilterAttribute)
                .WithSpan(12, 71, 12, 93),
        };
        await Verify.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [TestMethod(displayName: "Watch attributes containing non-component types trigger one message per wrong type.")]
    public async Task IReactiveSystemWithNonComponentsOnWatchAttribute()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

public readonly record struct Message : IMessage;
public readonly record struct Component : IComponent;
public readonly record struct SomeRandomType;

[Watch(typeof(Message))]
[Filter(ContextAccessorKind.Read, typeof(Component))]
public sealed class ReactiveSystem : IReactiveSystem
{
    public void OnAdded(World world, ImmutableArray<Entity> entities) { }
    public void OnRemoved(World world, ImmutableArray<Entity> entities) { }
    public void OnModified(World world, ImmutableArray<Entity> entities) { }
}";
        var expectedDiagnostics = new[]
        {
            Verify
                .Diagnostic(AttributeAnalyzer.NonComponentsOnWatchAttribute)
                .WithSpan(15, 8, 15, 23),
        };
        await Verify.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [TestMethod(displayName: "Filter attributes with non-message types trigger one message per wrong type.")]
    public async Task IMessagerSystemWithNonMessagesOnFilter()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct Message : IMessage;
public readonly record struct Component : IComponent;
public readonly record struct SomeRandomType;

[Messager(typeof(Message))]
[Filter(ContextAccessorKind.Read, typeof(Message), typeof(Component), typeof(SomeRandomType))]
public sealed class MessagerSystem : IMessagerSystem
{
    public void OnMessage(World world, Entity entity, IMessage message) { }
}";
        var expectedDiagnostics = new[]
        {
            Verify
                .Diagnostic(AttributeAnalyzer.NonMessagesOnMessagerFilterAttribute)
                .WithSpan(15, 52, 15, 69),
            Verify
                .Diagnostic(AttributeAnalyzer.NonMessagesOnMessagerFilterAttribute)
                .WithSpan(15, 71, 15, 93),
        };
        await Verify.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [TestMethod(displayName: "Messager attributes containing non-message types trigger one message per wrong type.")]
    public async Task IMessagerSystemWithNonMessagesOnMessagerAttribute()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct Message : IMessage;
public readonly record struct Component : IComponent;
public readonly record struct SomeRandomType;

[Messager(typeof(Component))]
[Filter(ContextAccessorKind.Read, typeof(Message))]
public sealed class MessagerSystem : IMessagerSystem
{
    public void OnMessage(World world, Entity entity, IMessage message) { }
}";
        var expectedDiagnostics = new[]
        {
            Verify
                .Diagnostic(AttributeAnalyzer.NonMessagesOnMessagerAttribute)
                .WithSpan(14, 11, 14, 28),
        };
        await Verify.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }
}