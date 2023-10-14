using System.Text;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands;

[Command(Key = "Login")]
public sealed class LoginCommand : IAsyncCommand<StringPackageInfo>
{
    private readonly PyroCache _cache;

    public LoginCommand(PyroCache  cache)
    {
        _cache = cache;
    }

    public async ValueTask ExecuteAsync(
        IAppSession session,
        StringPackageInfo package)
    {
        var response = package.Parameters.Any(p => p == "Jack")
            ? "Login successful\n"
            : "Login failed\n";
        
        await session.SendAsync(Encoding.UTF8.GetBytes(response).AsMemory());
    }
}