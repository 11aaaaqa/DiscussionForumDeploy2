﻿namespace RegisterMicroservice.Api.DTOs.User
{
    public class CreateBotAccountDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
    }
}
