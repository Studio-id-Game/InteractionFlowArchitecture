using InteractionFlow.Core.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts
{

    public interface IPersistencePort<TPersistenceId, TValue>
    {
        Task<Result> Save(TPersistenceId id, Result<TValue> value);

        Task<Result<TValue>> Load(TPersistenceId id, Result<TValue> oldValue);

        Task<Result> Delete(TPersistenceId id);

        Task<Result> Exist(TPersistenceId id);

        Task<Result<TPersistenceId[]>> GetAllId();
    }
}
