﻿namespace TopicMicroservice.Api.Models
{
    public class TopicParameters
    {
        private const int MaxPageSize = 30;
        public int PageNumber { get; set; } = 1;

        private int pageSize = 20;
        public int PageSize
        {
            get => pageSize;
            set => pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }
    }
}
