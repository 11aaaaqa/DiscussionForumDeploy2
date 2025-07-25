﻿namespace UserMicroservice.Api.Services.Ban
{
    public interface IBanService<TUser> where TUser : class
    {
        Task<bool> IsUserBannedAsync(Guid userId, params string[] banTypes);
        Task<bool> IsUserBannedAsync(string userName, params string[] banTypes);
        Task BanUserAsync(Guid userId, string reason, string banType, uint forDays, string bannedBy);
        Task BanUserAsync(string userName, string reason, string banType, uint forDays, string bannedBy);
        Task UnbanUserAsync(Guid userId);
        Task UnbanUserAsync(string userName);
    }
}
