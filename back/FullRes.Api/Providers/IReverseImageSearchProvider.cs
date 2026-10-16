using FullRes.Api.Models;

namespace FullRes.Api.Providers;

public interface IReverseImageSearchProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<IReadOnlyList<SearchCandidate>> SearchAsync(PreparedImage image, CancellationToken cancellationToken);
}
