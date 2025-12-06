# AstroImages Unit Tests

This test project contains comprehensive unit tests for the AstroImages application to ensure code quality and prevent regressions as new features are developed.

## Test Coverage

The test suite covers the following key areas:

### 1. **FITS Parser Tests** (`FitsParserTests.cs`)
- Validates FITS file header parsing
- Tests standard keyword extraction (NAXIS, BITPIX, etc.)
- Verifies handling of invalid/malformed files
- Ensures consistent parsing results

### 2. **XISF Parser Tests** (`XisfParserTests.cs`)
- Tests XISF metadata extraction
- Validates specific FITS keyword extraction from XISF files
- Tests error handling for invalid files
- Verifies case-insensitive keyword matching

### 3. **FITS Utilities Tests** (`FitsUtilitiesTests.cs`)
- File type detection (IsFitsFile, IsFitsData)
- Image statistics calculation (mean, min, max, standard deviation)
- Astronomical metadata extraction
- Edge case handling (null/empty data)

### 4. **XISF Utilities Tests** (`XisfUtilitiesTests.cs`)
- File type detection (IsXisfFile, HasXisfExtension)
- XISF data validation
- Image statistics calculation
- Extension matching (case-insensitive)

### 5. **Filename Parser Tests** (`FilenameParserTests.cs`)
- NINA filename format parsing
- Keyword extraction (RMS, HFR, Stars, etc.)
- Case-insensitive keyword matching
- Edge cases (empty values, missing keywords)

### 6. **File Management Service Tests** (`FileManagementServiceTests.cs`)
- Directory file loading
- File sorting by name
- FITS file filtering
- Unique filename generation
- File information retrieval

### 7. **Keyword Extraction Service Tests** (`KeywordExtractionServiceTests.cs`)
- Custom keyword extraction from filenames
- FITS keyword extraction from file headers
- XISF file keyword extraction
- Integration with FileItem model
- Skip XISF functionality

### 8. **Model Tests** (`FileItemTests.cs`)
- INotifyPropertyChanged implementation
- Property change notifications
- Default values validation
- Property storage

### 9. **ViewModel Tests** (`RelayCommandTests.cs`)
- Command execution
- CanExecute predicate evaluation
- CanExecuteChanged event raising
- MVVM pattern compliance

## Running the Tests

### From Visual Studio Code
1. Open the Test Explorer (beaker icon in the sidebar)
2. Click "Run All Tests" or run individual test classes/methods
3. View results in the Test Explorer

### From Command Line
```powershell
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests with coverage (if coverage tool installed)
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~FitsParserTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~FitsParserTests.ParseHeaderFromFile_ValidFitsFile_ReturnsHeader"
```

## Test Data

The tests use sample files from the `TestData` and `TestData2` directories:

- **TestData**: Contains sample FITS and XISF files for parser testing
  - `2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits`
  - `2025-10-17_07-31-45_R_RMS_0.00_HFR__Stars__100_0.02s_8.00C_0000.fits`
  - `L60_starless - small.xisf`
  - `M101 R.xisf`

- **TestData2**: Contains additional FITS files for batch testing

Tests gracefully skip if test data files are not available.

## Test Patterns

### Arrange-Act-Assert Pattern
All tests follow the AAA pattern for clarity:
```csharp
[Fact]
public void Method_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data
    var input = "test";
    
    // Act - Execute the method under test
    var result = MethodUnderTest(input);
    
    // Assert - Verify the outcome
    Assert.Equal("expected", result);
}
```

### Test Naming Convention
Tests use the naming pattern: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `ParseHeaderFromFile_ValidFitsFile_ReturnsHeader`
- `IsFitsFile_NonExistentFile_ReturnsFalse`
- `ExtractKeywordValues_NinaFormattedFilename_ExtractsRMS`

## Key Test Scenarios

### Critical Regression Prevention
These tests prevent regressions in:

1. **File Parsing**: Ensures FITS and XISF files are correctly parsed
2. **Keyword Extraction**: Validates that quality metrics (RMS, HFR, Stars) are extracted from NINA filenames
3. **File Management**: Verifies file loading and organization features
4. **Data Binding**: Tests INotifyPropertyChanged implementation for UI updates
5. **Commands**: Validates MVVM command pattern implementation

### Edge Cases Covered
- Null/empty inputs
- Non-existent files
- Malformed data
- Case sensitivity
- Missing keywords
- Empty values

## Adding New Tests

When adding new features, create corresponding tests:

1. Create a new test class for each service/utility/parser
2. Follow the existing naming conventions
3. Cover happy path, edge cases, and error conditions
4. Use test data files when testing file operations
5. Mock dependencies when testing services

Example template:
```csharp
public class NewFeatureTests
{
    [Fact]
    public void NewMethod_ValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var input = "test";
        
        // Act
        var result = NewMethod(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("expected", result);
    }
    
    [Fact]
    public void NewMethod_NullInput_HandlesGracefully()
    {
        // Arrange
        string input = null;
        
        // Act
        var result = NewMethod(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
```

## Continuous Integration

These tests should be run:
- Before committing changes
- As part of the CI/CD pipeline
- Before releasing new versions

## Test Maintenance

Keep tests:
- **Fast**: Tests should run quickly
- **Isolated**: Tests should not depend on each other
- **Repeatable**: Tests should produce the same results every time
- **Self-validating**: Tests should have clear pass/fail outcomes
- **Timely**: Write tests as you develop features

## Dependencies

The test project uses:
- **xUnit**: Modern testing framework for .NET
- **Moq**: Mocking library for creating test doubles (future use)
- **.NET 8.0**: Target framework matching the main application

## Future Enhancements

Potential areas for expanded test coverage:
- ViewModel interaction tests
- Image rendering tests
- Configuration service tests
- Theme service tests
- Update service tests
- Full integration tests
