using AutoLogin.App.Models;

namespace AutoLogin.App.Services.Storage;

public interface ILoginEntryRepository
{
    Task InitializeAsync();

    Task<IReadOnlyList<LoginEntry>> GetAllAsync();

    Task SaveAsync(LoginEntry entry);

    Task DeleteAsync(Guid id);
}
