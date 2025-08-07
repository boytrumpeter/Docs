using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DocumentProcessing.Application.Abstractions;
using DocumentProcessing.Application.Infrastructure;

namespace DocumentProcessing.Application.Tests.Infrastructure;

public class CommandDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WithValidCommand_ShouldCallHandler()
    {
        // Arrange
        var mockHandler = new Mock<ICommandHandler<TestCommand, TestResult>>();
        var expectedResult = new TestResult { Value = "test result" };
        mockHandler.Setup(x => x.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResult);

        var services = new ServiceCollection();
        services.AddSingleton(mockHandler.Object);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new CommandDispatcher(serviceProvider);
        var command = new TestCommand { Value = "test" };

        // Act
        var result = await dispatcher.DispatchAsync(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        mockHandler.Verify(x => x.HandleAsync(command, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithUnitCommand_ShouldCallHandler()
    {
        // Arrange
        var mockHandler = new Mock<ICommandHandler<TestUnitCommand>>();
        mockHandler.Setup(x => x.HandleAsync(It.IsAny<TestUnitCommand>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(Unit.Value);

        var services = new ServiceCollection();
        services.AddSingleton(mockHandler.Object);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new CommandDispatcher(serviceProvider);
        var command = new TestUnitCommand { Value = "test" };

        // Act
        await dispatcher.DispatchAsync(command, CancellationToken.None);

        // Assert
        mockHandler.Verify(x => x.HandleAsync(command, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerNotRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new CommandDispatcher(serviceProvider);
        var command = new TestCommand { Value = "test" };

        // Act & Assert
        var act = async () => await dispatcher.DispatchAsync(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerThrowsException_ShouldPropagateException()
    {
        // Arrange
        var mockHandler = new Mock<ICommandHandler<TestCommand, TestResult>>();
        var expectedException = new InvalidOperationException("Handler error");
        mockHandler.Setup(x => x.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(expectedException);

        var services = new ServiceCollection();
        services.AddSingleton(mockHandler.Object);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new CommandDispatcher(serviceProvider);
        var command = new TestCommand { Value = "test" };

        // Act & Assert
        var act = async () => await dispatcher.DispatchAsync(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Handler error");
    }

    // Test classes
    private class TestCommand : ICommand<TestResult>
    {
        public string Value { get; set; } = string.Empty;
    }

    private class TestUnitCommand : ICommand
    {
        public string Value { get; set; } = string.Empty;
    }

    private class TestResult
    {
        public string Value { get; set; } = string.Empty;
    }
}