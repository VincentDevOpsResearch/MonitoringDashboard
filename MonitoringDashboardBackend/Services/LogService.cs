using k8s;
using k8s.Models;
using System.Text.RegularExpressions;
using MonitoringDashboard.Models;

namespace MonitoringDashboard.Services
{
    public class LogService
    {
        private readonly IKubernetes _kubernetes;

        public LogService(IKubernetes kubernetes)
        {
            _kubernetes = kubernetes;
        }


        // Get namespaces
        public async Task<List<string>> GetNamespacesAsync()
        {
            var namespaces = await _kubernetes.CoreV1.ListNamespaceAsync();
            return namespaces.Items
                .Select(n => n.Metadata.Name )
                .ToList();
        }

        // Get Pods
        public async Task<List<string>> GetPodsAsync(string namespaceName)
        {
            var pods = await _kubernetes.CoreV1.ListNamespacedPodAsync(namespaceName);
            return pods.Items
                .Select(p => p.Metadata.Name )
                .ToList();
        }

        // Get Pod Logs
        public async Task<PaginatedLogResponse> GetPaginatedPodLogsAsync(
            string namespaceName,
            string podName,
            DateTime? startTime,
            DateTime? endTime,  
            int maxLines = 100)
        {
            var logStartTime = startTime ?? DateTime.UtcNow;
            var sinceSeconds = (int)(DateTime.UtcNow - logStartTime).TotalSeconds;

            var logStream = await _kubernetes.CoreV1.ReadNamespacedPodLogAsync(
                name: podName,
                namespaceParameter: namespaceName,
                follow: false,
                timestamps: true,
                sinceSeconds: startTime.HasValue ? (int)(DateTime.UtcNow - logStartTime).TotalSeconds : null
            );

            using (var reader = new StreamReader(logStream))
            {
                var logs = new List<LogEntry>();
                string line;
                DateTime? nextStartTime = null;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var parsedLog = ParseLogLine(line);
                    if (parsedLog == null) continue;

                    if (logs.Count >= maxLines || (endTime.HasValue && parsedLog.Timestamp >= endTime.Value))
                    {
                        nextStartTime = parsedLog.Timestamp;
                        break;
                    }

                    logs.Add(parsedLog);
                }

                return new PaginatedLogResponse
                {
                    Logs = logs,
                    NextStartTime = nextStartTime, 
                    HasMore = nextStartTime.HasValue 
                };
            }
        }


        private static readonly Regex LogLineRegex = new Regex(
            @"^(?<timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d+Z)\s+(?<log>.+)$",
            RegexOptions.Compiled);

        private LogEntry ParseLogLine(string logLine)
        {
            var match = LogLineRegex.Match(logLine);

            if (!match.Success) return null;

            if (DateTime.TryParse(match.Groups["timestamp"].Value, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var timestamp))
            {
                return new LogEntry
                {
                    Timestamp = timestamp,
                    Content = match.Groups["log"].Value
                };
            }

            return null;
        }
    }
}
