using Xunit;
using AstroImages.Wpf.ViewModels;
using System.ComponentModel;

namespace AstroImages.Tests
{
    /// <summary>
    /// Tests for RelayCommand which implements the ICommand pattern for MVVM.
    /// This is critical for button clicks and user actions in the UI.
    /// </summary>
    public class RelayCommandTests
    {
        [Fact]
        public void RelayCommand_Execute_CallsAction()
        {
            // Arrange
            var executeCalled = false;
            var command = new RelayCommand(obj => executeCalled = true);
            
            // Act
            command.Execute(null);
            
            // Assert
            Assert.True(executeCalled, "Execute should call the provided action");
        }
        
        [Fact]
        public void RelayCommand_CanExecute_ReturnsTrue_WhenNoPredicateProvided()
        {
            // Arrange
            var command = new RelayCommand(obj => { });
            
            // Act
            var canExecute = command.CanExecute(null);
            
            // Assert
            Assert.True(canExecute, "CanExecute should return true when no predicate is provided");
        }
        
        [Fact]
        public void RelayCommand_CanExecute_ReturnsPredicateResult()
        {
            // Arrange
            var canExecuteValue = true;
            var command = new RelayCommand(obj => { }, obj => canExecuteValue);
            
            // Act
            var result1 = command.CanExecute(null);
            
            canExecuteValue = false;
            var result2 = command.CanExecute(null);
            
            // Assert
            Assert.True(result1, "CanExecute should return true when predicate returns true");
            Assert.False(result2, "CanExecute should return false when predicate returns false");
        }
        
        [Fact]
        public void RelayCommand_CanExecuteChanged_CanBeSubscribedTo()
        {
            // Arrange
            var command = new RelayCommand(obj => { });
            var eventRaised = false;
            
            // Act - Subscribe to the event
            EventHandler handler = (sender, e) => { eventRaised = true; };
            command.CanExecuteChanged += handler;
            command.CanExecuteChanged -= handler;
            
            // Assert - Just verify we can subscribe/unsubscribe without errors
            Assert.False(eventRaised); // Event shouldn't have been raised yet
        }
        
        [Fact]
        public void RelayCommand_ExecuteWithParameter_PassesParameterToAction()
        {
            // Arrange
            object? receivedParameter = null;
            var expectedParameter = "test parameter";
            var command = new RelayCommand(obj => receivedParameter = obj);
            
            // Act
            command.Execute(expectedParameter);
            
            // Assert
            Assert.Equal(expectedParameter, receivedParameter);
        }
        
        [Fact]
        public void RelayCommand_CanExecuteWithParameter_PassesParameterToPredicate()
        {
            // Arrange
            object? receivedParameter = null;
            var expectedParameter = "test";
            var command = new RelayCommand(
                obj => { }, 
                obj => 
                { 
                    receivedParameter = obj;
                    return true;
                });
            
            // Act
            command.CanExecute(expectedParameter);
            
            // Assert
            Assert.Equal(expectedParameter, receivedParameter);
        }
    }
}
