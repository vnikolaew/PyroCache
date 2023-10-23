using Microsoft.Extensions.Configuration;

namespace PyroCache.Yaml;

public class YamlConfigurationProvider : FileConfigurationProvider
{
    private readonly string? _sectionName;
    
    public YamlConfigurationProvider(YamlConfigurationSource source,
        string? sectionName = default) : base(source)
        => _sectionName = sectionName ?? string.Empty;

    public override void Load(Stream stream)
    {
        var parser = new YamlConfigurationFileParser(_sectionName);
        Data = parser.Parse(stream);
    }
}