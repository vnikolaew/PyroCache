using Microsoft.Extensions.Configuration;

namespace PyroCache.Yaml;

public class YamlConfigurationSource : FileConfigurationSource
{
    public string SectionName { get; set; } = default!;
    
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        FileProvider ??= builder.GetFileProvider();
        return new YamlConfigurationProvider(this, SectionName);
    }
}