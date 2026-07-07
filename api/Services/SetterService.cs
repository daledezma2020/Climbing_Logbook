using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class SetterService : ISetterService
{
    private readonly ApplicationDbContext _context;

    public SetterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Setter>> GetSettersAsync()
    {
        return await _context.Setters
            .OrderByDescending(s => s.Name)
            .ToListAsync();
    }

    public async Task<Setter?> GetSetterByIdAsync(int id)
    {
        return await _context.Setters
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Setter?> GetSetterByNameAsync(string name)
    {
        return await _context.Setters
            .FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<Setter> CreateSetterAsync(Setter setter)
    {
        _context.Setters.Add(setter);
        await _context.SaveChangesAsync();
        return setter;
    }

    public async Task<Setter?> UpdateSetterAsync(int id, Setter setter)
    {
        var existingSetter = await _context.Setters.FindAsync(id);

        if (existingSetter == null)
        {
            return null;
        }

        existingSetter.Name = setter.Name;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await SetterExistsAsync(id))
            {
                return null;
            }
            throw;
        }

        return existingSetter;
    }

    public async Task<bool> DeleteSetterAsync(int id)
    {
        var setter = await _context.Setters.FindAsync(id);

        if (setter == null)
        {
            return false;
        }

        _context.Setters.Remove(setter);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetterExistsAsync(int id)
    {
        return await _context.Setters.AnyAsync(s => s.Id == id);
    }
}
