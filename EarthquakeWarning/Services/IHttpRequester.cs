using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EarthquakeWarning.Services;

public interface IHttpRequester
{
    Task<string> GetString(string url, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
    Task<string> PostString(string url, HttpContent? data = null, CancellationToken cancellationToken = default);
}