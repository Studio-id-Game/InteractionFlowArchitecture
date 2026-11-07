using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Secure.Entities;

namespace InteractionFlow.Samples.Notepad.Secure.ExternalPorts
{
    public interface ICredentialPersistencePort : IPersistencePort<string, CredentialData> { }
    public interface ICredentialSerializerPort : ISerializerPort<System.IO.Stream, CredentialData> { }
}
