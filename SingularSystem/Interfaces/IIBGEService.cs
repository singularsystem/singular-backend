using SingularSystem.Services;

namespace SingularSystem.Interfaces
{
    public interface IIBGEService
    {
        Task<IEnumerable<City>> GetCitiesByState(string uf);
    }

}