using System.Net.Http.Json;
using System.Text.Json;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models.Ldap;

namespace Authify.Client.Wasm.Services;

public class WasmLdapDataService : ILdapDataService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WasmLdapDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<OperationResult<List<LdapConfigurationDto>>> GetConfigurationsAsync() =>
        GetAsync<List<LdapConfigurationDto>>("api/ldap");

    public Task<OperationResult<LdapConfigurationDto>> CreateConfigurationAsync(CreateLdapConfigurationRequest request) =>
        PostAsync<LdapConfigurationDto>("api/ldap", request);

    public Task<OperationResult<LdapConfigurationDto>> UpdateConfigurationAsync(string configId, UpdateLdapConfigurationRequest request) =>
        PutAsync<LdapConfigurationDto>($"api/ldap/{configId}", request);

    public Task<OperationResult> DeleteConfigurationAsync(string configId) =>
        DeleteAsync($"api/ldap/{configId}");

    public async Task<LdapTestConnectionResult> TestConnectionAsync(LdapTestConnectionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/ldap/test-connection", request, _jsonOptions);
            var result = await response.Content.ReadFromJsonAsync<LdapTestConnectionResult>(_jsonOptions);
            return result ?? LdapTestConnectionResult.Fail("Deserialisierungsfehler.");
        }
        catch (Exception ex) { return LdapTestConnectionResult.Fail(ex.Message); }
    }

    #region Helpers

    private async Task<OperationResult<T>> GetAsync<T>(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            var result = await response.Content.ReadFromJsonAsync<OperationResult<T>>(_jsonOptions);
            return result ?? OperationResult<T>.Fail("Deserialisierungsfehler.");
        }
        catch (Exception ex) { return OperationResult<T>.Fail(ex.Message); }
    }

    private async Task<OperationResult<T>> PostAsync<T>(string url, object payload)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, _jsonOptions);
            var result = await response.Content.ReadFromJsonAsync<OperationResult<T>>(_jsonOptions);
            return result ?? OperationResult<T>.Fail("Deserialisierungsfehler.");
        }
        catch (Exception ex) { return OperationResult<T>.Fail(ex.Message); }
    }

    private async Task<OperationResult<T>> PutAsync<T>(string url, object payload)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(url, payload, _jsonOptions);
            var result = await response.Content.ReadFromJsonAsync<OperationResult<T>>(_jsonOptions);
            return result ?? OperationResult<T>.Fail("Deserialisierungsfehler.");
        }
        catch (Exception ex) { return OperationResult<T>.Fail(ex.Message); }
    }

    private async Task<OperationResult> DeleteAsync(string url)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(url);
            var result = await response.Content.ReadFromJsonAsync<OperationResult>(_jsonOptions);
            return result ?? OperationResult.Fail("Deserialisierungsfehler.");
        }
        catch (Exception ex) { return OperationResult.Fail(ex.Message); }
    }

    #endregion
}
