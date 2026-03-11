using System.Net.Http.Json;
using System.Text.Json;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models.Teams;
using Authify.UI.Services;

namespace Authify.Client.Wasm.Services;

public class WasmTeamDataService : ITeamDataService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WasmTeamDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region Helpers

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

    private async Task<OperationResult> PostAsync(string url, object payload)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, _jsonOptions);
            var result = await response.Content.ReadFromJsonAsync<OperationResult>(_jsonOptions);
            return result ?? OperationResult.Fail("Deserialisierungsfehler.");
        }
        catch (Exception ex) { return OperationResult.Fail(ex.Message); }
    }

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

    #endregion

    public Task<OperationResult<TeamDto>> CreateTeamAsync(CreateTeamRequest request) =>
        PostAsync<TeamDto>("api/team/create", request);

    public Task<OperationResult<TeamDto>> GetMyTeamAsync() =>
        GetAsync<TeamDto>("api/team/my-team");

    public Task<OperationResult<TeamDto>> UpdateTeamAsync(UpdateTeamRequest request) =>
        PutAsync<TeamDto>("api/team/update", request);

    public Task<OperationResult> DeleteTeamAsync() =>
        DeleteAsync("api/team/delete");

    public Task<OperationResult<List<TeamMemberDto>>> GetMembersAsync() =>
        GetAsync<List<TeamMemberDto>>("api/team/members");

    public Task<OperationResult<TeamMemberDto>> CreateMemberAsync(CreateTeamMemberRequest request) =>
        PostAsync<TeamMemberDto>("api/team/members/create", request);

    public Task<OperationResult> RemoveMemberAsync(string memberId) =>
        DeleteAsync($"api/team/members/{memberId}");

    public Task<OperationResult<TeamInvitationDto>> CreateInvitationAsync(CreateInvitationRequest request) =>
        PostAsync<TeamInvitationDto>("api/teaminvitation/create", request);

    public Task<OperationResult<List<TeamInvitationDto>>> GetInvitationsAsync() =>
        GetAsync<List<TeamInvitationDto>>("api/teaminvitation/list");

    public Task<OperationResult> RevokeInvitationAsync(string invitationId) =>
        DeleteAsync($"api/teaminvitation/{invitationId}/revoke");

    public Task<OperationResult<TeamInvitationDto>> GetInvitationByTokenAsync(string token) =>
        GetAsync<TeamInvitationDto>($"api/teaminvitation/by-token/{Uri.EscapeDataString(token)}");

    public Task<OperationResult<string>> AcceptInvitationAsync(AcceptInvitationRequest request) =>
        PostAsync<string>("api/teaminvitation/accept", request);

    public Task<OperationResult<bool>> IsTeamAdminAsync() =>
        GetAsync<bool>("api/team/is-admin");

    public Task<OperationResult<bool>> IsTeamMemberAsync() =>
        GetAsync<bool>("api/team/is-member");
}
