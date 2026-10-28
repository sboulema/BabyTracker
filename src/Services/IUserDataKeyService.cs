using System.Threading.Tasks;

namespace BabyTracker.Services;

public interface IUserDataKeyService
{
    Task<byte[]> GetOrCreateKeyAsync(string userId);
}
