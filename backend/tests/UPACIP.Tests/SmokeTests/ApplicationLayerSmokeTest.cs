using System.Reflection;
using UPACIP.Application.Interfaces;

namespace UPACIP.Tests.SmokeTests;

/// <summary>
/// Confirms the Application assembly is reachable from the test project
/// and that core abstractions are resolvable via reflection.
/// These are the minimal gates that prove the test infrastructure wiring
/// is correct before any feature test is written.
/// </summary>
public sealed class ApplicationLayerSmokeTest
{
    [Fact]
    public void ApplicationAssembly_Loads_WithoutException()
    {
        var assembly = Assembly.Load("UPACIP.Application");
        Assert.NotNull(assembly);
    }

    [Fact]
    public void IUnitOfWork_Interface_ExistsInApplicationAssembly()
    {
        var type = typeof(IUnitOfWork);
        Assert.Equal("UPACIP.Application", type.Assembly.GetName().Name);
    }

    [Fact]
    public void IUnitOfWork_Exposes_SaveChangesAsync()
    {
        var method = typeof(IUnitOfWork).GetMethod(nameof(IUnitOfWork.SaveChangesAsync));
        Assert.NotNull(method);
    }
}
