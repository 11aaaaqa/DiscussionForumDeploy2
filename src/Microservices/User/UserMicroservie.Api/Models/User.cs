﻿using Microsoft.EntityFrameworkCore;

namespace UserMicroservice.Api.Models
{
    [Index(nameof(UserName), IsUnique = true)]
    public class User
    {
        public string AspNetUserId { get; set; }
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public uint Posts { get; set; }
        public uint Answers { get; set; }
        public DateOnly RegisteredAt { get; set; }
        public bool IsBanned { get; set; }
        public DateTime BannedUntil { get; set; }
        public string BannedFor { get; set; }
        public string BanType { get; set; }
    }
}
