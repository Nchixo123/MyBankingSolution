using BankingSystem.Application.Dtos;

namespace BankingSystem.Application.Services.Interfaces;

public interface IAuthService
{
    Task<(bool success, string message, UserDto? user)> RegisterAsync(RegisterDto dto);
    Task<(bool success, string message, UserDto? user)> LoginAsync(LoginDto dto);
    Task<(bool success, string message)> AssignRoleAsync(string userId, string role);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<string> GenerateJwtTokenAsync(UserDto user);
}
