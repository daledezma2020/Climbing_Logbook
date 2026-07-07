using api.Models;

namespace api.Interfaces;

public interface ISetterService
{
    Task<IEnumerable<Setter>> GetSettersAsync();
    Task<Setter?> GetSetterByIdAsync(int id);
    Task<Setter?> GetSetterByNameAsync(string name);
    Task<Setter> CreateSetterAsync(Setter setter);
    Task<Setter?> UpdateSetterAsync(int id, Setter setter);
    Task<bool> DeleteSetterAsync(int id);
    Task<bool> SetterExistsAsync(int id);
}
