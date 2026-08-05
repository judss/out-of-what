using System.Threading.Tasks;

namespace OutOfWhatApp.Services;

public interface IDailyRollProvider
{
    Task<int> GetOrCreateTodayDenominatorAsync();
}
