﻿namespace RegisterMicroservice.Api.DTOs
{
    public class UserParameters
    {
        private const int MaxPageSize = 30;
        public int PageNumber { get; set; } = 1;

        private int pageSize = 10;

        public int PageSize
        {
            get => pageSize;
            set => pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }
    }
}
