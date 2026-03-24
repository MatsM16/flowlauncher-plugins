using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.PIA;

/// <summary>
/// FlowLauncher plugin for PIA (Private Internet Access) VPN client. This plugin allows users to interact with the PIA application directly from FlowLauncher, providing quick access to VPN features and settings.
/// </summary>
public class PiaPlugin : IAsyncPlugin
{
    private PluginInitContext _context;
    private Pia _pia;

    private IReadOnlyList<PiaRegion> _regions = [];

    /// <inheritdoc />
    public async Task InitAsync(PluginInitContext context)
    {
        _context = context;
        if (Pia.TryFindInstall(out _pia))
            _regions = await _pia.RegionsAsync();
    }

    /// <inheritdoc />
    public async Task<List<Result>> QueryAsync(Query query, CancellationToken cancellationToken)
    {
        if (_pia == null)
            return [PiaNotInstalled()];

        var region = await _pia.RegionAsync();
        var isConnected = await _pia.IsConnectedAsync();

        if (string.IsNullOrWhiteSpace(query.Search))
            return [RegionResult(region, true, isConnected, "")];

        if (_regions.Count is 0)
            return [new Result
            {
                Title = "No regions found",
                SubTitle = "Please ensure PIA is properly configured and try again.",
                IcoPath = "Images\\pia.png",
                RecordKey = "no_regions",
                Action = _ =>
                {
                    _context.API.OpenSettingDialog();
                    return true;
                }
            }];

        return [..SwitchToRegionResults(query.Search, region, isConnected)];
    }

    private IEnumerable<Result> SwitchToRegionResults(string search, PiaRegion selected, bool isConnected) => _regions
        .Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
        .Where(r => !r.Equals(selected))
        .OrderBy(r => r.Name.Equals(search, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ThenBy(r => r.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ThenBy(r => r.Name)
        .Select(r => RegionResult(r, r.Equals(selected), isConnected, search));

    private Result RegionResult(PiaRegion region, bool selected, bool connected, string query)
    {
        var result = new Result();
        Apply(result, region, selected, connected, query);
        return result;
    } 

    private void Apply(Result result, PiaRegion region, bool selected, bool connected, string query)
    {
        result.Title = selected ? $"{region.Name} ({(connected ? "current" : "current, disconnected")})" : region.Name;
        result.IcoPath = region.CountryCode is null ? "Images\\pia.png" : $"Images\\{region.CountryCode}.svg";
        result.RecordKey = region.ToString();
        result.SubTitle = !connected ? $"Connect to {region}" : selected ? $"Disconnect from {region}" : $"Switch to {region}";

        if (connected) result.AsyncAction = async _ =>
        {
            await _pia.DisconnectAsync();
            connected = false;
            Apply(result, region, selected, connected, query);
            return true;
        };
        else if (selected) result.AsyncAction = async _ =>
        {
            await _pia.ConnectAsync();
            connected = true;
            Apply(result, region, selected, connected, query);
            return true;
        };
        else result.AsyncAction = async _ =>
        {
            await _pia.ConnectAsync(region);
            connected = true;
            selected = true;
            Apply(result, region, selected, connected, query);
            return true;
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var index = region.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var data = new int[query.Length];
                for (var i = 0; i < query.Length; i++) data[i] = index + i;
                result.TitleHighlightData = data;
            }
        }
    }

    private Result PiaNotInstalled() => new()
    {
        Title = "PIA not found",
        SubTitle = "Please ensure Private Internet Access is installed and the path is correct in settings.",
        IcoPath = "Images\\pia.png",
        RecordKey = "pia_not_ready",
        Action = _ =>
        {
            _context.API.OpenSettingDialog();
            return true;
        }
    };
}
