using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using PyroCache.Settings;

namespace PyroCache.Yaml;

public static class YamlConfigurationExtensions
{
    public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder,
        string path)
    {
        return AddYamlFile(builder, provider: null, path: path, optional: false, reloadOnChange: false);
    }

    public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder,
        string path,
        bool optional)
    {
        return AddYamlFile(builder, provider: null, path: path, optional: optional, reloadOnChange: false);
    }

    public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder,
        string path,
        bool optional,
        bool reloadOnChange)
    {
        return AddYamlFile(builder, provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange);
    }

    public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder,
        IFileProvider? provider,
        string path,
        bool optional,
        bool reloadOnChange)
    {
        if (provider is null && Path.IsPathRooted(path))
        {
            provider = new PhysicalFileProvider(Path.GetDirectoryName(path));
            path = Path.GetFileName(path);
        }

        var source = new YamlConfigurationSource
        {
            FileProvider = provider,
            Path = path,
            Optional = optional,
            SectionName = nameof(CacheSettings),
            ReloadOnChange = reloadOnChange
        };
        builder.Add(source);
        return builder;
    }
}