﻿using BanHistoryMicroservice.Api.Models;
using BanHistoryMicroservice.Api.Services;
using MassTransit;
using MessageBus.Messages.BanMessages;

namespace BanHistoryMicroservice.Api.MessageBus.MessageBusConsumers
{
    public class UserBannedByUserNameBanHistoryConsumer : IConsumer<IUserBannedByUserName>
    {
        private readonly IBanService<Ban> banService;

        public UserBannedByUserNameBanHistoryConsumer(IBanService<Ban> banService)
        {
            this.banService = banService;
        }
        public async Task Consume(ConsumeContext<IUserBannedByUserName> context)
        {
            await banService.CreateAsync(new Ban
            {
                Id = Guid.NewGuid(), UserName = context.Message.UserName, Reason = context.Message.Reason,
                BanType = context.Message.BanType, DurationInDays = context.Message.DurationIdDays, UserId = context.Message.UserId, 
                BannedBy = context.Message.BannedBy, BannedFrom = DateTime.UtcNow, BannedUntil = DateTime.UtcNow.AddDays(context.Message.DurationIdDays)
            });
        }
    }
}
