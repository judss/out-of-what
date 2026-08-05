using System.Collections.Generic;
using System.Threading.Tasks;
using OutOfWhatApp.Models;

namespace OutOfWhatApp.Services;

public interface IRatingStore
{
    Task AddAsync(RatingEntry entry);

    Task<IReadOnlyList<RatingEntry>> GetAllAsync();
}
