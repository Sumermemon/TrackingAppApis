using AutoMapper;
using RunningCompetition.Application.DTOs;
using RunningCompetition.Application.DTOs.Auth;
using RunningCompetition.Application.DTOs.Runs;
using RunningCompetition.Application.DTOs.Users;
using RunningCompetition.Domain.Entities;

namespace RunningCompetition.Application.Mappings;

/// <summary>AutoMapper profile mapping domain entities to DTOs and vice versa.</summary>
public sealed class MappingProfile : Profile
{
    /// <summary>Initializes all entity-to-DTO mappings.</summary>
    public MappingProfile()
    {
        // ── User ──────────────────────────────────────────────────────────────
        CreateMap<User, UserProfileDto>()
            .ForMember(d => d.Age, o => o.MapFrom(s => s.Age));

        CreateMap<User, UserCardDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName));

        CreateMap<User, UserSummaryDto>()
            .ForMember(d => d.Roles, o => o.MapFrom(s =>
                s.UserRoles.Select(ur => ur.Role.Name).ToList().AsReadOnly()));

        CreateMap<User, FriendDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName));

        // ── Run Session ───────────────────────────────────────────────────────
        CreateMap<RunSession, RunSessionDto>();
        CreateMap<GpsLocation, GpsLocationDto>();
        CreateMap<RunLap, RunLapDto>();

        // ── Achievements ─────────────────────────────────────────────────────
        CreateMap<Achievement, AchievementDto>();
        CreateMap<UserAchievement, UserAchievementDto>()
            .ForMember(d => d.Achievement, o => o.MapFrom(s => s.Achievement));

        // ── Notifications ─────────────────────────────────────────────────────
        CreateMap<Notification, NotificationDto>();

        // ── Referrals ─────────────────────────────────────────────────────────
        CreateMap<Referral, ReferralDto>()
            .ForMember(d => d.ReferredUserName, o => o.MapFrom(s => s.ReferredUser.FullName));

        // ── Leaderboard ───────────────────────────────────────────────────────
        CreateMap<LeaderboardEntry, LeaderboardEntryDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.User.FullName))
            .ForMember(d => d.ProfilePictureUrl, o => o.MapFrom(s => s.User.ProfilePictureUrl))
            .ForMember(d => d.City, o => o.MapFrom(s => s.User.City))
            .ForMember(d => d.Country, o => o.MapFrom(s => s.User.Country))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId));

        // ── Admin ─────────────────────────────────────────────────────────────
        CreateMap<Role, RoleDto>()
            .ForMember(d => d.Permissions, o => o.MapFrom(s =>
                s.RolePermissions.Select(rp => rp.Permission).ToList()))
            .ForMember(d => d.UserCount, o => o.MapFrom(s => s.UserRoles.Count));

        CreateMap<Permission, PermissionDto>();

        CreateMap<SystemSetting, SystemSettingDto>();
        CreateMap<Announcement, AnnouncementDto>();
        CreateMap<AuditLog, AuditLogDto>();
        CreateMap<AiSetting, AiSettingDto>();
    }
}
