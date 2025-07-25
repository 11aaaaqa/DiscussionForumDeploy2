﻿using MassTransit;
using MessageBus.Messages.BanMessages;
using Microsoft.EntityFrameworkCore;
using UserMicroservice.Api.Database;

namespace UserMicroservice.Api.Services.Ban
{
    public class BanService : IBanService<Models.User>
    {
        private readonly ApplicationDbContext context;
        private readonly IPublishEndpoint publishEndpoint;

        public BanService(ApplicationDbContext context, IPublishEndpoint publishEndpoint)
        {
            this.context = context;
            this.publishEndpoint = publishEndpoint;
        }

        public async Task<bool> IsUserBannedAsync(Guid userId, params string[] banTypes)
        {
            var user = await context.Users.SingleOrDefaultAsync(x => x.Id == userId);
            if (user is null) return false;
            if (!user.IsBanned) return false;

            if (DateTime.UtcNow > user.BannedUntil)
            {
                user.IsBanned = false;
                user.BanType = "Not banned";
                user.BannedFor = "Not banned";
                user.BannedUntil = new DateTime();
                context.Users.Update(user);
                await context.SaveChangesAsync();
                return false;
            }

            if (banTypes.Length == 0)
                return true;
            
            foreach (var banType in banTypes)
            {
                if (user.BanType == banType)
                    return true;
            }

            return false;
        }

        public async Task<bool> IsUserBannedAsync(string userName, params string[] banTypes)
        {
            var user = await context.Users.SingleOrDefaultAsync(x => x.UserName == userName);
            if (user is null) return false;
            if (!user.IsBanned) return false;

            if (DateTime.UtcNow > user.BannedUntil)
            {
                user.IsBanned = false;
                user.BanType = "Not banned";
                user.BannedFor = "Not banned";
                user.BannedUntil = new DateTime();
                context.Users.Update(user);
                await context.SaveChangesAsync();
                return false;
            }

            if (banTypes.Length == 0)
                return true;

            foreach (var banType in banTypes)
            {
                if (user.BanType == banType)
                    return true;
            }

            return false;
        }

        public async Task BanUserAsync(Guid userId, string reason, string banType, uint forDays, string bannedBy)
        {
            var user = await context.Users.SingleOrDefaultAsync(x => x.Id == userId);
            if (user is null) return;

            user.IsBanned = true;
            user.BanType = banType;
            user.BannedFor = reason;
            user.BannedUntil = DateTime.UtcNow.AddDays(forDays);
            context.Users.Update(user);
            await context.SaveChangesAsync();

            await publishEndpoint.Publish<IUserBannedByUserId>(new
            {
                UserId = userId,
                Reason = reason,
                BanType = banType,
                DurationIdDays = forDays,
                user.UserName,
                BannedBy = bannedBy
            });
        }

        public async Task BanUserAsync(string userName, string reason, string banType, uint forDays, string bannedBy)
        {
            var user = await context.Users.SingleOrDefaultAsync(x => x.UserName == userName);
            if (user is null) return;

            user.IsBanned = true;
            user.BanType = banType;
            user.BannedFor = reason;
            user.BannedUntil = DateTime.UtcNow.AddDays(forDays);
            context.Users.Update(user);
            await context.SaveChangesAsync();

            await publishEndpoint.Publish<IUserBannedByUserName>(new
            {
                UserName = userName,
                Reason = reason,
                BanType = banType,
                DurationIdDays = forDays,
                UserId = user.Id,
                BannedBy = bannedBy
            });
        }

        public async Task UnbanUserAsync(Guid userId)
        {
            var user = await context.Users.SingleOrDefaultAsync(x => x.Id == userId);
            if (user is null) return;

            user.IsBanned = false;
            user.BanType = "Not banned";
            user.BannedFor = "Not banned";
            user.BannedUntil = new DateTime();
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }

        public async Task UnbanUserAsync(string userName)
        {
            var user = await context.Users.SingleOrDefaultAsync(x => x.UserName == userName);
            if (user is null) return;

            user.IsBanned = false;
            user.BanType = "Not banned";
            user.BannedFor = "Not banned";
            user.BannedUntil = new DateTime();
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }
    }
}
