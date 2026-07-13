using SuperDentist.Core.Services;

namespace SuperDentist.Application.Services
{
    public sealed class LocalCurrentActorProvider : ICurrentActorProvider
    {
        public string Actor => "LocalUser";
    }
}
