using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.PIA;

/// <summary>
/// Control PIA (Private Internet Access) VPN client via its CLI tool (piactl.exe).
/// </summary>
public class Pia
{
    private readonly Cli _pia;

    /// <summary>
    /// Wrap the PIA CLI tool in the folder at the given path.
    /// </summary>
    /// <param name="path">The path to the folder containing the PIA CLI tool.</param>
    /// <exception cref="ArgumentException"></exception>
    public Pia(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!IsInstallPath(path))
            throw new ArgumentException("The specified path does not contain the piactl.exe CLI tool.", nameof(path));

        _pia = new Cli(PiaExePath(path));
    }

    /// <summary>
    /// Returns the currently selected region.
    /// </summary>
    public async Task<PiaRegion> RegionAsync()
    {
        return new(await _pia.ReadAsync("get region"));
    }

    /// <summary>
    /// Returns a list of currently available regions.
    /// </summary>
    public async Task<IReadOnlyList<PiaRegion>> RegionsAsync()
    {
        var output = await _pia.ReadAsync("get regions");
        return [.. output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries).Select(region => new PiaRegion(region))];
    }

    /// <summary>
    /// Returns <see langword="true"/> if currently connected to the VPN.
    /// </summary>
    public async Task<bool> IsConnectedAsync()
    {
        var status = await _pia.ReadAsync("get connectionstate");
        return status.Equals("Connected", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Connects to the VPN if not already connected. Uses the currently selected region.
    /// </summary>
    public async Task ConnectAsync()
    {
        await _pia.RunAsync("background enable");
        await _pia.RunAsync("connect");
    }

    /// <summary>
    /// Connects to the VPN if not already connected. Uses the specified region.
    /// </summary>
    /// <param name="region">The region to connect to.</param>
    public async Task ConnectAsync(PiaRegion region)
    {
        await _pia.RunAsync("set region " + region);
        await ConnectAsync();
    }

    /// <summary>
    /// Disconnects from the VPN if currently connected.
    /// </summary>
    public async Task DisconnectAsync()
    {
        await _pia.RunAsync("disconnect");
    }

    /// <summary>
    /// Returns <see langword="true"/> if PIA is installed and the CLI tool is available at the given path.
    /// </summary>
    public static bool IsInstallPath(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(PiaExePath(path));
    }

    /// <summary>
    /// Tries to find the PIA installation folder and returns a <see cref="Pia"/> if successful.
    /// </summary>
    public static bool TryFindInstall(out Pia pia)
    {
        foreach (var path in CandidateInstallFolders())
        {
            if (IsInstallPath(path))
            {
                pia = new Pia(path);
                return true;
            }
        }

        pia = null;
        return false;
    }

    private static IEnumerable<string> CandidateInstallFolders()
    {
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Private Internet Access");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Private Internet Access");
    }

    private static string PiaExePath(string installPath)
    {
        return Path.Combine(installPath, "piactl.exe");
    }
}

/// <summary>
/// Represents a region available in PIA.
/// </summary>
public class PiaRegion
{
    private readonly string _value;

    /// <summary>
    /// The country code of the region, if available. Otherwise <see langword="null"/>.
    /// </summary>
    public string CountryCode { get; }

    /// <summary>
    /// Display name of the region.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Indicates whether the region is optimized for streaming.
    /// </summary>
    public bool IsStreamingOptimized { get; }

    /// <summary>
    /// Creates a new region from the raw value.
    /// </summary>
    public PiaRegion (string value)
    {
        _value = value;
        Name = value;

        if (value.EndsWith("-streaming-optimized", StringComparison.OrdinalIgnoreCase))
        {
            IsStreamingOptimized = true;
            Name = value[..^"-streaming-optimized".Length];
        }

        var parts = Name.Split('-');
        if (parts.Length >= 1 && parts[0].Length == 2)
        {
            CountryCode = parts[0].ToLowerInvariant();
            var country = _countries.FirstOrDefault(x => x.Alpha2.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
            if (country != default && Name.Length > 3)
            {
                Name = $"{country.Name}, {ToTitle(Name[3..])}";
            }
        }
        else
        {
            Name = ToTitle(Name);
            var country = _countries.FirstOrDefault(x => x.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
            if (country != default)
            {
                CountryCode = country.Alpha2.ToLowerInvariant();
            }
        }
    }

    /// <summary>
    /// Returns the raw value of the region as used in the PIA CLI tool.
    /// </summary>
    public override string ToString() => _value;

    private static string ToTitle(string text)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.Replace('-', ' '));
    }

    private readonly List<(string Alpha2, string Name)> _countries =
    [
        ("AF", "Afghanistan"),
        ("AL", "Albania"),
        ("DZ", "Algeria"),
        ("AS", "American Samoa"),
        ("AD", "Andorra"),
        ("AO", "Angola"),
        ("AI", "Anguilla"),
        ("AQ", "Antarctica"),
        ("AG", "Antigua and Barbuda"),
        ("AR", "Argentina"),
        ("AM", "Armenia"),
        ("AW", "Aruba"),
        ("AU", "Australia"),
        ("AT", "Austria"),
        ("AZ", "Azerbaijan"),
        ("BS", "Bahamas (the)"),
        ("BH", "Bahrain"),
        ("BD", "Bangladesh"),
        ("BB", "Barbados"),
        ("BY", "Belarus"),
        ("BE", "Belgium"),
        ("BZ", "Belize"),
        ("BJ", "Benin"),
        ("BM", "Bermuda"),
        ("BT", "Bhutan"),
        ("BO", "Bolivia (Plurinational State of)"),
        ("BQ", "Bonaire, Sint Eustatius and Saba"),
        ("BA", "Bosnia and Herzegovina"),
        ("BW", "Botswana"),
        ("BV", "Bouvet Island"),
        ("BR", "Brazil"),
        ("IO", "British Indian Ocean Territory (the)"),
        ("BN", "Brunei Darussalam"),
        ("BG", "Bulgaria"),
        ("BF", "Burkina Faso"),
        ("BI", "Burundi"),
        ("CV", "Cabo Verde"),
        ("KH", "Cambodia"),
        ("CM", "Cameroon"),
        ("CA", "Canada"),
        ("KY", "Cayman Islands (the)"),
        ("CF", "Central African Republic (the)"),
        ("TD", "Chad"),
        ("CL", "Chile"),
        ("CN", "China"),
        ("CX", "Christmas Island"),
        ("CC", "Cocos (Keeling) Islands (the)"),
        ("CO", "Colombia"),
        ("KM", "Comoros (the)"),
        ("CD", "Congo (the Democratic Republic of the)"),
        ("CG", "Congo (the)"),
        ("CK", "Cook Islands (the)"),
        ("CR", "Costa Rica"),
        ("HR", "Croatia"),
        ("CU", "Cuba"),
        ("CW", "Curaçao"),
        ("CY", "Cyprus"),
        ("CZ", "Czechia"),
        ("CI", "Côte d'Ivoire"),
        ("DK", "Denmark"),
        ("DJ", "Djibouti"),
        ("DM", "Dominica"),
        ("DO", "Dominican Republic (the)"),
        ("EC", "Ecuador"),
        ("EG", "Egypt"),
        ("SV", "El Salvador"),
        ("GQ", "Equatorial Guinea"),
        ("ER", "Eritrea"),
        ("EE", "Estonia"),
        ("SZ", "Eswatini"),
        ("ET", "Ethiopia"),
        ("FK", "Falkland Islands (the) [Malvinas]"),
        ("FO", "Faroe Islands (the)"),
        ("FJ", "Fiji"),
        ("FI", "Finland"),
        ("FR", "France"),
        ("GF", "French Guiana"),
        ("PF", "French Polynesia"),
        ("TF", "French Southern Territories (the)"),
        ("GA", "Gabon"),
        ("GM", "Gambia (the)"),
        ("GE", "Georgia"),
        ("DE", "Germany"),
        ("GH", "Ghana"),
        ("GI", "Gibraltar"),
        ("GR", "Greece"),
        ("GL", "Greenland"),
        ("GD", "Grenada"),
        ("GP", "Guadeloupe"),
        ("GU", "Guam"),
        ("GT", "Guatemala"),
        ("GG", "Guernsey"),
        ("GN", "Guinea"),
        ("GW", "Guinea-Bissau"),
        ("GY", "Guyana"),
        ("HT", "Haiti"),
        ("HM", "Heard Island and McDonald Islands"),
        ("VA", "Holy See (the)"),
        ("HN", "Honduras"),
        ("HK", "Hong Kong"),
        ("HU", "Hungary"),
        ("IS", "Iceland"),
        ("IN", "India"),
        ("ID", "Indonesia"),
        ("IR", "Iran (Islamic Republic of)"),
        ("IQ", "Iraq"),
        ("IE", "Ireland"),
        ("IM", "Isle of Man"),
        ("IL", "Israel"),
        ("IT", "Italy"),
        ("JM", "Jamaica"),
        ("JP", "Japan"),
        ("JE", "Jersey"),
        ("JO", "Jordan"),
        ("KZ", "Kazakhstan"),
        ("KE", "Kenya"),
        ("KI", "Kiribati"),
        ("KP", "Korea (the Democratic People's Republic of)"),
        ("KR", "Korea (the Republic of)"),
        ("KW", "Kuwait"),
        ("KG", "Kyrgyzstan"),
        ("LA", "Lao People's Democratic Republic (the)"),
        ("LV", "Latvia"),
        ("LB", "Lebanon"),
        ("LS", "Lesotho"),
        ("LR", "Liberia"),
        ("LY", "Libya"),
        ("LI", "Liechtenstein"),
        ("LT", "Lithuania"),
        ("LU", "Luxembourg"),
        ("MO", "Macao"),
        ("MG", "Madagascar"),
        ("MW", "Malawi"),
        ("MY", "Malaysia"),
        ("MV", "Maldives"),
        ("ML", "Mali"),
        ("MT", "Malta"),
        ("MH", "Marshall Islands (the)"),
        ("MQ", "Martinique"),
        ("MR", "Mauritania"),
        ("MU", "Mauritius"),
        ("YT", "Mayotte"),
        ("MX", "Mexico"),
        ("FM", "Micronesia (Federated States of)"),
        ("MD", "Moldova (the Republic of)"),
        ("MC", "Monaco"),
        ("MN", "Mongolia"),
        ("ME", "Montenegro"),
        ("MS", "Montserrat"),
        ("MA", "Morocco"),
        ("MZ", "Mozambique"),
        ("MM", "Myanmar"),
        ("NA", "Namibia"),
        ("NR", "Nauru"),
        ("NP", "Nepal"),
        ("NL", "Netherlands (the)"),
        ("NC", "New Caledonia"),
        ("NZ", "New Zealand"),
        ("NI", "Nicaragua"),
        ("NE", "Niger (the)"),
        ("NG", "Nigeria"),
        ("NU", "Niue"),
        ("NF", "Norfolk Island"),
        ("MP", "Northern Mariana Islands (the)"),
        ("NO", "Norway"),
        ("OM", "Oman"),
        ("PK", "Pakistan"),
        ("PW", "Palau"),
        ("PS", "Palestine, State of"),
        ("PA", "Panama"),
        ("PG", "Papua New Guinea"),
        ("PY", "Paraguay"),
        ("PE", "Peru"),
        ("PH", "Philippines (the)"),
        ("PN", "Pitcairn"),
        ("PL", "Poland"),
        ("PT", "Portugal"),
        ("PR", "Puerto Rico"),
        ("QA", "Qatar"),
        ("MK", "Republic of North Macedonia"),
        ("RO", "Romania"),
        ("RU", "Russian Federation (the)"),
        ("RW", "Rwanda"),
        ("RE", "Réunion"),
        ("BL", "Saint Barthélemy"),
        ("SH", "Saint Helena, Ascension and Tristan da Cunha"),
        ("KN", "Saint Kitts and Nevis"),
        ("LC", "Saint Lucia"),
        ("MF", "Saint Martin (French part)"),
        ("PM", "Saint Pierre and Miquelon"),
        ("VC", "Saint Vincent and the Grenadines"),
        ("WS", "Samoa"),
        ("SM", "San Marino"),
        ("ST", "Sao Tome and Principe"),
        ("SA", "Saudi Arabia"),
        ("SN", "Senegal"),
        ("RS", "Serbia"),
        ("SC", "Seychelles"),
        ("SL", "Sierra Leone"),
        ("SG", "Singapore"),
        ("SX", "Sint Maarten (Dutch part)"),
        ("SK", "Slovakia"),
        ("SI", "Slovenia"),
        ("SB", "Solomon Islands"),
        ("SO", "Somalia"),
        ("ZA", "South Africa"),
        ("GS", "South Georgia and the South Sandwich Islands"),
        ("SS", "South Sudan"),
        ("ES", "Spain"),
        ("LK", "Sri Lanka"),
        ("SD", "Sudan (the)"),
        ("SR", "Suriname"),
        ("SJ", "Svalbard and Jan Mayen"),
        ("SE", "Sweden"),
        ("CH", "Switzerland"),
        ("SY", "Syrian Arab Republic"),
        ("TW", "Taiwan (Province of China)"),
        ("TJ", "Tajikistan"),
        ("TZ", "Tanzania, United Republic of"),
        ("TH", "Thailand"),
        ("TL", "Timor-Leste"),
        ("TG", "Togo"),
        ("TK", "Tokelau"),
        ("TO", "Tonga"),
        ("TT", "Trinidad and Tobago"),
        ("TN", "Tunisia"),
        ("TR", "Turkey"),
        ("TM", "Turkmenistan"),
        ("TC", "Turks and Caicos Islands (the)"),
        ("TV", "Tuvalu"),
        ("UG", "Uganda"),
        ("UA", "Ukraine"),
        ("AE", "United Arab Emirates (the)"),
        ("GB", "United Kingdom of Great Britain and Northern Ireland (the)"),
        ("UM", "United States Minor Outlying Islands (the)"),
        ("US", "United States of America (the)"),
        ("UY", "Uruguay"),
        ("UZ", "Uzbekistan"),
        ("VU", "Vanuatu"),
        ("VE", "Venezuela (Bolivarian Republic of)"),
        ("VN", "Viet Nam"),
        ("VG", "Virgin Islands (British)"),
        ("VI", "Virgin Islands (U.S.)"),
        ("WF", "Wallis and Futuna"),
        ("EH", "Western Sahara"),
        ("YE", "Yemen"),
        ("ZM", "Zambia"),
        ("ZW", "Zimbabwe"),
        ("AX", "Åland Islands")
    ];
}
