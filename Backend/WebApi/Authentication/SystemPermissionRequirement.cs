using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authentication;

public sealed record SystemPermissionRequirement(SystemPermission Permission) : IAuthorizationRequirement;
