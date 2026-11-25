using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SeeRM.Leads.Infrastructure;

namespace SeeRM.Leads.Integration;

// Synchronous REST client for the Customer service (backend/seerm-dotnet).
// The Leads service never touches the Customer database; this HTTP API is the only link.
// Timeouts and retries are configured in Program.cs (Microsoft.Extensions.Http.Resilience).

public record CustomerSummary(long Id, string Name, string? Company, string Status);
public record CustomerCreateRequest(string FullName, string Email, string Phone, string AccountType, string? Company, string Status);
public record DealCreateRequest(string Title, decimal Value, string Stage);
public record DealSummary(long Id, string Title, decimal Value, string Stage);

internal record PagedCustomers(List<CustomerSummary> Items, int TotalCount);
internal record CreatedCustomer(long Id);

public class CustomerServiceClient(HttpClient http, ILogger<CustomerServiceClient> logger)
{
    /// <summary>Exact, case-insensitive email lookup. Null when no customer has that email.</summary>
    public async Task<CustomerSummary?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var url = $"api/customers?email={Uri.EscapeDataString(email)}&pageSize=1";
        var page = await SendAsync(c => c.GetAsync(url, ct), r => r.Content.ReadFromJsonAsync<PagedCustomers>(ct), ct);
        return page?.Items.FirstOrDefault();
    }

    /// <summary>
    /// Creates a customer and returns its id. If the Customer service reports the email already exists
    /// (409, e.g. a concurrent conversion), the existing customer is looked up and returned instead.
    /// </summary>
    public async Task<(long Id, bool Created)> CreateOrGetCustomerAsync(CustomerCreateRequest req, CancellationToken ct)
    {
        try
        {
            var created = await SendAsync(c => c.PostAsJsonAsync("api/customers", req, ct),
                r => r.Content.ReadFromJsonAsync<CreatedCustomer>(ct), ct);
            return (created!.Id, true);
        }
        catch (DownstreamConflictException)
        {
            var existing = await FindByEmailAsync(req.Email, ct)
                ?? throw new ServiceUnavailableException("Customer service reported a duplicate email but the customer could not be found.");
            return (existing.Id, false);
        }
    }

    public async Task<IReadOnlyList<DealSummary>> ListDealsAsync(long customerId, CancellationToken ct) =>
        await SendAsync(c => c.GetAsync($"api/customers/{customerId}/deals", ct),
            r => r.Content.ReadFromJsonAsync<List<DealSummary>>(ct), ct) ?? [];

    public async Task<DealSummary> CreateDealAsync(long customerId, DealCreateRequest req, CancellationToken ct) =>
        (await SendAsync(c => c.PostAsJsonAsync($"api/customers/{customerId}/deals", req, ct),
            r => r.Content.ReadFromJsonAsync<DealSummary>(ct), ct))!;

    public async Task<bool> IsHealthyAsync(CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync("health", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException
                                       or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
        {
            return false;
        }
    }

    /// <summary>
    /// Sends a request and maps failures:
    /// unreachable / timeout / 5xx → 503, 400 → 400 with the Customer service's validation messages, 409 → conflict.
    /// </summary>
    private async Task<T?> SendAsync<T>(
        Func<HttpClient, Task<HttpResponseMessage>> send, Func<HttpResponseMessage, Task<T?>> read, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await send(http);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException
                                       or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException
                                       || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Customer service call failed");
            throw new ServiceUnavailableException("The Customer service is unavailable. The lead was not converted; please try again.");
        }

        using (response)
        {
            if (response.IsSuccessStatusCode) return await read(response);

            switch (response.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    throw new BadRequestException("Customer service rejected the data: " + await DescribeProblemAsync(response, ct));
                case HttpStatusCode.Conflict:
                    throw new DownstreamConflictException(await DescribeProblemAsync(response, ct));
                case HttpStatusCode.NotFound:
                    throw new BadRequestException("Customer service: " + await DescribeProblemAsync(response, ct));
                default:
                    logger.LogWarning("Customer service returned {Status}", (int)response.StatusCode);
                    throw new ServiceUnavailableException(
                        $"The Customer service returned an error ({(int)response.StatusCode}). The lead was not converted; please try again.");
            }
        }
    }

    private static async Task<string> DescribeProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
            if (problem?.Errors is { Count: > 0 } errors)
                return string.Join("; ", errors.SelectMany(e => e.Value.Select(m => $"{e.Key}: {m}")));
            return problem?.Detail ?? problem?.Title ?? response.ReasonPhrase ?? "unknown error";
        }
        catch
        {
            return response.ReasonPhrase ?? "unknown error";
        }
    }
}

/// <summary>Internal signal: the Customer service returned 409.</summary>
public sealed class DownstreamConflictException(string message) : Exception(message);
