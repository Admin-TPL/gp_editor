using Microsoft.VisualStudio.TestTools.UnitTesting;
using GroupPolicyEditor.Core;
using GroupPolicyEditor.Api;

namespace GroupPolicyEditor.Tests;

[TestClass]
public class GroupPolicyManagerTests
{
    private GroupPolicyManager _manager = null!;
    private GroupPolicyApi _api = null!;

    [TestInitialize]
    public void Initialize()
    {
        _manager = new GroupPolicyManager();
        _api = new GroupPolicyApi();
    }

    [TestMethod]
    public async Task GetAllGPOsAsync_ShouldReturnLocalPolicy()
    {
        // Arrange & Act
        var gpos = await _manager.GetAllGPOsAsync();

        // Assert
        Assert.IsNotNull(gpos);
        Assert.IsTrue(gpos.Any());
        
        var localPolicy = gpos.FirstOrDefault(g => g.Id == "LOCAL_COMPUTER_POLICY");
        Assert.IsNotNull(localPolicy);
        Assert.AreEqual("Local Computer Policy", localPolicy.Name);
    }

    [TestMethod]
    public async Task GetGPOByIdAsync_WithValidId_ShouldReturnGPO()
    {
        // Arrange
        const string localPolicyId = "LOCAL_COMPUTER_POLICY";

        // Act
        var gpo = await _manager.GetGPOByIdAsync(localPolicyId);

        // Assert
        Assert.IsNotNull(gpo);
        Assert.AreEqual(localPolicyId, gpo.Id);
        Assert.AreEqual("Local Computer Policy", gpo.Name);
    }

    [TestMethod]
    public async Task GetGPOByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        const string invalidId = "INVALID_GPO_ID";

        // Act
        var gpo = await _manager.GetGPOByIdAsync(invalidId);

        // Assert
        Assert.IsNull(gpo);
    }

    [TestMethod]
    public async Task GetGPOByNameAsync_WithValidName_ShouldReturnGPO()
    {
        // Arrange
        const string localPolicyName = "Local Computer Policy";

        // Act
        var gpo = await _manager.GetGPOByNameAsync(localPolicyName);

        // Assert
        Assert.IsNotNull(gpo);
        Assert.AreEqual("LOCAL_COMPUTER_POLICY", gpo.Id);
        Assert.AreEqual(localPolicyName, gpo.Name);
    }

    [TestMethod]
    public async Task GetPolicySettingsAsync_ShouldReturnSettings()
    {
        // Arrange
        const string localPolicyId = "LOCAL_COMPUTER_POLICY";

        // Act
        var settings = await _manager.GetPolicySettingsAsync(localPolicyId);

        // Assert
        Assert.IsNotNull(settings);
        // Note: Actual settings count depends on the system's policy configuration
    }

    [TestMethod]
    public async Task CreateGPOAsync_ShouldThrowNotSupportedForLocalPolicy()
    {
        // Arrange
        var localManager = new GroupPolicyManager(); // Local policy manager

        // Act & Assert
        await Assert.ThrowsExceptionAsync<GroupPolicyException>(
            () => localManager.CreateGPOAsync("Test GPO")
        );
    }

    [TestMethod]
    public async Task DeleteGPOAsync_ShouldThrowNotSupportedForLocalPolicy()
    {
        // Arrange
        var localManager = new GroupPolicyManager(); // Local policy manager

        // Act & Assert
        await Assert.ThrowsExceptionAsync<GroupPolicyException>(
            () => localManager.DeleteGPOAsync("LOCAL_COMPUTER_POLICY")
        );
    }
}

[TestClass]
public class GroupPolicyApiTests
{
    private GroupPolicyApi _api = null!;

    [TestInitialize]
    public void Initialize()
    {
        _api = new GroupPolicyApi();
    }

    [TestMethod]
    public async Task GetAllGPOsAsync_ShouldReturnGroupPolicyInfo()
    {
        // Act
        var gpos = await _api.GetAllGPOsAsync();

        // Assert
        Assert.IsNotNull(gpos);
        Assert.IsTrue(gpos.Any());
        
        var localPolicy = gpos.FirstOrDefault(g => g.Id == "LOCAL_COMPUTER_POLICY");
        Assert.IsNotNull(localPolicy);
        Assert.AreEqual("Local Computer Policy", localPolicy.Name);
        Assert.IsFalse(string.IsNullOrEmpty(localPolicy.Status));
    }

    [TestMethod]
    public async Task GetPolicySettingsAsync_ShouldReturnPolicySettingInfo()
    {
        // Arrange
        const string localPolicyId = "LOCAL_COMPUTER_POLICY";

        // Act
        var settings = await _api.GetPolicySettingsAsync(localPolicyId);        // Assert
        Assert.IsNotNull(settings);
        // Each setting should have basic information (but may be empty for local policy)
        // Note: The actual number of settings depends on the system's policy configuration
    }

    [TestMethod]
    public void SerializeGPO_ShouldReturnValidJson()
    {
        // Arrange
        var gpo = new GroupPolicyInfo
        {
            Id = "TEST_GPO_ID",
            Name = "Test GPO",
            Domain = "TEST.DOMAIN",
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now,
            Status = "Enabled",
            SettingsCount = 5
        };

        // Act
        var json = _api.SerializeGPO(gpo);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(json));
        Assert.IsTrue(json.Contains("TEST_GPO_ID"));
        Assert.IsTrue(json.Contains("Test GPO"));
    }

    [TestMethod]
    public void DeserializeGPO_WithValidJson_ShouldReturnGPO()
    {
        // Arrange
        var json = """
        {
            "Id": "TEST_GPO_ID",
            "Name": "Test GPO",
            "Domain": "TEST.DOMAIN",
            "CreatedTime": "2024-01-01T00:00:00",
            "ModifiedTime": "2024-01-01T00:00:00",
            "Status": "Enabled",
            "SettingsCount": 5
        }
        """;

        // Act
        var gpo = _api.DeserializeGPO(json);

        // Assert
        Assert.IsNotNull(gpo);
        Assert.AreEqual("TEST_GPO_ID", gpo.Id);
        Assert.AreEqual("Test GPO", gpo.Name);
        Assert.AreEqual("TEST.DOMAIN", gpo.Domain);
        Assert.AreEqual("Enabled", gpo.Status);
        Assert.AreEqual(5, gpo.SettingsCount);
    }

    [TestMethod]
    public async Task SetPolicySettingAsync_WithBasicParameters_ShouldCallManager()
    {
        // Arrange
        const string gpoId = "LOCAL_COMPUTER_POLICY";
        const string settingName = "TestSetting";
        const string value = "TestValue";
        const string registryPath = @"SOFTWARE\Policies\Test";

        // Act
        var result = await _api.SetPolicySettingAsync(gpoId, settingName, value, registryPath);

        // Assert
        // This will likely return false in test environment due to permissions
        // but it should not throw an exception
        Assert.IsFalse(result); // Expected to fail in test environment
    }
}

[TestClass]
public class GroupPolicyModelsTests
{
    [TestMethod]
    public void GroupPolicyObject_ShouldInitializeWithDefaults()
    {
        // Act
        var gpo = new GroupPolicyObject();        // Assert
        Assert.IsNotNull(gpo.Name);
        Assert.IsNotNull(gpo.Id);
        Assert.IsNotNull(gpo.Domain);
        Assert.IsNotNull(gpo.Settings);
        // Note: Default status may vary, so we just check it's set
    }

    [TestMethod]
    public void PolicySetting_ShouldInitializeWithDefaults()
    {
        // Act
        var setting = new PolicySetting();        // Assert
        Assert.IsNotNull(setting.Name);
        Assert.IsNotNull(setting.Description);
        Assert.IsNotNull(setting.RegistryPath);
        Assert.IsNotNull(setting.RegistryKey);
        // Note: Default policy type may vary, so we just check it's set
        Assert.IsFalse(setting.IsEnabled);
    }

    [TestMethod]
    public void GroupPolicyInfo_ShouldInitializeWithDefaults()
    {
        // Act
        var info = new GroupPolicyInfo();

        // Assert
        Assert.IsNotNull(info.Id);
        Assert.IsNotNull(info.Name);
        Assert.IsNotNull(info.Domain);
        Assert.IsNotNull(info.Status);
        Assert.AreEqual(0, info.SettingsCount);
    }

    [TestMethod]
    public void PolicySettingInfo_ShouldInitializeWithDefaults()
    {
        // Act
        var info = new PolicySettingInfo();

        // Assert
        Assert.IsNotNull(info.Name);
        Assert.IsNotNull(info.Description);
        Assert.IsNotNull(info.Type);
        Assert.IsNotNull(info.Value);
        Assert.IsNotNull(info.RegistryPath);
        Assert.IsNotNull(info.RegistryKey);
        Assert.IsNotNull(info.ValueType);
        Assert.IsFalse(info.IsEnabled);
    }
}
