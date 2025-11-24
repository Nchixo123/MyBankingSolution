using AutoMapper;
using BankingSystem.Application.Dtos;
using BankingSystem.Application.Services.Interfaces;
using BankingSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Infrastructure.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IJwtService jwtService,
    IMapper mapper) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IMapper _mapper = mapper;

    public async Task<(bool success, string message, UserDto? user)> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return (false, "Email already registered", null);

        var user = _mapper.Map<ApplicationUser>(dto);

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, errors, null);
        }

        await EnsureRoleExistsAsync("Customer");
        await _userManager.AddToRoleAsync(user, "Customer");

        var userDto = await MapToUserDtoAsync(user);
        return (true, "Registration successful", userDto);
    }

    public async Task<(bool success, string message, UserDto? user)> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return (false, "Invalid email or password", null);

        var result = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!result)
            return (false, "Invalid email or password", null);

        if (!user.IsActive)
            return (false, "Account is inactive", null);

        var userDto = await MapToUserDtoAsync(user);
        return (true, "Login successful", userDto);
    }

    public async Task<string> GenerateJwtTokenAsync(UserDto user)
    {
        var appUser = await _userManager.FindByIdAsync(user.Id);
        if (appUser == null)
            throw new InvalidOperationException("User not found");

        return await _jwtService.GenerateTokenAsync(appUser);
    }

    public async Task<(bool success, string message)> AssignRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found");

        await EnsureRoleExistsAsync(role);

        if (await _userManager.IsInRoleAsync(user, role))
            return (false, $"User already has {role} role");

        var result = await _userManager.AddToRoleAsync(user, role);

        return result.Succeeded
            ? (true, $"{role} role assigned successfully")
            : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null ? await MapToUserDtoAsync(user) : null;
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null ? await MapToUserDtoAsync(user) : null;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            userDtos.Add(await MapToUserDtoAsync(user));
        }

        return userDtos;
    }

    private async Task EnsureRoleExistsAsync(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task<UserDto> MapToUserDtoAsync(ApplicationUser user)
    {
        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = await _userManager.GetRolesAsync(user);
        return userDto;
    }
}
